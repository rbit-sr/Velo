using Microsoft.Xna.Framework;
using System.Linq;
using System;

namespace Velo
{
    public class Toggle
    {
        public bool Enabled = false;
        public ushort Hotkey = 0x97;

        public Toggle(ushort hotkey = 0x97)
        {
            Hotkey = hotkey;
        }

        public Toggle(bool enabled, ushort hotkey = 0x97)
        {
            Enabled = enabled;
            Hotkey = hotkey;
        }

        public void ToggleEnabled()
        {
            Enabled = !Enabled;
        }

        public JsonElement ToJson()
        {
            return new JsonObject(2).
                AddBoolean("State", Enabled).
                AddDecimal("Hotkey", Hotkey);
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

        public JsonElement ToJson()
        {
            return new JsonString(ValueStr);
        }
    }

    public class ColorTransition
    {
        public int period = 500;
        public int offset = 0;
        public bool discrete = false;
        public Color[] colors;

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
            if (colors.Length == 1)
                return colors[0];

            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            milliseconds = (milliseconds + offset) % period;

            float index = milliseconds / (float)period * colors.Length;

            int index1 = (int)index;
            int index2 = index1 + 1 < colors.Length ? index1 + 1 : 0;

            if (!discrete)
                return Color.Lerp(colors[index1], colors[index2], index - index1);
            else
                return colors[index1];
        }

        public JsonElement ToJson()
        {
            return new JsonObject(4).
                AddDecimal("Period", period).
                AddDecimal("Offset", offset).
                AddBoolean("Discrete", discrete).
                AddElement("Colors", ToJsonExt.ToJson(colors));
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
    }

    public enum EOrientation
    {
        PLAYER, TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT, TOP, BOTTOM, LEFT, RIGHT, CENTER
    }

    public enum ELineStyle
    {
        SOLID, DASHED, DOTTED
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
            return new JsonArray(bools.Select((elem) => elem.ToJson()).ToList());
        }

        public static JsonElement ToJson(this string[] strings)
        {
            return new JsonArray(strings.Select((elem) => elem.ToJson()).ToList());
        }

        public static JsonElement ToJson(this Color[] colors)
        {
            return new JsonArray(colors.Select((elem) => elem.ToJson()).ToList());
        }
    }
}
