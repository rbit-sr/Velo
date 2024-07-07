using System.Runtime.InteropServices;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection.Emit;
using SDL2;
using System.Runtime.CompilerServices;
using System.Text;
using CEngine.Graphics.Component;
using CEngine.Graphics.Library;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Velo
{
    public class SettingsUI : ToggleModule
    {
        public BoolSetting DisableWarning;
        public BoolSetting DisableKeyInput;
        public IntSetting ControllerIndex;

        private bool initialized = false;

        private string selectedDriver = "";
        private bool unsupportedWarned = false;
        private TimeSpan unsupportedWarningTime = TimeSpan.Zero;
        private CachedFont unsupportedWarningFont;

        public bool SendUpdates = false;

        private SettingsUI() : base("UI")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)System.Windows.Forms.Keys.F1));

            DisableWarning = AddBool("disable warning", false);
            DisableWarning.Hidden = true;

            DisableKeyInput = AddBool("disable key input", false);
            ControllerIndex = AddInt("controller index", 0, 0, 3);

            DisableKeyInput.Tooltip =
                "Disables any key inputs while the settings menu is open.";
            ControllerIndex.Tooltip =
                "controller player index";
        }

        public static SettingsUI Instance = new SettingsUI();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (!initialized)
                return;

            setUiHotkey((byte)Enabled.Value.Hotkey);
        }

        public override void PostRender()
        {
            base.PostRender();

            if (unsupportedWarned)
            {
                if (Enabled.Modified())
                    Storage.Instance.Load();
                Enabled.Disable();
                DrawWarning();
                return;
            }

            if (AutoUpdate.Instance.Enabled)
                Enabled.Disable();

            if (Enabled.Modified())
            {
                if (Enabled.Value.Enabled)
                    Velo.EnableCursor(this);
                else
                    Velo.DisableCursor(this);

                if (!Enabled.Value.Enabled && initialized)
                    unfocusAll();
                if (Enabled.Value.Enabled)
                    TextInputEXT.StartTextInput();
                else
                    TextInputEXT.StopTextInput();
            }

            if (!Enabled.Value.Enabled)
                return;

            if (!initialized)
            {
                if (!InitImGui())
                {
                    unsupportedWarned = true;
                    unsupportedWarningTime = new TimeSpan(Velo.Time.Ticks);
                    CEngine.CEngine.Instance.Game.IsMouseVisible = false;
                    return;
                }
                string json = ModuleManager.Instance.ToJson(ToJsonArgs.ForUIInit).ToString(false);
                unsafe
                {
                    fixed (byte* bytes = Encoding.ASCII.GetBytes(json))
                        LoadProgram((IntPtr)bytes, json.Length);
                }
                initialized = true;
                SendUpdates = true;
                ModuleManager.Instance.AddModifiedListener(SettingUpdate);
            }

            int size = GetChangeSize();
            if (size > 0)
            {
                byte[] change = new byte[size];
                unsafe
                {
                    fixed (byte* bytes = change)
                        GetJsonUpdates((IntPtr)bytes);
                }
                string json = Encoding.ASCII.GetString(change);
                SendUpdates = false;
                ModuleManager.Instance.CommitChanges(JsonElement.FromString(json));
                SendUpdates = true;
            }

            MouseState mouse = Mouse.GetState();

            GamePadButtons(Input.GamePadButtonsDown);
            RenderImGui(
                    mouse.X,
                    mouse.Y,
                    CEngine.CEngine.Instance.GraphicsDevice.Viewport.Width,
                    CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height
                );
        }

        public void SettingUpdate(Setting setting)
        {
            if (!initialized || !SendUpdates || setting.Hidden)
                return;
            JsonObject jsonObj = new JsonObject(2).
                AddElement("Changes", new JsonArray(1).
                AddElement(setting.ToJson(ToJsonArgs.ForUIUpdate)));
            string json = jsonObj.ToString(false);
            unsafe
            {
                fixed (byte* bytes = Encoding.ASCII.GetBytes(json))
                    UpdateProgram((IntPtr)bytes, json.Length);
            }
        }

        private delegate uint GetPtrFromObjDel(object o);

        enum ERenderer
        {
            D3D11, OPEN_GL, VULKAN
        }

        public bool InitImGui()
        {
            ERenderer renderer = ERenderer.D3D11;

            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg.ToLower() == "opengl")
                {
                    renderer = ERenderer.OPEN_GL;
                    selectedDriver = arg;
                }
                if (arg.ToLower() == "vulkan")
                {
                    renderer = ERenderer.VULKAN;
                    selectedDriver = arg;
                }
            }

            if (renderer == ERenderer.D3D11)
            {
                var dyn = new DynamicMethod("GetSwapChainPtr", typeof(uint), new[] { typeof(object) }, typeof(Velo).Module);
                var il = dyn.GetILGenerator();
                il.DeclareLocal(typeof(object), true);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloc_0); // load GraphicsDevice
                il.Emit(OpCodes.Conv_I);
                il.Emit(OpCodes.Ldc_I4, 0x7C);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldind_I4); // load GraphicsDevice::GLDevice : FNA3D_Device*
                il.Emit(OpCodes.Conv_I);
                il.Emit(OpCodes.Ldc_I4, 0x12C);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldind_I4); // load FNA3D_Device::driverData : FNA3D_Renderer* (D3D11Renderer*)
                il.Emit(OpCodes.Conv_I);
                il.Emit(OpCodes.Ldc_I4, 0x28);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldind_I4); // load D3D11Renderer::swapchainDatas : D3D11SwapChainData**
                il.Emit(OpCodes.Conv_I);
                il.Emit(OpCodes.Ldind_I4);
                il.Emit(OpCodes.Conv_I);
                il.Emit(OpCodes.Ldind_I4); // load D3D11SwapChainData::swapchain : IDXGISwapChain*
                il.Emit(OpCodes.Conv_I);
                il.Emit(OpCodes.Ret);
                GetPtrFromObjDel GetSwapChainPtr = (GetPtrFromObjDel)dyn.CreateDelegate(typeof(GetPtrFromObjDel));

                GraphicsDevice o = CEngine.CEngine.Instance.GraphicsDevice;
                uint SwapChainPtr = GetSwapChainPtr(o);

                InitializeImGui_d3d11((IntPtr)SwapChainPtr);
            }
            else if (renderer == ERenderer.OPEN_GL)
            {
                InitializeImGui_opengl();
            }
            else if (renderer == ERenderer.VULKAN)
            {
                // unsupported!!
                return false;
            }

            return true;
        }

        public void SdlPoll(ref SDL.SDL_Event sdl_event)
        {
            if (!initialized || !Enabled.Value.Enabled)
                return;

            if (sdl_event.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
            {
                MouseState mouse = Mouse.GetState();
                sdl_event.motion.x = mouse.X;
                sdl_event.motion.y = mouse.Y;
            }
            unsafe
            {
                ProcessEvent((IntPtr)Unsafe.AsPointer(ref sdl_event));
            }
        }

        private void DrawWarning()
        {
            if (
                DisableWarning.Value ||
                new TimeSpan(Velo.Time.Ticks) - unsupportedWarningTime > TimeSpan.FromSeconds(10))
            {
                FontCache.Release(ref unsupportedWarningFont);
                return;
            }
            FontCache.Get(ref unsupportedWarningFont, "UI\\Font\\ariblk.ttf:24");

            CTextDrawComponent warning = new CTextDrawComponent("", unsupportedWarningFont.Font, Vector2.Zero)
            {
                StringText =
                "WARNING: Cannot open UI, unsupported driver \"" + selectedDriver + "\"!\n" +
                "This hotkey will now serve as a way to reload settings from file.",
                IsVisible = true,
                color = Color.Red,
                DropShadowColor = Color.Black,
                HasDropShadow = true,
                DropShadowOffset = Vector2.One
            };
            warning.UpdateBounds();

            float screenWidth = Velo.GraphicsDevice.Viewport.Width;
            float screenHeight = Velo.GraphicsDevice.Viewport.Height;

            float width = warning.Bounds.Width;
            float height = warning.Bounds.Height;

            warning.Position = new Vector2(
                (screenWidth - width) / 2, (screenHeight - height) / 2);

            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            warning.Draw(null);
            Velo.SpriteBatch.End();
        }

        [DllImport("Velo_UI.dll", EntryPoint = "InitializeImGui_d3d11")]
        private static extern void InitializeImGui_d3d11(IntPtr swapChain);
        [DllImport("Velo_UI.dll", EntryPoint = "InitializeImGui_opengl")]
        private static extern void InitializeImGui_opengl();
        [DllImport("Velo_UI.dll", EntryPoint = "RenderImGui")]
        private static extern void RenderImGui(float mouseX, float mouseY, float frameW, float frameH);

        [DllImport("Velo_UI.dll", EntryPoint = "ShutdownImGui")]
        private static extern void ShutdownImGui();

        [DllImport("Velo_UI.dll", EntryPoint = "unfocusAll")]
        private static extern void unfocusAll();

        [DllImport("Velo_UI.dll", EntryPoint = "setUiHotkey")]
        private static extern void setUiHotkey(int keyCode);

        [DllImport("Velo_UI.dll", EntryPoint = "LoadProgram")]
        private static extern void LoadProgram(IntPtr str, int strSize);

        [DllImport("Velo_UI.dll", EntryPoint = "UpdateProgram")]
        private static extern void UpdateProgram(IntPtr str, int strSize);

        [DllImport("Velo_UI.dll", EntryPoint = "ProcessEvent")]
        private static extern void ProcessEvent(IntPtr eventPtr);
        [DllImport("Velo_UI.dll", EntryPoint = "GamePadButtons")]
        private static extern void GamePadButtons(bool[] buttons);

        [DllImport("Velo_UI.dll", EntryPoint = "GetChangeSize")]
        private static extern int GetChangeSize();

        [DllImport("Velo_UI.dll", EntryPoint = "GetJsonUpdates")]
        private static extern void GetJsonUpdates(IntPtr str);
    }
}