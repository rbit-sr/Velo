using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Velo
{
    public abstract class Setting
    {
        public static Dictionary<int, Setting> IdToSetting = new Dictionary<int, Setting>();
        public static int NextId = 0;

        public string Name { get; }

        public bool modified;
        public int Id;

        public Setting(string name)
        {
            Name = name;
            modified = false;
            Id = NextId++;
            IdToSetting.Add(Id, this);
        }

        public bool Modified()
        {
            bool value = modified;
            modified = false;
            return value;
        }

        public abstract bool IsDefault();

        public virtual JsonElement ToJson(bool valueOnly = false)
        {
            return new JsonObject(8).
                AddString("Name", Name).
                AddDecimalIf("ID", Id, !valueOnly);
        }
    }

    public class Category : Setting
    {
        public List<Setting> Children;

        public Category(string name) :
            base(name)
        {
            Children = new List<Setting>();
        }

        public override bool IsDefault()
        {
            return true;
        }

        public override JsonElement ToJson(bool valueOnly = false)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddString("Name", Name).
                AddDecimalIf("Type", 14, !valueOnly).
                AddArray("Value", Children.Select((child) => child.ToJson(valueOnly)).ToList());
        }
    }

    public abstract class Setting<T> : Setting
    {
        private T value;

        public T Value
        {
            get { return value; }
            set { this.value = value; modified = true; }
        }
        public T DefaultValue { get; set; }

        public Setting(string name, T defaultValue) :
            base(name)
        {
            Value = defaultValue;
            DefaultValue = defaultValue;
        }

        public void SetValueAndDefault(T value)
        {
            Value = value;
            DefaultValue = value;
        }

        public override bool IsDefault()
        {
            return value.Equals(DefaultValue);
        }
    }

    public class IntSetting : Setting<int>
    {
        public int Min;
        public int Max;

        public IntSetting(string name, int defaultValue, int min, int max) :
            base(name, defaultValue) 
        {
            Min = min;
            Max = max;
        }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 0, !valueOnly).
                AddDecimalIf("Default", DefaultValue, !valueOnly).
                AddDecimalIf("Min", Min, !valueOnly).
                AddDecimalIf("Max", Max, !valueOnly).
                AddDecimal("Value", Value);
        }
    }

    public class FloatSetting : Setting<float>
    {
        public float Min;
        public float Max;

        public FloatSetting(string name, float defaultValue, float min, float max) :
            base(name, defaultValue)
        { 
            Min = min;
            Max = max;
        }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 1, !valueOnly).
                AddDecimalIf("Default", DefaultValue, !valueOnly).
                AddDecimalIf("Min", Min, !valueOnly).
                AddDecimalIf("Max", Max, !valueOnly).
                AddDecimal("Value", Value);
        }
    }

    public class BoolSetting : Setting<bool>
    {
        public BoolSetting(string name, bool defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 2, !valueOnly).
                AddBooleanIf("Default", DefaultValue, !valueOnly).
                AddBoolean("Value", Value);
        }
    }

    public class ToggleSetting : Setting<Toggle>
    {
        public ToggleSetting(string name, Toggle defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 3, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }
    }

    public class HotkeySetting : Setting<ushort>
    {
        public HotkeySetting(string name, ushort defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 4, !valueOnly).
                AddDecimalIf("Default", DefaultValue, !valueOnly).
                AddDecimal("Value", Value);
        }
    }

    public class VectorSetting : Setting<Vector2>
    {
        Vector2 Min;
        Vector2 Max;

        public VectorSetting(string name, Vector2 defaultValue, Vector2 min, Vector2 max) :
            base(name, defaultValue)
        { 
            Min = min;
            Max = max;
        }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 5, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElementIf("Min", Min.ToJson(), !valueOnly).
                AddElementIf("Max", Max.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }
    }

    public class StringSetting : Setting<string>
    {
        public StringSetting(string name, string defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 6, !valueOnly).
                AddStringIf("Default", DefaultValue, !valueOnly).
                AddString("Value", Value);
        }
    }

    public class RoundingMultiplierSetting : Setting<RoundingMultiplier>
    {
        public RoundingMultiplierSetting(string name, RoundingMultiplier defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 7, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }
    }

    public class HitboxListSetting : Setting<bool[]>
    {
        public HitboxListSetting(string name, bool[] defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 8, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddArrayIf("Identifiers", new List<JsonElement>
                    {
                        new JsonString("player"),
                        new JsonString("hook"),
                        new JsonString("fall tile"),
                        new JsonString("saw / spike"),
                        new JsonString("obstacle"),
                        new JsonString("boost section"),
                        new JsonString("super boost"),
                        new JsonString("trigger"),
                        new JsonString("item box"),
                        new JsonString("dropped obstacle"),
                        new JsonString("gate"),
                        new JsonString("rocket"),
                        new JsonString("bomb"),
                        new JsonString("straight rocket"),
                        new JsonString("fireball"),
                        new JsonString("boosta coke"),
                        new JsonString("bouncepad"),
                        new JsonString("freeze ray")
                    }, !valueOnly).
                AddElement("Value", Value.ToJson());
        }
    }

    public class StringListSetting : Setting<string[]>
    {
        public StringListSetting(string name, string[] defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 9, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }
    }

    public class OrientationSetting : Setting<EOrientation>
    {
        public OrientationSetting(string name, EOrientation defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 10, !valueOnly).
                AddDecimalIf("Default", (int)DefaultValue, !valueOnly).
                AddArrayIf("Identifiers", new List<JsonElement>
                    {
                        new JsonString("player"),
                        new JsonString("top left"),
                        new JsonString("top right"),
                        new JsonString("bottom left"),
                        new JsonString("bottom right"),
                        new JsonString("top"),
                        new JsonString("bottom"),
                        new JsonString("left"),
                        new JsonString("right"),
                        new JsonString("center")
                    }, !valueOnly).
                AddDecimal("Value", (int)Value);
        }
    }

    public class LineStyleSetting : Setting<ELineStyle>
    {
        public LineStyleSetting(string name, ELineStyle defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 10, !valueOnly).
                AddDecimalIf("Default", (int)DefaultValue, !valueOnly).
                AddArrayIf("Identifiers", new List<JsonElement>
                    {
                        new JsonString("solid"),
                        new JsonString("dashed"),
                        new JsonString("dotted")
                    }, !valueOnly).
                AddDecimal("Value", (int)Value);
        }
    }

    public class ColorSetting : Setting<Color>
    {
        public bool EnableAlpha;

        public ColorSetting(string name, Color defaultValue, bool enableAlpha = false) :
            base(name, defaultValue)
        {
            EnableAlpha = enableAlpha;
        }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 11, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddBooleanIf("EnableAlpha", EnableAlpha, !valueOnly).
                AddElement("Value", Value.ToJson());
        }
    }

    public class ColorTransitionSetting : Setting<ColorTransition>
    {
        public bool EnableAlpha;

        public ColorTransitionSetting(string name, ColorTransition defaultValue, bool enableAlpha = false) :
            base(name, defaultValue)
        {
            EnableAlpha = enableAlpha;
        }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 12, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddBooleanIf("EnableAlpha", EnableAlpha, !valueOnly).
                AddElement("Value", Value.ToJson());
        }
    }

    public class InputBoxSetting : Setting<InputBox>
    {
        public InputBoxSetting(string name, InputBox defaultValue) :
            base(name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly)
        {
            return ((JsonObject)base.ToJson(valueOnly)).
                AddDecimalIf("Type", 13, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }
    }
}
