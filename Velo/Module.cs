using CEngine.Graphics.Component;
using CEngine.Graphics.Library;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Velo
{
    public class ModuleManager
    {
        private readonly List<Module> modules = new List<Module>();
        private readonly Dictionary<string, Module> modulesLookup = new Dictionary<string, Module>();
        private readonly Dictionary<int, Setting> idToSetting = new Dictionary<int, Setting>();
        private int nextId = 0;
        private readonly List<Action<Setting>> modifiedListeners = new List<Action<Setting>>();

        public List<Module> Modules { get { return modules; } }

        private ModuleManager()
        {
            
        }

        public static ModuleManager Instance = new ModuleManager();

        public void Init()
        {
            foreach (Type t in typeof(Module).Assembly.GetTypes())
            {
                if (typeof(Module).IsAssignableFrom(t) && !t.IsAbstract)
                {
                    object inst = t.GetField("Instance").GetValue(null);
                }
            }

            modules.Sort((left, right) => left.Name.CompareTo(right.Name));

            modules.ForEach(module => module.Init());
        }

        public void Add(Module module)
        {
            modules.Add(module);
            modulesLookup.Add(module.Name, module);
        }

        public int Add(Setting setting)
        {
            idToSetting.Add(nextId, setting);
            return nextId++;
        }

        public void AddModifiedListener(Action<Setting> listener)
        {
            modifiedListeners.Add(listener);
        }

        public void ReportModified(Setting setting)
        {
            foreach (var listener in modifiedListeners)
            {
                listener(setting);
            }
        }

        public void PreUpdate()
        {
            foreach (var module in modules)
                module.PreUpdate();
        }

        public void PostUpdate()
        {
            foreach (var module in modules)
                module.PostUpdate();
        }

        public void PreRender()
        {
            foreach (var module in modules)
                module.PreRender();
        }

        public void PostRender()
        {
            foreach (var module in modules)
                module.PostRender();
        }

        public JsonElement ToJson(ToJsonArgs args)
        {
            return new JsonObject(2).
                AddDecimal("Count", modules.Sum(module => module.SettingsCount())).
                AddArray("Modules", modules.
                    Where(module => module.HasSettings()).
                    Select(module => module.ToJson(args)).ToList()
                );
        }

        public Module Get(string name)
        {
            if (modulesLookup.ContainsKey(name))
                return modulesLookup[name];
            return null;
        }

        public void CommitChanges(JsonElement elem)
        {
            if (!(elem is JsonObject))
                return;
            JsonElement changes = (elem as JsonObject).Get("Changes");
            if (!(changes is JsonArray))
                return;
            foreach (JsonElement item in (changes as JsonArray).value)
            {
                if (!(item is JsonObject)) 
                    continue;
                JsonElement jsonId = (item as JsonObject).Get("ID");
                if (!(jsonId is JsonDecimal))
                    continue;
                int id = jsonId.AsInt();
                if (idToSetting.ContainsKey(id))
                {
                    idToSetting[id].FromJson(item);
                }
            }
        }
    }

    public abstract class Module
    {
        private readonly string name;
        public string Name => name;
        public string Tooltip { get; set; }

        private readonly List<Setting> settings;
        private readonly Dictionary<string, Setting> settingsLookup;

        private SettingCategory currentCategory;
        protected SettingCategory CurrentCategory => currentCategory;

        public Module(string name)
        {
            this.name = name;
            Tooltip = name;
            settings = new List<Setting>();
            settingsLookup = new Dictionary<string, Setting>();
            currentCategory = null;

            ModuleManager.Instance.Add(this);
        }

        public T Add<T>(T setting) where T : Setting
        {
            if (currentCategory == null)
            {
                settings.Add(setting);
                settingsLookup.Add(setting.Name, setting);
            }
            else
                currentCategory.Children.Add(setting);
            return setting;
        }

        protected void NewCategory(string name)
        {
            currentCategory = new SettingCategory(this, name);
            settings.Add(currentCategory);
            settingsLookup.Add(name, currentCategory);
        }

        public bool HasSettings()
        {
            return settings.Count > 0;
        }

        protected IntSetting AddInt(string name, int defaultValue, int min, int max)
        {
            return Add(new IntSetting(this, name, defaultValue, min, max));
        }

        protected FloatSetting AddFloat(string name, float defaultValue, float min, float max)
        {
            return Add(new FloatSetting(this, name, defaultValue, min, max));
        }

        protected BoolSetting AddBool(string name, bool defaultValue)
        {
            return Add(new BoolSetting(this, name, defaultValue));
        }

        protected ToggleSetting AddToggle(string name, Toggle defaultValue)
        {
            return Add(new ToggleSetting(this, name, defaultValue));
        }

        protected HotkeySetting AddHotkey(string name, ushort defaultValue, bool autoRepeat = false)
        {
            return Add(new HotkeySetting(this, name, defaultValue, autoRepeat));
        }

        protected VectorSetting AddVector(string name, Vector2 defaultValue, Vector2 min, Vector2 max)
        {
            return Add(new VectorSetting(this, name, defaultValue, min, max));
        }

        protected StringSetting AddString(string name, string defaultValue)
        {
            return Add(new StringSetting(this, name, defaultValue));
        }

        protected RoundingMultiplierSetting AddRoundingMultiplier(string name, RoundingMultiplier defaultValue)
        {
            return Add(new RoundingMultiplierSetting(this, name, defaultValue));
        }

        protected BoolListSetting AddBoolList(string name, string[] labels, bool[] defaultValue)
        {
            return Add(new BoolListSetting(this, name, labels, defaultValue));
        }

        protected StringListSetting AddStringList(string name, string[] defaultValue)
        {
            return Add(new StringListSetting(this, name, defaultValue));
        }

        protected EnumSetting<E> AddEnum<E>(string name, E defaultValue, string[] labels) where E : struct
        {
            return Add(new EnumSetting<E>(this, name, defaultValue, labels));
        }

        protected ColorSetting AddColor(string name, Color defaultValue)
        {
            return Add(new ColorSetting(this, name, defaultValue));
        }

        protected ColorTransitionSetting AddColorTransition(string name, ColorTransition defaultValue)
        {
            return Add(new ColorTransitionSetting(this, name, defaultValue));
        }

        protected InputBoxSetting AddInputBox(string name, InputBox defaultValue)
        {
            return Add(new InputBoxSetting(this, name, defaultValue));
        }

        public int SettingsCount()
        {
            return settings.Sum(setting => setting is SettingCategory ? (setting as SettingCategory).Children.Count : 1);
        }

        public JsonElement ToJson(ToJsonArgs args)
        {
            return new JsonObject(3).
                AddString("Name", Name).
                AddStringIf("Tooltip", Tooltip, args.Definition).
                AddArray("Settings", settings.
                    Where(setting => !setting.Hidden || args.Hidden).
                    Select(setting => setting.ToJson(args)).ToList()
                );
        }

        public void LoadSettings(JsonElement elem)
        {
            if (!(elem is JsonObject))
                return;
            JsonElement settings = (elem as JsonObject).Get("Settings");
            if (!(settings is JsonArray))
                return;
            foreach (JsonElement setting in (settings as JsonArray).value)
            {
                JsonElement nameJson = (setting as JsonObject).Get("Name");
                if (!(nameJson is JsonString))
                    continue;
                string name = nameJson.AsString();
                if (settingsLookup.ContainsKey(name))
                {
                    settingsLookup[name].FromJson(setting);
                }
            }
        }

        public virtual void Init() { }
        public virtual void PreUpdate() { }
        public virtual void PostUpdate() { }
        public virtual void PreRender() { }
        public virtual void PostRender() { }
    }

    public abstract class ToggleModule : Module
    {
        public ToggleSetting Enabled;

        public ToggleModule(string name) : base(name)
        {
            Enabled = AddToggle("enabled", new Toggle());
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Input.Pressed(Enabled.Value.Hotkey))
            {
                Enabled.ToggleState();
            }
        }
    }

    public abstract class DisplayModule : ToggleModule
    {
        public bool IngameOnly;

        protected bool added = false;

        public DisplayModule(string name, bool ingameOnly) : base(name)
        {
            IngameOnly = ingameOnly;
        }

        public abstract bool FixedPos { get; }
        public abstract ICDrawComponent Component { get; }
        public abstract void UpdateComponent();

        public override void PreRender()
        {
            base.PreRender();

            if (!FixedPos)
                Update();
        }

        public override void PostRender()
        {
            base.PostRender();

            if (FixedPos)
                Update();
        }

        private void Update()
        {
            bool enabled = Enabled.Value.Enabled;

            if (IngameOnly && !Velo.Ingame)
                enabled = false;

            ICDrawComponent drawComp = Component;

            if (
                !enabled ||
                (!FixedPos && CEngine.CEngine.Instance.LayerManager.GetLayer("LocalPlayersLayer") == null))
            {
                if (added && drawComp != null)
                    CEngine.CEngine.Instance.LayerManager.RemoveDrawer(drawComp);
                added = false;
                return;
            }

            if (FixedPos && added)
            {
                if (drawComp != null)
                    CEngine.CEngine.Instance.LayerManager.RemoveDrawer(drawComp);
                added = false;
            }

            if (!FixedPos && !added)
            {
                CEngine.CEngine.Instance.LayerManager.AddDrawer("LocalPlayersLayer", drawComp);
                added = true;
            }

            UpdateComponent();

            if (FixedPos)
            {
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
                drawComp.Draw(null);
                Velo.SpriteBatch.End();
            }
        }
    }

    public abstract class MultiDisplayModule : ToggleModule
    {
        public bool IngameOnly;

        protected bool added = false;

        private readonly List<ICDrawComponent> components = new List<ICDrawComponent>();
            
        public MultiDisplayModule(string name, bool ingameOnly) : base(name)
        {
            IngameOnly = ingameOnly;
        }

        public abstract bool FixedPos { get; }
        public abstract void UpdateComponents();

        public override void PreRender()
        {
            base.PreRender();

            if (!FixedPos)
                Update();
        }

        public override void PostRender()
        {
            base.PostRender();

            if (FixedPos)
                Update();
        }

        public void AddComponent(ICDrawComponent component)
        {
            components.Add(component);
            if (!FixedPos && added)
                CEngine.CEngine.Instance.LayerManager.AddDrawer("LocalPlayersLayer", component);
        }

        public void RemoveComponent(ICDrawComponent component)
        {
            components.Remove(component);
            if (!FixedPos && added)
                CEngine.CEngine.Instance.LayerManager.RemoveDrawer(component);
        }

        private void Update()
        {
            bool enabled = Enabled.Value.Enabled;

            if (IngameOnly && !Velo.Ingame)
                enabled = false;

            if (
                !enabled ||
                (!FixedPos && CEngine.CEngine.Instance.LayerManager.GetLayer("LocalPlayersLayer") == null))
            {
                if (added)
                {
                    foreach (var component in components)
                        CEngine.CEngine.Instance.LayerManager.RemoveDrawer(component);
                }
                added = false;
                return;
            }

            if (FixedPos && added)
            {  
                foreach (var component in components)
                    CEngine.CEngine.Instance.LayerManager.RemoveDrawer(component);
                added = false;
            }

            if (!FixedPos && !added)
            {
                foreach (var component in components)
                    CEngine.CEngine.Instance.LayerManager.AddDrawer("LocalPlayersLayer", component);
                added = true;
            }

            UpdateComponents();

            if (FixedPos)
            {
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
                foreach (var component in components)
                    component.Draw(null);
                Velo.SpriteBatch.End();
            }
        }
    }

    public enum EOrientation
    {
        PLAYER, TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT, TOP, BOTTOM, LEFT, RIGHT, CENTER
    }

    public static class OrientationExt
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
                    origin.X = 0f;
                    break;
                case EOrientation.TOP_RIGHT:
                case EOrientation.RIGHT:
                case EOrientation.BOTTOM_RIGHT:
                    origin.X = screenWidth - width;
                    break;
                case EOrientation.TOP:
                case EOrientation.CENTER:
                case EOrientation.BOTTOM:
                    origin.X = (screenWidth - width) / 2f;
                    break;
            }

            switch (orientation)
            {
                case EOrientation.TOP_LEFT:
                case EOrientation.TOP:
                case EOrientation.TOP_RIGHT:
                    origin.Y = 0f;
                    break;
                case EOrientation.BOTTOM_LEFT:
                case EOrientation.BOTTOM:
                case EOrientation.BOTTOM_RIGHT:
                    origin.Y = screenHeight - height;
                    break;
                case EOrientation.LEFT:
                case EOrientation.CENTER:
                case EOrientation.RIGHT:
                    origin.Y = (screenHeight - height) / 2f;
                    break;
            }

            return origin;
        }
    }

    public abstract class StatDisplayModule : DisplayModule
    {
        public FloatSetting Scale;
        public EnumSetting<EOrientation> Orientation;
        public VectorSetting Offset;
        public StringSetting Font;
        public IntSetting FontSize;
        public FloatSetting Opacity;
        public FloatSetting Rotation;
        public RoundingMultiplierSetting RoundingMultiplier;
        public BoolSetting DisablePopup;

        protected CachedFont font;
        protected CTextDrawComponent drawComp = null;

        public StatDisplayModule(string name, bool ingameOnly) : base(name, ingameOnly)
        {
            
        }

        protected void AddStyleSettings(bool roundingMultiplier = true, bool disablePopup = true)
        {
            NewCategory("style");
            Scale = AddFloat("scale", 1f, 0f, 10f);
            Orientation = AddEnum("orientation", EOrientation.PLAYER, 
                Enum.GetValues(typeof(EOrientation)).Cast<EOrientation>().Select(orientation => orientation.Label()).ToArray());
            Offset = AddVector("offset", Vector2.Zero, new Vector2(-500f, -500f), new Vector2(500f, 500f));
            Font = AddString("font", "UI\\Font\\Souses.ttf");
            FontSize = AddInt("font size", 24, 1, 50);
            Opacity = AddFloat("opacity", 1f, 0f, 1f);
            Rotation = AddFloat("rotation", -0.05f, 0f, (float)(2 * Math.PI));
            if (roundingMultiplier)
                RoundingMultiplier = AddRoundingMultiplier("rounding multiplier", new RoundingMultiplier("1"));
            if (disablePopup)
                DisablePopup = AddBool("disable popup", true);

            Font.Tooltip =
                "font " +
                "(Root directory is the \"Content\" folder. For more fonts, see \"UI\\Font\" or add your own fonts.)";
            Rotation.Tooltip =
                "rotation angle in radians";
        }

        public abstract void Update();
        public abstract string GetText();
        public abstract Color GetColor();

        public override bool FixedPos => Orientation.Value != EOrientation.PLAYER;

        public override ICDrawComponent Component
        {
            get
            {
                if (drawComp == null)
                    drawComp = new CTextDrawComponent("", null, Vector2.Zero);
                return drawComp;
            }
        }

        public override void UpdateComponent()
        {
            FontCache.Get(ref font, Font.Value + ":" + FontSize.Value);

            Update();

            if (Orientation.Value == EOrientation.PLAYER && Velo.MainPlayer == null)
            {
                drawComp.IsVisible = false;
                return;
            }
            
            drawComp.color_replace = false;
            drawComp.IsVisible = true;
            drawComp.Font = font.Font;
            drawComp.Align = Orientation.Value == EOrientation.PLAYER ?
                0.5f * Vector2.One : Vector2.Zero;
            drawComp.Offset = Offset.Value;
            drawComp.HasDropShadow = true;
            drawComp.DropShadowColor = Color.Black;
            drawComp.DropShadowOffset = Vector2.One;
            drawComp.Flipped = Velo.MainPlayer != null && Velo.MainPlayer.popup.Flipped;
            drawComp.Scale = Scale.Value * Vector2.One;
            drawComp.Rotation = Rotation.Value;
            drawComp.StringText = GetText();
            drawComp.Color = GetColor();
            drawComp.DropShadowColor *= drawComp.Color.A / 255f;
            drawComp.Opacity = Opacity.Value;
            drawComp.UpdateBounds();

            float screenWidth = Velo.SpriteBatch.GraphicsDevice.Viewport.Width;
            float screenHeight = Velo.SpriteBatch.GraphicsDevice.Viewport.Height;

            float width = drawComp.Bounds.Width;
            float height = drawComp.Bounds.Height;

            drawComp.Position = Orientation.Value.GetOrigin(width, height, screenWidth, screenHeight, Velo.MainPlayer != null ? Velo.MainPlayer.actor.Position : Vector2.Zero);

            drawComp.UpdateBounds();
        }
    }
}
