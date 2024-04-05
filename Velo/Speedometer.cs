using Microsoft.Xna.Framework;
using System;
using System.Windows.Forms;

namespace Velo
{
    public class Speedometer : StatDisplayModule
    {
        public IntSetting UpdateInterval;
        public BoolSetting ShowXVelocity;

        private long lastUpdate = 0;

        private string text = "";
        private Color color = Microsoft.Xna.Framework.Color.White;

        private Speedometer() : base("Speedometer", true)
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F2));

            UpdateInterval = AddInt("update interval", 100, 0, 2000);
            ShowXVelocity = AddBool("show x-velocity", false);

            AddStyleSettings();
            Offset.SetValueAndDefault(new Vector2(7.0f, -60.0f));
            RoundingMultiplier.SetValueAndDefault(new RoundingMultiplier("5"));
        }

        public static Speedometer Instance = new Speedometer();

        public override void Update()
        {
            if (Velo.MainPlayer == null)
                return;

            float speed = ShowXVelocity.Value ? Velo.MainPlayer.actor.Velocity.X : Velo.MainPlayer.actor.Velocity.Length();

            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (milliseconds - lastUpdate >= UpdateInterval.Value)
            {
                lastUpdate = milliseconds;
                text = Util.ToStringRounded(speed, RoundingMultiplier.Value);

                if (Math.Abs(speed) < 300)
                    color = new Color(220, 220, 255);
                else if (Math.Abs(speed) < 600)
                    color = new Color(0, 200, 200);
                else if (Math.Abs(speed) < 750)
                    color = new Color(20, 200, 20);
                else if (Math.Abs(speed) < 900)
                    color = new Color(140, 230, 0);
                else if (Math.Abs(speed) < 1200)
                    color = new Color(230, 230, 0);
                else if (Math.Abs(speed) < 1400)
                    color = new Color(230, 100, 0);
                else if (Math.Abs(speed) < 1550)
                    color = new Color(180, 0, 0);
                else
                    color = new Color(80, 0, 0);
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
