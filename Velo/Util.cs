using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Velo
{
    public static class Util
    {
        public static void Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        public static string ToStringRounded(float value, float roundingMultiplier, int prec)
        {
            float s = value >= 0.0f ? 1.0f : -1.0f;

            value = Math.Abs(value);

            int c = (int)(value / roundingMultiplier + 0.5f);

            value = roundingMultiplier * c;
            value *= s;

            return value.ToString("F" + prec);
        }

        public static Vector2 GetOrigin(EOrientation orientation, float width, float height, float screenWidth, float screenHeight)
        {
            Vector2 origin = Vector2.Zero;

            switch (orientation)
            {
                case EOrientation.TOP_LEFT:
                case EOrientation.LEFT:
                case EOrientation.BOTTOM_LEFT:
                    origin.X = 0.0f;
                    break;
                case EOrientation.TOP_RIGHT:
                case EOrientation.RIGHT:
                case EOrientation.BOTTOM_RIGHT:
                    origin.X = screenWidth - width;
                    break;
                case EOrientation.TOP:
                case EOrientation.CENTER:
                case EOrientation.BOTTOM:
                    origin.X = (screenWidth - width) / 2.0f;
                    break;
            }

            switch (orientation)
            {
                case EOrientation.TOP_LEFT:
                case EOrientation.TOP:
                case EOrientation.TOP_RIGHT:
                    origin.Y = 0.0f;
                    break;
                case EOrientation.BOTTOM_LEFT:
                case EOrientation.BOTTOM:
                case EOrientation.BOTTOM_RIGHT:
                    origin.Y = screenHeight - height;
                    break;
                case EOrientation.LEFT:
                case EOrientation.CENTER:
                case EOrientation.RIGHT:
                    origin.Y = (screenHeight - height) / 2.0f;
                    break;
            }

            return origin;
        }

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        public static long UtcNow
        {
            get
            {
                GetSystemTimePreciseAsFileTime(out long filetime);
                return DateTime.FromFileTimeUtc(filetime).Ticks;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static bool focused = false;
        public static TimeSpan lastFocusedUpdate = TimeSpan.Zero;

        public static bool IsFocused()
        {
            if (DateTime.Now.Ticks >= lastFocusedUpdate.Ticks + 100 * TimeSpan.TicksPerMillisecond)
            {
                focused = Process.GetCurrentProcess().MainWindowHandle == GetForegroundWindow();
                lastFocusedUpdate = new TimeSpan(DateTime.Now.Ticks);
            }

            return focused;
        }

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        public static bool minimized = false;
        public static TimeSpan lastMinimizedUpdate;

        public static bool IsMinimized()
        {
            if (DateTime.Now.Ticks >= lastMinimizedUpdate.Ticks + 100 * TimeSpan.TicksPerMillisecond)
            {
                minimized = IsIconic(Process.GetCurrentProcess().MainWindowHandle);
                lastMinimizedUpdate = new TimeSpan(DateTime.Now.Ticks);
            }

            return minimized;
        }
    }
}
