using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Velo
{
    public abstract class Setting
    {
        private readonly Module module;
        public Module Module { get { return module; } }
        private readonly string name;
        public string Name { get { return name; } }
        private string tooltip;
        public string Tooltip 
        { 
            get { return tooltip; }
            set
            {
                tooltip = Util.LineBreaks(value, 80);
            }
        }

        protected bool modified;
        private readonly int id;
        public int Id { get { return id; } }
        public bool Hidden = false;

        public Setting(Module module, string name)
        {
            this.module = module;
            this.name = name;
            Tooltip = name;
            modified = false;
            id = ModuleManager.Instance.Add(this);
        }

        public Setting(Module module, string name, int id)
        {
            this.module = module;
            this.name = name;
            Tooltip = name;
            modified = false;
            this.id = id;
        }

        public bool Modified()
        {
            bool value = modified;
            modified = false;
            return value;
        }

        public abstract bool IsDefault();

        public virtual JsonElement ToJson(EToJsonType toJsonType)
        {
            return new JsonObject(8).
                AddStringIf("Name", Name, toJsonType.ForUIInit() || toJsonType.ForSaveFile()).
                AddStringIf("Tooltip", Tooltip, toJsonType.ForUIInit()).
                AddDecimalIf("ID", Id, toJsonType.ForUIInit() || toJsonType.ForUIUpdate());
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

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 14, toJsonType.ForUIInit()).
                AddArray("Value", Children.Select(child => child.ToJson(toJsonType)).ToList());
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
                if (match == null)
                    continue;
                match.FromJson(setting);
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
                bool modified = false;
                if (!this.value.Equals(value))
                    modified = true;
                this.value = value; 
                this.modified |= modified;
                if (modified)
                    ModuleManager.Instance.ReportModified(this);
            }
        }
        public T DefaultValue { get; set; }

        public Setting(Module module, string name, T defaultValue) :
            base(module, name)
        {
            value = defaultValue;
            DefaultValue = defaultValue;
        }

        public Setting(Module module, string name, T defaultValue, int id) :
           base(module, name, id)
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

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 0, toJsonType.ForUIInit()).
                AddDecimalIf("Default", DefaultValue, toJsonType.ForUIInit()).
                AddDecimalIf("Min", Min, toJsonType.ForUIInit()).
                AddDecimalIf("Max", Max, toJsonType.ForUIInit()).
                AddDecimal("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToInt()));
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

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 1, toJsonType.ForUIInit()).
                AddDecimalIf("Default", DefaultValue, toJsonType.ForUIInit()).
                AddDecimalIf("Min", Min, toJsonType.ForUIInit()).
                AddDecimalIf("Max", Max, toJsonType.ForUIInit()).
                AddDecimal("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToFloat()));
        }
    }

    public class BoolSetting : Setting<bool>
    {
        public BoolSetting(Module module, string name, bool defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 2, toJsonType.ForUIInit()).
                AddBooleanIf("Default", DefaultValue, toJsonType.ForUIInit()).
                AddBoolean("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToBool()));
        }
    }

    public class ToggleSetting : Setting<Toggle>
    {
        public ToggleSetting(Module module, string name, Toggle defaultValue) :
            base(module, name, defaultValue)
        { }

        public void ToggleState()
        {
            Value = new Toggle(!Value.Enabled, Value.Hotkey);
        }

        public void Enable()
        {
            Value = new Toggle(true, Value.Hotkey);
        }

        public void Disable()
        {
            Value = new Toggle(false, Value.Hotkey);
        }

        public override bool IsDefault()
        {
            return Value.Enabled == DefaultValue.Enabled;
        }

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 3, toJsonType.ForUIInit()).
                AddElementIf("Default", DefaultValue.ToJson(), toJsonType.ForUIInit()).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToToggle()));
        }
    }

    public class HotkeySetting : Setting<ushort>
    {
        public HotkeySetting(Module module, string name, ushort defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 4, toJsonType.ForUIInit()).
                AddDecimalIf("Default", DefaultValue, toJsonType.ForUIInit()).
                AddDecimal("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = (ushort)value.ToInt()));

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

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 5, toJsonType.ForUIInit()).
                AddElementIf("Default", DefaultValue.ToJson(), toJsonType.ForUIInit()).
                AddElementIf("Min", Min.ToJson(), toJsonType.ForUIInit()).
                AddElementIf("Max", Max.ToJson(), toJsonType.ForUIInit()).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToVector2()));
        }
    }

    public class StringSetting : Setting<string>
    {
        public StringSetting(Module module, string name, string defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 6, toJsonType.ForUIInit()).
                AddStringIf("Default", DefaultValue, toJsonType.ForUIInit()).
                AddString("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = FromJsonExt.ToString(value)));

        }
    }

    public class RoundingMultiplierSetting : Setting<RoundingMultiplier>
    {
        public RoundingMultiplierSetting(Module module, string name, RoundingMultiplier defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 7, toJsonType.ForUIInit()).
                AddElementIf("Default", DefaultValue.ToJson(), toJsonType.ForUIInit()).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToRoundingMultiplier()));
        }
    }

    public class BoolListSetting : Setting<bool[]>
    {
        private readonly string[] labels;

        public BoolListSetting(Module module, string name, string[] labels, bool[] defaultValue) :
            base(module, name, defaultValue)
        {
            this.labels = labels;
        }

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 8, toJsonType.ForUIInit()).
                AddElementIf("Default", DefaultValue.ToJson(), toJsonType.ForUIInit()).
                AddArrayIf("Identifiers", labels.Select(label => (JsonElement)new JsonString(label)).ToList(), toJsonType.ForUIInit()).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToBoolArr()));
        }
    }

    public class StringListSetting : Setting<string[]>
    {
        public StringListSetting(Module module, string name, string[] defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 9, toJsonType.ForUIInit()).
                AddElementIf("Default", DefaultValue.ToJson(), toJsonType.ForUIInit()).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToStringArr()));
        }
    }

    public class EnumSetting<E> : Setting<E> where E : struct
    {
        private readonly string[] labels;

        public EnumSetting(Module module, string name, E defaultValue, string[] labels) :
            base(module, name, defaultValue)
        {
            this.labels = labels;
        }

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 10, toJsonType.ForUIInit()).
                AddDecimalIf("Default", (int)(object)DefaultValue, toJsonType.ForUIInit()).
                AddArrayIf("Identifiers", labels.Select(label => (JsonElement)new JsonString(label)).ToList(), toJsonType.ForUIInit()).
                AddDecimal("Value", (int)(object)Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = (E)(object)value.ToInt()));
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

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 11, toJsonType.ForUIInit()).
                AddElementIf("Default", DefaultValue.ToJson(), toJsonType.ForUIInit()).
                AddBooleanIf("EnableAlpha", EnableAlpha, toJsonType.ForUIInit()).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToColor()));
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

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 12, toJsonType.ForUIInit()).
                AddElementIf("Default", DefaultValue.ToJson(), toJsonType.ForUIInit()).
                AddBooleanIf("EnableAlpha", EnableAlpha, toJsonType.ForUIInit()).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToColorTransition()));
        }
    }

    public class InputBoxSetting : Setting<InputBox>
    {
        public InputBoxSetting(Module module, string name, InputBox defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(EToJsonType toJsonType)
        {
            return ((JsonObject)base.ToJson(toJsonType)).
                AddDecimalIf("Type", 13, toJsonType.ForUIInit()).
                AddElementIf("Default", DefaultValue.ToJson(), toJsonType.ForUIInit()).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            elem.Match<JsonObject>(setting => setting.DoWithValue("Value", value => Value = value.ToInputBox()));
        }
    }
}
