using CEngine.Graphics.Camera;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using CEngine.World.Actor;

namespace Velo
{
    public class LocalGameMods : Module
    {
        public FloatSetting TimeScale;
        public FloatSetting MaxSpeed;
        public VectorSetting Gravity;
        public FloatSetting GrappleHookSpeed;
        public FloatSetting GrappleCooldown;
        public FloatSetting SlideCooldown;
        public ToggleSetting FixGrappleGlitches;
        public BoolSetting EnableOldMoonwalk;
        public BoolSetting DisableGhostLaserInteraction;

        public BoolSetting ResetObstacles;
        public BoolSetting ResetGates;
        public BoolSetting ResetFallTiles;
        public BoolSetting ResetLasers;
        public BoolSetting ResetItems;
        public BoolSetting ResetBoost;
        public BoolSetting ResetBoostaCoke;
        public BoolSetting ResetWallBoost;

        public FloatSetting CameraZoom;
        public FloatSetting CameraMaxSpeed;

        public HotkeySetting SaveKey;
        public HotkeySetting LoadKey;
        public IntSetting LoadHaltDuration;
        public BoolSetting StoreAIVolumes;

        public IntSetting DeltaTime;
        public BoolSetting FixDeltaTime;
        public HotkeySetting FreezeKey;
        public HotkeySetting Step1Key;
        public HotkeySetting Step10Key;

        private readonly Savestate savestate;
        public long savestateLoadTime = 0;

        private bool frozen = false;
        private int stepCount = 0;

        public Recording recCurrent = new Recording();
        public Recording recLast = new Recording();

        private readonly Recorder recorder = new Recorder();
        private readonly Playback playback = new Playback();

        private LocalGameMods() : base("Local Game Mods")
        {
            NewCategory("physics");
            TimeScale = AddFloat("time scale", 1f, 0.1f, 5f);
            MaxSpeed = AddFloat("max speed", 1500f, 100f, 10000f);
            Gravity = AddVector("gravity", new Vector2(0f, 1000f), new Vector2(-5000f, -5000f), new Vector2(5000f, 5000f));
            GrappleHookSpeed = AddFloat("grapple hook speed", 3000f, 100f, 20000f);
            GrappleCooldown = AddFloat("grapple cooldown", 0.25f, 0f, 2f);
            SlideCooldown = AddFloat("slide cooldown", 0.5f, 0f, 2f);
            FixGrappleGlitches = AddToggle("fix grapple glitches", new Toggle());
            EnableOldMoonwalk = AddBool("enable old moonwalk", false);
            DisableGhostLaserInteraction = AddBool("disable ghost laser interaction", true);

            FixGrappleGlitches.Tooltip =
                "Fixes reverse grapples, 90s, flaccid drops.\n" +
                "You can specify a hotkey to hold in order to temporarily disable the fix.";

            EnableOldMoonwalk.Tooltip =
                "Reenables an old and long fixed glitch that allowed you to initiate a moonwalk" +
                "by hitting ceiling slopes from below.";

            NewCategory("reset");
            ResetObstacles = AddBool("obstacles", false);
            ResetGates = AddBool("gates", false);
            ResetFallTiles = AddBool("fall tiles", false);
            ResetLasers = AddBool("lasers", false);
            ResetItems = AddBool("items", false);
            ResetBoost = AddBool("boost", false);
            ResetBoostaCoke = AddBool("boosta coke", false);
            ResetWallBoost = AddBool("wall boost", true);

            CurrentCategory.Tooltip = "Things to reset to their default state when pressing the reset key.";
            ResetObstacles.Tooltip = "Respawn all obstacles (boxes) on pressing reset.";
            ResetGates.Tooltip = "Close all gates and triggers on pressing reset.";
            ResetFallTiles.Tooltip = "Respawn all fall tiles (black boxes) on pressing reset.";
            ResetLasers.Tooltip = "Set all lasers to their default rotation on pressing reset.";
            ResetItems.Tooltip = "Destroy all currently alive items on pressing reset.";
            ResetBoost.Tooltip = "Set boost to 0 on pressing reset.";
            ResetBoostaCoke.Tooltip = "Set boosta coke to 100% on pressing reset.";
            ResetWallBoost.Tooltip = "Set the timer for the extra acceleration you receive after getting off of a wall to 0 on pressing reset.";

            NewCategory("camera");
            CameraZoom = AddFloat("zoom", -1f, 0.1f, 10f);
            CameraMaxSpeed = AddFloat("max speed", 1250f, 100f, 2000f);

            CameraZoom.Tooltip =
                "zoom (Set to -1 for no change)";

            NewCategory("savestates");
            SaveKey = AddHotkey("save key", 0x97);
            LoadKey = AddHotkey("load key", 0x97);
            LoadHaltDuration = AddInt("load halt duration", 0, 0, 2000);
            StoreAIVolumes = AddBool("store AI volumes", false);

            LoadHaltDuration.Tooltip =
                "Duration in milliseconds the game will run in slow motion after loading a savestate.";
            StoreAIVolumes.Tooltip =
                "Whether to store AI volumes or not. " +
                "Storing them should be unnecessary in most circumstances.";

            NewCategory("TAS");
            FixDeltaTime = AddBool("fix delta time", false);
            DeltaTime = AddInt("delta time", 33333, 0, 666666);

            FixDeltaTime.Tooltip =
                "Fixes the delta time to a constant value, making the game's physics 100% deterministic " +
                "(excluding randomized events like item pickups and bot behavior).";
            DeltaTime.Tooltip =
                "The delta time is the measured time difference between subsequent frames. " +
                "This value is used for physics calculations to determine for example how far " +
                "to move the player on the current frame. " +
                "As the delta times depends on measurements, " +
                "they are highly inconsistent and lead to indeterminism in the game's physics.";
            
            FreezeKey = AddHotkey("freeze key", 0x97);
            Step1Key = AddHotkey("step 1 key", 0x97);
            Step10Key = AddHotkey("step 10 key", 0x97);

            FreezeKey.Tooltip =
                "Toggles the game between a frozen and unfrozen state. " +
                "When frozen, the game stops doing any physics updates.";
            Step1Key.Tooltip =
                "Steps 1 frame forward. Automatically freezes the game.";
            Step10Key.Tooltip =
                "Steps 10 frames forward. Automatically freezes the game.";

            savestate = new Savestate();
        }

        public static LocalGameMods Instance = new LocalGameMods();

        public bool DtFixed
        {
            get
            {
                if (Velo.Online)
                    return false;

                return FixDeltaTime.Value || stepCount > 0 || frozen || (playback != null && playback.DtFixed);
            }
        }

        public float TimeScaleVal
        {
            get
            {
                if (!Velo.Online && Velo.Ingame)
                {
                    if (savestateLoadTime == 0 || LoadHaltDuration.Value == 0)
                        return TimeScale.Value;

                    long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    float ratio = (float)Math.Min(milliseconds - savestateLoadTime, LoadHaltDuration.Value) / LoadHaltDuration.Value;

                    return TimeScale.Value * ratio;
                }

                return 1f;
            }
        }

        public override void Init()
        {
            base.Init();

            Velo.OnMainPlayerReset.Add(OnMainPlayerReset);
            Velo.OnLapFinish.Add(OnLapFinish);
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Keyboard.Pressed[SaveKey.Value])
            {
                if (StoreAIVolumes.Value)
                    savestate.Save(new List<Savestate.ActorType> { Savestate.AIVolume }, Savestate.EListMode.EXCLUDE);
                else
                    savestate.Save(new List<Savestate.ActorType> { }, Savestate.EListMode.EXCLUDE);
            }

            if (Keyboard.Pressed[LoadKey.Value])
            {
                if (savestate.Load(DtFixed))
                    savestateLoadTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }

            if (!Velo.IngamePrev && Velo.Ingame)
            {
                recorder.Start(recCurrent);
            }
            if (Velo.IngamePrev && !Velo.Ingame)
            {
                recorder.Stop();
                playback.Stop();
            }
            if (Velo.Online)
            {
                frozen = false;
                stepCount = 0;
                return;
            }

            if (Keyboard.Pressed[FreezeKey.Value])
            {
                if (!frozen)
                    frozen = true;
                else
                    frozen = false;
            }

            if (Keyboard.Pressed[Step1Key.Value])
            {
                stepCount = 1;
            }

            if (Keyboard.Pressed[Step10Key.Value])
            {
                stepCount = 10;
            }

            /*
            if (Test.Modified())
            {
                //Velo.MainPlayer.gameInfo.setOption(2, Test.Value.Enabled);
                //Velo.MainPlayer.gameInfo.options[2] = false;
            }*/

            if (DtFixed)
            {
                CEngine.CEngine cengine = CEngine.CEngine.Instance;
                long delta;
                if (frozen && stepCount == 0)
                    delta = 0;
                else
                    delta = DeltaTime.Value;

                long time = cengine.gameTime.TotalGameTime.Ticks;
                if (!(cengine.isHoldingElapsedTime || cengine.isHoldingTotalTime || cengine.pausedLocal))
                    time += delta;
                cengine.gameTime = new GameTime(new TimeSpan(time), new TimeSpan(delta));
            }

            recorder.PreUpdate();
            recCurrent.Rules.Update();
            playback.PreUpdate();
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (Velo.Online)
                return;

            recorder.PostUpdate();
            playback.PostUpdate();

            if (stepCount > 0)
            {
                stepCount--;
                if (stepCount == 0)
                    frozen = true;
            }
        }

        private void OnMainPlayerReset()
        {
            if (!Velo.Ingame || Velo.Online)
                return;
            ResetStatesAndActors();
            recorder.Restart();
            recCurrent.Rules.LapStart(true);
            playback.Restart();
        }

        private void OnLapFinish()
        {
            recLast = recCurrent.Clone();
            recLast.Finish();

            Leaderboard.Instance.OnRunFinished(recLast);

            recorder.SetLapStartToBack();
            recCurrent.Rules.LapStart(false);
        }

        public bool SetInputs()
        {
            return playback.SetInputs();
        }

        public bool SkipIfGhost()
        {
            return playback.SkipIfGhost();
        }

        public bool SkipUpdateSprite(Player player)
        {
            return playback.SkipUpdateSprite(player);
        }

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

        public bool IsPlaybackRunning()
        {
            return !playback.Finished && playback.Type != Playback.EPlaybackType.SET_GHOST;
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

        private void ResetStatesAndActors()
        {
            if (!Velo.Ingame)
                return;

            List<CActor> actors = CEngine.CEngine.Instance.World.CollisionEngine.actors;
            if (ResetObstacles.Value)
            {
                foreach (CActor actor in actors)
                {
                    if (actor.controller is Obstacle)
                    {
                        actor.controller.Reset();
                    }
                }
            }
            if (ResetGates.Value)
            {
                foreach (CActor actor in actors)
                {
                    if (actor.controller is SwitchBlock)
                    {
                        actor.controller.Reset();
                    }
                    if (actor.controller is Trigger)
                    {
                        (actor.controller as Trigger).open = false;
                        (actor.controller as Trigger).timer = 0f;
                    }
                }
            }
            if (ResetFallTiles.Value)
            {
                foreach (CActor actor in actors)
                {
                    if (actor.controller is FallTile)
                    {
                        actor.controller.Reset();
                    }
                }
            }
            if (ResetLasers.Value)
            {
                foreach (CActor actor in actors)
                {
                    if (actor.controller is Laser)
                    {
                        actor.controller.Reset();
                        actor.controller.Update(CEngine.CEngine.Instance.gameTime);
                    }
                }
            }
            if (ResetItems.Value)
            {
                foreach (CActor actor in actors)
                {
                    if (actor.controller is DroppedObstacle)
                    {
                        (actor.controller as DroppedObstacle).Break();
                    }
                    if (actor.controller is DroppedBomb)
                    {
                        (actor.controller as DroppedBomb).Break();
                    }
                    if (actor.controller is Fireball)
                    {
                        actor.IsCollisionActive = false;
                    }
                    if (actor.controller is Rocket)
                    {
                        (actor.controller as Rocket).Break();
                    }
                    if (actor.controller is Shockwave)
                    {
                        (actor.controller as Shockwave).Break();
                    }
                }
                if (Velo.MainPlayer.item_id == (byte)EItem.TRIGGER)
                    Velo.MainPlayer.item_id = (byte)EItem.NONE;
            }
            if (ResetBoost.Value)
            {
                Velo.MainPlayer.boost = 0f;
            }
            if (ResetBoostaCoke.Value)
            {
                Velo.MainPlayer.boostacoke.Value = 0f;
            }
            if (ResetWallBoost.Value)
            {
                Velo.MainPlayer.wall_cd = 0f;
            }
        }

        public void StartPlayback(Recording recording, Playback.EPlaybackType type)
        {
            recorder.Stop();
            playback.Start(recording, type);
            if (type == Playback.EPlaybackType.VIEW_REPLAY)
                Notifications.Instance.PushNotification("playback start");
        }

        public void StopPlayback()
        {
            playback.Stop();
            recorder.Start(recCurrent);
            if (playback.Type == Playback.EPlaybackType.VIEW_REPLAY)
                Notifications.Instance.PushNotification("playback stop");
        }
    }
}
