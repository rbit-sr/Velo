using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using static Velo.Frame;

namespace Velo
{
    // Implemented by `Recording` and `TASRecording`
    // `Playback` will accept any `IRecording`
    public interface IReplayable
    {
        int LapStart { get; }
        RunInfo Info { get; }
        bool LoadSavestate(int i, bool setGlobalTime, int ghostIndex = -1, Savestate.ResultFlags resultFlags = null);
        Frame this[int i] { get; }
        int Count { get; }
    }

    public class Playback : ISeekable
    {
        public enum EPlaybackType
        {
            SET_GHOST, VIEW_REPLAY, VERIFY, CAPTURE
        }

        private static bool SetGlobalTime(EPlaybackType type)
        {
            return type == EPlaybackType.VERIFY || type == EPlaybackType.CAPTURE;
        }

        private IReplayable recording;
        private Player player;
        private int i = 0;
        private TimeSpan time = TimeSpan.Zero;
        private TimeSpan holdingTime = TimeSpan.Zero;

        private double discrepancy = 0d;
        private TimeSpan lastNotificationUpdate = TimeSpan.Zero;

        public EPlaybackType Type;
        public int GhostIndex;

        public byte ItemId;
        public byte ItemIdPrev;

        public Action OnFinish;

        private readonly Savestate savestatePreVerify = new Savestate();

        private bool running = false;
        public bool Running => running;

        public TimeSpan Time => time - recording[recording.LapStart].DeltaSum;
        public int Frame => i - recording.LapStart;

        public VideoCapturer videoCapturer;
        private TimeSpan captureTime;

        public Playback()
        {
            
        }

        public IReplayable Recording { get => recording; }

        public void Start(IReplayable recording, EPlaybackType type, int ghostIndex, VideoCapturer videoCapturer = null)
        {
            if (SetGlobalTime(type) && !(Running && SetGlobalTime(Type)))
                savestatePreVerify.Save(new List<Savestate.ActorType>(), Savestate.EListMode.EXCLUDE);
            else if (Type == EPlaybackType.VERIFY && Running && !SetGlobalTime(type))
                savestatePreVerify.Load(true);

            this.recording = recording;
            Type = type;
            GhostIndex = ghostIndex;
            this.videoCapturer = videoCapturer;
            captureTime = TimeSpan.Zero;
            Restart();
        }

        public void Restart()
        {
            if (recording == null || !Velo.Ingame)
                return;

            switch (Type)
            {
                case EPlaybackType.VIEW_REPLAY:
                case EPlaybackType.VERIFY:
                case EPlaybackType.CAPTURE:
                    player = Velo.MainPlayer;
                    break;
                case EPlaybackType.SET_GHOST:
                    player = Ghosts.Instance.Get(GhostIndex);
                    break;
            }

            if (player == null)
                return;

            i = 0;
            time = recording[0].DeltaSum;

            RestoreState();
            discrepancy = 0d;

            ItemId = (byte)EItem.NONE;
            ItemIdPrev = (byte)EItem.NONE;

            running = true;
        }

        public void Stop()
        {
            running = false;
            if (SetGlobalTime(Type))
                savestatePreVerify.Load(true);
        }

        public void Finish()
        {
            Stop();
            OnFinish?.Invoke();
        }

        private void RestoreState()
        {
            Savestate.ResultFlags flags = new Savestate.ResultFlags();
            bool savestateLoaded = false;
            if ((recording.Info.InfoFlags & RunInfo.FLAG_NO_SAVESTATE) != 0)
                goto fallback;
            if (!recording.LoadSavestate(i, setGlobalTime: SetGlobalTime(Type), GhostIndex, flags))
                goto fallback;

            if (recording.Info.Id >= 0)
                Savestate.LoadedVeloVersion = Version.VERSION; // assume runs from the leaderboard are fine
            savestateLoaded = true;

        fallback:
            TimeSpan nowRel = time - recording[i].DeltaSum;
            if (!savestateLoaded || nowRel != TimeSpan.Zero)
                ApplyCurrentFrame();
            recording[i].ApplyInputs(player);

            if (
                (!savestateLoaded || flags.progressOnly) && // meaning that the savestate did not load the camera
                (Type == EPlaybackType.VIEW_REPLAY || Type == EPlaybackType.VERIFY || Type == EPlaybackType.CAPTURE)
            )
            {
                Velo.ModuleSolo.camera1.worldFocusPoint = player.actor.Bounds.Center + new Vector2(0f, -100f);
                Velo.CEngineInst.CameraManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.Zero));
            }
        }

        private TimeSpan Cycle(TimeSpan time)
        {
            Frame lastFrame = recording[recording.Count - 1];
            TimeSpan begin = recording[recording.LapStart].DeltaSum;
            TimeSpan end = lastFrame.DeltaSum + lastFrame.Delta;
            if (time >= end)
            {
                time = new TimeSpan((time - begin).Ticks % (end - begin).Ticks) + begin;
            }
            return time;
        }

        private int Cycle(int frame)
        {
            if (frame >= recording.Count)
            {
                frame = (frame - recording.LapStart) % (recording.Count - recording.LapStart) + recording.LapStart;
            }
            return frame;
        }

        public void JumpToTime(TimeSpan newTime)
        {
            if (Type == EPlaybackType.VERIFY || Type == EPlaybackType.CAPTURE)
                return;

            newTime += recording[recording.LapStart].DeltaSum;
            if (Type == EPlaybackType.SET_GHOST)
                newTime = Cycle(newTime);

            if (newTime < recording[0].DeltaSum)
            {
                if (Type == EPlaybackType.SET_GHOST)
                    holdingTime = recording[0].DeltaSum - newTime;
                newTime = recording[0].DeltaSum;
                i = 0;
            }
            else if (newTime < time)
            {
                while (recording[i].DeltaSum > newTime)
                {
                    i--;
                    if (i == 0)
                        break;
                }
            }
            else if (newTime > time)
            {
                while (recording[i].DeltaSum + recording[i].Delta <= newTime)
                {
                    i++;
                    if (i >= recording.Count)
                    {
                        Finish();
                        return;
                    }
                }
            }

            time = newTime;

            RestoreState();
        }

        public void JumpToFrame(int frame)
        {
            if (Type == EPlaybackType.VERIFY || Type == EPlaybackType.CAPTURE)
                return;

            frame += recording.LapStart;
            i = frame;

            if (Type == EPlaybackType.SET_GHOST)
                i = Cycle(i);

            if (i < 0)
                i = 0;
            if (i >= recording.Count)
            {
                Finish();
                return;
            }
            time = recording[i].DeltaSum;

            RestoreState();
        }

        public bool DtFixed
        {
            get
            {
                return Running && (Type == EPlaybackType.VERIFY || Type == EPlaybackType.CAPTURE);
            }
        }

        public void ApplyCurrentFrame()
        {
            if (i >= recording.Count)
                return;
            if (float.IsNaN(recording[i].PosX))
            {
                return;
            }
            else if (i < recording.Count - 1)
            {
                TimeSpan frameDelta = recording[i].Delta;
                TimeSpan nowRel = time - recording[i].DeltaSum;
                if (nowRel < TimeSpan.Zero)
                    nowRel = TimeSpan.Zero;
                if (nowRel > frameDelta)
                    nowRel = frameDelta;

                Frame frame = Lerp(recording[i], recording[i + 1], (float)((double)nowRel.Ticks / frameDelta.Ticks));

                TimeSpan dt = Velo.GameTime - recording[i + 1].Time;

                frame.Apply(player, dt);
                frame.RestoreGrapple(player, recording[i + 1]);
            }
            else
            {
                recording[recording.Count - 1].Apply(player, TimeSpan.Zero);
            }
            if (
                recording is Timeline &&
                OfflineGameMods.Instance.WatermarkType.Value == OfflineGameMods.EWatermarkType.SUNGLASSES
            )
            {
                player.itemId = (int)EItem.SUNGLASSES;
            }
            Velo.measure("physics");
            player.UpdateHitbox();
            player.UpdateSprite(CEngine.CEngine.Instance.gameTime);
            player.grapple.sprite.Position = player.grapple.actor.Position;
            Velo.measure("Velo");
        }

        public void PreUpdate()
        {
            if (player.destroyed)
                return;

            if (holdingTime > TimeSpan.Zero)
                return;

            if (Type == EPlaybackType.VERIFY || Type == EPlaybackType.CAPTURE)
            {
                CEngine.CEngine cengine = CEngine.CEngine.Instance;
                TimeSpan delta = recording[i].Delta;
                TimeSpan time = recording[i].Time;
                cengine.gameTime = new GameTime(time, delta);

                if (Type == EPlaybackType.CAPTURE && captureTime == TimeSpan.Zero)
                {
                    cengine.gameTime = new GameTime(time, TimeSpan.Zero);
                    Capture();
                }
            }
            if (Type == EPlaybackType.VERIFY)
            {
                TimeSpan now = Velo.RealTime;
                if (!OfflineGameMods.Instance.DisableVerifyNotifications.Value && (now - lastNotificationUpdate).TotalSeconds > 0.25)
                {
                    lastNotificationUpdate = now;

                    double avgFramerate = i > 0 ? 1.0 / new TimeSpan((recording[i].DeltaSum - recording[0].DeltaSum).Ticks / i).TotalSeconds : 0d;

                    Notifications.Instance.ForceNotification("discrepancy: " + (int)discrepancy + "\navg. framerate: " + (int)avgFramerate);
                }
            }
        }

        public void PostUpdate()
        {
            if (player.destroyed)
                return;

            if (holdingTime > TimeSpan.Zero)
                return;

            if (holdingTime > TimeSpan.Zero)
                return;

            ItemIdPrev = ItemId;
            ItemId = player.itemId;

            if (i >= recording.Count)
            {
                Finish();
                return;
            }

            if (Type != EPlaybackType.SET_GHOST && recording[i].GetFlag(EFlag.RESET_LAP))
            {
                OfflineGameMods.Instance.RecordingAndReplay.DisableResetRestartingPlayback = true;
                Velo.ModuleSolo.ResetLap(
                    Velo.ModuleSolo.ghostReplay.Unknown && Velo.ModuleSolo.ghostSlot.Player != null,
                    new GameTime(recording[i].Time, recording[i].Delta)
                );
            }
            if (Type == EPlaybackType.VERIFY)
            {
                if (i == recording.LapStart)
                    recording[i].Apply(player, TimeSpan.Zero);

                if (!float.IsNaN(recording[i].PosX))
                {
                    double curDiscrepancy = (new Vector2(recording[i].PosX, recording[i].PosY) - player.actor.Position).Length();
                    discrepancy += curDiscrepancy;
                    if (curDiscrepancy >= 1d)
                        recording[i].Apply(player, TimeSpan.Zero);
                }
            }
            else if (Type == EPlaybackType.VIEW_REPLAY || Type == EPlaybackType.SET_GHOST)
            {
                ApplyCurrentFrame();
            }

            if (Type == EPlaybackType.CAPTURE)
            {
                Capture();
            }
        }

        private void Capture()
        {
            while (time - recording[0].DeltaSum >= captureTime)
            {
                videoCapturer.Capture();
                captureTime += new TimeSpan(TimeSpan.TicksPerSecond / videoCapturer.CaptureParams.CaptureRate);
            }
        }

        public bool SetInputs(Player player)
        {
            if (!Running)
                return false;
            if (player != this.player)
                return false;

            //if (holdingTime > TimeSpan.Zero)
               // return false;

            bool flag = player.leftHeld;
            bool flag2 = player.rightHeld;

            if (holdingTime > TimeSpan.Zero)
            {
                holdingTime -= Velo.GameDelta;
                if (holdingTime < TimeSpan.Zero)
                {
                    time -= holdingTime;
                    holdingTime = TimeSpan.Zero;
                }
            }
            else
                time += Velo.GameDelta;

            while (time >= recording[i].DeltaSum + recording[i].Delta)
            {
                i++;
                if (i >= recording.Count)
                {
                    if (Type == EPlaybackType.SET_GHOST)
                    {
                        i = recording.LapStart;
                        time = Cycle(time);
                    }
                    else
                        return false;
                }
            }

            Frame frame = recording[i];
            Frame nextFrame = recording[i + 1];

            frame.ApplyInputs(player);
            if (Type != EPlaybackType.VERIFY && Type != EPlaybackType.CAPTURE)
                frame.RestoreGrapple(player, nextFrame);

            if (player.leftHeld && player.rightHeld)
            {
                if (flag2 && !flag)
                {
                    player.dominatingDirection = -1;
                }
                else if (flag && !flag2)
                {
                    player.dominatingDirection = 1;
                }
            }
            else
            {
                player.dominatingDirection = 0;
            }

            return true;
        }

        public void LapFinish(float time)
        {
            if (Type == EPlaybackType.SET_GHOST)
            {
                Restart();
            }
            if (Type == EPlaybackType.VERIFY && !OfflineGameMods.Instance.DisableVerifyNotifications.Value)
            {
                Notifications.Instance.ForceNotification("Final time:\n" + time.ToString("0.00000000"));
            }
        }

        public bool SkipUpdateSprite(Player player)
        {
            if (!Running || Type == EPlaybackType.VERIFY || Type == EPlaybackType.CAPTURE)
                return false;
            return player == this.player;
        }

        public void PostRender()
        {
            videoCapturer?.PostRender();
        }

        public void PostPresent()
        {
            videoCapturer?.PostPresent();
        }
    }
}
