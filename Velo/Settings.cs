using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Velo
{
    public struct ToJsonArgs
    {
        public bool Name;
        public bool Definition;
        public bool ID;
        public bool Hidden;

        public static readonly ToJsonArgs ForUIInit = new ToJsonArgs
        {
            Name = true,
            Definition = true,
            ID = true,
            Hidden = false
        };

        public static readonly ToJsonArgs ForUIUpdate = new ToJsonArgs
        {
            Name = false,
            Definition = false,
            ID = true,
            Hidden = false
        };

        public static readonly ToJsonArgs ForStorage = new ToJsonArgs
        {
            Name = true,
            Definition = false,
            ID = false,
            Hidden = true
        };
    }

    public abstract class Setting
    {
        private readonly Module module;
        public Module Module => module;
        private readonly string name;
        public string Name => name;
        private string tooltip;
        public string Tooltip 
        {
            get => tooltip;
            set => tooltip = Util.LineBreaks(value, 80);
        }

        protected bool modified;
        private readonly int id;
        public int Id => id;
        public bool Hidden = false;

        public Setting(Module module, string name)
        {
            this.module = module;
            this.name = name;
            Tooltip = name;
            modified = false;
            if (module != null)
                id = ModuleManager.Instance.Add(this);
        }

        public bool Modified()
        {
            bool value = modified;
            modified = false;
            return value;
        }

        public abstract bool IsDefault();

        public virtual JsonElement ToJson(ToJsonArgs args)
        {
            return new JsonObject(8).
                AddStringIf("Name", Name, args.Name).
                AddStringIf("Tooltip", Tooltip, args.Definition).
                AddDecimalIf("ID", Id, args.ID);
        }

        public virtual void FromJson(JsonElement elem)
        {
            
        }
    }

    public class SettingCategory : Setting
    {
        public List<Setting> Children;

        public SettingCategory(Module module, string name) :
            base(module, name)
        {
            Children = new List<Setting>();
        }

        public override bool IsDefault()
        {
            return true;
        }

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 14, args.Definition).
                AddArray("Value", Children.Select(child => child.ToJson(args)).ToList());
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
                string name = nameJson.AsString();
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

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 0, args.Definition).
                AddDecimalIf("Default", DefaultValue, args.Definition).
                AddDecimalIf("Min", Min, args.Definition).
                AddDecimalIf("Max", Max, args.Definition).
                AddDecimal("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsInt());
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

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 1, args.Definition).
                AddDecimalIf("Default", DefaultValue, args.Definition).
                AddDecimalIf("Min", Min, args.Definition).
                AddDecimalIf("Max", Max, args.Definition).
                AddDecimal("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsFloat());
        }
    }

    public class BoolSetting : Setting<bool>
    {
        public BoolSetting(Module module, string name, bool defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 2, args.Definition).
                AddBooleanIf("Default", DefaultValue, args.Definition).
                AddBoolean("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsBool());
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

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 3, args.Definition).
                AddElementIf("Default", DefaultValue.ToJson(), args.Definition).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsToggle());
        }
    }

    public class HotkeySetting : Setting<ushort>
    {
        private readonly bool autoRepeat;
        private TimeSpan pressTime;
        private TimeSpan lastRepeat;

        public HotkeySetting(Module module, string name, ushort defaultValue, bool autoRepeat = false) :
            base(module, name, defaultValue)
        {
            this.autoRepeat = autoRepeat;
        }

        public bool Pressed()
        {
            if (Input.Pressed(Value))
            {
                if (autoRepeat)
                {
                    pressTime = Velo.Time;
                    lastRepeat = TimeSpan.Zero;
                }
                return true;
            }

            if (!autoRepeat || !Input.Held(Value))
                return false;

            TimeSpan now = Velo.Time;

            if ((now - pressTime).TotalSeconds >= 0.5 && (now - lastRepeat).TotalSeconds >= 0.05)
            {
                lastRepeat = now;
                return true;
            }

            return false;
        }

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 4, args.Definition).
                AddDecimalIf("Default", DefaultValue, args.Definition).
                AddDecimal("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = (ushort)value.AsInt());
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

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 5, args.Definition).
                AddElementIf("Default", DefaultValue.ToJson(), args.Definition).
                AddElementIf("Min", Min.ToJson(), args.Definition).
                AddElementIf("Max", Max.ToJson(), args.Definition).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsVector2());
        }
    }

    public class StringSetting : Setting<string>
    {
        public StringSetting(Module module, string name, string defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 6, args.Definition).
                AddStringIf("Default", DefaultValue, args.Definition).
                AddString("Value", Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsString());
        }
    }

    public class RoundingMultiplierSetting : Setting<RoundingMultiplier>
    {
        public RoundingMultiplierSetting(Module module, string name, RoundingMultiplier defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 7, args.Definition).
                AddElementIf("Default", DefaultValue.ToJson(), args.Definition).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsRoundingMultiplier());
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

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 8, args.Definition).
                AddElementIf("Default", DefaultValue.ToJson(), args.Definition).
                AddArrayIf("Identifiers", labels.Select(label => (JsonElement)new JsonString(label)).ToList(), args.Definition).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsBoolArr());
        }
    }

    public class StringListSetting : Setting<string[]>
    {
        public StringListSetting(Module module, string name, string[] defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 9, args.Definition).
                AddElementIf("Default", DefaultValue.ToJson(), args.Definition).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsStringArr());
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

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 10, args.Definition).
                AddDecimalIf("Default", (int)(object)DefaultValue, args.Definition).
                AddArrayIf("Identifiers", labels.Select(label => (JsonElement)new JsonString(label)).ToList(), args.Definition).
                AddDecimal("Value", (int)(object)Value);
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = (E)(object)value.AsInt());
        }
    }

    public class ColorSetting : Setting<Color>
    {
        public ColorSetting(Module module, string name, Color defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 11, args.Definition).
                AddElementIf("Default", DefaultValue.ToJson(), args.Definition).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsColor());
        }
    }

    public class ColorTransitionSetting : Setting<ColorTransition>
    {
        public ColorTransitionSetting(Module module, string name, ColorTransition defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 12, args.Definition).
                AddElementIf("Default", DefaultValue.ToJson(), args.Definition).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsColorTransition());
        }
    }

    public class InputBoxSetting : Setting<InputBox>
    {
        public InputBoxSetting(Module module, string name, InputBox defaultValue) :
            base(module, name, defaultValue)
        { }

        public override JsonElement ToJson(ToJsonArgs args)
        {
            return ((JsonObject)base.ToJson(args)).
                AddDecimalIf("Type", 13, args.Definition).
                AddElementIf("Default", DefaultValue.ToJson(), args.Definition).
                AddElement("Value", Value.ToJson());
        }

        public override void FromJson(JsonElement elem)
        {
            base.FromJson(elem);

            if (elem is JsonObject setting)
                setting.DoWithValue("Value", value => Value = value.AsInputBox());
        }
    }
}
