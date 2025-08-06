using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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

        public ReplayHUDContex(ToggleSetting enabled, RecordingAndReplayManager recordingAndReplay) :
            base(enabled, false)
        {
            PauseButton = new ButtonW("Pause", Fonts.FontMedium);
            Style.ApplyButton(PauseButton);
            PauseButton.OnLeftClick = () =>
            {
                if (!OfflineGameMods.Instance.Paused())
                    OfflineGameMods.Instance.Pause();
                else
                    OfflineGameMods.Instance.Unpause();
            };

            Rewind1FrameButton = new ButtonW("<", Fonts.FontMedium);
            Style.ApplyButton(Rewind1FrameButton);
            Rewind1FrameButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                {
                    if (recordingAndReplay.PrimarySeekable is Playback playback && playback.Recording is Timeline)
                    {
                        recordingAndReplay.OffsetFrames(-1);
                    }
                    else
                    {
                        recordingAndReplay.OffsetSeconds(-(float)new TimeSpan(OfflineGameMods.Instance.DeltaTime.Value).TotalSeconds);
                    }
                }
            };

            Forward1FrameButton = new ButtonW(">", Fonts.FontMedium);
            Style.ApplyButton(Forward1FrameButton);
            Forward1FrameButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                {
                    if (recordingAndReplay.PrimarySeekable is Playback playback && playback.Recording is Timeline)
                    {
                        recordingAndReplay.OffsetFrames(1);
                    }
                    else
                    {
                        OfflineGameMods.Instance.StepFrames(1);
                    }
                }
            };

            Rewind200msButton = new ButtonW("<<", Fonts.FontMedium);
            Style.ApplyButton(Rewind200msButton);
            Rewind200msButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    recordingAndReplay.OffsetSeconds(-0.2f);
            };

            Forward200msButton = new ButtonW(">>", Fonts.FontMedium);
            Style.ApplyButton(Forward200msButton);
            Forward200msButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    recordingAndReplay.OffsetSeconds(0.2f);
            };

            StopButton = new ButtonW("×", Fonts.FontMedium);
            Style.ApplyButton(StopButton);
            StopButton.OnLeftClick = () =>
            {
                recordingAndReplay.StopPlayback();
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

        public override void EnterMenu(bool animation = true)
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

    public interface ISeekable
    {
        TimeSpan Time { get; }
        int Frame { get; }
        void JumpToTime(TimeSpan time);
        void JumpToFrame(int frame);
    }

    public interface IRecorder : ISeekable
    {
        bool Running { get; }
        bool DtFixed { get; }

        void PreUpdate();
        void PostUpdate();

        void Resume();
        void Stop();
        void Close();

        void MainPlayerReset();
        void LapFinish(float time);
    }

    public enum ERecordingMode
    {
        NONE,
        NORMAL,
        TAS
    }

    public class RecordingAndReplayManager
    {
        private Recording recCurrent;
        private Recording recLast;
        private readonly Recorder normalRecorder = new Recorder();

        private TASProject tasProject;
        private readonly TASRecorder tasRecorder = new TASRecorder();

        private readonly Playback playback = new Playback();
        private readonly List<Playback> playbackGhosts = new List<Playback>();

        public Recording CurrentNormalRecording => recCurrent;
        public TASProject CurrentTASRecording => tasProject;
        private ERecordingMode recordingMode = ERecordingMode.NONE;
        public ERecordingMode RecordingMode => recordingMode;
        public IRecorder Recorder
        {
            get
            {
                switch (recordingMode)
                {
                    case ERecordingMode.NORMAL:
                        return normalRecorder;
                    case ERecordingMode.TAS:
                        return tasRecorder;
                    default:
                        return null;
                }
            }
        }
        private IRecorder RunningRecoder => Recorder != null && Recorder.Running ? Recorder : null;
        private IEnumerable<Playback> Playbacks
        {
            get
            {
                if (playback.Running)
                    return new[] { playback }.Concat(playbackGhosts);
                else
                    return playbackGhosts;
            }
        }
        public ISeekable PrimarySeekable
        {
            get
            {
                IRecorder recorder = RunningRecoder;
                if (recorder != null)
                    return recorder;
                return playback.Running ? playback : null;
            }
        }

        public bool DtFixed =>
            playback.DtFixed ||
            (RunningRecoder != null && RunningRecoder.DtFixed);

        public bool IsPlaybackRunning => playback.Running;
        public bool IsCaptureRunning => IsPlaybackRunning && playback.Type == Playback.EPlaybackType.CAPTURE;
        public IReplayable PlaybackRecording => playback.Recording;
        public int GhostPlaybackCount => playbackGhosts.Count;
        public bool GhostPlaybackUsedItem =>
            playbackGhosts.Any(playback =>
            {
                return playback.ItemIdPrev != (byte)EItem.NONE && playback.ItemIdPrev != playback.ItemId;
            });
        public bool IsOwnPlaybackFromLeaderboard =>
            playback.Recording.Info.PlayerId == Steamworks.SteamUser.GetSteamID().m_SteamID &&
            playback.Recording.Info.Id != -50;

        private ToggleSetting replayHUDEnabled;
        private ReplayHUDContex replayHUDContext;

        public bool DisableResetRestartingPlayback = false;

        public void Init()
        {
            Savestates.Instance.AddOnLoad((_, savestate) =>
            {
                if (normalRecorder.Running)
                {
                    normalRecorder.Resume();
                    recCurrent.Rules.SetCooldown(EViolation.SAVESTATE, 1f);
                }
                SyncGhosts();
            });
            tasRecorder.AddOnLoad((_, __) =>
            {
                SyncGhosts();
            });

            playback.OnFinish = () => Velo.AddOnPreUpdate(() =>
            {
                if (!OfflineGameMods.Instance.LoopReplay.Value || playback.Type != Playback.EPlaybackType.VIEW_REPLAY)
                    StopPlayback(notification: !OfflineGameMods.Instance.DisableReplayNotifications.Value);
                else
                    StartPlayback(playback.Recording, Playback.EPlaybackType.VIEW_REPLAY, ghostIndex: 0, notification: false);
            });

            replayHUDEnabled = new ToggleSetting(null, "", new Toggle());
            replayHUDContext = new ReplayHUDContex(replayHUDEnabled, this);
        }

        public void PreUpdate()
        {
            if (!OfflineGameMods.Instance.EnableReplayHUD.Value)
                replayHUDEnabled.Disable();

            if (IsPlaybackRunning)
            {
                if (OfflineGameMods.Instance.RecordingAndReplay.IsOwnPlaybackFromLeaderboard)
                    recCurrent.Rules.SetCooldown(EViolation.REPLAY, 1f);
                else
                    recCurrent.Rules.SetCooldown(EViolation.REPLAY, 5f);
            }

            RunningRecoder?.PreUpdate();
            Playbacks.ForEach(p => p.PreUpdate());
        }

        public void PostUpdate()
        {
            RunningRecoder?.PostUpdate();
            Playbacks.ForEach(p => p.PostUpdate());
        }

        public void PostRender()
        {
            replayHUDContext.Draw();
        }

        public void PlaybackPostRender()
        {
            playback.PostRender();
        }

        public void PostPresent()
        {
            playback.PostPresent();
        }

        public void Close()
        {
            Recorder?.Close();
            recCurrent = null;
            tasProject = null;

            Playbacks.ForEach(p => p.Stop());
            playbackGhosts.Clear();
            replayHUDEnabled.Disable();
            OfflineGameMods.Instance.Unpause();
        }

        public void Start()
        {
            recordingMode = ERecordingMode.NORMAL;
            recCurrent = new Recording();
            normalRecorder.Set(recCurrent);
            normalRecorder.Resume();
        }

        public void StartPlayback(IReplayable recording, Playback.EPlaybackType type, int ghostIndex, bool notification = true, VideoCapturer videoCapturer = null)
        {
            if (!Velo.Ingame || Velo.ModuleSolo == null)
                return;

            if (type == Playback.EPlaybackType.VIEW_REPLAY || type == Playback.EPlaybackType.VERIFY || type == Playback.EPlaybackType.CAPTURE)
            {
                Recorder?.Stop();

                playback.Start(recording, type, -1, videoCapturer);
                SyncGhosts();
                if (type == Playback.EPlaybackType.VIEW_REPLAY && OfflineGameMods.Instance.EnableReplayHUD.Value)
                {
                    replayHUDEnabled.Enable();
                }
                else
                {
                    replayHUDEnabled.Disable();
                }
                if (OfflineGameMods.Instance.UnfreezeOnReplayStart.Value)
                    OfflineGameMods.Instance.Unpause();
                Velo.ModuleSolo.hud.playerBars[0].playerName.Text = SteamCache.GetPlayerName(recording.Info.PlayerId);
            }
            else if (type == Playback.EPlaybackType.SET_GHOST)
            {
                Playback playbackGhost = playbackGhosts.Count > ghostIndex ? playbackGhosts[ghostIndex] : new Playback();
                playbackGhost.Start(recording, type, ghostIndex);
                if (playbackGhosts.Count <= ghostIndex)
                    playbackGhosts.Add(playbackGhost);
                SyncGhosts();
            }

            if (type == Playback.EPlaybackType.VIEW_REPLAY && notification)
                Notifications.Instance.PushNotification("replay start");
        }

        public void StopPlayback(bool notification = true)
        {
            playback.Stop();
            replayHUDEnabled.Disable();
            OfflineGameMods.Instance.Unpause();

            if (!Velo.Ingame || Velo.ModuleSolo == null)
                return;

            Recorder?.Resume();
            SyncGhosts();

            if (OfflineGameMods.Instance.RecordingAndReplay.IsOwnPlaybackFromLeaderboard)
                recCurrent.Rules.SetCooldown(EViolation.REPLAY, 1f);
            else
                recCurrent.Rules.SetCooldown(EViolation.REPLAY, 5f);

            Velo.ModuleSolo.hud.playerBars[0].playerName.Text = SteamCache.GetPlayerName(Steamworks.SteamUser.GetSteamID().m_SteamID);

            if (playback.Type == Playback.EPlaybackType.VIEW_REPLAY && notification)
                Notifications.Instance.PushNotification("replay stop");
        }

        public void OnMainPlayerReset()
        {
            if (!DisableResetRestartingPlayback)
            {
                RunningRecoder?.MainPlayerReset();
                playbackGhosts.RemoveAll(p => Ghosts.Instance.Get(p.GhostIndex) == null);
                Playbacks.ForEach(p => p.Restart());
                SyncGhosts();
            }
            else
                DisableResetRestartingPlayback = false;
        }

        public void OnLapFinish(float time)
        {
            if (recordingMode == ERecordingMode.NORMAL && !IsPlaybackRunning)
            {
                recLast = recCurrent.Clone();
                recLast.Finish(time, normalRecorder.StatsTracker);

                Leaderboard.Instance.OnRunFinished(recLast);
            }

            RunningRecoder?.LapFinish(time);
            Playbacks.ForEach(p => p.LapFinish(time));
            SyncGhosts();
        }

        public bool SetInputs(Player player)
        {
            return Playbacks.Any(p => p.SetInputs(player));
        }

        public bool SkipUpdateSprite(Player player)
        {
            return Playbacks.Any(p => p.SkipUpdateSprite(player));
        }

        public int CurrentRunStatus()
        {
            if (Velo.ModuleSolo == null || recCurrent == null)
                return 0;
            if (!recCurrent.Rules.Valid)
                return 2;
            if (recCurrent.Rules.CategoryType == ECategoryType.ONE_LAP || recCurrent.Rules.CategoryType == ECategoryType.ONE_LAP_SKIPS)
                return 1;
            return 0;
        }

        public void SyncGhosts()
        {
            ISeekable primary = PrimarySeekable;
            if (primary == null)
                return;
            TimeSpan time = primary.Time;
            playbackGhosts.ForEach(s => s.JumpToTime(time + TimeSpan.FromSeconds(OfflineGameMods.Instance.GhostOffsetTime.Value)));
        }

        public void OffsetFrames(int frames)
        {
            ISeekable primary = PrimarySeekable;
            if (primary == null)
                return;
            primary.JumpToFrame(primary.Frame + frames);
            SyncGhosts();

            OfflineGameMods.Instance.Pause();
            if (frames < 0)
                Velo.MainPlayer?.trail?.trails?.ForEach(trail => trail.Clear());
        }

        public void OffsetSeconds(float seconds)
        {
            ISeekable primary = PrimarySeekable;
            if (primary == null)
                return;
            primary.JumpToTime(primary.Time + TimeSpan.FromSeconds(seconds));
            SyncGhosts();

            OfflineGameMods.Instance.Pause();
            if (seconds < 0f)
                Velo.MainPlayer?.trail?.trails?.ForEach(trail => trail.Clear());
        }

        public void JumpToFrame(int frame)
        {
            ISeekable primary = PrimarySeekable;
            if (primary == null)
                return;
            primary.JumpToFrame(frame);
            SyncGhosts();

            OfflineGameMods.Instance.Pause();
            Velo.MainPlayer?.trail?.trails?.ForEach(trail => trail.Clear());
        }

        public void JumpToSecond(float second)
        {
            ISeekable primary = PrimarySeekable;
            if (primary == null)
                return;
            primary.JumpToTime(TimeSpan.FromSeconds(second));
            SyncGhosts();

            OfflineGameMods.Instance.Pause();
            Velo.MainPlayer?.trail?.trails?.ForEach(trail => trail.Clear());
        }

        public void StepToFrame(int frame)
        {
            ISeekable primary = PrimarySeekable;
            if (primary == null || frame <= primary.Frame)
                return;
            OfflineGameMods.Instance.StepFrames(frame - primary.Frame);
        }

        public bool SaveLastRecording(string name)
        {
            if (recLast == null)
                return false;

            if (!Directory.Exists("Velo\\recordings"))
                Directory.CreateDirectory("Velo\\recordings");

            using (FileStream stream = File.Create($"Velo\\recordings\\{name}.srrec"))
            {
                RunInfo infoPrev = recLast.Info;
                RunInfo infoTemp = infoPrev;
                infoTemp.Id = -50;
                recLast.Info = infoTemp;
                recLast.Write(stream, compress: false);
                recLast.Info = infoPrev;
            }
            return true;
        }

        public void ReplayRecording(IReplayable recording)
        {
            StartPlayback(
                recording, 
                Playback.EPlaybackType.VIEW_REPLAY, 
                ghostIndex: 0,
                notification: !OfflineGameMods.Instance.DisableReplayNotifications.Value
                );
        }

        public void VerifyRecording(IReplayable recording)
        {
            StartPlayback(recording, Playback.EPlaybackType.VERIFY, ghostIndex: 0);
        }

        public int SetGhostRecording(IReplayable recording, int index = -1)
        {
            int ghostIndex;
            if (index == -1)
                ghostIndex = !OfflineGameMods.Instance.EnableMultiGhost.Value ? 0 : GhostPlaybackCount;
            else
                ghostIndex = Math.Min(index, GhostPlaybackCount);
            Ghosts.Instance.GetOrSpawn(ghostIndex, OfflineGameMods.Instance.GhostDifferentColors.Value);
            Ghosts.Instance.WaitForGhost(ghostIndex);
            StartPlayback(recording, Playback.EPlaybackType.SET_GHOST, ghostIndex);
            return ghostIndex;
        }

        public void CaptureRecording(IReplayable recording, CaptureParams captureParams)
        {
            StartPlayback(recording, Playback.EPlaybackType.CAPTURE, ghostIndex: 0, notification: false, new VideoCapturer(captureParams));
        }

        public void CreateNewTAS(string name)
        {
            TASProject tasProject = new TASProject(name);
            SetTASProject(tasProject);
        }

        public TASProject GetTASProject()
        {
            return tasProject;
        }

        public void SetTASProject(TASProject tasProject)
        {
            Recorder?.Stop();
            if (IsPlaybackRunning)
                StopPlayback();

            recordingMode = ERecordingMode.TAS;
            this.tasProject = tasProject;
            tasRecorder.Set(tasProject);
            tasRecorder.Resume();
            SyncGhosts();
        }

        public void CloseTASProject()
        {
            tasRecorder.Close();
            tasProject = null;
            Start();
            if (IsPlaybackRunning)
                StopPlayback(true);
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
