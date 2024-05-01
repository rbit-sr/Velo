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
#if !VELO_OLD
using Microsoft.Xna.Framework.Input;
#endif
#if VELO_OLD
using CEngine.Util.Input.SDLInput;
#endif

namespace Velo
{
    public class SettingsUI : ToggleModule
    {
#if !VELO_OLD
        public BoolSetting DisableWarning;
#endif
        public BoolSetting DisableKeyInput;

        private bool initialized = false;

#if !VELO_OLD
        private string selectedDriver = "";
        private bool unsupportedWarned = false;
        private TimeSpan unsupportedWarningTime = TimeSpan.Zero;
        private CFont unsupportedWarningFont = null;
#endif

        private bool sendUpdates = false;

        private SettingsUI() : base("UI")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)System.Windows.Forms.Keys.F1));

#if !VELO_OLD
            DisableWarning = AddBool("disable warning", false);
            DisableWarning.Hidden = true;
#endif
            DisableKeyInput = AddBool("disable key input", false);

            DisableKeyInput.Tooltip =
                "Disables any key inputs while the settings menu is open.";
        }

        public static SettingsUI Instance = new SettingsUI();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (!initialized)
                return;

            setUiHotkey((byte)Enabled.Value.Hotkey);

            if (!Enabled.Value.Enabled)
                return;

            ResetInputStates();
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (!initialized || !Enabled.Value.Enabled)
                return;

            ResetInputStates();
        }

        public override void PreRender()
        {
            base.PreRender();

            if (!initialized || !Enabled.Value.Enabled)
                return;

            ResetInputStates();
        }

        public override void PostRender()
        {
            base.PostRender();

#if !VELO_OLD
            if (unsupportedWarned)
            {
                if (Enabled.Modified())
                    Storage.Instance.Load();
                Enabled.Disable();
                DrawWarning();
                return;
            }
#endif

            if (Enabled.Modified())
            {
                CEngine.CEngine.Instance.Game.IsMouseVisible = Enabled.Value.Enabled;
                if (!Enabled.Value.Enabled)
                    unfocusAll();
            }

            if (!Enabled.Value.Enabled)
                return;

            if (!initialized)
            {
                if (!InitImGui())
                {
#if !VELO_OLD
                    unsupportedWarned = true;
                    unsupportedWarningTime = new TimeSpan(DateTime.Now.Ticks);
#endif
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
                sendUpdates = true;
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
                sendUpdates = false;
                ModuleManager.Instance.CommitChanges(JsonElement.FromString(json));
                sendUpdates = true;
            }

            MouseState mouse = Mouse.GetState();

            RenderImGui(
                    mouse.X,
                    mouse.Y,
                    CEngine.CEngine.Instance.GraphicsDevice.Viewport.Width,
                    CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height
                );

            ResetInputStates();
        }

        private void ResetInputStates()
        {
            CEngine.CEngine.Instance.input_manager.mouse_state1 = new MouseState(
                    CEngine.CEngine.Instance.input_manager.mouse_state1.X,
                    CEngine.CEngine.Instance.input_manager.mouse_state1.Y,
                    0, new ButtonState(), new ButtonState(), new ButtonState(), new ButtonState(), new ButtonState()
                );
            CEngine.CEngine.Instance.input_manager.mouse_state2 = new MouseState(
                    CEngine.CEngine.Instance.input_manager.mouse_state2.X,
                    CEngine.CEngine.Instance.input_manager.mouse_state2.Y,
                    0, new ButtonState(), new ButtonState(), new ButtonState(), new ButtonState(), new ButtonState()
                );
            if (DisableKeyInput.Value)
            {
                CEngine.CEngine.Instance.input_manager.keyboard_state1 = new KeyboardState();
                CEngine.CEngine.Instance.input_manager.keyboard_state2 = new KeyboardState();
            }
        }

        public void SettingUpdate(Setting setting)
        {
            if (!initialized || !sendUpdates || setting.Hidden)
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

#if !VELO_OLD
        enum ERenderer
        {
            D3D11, OPEN_GL, VULKAN
        }
#endif

        public bool InitImGui()
        {
#if !VELO_OLD
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
                il.Emit(OpCodes.Ldc_I4, 0x128);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldind_I4); // load FNA3D_Device::driverData : FNA3D_Renderer* (D3D11Renderer*)
                il.Emit(OpCodes.Conv_I);
                il.Emit(OpCodes.Ldc_I4, 0x24);
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
#else
            var dyn = new DynamicMethod("GetDevicePtr", typeof(uint), new[] { typeof(object) }, typeof(Velo).Module);
            var il = dyn.GetILGenerator();
            il.DeclareLocal(typeof(object), true);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0); // load GraphicsDevice
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Ldc_I4, 0xB8);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldind_I4); // load GraphicsDevice::pComPtr : IDirect3DDevice9*
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Ret);
            GetPtrFromObjDel GetDevicePtr = (GetPtrFromObjDel)dyn.CreateDelegate(typeof(GetPtrFromObjDel));

            GraphicsDevice o = CEngine.CEngine.Instance.GraphicsDevice;
            uint DevicePtr = GetDevicePtr(o);

            InitializeImGui_d3d9((IntPtr)DevicePtr);
#endif
            return true;
        }

#if !VELO_OLD
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

            if (
                (DisableKeyInput.Value &&
                (sdl_event.type == SDL.SDL_EventType.SDL_KEYUP ||
                sdl_event.type == SDL.SDL_EventType.SDL_KEYDOWN)) ||
                sdl_event.type == SDL.SDL_EventType.SDL_MOUSEWHEEL
                )
                sdl_event.type = 0;
        }
#endif

#if !VELO_OLD
        private void DrawWarning()
        {
            if (
                    DisableWarning.Value ||
                    new TimeSpan(DateTime.Now.Ticks) - unsupportedWarningTime > TimeSpan.FromSeconds(10)
                    )
            {
                if (unsupportedWarningFont != null)
                {
                    Velo.ContentManager.Release(unsupportedWarningFont);
                    unsupportedWarningFont = null;
                }
                return;
            }
            if (unsupportedWarningFont == null)
            {
                unsupportedWarningFont = FontCache.Get("UI\\Font\\ariblk.ttf", 24);
                Velo.ContentManager.Load(unsupportedWarningFont, false);
            }
            CTextDrawComponent warning = new CTextDrawComponent("", unsupportedWarningFont, Vector2.Zero)
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

            float screenWidth = Velo.SpriteBatch.GraphicsDevice.Viewport.Width;
            float screenHeight = Velo.SpriteBatch.GraphicsDevice.Viewport.Height;

            float width = warning.Bounds.Width;
            float height = warning.Bounds.Height;

            warning.Position = new Vector2(
                (screenWidth - width) / 2, (screenHeight - height) / 2);

            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            warning.Draw(null);
            Velo.SpriteBatch.End();
        }
#endif

#if !VELO_OLD
        [DllImport("Velo_UI.dll", EntryPoint = "InitializeImGui_d3d11")]
        private static extern void InitializeImGui_d3d11(IntPtr swapChain);
        [DllImport("Velo_UI.dll", EntryPoint = "InitializeImGui_opengl")]
        private static extern void InitializeImGui_opengl();
#else
        [DllImport("Velo_UI.dll", EntryPoint = "InitializeImGui_d3d9")]
        private static extern void InitializeImGui_d3d9(IntPtr device);
#endif
        [DllImport("Velo_UI.dll", EntryPoint = "RenderImGui")]
        private static extern void RenderImGui(float mouseX, float mouseY, float frameW, float frameH);

        [DllImport("Velo_UI.dll", EntryPoint = "ShutdownImGui")]
        private static extern void ShutdownImGui();

        [DllImport("Velo_UI.dll", EntryPoint = "unfocusAll")]
        private static extern void unfocusAll();

        [DllImport("Velo_UI.dll", EntryPoint = "setUiHotkey")]
        private static extern void setUiHotkey(byte keyCode);

        [DllImport("Velo_UI.dll", EntryPoint = "LoadProgram")]
        private static extern void LoadProgram(IntPtr str, int strSize);

        [DllImport("Velo_UI.dll", EntryPoint = "UpdateProgram")]
        private static extern void UpdateProgram(IntPtr str, int strSize);

        [DllImport("Velo_UI.dll", EntryPoint = "ProcessEvent")]
        private static extern void ProcessEvent(IntPtr eventPtr);

        [DllImport("Velo_UI.dll", EntryPoint = "GetChangeSize")]
        private static extern int GetChangeSize();

        [DllImport("Velo_UI.dll", EntryPoint = "GetJsonUpdates")]
        private static extern void GetJsonUpdates(IntPtr str);
    }
}