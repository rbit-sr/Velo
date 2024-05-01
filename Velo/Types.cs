using Microsoft.Xna.Framework;
using System.Linq;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections;

namespace Velo
{
    public class Toggle
    {
        private readonly bool enabled = false;
        public bool Enabled { get { return enabled; } }
        public ushort Hotkey = 0x97;

        public Toggle(ushort hotkey = 0x97)
        {
            Hotkey = hotkey;
        }

        public Toggle(bool enabled, ushort hotkey = 0x97)
        {
            this.enabled = enabled;
            Hotkey = hotkey;
        }

        public JsonElement ToJson()
        {
            return new JsonObject(2).
                AddBoolean("State", Enabled).
                AddDecimal("Hotkey", Hotkey);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Toggle))
                return false;
            Toggle toggle = obj as Toggle;
            return enabled == toggle.enabled &&
                   Enabled == toggle.Enabled &&
                   Hotkey == toggle.Hotkey;
        }

        public override int GetHashCode()
        {
            int hashCode = 689577727;
            hashCode = hashCode * -1521134295 + enabled.GetHashCode();
            hashCode = hashCode * -1521134295 + Enabled.GetHashCode();
            hashCode = hashCode * -1521134295 + Hotkey.GetHashCode();
            return hashCode;
        }
    }

    public class RoundingMultiplier
    {
        public string ValueStr;
        public float Value;
        public int Precision;

        public RoundingMultiplier(string String)
        {
            ValueStr = String;
            float.TryParse(String, out Value);
            if (!String.Contains('.'))
                Precision = 0;
            else
                Precision = String.Length - String.IndexOf('.') - 1;
        }

        public string ToStringRounded(float value)
        {
            if (Value == 0)
                return value + "";

            float s = value >= 0f ? 1f : -1f;

            value = Math.Abs(value);

            int c = (int)(value / Value + 0.5f);

            value = Value * c;
            value *= s;

            return value.ToString("F" + Precision);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RoundingMultiplier))
                return false;
            RoundingMultiplier roundingMultiplier = obj as RoundingMultiplier;
            return ValueStr == roundingMultiplier.ValueStr;
        }

        public override int GetHashCode()
        {
            return 135179013 + EqualityComparer<string>.Default.GetHashCode(ValueStr);
        }

        public JsonElement ToJson()
        {
            return new JsonString(ValueStr);
        }
    }

    public class ColorTransition
    {
        private readonly int period = 500;
        private readonly int offset = 0;
        private readonly bool discrete = false;
        private readonly Color[] colors;

        public ColorTransition() { }

        public ColorTransition(Color def)
        {
            colors = new Color[1] { def };
        }

        public ColorTransition(int period, int offset, bool discrete, Color[] colors)
        {
            this.period = period;
            this.offset = offset;
            this.discrete = discrete;
            this.colors = colors;
        }

        public Color Get()
        {
            if (colors.Length == 0)
                return Color.White;

            if (colors.Length == 1)
                return Util.ApplyAlpha(colors[0]);

            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            return Get(milliseconds, true);
        }

        public Color Get(long x, bool repeat)
        {
            if (colors.Length == 0)
                return Color.White;

            if (colors.Length == 1)
                return Util.ApplyAlpha(colors[0]);

            if (period == 0)
                return colors[0];

            double index = repeat ?
                ((x + offset) % period) / (double)period * colors.Length :
                Math.Min(Math.Max(Math.Abs(x + offset), 0), period) / (double)period * (colors.Length - 1);

            int index1 = (int)index;
            int index2 = index1 + 1;
            
            if (index2 >= colors.Length)
            {
                if (repeat)
                    index2 = 0;
                else
                    index2 = colors.Length - 1;
            }

            if (!discrete)
                return Util.ApplyAlpha(Color.Lerp(colors[index1], colors[index2], (float)(index - index1)));
            else
                return Util.ApplyAlpha(colors[index1]);
        }

        public Color Get(double x, bool repeat)
        {
            if (colors.Length == 0)
                return Color.White;

            if (colors.Length == 1)
                return Util.ApplyAlpha(colors[0]);

            if (period == 0)
                return colors[0];

            double index = repeat ?
                ((x + offset) % period) / period * colors.Length :
                Math.Min(Math.Max(Math.Abs(x + offset), 0), period) / period * (colors.Length - 1);

            int index1 = (int)index;
            int index2 = index1 + 1;

            if (index2 >= colors.Length)
            {
                if (repeat)
                    index2 = 0;
                else
                    index2 = colors.Length - 1;
            }

            if (!discrete)
                return Util.ApplyAlpha(Color.Lerp(colors[index1], colors[index2], (float)(index - index1)));
            else
                return Util.ApplyAlpha(colors[index1]);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ColorTransition))
                return false;
            ColorTransition colorTransition = obj as ColorTransition;
            return period == colorTransition.period &&
                   offset == colorTransition.offset &&
                   discrete == colorTransition.discrete &&
                   colors.SequenceEqual(colorTransition.colors);
        }

        public override int GetHashCode()
        {
            int hashCode = -1419435415;
            hashCode = hashCode * -1521134295 + period.GetHashCode();
            hashCode = hashCode * -1521134295 + offset.GetHashCode();
            hashCode = hashCode * -1521134295 + discrete.GetHashCode();
            hashCode = hashCode * -1521134295 + ((IStructuralEquatable)colors).GetHashCode(EqualityComparer<Color>.Default);
            return hashCode;
        }

        public JsonElement ToJson()
        {
            return new JsonObject(4).
                AddDecimal("Period", period).
                AddDecimal("Offset", offset).
                AddBoolean("Discrete", discrete).
                AddElement("Colors", colors.ToJson());
        }
    }

    public class InputBox
    {
        public string text;
        public Vector2 position;
        public Vector2 size;

        public InputBox()
        {

        }

        public InputBox(string text, Vector2 position, Vector2 size)
        {
            this.text = text;
            this.position = position;
            this.size = size;
        }

        public JsonElement ToJson()
        {
            return new JsonObject(5).
                AddString("Label", text).
                AddDecimal("X", position.X).
                AddDecimal("Y", position.Y).
                AddDecimal("W", size.X).
                AddDecimal("H", size.Y);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InputBox))
                return false;
            InputBox inputBox = obj as InputBox;
            return text == inputBox.text &&
                   position.Equals(inputBox.position) &&
                   size.Equals(inputBox.size);
        }

        public override int GetHashCode()
        {
            int hashCode = -1359643628;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(text);
            hashCode = hashCode * -1521134295 + position.GetHashCode();
            hashCode = hashCode * -1521134295 + size.GetHashCode();
            return hashCode;
        }
    }

    public static class ToJsonExt
    {
        public static JsonElement ToJson(this int value)
        {
            return new JsonDecimal(value);
        }

        public static JsonElement ToJson(this float value)
        {
            return new JsonDecimal(value);
        }

        public static JsonElement ToJson(this bool value)
        {
            return new JsonBoolean(value);
        }

        public static JsonElement ToJson(this string value)
        {
            return new JsonString(value);
        }

        public static JsonElement ToJson(this Vector2 vector)
        {
            return new JsonObject(2).
                AddDecimal("X", vector.X).
                AddDecimal("Y", vector.Y);
        }

        public static JsonElement ToJson(this Color color)
        {
            return new JsonObject(4).
                AddDecimal("R", color.R).
                AddDecimal("G", color.G).
                AddDecimal("B", color.B).
                AddDecimal("A", color.A);
        }

        public static JsonElement ToJson(this bool[] bools)
        {
            return new JsonArray(bools.Select(elem => elem.ToJson()).ToList());
        }

        public static JsonElement ToJson(this string[] strings)
        {
            return new JsonArray(strings.Select(elem => elem.ToJson()).ToList());
        }

        public static JsonElement ToJson(this Color[] colors)
        {
            return new JsonArray(colors.Select(elem => elem.ToJson()).ToList());
        }
    }

    public static class FromJsonExt
    {
        public static int AsInt(this JsonElement elem)
        {
            if (!(elem is JsonDecimal))
                return default(int);
            int value;
            int.TryParse((elem as JsonDecimal).value, out value);
            return value;
        }

        public static float AsFloat(this JsonElement elem)
        {
            if (!(elem is JsonDecimal))
                return default(float);
            float value;
            float.TryParse((elem as JsonDecimal).value, out value);
            return value;
        }

        public static bool AsBool(this JsonElement elem)
        {
            if (!(elem is JsonBoolean))
                return default(bool);
            return (elem as JsonBoolean).value;
        }

        public static string AsString(this JsonElement elem)
        {
            if (!(elem is JsonString))
                return "";
            return (elem as JsonString).value;
        }

        public static Vector2 AsVector2(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default(Vector2);
            JsonObject jsonObject = (JsonObject)elem;
            return new Vector2(
                jsonObject.DoWithValueRet("X", AsFloat),
                jsonObject.DoWithValueRet("Y", AsFloat)
                );
        }

        public static Color AsColor(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default(Color);
            JsonObject jsonObject = (JsonObject)elem;
            return new Color(
                jsonObject.DoWithValueRet("R", AsInt),
                jsonObject.DoWithValueRet("G", AsInt),
                jsonObject.DoWithValueRet("B", AsInt),
                jsonObject.DoWithValueRet("A", AsInt)
                );
        }

        public static bool[] AsBoolArr(this JsonElement elem)
        {
            if (!(elem is JsonArray))
                return new bool[0];
            return ((JsonArray)elem).value.Select(AsBool).ToArray();
        }

        public static string[] AsStringArr(this JsonElement elem)
        {
            if (!(elem is JsonArray))
                return new string[0];
            return ((JsonArray)elem).value.Select(AsString).ToArray();
        }

        public static Color[] AsColorArr(this JsonElement elem)
        {
            if (!(elem is JsonArray))
                return new Color[0];
            return ((JsonArray)elem).value.Select(AsColor).ToArray();
        }

        public static Toggle AsToggle(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return new Toggle();
            JsonObject jsonObject = (JsonObject)elem;
            return new Toggle(
                jsonObject.DoWithValueRet("State", AsBool),
                jsonObject.DoWithValueRet("Hotkey", value => (ushort)value.AsInt())
                );
        }

        public static RoundingMultiplier AsRoundingMultiplier(this JsonElement elem)
        {
            if (!(elem is JsonString))
                return new RoundingMultiplier("1");
            return new RoundingMultiplier(AsString(elem));
        }

        public static ColorTransition AsColorTransition(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return new ColorTransition(Color.White);
            JsonObject jsonObject = (JsonObject)elem;
            return new ColorTransition(
                jsonObject.DoWithValueRet("Period", AsInt),
                jsonObject.DoWithValueRet("Offset", AsInt),
                jsonObject.DoWithValueRet("Discrete", AsBool),
                jsonObject.DoWithValueRet("Colors", AsColorArr)
                );
        }

        public static InputBox AsInputBox(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return new InputBox();
            JsonObject jsonObject = (JsonObject)elem;
            return new InputBox(
                jsonObject.DoWithValueRet("Label", AsString),
                new Vector2(
                    jsonObject.DoWithValueRet("X", AsInt),
                    jsonObject.DoWithValueRet("Y", AsInt)
                ),
                new Vector2(
                    jsonObject.DoWithValueRet("W", AsInt),
                    jsonObject.DoWithValueRet("H", AsInt)
                ));
        }
    }
}
