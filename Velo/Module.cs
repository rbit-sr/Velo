using CEngine.Graphics.Component;
using CEngine.Graphics.Library;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
namespace Velo
{
    public class ModuleManager
    {
        private readonly List<Module> modules = new List<Module>();
        private readonly Dictionary<string, Module> modulesLookup = new Dictionary<string, Module>();

        private ModuleManager()
        {
            
        }

        public static ModuleManager Instance = new ModuleManager();

        public void Add(Module module)
        {
            modules.Add(module);
            modulesLookup.Add(module.Name, module);
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

        public JsonElement ToJson(bool valueOnly = false)
        {
            return new JsonObject(2).
                AddDecimal("Count", modules.Sum(module => module.SettingsCount())).
                AddArray("Modules", modules.Where(module => module.HasSettings()).Select(module => module.ToJson(valueOnly)).ToList());
        }

        public void CommitChanges(JsonElement elem)
        {
            if (!(elem is JsonObject))
                return;
            JsonElement changes = ((JsonObject)elem).Get("Changes");
            if (!(changes is JsonArray))
                return;
            foreach (JsonElement item in ((JsonArray)changes).value)
            {
                if (!(item is JsonObject)) 
                    continue;
                JsonElement jsonId = ((JsonObject)item).Get("ID");
                if (!(jsonId is JsonDecimal))
                    continue;
                int id = jsonId.ToInt();
                if (Setting.IdToSetting.ContainsKey(id))
                {
                    Setting.IdToSetting[id].FromJson(item);
                }
            }
        }

        public void LoadSettings(JsonElement elem)
        {
            if (!(elem is JsonObject))
                return;
            JsonElement modules = ((JsonObject)elem).Get("Modules");
            if (!(modules is JsonArray))
                return;
            foreach (JsonElement module in ((JsonArray)modules).value)
            {
                if (!(module is JsonObject)) 
                    continue;
                JsonElement nameJson = ((JsonObject)module).Get("Name");
                if (!(nameJson is JsonString))
                    continue;
                string name = FromJsonExt.ToString(nameJson);
                if (modulesLookup.ContainsKey(name))
                {
                    modulesLookup[name].LoadSettings(module);
                }
            }
        }
    }

    public abstract class Module
    {
        public string Name { get; }

        private readonly List<Setting> settings;
        private readonly Dictionary<string, Setting> settingsLookup;

        private Category currentCategory;

        public Module(string name)
        {
            Name = name;
            settings = new List<Setting>();
            settingsLookup = new Dictionary<string, Setting>();
            currentCategory = null;

            ModuleManager.Instance.Add(this);
        }

        private Setting Add(Setting setting)
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
            currentCategory = new Category(name);
            settings.Add(currentCategory);
            settingsLookup.Add(name, currentCategory);
        }

        protected void EndCategory()
        {
            currentCategory = null;
        }

        public bool HasSettings()
        {
            return settings.Count > 0;
        }

        protected IntSetting AddInt(string name, int defaultValue, int min, int max)
        {
            return (IntSetting)Add(new IntSetting(name, defaultValue, min, max));
        }

        protected FloatSetting AddFloat(string name, float defaultValue, float min, float max)
        {
            return (FloatSetting)Add(new FloatSetting(name, defaultValue, min, max));
        }

        protected BoolSetting AddBool(string name, bool defaultValue)
        {
            return (BoolSetting)Add(new BoolSetting(name, defaultValue));
        }

        protected ToggleSetting AddToggle(string name, Toggle defaultValue)
        {
            return (ToggleSetting)Add(new ToggleSetting(name, defaultValue));
        }

        protected HotkeySetting AddHotkey(string name, ushort defaultValue)
        {
            return (HotkeySetting)Add(new HotkeySetting(name, defaultValue));
        }

        protected VectorSetting AddVector(string name, Vector2 defaultValue, Vector2 min, Vector2 max)
        {
            return (VectorSetting)Add(new VectorSetting(name, defaultValue, min, max));
        }

        protected StringSetting AddString(string name, string defaultValue)
        {
            return (StringSetting)Add(new StringSetting(name, defaultValue));
        }

        protected RoundingMultiplierSetting AddRoundingMultiplier(string name, RoundingMultiplier defaultValue)
        {
            return (RoundingMultiplierSetting)Add(new RoundingMultiplierSetting(name, defaultValue));
        }

        protected HitboxListSetting AddHitboxList(string name, bool[] defaultValue)
        {
            return (HitboxListSetting)Add(new HitboxListSetting(name, defaultValue));
        }

        protected StringListSetting AddStringList(string name, string[] defaultValue)
        {
            return (StringListSetting)Add(new StringListSetting(name, defaultValue));
        }

        protected OrientationSetting AddOrientation(string name, EOrientation defaultValue)
        {
            return (OrientationSetting)Add(new OrientationSetting(name, defaultValue));
        }

        protected LineStyleSetting AddLineStyle(string name, ELineStyle defaultValue)
        {
            return (LineStyleSetting)Add(new LineStyleSetting(name, defaultValue));
        }

        protected ColorSetting AddColor(string name, Color defaultValue, bool enableAlpha = true)
        {
            return (ColorSetting)Add(new ColorSetting(name, defaultValue, enableAlpha));
        }

        protected ColorTransitionSetting AddColorTransition(string name, ColorTransition defaultValue, bool enableAlpha = true)
        {
            return (ColorTransitionSetting)Add(new ColorTransitionSetting(name, defaultValue, enableAlpha));
        }

        protected InputBoxSetting AddInputBox(string name, InputBox defaultValue)
        {
            return (InputBoxSetting)Add(new InputBoxSetting(name, defaultValue));
        }

        public int SettingsCount()
        {
            return settings.Sum(setting => setting is Category category ? category.Children.Count : 1);
        }

        public JsonElement ToJson(bool valueOnly = false)
        {
            return new JsonObject(2).
                AddString("Name", Name).
                AddArray("Settings", settings.Select(setting => setting.ToJson(valueOnly)).ToList());
        }

        public void LoadSettings(JsonElement elem)
        {
            if (!(elem is JsonObject))
                return;
            JsonElement settings = ((JsonObject)elem).Get("Settings");
            if (!(settings is JsonArray))
                return;
            foreach (JsonElement setting in ((JsonArray)settings).value)
            {
                JsonElement nameJson = ((JsonObject)setting).Get("Name");
                if (!(nameJson is JsonString))
                    continue;
                string name = FromJsonExt.ToString(nameJson);
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

            if (Keyboard.Pressed[Enabled.Value.Hotkey])
            {
                Enabled.ToggleEnabled();
                Enabled.modified = true;
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

        public abstract bool FixedPos();
        public abstract ICDrawComponent GetComponent();
        public abstract void UpdateComponent();

        public override void PreRender()
        {
            base.PreRender();

            if (!FixedPos())
                Update();
        }

        public override void PostRender()
        {
            base.PostRender();

            if (FixedPos())
                Update();
        }

        private void Update()
        {
            bool enabled = Enabled.Value.Enabled;

            if (IngameOnly && !Velo.Ingame)
                enabled = false;

            ICDrawComponent drawComp = GetComponent();

            if (
                !enabled ||
                (!FixedPos() && CEngine.CEngine.Instance.LayerManager.GetLayer("LocalPlayersLayer") == null))
            {
                if (added && drawComp != null)
                    CEngine.CEngine.Instance.LayerManager.RemoveDrawer(drawComp);
                added = false;
                return;
            }

            if (FixedPos() && added)
            {
                if (drawComp != null)
                    CEngine.CEngine.Instance.LayerManager.RemoveDrawer(drawComp);
                added = false;
            }

            if (!FixedPos() && !added)
            {
                CEngine.CEngine.Instance.LayerManager.AddDrawer("LocalPlayersLayer", drawComp);
                added = true;
            }

            UpdateComponent();

            if (FixedPos())
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

        public abstract bool FixedPos();
        public abstract void UpdateComponents();

        public override void PreRender()
        {
            base.PreRender();

            if (!FixedPos())
                Update();
        }

        public override void PostRender()
        {
            base.PostRender();

            if (FixedPos())
                Update();
        }

        public void AddComponent(ICDrawComponent component)
        {
            components.Add(component);
            if (!FixedPos() && added)
                CEngine.CEngine.Instance.LayerManager.AddDrawer("LocalPlayersLayer", component);
        }

        public void RemoveComponent(ICDrawComponent component)
        {
            components.Remove(component);
            if (!FixedPos() && added)
                CEngine.CEngine.Instance.LayerManager.RemoveDrawer(component);
        }

        private void Update()
        {
            bool enabled = Enabled.Value.Enabled;

            if (IngameOnly && !Velo.Ingame)
                enabled = false;

            if (
                !enabled ||
                (!FixedPos() && CEngine.CEngine.Instance.LayerManager.GetLayer("LocalPlayersLayer") == null))
            {
                if (added)
                {
                    foreach (var component in components)
                        CEngine.CEngine.Instance.LayerManager.RemoveDrawer(component);
                }
                added = false;
                return;
            }

            if (FixedPos() && added)
            {  
                foreach (var component in components)
                    CEngine.CEngine.Instance.LayerManager.RemoveDrawer(component);
                added = false;
            }

            if (!FixedPos() && !added)
            {
                foreach (var component in components)
                    CEngine.CEngine.Instance.LayerManager.AddDrawer("LocalPlayersLayer", component);
                added = true;
            }

            UpdateComponents();

            if (FixedPos())
            {
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
                foreach (var component in components)
                    component.Draw(null);
                Velo.SpriteBatch.End();
            }
        }
    }

    public abstract class StatDisplayModule : DisplayModule
    {
        public FloatSetting Scale;
        public OrientationSetting Orientation;
        public VectorSetting Offset;
        public StringSetting Font;
        public IntSetting FontSize;
        public FloatSetting Opacity;
        public FloatSetting Rotation;
        public RoundingMultiplierSetting RoundingMultiplier;
        public BoolSetting DisablePopup;
        public BoolSetting UseFixedColor;
        public ColorTransitionSetting Color;

        protected CFont font;
        protected CTextDrawComponent drawComp = null;

        public StatDisplayModule(string name, bool ingameOnly) : base(name, ingameOnly)
        {
            
        }

        protected void AddStyleSettings(bool roundingMultiplier = true, bool disablePopup = true, bool useFixedColor = true)
        {
            NewCategory("style");
            Scale = AddFloat("scale", 1.5f, 0.0f, 10.0f);
            Orientation = AddOrientation("orientation", EOrientation.PLAYER);
            Offset = AddVector("offset", Vector2.Zero, new Vector2(-500.0f, -500.0f), new Vector2(500.0f, 500.0f));
            Font = AddString("font", "UI\\Font\\Souses.ttf");
            FontSize = AddInt("font size", 16, 1, 50);
            Opacity = AddFloat("opacity", 1.0f, 0.0f, 1.0f);
            Rotation = AddFloat("rotation", -0.05f, 0.0f, 3.14159f);
            if (roundingMultiplier)
                RoundingMultiplier = AddRoundingMultiplier("rounding multiplier", new RoundingMultiplier("1"));
            if (disablePopup)
                DisablePopup = AddBool("disable popup", true);
            if (useFixedColor)
                UseFixedColor = AddBool("fixed color", false);
            Color = AddColorTransition("color", new ColorTransition(Microsoft.Xna.Framework.Color.Yellow));
            EndCategory();
        }

        public abstract void Update();
        public abstract string GetText();
        public abstract Color GetColor();

        public override bool FixedPos()
        {
            return Orientation.Value != EOrientation.PLAYER;
        }

        public override ICDrawComponent GetComponent()
        {
            if (drawComp == null)
                drawComp = new CTextDrawComponent("", null, Vector2.Zero);
            return drawComp;
        }

        public override void UpdateComponent()
        {
            if (Font.Modified() || FontSize.Modified() || Scale.Modified())
            {
                if (font != null)
                    Velo.ContentManager.Release(font);
                font = null;
            }

            if (font == null)
            {
                font = new CFont(Font.Value, (int)(FontSize.Value * Scale.Value));
                Velo.ContentManager.Load(font, false);
            }

            Update();

            drawComp.color_replace = false;
            drawComp.IsVisible = true;
            drawComp.Font = font;
            drawComp.Align = 0.5f * Vector2.One;
            drawComp.Offset = Offset.Value;
            drawComp.HasDropShadow = true;
            drawComp.DropShadowColor = Microsoft.Xna.Framework.Color.Black;
            drawComp.DropShadowOffset = Vector2.One;
            drawComp.Flipped = Velo.MainPlayer != null && Velo.MainPlayer.popup.Flipped;
            drawComp.Scale = Vector2.One;
            drawComp.Rotation = Rotation.Value;
            drawComp.StringText = GetText();
            drawComp.Color = GetColor();
            drawComp.Opacity = Opacity.Value;
            drawComp.UpdateBounds();

            float screenWidth = Velo.SpriteBatch.GraphicsDevice.Viewport.Width;
            float screenHeight = Velo.SpriteBatch.GraphicsDevice.Viewport.Height;

            float width = drawComp.Bounds.Width;
            float height = drawComp.Bounds.Height;

            Vector2 origin = Vector2.Zero;

            if (Orientation.Value == EOrientation.PLAYER && Velo.MainPlayer != null)
                origin = Velo.MainPlayer.actor.Position;

            switch (Orientation.Value)
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

            switch (Orientation.Value)
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

            drawComp.Position = origin;
            drawComp.UpdateBounds();
        }
    }
}
