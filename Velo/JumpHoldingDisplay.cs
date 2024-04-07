using System;
using Microsoft.Xna.Framework;

namespace Velo
{
    public class JumpHoldingDisplay : StatDisplayModule
    {
        public FloatSetting BadDuration;
        public ColorTransitionSetting GoodDurationColor;
        public ColorTransitionSetting BadDurationColor;

        private TimeSpan releaseTime = new TimeSpan(0);
        private bool wasConnected = false;

        private string text = "";
        private Color color = Microsoft.Xna.Framework.Color.White;

        private JumpHoldingDisplay() : base("Jump Holding Display", true)
        {
            BadDuration = AddFloat("bad duration", 0.1f, 0.0f, 1.0f);
            GoodDurationColor = AddColorTransition("good color", new ColorTransition(new Color(0, 255, 0)));
            BadDurationColor = AddColorTransition("bad color", new ColorTransition(new Color(255, 0, 0)));

            AddStyleSettings();
            Offset.SetValueAndDefault(new Vector2(7.0f, -90.0f));
            RoundingMultiplier.SetValueAndDefault(new RoundingMultiplier("0.001"));
        }

        public static JumpHoldingDisplay Instance = new JumpHoldingDisplay();

        public override void Update()
        {
            if (Velo.MainPlayer == null)
                return;

            TimeSpan duration = new TimeSpan(0);
            bool durationChanged = false;

            if (Velo.MainPlayer.jumpPressed && Velo.MainPlayer.timespan2 + TimeSpan.FromSeconds(0.25) > Velo.MainPlayer.game_time.TotalGameTime)
            {
                releaseTime = Velo.MainPlayer.game_time.TotalGameTime;
            }

            if (Velo.MainPlayer.grapple.connected && !wasConnected)
            {
                wasConnected = true;
                duration = Velo.MainPlayer.game_time.TotalGameTime - releaseTime;
                durationChanged = true;
            }
            if (!Velo.MainPlayer.grapple.connected)
            {
                wasConnected = false;
            }

            if (durationChanged || text.Length == 0)
            {
                text = Util.ToStringRounded((float)(duration.Ticks / (double)TimeSpan.TicksPerSecond), RoundingMultiplier.Value);
                float ratio = BadDuration.Value != 0.0f ?
                    MathHelper.Clamp((float)(duration.Ticks / (double)TimeSpan.TicksPerSecond) / BadDuration.Value, 0.0f, 1.0f) :
                    1.0f;
                color = Microsoft.Xna.Framework.Color.Lerp(GoodDurationColor.Value.Get(), BadDurationColor.Value.Get(), ratio);
            }
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
