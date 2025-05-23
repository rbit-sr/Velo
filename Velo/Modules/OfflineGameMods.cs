﻿using CEngine.Graphics.Camera;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using CEngine.World.Actor;
using System.IO;
using System.Linq;

namespace Velo
{
    public class ReplayHUDContex : MenuContext
    {
        public ButtonW PauseButton;
        public ButtonW Rewind1FrameButton;
        public ButtonW Forward1FrameButton;
        public ButtonW Rewind200msButton;
        public ButtonW Forward200msButton;
        public ButtonW StopButton;
        public ButtonW SpeedButton;
        public LayoutW Layout;

        private readonly float[] speedModes = new[] { 0.1f, 0.25f, 0.5f, 1.0f, 1.5f, 2.0f, 4.0f };
        private int currentSpeedMode = 3;
        private bool speedmodeChanged = false;

        public ReplayHUDContex(ToggleSetting enabled) :
            base(enabled, false)
        {
            PauseButton = new ButtonW("Pause", Fonts.FontMedium);
            Style.ApplyButton(PauseButton);
            PauseButton.OnLeftClick = () =>
            {
                OfflineGameMods.Instance.Freeze.ToggleState();
            };

            Rewind1FrameButton = new ButtonW("<", Fonts.FontMedium);
            Style.ApplyButton(Rewind1FrameButton);
            Rewind1FrameButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    OfflineGameMods.Instance.RewindFrames(1);
            };

            Forward1FrameButton = new ButtonW(">", Fonts.FontMedium);
            Style.ApplyButton(Forward1FrameButton);
            Forward1FrameButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    OfflineGameMods.Instance.StepFrames(1);
            };

            Rewind200msButton = new ButtonW("<<", Fonts.FontMedium);
            Style.ApplyButton(Rewind200msButton);
            Rewind200msButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    OfflineGameMods.Instance.RewindSeconds(0.2f);
            };

            Forward200msButton = new ButtonW(">>", Fonts.FontMedium);
            Style.ApplyButton(Forward200msButton);
            Forward200msButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    OfflineGameMods.Instance.ForwardSeconds(0.2f);
            };

            StopButton = new ButtonW("×", Fonts.FontMedium);
            Style.ApplyButton(StopButton);
            StopButton.OnLeftClick = () =>
            {
                OfflineGameMods.Instance.StopPlayback();
            };

            SpeedButton = new ButtonW("1.00", Fonts.FontMedium);
            Style.ApplyButton(SpeedButton);
            SpeedButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    currentSpeedMode--;
                    if (currentSpeedMode < 0)
                        currentSpeedMode = 0;
                    OfflineGameMods.Instance.TimeScale.Value = speedModes[currentSpeedMode];
                    speedmodeChanged = true;
                }
                if (wevent.Button == WEMouseClick.EButton.RIGHT)
                {
                    currentSpeedMode++;
                    if (currentSpeedMode >= speedModes.Length)
                        currentSpeedMode = speedModes.Length - 1;
                    OfflineGameMods.Instance.TimeScale.Value = speedModes[currentSpeedMode];
                    speedmodeChanged = true;
                }
            };

            Layout = new HLayoutW();
            Layout.AddChild(StopButton, 35f);
            Layout.AddSpace(10f);
            Layout.AddChild(Rewind200msButton, 35f);
            Layout.AddSpace(10f);
            Layout.AddChild(Rewind1FrameButton, 35f);
            Layout.AddSpace(10f);
            Layout.AddChild(PauseButton, 70f);
            Layout.AddSpace(10f);
            Layout.AddChild(Forward1FrameButton, 35f);
            Layout.AddSpace(10f);
            Layout.AddChild(Forward200msButton, 35f);
            Layout.AddSpace(10f);
            Layout.AddChild(SpeedButton, 55f);

            AddElem(Layout, StackW.BOTTOM_LEFT, new Vector2((1920f - Layout.RequestedSize.X) / 2f, -10f), new Vector2(StackW.REQUESTED_SIZE_X, 35f));
        }

        public override void EnterMenu()
        {
            base.EnterMenu();
            speedmodeChanged = false;
        }

        public override void ExitMenu(bool animation = true)
        {
            base.ExitMenu(animation);
            if (speedmodeChanged)
                OfflineGameMods.Instance.TimeScale.Value = 1.0f;
        }

        public override bool Draw()
        {
            if (OfflineGameMods.Instance.Freeze.Value.Enabled)
                PauseButton.BackgroundColor = SettingsUI.Instance.ButtonHoveredColor.Value.Get;
            else
                PauseButton.BackgroundColor = SettingsUI.Instance.ButtonColor.Value.Get;
            SpeedButton.Text = new RoundingMultiplier("0.01").ToStringRounded(OfflineGameMods.Instance.TimeScale.Value);
            return base.Draw();
        }
    }

    // TODO: separate the recording logic from this class
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
        public BoolSetting StoreAIVolumes;

        public BoolSetting EnableReplayHUD;
        public HotkeySetting StopReplay;
        public HotkeySetting Rewind1Second;
        public HotkeySetting SaveRun;
        public HotkeySetting ReplaySavedRun;
        public HotkeySetting VerifySavedRun;
        public HotkeySetting SetGhostSavedRun;
        public BoolSetting LoopReplay;
        public FloatSetting GhostOffsetTime;
        public BoolSetting EnableMultiGhost;
        public BoolSetting GhostDifferentColors;
        public BoolSetting DisableReplayNotifications;

        public IntSetting DeltaTime;
        public BoolSetting FixDeltaTime;
        public ToggleSetting Freeze;
        public HotkeySetting Step1Key;
        public HotkeySetting Step10Key;
        public HotkeySetting JumpBack1Key;
        public HotkeySetting JumpBack10Key;
        public BoolSetting EnableRewind;

        private readonly Savestates savestates;
        public TimeSpan SavestateLoadTime = TimeSpan.Zero;

        private int stepCount = 0;

        private readonly Recording recCurrent = new Recording();
        private Recording recLast;

        private readonly Recorder recorder = new Recorder();
        private readonly Playback playback = new Playback();
        private readonly List<Playback> playbackGhosts = new List<Playback>();

        private readonly SavestateStack rewindList = new SavestateStack();

        private ToggleSetting replayHUDEnabled;
        private ReplayHUDContex replayHUDContext;

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
            StoreAIVolumes = AddBool("store AI volumes", false);

            CurrentCategory.Tooltip =
                "Savestates allow you to save the current state of the player and level and to then restore it at any time by pressing a hotkey. " +
                "Savestates are stored in \"Velo\\savestate\".";
            LoadHaltDuration.Tooltip =
                "Duration in seconds the game will run in slow motion after loading a savestate.";
            StoreAIVolumes.Tooltip =
                "Whether to store AI volumes or not. " +
                "Storing them should be unnecessary in most circumstances.";

            NewCategory("recording and replay");
            EnableReplayHUD = AddBool("enable replay HUD", true);
            StopReplay = AddHotkey("stop replay", 0x97);
            Rewind1Second = AddHotkey("rewind 1 second", 0x97, autoRepeat: true);
            SaveRun = AddHotkey("save last run", 0x97);
            ReplaySavedRun = AddHotkey("replay saved run", 0x97);
            VerifySavedRun = AddHotkey("verify saved run", 0x97);
            SetGhostSavedRun = AddHotkey("set ghost saved run", 0x97);
            LoopReplay = AddBool("loop replay", false);
            GhostOffsetTime = AddFloat("ghost offset", 0f, -2f, 2f);
            EnableMultiGhost = AddBool("enable multighost", false);
            GhostDifferentColors = AddBool("ghosts different colors", true);
            DisableReplayNotifications = AddBool("disable replay notifications", false);

            Rewind1Second.Tooltip =
                "Rewinds replay by 1 second.";
            SaveRun.Tooltip =
                "Saves a recording of the previous run to \"Velo\\saved run\". If you believe your run to be wrongly categorized or invalidated, send the file to a leaderboard moderator.";
            GhostOffsetTime.Tooltip = "ghost offset time in seconds";
            EnableMultiGhost.Tooltip = "Allows you to have multiple ghosts at the same time.";
            GhostDifferentColors.Tooltip = "Color multiple ghosts differently or use the same color.";
            DisableReplayNotifications.Tooltip = "Disables \"replay start/stop\" notifications.";

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
            EnableRewind = AddBool("enable rewind", true);

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
            EnableRewind.Tooltip =
                "Allows you to jump frames backwards while in fixed delta time or stepping mode. " +
                "Decreases performance and increases RAM usage.";

            savestates = new Savestates(this, onSave: null, onLoad: savestate =>
            {
                SavestateLoadTime = Velo.RealTime;
                recorder.Restart();
            });
        }

        public static OfflineGameMods Instance = new OfflineGameMods();

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
                    if (SavestateLoadTime == TimeSpan.Zero)
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

            savestates.Init();

            Velo.AddOnMainPlayerReset(() => Velo.AddOnPreUpdate(OnMainPlayerReset));
            Velo.AddOnLapFinish(time => Velo.AddOnPreUpdate(() => OnLapFinish(time)));
            Velo.AddOnMainPlayerReset(() => Savestate.LoadedVersion = Version.VERSION);

            playback.OnFinish = (recording, type) => Velo.AddOnPreUpdate(() =>
            {
                if (!LoopReplay.Value || type != Playback.EPlaybackType.VIEW_REPLAY)
                    StopPlayback(notification: !DisableReplayNotifications.Value);
                else
                    StartPlayback(recording, Playback.EPlaybackType.VIEW_REPLAY, notification: false);
            });

            replayHUDEnabled = new ToggleSetting(null, "", new Toggle());
            replayHUDContext = new ReplayHUDContex(replayHUDEnabled);
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (!EnableReplayHUD.Value)
                replayHUDEnabled.Disable();

            if (Input.IsPressed(Freeze.Value.Hotkey))
            {
                Freeze.ToggleState();
            }

            if ((Velo.RealTime - SavestateLoadTime) > TimeSpan.FromSeconds(0.1))
                savestates.PreUpdate();

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
                playbackGhosts.Clear();
            }

            if (Rewind1Second.Pressed())
            {
                RewindSeconds(1f);
            }

            if (Velo.Online || !Velo.Ingame)
            {
                Freeze.Disable();
                stepCount = 0;
                return;
            }

            if (StopReplay.Pressed() && IsPlaybackRunning())
            {
                StopPlayback(notification: !DisableReplayNotifications.Value);
            }
            if (SaveRun.Pressed())
            {
                SaveLast();
            }
            if (ReplaySavedRun.Pressed())
            {
                Recording rec = LoadSaved();
                if (rec != null)
                    StartPlayback(rec, Playback.EPlaybackType.VIEW_REPLAY, notification: !DisableReplayNotifications.Value, showTime: true);
            }
            if (VerifySavedRun.Pressed())
            {
                Recording rec = LoadSaved();
                if (rec != null)
                    StartPlayback(rec, Playback.EPlaybackType.VERIFY);
            }
            if (SetGhostSavedRun.Pressed())
            {
                int ghostIndex = !EnableMultiGhost.Value ? 0 : GhostPlaybackCount();
                Ghosts.Instance.GetOrSpawn(ghostIndex, Instance.GhostDifferentColors.Value);
                Recording rec = LoadSaved();
                Ghosts.Instance.WaitForGhost(ghostIndex);
                if (rec != null)
                    StartPlayback(rec, Playback.EPlaybackType.SET_GHOST);
            }

            if (Step1Key.Pressed())
            {
                stepCount = 1;
            }

            if (Step10Key.Pressed())
            {
                stepCount = 10;
            }

            if (JumpBack1Key.Pressed())
            {
                RewindFrames(1);
            }

            if (JumpBack10Key.Pressed())
            {
                if (!IsPlaybackRunning())
                {
                    rewindList.Position = Math.Max(0, rewindList.Position - 10);
                    rewindList.PeekAndLoad(false);
                }
                else
                {
                    if (playback.Type == Playback.EPlaybackType.VIEW_REPLAY || playback.Type == Playback.EPlaybackType.SET_GHOST)
                        playback.Jump((float)(-10 * DeltaTime.Value / (double)TimeSpan.TicksPerSecond));
                    else if (playback.Type == Playback.EPlaybackType.VERIFY)
                        playback.JumpFrames(-10);
                }
                Freeze.Enable();
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
            
                if (delta > 0 && EnableRewind.Value)
                {
                    Savestate savestate = new Savestate();
                    if (StoreAIVolumes.Value)
                        savestate.Save(new List<Savestate.ActorType> { Savestate.ATAIVolume }, Savestate.EListMode.EXCLUDE);
                    else
                        savestate.Save(new List<Savestate.ActorType> { }, Savestate.EListMode.EXCLUDE);
                    rewindList.Write(savestate);
                }
            }

            if (!DtFixed || !EnableRewind.Value)
            {
                rewindList.Clear();
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

        public override void PostRender()
        {
            base.PostRender();

            replayHUDContext.Draw();
        }

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

                recorder.Restart();
                recCurrent.Rules.LapStart(true);
                playback.Restart();
                playbackGhosts.RemoveAll(playback => Ghosts.Instance.Get(playback.GhostIndex) == null);
                playbackGhosts.ForEach(playback => playback.Restart());
                playbackGhosts.ForEach(playback => playback.Jump(GhostOffsetTime.Value, hold: true));
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
                playbackGhosts.ForEach(playback => playback.Jump(GhostOffsetTime.Value, hold: true));
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
                !JumpStrength.IsDefault() ||
                !WallJumpStrength.IsDefault() ||
                !JumpDuration.IsDefault() ||
                !GrappleHookSpeed.IsDefault() ||
                (!GrappleCooldown.IsDefault() && GrappleCooldown.Value != 0.25f) ||
                !SlideCooldown.IsDefault() ||
                !InfiniteJumps.IsDefault() ||
                !FixGrappleGlitches.IsDefault() ||
                !EnableOldMoonwalk.IsDefault();
        }

        public float GetGrappleCooldown()
        {
            if (Velo.Online)
                return GrappleCooldown.DefaultValue;

            if (IsPlaybackRunning())
            {
                return (playback.Recording.Info.PhysicsFlags & RunInfo.FLAG_NEW_GCD) != 0 ? 0.20f : 0.25f;
            }
            return GrappleCooldown.Value;
        }

        public bool GetFixBounceGlitch()
        {
            if (Velo.Online)
                return FixBounceGlitch.DefaultValue;

            if (IsPlaybackRunning())
            {
                return (playback.Recording.Info.PhysicsFlags & RunInfo.FLAG_FIX_BOUNCE_GLITCH) != 0 ? true : false;
            }
            return FixBounceGlitch.Value;
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

        public Recording CurrentRecording => recCurrent;

        public bool IsPlaybackRunning()
        {
            return !playback.Finished;
        }

        public int GhostPlaybackCount()
        {
            return playbackGhosts.Count;
        }

        public bool GhostPlaybackUsedItem()
        {
            return playbackGhosts.Any(playback =>
            {
                return playback.ItemIdPrev != (byte)EItem.NONE && playback.ItemIdPrev != playback.ItemId;
            });
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
                Velo.MainPlayer.Boost = 0f;
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
                playbackGhosts.ForEach(playback => playback.Jump(GhostOffsetTime.Value, hold: true));
                if (type == Playback.EPlaybackType.VIEW_REPLAY && EnableReplayHUD.Value)
                {
                    replayHUDEnabled.Enable();
                }
            }
            else if (type == Playback.EPlaybackType.SET_GHOST)
            {
                bool multiGhost = EnableMultiGhost.Value;
                Playback playbackGhost = !multiGhost && playbackGhosts.Count >= 1 ? playbackGhosts[0] : new Playback();
                playbackGhost.OnFinish = (recording_, __) => Velo.AddOnPreUpdate(() =>
                {
                    playbackGhost.Restart();
                    playbackGhost.Jump(GhostOffsetTime.Value, hold: true);
                });

                playbackGhost.Start(recording, type, !multiGhost ? 0 : playbackGhosts.Count);
                playbackGhost.Jump(GhostOffsetTime.Value, hold: true);
                if (multiGhost || playbackGhosts.Count == 0)
                    playbackGhosts.Add(playbackGhost);
            }

            if (type == Playback.EPlaybackType.VIEW_REPLAY && notification)
                Notifications.Instance.PushNotification("replay start" + (showTime ? "\n" + Util.FormatTime(recording.Info.RunTime, Leaderboard.Instance.TimeFormat.Value) : ""));
        }

        public void StopPlayback(bool notification = true)
        {
            playback.Stop();
            replayHUDEnabled.Disable();
            stepCount = 0;
            Freeze.Disable();

            if (!Velo.Ingame || Velo.ModuleSolo == null)
                return;

            if (playback.Type == Playback.EPlaybackType.VIEW_REPLAY && notification)
                Notifications.Instance.PushNotification("replay stop");
        }

        public void RewindFrames(int frames)
        {
            if (rewindList.Position == 0 && !IsPlaybackRunning())
                return;

            if (!IsPlaybackRunning())
            {
                rewindList.Position = Math.Max(0, rewindList.Position - frames);
                rewindList.PeekAndLoad(false);
            }
            else
            {
                if (playback.Type == Playback.EPlaybackType.VIEW_REPLAY || playback.Type == Playback.EPlaybackType.SET_GHOST)
                    playback.Jump((float)(frames * -DeltaTime.Value / (double)TimeSpan.TicksPerSecond));
                else if (playback.Type == Playback.EPlaybackType.VERIFY)
                    playback.JumpFrames(-frames);
            }
            Freeze.Enable();
            Velo.MainPlayer?.trail?.trails?.ForEach(trail => trail.Clear());
        }

        public void StepFrames(int frames)
        {
            stepCount = frames;
        }

        public void RewindSeconds(float seconds)
        {
            playback.Jump(-seconds);
            Velo.MainPlayer?.trail?.trails?.ForEach(trail => trail.Clear());
        }

        public void ForwardSeconds(float seconds)
        {
            playback.Jump(seconds);
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
                recLast.Write(stream, compress: false);
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
