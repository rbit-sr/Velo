using System.Runtime.InteropServices;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection.Emit;
using System.Windows.Forms;
using SDL2;
using System.Runtime.CompilerServices;
using System.Text;

namespace Velo
{
    public class SettingsUI : ToggleModule
    {
        private bool initialized = false;
        private bool sendUpdates = false;

        private SettingsUI() : base("UI")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F1));
        }

        public static SettingsUI Instance = new SettingsUI();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (!initialized || !Enabled.Value.Enabled)
                return;

            CEngine.CEngine.Instance.input_manager.mouse_state1 = new Microsoft.Xna.Framework.Input.MouseState();
            CEngine.CEngine.Instance.input_manager.mouse_state2 = new Microsoft.Xna.Framework.Input.MouseState();
            CEngine.CEngine.Instance.input_manager.keyboard_state1 = new Microsoft.Xna.Framework.Input.KeyboardState();
            CEngine.CEngine.Instance.input_manager.keyboard_state2 = new Microsoft.Xna.Framework.Input.KeyboardState();
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (!initialized || !Enabled.Value.Enabled)
                return;

            CEngine.CEngine.Instance.input_manager.mouse_state1 = new Microsoft.Xna.Framework.Input.MouseState();
            CEngine.CEngine.Instance.input_manager.mouse_state2 = new Microsoft.Xna.Framework.Input.MouseState();
            CEngine.CEngine.Instance.input_manager.keyboard_state1 = new Microsoft.Xna.Framework.Input.KeyboardState();
            CEngine.CEngine.Instance.input_manager.keyboard_state2 = new Microsoft.Xna.Framework.Input.KeyboardState();
        }

        public override void PreRender()
        {
            base.PreRender();

            if (!initialized || !Enabled.Value.Enabled)
                return;

            CEngine.CEngine.Instance.input_manager.mouse_state1 = new Microsoft.Xna.Framework.Input.MouseState();
            CEngine.CEngine.Instance.input_manager.mouse_state2 = new Microsoft.Xna.Framework.Input.MouseState();
            CEngine.CEngine.Instance.input_manager.keyboard_state1 = new Microsoft.Xna.Framework.Input.KeyboardState();
            CEngine.CEngine.Instance.input_manager.keyboard_state2 = new Microsoft.Xna.Framework.Input.KeyboardState();
        }

        public override void PostRender()
        {
            base.PostRender();

            if (Enabled.Modified())
                CEngine.CEngine.Instance.Game.IsMouseVisible = Enabled.Value.Enabled;

            if (!Enabled.Value.Enabled)
                return;

            if (!initialized)
            {
                InitImGui();
                string json = ModuleManager.Instance.ToJson(false).ToString(false);
                unsafe
                {
                    fixed (byte* bytes = Encoding.ASCII.GetBytes(json))
                        LoadProgram((IntPtr)bytes, json.Length);
                }
                initialized = true;
                sendUpdates = true;
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
                Console.WriteLine(json);
                sendUpdates = false;
                ModuleManager.Instance.CommitChanges(JsonElement.FromString(json));
                sendUpdates = true;
            }

            RenderImGui();

            CEngine.CEngine.Instance.input_manager.mouse_state1 = new Microsoft.Xna.Framework.Input.MouseState();
            CEngine.CEngine.Instance.input_manager.mouse_state2 = new Microsoft.Xna.Framework.Input.MouseState();
            CEngine.CEngine.Instance.input_manager.keyboard_state1 = new Microsoft.Xna.Framework.Input.KeyboardState();
            CEngine.CEngine.Instance.input_manager.keyboard_state2 = new Microsoft.Xna.Framework.Input.KeyboardState();
        }

        public void SettingUpdate(Setting setting)
        {
            if (!initialized || !sendUpdates)
                return;
            JsonObject jsonObj = new JsonObject(2).
                AddElement("Changes", new JsonArray(1).
                    AddElement(setting.ToJson(true, true)));
            string json = jsonObj.ToString(false);
            unsafe
            {
                fixed (byte* bytes = Encoding.ASCII.GetBytes(json))
                    UpdateProgram((IntPtr)bytes, json.Length);
            }
        }

        private delegate uint GetPtrFromObjDel(object o);

        public void InitImGui()
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

            InitializeImGui((IntPtr)SwapChainPtr);
        }

        public void SdlPoll(ref SDL.SDL_Event sdl_event)
        {
            if (!initialized || !Enabled.Value.Enabled)
                return;
            unsafe
            {
                ProcessEvent((IntPtr)Unsafe.AsPointer(ref sdl_event));
                if (
                    sdl_event.type == SDL.SDL_EventType.SDL_KEYUP ||
                    sdl_event.type == SDL.SDL_EventType.SDL_KEYDOWN ||
                    sdl_event.type == SDL.SDL_EventType.SDL_MOUSEWHEEL
                    )
                    sdl_event.type = 0;
            }
        }

        [DllImport("DirectX11.dll", EntryPoint = "InitializeImGui")]
        private static extern void InitializeImGui(IntPtr swapChain);

        [DllImport("DirectX11.dll", EntryPoint = "RenderImGui")]
        private static extern void RenderImGui();

        [DllImport("DirectX11.dll", EntryPoint = "ShutdownImGui")]
        private static extern void ShutdownImGui();

        [DllImport("DirectX11.dll", EntryPoint = "LoadProgram")]
        private static extern void LoadProgram(IntPtr str, int strSize);

        [DllImport("DirectX11.dll", EntryPoint = "UpdateProgram")]
        private static extern void UpdateProgram(IntPtr str, int strSize);

        [DllImport("DirectX11.dll", EntryPoint = "ProcessEvent")]
        private static extern void ProcessEvent(IntPtr eventPtr);

        [DllImport("DirectX11.dll", EntryPoint = "GetChangeSize")]
        private static extern int GetChangeSize();

        [DllImport("DirectX11.dll", EntryPoint = "GetJsonUpdates")]
        private static extern void GetJsonUpdates(IntPtr str);
    }
}