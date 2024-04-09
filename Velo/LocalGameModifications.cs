using CEngine.Graphics.Camera;
using CEngine.Util.Draw;
using Microsoft.Xna.Framework;
using System;

namespace Velo
{
    public class LocalGameModifications : Module
    {
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
            NewCategory("physics");
            TimeScale = AddFloat("time scale", 1.0f, 0.0f, 10.0f);
            MaxSpeed = AddFloat("max speed", 1500.0f, 0.0f, 10000.0f);
            Gravity = AddVector("gravity", new Vector2(0.0f, 1000.0f), new Vector2(-10000.0f, -10000.0f), new Vector2(10000.0f, 10000.0f));
            GrappleHookSpeed = AddFloat("grapple hook speed", 3000.0f, 0.0f, 20000.0f);
            GrappleCooldown = AddFloat("grapple cooldown", 0.25f, 0.0f, 5.0f);
            SlideCooldown = AddFloat("slide cooldown", 0.5f, 0.0f, 5.0f);
            FixGrappleGlitches = AddToggle("fix grapple glitches", new Toggle());
            EnableOldMoonwalk = AddBool("enable old moonwalk", false);

            FixGrappleGlitches.Tooltip =
                "Fixes reverse grapples, 90s, flaccid drops.\n" +
                "You can specify a hotkey to hold in order to temporarily disable the fix.";

            EnableOldMoonwalk.Tooltip =
                "Reenables an old and long fixed glitch that allowed you to initiate a moonwalk" +
                "by hitting ceiling slopes from below.";

            NewCategory("camera");
            CameraZoom = AddFloat("zoom", -1.0f, 0.0f, 10.0f);
            CameraMaxSpeed = AddFloat("max speed", 1250.0f, 0.0f, 2000.0f);
        }

        public static LocalGameModifications Instance = new LocalGameModifications();

        public bool IsModded()
        {
            return
                (Performance.Instance.Framelimit.Value != -1 && Performance.Instance.Framelimit.Value < 30) ||
                Performance.Instance.Framelimit.Value > 300 ||
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
            cameraMod.Match<Camera>(camera => camera.camera.Position = Vector2.Zero); // not sure anymore why I added this

            if (!Velo.Online && CameraZoom.Value != CameraZoom.DefaultValue)
            {
                float zoom = CameraZoom.Value;

                if (cameraMod is Camera)
                {
                    Camera camera = cameraMod as Camera;
                    camera.zoom1 = zoom;
                    camera.camera.Zoom = zoom * camera.unknown1;
                }
                else if (cameraMod is CameraMP)
                {
                    CameraMP cameraMP = cameraMod as CameraMP;
                    cameraMP.zoom1 = zoom;
                    cameraMP.camera.Zoom = zoom * cameraMP.unknown1;
                }
            }
        }
    }
}
