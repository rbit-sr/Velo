using CEngine.Graphics.Camera;
using Microsoft.Xna.Framework;
using System;
using System.Windows.Forms;

namespace Velo
{
    public class BlindrunSimulator : ToggleModule
    {
        public VectorSetting SlowSpeed;
        public VectorSetting FastSpeed;
        public VectorSetting SlowSpeedBorderOffset;
        public VectorSetting FastSpeedBorderOffset;
        public VectorSetting MaxAcceleration;

        public Vector2 camPos = Vector2.Zero;
        public Vector2 camVel = Vector2.Zero;

        private BlindrunSimulator() : base("Blindrun Simulator")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F5));

            SlowSpeed = AddVector("slow speed", new Vector2(100, 200), new Vector2(0, 0), new Vector2(2000, 2000));
            FastSpeed = AddVector("fast speed", new Vector2(1250, 1250), new Vector2(0, 0), new Vector2(2000, 2000));
            SlowSpeedBorderOffset = AddVector("slow border off", new Vector2(-100, -50), new Vector2(-500, -500), new Vector2(500, 500));
            FastSpeedBorderOffset = AddVector("fast border off", new Vector2(100, 50), new Vector2(-500, -500), new Vector2(500, 500));
            MaxAcceleration = AddVector("max acc", new Vector2(3000, 6000), new Vector2(0, 0), new Vector2(20000, 20000));
        }

        public static BlindrunSimulator Instance = new BlindrunSimulator();

        public void Update(ICCameraModifier cameraMod)
        {
            if (!Enabled.Value.Enabled)
                return;

            Camera camera = (Camera)cameraMod;
            
            if (Enabled.Modified())
            {
                camPos = camera.player.actor.Position;
                camVel = Vector2.Zero;
            }

            Vector2 diff = camera.player.actor.Bounds.Center - camPos;
            Vector2 slowSpeedBorder = (new Vector2(640.0f, 360.0f) + SlowSpeedBorderOffset.Value) * camera.zoom1;
            Vector2 fastSpeedBorder = (new Vector2(640.0f, 360.0f) + FastSpeedBorderOffset.Value) * camera.zoom1;
            Vector2 targetVel;

            if (Math.Abs(diff.X) < slowSpeedBorder.X)
                targetVel.X = Math.Abs(diff.X) / slowSpeedBorder.X * SlowSpeed.Value.X;
            else if (Math.Abs(diff.X) < fastSpeedBorder.X)
                targetVel.X = SlowSpeed.Value.X + (Math.Abs(diff.X) - slowSpeedBorder.X) / (fastSpeedBorder.X - slowSpeedBorder.X) * (FastSpeed.Value.X - SlowSpeed.Value.X);
            else
                targetVel.X = FastSpeed.Value.X;

            if (Math.Abs(diff.Y) < slowSpeedBorder.Y)
                targetVel.Y = Math.Abs(diff.Y) / slowSpeedBorder.Y * SlowSpeed.Value.Y;
            else if (Math.Abs(diff.Y) < fastSpeedBorder.Y)
                targetVel.Y = SlowSpeed.Value.Y + (Math.Abs(diff.Y) - slowSpeedBorder.Y) / (fastSpeedBorder.Y - slowSpeedBorder.Y) * (FastSpeed.Value.Y - SlowSpeed.Value.Y);
            else
                targetVel.Y = FastSpeed.Value.Y;

            if (diff.X < 0.0f)
                targetVel.X = -targetVel.X;
            if (diff.Y < 0.0f)
                targetVel.Y = -targetVel.Y;

            Vector2 camVelDiff = targetVel - camVel;
            Vector2 clamp = MaxAcceleration.Value * (float)CEngine.CEngine.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            camVelDiff = new Vector2(MathHelper.Clamp(camVelDiff.X, -clamp.X, clamp.X), MathHelper.Clamp(camVelDiff.Y, -clamp.Y, clamp.Y));
            camVel += camVelDiff;
            camPos += camVel * (float)CEngine.CEngine.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            camera.position = camPos;
        }
    }
}
