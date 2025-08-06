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

        public BoolSetting Grapple;
        public BoolSetting SlopeSurf;
        public BoolSetting IgnoreFullJump;
        public EnumSetting<EVariable> Variable;
        public ColorTransitionSetting Color;

        private TimeSpan releaseTime = new TimeSpan(0);
        private bool wasConnected = false;

        private TimeSpan timePrev = new TimeSpan(0);
        private Vector2 velPrev = Vector2.Zero;
        private bool wasOnGround = false;

        private bool prevJumpPress = false;
        private bool hasJumped = false;
        private TimeSpan miss;

        private string text = "";

        private JumpHoldingDisplay() : base("Jump Holding Display", true)
        {
            NewCategory("general");
            Grapple = AddBool("grapple", true);
            SlopeSurf = AddBool("slope surf", true);
            IgnoreFullJump = AddBool("ignore full jump", true);

            CurrentCategory.Tooltip =
                "Shows you how efficient your jump holding during spam grappling or slope surfing is. " +
                "This is an important aspect for building speed that many players are not aware of. " +
                "For spam grappling, hold the jump button until your grapple connects for maximum efficiency. " +
                "For slope surfing, hold the jump button as long as you can before the jump runs out and you stick to the slope. " +
                "The jump holding display will show you how much longer you should have held jump than you actually did. " +
                "Try to bring this number down to 0.000!";
            Grapple.Tooltip =
                "Indicate jump holding miss for grappling.";
            SlopeSurf.Tooltip =
                "Indicate jump holding miss for slope surfing.";
            IgnoreFullJump.Tooltip =
                "Ignore jump releases caused by the jump running out.";

            NewCategory("color");
            Variable = AddEnum("variable", EVariable.MISS, VariableLabels);
            Color = AddColorTransition("color", new ColorTransition(100, 0, false, new[] { new Color(0, 255, 0), new Color(255, 0, 0) }));

            Variable.Tooltip =
                "Binds the color transition to a specific variable:\n" +
                "-global time: global time in milliseconds\n" +
                "-miss: time between the last jump release and subsequent grapple or slope connection in milliseconds";

            AddStyleSettings();
            Offset.SetValueAndDefault(new Vector2(7f, -90f));
            RoundingMultiplier.SetValueAndDefault(new RoundingMultiplier("0.001"));
        }

        public static JumpHoldingDisplay Instance = new JumpHoldingDisplay();

        public override void Update()
        {
            if (Velo.MainPlayer == null || Velo.MainPlayer.grapple == null)
                return;

            bool missChanged = false;

            if (Velo.MainPlayer.jumpHeld && !prevJumpPress)
            {
                hasJumped = true;
            }

            if (
                (Velo.MainPlayer.jumpHeld && !Velo.MainPlayer.sliding && !Velo.MainPlayer.onGround && Velo.MainPlayer.repressJumpTime + TimeSpan.FromSeconds(0.25) > Velo.CEngineInst.gameTime.TotalGameTime) ||
                (IgnoreFullJump.Value && Velo.MainPlayer.jumpVelocity.Y <= -359.49f)
                )
            {
                releaseTime = Velo.CEngineInst.gameTime.TotalGameTime;
            }

            if (Velo.MainPlayer.grapple.connected && !wasConnected)
            {
                wasConnected = true;
                if (Grapple.Value && Velo.MainPlayer.actor.Velocity.Y < 0.0f && new Vector2(velPrev.X, Velo.MainPlayer.jumpVelocity.Y).Length() > 750f && hasJumped)
                {
                    miss = Velo.CEngineInst.gameTime.TotalGameTime - releaseTime;
                    missChanged = true;
                }
                hasJumped = false;
            }
            if (!Velo.MainPlayer.grapple.connected)
            {
                wasConnected = false;
            }

            if (Velo.MainPlayer.onGround && !wasOnGround)
            {
                wasOnGround = true;
                if (SlopeSurf.Value && velPrev.Y < -10.0f && Velo.MainPlayer.actor.Velocity.Y < -10.0f && hasJumped)
                {
                    miss = timePrev - releaseTime;
                    missChanged = true;
                }
                hasJumped = false;
            }
            timePrev = Velo.CEngineInst.gameTime.TotalGameTime;
            velPrev = Velo.MainPlayer.actor.Velocity;
            if (!Velo.MainPlayer.onGround)
            {
                wasOnGround = false;
            }

            if (missChanged || text.Length == 0)
            {
                text = RoundingMultiplier.Value.ToStringRounded((float)(miss.Ticks / (double)TimeSpan.TicksPerSecond));
            }

            prevJumpPress = Velo.MainPlayer.jumpHeld;
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
