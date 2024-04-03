using Microsoft.Xna.Framework;
using CEngine.Graphics.Component;
using CEngine.Util.Draw;

namespace Velo
{
    public class RopeIndicators : DisplayModule
    {
        public IntSetting Thickness;
        public IntSetting MaxCount;
        public ColorTransitionSetting Color;

        public CLineDrawComponent drawComp;

        public Vector2 position = Vector2.Zero;
        public bool wasConnected = false;
        public int connectedFrames = 0;

        private RopeIndicators() : base("Rope Release Indicators", true)
        {
            Thickness = AddInt("thickness", 3, 0, 10);
            MaxCount = AddInt("max count", 50, 1, 500);
            Color = AddColorTransition("color", new ColorTransition(Microsoft.Xna.Framework.Color.Red));
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
            if (Velo.MainPlayer == null)
                return;

            if (!Velo.MainPlayer.grapple.connected && wasConnected && connectedFrames >= 2)
            {
                Vector2 playerPos = (position + Velo.MainPlayer.actor.Position + new Vector2(12.5f, 22.5f)) / 2;
                drawComp.AddLine(new CLine(Velo.MainPlayer.grapple.actor.Collision.Center, playerPos, new Color(0, 0, 0)));
                drawComp.AddLine(new CLine(Velo.MainPlayer.grapple.actor.Collision.Center, playerPos, new Color(0, 0, 0)));
                connectedFrames = 0;
            }

            position = Velo.MainPlayer.actor.Position + new Vector2(12.5f, 22.5f);

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
                line.color.R = color.R;
                line.color.G = color.G;
                line.color.B = color.B;

                if (!antiAlias)
                    line.thickness = Thickness.Value;
                else
                {
                    line.thickness = Thickness.Value + 2;
                    line.color.A = 128;
                }

                antiAlias = !antiAlias;
            }
        }
    }
}
