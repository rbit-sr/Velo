using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using CEngine.Graphics.Camera;
using CEngine.Graphics.Component;
using CEngine.Graphics.Library;
using CEngine.World.Collision.Shape;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;

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

        public static string FormatLongTime(long seconds)
        {
            string text = "";
            bool hasUnit = false;
            int count = 0;
            if (seconds >= 60 * 60 * 24)
            {
                long days = seconds / (60 * 60 * 24);
                text = days.ToString() + "d ";
                seconds -= days * 60 * 60 * 24;
                hasUnit = true;
                count++;
            }
            if (seconds >= 60 * 60 || hasUnit)
            {
                long hours = seconds / (60 * 60);
                if (hasUnit)
                    text += hours.ToString("00");
                else
                    text += hours.ToString();
                text += "h ";
                seconds -= hours * 60 * 60;
                hasUnit = true;
                count++;
            }
            if ((seconds >= 60 || hasUnit) && count < 2)
            {
                long minutes = seconds / 60;
                if (hasUnit)
                    text += minutes.ToString("00");
                else
                    text += minutes.ToString();
                text += "m ";
                seconds -= minutes * 60;
                hasUnit = true;
                count++;
            }
            if (count < 2)
            {
                if (hasUnit)
                    text += seconds.ToString("00");
                else
                    text += seconds.ToString();
                text += "s";
            }
            if (text.EndsWith(" "))
                text = text.Substring(0, text.Length - 1);
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
                    throw new EndOfStreamException();
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

        public static void ToBytes<T>(this T value, byte[] buffer) where T : struct
        {
            unsafe
            {
                int size = Marshal.SizeOf<T>();
                void* valuePtr = Unsafe.AsPointer(ref value);
                fixed (byte* bytePtr = buffer)
                {
                    Buffer.MemoryCopy(valuePtr, bytePtr, size, size);
                }
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

        public static T FromBytes<T>(this byte[] bytes, int size) where T : struct
        {
            unsafe
            {
                T value = new T();
                void* valuePtr = Unsafe.AsPointer(ref value);
                fixed (byte* bytePtr = bytes)
                {
                    Buffer.MemoryCopy(bytePtr, valuePtr, size, size);
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

        [DllImport("Velo_UI.dll", EntryPoint = "IsSrFocused")]
        private static extern int IsSrFocused();

        public static bool IsFocused()
        {
            return IsSrFocused() == 1;
        }

        private readonly static bool filterKeysEnabled = new Func<bool>(() =>
        {
            object flags = Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Accessibility\\Keyboard Response", "Flags", "0");
            if (flags is string flagsStr)
            {
                if (int.TryParse(flagsStr, out int resultStr))
                    return (resultStr | 1) == 1;
            }
            return false;
        })();
        public readonly static TimeSpan RepeatDelay = new Func<TimeSpan>(() =>
        {
            if (!filterKeysEnabled)
            {
                object delay = Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Keyboard", "KeyboardDelay", "1");
                if (delay is string delayStr)
                {
                    if (int.TryParse(delayStr, out int result))
                        return TimeSpan.FromSeconds(0.25d * (result + 1));
                }
                return TimeSpan.FromSeconds(0.5d);
            }
            else
            {
                object delay = Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Accessibility\\Keyboard Response", "AutoRepeatDelay", "1000");
                if (delay is string delayStr)
                {
                    if (int.TryParse(delayStr, out int result))
                        return TimeSpan.FromMilliseconds(result);
                }
                return TimeSpan.FromSeconds(1d);
            }
        })();
        public readonly static TimeSpan RepeatRate = new Func<TimeSpan>(() =>
        {
            if (!filterKeysEnabled)
            {
                object delay = Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Keyboard", "KeyboardSpeed", "31");
                if (delay is string delayStr)
                {
                    if (int.TryParse(delayStr, out int result))
                        return TimeSpan.FromSeconds(1d / (result + 2));
                }
                return TimeSpan.FromSeconds(0.03d);
            }
            else
            {
                object delay = Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Keyboard", "AutoRepeatRate", "500");
                if (delay is string delayStr)
                {
                    if (int.TryParse(delayStr, out int result))
                        return TimeSpan.FromMilliseconds(result);
                }
                return TimeSpan.FromSeconds(0.5d);
            }
        })();

        private static readonly List<Func<bool>> enableCursorOn = new List<Func<bool>>();

        public static void EnableCursorOn(Func<bool> enableOn)
        {
            enableCursorOn.Add(enableOn);
        }

        public static bool CursorEnabled()
        {
            int count = enableCursorOn.Count;
            for (int i = 0; i < count; i++)
            {
                if (enableCursorOn[i]())
                    return true;
            }
            return false;
        }

        private static readonly List<Func<bool>> disableMouseInputsOn = new List<Func<bool>>();

        public static void DisableMouseInputsOn(Func<bool> disableOn)
        {
            disableMouseInputsOn.Add(disableOn);
        }

        public static bool MouseInputsDisabled()
        {
            int count = disableMouseInputsOn.Count;
            for (int i = 0; i < count; i++)
            {
                if (disableMouseInputsOn[i]())
                    return true;
            }
            return false;
        }

        private static readonly List<Func<bool>> disableKeyInputsOn = new List<Func<bool>>();

        public static void DisableKeyInputsOn(Func<bool> disableOn)
        {
            disableKeyInputsOn.Add(disableOn);
        }

        public static bool KeyInputsDisabled()
        {
            int count = disableKeyInputsOn.Count;
            for (int i = 0; i < count; i++)
            {
                if (disableKeyInputsOn[i]())
                    return true;
            }
            return false;
        }
    }

    public static class StreamUtil
    {
        [DllImport("Velo_UI.dll", EntryPoint = "Memcpy")]
        private static unsafe extern void Memcpy(void* dst, void* src, uint size);

        [ThreadStatic]
        public static byte[] buffer;

        public static unsafe void Write(this Stream stream, object obj, int off, int size)
        {
            if (buffer == null || buffer.Length < size)
                buffer = new byte[size * 2];
            void* objPtr = (byte*)MemUtil.GetPtr(obj) + off;
            fixed (byte* bufferPtr = buffer)
            {
                Memcpy(bufferPtr, objPtr, (uint)size);
                //Buffer.MemoryCopy(objPtr, bufferPtr, size, size);
            }
            stream.Write(buffer, 0, size);
        }

        public static unsafe void Read(this Stream stream, object obj, int off, int size)
        {
            if (buffer == null || buffer.Length < size)
                buffer = new byte[size * 2];
            stream.ReadExactly(buffer, 0, size);
            void* objPtr = (byte*)MemUtil.GetPtr(obj) + off;
            fixed (byte* bufferPtr = buffer)
            {
                Memcpy(objPtr, bufferPtr, (uint)size);
                //Buffer.MemoryCopy(bufferPtr, objPtr, size, size);
            }
        }

        public static unsafe void Write<T>(this Stream stream, T value) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            if (buffer == null || buffer.Length < size)
                buffer = new byte[size * 2];
            value.ToBytes(buffer);
            stream.Write(buffer, 0, size);
        }

        public static unsafe T Read<T>(this Stream stream) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            if (buffer == null || buffer.Length < size)
                buffer = new byte[size * 2];
            stream.ReadExactly(buffer, 0, size);
            return buffer.FromBytes<T>(size);
        }

        public static unsafe void WriteArr<T>(this Stream stream, T[] value) where T : struct
        {
            if (value == null)
            {
                Write(stream, -1);
                return;
            }
            Write(stream, value.Length);
            WriteArrFixed(stream, value, value.Length);
        }

        public static unsafe T[] ReadArr<T>(this Stream stream) where T : struct
        {
            int length = Read<int>(stream);
            if (length == -1)
                return null;
            return ReadArrFixed<T>(stream, length);
        }

        public static unsafe void WriteArrFixed<T>(this Stream stream, T[] value, int length) where T : struct
        {
            if (value is byte[] valueBytes)
            {
                stream.Write(valueBytes, 0, length);
            }
            else
            {
                for (int i = 0; i < length; i++)
                    Write(stream, value[i]);
            }
        }

        public static unsafe T[] ReadArrFixed<T>(this Stream stream, int length) where T : struct
        {
            T[] value = new T[length];
            if (value is byte[] valueBytes)
            {
                stream.Read(valueBytes, 0, length);
            }
            else
            {
                for (int i = 0; i < length; i++)
                    value[i] = Read<T>(stream);
            }
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
            byte nextB = 0;
            int i = 0;
            for (; i < value.Length; i++)
            {
                nextB |= (byte)((value[i] ? 1 : 0) << (i & 0b111));
                if ((i & 0b111) == 0b111)
                {
                    stream.WriteByte(nextB);
                    nextB = 0;
                }
            }
            i--;
            if ((i & 0b111) != 0b111)
                stream.WriteByte(nextB);
        }

        public static unsafe bool[] ReadBoolArr(this Stream stream)
        {
            int length = Read<int>(stream);
            if (length == -1)
                return null;
            bool[] value = new bool[length];
            byte nextB = 0;
            for (int i = 0; i < length; i++)
            {
                if ((i & 0b111) == 0)
                    nextB = (byte)stream.ReadByte();
                value[i] = (nextB & 1) == 1;
                nextB >>= 1;
            }
            return value;
        }

        public static unsafe void WriteStr(this Stream stream, string str, int minLength = 0)
        {
            byte[] bytes = str != null ? Encoding.ASCII.GetBytes(str) : null;
            WriteArr(stream, bytes);
            int length = bytes != null ? bytes.Length : 0;
            if (length < minLength)
            {
                byte[] dummy = new byte[minLength - length];
                WriteArrFixed(stream, dummy, dummy.Length);
            }
        }

        public static unsafe string ReadStr(this Stream stream, int minLength = 0)
        {
            byte[] bytes = ReadArr<byte>(stream);
            if (bytes == null)
            {
                stream.Position += minLength;
                return null;
            }
            if (bytes.Length < minLength)
            {
                stream.Position += minLength - bytes.Length;
            }
            return Encoding.ASCII.GetString(bytes);
        }
    }

    public class CircArray<T>
    {
        private T[] arr;
        private int begin;
        private int end;

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

    public class TextDraw : ICDrawComponent
    {
        private string text;
        private Vector2 stringSize;

        private string layerId;
        private ICParentDrawComponent parent;

        private string fontName;
        private CachedFont font;
        private bool update;

        public TextDraw()
        {
            text = "";
            IsVisible = true;
            Opacity = 1f;
            Scale = Vector2.One;
        }

        public string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value;
                    update = true;
                }
            }
        }

        public void SetFont(string newFontName)
        {
            if (font == null || newFontName != fontName)
            {
                FontCache.Get(ref font, newFontName);
                fontName = newFontName;
                update = true;
            }
        }

        public CFont Font => font.Font;

        public void SetFont(CachedFont font)
        {
            this.font = font;
            fontName = "";
        }

        public Color Color { get; set; }
        public float Opacity { get; set; }
        public bool IsVisible { get; set; }
        public bool HasDropShadow { get; set; }
        public Vector2 DropShadowOffset { get; set; }
        public Color DropShadowColor { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Offset { get; set; }
        public Vector2 Scale { get; set; }
        public Vector2 Align { get; set; }
        public float Rotation { get; set; }
        public bool Flipped { get; set; }
        public CAABB Bounds
        {
            get
            {
                if (update)
                {
                    stringSize = font.Font.MeasureString(text);
                    update = false;
                }
                return CAABB.CreateFromPositionSize(Position + Offset - stringSize * Scale * Align, stringSize * Scale);
            }
        }
            
        public string LayerId
        {
            get
            {
                if (layerId == null)
				{
                    return parent.LayerId;
                }
                return layerId;
            }
            set
            {
                layerId = value;
            }
        }

        public ICParentDrawComponent Parent
        {
            set
            {
                if (value != null)
                {
                    parent = value;
                }
                else
                {
                    parent = CRootDrawComponent.Instance;
                }
            }
        }

        public bool Cull => true;

        public ICGraphicsLibraryItem Asset => null;

        public void Draw()
        {
            if (update)
            {
                stringSize = font.Font.MeasureString(text);
                update = false;
            }

            if (IsVisible)
            {
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);

                if (HasDropShadow)
                    font.Font.DrawString(Velo.SpriteBatch, text, Position + Offset * Scale + DropShadowOffset, DropShadowColor * Opacity, Rotation, stringSize * Align, Scale, Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

                font.Font.DrawString(Velo.SpriteBatch, text, Position + Offset * Scale, Color * Opacity, Rotation, stringSize * Align, Scale, Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

                Velo.SpriteBatch.End();
            }
        }

        public void Init()
        {

        }

        public void Destroy()
        {

        }

        public void UpdateBounds()
        {

        }

        public void Draw(CCamera camera)
        {
            if (update)
            {
                stringSize = font.Font.MeasureString(text);
                update = false;
            }

            if (IsVisible)
            {
                if (HasDropShadow)
                    font.Font.DrawString(Velo.SpriteBatch, text, Position + Offset * Scale + DropShadowOffset, DropShadowColor * Opacity, Rotation, stringSize * Align, Scale, Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

                font.Font.DrawString(Velo.SpriteBatch, text, Position + Offset * Scale, Color * Opacity, Rotation, stringSize * Align, Scale, Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            }
        }
    }

    enum AutorepeatEvent
    {
        NONE, PRESS, REPEAT
    }

    class AutorepeatContext
    {
        public bool AutoRepeat;

        private TimeSpan pressTime = TimeSpan.Zero;
        private TimeSpan lastRepeat = TimeSpan.Zero;
        private bool downPrev = false;

        public AutorepeatContext(bool autoRepeat) 
        {
            AutoRepeat = autoRepeat;
        }

        public AutorepeatEvent PressEvent(bool down)
        {
            if (down && !downPrev)
            {
                if (AutoRepeat)
                {
                    pressTime = Velo.RealTime;
                    lastRepeat = TimeSpan.Zero;
                }
                downPrev = true;
                return AutorepeatEvent.PRESS;
            }
            downPrev = down;

            if (!AutoRepeat || !down)
                return AutorepeatEvent.NONE;

            TimeSpan now = Velo.RealTime;

            if (now - pressTime >= Util.RepeatDelay && now - lastRepeat >= Util.RepeatRate)
            {
                lastRepeat = now;
                return AutorepeatEvent.REPEAT;
            }

            return AutorepeatEvent.NONE;
        }
    }
}
