using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Velo
{
    public abstract class Setting
    {
        public Module Module { get; }
        public string Name { get; }
        public string Tooltip { get; set; }

        protected bool modified;
        public int Id { get; }

        public Setting(Module module, string name)
        {
            Module = module;
            Name = name;
            Tooltip = name;
            modified = false;
            Id = ModuleManager.Instance.Add(this);
        }

        public bool Modified()
        {
            bool value = modified;
            modified = false;
            return value;
        }

        public abstract bool IsDefault();

        public virtual JsonElement ToJson(bool valueOnly = false, bool useId = false)
        {
            return new JsonObject(8).
                AddStringIf("Name", Name, !useId).
                AddStringIf("Tooltip", Tooltip, !valueOnly).
                AddDecimalIf("ID", Id, !valueOnly || useId);
        }

        public virtual void FromJson(JsonElement elem)
        {
            
        }
    }

    public class Category : Setting
    {
        public List<Setting> Children;

        public Category(Module module, string name) :
            base(module, name)
        {
            Children = new List<Setting>();
        }

        public override bool IsDefault()
        {
            return true;
        }

        public override JsonElement ToJson(bool valueOnly = false, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 14, !valueOnly).
                AddArray("Value", Children.Select(child => child.ToJson(valueOnly, useId)).ToList());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (!(elem is JsonObject))
                return;

            JsonElement settings = ((JsonObject)elem).Get("Value");
            if (!(settings is JsonArray))
                return;

            foreach (var setting in ((JsonArray)settings).value)
            {
                JsonElement nameJson = ((JsonObject)setting).Get("Name");
                if (!(nameJson is JsonString))
                    continue;
                string name = FromJsonExt.ToString(nameJson);
                Setting match = Children.Find(child => child.Name == name);
                match?.FromJson(setting);
            }
        }
    }

    public abstract class Setting<T> : Setting
    {
        private T value;

        public T Value
        {
            get { return value; }
            set 
            { 
                this.value = value; 
                modified = true;
                ModuleManager.Instance.ReportModified(this);
            }
        }
        public T DefaultValue { get; set; }

        public Setting(Module module, string name, T defaultValue) :
            base(module,name)
        {
            value = defaultValue;
            DefaultValue = defaultValue;
        }

        public void SetValueAndDefault(T value)
        {
            this.value = value;
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

        public IntSetting(Module module, string name, int defaultValue, int min, int max) :
            base(module, name, defaultValue) 
        {
            Min = min;
            Max = max;
        }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 0, !valueOnly).
                AddDecimalIf("Default", DefaultValue, !valueOnly).
                AddDecimalIf("Min", Min, !valueOnly).
                AddDecimalIf("Max", Max, !valueOnly).
                AddDecimal("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToInt());
        }
    }

    public class FloatSetting : Setting<float>
    {
        public float Min;
        public float Max;

        public FloatSetting(Module module, string name, float defaultValue, float min, float max) :
            base(module, name, defaultValue)
        { 
            Min = min;
            Max = max;
        }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 1, !valueOnly).
                AddDecimalIf("Default", DefaultValue, !valueOnly).
                AddDecimalIf("Min", Min, !valueOnly).
                AddDecimalIf("Max", Max, !valueOnly).
                AddDecimal("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToFloat());
        }
    }

    public class BoolSetting : Setting<bool>
    {
        public BoolSetting(Module module, string name, bool defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 2, !valueOnly).
                AddBooleanIf("Default", DefaultValue, !valueOnly).
                AddBoolean("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToBool());
        }
    }

    public class ToggleSetting : Setting<Toggle>
    {
        public ToggleSetting(Module module, string name, Toggle defaultValue) :
            base(module, name, defaultValue)
        { }

        public void ToggleEnabled()
        {
            Value = new Toggle(!Value.Enabled, Value.Hotkey);
        }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 3, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToToggle());
        }
    }

    public class HotkeySetting : Setting<ushort>
    {
        public HotkeySetting(Module module, string name, ushort defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 4, !valueOnly).
                AddDecimalIf("Default", DefaultValue, !valueOnly).
                AddDecimal("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = (ushort)value.ToInt());
        }
    }

    public class VectorSetting : Setting<Vector2>
    {
        Vector2 Min;
        Vector2 Max;

        public VectorSetting(Module module, string name, Vector2 defaultValue, Vector2 min, Vector2 max) :
            base(module, name, defaultValue)
        { 
            Min = min;
            Max = max;
        }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 5, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElementIf("Min", Min.ToJson(), !valueOnly).
                AddElementIf("Max", Max.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToVector2());
        }
    }

    public class StringSetting : Setting<string>
    {
        public StringSetting(Module module, string name, string defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 6, !valueOnly).
                AddStringIf("Default", DefaultValue, !valueOnly).
                AddString("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = FromJsonExt.ToString(value));
        }
    }

    public class RoundingMultiplierSetting : Setting<RoundingMultiplier>
    {
        public RoundingMultiplierSetting(Module module, string name, RoundingMultiplier defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 7, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToRoundingMultiplier());
        }
    }

    public class HitboxListSetting : Setting<bool[]>
    {
        public HitboxListSetting(Module module, string name, bool[] defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
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

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToBoolArr());
        }
    }

    public class StringListSetting : Setting<string[]>
    {
        public StringListSetting(Module module, string name, string[] defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 9, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToStringArr());
        }
    }

    public class EnumSetting<E> : Setting<E> where E : struct, Enum
    {
        private readonly string[] labels;

        public EnumSetting(Module module, string name, E defaultValue, string[] labels) :
            base(module, name, defaultValue)
        {
            this.labels = labels;
        }

        public override JsonElement ToJson(bool valueOnly = false, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 10, !valueOnly).
                AddDecimalIf("Default", (int)(object)DefaultValue, !valueOnly).
                AddArrayIf("Identifiers", labels.Select(label => (JsonElement)new JsonString(label)).ToList(), !valueOnly).
                AddDecimal("Value", (int)(object)Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = (E)(object)value.ToInt());
        }
    }

    public class ColorSetting : Setting<Color>
    {
        public bool EnableAlpha;

        public ColorSetting(Module module, string name, Color defaultValue, bool enableAlpha = true) :
            base(module, name, defaultValue)
        {
            EnableAlpha = enableAlpha;
        }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 11, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddBooleanIf("EnableAlpha", EnableAlpha, !valueOnly).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToColor());
        }
    }

    public class ColorTransitionSetting : Setting<ColorTransition>
    {
        public bool EnableAlpha;

        public ColorTransitionSetting(Module module, string name, ColorTransition defaultValue, bool enableAlpha = true) :
            base(module, name, defaultValue)
        {
            EnableAlpha = enableAlpha;
        }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 12, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddBooleanIf("EnableAlpha", EnableAlpha, !valueOnly).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToColorTransition());
        }
    }

    public class InputBoxSetting : Setting<InputBox>
    {
        public InputBoxSetting(Module module, string name, InputBox defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(bool valueOnly, bool useId = false)
        {
            return ((JsonObject)base.ToJson(valueOnly, useId)).
                AddDecimalIf("Type", 13, !valueOnly).
                AddElementIf("Default", DefaultValue.ToJson(), !valueOnly).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.ToInputBox());
        }
    }
}
