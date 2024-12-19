using Microsoft.Xna.Framework;
using System.Windows.Forms;

namespace Velo
{
    public class MpContext : MenuContext
    {
        public BackstackW<IMpWidget> Page;
        public LabelW VersionText;

        public MpContext(ToggleSetting enabled)
            : base(enabled, enableDim: true)
        {
            Page = new BackstackW<IMpWidget>();
            VersionText = new LabelW(Version.VERSION_NAME + " - " + Version.AUTHOR, Fonts.FontSmall);

            AddElem(Page, new Vector2(375f, 100f), new Vector2(1170f, 880f));
            AddElem(VersionText, new Vector2(20f, 1035f), new Vector2(180f, 25f));
        }

        public override void EnterMenu()
        {
            Page.TransitionTo(new MpBrowsePage(this), 8f, Vector2.Zero);

            base.EnterMenu();

            Request();
        }

        public void Request()
        {
            // ModsDatabase.Instance.CancelAll();
            Page.Child.PushRequests();
            // ModsDatabase.Instance.RunRequestMods(Refresh, error => Error = error.message);
        }
    }

    public class ModPortal : ToggleModule
    {
        public BoolSetting ExampleSetting1;
        public FloatSetting ExampleSetting2;

        private MpContext context;

        private ModPortal() : base("Mod Portal")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F3));

            NewCategory("Category 1");
            ExampleSetting1 = AddBool("example 1", true);

            ExampleSetting1.Tooltip = "Tooltip";

            NewCategory("Category 2");
            ExampleSetting2 = AddFloat("example 2", 3f, 1f, 5f);
        }

        public static ModPortal Instance = new ModPortal();

        public override void Init()
        {
            base.Init();

            context = new MpContext(Enabled);
        }

        public override void PreUpdate()
        {
            base.PreUpdate();
        }

        public override void PostRender()
        {
            base.PostRender();

            context.Render();
        }
    }
}
