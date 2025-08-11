using CEngine.Graphics.Camera;
using CEngine.World.Actor;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Velo
{
    public class OfflineGameMods : Module
    {
        public FloatSetting TimeScale;
        public FloatSetting MaxSpeed;
        public VectorSetting Gravity;
        public FloatSetting JumpStrength;
        public FloatSetting WallJumpStrength;
        public FloatSetting JumpDuration;
        public FloatSetting GrappleHookSpeed;
        public FloatSetting GrappleCooldown;
        public FloatSetting SlideCooldown;
        public BoolSetting InfiniteJumps;
        public ToggleSetting FixGrappleGlitches;
        public BoolSetting FixBounceGlitch;
        public BoolSetting EnableOldMoonwalk;
        public BoolSetting BufferGrapples;
        public BoolSetting DisableGhostLaserInteraction;
        public BoolSetting DisableGhostFallTileInteraction;

        public BoolSetting ResetObstacles;
        public BoolSetting ResetGates;
        public BoolSetting ResetFallTiles;
        public BoolSetting ResetLasers;
        public BoolSetting ResetItems;
        public BoolSetting ResetBoost;
        public BoolSetting ResetBoostaCoke;
        public BoolSetting ResetWallBoost;
        public BoolSetting ResetJumpTime;

        public FloatSetting CameraZoom;
        public FloatSetting CameraMaxSpeed;

        public HotkeySetting[] SaveKeys = new HotkeySetting[10];
        public HotkeySetting[] LoadKeys = new HotkeySetting[10];
        public FloatSetting LoadHaltDuration;
        public BoolListSetting StoreList;
        public BoolSetting CompatabilityMode;

        public enum EWatermarkType
        {
            TAS,
            SUNGLASSES
        }

        public IntSetting DeltaTime;
        public BoolSetting FixDeltaTime;
        public ToggleSetting Freeze;
        public HotkeySetting Step1Key;
        public HotkeySetting Step10Key;
        public HotkeySetting JumpBack1Key;
        public HotkeySetting JumpBack10Key;
        public HotkeySetting SaveKey;
        public IntSetting SaveReminderInterval;
        public EnumSetting<EWatermarkType> WatermarkType;

        public enum EPreset
        {
            ULTRAFAST, 
            SUPERFAST, 
            VERYFAST, 
            FASTER, 
            FAST, 
            MEDIUM, 
            SLOW, 
            SLOWER, 
            VERYSLOW
        }

        public enum EPixelFormat
        {
            YUV420P, 
            YUV422P, 
            YUV444P, 
            YUV420P10LE, 
            YUV422P10LE, 
            YUV444P10LE,
            RGB24
        }

        public IntSetting CaptureRate;
        public IntSetting VideoRate;
        public IntSetting Crf;
        public EnumSetting<EPreset> Preset;
        public EnumSetting<EPixelFormat> PixelFormat;

        public TimeSpan SavestateLoadTime = TimeSpan.Zero;

        private int stepCount = 0;

        private OfflineGameMods() : base("Offline Game Mods")
        {
            NewCategory("physics");
            TimeScale = AddFloat("time scale", 1f, 0.1f, 5f);
            MaxSpeed = AddFloat("max speed", 1500f, 100f, 10000f);
            Gravity = AddVector("gravity", new Vector2(0f, 1000f), new Vector2(-5000f, -5000f), new Vector2(5000f, 5000f));
            JumpStrength = AddFloat("jump strength", 2876f, 500f, 10000f);
            WallJumpStrength = AddFloat("wall jump strength", 1500f, 500f, 10000f);
            JumpDuration = AddFloat("jump duration", 0.25f, 0.05f, 2f);
            GrappleHookSpeed = AddFloat("grapple hook speed", 3000f, 100f, 20000f);
            GrappleCooldown = AddFloat("grapple cooldown", 0.2f, 0f, 2f);
            SlideCooldown = AddFloat("slide cooldown", 0.5f, 0f, 2f);
            InfiniteJumps = AddBool("infinite jumps", false);
            FixGrappleGlitches = AddToggle("fix grapple glitches", new Toggle());
            FixBounceGlitch = AddBool("fix bounce glitch", true);
            EnableOldMoonwalk = AddBool("enable old moonwalk", false);
            BufferGrapples = AddBool("buffer grapples", false);
            DisableGhostLaserInteraction = AddBool("disable ghost laser interaction", true);
            DisableGhostFallTileInteraction = AddBool("disable ghost fall tile interaction", true);

            TimeScale.Tooltip =
                "Slow down or speed up the time of the game.";
            MaxSpeed.Tooltip =
                "maximum speed the player can reach";
            Gravity.Tooltip =
                "gravity vector the player is pulled towards";
            InfiniteJumps.Tooltip =
                "Allows you to do as many air jumps as you want without landing.";
            FixGrappleGlitches.Tooltip =
                "Fixes reverse grapples, 90s, flaccid drops.\n" +
                "You can specify a hotkey to hold in order to temporarily disable the fix.";
            FixBounceGlitch.Tooltip =
                "Partially fixes the flat slope glitch where 25 units behind the peak of a slope there is a 1 unit wide spot on the ground that has slope properties. " +
                "This fix only makes it impossible to get bounced up by this spot while landing on it is still possible.";
            EnableOldMoonwalk.Tooltip =
                "Re-enables an old and long fixed glitch that allowed you to initiate a moonwalk " +
                "by hitting ceiling slopes from below.";
            DisableGhostLaserInteraction.Tooltip =
                "Disables any interactions of ghosts with lasers. " +
                "Ghosts can actually block lasers, which is forbidden as per leaderboard rules.";
            DisableGhostFallTileInteraction.Tooltip =
                "Disables any interactions of ghosts with fall tiles (black boxes). " +
                "Ghosts can actually destroy fall tiles, which is forbidden as per leaderboard rules.";

            NewCategory("reset");
            ResetObstacles = AddBool("obstacles", true);
            ResetGates = AddBool("gates", true);
            ResetFallTiles = AddBool("fall tiles", true);
            ResetLasers = AddBool("lasers", true);
            ResetItems = AddBool("items", true);
            ResetBoost = AddBool("boost", true);
            ResetBoostaCoke = AddBool("boosta coke", true);
            ResetWallBoost = AddBool("wall boost", true);
            ResetJumpTime = AddBool("jump time", true);

            CurrentCategory.Tooltip = "Things to reset to their default state when pressing the reset key.";
            ResetObstacles.Tooltip = "Respawn all obstacles (boxes) on pressing reset.";
            ResetGates.Tooltip = "Close all gates and triggers on pressing reset.";
            ResetFallTiles.Tooltip = "Respawn all fall tiles (black boxes) on pressing reset.";
            ResetLasers.Tooltip = "Set all lasers to their default rotation on pressing reset.";
            ResetItems.Tooltip = "Destroy all currently alive items on pressing reset.";
            ResetBoost.Tooltip = "Set boost to 0 on pressing reset.";
            ResetBoostaCoke.Tooltip = "Set boosta coke to 100% on pressing reset.";
            ResetWallBoost.Tooltip =
                "Set the timer for the extra acceleration you receive after getting off of a wall to 0 on pressing reset. " +
                "This timer actually does not get reset on pressing reset, allowing players to get a faster acceleration by quickly jumping right after resetting.";
            ResetJumpTime.Tooltip =
                "Set the time point on when the player last started jumping to 0 on pressing reset. " +
                "This time point actually does not get reset on pressing reset, increasing the ground acceleration ever so slightly for a few milliseconds right after pressing jump and then resetting due to a bug.";

            NewCategory("camera");
            CameraZoom = AddFloat("zoom", -1f, 0.1f, 10f);
            CameraMaxSpeed = AddFloat("max speed", 1250f, 100f, 2000f);

            CameraZoom.Tooltip =
                "zoom (Set to -1 for no change)";
            CameraMaxSpeed.Tooltip =
                "maximum speed the camera can reach";

            NewCategory("savestates");
            // nested categories are not directly supported so this might be a bit ugly
            SettingCategory saveCategory = Add(new SettingCategory(this, "save"));
            saveCategory.Tooltip = "Create a savestate.";
            for (int i = 0; i < 10; i++)
            {
                SaveKeys[i] = saveCategory.Add(new HotkeySetting(this, "save " + (i + 1) + " key", 0x97));
            }
            SettingCategory loadCategory = Add(new SettingCategory(this, "load"));
            loadCategory.Tooltip = "Load a savestate.";
            for (int i = 0; i < 10; i++)
            {
                LoadKeys[i] = loadCategory.Add(new HotkeySetting(this, "load " + (i + 1) + " key", 0x97));
            }
            LoadHaltDuration = AddFloat("load halt duration", 0f, 0f, 2f);
            StoreList = AddBoolList("store list", Savestate.ActorTypes.Select(at => at.Type.Name).ToArray(), Savestate.ActorTypes.Select(at => at.Id != Savestate.ATAIVolume.Id).ToArray());
            CompatabilityMode = AddBool("compatability mode", Environment.OSVersion.Platform != PlatformID.Win32NT);

            CurrentCategory.Tooltip =
                "Savestates allow you to save the current state of the player and level and to then restore it at any time by pressing a hotkey. " +
                "Savestates are stored in \"Velo\\savestate\".";
            LoadHaltDuration.Tooltip =
                "duration in seconds the game will run in slow motion after loading a savestate";
            StoreList.Tooltip =
                "list of actors to store";

            NewCategory("recording and replay");
            AddSubmodule(RecordingAndReplay.Instance);

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
            
            Freeze = AddToggle("freeze", new Toggle());
            Step1Key = AddHotkey("step 1 key", 0x97, autoRepeat: true);
            Step10Key = AddHotkey("step 10 key", 0x97, autoRepeat: true);
            JumpBack1Key = AddHotkey("jump back 1 key", 0x97, autoRepeat: true);
            JumpBack10Key = AddHotkey("jump back 10 key", 0x97, autoRepeat: true);
            SaveKey = AddHotkey("save key", 0x97);
            SaveReminderInterval = AddInt("save reminder interval", 15, 2, 120);
            WatermarkType = AddEnum("watermark type", EWatermarkType.TAS, new[] { "TAS text", "sunglasses" });

            Freeze.Tooltip =
                "When frozen, the game stops doing any physics updates.";
            Step1Key.Tooltip =
                "Steps 1 frame forward. Automatically freezes the game.";
            Step10Key.Tooltip =
                "Steps 10 frames forward. Automatically freezes the game.";
            JumpBack1Key.Tooltip =
                "Jumps 1 frame backwards. \"enable rewind\" needs to be enabled.";
            JumpBack10Key.Tooltip =
                "Jumps 10 frames backwards. \"enable rewind\" needs to be enabled.";
            SaveKey.Tooltip =
                "Saves the TAS-project.";
            SaveReminderInterval.Tooltip =
                "interval in minutes";

            SettingCategory tasEditorCategory = Add(new SettingCategory(this, "TAS editor"));
            CurrentCategory = tasEditorCategory;
            AddSubmodule(TASeditor.Instance);

            NewCategory("video capture");
            CaptureRate = AddInt("capture rate", 60, 10, 300);
            VideoRate = AddInt("video rate", 60, 10, 300);
            Crf = AddInt("crf", 18, 0, 51);
            Preset = AddEnum("preset", EPreset.FAST, Enum.GetNames(typeof(EPreset)).Select(s => s.ToLower()).ToArray());
            PixelFormat = AddEnum("pixel format", EPixelFormat.YUV420P, Enum.GetNames(typeof(EPixelFormat)).Select(s => s.ToLower()).ToArray());

            CaptureRate.Tooltip =
                "Controls the rate in which frames are captured, in frames per second. " +
                "Note that a \"second\" here refers to a second of the recording itself and is independent of your game's or the recording's framerate or how fast the capturing process is running.";
            VideoRate.Tooltip =
                "Controls the resulting video's framerate. " +
                "I recommend just leaving this at 60. If your capture rate is higher than your video rate, you can fix it later by speeding up the video.";
            Crf.Tooltip =
                "constant rate factor for video encoding\n" +
                "Velo uses H.264 to compress video files. " +
                "The crf value determines the output video's quality and file size:\n" +
                "0 is best quality (lossless) with largest file size.\n" +
                "17-18 is considered to be visually lossless.\n" +
                "23 is default.\n" +
                "51 is worst quality with smallest file size.";
            Preset.Tooltip =
                "preset for video encoding\n" +
                "\"ultrafast\" provides the fasted encoding time and largest file size.\n" +
                "\"veryslow\" provides the slowest encoding time and smallest file size.\n" +
                "These do not influence quality.";
            PixelFormat.Tooltip =
                "pixel format\n" +
                "Any format other than yuv420p might not be that well supported by many video players. " +
                "Use yuv420p if you want to share the run over Discord for example. " +
                "Use yuv444p10le or rgb24 if you want the colors to be most accurately represented, depending on what is supported by your video player.";
        }

        public static OfflineGameMods Instance = new OfflineGameMods();

        public IEnumerable<Savestate.ActorType> StoreActorTypes => StoreList.Value.Select((b, i) => b ? Savestate.ActorTypes[i] : null).Where(at => at != null);

        public bool DtFixed =>
            !Velo.Online && (
            FixDeltaTime.Value ||
            stepCount > 0 ||
            Freeze.Value.Enabled ||
            RecordingAndReplay.Instance.DtFixed
            );
       
        public float TimeScaleVal
        {
            get
            {
                if (!Velo.Online && Velo.Ingame)
                {
                    if (
                        SavestateLoadTime == TimeSpan.Zero ||
                        RecordingAndReplay.Instance.RecordingMode == ERecordingMode.TAS
                    )
                        return TimeScale.Value;

                    return TimeScale.Value * SavestateLoadHaltScale;
                }

                return 1f;
            }
        }

        public override void Init()
        {
            base.Init();

            if (Version.Compare(GrappleCooldown.Version, "2.2.8") <= 0)
            {
                GrappleCooldown.Value = GrappleCooldown.DefaultValue;
                GrappleCooldown.Version = Version.VERSION_NAME;
            }
            if (Version.Compare(ResetBoost.Version, "2.2.27b") <= 0)
            {
                ResetBoost.Value = ResetBoost.DefaultValue;
                ResetBoost.Version = Version.VERSION_NAME;
            }
            if (Version.Compare(ResetBoostaCoke.Version, "2.2.27b") <= 0)
            {
                ResetBoostaCoke.Value = ResetBoostaCoke.DefaultValue;
                ResetBoostaCoke.Version = Version.VERSION_NAME;
            }

            Savestates.Instance.Init();
            Savestates.Instance.AddOnLoad((_, savestate) =>
            {
                SavestateLoadTime = Velo.RealTime;
            });

            Velo.AddOnMainPlayerReset(() => Velo.AddOnPreUpdate(OnMainPlayerReset));
            Velo.AddOnLapFinish(time => Velo.AddOnPreUpdate(() => OnLapFinish(time)));
            Velo.AddOnMainPlayerReset(() => Savestate.LoadedVeloVersion = Version.VERSION);

            RecordingAndReplay.Instance.Init();
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Input.IsPressed(Freeze.Value.Hotkey) && !Util.HotkeysDisabled())
            {
                Freeze.ToggleState();
            }

            if (Velo.IngamePrev && !Velo.Ingame)
            {
                RecordingAndReplay.Instance.Close();
            }

            if (Velo.Online || !Velo.Ingame)
            {
                Freeze.Disable();
                stepCount = 0;
                return;
            }

            if (Velo.Ingame && Velo.ModuleSolo != null && Velo.PausedPrev && !Velo.Paused)
            {
                RecordingAndReplay.Instance.Start();
            }

            if ((Velo.RealTime - SavestateLoadTime) > TimeSpan.FromSeconds(0.1))
            {
                bool savedAny = false;
                for (int i = 0; i < 10; i++)
                {
                    string key = "ss" + (i + 1);
                    if (SaveKeys[i].Pressed())
                    {
                        Commands.Wrap(() => Commands.SaveSS(new Filename { Name = key }));
                        savedAny = true;
                    }
                }

                if (!savedAny)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        string key = "ss" + (i + 1);
                        if (LoadKeys[i].Pressed())
                        {
                            Commands.Wrap(() => Commands.LoadSS(new Filename { Name = key }));
                        }
                    }
                }
            }

            RecordingAndReplay.Instance.UpdateHotkeys();

            if (Step1Key.Pressed())
            {
                StepFrames(1);
            }
            if (Step10Key.Pressed())
            {
                StepFrames(10);
            }
            if (JumpBack1Key.Pressed())
            {
                RecordingAndReplay.Instance.OffsetFrames(-1);
            }
            if (JumpBack10Key.Pressed())
            {
                RecordingAndReplay.Instance.OffsetFrames(-10);
            }
            if (SaveKey.Pressed() && RecordingAndReplay.Instance.Recorder is TASRecorder tasRecorder)
            {
                Commands.Wrap(() => Commands.SaveTAS());
            }

            if (InfiniteJumps.Value)
            {
                if (Velo.MainPlayer != null && Velo.MainPlayer.jumpState >= 2)
                    Velo.MainPlayer.jumpState = 1;
            }

            if (DtFixed)
            {
                CEngine.CEngine cengine = CEngine.CEngine.Instance;
                long delta;
                if (Freeze.Value.Enabled && stepCount == 0)
                    delta = 0;
                else
                    delta = DeltaTime.Value;

                long time = cengine.gameTime.TotalGameTime.Ticks;
                if (!(cengine.isHoldingElapsedTime || cengine.isHoldingTotalTime || cengine.pausedLocal))
                    time += delta;
                cengine.gameTime = new GameTime(new TimeSpan(time), new TimeSpan(delta));
            }

            if (
                Velo.ModuleSolo != null &&
                Velo.GameDelta > TimeSpan.Zero &&
                !Velo.PauseMenu
            )
            {
                RecordingAndReplay.Instance.PreUpdate();
            }
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (Velo.Online)
                return;

            if (
                Velo.ModuleSolo != null &&
                Velo.GameDelta > TimeSpan.Zero &&
                !Velo.PauseMenuPrev
            )
            {
                RecordingAndReplay.Instance.PostUpdate();
            }

            if (stepCount > 0)
            {
                stepCount--;
                if (stepCount == 0)
                    Freeze.Enable();
            }
        }

        public override void PostRender()
        {
            base.PostRender();

            RecordingAndReplay.Instance.PostRender();
        }

        public override void PostPresent()
        {
            base.PostPresent();

            RecordingAndReplay.Instance.PostPresent();
        }

        public ISavestateManager SavestateManager =>
            RecordingAndReplay.Instance.RecordingMode == ERecordingMode.TAS ? 
            (ISavestateManager)RecordingAndReplay.Instance.Recorder : 
            Savestates.Instance;

        public float SavestateLoadHaltScale
        {
            get
            {
                if (LoadHaltDuration.Value <= 0)
                    return 1f;
                return (float)Math.Min((Velo.RealTime - SavestateLoadTime).TotalSeconds, LoadHaltDuration.Value) / LoadHaltDuration.Value;
            }
        }

        private void OnMainPlayerReset()
        {
            if (!Velo.Ingame || Velo.Online)
                return;

            if (Velo.ModuleSolo != null)
            {
                ResetStatesAndActors();
            }

            RecordingAndReplay.Instance.OnMainPlayerReset();
        }

        public void OnLapFinish(float time)
        {
            if (Velo.ModuleSolo != null)
            {
                ResetStatesAndActors(obstaclesOnly: true);
            }

            RecordingAndReplay.Instance.OnLapFinish(time);
        }

        public bool IsModded()
        {
            return
                (Performance.Instance.Framelimit.Value != -1 && Performance.Instance.Framelimit.Value < 30) ||
                Performance.Instance.Framelimit.Value > 300 ||
                !TimeScale.IsDefault() ||
                (!CameraZoom.IsDefault() && !Origins.Instance.IsOrigins()) ||
                !CameraMaxSpeed.IsDefault() ||
                !MaxSpeed.IsDefault() ||
                !Gravity.IsDefault() ||
                !JumpStrength.IsDefault() ||
                !WallJumpStrength.IsDefault() ||
                !JumpDuration.IsDefault() ||
                !GrappleHookSpeed.IsDefault() ||
                (!GrappleCooldown.IsDefault() && GrappleCooldown.Value != 0.25f) ||
                !SlideCooldown.IsDefault() ||
                !InfiniteJumps.IsDefault() ||
                !FixGrappleGlitches.IsDefault() ||
                !EnableOldMoonwalk.IsDefault() ||
                !BufferGrapples.IsDefault();
        }

        public float GetGrappleCooldown()
        {
            if (Velo.Online)
                return GrappleCooldown.DefaultValue;

            if (RecordingAndReplay.Instance.IsPlaybackRunning)
            {
                return (RecordingAndReplay.Instance.PlaybackRecording.Info.PhysicsFlags & RunInfo.FLAG_NEW_GCD) != 0 ? 0.20f : 0.25f;
            }
            return GrappleCooldown.Value;
        }

        public bool GetFixBounceGlitch()
        {
            if (Velo.Online)
                return FixBounceGlitch.DefaultValue;

            if (RecordingAndReplay.Instance.IsPlaybackRunning)
            {
                return (RecordingAndReplay.Instance.PlaybackRecording.Info.PhysicsFlags & RunInfo.FLAG_FIX_BOUNCE_GLITCH) != 0;
            }
            return FixBounceGlitch.Value;
        }

        public void UpdateCamera(ICCameraModifier cameraMod)
        {
            if (cameraMod is SoloCameraModifier solo_)
                solo_.camera.Position = Vector2.Zero; // not sure anymore why I added this

            if (!Velo.Online && CameraZoom.Value != CameraZoom.DefaultValue)
            {
                float zoom = CameraZoom.Value;

                if (cameraMod is SoloCameraModifier solo)
                {
                    solo.zoom = zoom;
                    solo.camera.Zoom = zoom * solo.heightRatioTo720;
                }
                else if (cameraMod is MultiplayerCameraModifier multiplayer)
                {
                    multiplayer.zoom = zoom;
                    multiplayer.camera.Zoom = zoom * multiplayer.heightRatioTo720;
                }
            }
        }

        public void UpdateBufferGrapples(Player player)
        {
            if (!BufferGrapples.Value)
                return;
            if (Velo.Online)
                return;
            if (player != Velo.MainPlayer)
                return;
            if (!player.grappling && !player.canGrapple && player.grappleHeld)
                player.grappleHeld = false;
        }

        private void ResetStatesAndActors(bool obstaclesOnly = false)
        {
            if (!Velo.Ingame)
                return;

            List<CActor> actors = CEngine.CEngine.Instance.World.CollisionEngine.actors;
            if (ResetObstacles.Value || Origins.Instance.IsOrigins())
            {
                foreach (CActor actor in actors)
                {
                    if (actor.controller is Obstacle)
                    {
                        actor.controller.Reset();
                    }
                }
            }
            if ((ResetGates.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
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
                    if (actor.controller is Lever)
                    {
                        actor.controller.Reset();
                    }
                }
            }
            if ((ResetFallTiles.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
            {
                foreach (CActor actor in actors)
                {
                    if (actor.controller is FallTile)
                    {
                        actor.controller.Reset();
                    }
                }
            }
            if ((ResetLasers.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
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
            if ((ResetItems.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
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
                        (actor.controller as Fireball).sprite.IsVisible = false;
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
                if (Velo.MainPlayer.itemId == (byte)EItem.TRIGGER)
                    Velo.MainPlayer.itemId = (byte)EItem.NONE;
            }
            if ((ResetBoost.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
            {
                Velo.MainPlayer.Boost = 0f;
            }
            if ((ResetBoostaCoke.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
            {
                Velo.MainPlayer.boostacoke.Value = 0f;
            }
            if ((ResetWallBoost.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
            {
                Velo.MainPlayer.wallJumpBonusTimer = 0f;
            }
            if ((ResetJumpTime.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
            {
                Velo.MainPlayer.jumpTime = TimeSpan.Zero;
            }
        }

        public bool Paused()
        {
            return Freeze.Value.Enabled;
        }

        public void Pause()
        {
            Freeze.Enable();
            stepCount = 0;
        }

        public void Unpause()
        {
            Freeze.Disable();
            stepCount = 0;
        }

        public void StepFrames(int frames)
        {
            stepCount = frames;
        }
    }
}
