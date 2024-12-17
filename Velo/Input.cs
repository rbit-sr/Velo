using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Velo
{
    public static class Input
    {
        private struct KeyboardOemState
        {
            public bool Oem1;
            public bool Oem2;
            public bool Oem3;
            public bool Oem4;
            public bool Oem5;
            public bool Oem6;
            public bool Oem7;
            public bool Oem8;
            public bool Oem102;
        }

        private static KeyboardState keysPrev;
        private static KeyboardState keysCurr;
        private static KeyboardOemState keysOemPrev;
        private static KeyboardOemState keysOemCurr;
        private static MouseState mousePrev;
        private static MouseState mouseCurr;
        private static GamePadState gamePadPrev;
        private static GamePadState gamePadCurr;

        public static bool[] GamePadButtonsDown = new bool[17];

        public enum EGamePadButton
        {
            A, B, X, Y, LEFT_SHOULDER, RIGHT_SHOULDER, LEFT_STICK, RIGHT_STICK, START, BACK, BIG_BUTTON,
            TRIGGER_LEFT, TRIGGER_RIGHT,
            DPAD_LEFT, DPAD_RIGHT, DPAD_UP, DPAD_DOWN,
            COUNT
        }

        private static bool IsDown(KeyboardState keys, KeyboardOemState keysOem, MouseState mouse, GamePadState gamePad, ushort key)
        {
            if ((key & 0x400) != 0)
            {
                switch ((EGamePadButton)(key & 0xff))
                {
                    case EGamePadButton.A:
                        return gamePad.Buttons.A == ButtonState.Pressed;
                    case EGamePadButton.B:
                        return gamePad.Buttons.B == ButtonState.Pressed;
                    case EGamePadButton.X:
                        return gamePad.Buttons.X == ButtonState.Pressed;
                    case EGamePadButton.Y:
                        return gamePad.Buttons.Y == ButtonState.Pressed;
                    case EGamePadButton.LEFT_SHOULDER:
                        return gamePad.Buttons.LeftShoulder == ButtonState.Pressed;
                    case EGamePadButton.RIGHT_SHOULDER:
                        return gamePad.Buttons.RightShoulder == ButtonState.Pressed;
                    case EGamePadButton.LEFT_STICK:
                        return gamePad.Buttons.LeftStick == ButtonState.Pressed;
                    case EGamePadButton.RIGHT_STICK:
                        return gamePad.Buttons.RightStick == ButtonState.Pressed;
                    case EGamePadButton.START:
                        return gamePad.Buttons.Start == ButtonState.Pressed;
                    case EGamePadButton.BACK:
                        return gamePad.Buttons.Back == ButtonState.Pressed;
                    case EGamePadButton.BIG_BUTTON:
                        return gamePad.Buttons.BigButton == ButtonState.Pressed;
                    case EGamePadButton.TRIGGER_LEFT:
                        return gamePad.Triggers.Left > 0.5f;
                    case EGamePadButton.TRIGGER_RIGHT:
                        return gamePad.Triggers.Right > 0.5f;
                    case EGamePadButton.DPAD_LEFT:
                        return gamePad.DPad.Left == ButtonState.Pressed;
                    case EGamePadButton.DPAD_RIGHT:
                        return gamePad.DPad.Right == ButtonState.Pressed;
                    case EGamePadButton.DPAD_UP:
                        return gamePad.DPad.Up == ButtonState.Pressed;
                    case EGamePadButton.DPAD_DOWN:
                        return gamePad.DPad.Down == ButtonState.Pressed;
                    default:
                        return false;
                }
            }
            else
            {
                key &= 0x3ff;
                bool isShift = (key & 0x100) != 0;
                bool isCtrl = (key & 0x200) != 0;
                bool shiftDown = keys[Keys.LeftShift] == KeyState.Down || keys[Keys.RightShift] == KeyState.Down;
                bool ctrlDown = keys[Keys.LeftControl] == KeyState.Down || keys[Keys.RightControl] == KeyState.Down;

                if (isShift != shiftDown || isCtrl != ctrlDown)
                    return false;

                switch ((System.Windows.Forms.Keys)key)
                {
                    case System.Windows.Forms.Keys.LButton:
                        return mouse.LeftButton == ButtonState.Pressed;
                    case System.Windows.Forms.Keys.RButton:
                        return mouse.RightButton == ButtonState.Pressed;
                    case System.Windows.Forms.Keys.MButton:
                        return mouse.MiddleButton == ButtonState.Pressed;
                    case System.Windows.Forms.Keys.XButton1:
                        return mouse.XButton1 == ButtonState.Pressed;
                    case System.Windows.Forms.Keys.XButton2:
                        return mouse.XButton2 == ButtonState.Pressed;
                    case System.Windows.Forms.Keys.Oem1:
                        return keysOem.Oem1;
                    case System.Windows.Forms.Keys.Oem2:
                        return keysOem.Oem2;
                    case System.Windows.Forms.Keys.Oem3:
                        return keysOem.Oem3;
                    case System.Windows.Forms.Keys.Oem4:
                        return keysOem.Oem4;
                    case System.Windows.Forms.Keys.Oem5:
                        return keysOem.Oem5;
                    case System.Windows.Forms.Keys.Oem6:
                        return keysOem.Oem6;
                    case System.Windows.Forms.Keys.Oem7:
                        return keysOem.Oem7;
                    case System.Windows.Forms.Keys.Oem8:
                        return keysOem.Oem8;
                    case System.Windows.Forms.Keys.Oem102:
                        return keysOem.Oem102;
                }
               
                return keys[(Keys)(key & 0xff)] == KeyState.Down;
            }
        }

        public static bool IsPressed(ushort key)
        {
            if (key == 0x97)
                return false;
            return IsDown(keysCurr, keysOemCurr, mouseCurr, gamePadCurr, key) && !IsDown(keysPrev, keysOemPrev, mousePrev, gamePadPrev, key);
        }

        public static bool IsDown(ushort key)
        {
            if (key == 0x97)
                return false;
            return IsDown(keysCurr, keysOemCurr, mouseCurr, gamePadCurr, key);
        }

        [DllImport("Velo_UI.dll", EntryPoint = "InitLLKeyboardHook")]
        public static extern void InitLLKeyboardHook();
        [DllImport("Velo_UI.dll", EntryPoint = "InitLLMouseHook")]
        public static extern void InitLLMouseHook();
        [DllImport("Velo_UI.dll", EntryPoint = "InitWndProcHook")]
        public static extern void InitWndProcHook();
        [DllImport("Velo_UI.dll", EntryPoint = "RemoveHooks")]
        public static extern void RemoveHooks();
        [DllImport("Velo_UI.dll", EntryPoint = "PollLLHooks")]
        public static extern void PollLLHooks();

        [DllImport("Velo_UI.dll", EntryPoint = "IsDown")]
        public static extern bool IsKeyDown(byte key);
        [DllImport("Velo_UI.dll", EntryPoint = "ClearKeys")]
        public static extern void ClearKeys();

        [DllImport("Velo_UI.dll", EntryPoint = "WindowDragging")]
        public static extern int WindowDragging();

        public static void Init()
        {
            InitLLKeyboardHook();
            InitWndProcHook();
            AppDomain.CurrentDomain.ProcessExit += (_, __) => RemoveHooks();
            AppDomain.CurrentDomain.DomainUnload += (_, __) => RemoveHooks();
            System.Windows.Forms.Application.ApplicationExit += (_, __) => RemoveHooks();
            Console.CancelKeyPress += (_, __) => RemoveHooks();
        }

        private static bool first = true;
        private static bool focusedPrev = false;
        private static bool initializedLLMouseHook = false;

        public static bool Focused = false;

        public static void Update()
        {
            Focused = Util.IsFocused();
            if (first)
                focusedPrev = Focused;

            keysPrev = keysCurr;
            keysCurr = Keyboard.GetState();

            keysOemPrev = keysOemCurr;
            keysOemCurr = new KeyboardOemState();

            if (Focused)
            {
                PollLLHooks(); 
                keysOemCurr.Oem1 = IsKeyDown((byte)System.Windows.Forms.Keys.Oem1);
                keysOemCurr.Oem2 = IsKeyDown((byte)System.Windows.Forms.Keys.Oem2);
                keysOemCurr.Oem3 = IsKeyDown((byte)System.Windows.Forms.Keys.Oem3);
                keysOemCurr.Oem4 = IsKeyDown((byte)System.Windows.Forms.Keys.Oem4);
                keysOemCurr.Oem5 = IsKeyDown((byte)System.Windows.Forms.Keys.Oem5);
                keysOemCurr.Oem6 = IsKeyDown((byte)System.Windows.Forms.Keys.Oem6);
                keysOemCurr.Oem7 = IsKeyDown((byte)System.Windows.Forms.Keys.Oem7);
                keysOemCurr.Oem8 = IsKeyDown((byte)System.Windows.Forms.Keys.Oem8);
                keysOemCurr.Oem102 = IsKeyDown((byte)System.Windows.Forms.Keys.Oem102);
            }

            mousePrev = mouseCurr;
            mouseCurr = Mouse.GetState();

            gamePadPrev = gamePadCurr;
            gamePadCurr = GamePad.GetState((PlayerIndex)SettingsUI.Instance.ControllerIndex.Value);

            GamePadButtonsDown[(int)EGamePadButton.A] = gamePadCurr.Buttons.A == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.B] = gamePadCurr.Buttons.B == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.X] = gamePadCurr.Buttons.X == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.Y] = gamePadCurr.Buttons.Y == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.LEFT_SHOULDER] = gamePadCurr.Buttons.LeftShoulder == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.RIGHT_SHOULDER] = gamePadCurr.Buttons.RightShoulder == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.LEFT_STICK] = gamePadCurr.Buttons.LeftStick == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.RIGHT_STICK] = gamePadCurr.Buttons.RightStick == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.START] = gamePadCurr.Buttons.Start == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.BACK] = gamePadCurr.Buttons.Back == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.BIG_BUTTON] = gamePadCurr.Buttons.BigButton == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.TRIGGER_LEFT] = gamePadCurr.Triggers.Left > 0.5f;
            GamePadButtonsDown[(int)EGamePadButton.TRIGGER_RIGHT] = gamePadCurr.Triggers.Right > 0.5f;
            GamePadButtonsDown[(int)EGamePadButton.DPAD_LEFT] = gamePadCurr.DPad.Left == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.DPAD_RIGHT] = gamePadCurr.DPad.Right == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.DPAD_UP] = gamePadCurr.DPad.Up == ButtonState.Pressed;
            GamePadButtonsDown[(int)EGamePadButton.DPAD_DOWN] = gamePadCurr.DPad.Down == ButtonState.Pressed;

            if (!Focused)
            {
                RemoveHooks();
                ClearKeys();
            }

            if (IsPressed((ushort)Keys.Escape) || Velo.Ingame != Velo.IngamePrev || (Focused && !focusedPrev))
            {
                InitLLKeyboardHook();
                InitWndProcHook();
                if (initializedLLMouseHook || IsPressed((ushort)Keys.Escape) || Velo.Ingame != Velo.IngamePrev)
                {
                    InitLLMouseHook();
                    initializedLLMouseHook = true;
                }
            }

            focusedPrev = Focused;

            first = false;
        }
    }
}
