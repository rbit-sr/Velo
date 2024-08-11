﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;

namespace Velo
{
    public enum EItem : byte
    {
        NONE,
        GOLDEN_HOOK,
        BOX,
        DRILL,
        ROCKET,
        BOMB,
        TRIGGER,
        TRIPLE_JUMP,
        TWO_BOXES,
        THREE_BOXES,
        SUNGLASSES,
        ONE_ROCKET,
        TWO_ROCKETS,
        THREE_ROCKETS,
        SHOCKWAVE,
        FIREBALL,
        FREEZE,
        SMILEY
    }

    public enum EGameOptions : int
    {
        LETHAL_SPIKES,
        SPEEDHOOKERS,
        SRENNUR_DEEPS,
        ROCKETRUNNERS,
        SUPER_SPEED_RUNNERS,
        SPEED_RAPTURE,
        NO_ITEMS,
        ROULETTE_WHEEL,
        SUDDEN_DEATH,
        DESTRUCTIBLE_ENVIRONMENT
    }

    public enum ETimeFormat
    {
        UNITS, COLON, DOT_COLON, COLON_MIN_OPT, DOT_COLON_MIN_OPT
    }

    public static class Util
    {
        public static T[] Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
            return array;
        }

        public static void ForEach<T>(this IEnumerable<T> elems, Action<T> action)
        {
            foreach (var elem in elems)
                action(elem);
        }

        public static string LineBreaks(string s, int maxCharPerLine)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            while (true)
            {
                if (index + maxCharPerLine >= s.Length)
                {
                    sb.Append(s.Substring(index));
                    return sb.ToString();
                }

                int nextBreak = s.IndexOf('\n', index);
                if (nextBreak != -1 && nextBreak <= index + maxCharPerLine)
                {
                    sb.Append(s.Substring(index, nextBreak - index));
                    sb.Append('\n');
                    index = nextBreak + 1;
                    continue;
                }

                int nextIndex = s.LastIndexOf(' ', index + maxCharPerLine);
                sb.Append(s.Substring(index, nextIndex - index));
                sb.Append('\n');
                index = nextIndex + 1;
            }
        }

        public static string ApproxTime(long seconds)
        {
            string unit;
            long value;

            if (seconds < 60)
            {
                value = seconds;
                unit = "second";
            }
            else if (seconds < 60 * 60)
            {
                value =  seconds / 60;
                unit = "minute";
            }
            else if (seconds < 60 * 60 * 24)
            {
                value = seconds / (60 * 60);
                unit = "hour";
            }
            else if (seconds < 60 * 60 * 24 * 30)
            {
                value = seconds / (60 * 60 * 24);
                unit = "day";
            }
            else if (seconds < 60 * 60 * 24 * 365)
            {
                value = seconds / (60 * 60 * 24 * 30);
                unit = "month";
            }
            else
            {
                value = seconds / (60 * 60 * 24 * 365);
                unit = "year";
            }
            return value + " " + unit + (value == 1 ? "" : "s");
        }

        public static string FormatTime(long millis, ETimeFormat style)
        {
            string text = "";
            bool hasUnit = false;
            switch (style)
            {
                case ETimeFormat.UNITS:
                    if (millis >= 1000 * 60)
                    {
                        long minutes = millis / (1000 * 60);
                        text = minutes.ToString() + "m ";
                        millis -= minutes * 1000 * 60;
                        hasUnit = true;
                    }
                    if (millis >= 1000 || hasUnit)
                    {
                        long seconds = millis / 1000;
                        if (hasUnit)
                            text += seconds.ToString("00");
                        else
                            text += seconds.ToString();
                        text += "s ";
                        millis -= seconds * 1000;
                        hasUnit = true;
                    }
                    if (hasUnit)
                        text += millis.ToString("000");
                    else
                        text += millis.ToString();
                    text += "ms";
                    break;
                case ETimeFormat.COLON:
                    {
                        long minutes = millis / (1000 * 60);
                        text = minutes.ToString("00") + ":";
                        millis -= minutes * 1000 * 60;

                        long seconds = millis / 1000;
                        text += seconds.ToString("00");
                        text += ":";
                        millis -= seconds * 1000;

                        text += millis.ToString("000");
                    }
                    break;
                case ETimeFormat.DOT_COLON:
                    {
                        long minutes = millis / (1000 * 60);
                        text = minutes.ToString("00") + ".";
                        millis -= minutes * 1000 * 60;

                        long seconds = millis / 1000;
                        text += seconds.ToString("00");
                        text += ":";
                        millis -= seconds * 1000;

                        text += millis.ToString("000");
                    }
                    break;
                case ETimeFormat.COLON_MIN_OPT:
                    {
                        long minutes = millis / (1000 * 60);
                        if (minutes != 0)
                        {
                            text = minutes.ToString("00") + ":";
                            millis -= minutes * 1000 * 60;
                        }

                        long seconds = millis / 1000;
                        text += seconds.ToString("00");
                        text += ":";
                        millis -= seconds * 1000;

                        text += millis.ToString("000");
                    }
                    break;
                case ETimeFormat.DOT_COLON_MIN_OPT:
                    {
                        long minutes = millis / (1000 * 60);
                        if (minutes != 0)
                        {
                            text = minutes.ToString("00") + ".";
                            millis -= minutes * 1000 * 60;
                        }

                        long seconds = millis / 1000;
                        text += seconds.ToString("00");
                        text += ":";
                        millis -= seconds * 1000;

                        text += millis.ToString("000");
                    }
                    break;
            }
            return text;
        }

        public static Color ApplyAlpha(Color color)
        {
            return new Color(color.R, color.G, color.B) * (color.A / 255f);
        }

        public static Color FullAlpha(Color color)
        {
            return new Color(color.R, color.G, color.B, 255);
        }

        public static void ReadExactly(this System.IO.Stream stream, byte[] buffer, int offset, int count)
        {
            int remaining = count;
            while (remaining > 0)
            {
                int read = stream.Read(buffer, offset, remaining);
                if (read == 0)
                    throw new System.IO.EndOfStreamException();
                offset += read;
                remaining -= read;
            }
        }

        public static byte[] ToBytes<T>(this T value) where T : struct
        {
            unsafe
            {
                byte[] bytes = new byte[Marshal.SizeOf<T>()];
                void* valuePtr = Unsafe.AsPointer(ref value);
                fixed (byte* bytePtr = bytes)
                {
                    Buffer.MemoryCopy(valuePtr, bytePtr, bytes.Length, bytes.Length);
                }
                return bytes;
            }
        }

        public static T FromBytes<T>(this byte[] bytes) where T : struct
        {
            unsafe
            {
                T value = new T();
                void* valuePtr = Unsafe.AsPointer(ref value);
                fixed (byte* bytePtr = bytes)
                {
                    Buffer.MemoryCopy(bytePtr, valuePtr, bytes.Length, bytes.Length);
                }
                return value;
            }
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern uint RtlGetVersion(out OsVersionInfo versionInformation);

        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct OsVersionInfo
        {
            private readonly uint OsVersionInfoSize;

            internal readonly uint MajorVersion;
            internal readonly uint MinorVersion;

            private readonly uint BuildNumber;

            private readonly uint PlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            private readonly string CSDVersion;

            public static readonly OsVersionInfo Value = new Func<OsVersionInfo>(() =>
            {
                RtlGetVersion(out OsVersionInfo value);
                return value;
            })();
        }

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimeAsFileTime(out long filetime);

        public static long UtcNow
        {
            get
            {
                long filetime;
                if (
                    (OsVersionInfo.Value.MajorVersion == 6 && OsVersionInfo.Value.MinorVersion >= 2) ||
                    OsVersionInfo.Value.MajorVersion > 6
                    )
                    GetSystemTimePreciseAsFileTime(out filetime);
                else
                    GetSystemTimeAsFileTime(out filetime);

                return DateTime.FromFileTimeUtc(filetime).Ticks;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static bool focused = false;
        public static TimeSpan lastFocusedUpdate = TimeSpan.Zero;

        public static bool IsFocused()
        {
            if (Velo.Time.Ticks >= lastFocusedUpdate.Ticks + 100 * TimeSpan.TicksPerMillisecond)
            {
                focused = Process.GetCurrentProcess().MainWindowHandle == GetForegroundWindow();
                lastFocusedUpdate = new TimeSpan(Velo.Time.Ticks);
            }

            return focused;
        }

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        public static bool minimized = false;
        public static TimeSpan lastMinimizedUpdate;

        public static bool IsMinimized()
        {
            if (Velo.Time.Ticks >= lastMinimizedUpdate.Ticks + 100 * TimeSpan.TicksPerMillisecond)
            {
                minimized = IsIconic(Process.GetCurrentProcess().MainWindowHandle);
                lastMinimizedUpdate = Velo.Time;
            }

            return minimized;
        }

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(Keys key);
    }

    public static class StreamUtil
    {
        public static unsafe void Write(this Stream stream, object obj, int off, int size)
        {
            byte[] buffer = new byte[size];
            void* objPtr = (byte*)MemUtil.GetPtr(obj) + off;
            fixed (byte* bufferPtr = buffer)
            {
                Buffer.MemoryCopy(objPtr, bufferPtr, size, size);
            }
            stream.Write(buffer, 0, size);
        }

        public static unsafe void Read(this Stream stream, object obj, int off, int size)
        {
            byte[] buffer = new byte[size];
            stream.ReadExactly(buffer, 0, size);
            void* objPtr = (byte*)MemUtil.GetPtr(obj) + off;
            fixed (byte* bufferPtr = buffer)
            {
                Buffer.MemoryCopy(bufferPtr, objPtr, size, size);
            }
        }

        public static unsafe void Write<T>(this Stream stream, T value) where T : struct
        {
            byte[] buffer = value.ToBytes();
            stream.Write(buffer, 0, buffer.Length);
        }

        public static unsafe T Read<T>(this Stream stream) where T : struct
        {
            byte[] buffer = new byte[Marshal.SizeOf<T>()];
            stream.ReadExactly(buffer, 0, buffer.Length);
            return buffer.FromBytes<T>();
        }

        public static unsafe void WriteArr<T>(this Stream stream, T[] value) where T : struct
        {
            if (value == null)
            {
                Write(stream, -1);
                return;
            }
            Write(stream, value.Length);
            foreach (T elem in value)
                Write(stream, elem);
        }

        public static unsafe T[] ReadArr<T>(this Stream stream) where T : struct
        {
            int length = Read<int>(stream);
            if (length == -1)
                return null;
            T[] value = new T[length];
            for (int i = 0; i < length; i++)
                value[i] = Read<T>(stream);
            return value;
        }

        public static unsafe void WriteArr<T>(this Stream stream, T[] value, int length) where T : struct
        {
            for (int i = 0; i < length; i++)
                Write(stream, value[i]);
        }

        public static unsafe T[] ReadArr<T>(this Stream stream, int length) where T : struct
        {
            T[] value = new T[length];
            for (int i = 0; i < length; i++)
                value[i] = Read<T>(stream);
            return value;
        }

        public static unsafe void WriteBoolArr(this Stream stream, bool[] value)
        {
            if (value == null)
            {
                Write(stream, -1);
                return;
            }
            Write(stream, value.Length);
            byte[] packed = new byte[(value.Length + 7) >> 3];
            for (int i = 0; i < value.Length; i++)
                packed[i >> 3] |= (byte)((value[i] ? 1 : 0) << (i & 7));
            foreach (byte b in packed)
                Write(stream, b);
        }

        public static unsafe bool[] ReadBoolArr(this Stream stream)
        {
            int length = Read<int>(stream);
            if (length == -1)
                return null;
            bool[] value = new bool[length];
            byte[] packed = new byte[(length + 7) >> 3];
            for (int i = 0; i < packed.Length; i++)
                packed[i] = Read<byte>(stream);
            for (int i = 0; i < value.Length; i++)
                value[i] = ((packed[i >> 3] >> (i & 7)) & 1) == 1;
            return value;
        }

        public static unsafe void WriteStr(this Stream stream, string str)
        {
            byte[] bytes = str != null ? Encoding.ASCII.GetBytes(str) : null;
            WriteArr(stream, bytes);
        }

        public static unsafe string ReadStr(this Stream stream)
        {
            byte[] bytes = ReadArr<byte>(stream);
            if (bytes == null)
                return null;
            return Encoding.ASCII.GetString(bytes);
        }
    }


    public class CircArray<T>
    {
        T[] arr;
        int begin;
        int end;

        public CircArray()
        {
            arr = new T[1];
            begin = 0;
            end = 0;
        }

        public void PushBack(T elem)
        {
            arr[end] = elem;
            end++;
            if (end == arr.Length)
                end = 0;

            if (begin == end)
            {
                T[] newArr = new T[arr.Length * 2];
                Array.Copy(arr, begin, newArr, 0, arr.Length - begin);
                Array.Copy(arr, 0, newArr, arr.Length - begin, end);
                begin = 0;
                end = arr.Length;
                arr = newArr;
            }
        }

        public void PopFront()
        { 
            if (typeof(T).IsClass)
                arr[begin] = default;
            begin++;
            if (begin == arr.Length)
                begin = 0;
        }

        public T this[int i]
        {
            get 
            {
                i += begin;
                if (i >= arr.Length)
                    i -= arr.Length;
                return arr[i]; 
            }
            set 
            {
                i += begin;
                if (i >= arr.Length)
                    i -= arr.Length;
                arr[i] = value; 
            }
        }

        public int Count
        {
            get
            {
                return
                    end >= begin ?
                    end - begin : arr.Length + end - begin;
            }
        }

        public void Clear()
        {
            if (typeof(T).IsClass)
            {
                for (int i = begin; i != end; )
                {
                    arr[i] = default;
                    i++;
                    if (i == arr.Length)
                        i = 0;
                }
            }
            begin = 0;
            end = 0;
        }

        public CircArray<T> Clone()
        {
            CircArray<T> clone = new CircArray<T>
            {
                begin = begin,
                end = end,
                arr = new T[arr.Length]
            };
            Array.Copy(arr, clone.arr, arr.Length);
            return clone;
        }
    }
}
