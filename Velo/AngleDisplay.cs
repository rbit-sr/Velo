using Microsoft.Xna.Framework;
using System;
using System.Windows.Forms;

namespace Velo
{
    public class AngleDisplay : StatDisplayModule
    {
        public FloatSetting BadAngleOffset;
        public ColorTransitionSetting GoodAngleColor;
        public ColorTransitionSetting BadAngleColor;

        public Vector2 position = Vector2.Zero;
        public bool wasConnected = false;
        public int connectedFrames = 0;

        private string text = "";
        private Color color = Microsoft.Xna.Framework.Color.White;

        private AngleDisplay() : base("Angle Display", true)
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F6));
            Offset.SetValueAndDefault(new Vector2(7.0f, -30.0f));
            RoundingMultiplier.SetValueAndDefault(new RoundingMultiplier("0.1"));

            BadAngleOffset = AddFloat("bad angle offset", 20.0f, 0.0f, 180.0f);
            GoodAngleColor = AddColorTransition("good angle color", new ColorTransition(Microsoft.Xna.Framework.Color.Green));
            BadAngleColor = AddColorTransition("bad angle color", new ColorTransition(Microsoft.Xna.Framework.Color.Red));
        }

        public static AngleDisplay Instance = new AngleDisplay();

        public override void Update()
        {
            if (Velo.MainPlayer == null)
                return;

            double angle = 0.0f;
            bool angleChanged = false;

            if (!Velo.MainPlayer.grapple.connected && wasConnected && connectedFrames >= 2)
            {
                Vector2 diff = position - Velo.MainPlayer.grapple.actor.Collision.Center;
                angle = Math.Atan2(diff.Y, diff.X) / Math.PI * 180.0;
                connectedFrames = 0;
                angleChanged = true;
            }

            if (angleChanged || text.Length == 0)
            {
                text = Util.ToStringRounded((float)angle, RoundingMultiplier.Value.Value, RoundingMultiplier.Value.Precision);
                float ratio = MathHelper.Clamp(Math.Abs((float)(angle - 90.0)) / BadAngleOffset.Value, 0.0f, 1.0f);
                color = Microsoft.Xna.Framework.Color.Lerp(GoodAngleColor.Value.Get(), BadAngleColor.Value.Get(), ratio);
            }

            position = Velo.MainPlayer.actor.Position + new Vector2(12.5f, 22.5f);

            wasConnected = Velo.MainPlayer.grapple.connected;
            if (Velo.MainPlayer.grapple.connected)
                connectedFrames++;
        }

        public override string GetText()
        {
            return text;
        }

        public override Color GetColor()
        {
            if (UseFixedColor.Value)
                return Color.Value.Get();

            return color;
        }
    }
}
