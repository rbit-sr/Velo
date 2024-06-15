using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Velo
{
    public class FpsDisplay : StatDisplayModule
    {
        public enum EVariable
        {
            GLOBAL_TIME, FRAMERATE
        }

        private static readonly string[] VariableLabels = new[] { "global time", "framerate" };

        public IntSetting UpdateInterval;
        public IntSetting MeasurementPeriod;

        public EnumSetting<EVariable> Variable;
        public ColorTransitionSetting Color;

        private readonly List<TimeSpan> measurements = new List<TimeSpan>();

        private TimeSpan lastUpdate = TimeSpan.Zero;

        private string text = "-1";
        private int fps = -1;

        private FpsDisplay() : base("FPS Display", false)
        {
            NewCategory("general");
            UpdateInterval = AddInt("update interval", 1000, 0, 3000);
            MeasurementPeriod = AddInt("measurement period", 1000, 0, 3000);

            UpdateInterval.Tooltip =
                "update interval in milliseconds";
            MeasurementPeriod.Tooltip =
                "measurement period in milliseconds";

            NewCategory("color");
            Variable = AddEnum("variable", EVariable.GLOBAL_TIME, VariableLabels);
            Color = AddColorTransition("color", new ColorTransition(Microsoft.Xna.Framework.Color.Red));
            
            Variable.Tooltip =
                "Set the variable to which the color transition should be bound to:\n" +
                "-global time: global time in milliseconds\n" +
                "-framerate: frames per second";

            AddStyleSettings(false, false);
            Scale.SetValueAndDefault(1f);
            Orientation.SetValueAndDefault(EOrientation.TOP_RIGHT);
            Offset.SetValueAndDefault(new Vector2(-16f, 16f));
            Font.SetValueAndDefault("UI\\Font\\GOTHIC.ttf");
            FontSize.SetValueAndDefault(18);
            Rotation.SetValueAndDefault(0f);
        }

        public static FpsDisplay Instance = new FpsDisplay();

        public override void Update()
        {
            TimeSpan now = new TimeSpan(Util.UtcNow);
            measurements.Add(now);
            while (measurements.Count > 2 && now - measurements[0] > TimeSpan.FromMilliseconds(MeasurementPeriod.Value))
            {
                measurements.RemoveAt(0);
            }

            int fps = -1;
            if (now - lastUpdate >= TimeSpan.FromMilliseconds(UpdateInterval.Value))
            {
                if (now - measurements[0] > TimeSpan.Zero)
                    fps = (int)((measurements.Count - 1) / (now - measurements[0]).TotalSeconds + 0.5);
                lastUpdate = now;
                text = fps + "";
                this.fps = fps;
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
            else if (Variable.Value == EVariable.FRAMERATE)
            {
                return Color.Value.Get(fps, false);
            }
            return Microsoft.Xna.Framework.Color.White;
        }
    }
}
