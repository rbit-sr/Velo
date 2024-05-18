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
        public VectorSetting SlowBorderOff;
        public VectorSetting FastBorderOff;
        public VectorSetting MaxAcc;

        public Vector2 camPos = Vector2.Zero;
        public Vector2 camVel = Vector2.Zero;

        private BlindrunSimulator() : base("Blindrun Simulator")
        {
            NewCategory("general");
            SlowSpeed = AddVector("slow speed", new Vector2(100f, 200f), new Vector2(0f, 0f), new Vector2(1500f, 1500f));
            FastSpeed = AddVector("fast speed", new Vector2(1250f, 1250f), new Vector2(0f, 0f), new Vector2(1500f, 1500f));
            SlowBorderOff = AddVector("slow border off", new Vector2(-100f, -50f), new Vector2(-500, -300), new Vector2(500f, 300f));
            FastBorderOff = AddVector("fast border off", new Vector2(100f, 50f), new Vector2(-500, -300), new Vector2(500f, 300f));
            MaxAcc = AddVector("max acc", new Vector2(3000f, 6000f), new Vector2(0f, 0f), new Vector2(20000, 20000));

            CurrentCategory.Tooltip =
                "The blindrun simulator has two different speed settings: slow and fast. " +
                "The camera's move speed depends on the player's current position on screen; " +
                "if they're on the screen's center position, the camera will not move at all, " +
                "if they're on the slow border, the camera will move with slow speed, " +
                "and if they're on the fast border, the camera will move with fast speed. " +
                "For any position inbetween, the speed will be linearly interpolated. " +
                "Beyond the fast speed border, the camera will just move with fast speed.";

            SlowBorderOff.Tooltip = 
                "offset from the screen's border for when to move the camera with slow speed " +
                "(From the screen's center, its border is 640 units away horizontally and 360 units vertically.)";
            FastBorderOff.Tooltip =
                "offset from the screen's border for when to move the camera with fast speed " +
                "(From the screen's center, its border is 640 units away horizontally and 360 units vertically.)";
            MaxAcc.Tooltip =
                "camera's maximum acceleration (provide a lower maximum for more jerk-free movement)";
        }

        public static BlindrunSimulator Instance = new BlindrunSimulator();

        public override void Init()
        {
            base.Init();

            Velo.OnMainPlayerReset.Add(() =>
            {
                if (Velo.Ingame)
                {
                    camPos = Velo.MainPlayer.actor.Bounds.Center + new Vector2(0f, -100f);
                    camVel = Vector2.Zero;
                }
            });
        }

        public void Update(ICCameraModifier cameraMod)
        {
            if (!Enabled.Value.Enabled)
                return;

            Camera camera = (Camera)cameraMod;

            if (Enabled.Modified())
            {
                camPos = Velo.MainPlayer.actor.Bounds.Center + new Vector2(0f, -100f);
                camVel = Vector2.Zero;
            }
                
            Vector2 diff = camera.player.actor.Bounds.Center + new Vector2(0f, -100f) - camPos;
            Vector2 slowSpeedBorder = (new Vector2(640f, 360f) + SlowBorderOff.Value) * camera.zoom1;
            Vector2 fastSpeedBorder = (new Vector2(640f, 360f) + FastBorderOff.Value) * camera.zoom1;
            Vector2 targetVel;

            if (Math.Abs(diff.X) < slowSpeedBorder.X)
            {
                targetVel.X = 
                    Math.Abs(diff.X) / slowSpeedBorder.X * SlowSpeed.Value.X;
            }
            else if (Math.Abs(diff.X) < fastSpeedBorder.X)
            {
                targetVel.X = 
                    SlowSpeed.Value.X + 
                    (Math.Abs(diff.X) - slowSpeedBorder.X) / (fastSpeedBorder.X - slowSpeedBorder.X) * (FastSpeed.Value.X - SlowSpeed.Value.X);
            }
            else
            {
                targetVel.X = 
                    FastSpeed.Value.X;
            }

            if (Math.Abs(diff.Y) < slowSpeedBorder.Y)
            {
                targetVel.Y = 
                    Math.Abs(diff.Y) / slowSpeedBorder.Y * SlowSpeed.Value.Y;
            }
            else if (Math.Abs(diff.Y) < fastSpeedBorder.Y)
            {
                targetVel.Y = 
                    SlowSpeed.Value.Y + 
                    (Math.Abs(diff.Y) - slowSpeedBorder.Y) / (fastSpeedBorder.Y - slowSpeedBorder.Y) * (FastSpeed.Value.Y - SlowSpeed.Value.Y);
            }
            else
            {
                targetVel.Y = 
                    FastSpeed.Value.Y;
            }

            if (diff.X < 0f)
                targetVel.X = -targetVel.X;
            if (diff.Y < 0f)
                targetVel.Y = -targetVel.Y;

            Vector2 camVelDiff = targetVel - camVel;
            Vector2 clamp = MaxAcc.Value * (float)CEngine.CEngine.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            camVelDiff = new Vector2(MathHelper.Clamp(camVelDiff.X, -clamp.X, clamp.X), MathHelper.Clamp(camVelDiff.Y, -clamp.Y, clamp.Y));
            camVel += camVelDiff;
            camPos += camVel * (float)CEngine.CEngine.Instance.GameTime.ElapsedGameTime.TotalSeconds;
            camera.position = camPos;
        }
    }
}
