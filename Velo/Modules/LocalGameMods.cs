﻿using CEngine.Graphics.Camera;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using CEngine.World.Actor;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace Velo
{
    // TODO: separate the recording logic from this class
    public class LocalGameMods : Module
    {
        public FloatSetting TimeScale;
        public FloatSetting MaxSpeed;
        public VectorSetting Gravity;
        public FloatSetting GrappleHookSpeed;
        public FloatSetting GrappleCooldown;
        public FloatSetting SlideCooldown;
        public BoolSetting InfiniteJumps;
        public ToggleSetting FixGrappleGlitches;
        public BoolSetting EnableOldMoonwalk;
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
        public IntSetting LoadHaltDuration;
        public BoolSetting StoreAIVolumes;

        public IntSetting DeltaTime;
        public BoolSetting FixDeltaTime;
        public ToggleSetting Freeze;
        public HotkeySetting Step1Key;
        public HotkeySetting Step10Key;

        private readonly Savestates savestates;
        public TimeSpan SavestateLoadTime = TimeSpan.Zero;

        private int stepCount = 0;

        private readonly Recording recCurrent = new Recording();
        private Recording recLast;

        private readonly Recorder recorder = new Recorder();
        private readonly Playback playback = new Playback();
        private readonly List<Playback> playbackGhosts = new List<Playback>();

        public HotkeySetting Test;
        public static bool test2;
        public static bool test;

        private LocalGameMods() : base("Local Game Mods")
        {
            NewCategory("physics");
            TimeScale = AddFloat("time scale", 1f, 0.1f, 5f);
            MaxSpeed = AddFloat("max speed", 1500f, 100f, 10000f);
            Gravity = AddVector("gravity", new Vector2(0f, 1000f), new Vector2(-5000f, -5000f), new Vector2(5000f, 5000f));
            GrappleHookSpeed = AddFloat("grapple hook speed", 3000f, 100f, 20000f);
            GrappleCooldown = AddFloat("grapple cooldown", 0.25f, 0f, 2f);
            SlideCooldown = AddFloat("slide cooldown", 0.5f, 0f, 2f);
            InfiniteJumps = AddBool("infinite jumps", false);
            FixGrappleGlitches = AddToggle("fix grapple glitches", new Toggle());
            EnableOldMoonwalk = AddBool("enable old moonwalk", false);
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
            EnableOldMoonwalk.Tooltip =
                "Reenables an old and long fixed glitch that allowed you to initiate a moonwalk" +
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
            ResetBoost = AddBool("boost", false);
            ResetBoostaCoke = AddBool("boosta coke", false);
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
                SaveKeys[i] = new HotkeySetting(this, "save " + (i + 1) + " key", 0x97);
                saveCategory.Children.Add(SaveKeys[i]);
            }
            SettingCategory loadCategory = Add(new SettingCategory(this, "load"));
            loadCategory.Tooltip = "Load a savestate.";
            for (int i = 0; i < 10; i++)
            {
                LoadKeys[i] = new HotkeySetting(this, "load " + (i + 1) + " key", 0x97);
                loadCategory.Children.Add(LoadKeys[i]);
            }
            LoadHaltDuration = AddInt("load halt duration", 0, 0, 2000);
            StoreAIVolumes = AddBool("store AI volumes", false);

            CurrentCategory.Tooltip =
                "Savestates allow you to save the current state of the player and level and to then restore it at any time by pressing a hotkey. " +
                "Savestates are stored in \"Velo\\savestate\".";
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
            
            Freeze = AddToggle("freeze", new Toggle());
            Step1Key = AddHotkey("step 1 key", 0x97, autoRepeat: true);
            Step10Key = AddHotkey("step 10 key", 0x97, autoRepeat: true);

            Freeze.Tooltip =
                "When frozen, the game stops doing any physics updates.";
            Step1Key.Tooltip =
                "Steps 1 frame forward. Automatically freezes the game.";
            Step10Key.Tooltip =
                "Steps 10 frames forward. Automatically freezes the game.";

            savestates = new Savestates(this, onSave: null, onLoad: savestate =>
            {
                SavestateLoadTime = Velo.Time;
                Cooldowns.Instance.SavestateLoaded();
                recorder.Restart();
            });

            //NewCategory("test");
            //Test = AddHotkey("test", 0x97);
        }

        public static LocalGameMods Instance = new LocalGameMods();

        public bool DtFixed
        {
            get
            {
                if (Velo.Online)
                    return false;

                return FixDeltaTime.Value || stepCount > 0 || Freeze.Value.Enabled || (playback != null && playback.DtFixed);
            }
        }

        public float TimeScaleVal
        {
            get
            {
                if (!Velo.Online && Velo.Ingame)
                {
                    if (SavestateLoadTime == TimeSpan.Zero || LoadHaltDuration.Value == 0)
                        return TimeScale.Value;

                    float ratio = (float)Math.Min((Velo.Time - SavestateLoadTime).TotalMilliseconds, LoadHaltDuration.Value) / LoadHaltDuration.Value;

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

            playback.OnFinish = (recording, type) => Velo.AddOnPreUpdate(() =>
            {
                if (!Leaderboard.Instance.LoopReplay.Value || type != Playback.EPlaybackType.VIEW_REPLAY)
                    StopPlayback(notification: !Leaderboard.Instance.DisableReplayNotifications.Value);
                else
                    StartPlayback(recording, Playback.EPlaybackType.VIEW_REPLAY, notification: false);
            });
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Input.Pressed(Freeze.Value.Hotkey))
            {
                Freeze.ToggleState();
            }

            savestates.PreUpdate();

            /*if (Input.Held(Test.Value) && Velo.Ingame)
            {
                if (!test2)
                {
                    test = true;
                    Velo.MainPlayer.gameInfo.setOption(2, true);
                    test = false;
                }
                test2 = true;
            }
            if (!Input.Held(Test.Value) && Velo.Ingame)
            {
                if (test2)
                {
                    test = true;
                    Velo.MainPlayer.gameInfo.setOption(2, false);
                    test = false;
                }
                test2 = false;
            }
            if (Velo.MainPlayer != null && Velo.MainPlayer.gameInfo != null)
                Velo.MainPlayer.gameInfo.options[2] = false;*/

            if (Velo.ModuleSolo != null)
            {
                if (Velo.Ingame && Velo.PausedPrev && !Velo.Paused)
                {
                    recorder.Start(recCurrent);
                    recCurrent.Rules.LapStart(true);
                }
            }
            if (Velo.IngamePrev && !Velo.Ingame)
            {
                recorder.Stop();
                playback.Stop();
                playbackGhosts.ForEach(playback => playback.Stop());
            }

            if (Leaderboard.Instance.Rewind1Second.Pressed())
            {
                playback.Jump(-1f);
            }

            if (Velo.Online)
            {
                Freeze.Disable();
                stepCount = 0;
                return;
            }

            if (Step1Key.Pressed())
            {
                stepCount = 1;
            }

            if (Step10Key.Pressed())
            {
                stepCount = 10;
            }

            if (InfiniteJumps.Value)
            {
                if (Velo.MainPlayer != null && Velo.MainPlayer.jump_state >= 2)
                    Velo.MainPlayer.jump_state = 1;
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

            if (Velo.ModuleSolo != null)
            {
                recorder.PreUpdate();
                playback.PreUpdate();
                playbackGhosts.ForEach(playback => playback.PreUpdate());
            }
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (Velo.Online)
                return;

            playbackGhosts.RemoveAll(playback => playback.Finished);

            if (Velo.ModuleSolo != null)
            {
                recorder.PostUpdate();
                recCurrent.Rules.Update();
                playback.PostUpdate();
                playbackGhosts.ForEach(playback => playback.PostUpdate());
            }

            if (stepCount > 0)
            {
                stepCount--;
                if (stepCount == 0)
                    Freeze.Enable();
            }
        }

        private void OnMainPlayerReset()
        {
            if (!Velo.Ingame || Velo.Online)
                return;

            if (Velo.ModuleSolo != null)
            {
                ResetStatesAndActors();

                recorder.Restart();
                recCurrent.Rules.LapStart(true);
                playback.Restart();
                playbackGhosts.ForEach(playback => playback.Restart());
                playbackGhosts.ForEach(playback => playback.Jump(Leaderboard.Instance.GhostOffsetTime.Value, hold: true));
            }
        }

        private void OnLapFinish(float time)
        {
            if (Velo.ModuleSolo != null)
            {
                recLast = recCurrent.Clone();
                recLast.Finish(time);

                Leaderboard.Instance.OnRunFinished(recLast);

                ResetStatesAndActors(obstaclesOnly: true);

                recorder.SetLapStartToBack();
                recCurrent.Rules.LapStart(false);
                playbackGhosts.ForEach(playback => playback.Restart());
                playbackGhosts.ForEach(playback => playback.Jump(Leaderboard.Instance.GhostOffsetTime.Value, hold: true));
            }
        }

        public bool SetInputs(Player player)
        {
            return playback.SetInputs(player) || playbackGhosts.Any(playback => playback.SetInputs(player));
        }

        public bool SkipUpdateSprite(Player player)
        {
            return playback.SkipUpdateSprite(player) || playbackGhosts.Any(playback => playback.SkipUpdateSprite(player));
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
                !GrappleHookSpeed.IsDefault() ||
                !GrappleCooldown.IsDefault() ||
                !SlideCooldown.IsDefault() ||
                !InfiniteJumps.IsDefault() ||
                !FixGrappleGlitches.IsDefault() ||
                !EnableOldMoonwalk.IsDefault();
        }

        public int CurrentRunStatus()
        {
            if (Velo.ModuleSolo == null)
                return 0;
            if (!recCurrent.Rules.Valid)
                return 2;
            if (recCurrent.Rules.CategoryType == ECategoryType.ONE_LAP || recCurrent.Rules.CategoryType == ECategoryType.ONE_LAP_SKIPS)
                return 1;
            return 0;
        }

        public bool IsPlaybackRunning()
        {
            return !playback.Finished;
        }

        public int GhostPlaybackCount()
        {
            return playbackGhosts.Count;
        }

        public bool IsOwnPlaybackFromLeaderboard()
        {
            return 
                playback.Recording.Info.PlayerId == Steamworks.SteamUser.GetSteamID().m_SteamID &&
                playback.Recording.Info.Id != -50;
        }

        public void UpdateCamera(ICCameraModifier cameraMod)
        {
            if (cameraMod is Camera camera1)
                camera1.camera.Position = Vector2.Zero; // not sure anymore why I added this

            if (!Velo.Online && CameraZoom.Value != CameraZoom.DefaultValue)
            {
                float zoom = CameraZoom.Value;

                if (cameraMod is Camera camera)
                {
                    camera.zoom1 = zoom;
                    camera.camera.Zoom = zoom * camera.unknown1;
                }
                else if (cameraMod is CameraMP cameraMP)
                {
                    cameraMP.zoom1 = zoom;
                    cameraMP.camera.Zoom = zoom * cameraMP.unknown1;
                }
            }
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
                        (actor.controller as Fireball).animSpriteDraw.IsVisible = false;
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
            if ((ResetBoost.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
            {
                Velo.MainPlayer.boost = 0f;
            }
            if ((ResetBoostaCoke.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
            {
                Velo.MainPlayer.boostacoke.Value = 0f;
            }
            if ((ResetWallBoost.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
            {
                Velo.MainPlayer.wall_cd = 0f;
            }
            if ((ResetJumpTime.Value && !obstaclesOnly) || Origins.Instance.IsOrigins())
            {
                Velo.MainPlayer.timespan1 = TimeSpan.Zero;
            }
        }

        public void StartPlayback(Recording recording, Playback.EPlaybackType type, bool notification = true, bool showTime = false)
        {
            if (!Velo.Ingame || Velo.ModuleSolo == null)
                return;
            
            if (type == Playback.EPlaybackType.VIEW_REPLAY || type == Playback.EPlaybackType.VERIFY)
            {
                playback.Start(recording, type, -1);
                playbackGhosts.ForEach(playback => playback.Restart());
                playbackGhosts.ForEach(playback => playback.Jump(Leaderboard.Instance.GhostOffsetTime.Value, hold: true));
            }
            else if (type == Playback.EPlaybackType.SET_GHOST)
            {
                bool multiGhost = Leaderboard.Instance.EnableMultiGhost.Value;
                Playback playback = !multiGhost && playbackGhosts.Count >= 1 ? playbackGhosts[0] : new Playback();
                playback.OnFinish = (recording_, __) => Velo.AddOnPreUpdate(() =>
                {
                    playback.Restart();
                    playback.Jump(Leaderboard.Instance.GhostOffsetTime.Value, hold: true);
                });

                playback.Start(recording, type, !multiGhost ? 0 : playbackGhosts.Count);
                playback.Jump(Leaderboard.Instance.GhostOffsetTime.Value, hold: true);
                if (multiGhost || playbackGhosts.Count == 0)
                    playbackGhosts.Add(playback);
            }

            if (type == Playback.EPlaybackType.VIEW_REPLAY && notification)
                Notifications.Instance.PushNotification("replay start" + (showTime ? "\n" + Util.FormatTime(recording.Info.RunTime, Leaderboard.Instance.TimeFormat.Value) : ""));
        }

        public void StopPlayback(bool notification = true)
        {
            playback.Stop();

            if (!Velo.Ingame || Velo.ModuleSolo == null)
                return;

            if (playback.Type == Playback.EPlaybackType.VIEW_REPLAY && notification)
                Notifications.Instance.PushNotification("replay stop");
        }

        public void SaveLast()
        {
            if (recLast == null)
                return;

            if (!Directory.Exists("Velo\\saved run"))
                Directory.CreateDirectory("Velo\\saved run");

            using (FileStream stream = new FileStream("Velo\\saved run\\run.srrec", FileMode.OpenOrCreate, FileAccess.Write))
            {
                int idPrev = recLast.Info.Id;
                recLast.Info.Id = -50;
                recLast.Write(stream);
                recLast.Info.Id = idPrev;
            }
        }

        public Recording LoadSaved()
        {
            if (!File.Exists("Velo\\saved run\\run.srrec"))
                return null;
            Recording rec = new Recording();

            using (FileStream stream = new FileStream("Velo\\saved run\\run.srrec", FileMode.Open, FileAccess.Read))
            {
                rec.Read(stream);
            }
            return rec;
        }

        public void ShowLastAppliedRules()
        {
            if (recLast == null)
                return;

            string reasons =
                recLast.Rules.Valid ? recLast.Rules.CategoryType.Label() + ":\n" : "Invalid:\n";

            foreach (var reason in recLast.Rules.OneLapReasons)
            {
                if (reason != null)
                    reasons += reason + "\n";
            }
            foreach (var reason in recLast.Rules.SkipReasons)
            {
                if (reason != null)
                    reasons += reason + "\n";
            }
            foreach (var reason in recLast.Rules.Violations)
            {
                if (reason != null)
                    reasons += reason + "\n";
            }

            reasons = reasons.Substring(0, reasons.Length - 1);
            Notifications.Instance.ForceNotification(reasons);
        }
    }
}
