using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using CEngine.Graphics.Component;
using CEngine.Graphics.Library;
using System.Collections.Generic;
using System.Linq;

namespace Velo
{
    public static class Style
    {
        public static void ApplyText(LabelW text)
        {
            text.Color = () => SettingsUI.Instance.TextColor.Value.Get();
        }

        public static void ApplyTextHeader(LabelW text)
        {
            text.Color = () => SettingsUI.Instance.HeaderTextColor.Value.Get();
        }

        public static void ApplyButton(LabelW button)
        {
            button.Hoverable = true;
            button.BackgroundVisible = true;
            button.BackgroundVisibleHovered = true;
            button.Color = () => SettingsUI.Instance.TextColor.Value.Get();
            button.BackgroundColor = () => SettingsUI.Instance.ButtonColor.Value.Get();
            button.BackgroundColorHovered = () => SettingsUI.Instance.ButtonHoveredColor.Value.Get();
        }

        public static void ApplySelectorButton(SelectorButtonW button)
        {
            button.ButtonBackgroundVisible = true;
            button.ButtonBackgroundVisibleHovered = true;
            button.ButtonBackgroundVisibleSelected = true;
            button.Color = () => SettingsUI.Instance.TextColor.Value.Get();
            button.ButtonBackgroundColor = () => SettingsUI.Instance.ButtonColor.Value.Get();
            button.ButtonBackgroundColorHovered = () => SettingsUI.Instance.ButtonHoveredColor.Value.Get();
            button.ButtonBackgroundColorSelected = () => SettingsUI.Instance.ButtonSelectedColor.Value.Get();
        }

        public static void ApplyList<T>(ListW<T> list) where T : struct
        {
            list.EntryBackgroundVisible = true;
            list.EntryBackgroundColor1 = SettingsUI.Instance.EntryColor1.Value.Get;
            list.EntryBackgroundColor2 = SettingsUI.Instance.EntryColor2.Value.Get;
            list.EntryHoverable = true;
            list.EntryBackgroundColorHovered = SettingsUI.Instance.EntryHoveredColor.Value.Get;
        }

        public static void ApplyScroll(ScrollW scroll)
        {
            scroll.ScrollBarColor = SettingsUI.Instance.ButtonColor.Value.Get;
            scroll.ScrollBarWidth = SettingsUI.Instance.ScrollBarWidth.Value;
        }

        public static void ApplyTable<T>(TableW<T> table) where T : struct
        {
            table.HeaderAlign = new Vector2(0f, 0.5f);
            table.HeaderColor = SettingsUI.Instance.HeaderTextColor.Value.Get;
            table.EntryBackgroundVisible = true;
            table.EntryBackgroundColor1 = SettingsUI.Instance.EntryColor1.Value.Get;
            table.EntryBackgroundColor2 = SettingsUI.Instance.EntryColor2.Value.Get;
            table.EntryHoverable = true;
            table.EntryBackgroundColorHovered = SettingsUI.Instance.EntryHoveredColor.Value.Get;
            table.ScrollBarColor = SettingsUI.Instance.ButtonColor.Value.Get;
            table.ScrollBarWidth = SettingsUI.Instance.ScrollBarWidth.Value;
        }
    }

    public static class LoadSymbol
    {
        public static readonly int SIZE = 50;
        private static readonly float RADIUS = 20f;
        private static readonly float WIDTH = 4f;
        private static Texture2D texture;

        public static Texture2D Get()
        {
            if (texture != null)
                return texture;

            texture = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, SIZE, SIZE);
            Color[] pixels = new Color[SIZE * SIZE];

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    Vector2 diff = new Vector2(x, y) + 0.5f * Vector2.One - new Vector2(SIZE, SIZE) / 2;
                    float length = diff.Length();
                    if (length <= RADIUS + 0.5 && length >= RADIUS - WIDTH - 0.5)
                    {
                        float factor = 1f;
                        if (length >= RADIUS - 0.5f)
                        {
                            factor *= RADIUS - length + 0.5f;
                        }
                        if (length <= RADIUS - WIDTH + 0.5)
                        {
                            factor *= length - RADIUS + WIDTH + 0.5f;
                        }

                        pixels[x + SIZE * y] = Color.Lerp(Color.Black, Color.White, (float)(Math.Atan2(diff.Y, diff.X) / (2.0 * Math.PI) + 0.5)) * factor;
                    }
                    else
                    {
                        pixels[x + SIZE * y] = Color.Transparent;
                    }
                }
            }
            texture.SetData(pixels);
            return texture;
        }
    }

    public abstract class Menu : LayoutW
    {
        protected MenuModule module;

        public Menu(MenuModule module, EOrientation orientation) :
            base(orientation)
        {
            this.module = module;
        }

        public abstract void Refresh();
        public abstract void Reset();
    }

    public class MenuFonts
    {
        public CachedFont FontSmall;
        public CachedFont FontMedium;
        public CachedFont FontLarge;
        public CachedFont FontTitle;
        public CachedFont FontConsole;
    }

    public abstract class MenuModule : ToggleModule
    {
        private WidgetContainer container;
        private TransitionW<Widget> menu;
        private StackW menuStack;
        private TransitionW<Menu> page;

        private List<Menu> menuElems = new List<Menu>();

        private Stack<Menu> backStack;
        public MenuFonts Fonts;
        public string Error;

        private readonly bool enableDim;

        public Menu Page => page.Child;

        public MenuModule(string name, bool addEnabledSetting = true, bool enableDim = true, Vector2? menuPos = null, Vector2? menuSize = null) : base(name, addEnabledSetting)
        {
            this.enableDim = enableDim;
            
            backStack = new Stack<Menu>();

            page = new TransitionW<Menu>();
            menuStack = new StackW();
            menuStack.AddChild(page, menuPos != null ? menuPos.Value : new Vector2(375f, 100f), menuSize != null ? menuSize.Value : new Vector2(1170f, 880f));
            menu = new TransitionW<Widget>();
            container = new WidgetContainer(menu, new Rectangle(0, 0, 1920, 1080));
        }

        public override void Init()
        {
            base.Init();

            Fonts = new MenuFonts();
            
            FontCache.Get(ref Fonts.FontSmall, "UI\\Font\\NotoSans-Regular.ttf:15,UI\\Font\\NotoSansCJKtc-Regular.otf:15,UI\\Font\\NotoSansCJKkr-Regular.otf:15");
            FontCache.Get(ref Fonts.FontMedium, "UI\\Font\\NotoSans-Regular.ttf:18,UI\\Font\\NotoSansCJKtc-Regular.otf:18,UI\\Font\\NotoSansCJKkr-Regular.otf:18");
            FontCache.Get(ref Fonts.FontLarge, "UI\\Font\\NotoSans-Regular.ttf:24,UI\\Font\\NotoSansCJKtc-Regular.otf:24,UI\\Font\\NotoSansCJKkr-Regular.otf:24");
            FontCache.Get(ref Fonts.FontTitle, "UI\\Font\\Souses.ttf:42,UI\\Font\\NotoSansCJKtc-Regular.otf:42,UI\\Font\\NotoSansCJKkr-Regular.otf:42");
            FontCache.Get(ref Fonts.FontConsole, "CEngine\\Debug\\FreeMonoBold.ttf:18");
        }

        public void EnterMenu(Menu menu)
        {
            this.menu.TransitionTo(menuStack, 4f, new Vector2(-500f, 0f));
            page.GoTo(menu);

            Reset();
            Refresh();
            OnChange();
        }

        public void ExitMenu(bool animation = true)
        {
            if (menu.Child == null)
                return;
            Enabled.Disable();
            backStack.Clear();
            if (animation)
                menu.TransitionTo(null, 4f, new Vector2(-500f, 0f));
            else
                menu.GoTo(null);
        }

        public void ChangePage(Menu newPage, bool pushBackStack = true)
        {
            if (page.Child != null && pushBackStack)
                backStack.Push(page.Child);
            page.TransitionTo(newPage, 8f, Vector2.Zero);

            Reset();
            Refresh();
            OnChange();
        }

        public void PushBackStack(Menu menu)
        {
            backStack.Push(menu);
        }

        public void PopBackStack()
        {
            backStack.Pop();
        }

        public void PopPage()
        {
            page.TransitionTo(backStack.Pop(), 8f, Vector2.Zero);
            Refresh();
            OnChange();
        }

        public void AddElem(IWidget elem, Vector2 position, Vector2 size)
        {
            menuStack.AddChild(elem, position, size);
            if (elem is Menu menu)
                menuElems.Add(menu);
        }

        public void Refresh()
        {
            Page.Refresh();
            menuElems.ForEach(elem => elem.Refresh());
        }

        public void Reset()
        {
            Page.Reset();
            menuElems.ForEach(elem => elem.Reset());
        }

        public virtual void OnChange()
        {

        }

        public override void PostRender()
        {
            base.PostRender();

            bool modified = Enabled.Modified();

            if (modified)
            {
                if (Enabled.Value.Enabled)
                    Cursor.EnableCursor(this);
                else
                    Cursor.DisableCursor(this);
            }

            if (modified)
            {
                if (Enabled.Value.Enabled)
                    EnterMenu(GetStartMenu());
                else
                    ExitMenu(animation: true);
            }

            if (menu.Child == null && !menu.Transitioning())
                return;

            if (enableDim)
            {
                CRectangleDrawComponent dimRecDraw = new CRectangleDrawComponent(0, 0, CEngine.CEngine.Instance.GraphicsDevice.Viewport.Width, CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height)
                {
                    IsVisible = true,
                    OutlineEnabled = false,
                    OutlineThickness = 0,
                    FillEnabled = true,
                    FillColor = SettingsUI.Instance.DimColor.Value.Get() * (menu.Child != null ? menu.R : 1f - menu.R)
                };
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
                dimRecDraw.Draw(null);
                Velo.SpriteBatch.End();
            }

            container.Draw();
        }

        public abstract Menu GetStartMenu();
    }
}
