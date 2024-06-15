using Microsoft.Xna.Framework;
using System;
using System.Windows.Forms;

namespace Velo
{
    public class Speedometer : StatDisplayModule
    {
        public enum EVariable
        {
            GLOBAL_TIME, SPEED
        }

        private static readonly string[] VariableLabels = new[] { "global time", "speed" };

        public enum ESpeedType
        {
            ABSOLUTE, X_VELOCITY, Y_VELOCITY, X_VELOCITY_ABSOLUTE, Y_VELOCITY_ABSOLUTE, POSITION
        }

        private static readonly string[] SpeedTypeLabels = new[]
        {
            "absolute", "x-velocity", "y-velocity", "x-velocity (absolute)", "y-velocity (absolute)", "position"
        };

        public IntSetting UpdateInterval;
        public EnumSetting<ESpeedType> SpeedType;

        public EnumSetting<EVariable> Variable;
        public ColorTransitionSetting Color;

        private long lastUpdate = 0;

        private string text = "";
        private float speed = 0f;

        private Speedometer() : base("Speedometer", true)
        {
            NewCategory("general");
            UpdateInterval = AddInt("update interval", 100, 0, 2000);
            SpeedType = AddEnum("speed type", ESpeedType.ABSOLUTE, SpeedTypeLabels);

            UpdateInterval.Tooltip =
                "update interval in milliseconds";
            SpeedType.Tooltip =
                "Set the type of speed to display:\n" +
                "-absolute: absolute speed (velocity magnitude)\n" +
                "-x-velocity: x component of velocity\n" +
                "-y-velocity: y component of velocity\n" +
                "-x-velocity (absolute): x-velocity without sign\n" +
                "-y-velocity (absolute): y-velocity without sign";

            NewCategory("color");
            Variable = AddEnum("variable", EVariable.SPEED, VariableLabels);
            Color = AddColorTransition("color", new ColorTransition(1550, 0, true, new[] 
            {
                new Color(220, 220, 255), // 0
                new Color(220, 220, 255), // 50
                new Color(220, 220, 255), // 100
                new Color(220, 220, 255), // 150
                new Color(220, 220, 255), // 200
                new Color(220, 220, 255), // 250

                new Color(0, 200, 200), // 300
                new Color(0, 200, 200), // 350
                new Color(0, 200, 200), // 400
                new Color(0, 200, 200), // 450
                new Color(0, 200, 200), // 500
                new Color(0, 200, 200), // 550

                new Color(20, 200, 20), // 600
                new Color(20, 200, 20), // 650
                new Color(20, 200, 20), // 700

                new Color(140, 230, 0), // 750
                new Color(140, 230, 0), // 800
                new Color(140, 230, 0), // 850

                new Color(230, 230, 0), // 900
                new Color(230, 230, 0), // 950
                new Color(230, 230, 0), // 1000
                new Color(230, 230, 0), // 1050
                new Color(230, 230, 0), // 1100
                new Color(230, 230, 0), // 1150

                new Color(230, 100, 0), // 1200
                new Color(230, 100, 0), // 1250
                new Color(230, 100, 0), // 1300
                new Color(230, 100, 0), // 1350

                new Color(180, 0, 0), // 1400
                new Color(180, 0, 0), // 1450
                new Color(180, 0, 0), // 1500

                new Color(80, 0, 0), // 1550
            }));

            Variable.Tooltip =
                "Set the variable to which the color transition should be bound to:\n" +
                "-global time: global time in milliseconds\n" +
                "-speed: player's speed in units per second";

            AddStyleSettings();
            Offset.SetValueAndDefault(new Vector2(7f, -60f));
            RoundingMultiplier.SetValueAndDefault(new RoundingMultiplier("5"));
        }

        public static Speedometer Instance = new Speedometer();

        public override void Update()
        {
            if (Velo.MainPlayer == null)
                return;

            Vector2 velocity = Velo.MainPlayer.actor.Velocity;

            float speed = 0f;

            switch (SpeedType.Value)
            {
                case ESpeedType.ABSOLUTE:
                    speed = velocity.Length();
                    break;
                case ESpeedType.X_VELOCITY:
                    speed = velocity.X;
                    break;
                case ESpeedType.Y_VELOCITY:
                    speed = velocity.Y;
                    break;
                case ESpeedType.X_VELOCITY_ABSOLUTE:
                    speed = Math.Abs(velocity.X);
                    break;
                case ESpeedType.Y_VELOCITY_ABSOLUTE:
                    speed = Math.Abs(velocity.Y);
                    break;
            }

            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (milliseconds - lastUpdate >= UpdateInterval.Value)
            {
                lastUpdate = milliseconds;
                text = RoundingMultiplier.Value.ToStringRounded(speed);
                if (SpeedType.Value == ESpeedType.POSITION)
                    text = 
                        RoundingMultiplier.Value.ToStringRounded(Velo.MainPlayer.actor.Position.X) + " " +
                        RoundingMultiplier.Value.ToStringRounded(Velo.MainPlayer.actor.Position.Y);
                this.speed = speed;
            }
        }

        public override string GetText()
        {
            return text;
        }

        public override Color GetColor()
        {
            if (Variable.Value == EVariable.GLOBAL_TIME)
            {
                return Color.Value.Get();
            }
            else if (Variable.Value == EVariable.SPEED)
            {
                return Color.Value.Get(speed, false);
            }
            return Microsoft.Xna.Framework.Color.White;
        }
    }
}
