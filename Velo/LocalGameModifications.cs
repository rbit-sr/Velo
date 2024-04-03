using CEngine.Graphics.Camera;
using CEngine.Util.Draw;
using Microsoft.Xna.Framework;
using System;

namespace Velo
{
    public class LocalGameModifications : Module
    {
        public IntSetting Framelimit;
        public FloatSetting TimeScale;
        public FloatSetting CameraZoom;
        public FloatSetting CameraMaxSpeed;
        public FloatSetting MaxSpeed;
        public VectorSetting Gravity;
        public FloatSetting GrappleHookSpeed;
        public FloatSetting GrappleCooldown;
        public FloatSetting SlideCooldown;
        public ToggleSetting FixGrappleGlitches;
        public BoolSetting EnableOldMoonwalk;

        private LocalGameModifications() : base("Local Game Modifications")
        {
            Framelimit = AddInt("framelimit", -1, -1, 2500);
            TimeScale = AddFloat("time scale", 1.0f, 0.0f, 10.0f);
            CameraZoom = AddFloat("camera zoom", -1.0f, 0.0f, 10.0f);
            CameraMaxSpeed = AddFloat("camera max speed", 1250.0f, 0.0f, 2000.0f);
            MaxSpeed = AddFloat("max speed", 1500.0f, 0.0f, 10000.0f);
            Gravity = AddVector("gravity", new Vector2(0.0f, 1000.0f), new Vector2(-10000.0f, -10000.0f), new Vector2(10000.0f, 10000.0f));
            GrappleHookSpeed = AddFloat("grapple hook speed", 3000.0f, 0.0f, 20000.0f);
            GrappleCooldown = AddFloat("grapple cooldown", 0.25f, 0.0f, 5.0f);
            SlideCooldown = AddFloat("slide cooldown", 0.5f, 0.0f, 5.0f);
            FixGrappleGlitches = AddToggle("fix grapple glitches", new Toggle());
            EnableOldMoonwalk = AddBool("enable old moonwalk", false);
        }

        public static LocalGameModifications Instance = new LocalGameModifications();

        public bool IsModded()
        {
            return
                (Framelimit.Value != -1 && Framelimit.Value < 30) ||
                Framelimit.Value > 300 ||
                !TimeScale.IsDefault() ||
                !CameraZoom.IsDefault() ||
                !CameraMaxSpeed.IsDefault() ||
                !MaxSpeed.IsDefault() ||
                !Gravity.IsDefault() ||
                !GrappleHookSpeed.IsDefault() ||
                !GrappleCooldown.IsDefault() ||
                !SlideCooldown.IsDefault() ||
                !FixGrappleGlitches.IsDefault() ||
                !EnableOldMoonwalk.IsDefault();
        }

        public void UpdateCamera(ICCameraModifier cameraMod)
        {
            if (cameraMod is Camera camera)
                camera.camera.Position = Vector2.Zero;

            if (!Velo.Online && CameraZoom.Value != CameraZoom.DefaultValue)
            {
                float zoom = CameraZoom.Value;

                if (cameraMod is Camera camera1)
                {
                    camera1.zoom1 = zoom;
                    camera1.camera.Zoom = zoom * camera1.unknown1;
                }
                else if (cameraMod is CameraMP cameraMP)
                {
                    cameraMP.zoom1 = zoom;
                    cameraMP.camera.Zoom = zoom * cameraMP.unknown1;
                }
            }
        }
    }
}
