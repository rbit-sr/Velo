using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Velo
{
    public class FpsDisplay : StatDisplayModule
    {
        public IntSetting UpdateInterval;
        public IntSetting MeasurementPeriod;

        public List<TimeSpan> measurements = new List<TimeSpan>();

        public int fps = 0;
        public TimeSpan lastUpdate = TimeSpan.Zero;

        private string text = "";
        private Color color = Microsoft.Xna.Framework.Color.White;

        private FpsDisplay() : base("FPS Display", false)
        {
            Scale.SetValueAndDefault(1.0f);
            Orientation.SetValueAndDefault(EOrientation.TOP_RIGHT);
            Offset.SetValueAndDefault(new Vector2(-16.0f, 16.0f));
            Font.SetValueAndDefault("UI\\Font\\GOTHIC.ttf");
            FontSize.SetValueAndDefault(18);
            Rotation.SetValueAndDefault(0.0f);
            Settings.Remove(RoundingMultiplier);
            RoundingMultiplier = null;
            Settings.Remove(DisablePopup);
            DisablePopup = null;
            Settings.Remove(UseFixedColor);
            UseFixedColor = null;
            Color.SetValueAndDefault(new ColorTransition(Microsoft.Xna.Framework.Color.Red));

            UpdateInterval = AddInt("update interval", 1000, 0, 10000);
            MeasurementPeriod = AddInt("measurement period", 1000, 0, 10000);
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

            if (now - lastUpdate >= TimeSpan.FromMilliseconds(UpdateInterval.Value))
            {
                if (now - measurements[0] > TimeSpan.Zero)
                    fps = (int)((measurements.Count - 1) / (now - measurements[0]).Seconds + 0.5);
                else
                    fps = -1;
                lastUpdate = now;
            }

            text = fps + "";
            color = Color.Value.Get();
        }

        public override string GetText()
        {
            return text;
        }

        public override Color GetColor()
        {
            return color;
        }
    }
}
