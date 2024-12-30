/*using Microsoft.Xna.Framework;
using System.Windows.Forms;

namespace Velo
{
    public class TestContext : MenuContext
    {
        public LayoutW Layout;

        public TestContext(ToggleSetting enabled) :
            base(enabled, enableDim: true)
        {
            Layout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            AddElem(Layout, StackW.TOP_LEFT, Vector2.Zero, new Vector2(1000f, 1000f));

            LabelW button = new LabelW("Test test", Fonts.FontMedium);
            Style.ApplyButton(button);
            Layout.AddChild(button, LayoutW.FILL);
        }
    }

    public class Test : ToggleModule
    {
        private TestContext context;

        private Test() : base("Test")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F4));
        }

        public static Test Instance = new Test();

        public override void Init()
        {
            base.Init();

            context = new TestContext(Enabled);
        }

        public override void PostRender()
        {
            base.PostRender();

            context.Draw();
        }
    }
}
*/