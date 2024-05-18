using Microsoft.Xna.Framework;
using CEngine.Graphics.Component;
using CEngine.Util.Draw;

namespace Velo
{
    public class RopeIndicators : DisplayModule
    {
        public IntSetting MaxCount;
        public IntSetting Thickness;
        public BoolSetting AntiAliasing;
        public ColorTransitionSetting Color;

        public CLineDrawComponent drawComp;

        public Vector2 prevPosition = Vector2.Zero;
        public bool wasConnected = false;
        public int connectedFrames = 0;

        private RopeIndicators() : base("Rope Indicators", true)
        {
            NewCategory("general");
            MaxCount = AddInt("max count", 50, 1, 500);

            NewCategory("style");
            Thickness = AddInt("thickness", 3, 0, 10);
            AntiAliasing = AddBool("anti aliasing", true);
            Color = AddColorTransition("color", new ColorTransition(Microsoft.Xna.Framework.Color.Red));

            AntiAliasing.Tooltip =
                "Draws another slightly thicker line on top with half transparency.";
        }

        public static RopeIndicators Instance = new RopeIndicators();

        public override bool FixedPos()
        {
            return false;
        }

        public override ICDrawComponent GetComponent()
        {
            if (drawComp == null)
                drawComp = new CLineDrawComponent();
            return drawComp;
        }

        public override void UpdateComponent()
        {
            if (Velo.MainPlayer == null || Velo.MainPlayer.grapple == null)
                return;

            if (Enabled.Modified())
                drawComp.lines.Clear();

            if (!Velo.MainPlayer.grapple.connected && wasConnected && connectedFrames >= 2)
            {
                Vector2 playerPos = (prevPosition + Velo.MainPlayer.actor.Position + new Vector2(12.5f, 22.5f)) / 2;
                drawComp.AddLine(new CLine(Velo.MainPlayer.grapple.actor.Collision.Center, playerPos, new Color(0, 0, 0)));
                drawComp.AddLine(new CLine(Velo.MainPlayer.grapple.actor.Collision.Center, playerPos, new Color(0, 0, 0)));
                connectedFrames = 0;
            }

            prevPosition = Velo.MainPlayer.actor.Position + new Vector2(12.5f, 22.5f);

            wasConnected = Velo.MainPlayer.grapple.connected;
            if (Velo.MainPlayer.grapple.connected)
                connectedFrames++;

            while (drawComp.Lines.Count > MaxCount.Value * 2)
            {
                drawComp.Lines.RemoveAt(0);
                drawComp.Lines.RemoveAt(0);
            }

            Color color = Color.Value.Get();

            bool antiAlias = false;

            foreach (CLine line in drawComp.Lines)
            {
                line.color = color;

                if (!antiAlias)
                    line.thickness = Thickness.Value;
                else
                {
                    line.thickness = Thickness.Value + 1;
                    if (AntiAliasing.Value)
                        line.color *= 0.5f;
                    else
                        line.color = new Color(0, 0, 0, 0);
                }

                antiAlias = !antiAlias;
            }
        }
    }
}
