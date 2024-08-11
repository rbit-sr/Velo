using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using CEngine.Graphics.Component;
using CEngine.Graphics.Library;
using System.Collections.Generic;

namespace Velo
{
    public static class Style
    {
        public static void ApplyText(LabelW text)
        {
            text.Color = () => Leaderboard.Instance.TextColor.Value.Get();
        }

        public static void ApplyTextHeader(LabelW text)
        {
            text.Color = () => Leaderboard.Instance.HeaderTextColor.Value.Get();
        }

        public static void ApplyButton(LabelW button)
        {
            button.Hoverable = true;
            button.BackgroundVisible = true;
            button.BackgroundVisibleHovered = true;
            button.Color = () => Leaderboard.Instance.TextColor.Value.Get();
            button.BackgroundColor = () => Leaderboard.Instance.ButtonColor.Value.Get();
            button.BackgroundColorHovered = () => Leaderboard.Instance.ButtonHoveredColor.Value.Get();
        }

        public static void ApplySelectorButton(SelectorButtonW button)
        {
            button.ButtonBackgroundVisible = true;
            button.ButtonBackgroundVisibleHovered = true;
            button.ButtonBackgroundVisibleSelected = true;
            button.Color = () => Leaderboard.Instance.TextColor.Value.Get();
            button.ButtonBackgroundColor = () => Leaderboard.Instance.ButtonColor.Value.Get();
            button.ButtonBackgroundColorHovered = () => Leaderboard.Instance.ButtonHoveredColor.Value.Get();
            button.ButtonBackgroundColorSelected = () => Leaderboard.Instance.ButtonSelectedColor.Value.Get();
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

    public abstract class Menu : HolderW<Widget>
    {
        protected MenuContext context;

        public Menu(MenuContext context) :
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
    }

    public abstract class MenuContext
    {
        protected readonly WidgetContainer container;
        protected readonly TransitionW<Widget> menu;
        protected readonly StackW menuStack;
        protected readonly TransitionW<Menu> page;

        protected readonly Stack<Menu> backStack;
        public MenuFonts Fonts;
        public string Error;

        public Menu Menu => page.Child;

        public MenuContext()
        {
            Fonts = new MenuFonts();

            FontCache.Get(ref Fonts.FontSmall, "UI\\Font\\NotoSans-Regular.ttf:15");
            FontCache.Get(ref Fonts.FontMedium, "UI\\Font\\NotoSans-Regular.ttf:18,UI\\Font\\NotoSansCJKtc-Regular.otf:18,UI\\Font\\NotoSansCJKkr-Regular.otf:18");
            FontCache.Get(ref Fonts.FontLarge, "UI\\Font\\Souses.ttf:42,UI\\Font\\NotoSansCJKtc-Regular.otf:42,UI\\Font\\NotoSansCJKkr-Regular.otf:42");

            backStack = new Stack<Menu>();

            page = new TransitionW<Menu>();
            menuStack = new StackW();
            menuStack.AddChild(page, new Vector2(375f, 100f), new Vector2(1170f, 880f));
            menu = new TransitionW<Widget>();
            container = new WidgetContainer(menu, new Rectangle(0, 0, 1920, 1080));
        }

        public void EnterMenu(Menu menu)
        {
            this.menu.TransitionTo(menuStack, 4f, new Vector2(-500f, 0f));
            this.page.GoTo(menu);

            ResetStateRerequest();
        }

        public void ExitMenu(bool animation = true)
        {
            OnExit();
            backStack.Clear();
            if (animation)
                menu.TransitionTo(null, 4f, new Vector2(-500f, 0f));
            else
                menu.GoTo(null);
        }

        public abstract void OnExit();

        public virtual void ChangePage(Menu newPage)
        {
            if (page.Child != null)
                backStack.Push(page.Child);
            page.TransitionTo(newPage, 8f, Vector2.Zero);

            ResetStateRerequest();
        }

        public void PushBackStack(Menu menu)
        {
            backStack.Push(menu);
        }

        public void PopPage()
        {
            page.TransitionTo(backStack.Pop(), 8f, Vector2.Zero);
            Rerequest();
        }

        public void ResetStateRerequest()
        {
            OnCancelAllRequests();
            Menu.ResetState();
            Menu.Rerequest();
            Menu.Refresh();
        }

        public void Rerequest()
        {
            OnCancelAllRequests();
            Menu.Rerequest();
            Menu.Refresh();
        }

        public void ClearCacheRerequest()
        {
            OnCancelAllRequests();
            OnClearCache();
            Menu.ResetState();
            Menu.Rerequest();
            Menu.Refresh();
        }

        public abstract void OnCancelAllRequests();
        public abstract void OnClearCache();

        public void Draw()
        {
            if (menu.Child == null && !menu.Transitioning())
                return;

            CRectangleDrawComponent dimRecDraw = new CRectangleDrawComponent(0, 0, CEngine.CEngine.Instance.GraphicsDevice.Viewport.Width, CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height)
            {
                IsVisible = true,
                OutlineEnabled = false,
                OutlineThickness = 0,
                FillEnabled = true,
                FillColor = Leaderboard.Instance.DimColor.Value.Get() * (menu.Child != null ? menu.R : 1f - menu.R)
            };
            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            dimRecDraw.Draw(null);
            Velo.SpriteBatch.End();

            container.Draw();
        }
    }
}
