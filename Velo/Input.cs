using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Velo
{
    public static class Input
    {
        private static KeyboardState keysPrev;
        private static KeyboardState keysCurr;
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

        private static bool IsDown(KeyboardState keys, GamePadState gamePad, ushort key)
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

                return
                    keys[(Keys)(key & 0xff)] == KeyState.Down &&
                    isShift == shiftDown && isCtrl == ctrlDown;
            }
        }

        public static bool Pressed(ushort key)
        {
            return IsDown(keysCurr, gamePadCurr, key) && !IsDown(keysPrev, gamePadPrev, key);
        }

        public static bool Held(ushort key)
        {
            return IsDown(keysCurr, gamePadCurr, key);
        }

        public static void Update()
        {
            keysPrev = keysCurr;
            keysCurr = Keyboard.GetState();

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
        }
    }
}
