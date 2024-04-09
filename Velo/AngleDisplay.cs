﻿using Microsoft.Xna.Framework;
using System;
using System.Windows.Forms;

namespace Velo
{
    public class AngleDisplay : StatDisplayModule
    {
        public enum EVariable
        {
            GLOBAL_TIME, ANGLE
        }

        private static readonly string[] VariableLabels = new[] { "global time", "angle" };

        public EnumSetting<EVariable> Variable;
        public ColorTransitionSetting Color;

        public Vector2 positionPrev = Vector2.Zero; // player center position of previous frame
        public bool wasConnected = false;
        public int connectedFrames = 0;

        private string text = "";
        private double angle = 0.0f;

        private AngleDisplay() : base("Angle Display", true)
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F6));

            NewCategory("color");
            Variable = AddEnum("variable", EVariable.ANGLE, VariableLabels);
            Color = AddColorTransition("color", new ColorTransition(50, 0, false, new[] { new Color(0, 255, 0), new Color(255, 0, 0) }));

            Variable.Tooltip =
                "Set the variable to which the color transition should be bound to:\n" +
                "-global time: global time in milliseconds\n" +
                "-angle: last grapple release angle in degrees";

            AddStyleSettings();
            Offset.SetValueAndDefault(new Vector2(7.0f, -30.0f));
            RoundingMultiplier.SetValueAndDefault(new RoundingMultiplier("0.1"));
        }

        public static AngleDisplay Instance = new AngleDisplay();

        public override void Update()
        {
            if (Velo.MainPlayer == null)
                return;

            bool angleChanged = false;

            if (
                !Velo.MainPlayer.grapple.connected && 
                wasConnected && 
                connectedFrames >= 2 // don't measure single frame grapples which don't affect speed at all
                )
            {
                Vector2 diff = positionPrev - Velo.MainPlayer.grapple.actor.Collision.Center; // rope vector
                angle = Math.Atan2(diff.Y, diff.X) / Math.PI * 180.0;
                connectedFrames = 0;
                angleChanged = true;
            }

            if (angleChanged || text.Length == 0)
            {
                text = Util.ToStringRounded((float)angle, RoundingMultiplier.Value);
            }

            positionPrev = Velo.MainPlayer.actor.Position + new Vector2(12.5f, 22.5f);

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
            if (Variable.Value == EVariable.GLOBAL_TIME)
            {
                return Color.Value.Get();
            }
            else if (Variable.Value == EVariable.ANGLE)
            {
                return Color.Value.Get(angle - 90.0, false);
            }
            return Microsoft.Xna.Framework.Color.White;
        }
    }
}
