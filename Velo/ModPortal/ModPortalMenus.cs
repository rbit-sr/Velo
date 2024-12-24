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
    public abstract class MpMenuPage : VLayoutW, IMpWidget
    {
        protected MpContext context;

        protected LabelW title;
        protected HLayoutW titleBar;
        protected HolderW content; // Child classes will put their content in here
        protected readonly LayoutW buttonRow;

        private readonly bool showStatus;

        private float loadingRotation = -(float)Math.PI / 2f;

        public MpMenuPage(MpContext context, string title, bool showStatus = true)
        {
            this.context = context;
            this.showStatus = showStatus;

            content = new HolderW(null);

            this.title = new LabelW(title, context.Fonts.FontTitle)
            {
                Align = new Vector2(0f, 0.5f),
                Color = SettingsUI.Instance.HeaderTextColor.Value.Get
            };
            titleBar = new HLayoutW();
            titleBar.AddChild(this.title, FILL);
            AddChild(titleBar, 80f);
            AddSpace(10f);
            AddChild(content, FILL);
            AddSpace(10f);

            buttonRow = new LayoutW(EOrientation.HORIZONTAL);
            AddChild(buttonRow, 35f);
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
    public class MpBrowseCategoryTab : VLayoutW, IMpWidget, IListEntryFactory<TestEntity>
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

        public MpBrowseCategoryTab(MpContext context, ECategory category)
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

        public IWidget Create(TestEntity elem, int i)
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
        private readonly ButtonW backButton;

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

            backButton = new ButtonW("Back", context.Fonts.FontMedium);
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
