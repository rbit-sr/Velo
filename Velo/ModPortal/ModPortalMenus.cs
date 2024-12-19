using CEngine.Graphics.Library;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Velo
{
    public interface IMpWidget : IWidget
    {
        void PushRequests();
    }

    // just a base class for a menu page that provides a title bar, content and a button row at the bottom
    // it also handles the loading symbol / error message
    // this class may be rewritten or removed
    public abstract class MpMenuPage : LayoutW, IMpWidget
    {
        protected MpContext context;

        protected LabelW title;
        protected LayoutW titleBar;
        protected HolderW<Widget> content; // Child classes will put their content in here
        protected readonly LayoutW buttonRow;

        private readonly bool showStatus;

        private float loadingRotation = -(float)Math.PI / 2f;

        public MpMenuPage(MpContext context, string title, bool showStatus = true) :
            base(EOrientation.VERTICAL)
        {
            this.context = context;
            this.showStatus = showStatus;

            content = new HolderW<Widget>();

            this.title = new LabelW(title, context.Fonts.FontTitle)
            {
                Align = new Vector2(0f, 0.5f),
                Color = SettingsUI.Instance.HeaderTextColor.Value.Get
            };
            titleBar = new LayoutW(EOrientation.HORIZONTAL);
            titleBar.AddChild(this.title, FILL);
            AddChild(titleBar, 80f);
            AddSpace(10f);
            AddChild(content, FILL);
            AddSpace(10f);

            buttonRow = new LayoutW(EOrientation.HORIZONTAL);
            AddChild(buttonRow, 35f);
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!showStatus)
                return;

            // maybe it would be more elegant to not draw these directly
            
            IWidget lowest = Children.Last();
            if (lowest == null)
                return;

            // loading
            if (/*ModsDatabase.Instance.Pending()*/ false)
            {
                context.Error = null;

                float dt = (float)Velo.RealDelta.TotalSeconds;
                if (dt > 1f)
                    dt = 1f;

                loadingRotation += 3f * dt;

                Velo.SpriteBatch.End();
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);

                Vector2 pos =
                    lowest.Position +
                    new Vector2(lowest.Size.X + 8f, (lowest.Size.Y - LoadSymbol.SIZE) / 2f) +
                    Vector2.One * LoadSymbol.SIZE / 2f;
                Velo.SpriteBatch.Draw(LoadSymbol.Get(), pos * scale, new Rectangle?(), Color.White * opacity, loadingRotation, Vector2.One * LoadSymbol.SIZE / 2f, scale, SpriteEffects.None, 0f);
            }
            else
            {
                loadingRotation = -(float)Math.PI / 2f;
            }

            // error
            if (context.Error != "" && context.Error != null)
            {
                Velo.SpriteBatch.End();
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);

                TextDraw errorDraw = new TextDraw()
                {
                    IsVisible = true,
                    Text = Util.LineBreaks("Error: " + context.Error, 30),
                    Color = Color.Red,
                    HasDropShadow = true,
                    DropShadowColor = Color.Black,
                    DropShadowOffset = Vector2.One,
                    Opacity = opacity
                };
                errorDraw.SetFont(context.Fonts.FontMedium);
                errorDraw.UpdateBounds();

                errorDraw.Position =
                    (lowest.Position +
                    new Vector2(lowest.Size.X + 8f, (lowest.Size.Y - errorDraw.Bounds.Height) / 2f)) * scale;
                errorDraw.Scale = Vector2.One * scale;
                errorDraw.Draw(null);
            }
        }

        public abstract void PushRequests();
    }

    // a test entity to show in the list
    public struct TestEntity
    {
        public int Id;
        public string Test;
    }

    // every category gets its own tab widget object
    public class MpBrowseCategoryTab : LayoutW, IMpWidget, IListEntryFactory<TestEntity>
    {
        private readonly MpContext context;

        public enum ECategory
        {
            CHARACTERS, SOUNDS, BACKGROUNDS,
            COUNT
        }

        private readonly ListW<TestEntity> list;
        private readonly ScrollW scroll;

        private readonly ECategory category;

        public MpBrowseCategoryTab(MpContext context, ECategory category) :
            base(EOrientation.VERTICAL)
        {
            this.context = context;
            this.category = category;

            list = new ListW<TestEntity>(this);
            Style.ApplyList(list);

            scroll = new ScrollW(list);
            Style.ApplyScroll(scroll);

            AddChild(scroll, FILL);
        }
        
        public void PushRequests()
        {

        }

        public IEnumerable<TestEntity> GetElems()
        {
            // return ModsDatabase.Instance.GetSomething();
            return Enumerable.Range(0, 100).Select(i => new TestEntity { Id = i, Test = "test" + i + " " + category });
        }

        public float Height(TestEntity elem, int i)
        {
            return 35f;
        }

        public Widget Create(TestEntity elem, int i)
        {
            LabelW label = new LabelW(elem.Test, context.Fonts.FontMedium);
            Style.ApplyText(label);
            return label;
        }
    }

    public class MpBrowsePage : MpMenuPage
    {
        private readonly SelectorButtonW categorySelect;
        private readonly TabbedW<MpBrowseCategoryTab> tabs;
        private readonly LabelW backButton;

        public MpBrowsePage(MpContext context) :
            base(context, "Browse")
        {
            categorySelect = new SelectorButtonW(new[] { "Characters", "Sounds", "Backgrounds" }, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(categorySelect);

            tabs = new TabbedW<MpBrowseCategoryTab>(categorySelect);
            for (int i = 0; i < (int)MpBrowseCategoryTab.ECategory.COUNT; i++)
            {
                tabs.SetTab(i, new MpBrowseCategoryTab(context, (MpBrowseCategoryTab.ECategory)i));
            }
            tabs.OnSwitch = _ => context.Request();

            content.Child = tabs; // never forget

            backButton = new LabelW("Back", context.Fonts.FontMedium);
            Style.ApplyButton(backButton);
            backButton.OnLeftClick = () => context.Page.TransitionBack(8f, Vector2.Zero);

            buttonRow.AddChild(backButton, 190f);
            buttonRow.AddSpace(FILL);
            buttonRow.AddChild(categorySelect, 3 * 190f);
        }
        
        public override void PushRequests()
        {
            tabs.Current.PushRequests();
        }
    }
}
