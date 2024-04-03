using System.Runtime.InteropServices;
using System;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection.Emit;
using System.Windows.Forms;
using System.Text;

namespace Velo
{
    public class SettingsUI : ToggleModule
    {
        private bool initialized = false;

        private SettingsUI() : base("UI")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F1));
        }

        public static SettingsUI Instance = new SettingsUI();

        public override void PostRender()
        {
            base.PostRender();

            if (!Enabled.Value.Enabled)
                return;

            if (!initialized)
            {
                InitImGui();
                string json = ModuleManager.Instance.ToJson(false).ToString(0);
                unsafe
                {
                    //fixed (byte* bytes = Encoding.ASCII.GetBytes(json))
                        //LoadProgram((IntPtr)bytes, json.Length);
                }
                initialized = true;
            }

            RenderImGui();
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

        [DllImport("DirectX11IMGUI.dll", EntryPoint = "InitializeImGui")]
        private static extern void InitializeImGui(IntPtr swapChain);

        [DllImport("DirectX11IMGUI.dll", EntryPoint = "RenderImGui")]
        private static extern void RenderImGui();

        [DllImport("DirectX11IMGUI.dll", EntryPoint = "ShutdownImGui")]
        private static extern void ShutdownImGui();

        [DllImport("DirectX11IMGUI.dll", EntryPoint = "LoadProgram")]
        private static extern void LoadProgram(IntPtr str, int strSize);
    }
}