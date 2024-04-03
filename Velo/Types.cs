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
        public static int ToInt(this JsonElement elem)
        {
            if (!(elem is JsonDecimal))
                return default;
            int.TryParse(((JsonDecimal)elem).value, out int value);
            return value;
        }

        public static float ToFloat(this JsonElement elem)
        {
            if (!(elem is JsonDecimal))
                return default;
            float.TryParse(((JsonDecimal)elem).value, out float value);
            return value;
        }

        public static bool ToBool(this JsonElement elem)
        {
            if (!(elem is JsonBoolean))
                return default;
            return ((JsonBoolean)elem).value;
        }

        public static string ToString(this JsonElement elem)
        {
            if (!(elem is JsonString))
                return default;
            return ((JsonString)elem).value;
        }

        public static Vector2 ToVector2(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default;
            JsonObject jsonObject = (JsonObject)elem;
            return new Vector2(
                jsonObject.Get("X").ToFloat(), 
                jsonObject.Get("Y").ToFloat()
                );
        }

        public static Color ToColor(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default;
            JsonObject jsonObject = (JsonObject)elem;
            return new Color(
                jsonObject.Get("R").ToInt(), 
                jsonObject.Get("G").ToInt(),
                jsonObject.Get("B").ToInt(),
                jsonObject.Get("A").ToInt()
                );
        }

        public static bool[] ToBoolArr(this JsonElement elem)
        {
            if (!(elem is JsonArray))
                return default;
            return ((JsonArray)elem).value.Select(jsonBool => jsonBool.ToBool()).ToArray();
        }

        public static string[] ToStringArr(this JsonElement elem)
        {
            if (!(elem is JsonArray))
                return default;
            return ((JsonArray)elem).value.Select(jsonString => ToString(jsonString)).ToArray();
        }

        public static Color[] ToColorArr(this JsonElement elem)
        {
            if (!(elem is JsonArray))
                return default;
            return ((JsonArray)elem).value.Select(jsonColor => jsonColor.ToColor()).ToArray();
        }

        public static Toggle ToToggle(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default;
            return new Toggle(
                ((JsonObject)elem).Get("State").ToBool(),
                (ushort)((JsonObject)elem).Get("Hotkey").ToInt()
                );
        }

        public static RoundingMultiplier ToRoundingMultiplier(this JsonElement elem)
        {
            if (!(elem is JsonString))
                return default;
            return new RoundingMultiplier(ToString(elem));
        }

        public static ColorTransition ToColorTransition(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default;
            return new ColorTransition(
                ((JsonObject)elem).Get("Period").ToInt(),
                ((JsonObject)elem).Get("Offset").ToInt(),
                ((JsonObject)elem).Get("Discrete").ToBool(),
                ((JsonObject)elem).Get("Colors").ToColorArr()
                );
        }

        public static InputBox ToInputBox(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default;
            return new InputBox(
                ToString(((JsonObject)elem).Get("Label")),
                new Vector2(
                    ((JsonObject)elem).Get("X").ToInt(),
                    ((JsonObject)elem).Get("Y").ToInt()
                ),
                new Vector2(
                    ((JsonObject)elem).Get("W").ToInt(),
                    ((JsonObject)elem).Get("H").ToInt()
                ));
        }
    }
}
