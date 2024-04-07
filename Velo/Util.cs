﻿using System;
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

        public static string ToStringRounded(float value, RoundingMultiplier roundingMultiplier)
        {
            if (roundingMultiplier.Value == 0)
                return value + "";

            float s = value >= 0.0f ? 1.0f : -1.0f;

            value = Math.Abs(value);

            int c = (int)(value / roundingMultiplier.Value + 0.5f);

            value = roundingMultiplier.Value * c;
            value *= s;

            return value.ToString("F" + roundingMultiplier.Precision);
        }

        public static Color ApplyAlpha(Color color)
        {
            return new Color(color.R, color.G, color.B) * (color.A / 255.0f);
        }

        public static Color FullAlpha(Color color)
        {
            return new Color(color.R, color.G, color.B, 255);
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
