using Microsoft.Xna.Framework;
using System.Linq;
using System.Windows.Forms;

namespace Velo
{
    // the Velo module itself
    // base class would be Module
    // a RequestingMenuModule is a special Module type that has a Menu and does requests on certain actions
    public class ModPortal : MenuModule
    {
        public BoolSetting ExampleSetting1;
        public FloatSetting ExampleSetting2;

        private LabelW versionText;

        private ModPortal() : base("Mod Portal", menuPos: new Vector2(375f, 100f), menuSize: new Vector2(1170f, 880f))
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

            // version text in the bottom left
            versionText = new LabelW(Version.VERSION_NAME + " - " + Version.AUTHOR, Fonts.FontSmall);
            Style.ApplyText(versionText);
            versionText.Align = new Vector2(0f, 0.5f);
            versionText.Color = () => Color.Gray * 0.5f;

            AddElem(versionText, new Vector2(20f, 1035f), new Vector2(180f, 25f));
        }

        public override void PreUpdate()
        {
            base.PreUpdate();
        }

        public override void OnChange()
        {
            // ModsDatabase.Instance.CancelAll();
            (Page as IRequestable).PushRequests();
            // ModsDatabase.Instance.RunRequestRuns(Refresh, error => Error = error.Message);
        }

        public override Menu GetStartMenu()
        {
            return new MpBrowsePage(this);
        }
    }
}
