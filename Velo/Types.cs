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
            return obj is Toggle toggle &&
                   enabled == toggle.enabled &&
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

        public override bool Equals(object obj)
        {
            return obj is RoundingMultiplier multiplier &&
                   ValueStr == multiplier.ValueStr;
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
            if (colors.Length == 0)
                return Color.White;

            if (colors.Length == 1)
                return Util.ApplyAlpha(colors[0]);

            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            milliseconds = (milliseconds + offset) % period;

            float index = milliseconds / (float)period * colors.Length;

            int index1 = (int)index;
            int index2 = index1 + 1 < colors.Length ? index1 + 1 : 0;

            if (!discrete)
                return Util.ApplyAlpha(Color.Lerp(colors[index1], colors[index2], index - index1));
            else
                return Util.ApplyAlpha(colors[index1]);
        }

        public override bool Equals(object obj)
        {
            return obj is ColorTransition transition &&
                   period == transition.period &&
                   offset == transition.offset &&
                   discrete == transition.discrete &&
                   colors.SequenceEqual(transition.colors);
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
            return obj is InputBox box &&
                   text == box.text &&
                   position.Equals(box.position) &&
                   size.Equals(box.size);
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

    public enum EOrientation
    {
        PLAYER, TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT, TOP, BOTTOM, LEFT, RIGHT, CENTER
    }

    public enum EEvent
    {
        DEFAULT, NONE, SRENNUR_DEEPS, SCREAM_RUNNERS, WINTER
    }

    public enum ELineStyle
    {
        SOLID, DASHED, DOTTED
    }

    public static class EnumExt
    {
        public static string Label(this EOrientation orientation)
        {
            switch (orientation)
            {
                case EOrientation.PLAYER:
                    return "player";
                case EOrientation.TOP_LEFT:
                    return "top left";
                case EOrientation.TOP_RIGHT:
                    return "top right";
                case EOrientation.BOTTOM_LEFT:
                    return "bottom left";
                case EOrientation.BOTTOM_RIGHT:
                    return "bottom right";
                case EOrientation.TOP:
                    return "top";
                case EOrientation.BOTTOM:
                    return "bottom";
                case EOrientation.LEFT:
                    return "left";
                case EOrientation.RIGHT:
                    return "right";
                case EOrientation.CENTER:
                    return "center";
                default:
                    return "";
            }
        }

        public static Vector2 GetOrigin(this EOrientation orientation, float width, float height, float screenWidth, float screenHeight, Vector2 playerPos)
        {
            if (orientation == EOrientation.PLAYER)
                return playerPos;

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

        public static string Label(this EEvent _event)
        {
            switch (_event)
            {
                case EEvent.DEFAULT:
                    return "default";
                case EEvent.NONE:
                    return "none";
                case EEvent.SRENNUR_DEEPS:
                    return "srennuRdeepS";
                case EEvent.SCREAM_RUNNERS:
                    return "ScreamRunners";
                case EEvent.WINTER:
                    return "winter";
                default:
                    return "";
            }
        }

        public static int Id(this EEvent _event)
        {
            switch (_event)
            {
                case EEvent.DEFAULT:
                    return 255;
                case EEvent.NONE:
                    return 0;
                case EEvent.SRENNUR_DEEPS:
                    return 2;
                case EEvent.SCREAM_RUNNERS:
                    return 11;
                case EEvent.WINTER:
                    return 14;
                default:
                    return 255;
            }
        }

        public static string Label(this ELineStyle lineStyle)
        {
            switch (lineStyle)
            {
                case ELineStyle.SOLID:
                    return "solid";
                case ELineStyle.DASHED:
                    return "dashed";
                case ELineStyle.DOTTED:
                    return "dotted";
                default:
                    return "";
            }
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
                return "";
            return ((JsonString)elem).value;
        }

        public static Vector2 ToVector2(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default;
            JsonObject jsonObject = (JsonObject)elem;
            return new Vector2(
                jsonObject.DoWithValue("X", value => value.ToFloat()),
                jsonObject.DoWithValue("Y", value => value.ToFloat())
                );
        }

        public static Color ToColor(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default;
            JsonObject jsonObject = (JsonObject)elem;
            return new Color(
                jsonObject.DoWithValue("R", value => value.ToInt()),
                jsonObject.DoWithValue("G", value => value.ToInt()),
                jsonObject.DoWithValue("B", value => value.ToInt()),
                jsonObject.DoWithValue("A", value => value.ToInt())
                );
        }

        public static bool[] ToBoolArr(this JsonElement elem)
        {
            if (!(elem is JsonArray))
                return new bool[0];
            return ((JsonArray)elem).value.Select(jsonBool => jsonBool.ToBool()).ToArray();
        }

        public static string[] ToStringArr(this JsonElement elem)
        {
            if (!(elem is JsonArray))
                return new string[0];
            return ((JsonArray)elem).value.Select(jsonString => ToString(jsonString)).ToArray();
        }

        public static Color[] ToColorArr(this JsonElement elem)
        {
            if (!(elem is JsonArray))
                return new Color[0];
            return ((JsonArray)elem).value.Select(jsonColor => jsonColor.ToColor()).ToArray();
        }

        public static Toggle ToToggle(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return new Toggle();
            JsonObject jsonObject = (JsonObject)elem;
            return new Toggle(
                jsonObject.DoWithValue("State", value => value.ToBool()),
                jsonObject.DoWithValue("Hotkey", value => (ushort)value.ToInt())
                );
        }

        public static RoundingMultiplier ToRoundingMultiplier(this JsonElement elem)
        {
            if (!(elem is JsonString))
                return new RoundingMultiplier("1");
            return new RoundingMultiplier(ToString(elem));
        }

        public static ColorTransition ToColorTransition(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return new ColorTransition(Color.White);
            JsonObject jsonObject = (JsonObject)elem;
            return new ColorTransition(
                jsonObject.DoWithValue("Period", value => value.ToInt()),
                jsonObject.DoWithValue("Offset", value => value.ToInt()),
                jsonObject.DoWithValue("Discrete", value => value.ToBool()),
                jsonObject.DoWithValue("Colors", value => value.ToColorArr())
                );
        }

        public static InputBox ToInputBox(this JsonElement elem)
        {
            if (!(elem is JsonObject))
                return default;
            JsonObject jsonObject = (JsonObject)elem;
            return new InputBox(
                jsonObject.DoWithValue("Label", value => ToString(value)),
                new Vector2(
                    jsonObject.DoWithValue("X", value => value.ToInt()),
                    jsonObject.DoWithValue("Y", value => value.ToInt())
                ),
                new Vector2(
                    jsonObject.DoWithValue("W", value => value.ToInt()),
                    jsonObject.DoWithValue("H", value => value.ToInt())
                ));
        }
    }
}
