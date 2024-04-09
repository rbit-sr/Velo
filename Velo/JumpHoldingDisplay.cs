using System;
using Microsoft.Xna.Framework;

namespace Velo
{
    public class JumpHoldingDisplay : StatDisplayModule
    {
        public enum EVariable
        {
            GLOBAL_TIME, MISS
        }

        private static readonly string[] VariableLabels = new[] { "global time", "miss" };

        public EnumSetting<EVariable> Variable;
        public ColorTransitionSetting Color;

        private TimeSpan releaseTime = new TimeSpan(0);
        private bool wasConnected = false;
        private TimeSpan miss;

        private string text = "";

        private JumpHoldingDisplay() : base("Jump Holding Display", true)
        {
            NewCategory("color");
            Variable = AddEnum("variable", EVariable.MISS, VariableLabels);
            Color = AddColorTransition("color", new ColorTransition(100, 0, false, new[] { new Color(0, 255, 0), new Color(255, 0, 0) }));

            Variable.Tooltip =
                "Set the variable to which the color transition should be bound to:\n" +
                "-global time: global time in milliseconds\n" +
                "-miss: time between the last jump release and subsequent grapple connection in milliseconds";

            AddStyleSettings();
            Offset.SetValueAndDefault(new Vector2(7.0f, -90.0f));
            RoundingMultiplier.SetValueAndDefault(new RoundingMultiplier("0.001"));
        }

        public static JumpHoldingDisplay Instance = new JumpHoldingDisplay();

        public override void Update()
        {
            if (Velo.MainPlayer == null)
                return;

            bool missChanged = false;

            if (Velo.MainPlayer.jumpPressed && Velo.MainPlayer.timespan2 + TimeSpan.FromSeconds(0.25) > Velo.MainPlayer.game_time.TotalGameTime)
            {
                releaseTime = Velo.MainPlayer.game_time.TotalGameTime;
            }

            if (Velo.MainPlayer.grapple.connected && !wasConnected)
            {
                wasConnected = true;
                miss = Velo.MainPlayer.game_time.TotalGameTime - releaseTime;
                missChanged = true;
            }
            if (!Velo.MainPlayer.grapple.connected)
            {
                wasConnected = false;
            }

            if (missChanged || text.Length == 0)
            {
                text = Util.ToStringRounded((float)(miss.Ticks / (double)TimeSpan.TicksPerSecond), RoundingMultiplier.Value);
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
            else if (Variable.Value == EVariable.MISS)
            {
                return Color.Value.Get(miss.TotalMilliseconds, false);
            }
            return Microsoft.Xna.Framework.Color.White;
        }
    }
}
