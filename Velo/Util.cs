using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;

namespace Velo
{
    public static class Util
    {
        public static void Match<T>(this object o, Action<T> act)
        {
            if (o is T)
                act((T)o);
        }

        public static T Match<T>(this object o, T def)
        {
            if (o is T)
                return ((T)o);
            else
                return def;
        }

        public static void NullCond<T>(this T t, Action<T> act)
        {
            if (t != null)
                act(t);
        }

        public static T[] Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
            return array;
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
            string unit = "";
            long value = 0;

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

        public static string FormatTime(long millis)
        {
            byte[] text = new byte[12];
            text[11] = (byte)'s';
            text[10] = (byte)'m';
            text[9] = (byte)((byte)'0' + millis % 10);
            millis /= 10;
            text[8] = (byte)((byte)'0' + millis % 10);
            millis /= 10;
            text[7] = (byte)((byte)'0' + millis % 10);
            millis /= 10;
            text[6] = (byte)' ';
            text[5] = (byte)'s';
            text[4] = (byte)((byte)'0' + millis % 10);
            millis /= 10;
            text[3] = (byte)((byte)'0' + millis % 6);
            millis /= 6;
            text[2] = (byte)' ';
            text[1] = (byte)'m';
            text[0] = (byte)((byte)'0' + millis % 10);
            millis /= 10;
            return Encoding.ASCII.GetString(text);
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

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        public static long UtcNow
        {
            get
            {
                long filetime;
                GetSystemTimePreciseAsFileTime(out filetime);
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
                arr[begin] = default(T);
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
                    arr[i] = default(T);
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
            CircArray<T> clone = new CircArray<T>();
            clone.begin = begin;
            clone.end = end;
            clone.arr = new T[arr.Length];
            Array.Copy(arr, clone.arr, arr.Length);
            return clone;
        }
    }
}
