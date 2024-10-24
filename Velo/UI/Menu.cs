using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using CEngine.Graphics.Component;
using CEngine.Graphics.Library;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;

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

    public interface IMenu : IWidget
    {
        void Refresh();
    }

    public abstract class Menu : HolderW<Widget>, IMenu
    {
        protected MenuContext context;

        public Menu(MenuContext context) :
            base()
        {
            this.context = context;
        }

        public abstract void Refresh();
    }

    public abstract class RequestingMenu : HolderW<Widget>, IMenu
    {
        protected RequestingMenuContext context;
        
        public RequestingMenu(RequestingMenuContext context) :
            base()
        {
            this.context = context;
        }

        public abstract void Refresh();
        public abstract void Rerequest();
        public abstract void ResetState();
    }

    public class MenuFonts
    {
        public CachedFont FontSmall;
        public CachedFont FontMedium;
        public CachedFont FontLarge;
        public CachedFont FontTitle;
        public CachedFont FontConsole;
    }

    public abstract class MenuContext<T> : ToggleModule where T : class, IMenu
    {
        protected WidgetContainer container;
        protected TransitionW<Widget> menu;
        protected StackW menuStack;
        protected TransitionW<T> page;
        
        protected Stack<T> backStack;
        public MenuFonts Fonts;
        public string Error;

        private readonly bool enableDim;

        public T Menu => page.Child;

        public MenuContext(string name, bool addEnabledSetting = true, bool enableDim = true, Vector2? menuPos = null, Vector2? menuSize = null) : base(name, addEnabledSetting)
        {
            this.enableDim = enableDim;
            
            backStack = new Stack<T>();

            page = new TransitionW<T>();
            menuStack = new StackW();
            menuStack.AddChild(page, menuPos != null ? menuPos.Value : new Vector2(375f, 100f), menuSize != null ? menuSize.Value : new Vector2(1170f, 880f));
            menu = new TransitionW<Widget>();
            container = new WidgetContainer(menu, new Rectangle(0, 0, 1920, 1080));
        }

        public override void Init()
        {
            base.Init();

            Fonts = new MenuFonts();
            
            FontCache.Get(ref Fonts.FontSmall, "UI\\Font\\NotoSans-Regular.ttf:15");
            FontCache.Get(ref Fonts.FontMedium, "UI\\Font\\NotoSans-Regular.ttf:18,UI\\Font\\NotoSansCJKtc-Regular.otf:18,UI\\Font\\NotoSansCJKkr-Regular.otf:18");
            FontCache.Get(ref Fonts.FontLarge, "UI\\Font\\NotoSans-Regular.ttf:24,UI\\Font\\NotoSansCJKtc-Regular.otf:24,UI\\Font\\NotoSansCJKkr-Regular.otf:24");
            FontCache.Get(ref Fonts.FontTitle, "UI\\Font\\Souses.ttf:42,UI\\Font\\NotoSansCJKtc-Regular.otf:42,UI\\Font\\NotoSansCJKkr-Regular.otf:42");
            FontCache.Get(ref Fonts.FontConsole, "CEngine\\Debug\\FreeMonoBold.ttf:18");
        }

        public void EnterMenu(T menu)
        {
            this.menu.TransitionTo(menuStack, 4f, new Vector2(-500f, 0f));
            page.GoTo(menu);

            OnGoToNewPage();
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

        public void ChangePage(T newPage)
        {
            if (page.Child != null)
                backStack.Push(page.Child);
            page.TransitionTo(newPage, 8f, Vector2.Zero);

            OnGoToNewPage();
        }

        public void PushBackStack(T menu)
        {
            backStack.Push(menu);
        }

        public void PopPage()
        {
            page.TransitionTo(backStack.Pop(), 8f, Vector2.Zero);
            OnGoBackPage();
        }

        public virtual void OnGoToNewPage()
        {
            Menu.Refresh();
        }

        public virtual void OnChangePage()
        {
            Menu.Refresh();
        }

        public virtual void OnGoBackPage()
        {
            Menu.Refresh();
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

        public abstract T GetStartMenu();
    }

    public abstract class MenuContext : MenuContext<Menu>
    {
        protected MenuContext(string name, bool addEnabledSetting = true, bool enableDim = true, Vector2? menuPos = null, Vector2? menuSize = null) : 
            base(name, addEnabledSetting, enableDim, menuPos, menuSize)
        {

        }
    }

    public abstract class RequestingMenuContext : MenuContext<RequestingMenu>
    {
        public RequestingMenuContext(string name, bool addEnabledSetting = true, bool enableDim = true, Vector2? menuPos = null, Vector2? menuSize = null) :
            base(name, addEnabledSetting, enableDim, menuPos, menuSize)
        {

        }

        public override void OnGoToNewPage()
        {
            CancelAllRequests();
            Menu.ResetState();
            Menu.Rerequest();
            Menu.Refresh();
        }

        public override void OnChangePage()
        {
            CancelAllRequests();
            Menu.Rerequest();
            Menu.Refresh();
        }

        public override void OnGoBackPage()
        {
            CancelAllRequests();
            Menu.Rerequest();
            Menu.Refresh();
        }

        public void ClearCacheRerequest()
        {
            CancelAllRequests();
            ClearCache();
            Menu.ResetState();
            Menu.Rerequest();
            Menu.Refresh();
        }

        public abstract void CancelAllRequests();
        public abstract void ClearCache();
    }
}
