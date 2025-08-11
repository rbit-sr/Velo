using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Windows;
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
        public int AbsoluteFrame => i;

        private bool playShoot = false;
        private bool playConnect = false;

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
            videoCapturer?.Finish();
            videoCapturer = null;
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
            {
                ApplyCurrentFrame();
                player.Update(new GameTime(Velo.GameTime, new TimeSpan(1)));
                ApplyCurrentFrame();
            }
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

                TimeSpan dt = Velo.GameTime - recording[i].GlobalTime;

                frame.Apply(player, dt);
                frame.ApplyGrapple(player);
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
            player.grapple.Update(null);
            Velo.update_rope_color(player.rope);
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
                TimeSpan time = recording[i].GlobalTime;
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
                if (!RecordingAndReplay.Instance.DisableVerifyNotifications.Value && (now - lastNotificationUpdate).TotalSeconds > 0.25)
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
                RecordingAndReplay.Instance.DisableResetRestartingPlayback = true;
                Velo.ModuleSolo.ResetLap(
                    Velo.ModuleSolo.ghostReplay.Unknown && Velo.ModuleSolo.ghostSlot.Player != null,
                    new GameTime(recording[i].GlobalTime, recording[i].Delta)
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
            if (Type == EPlaybackType.VIEW_REPLAY || Type == EPlaybackType.SET_GHOST)
            {
                if (playShoot && !Velo.disable_grapple_sound(player))
                {
                    player.shootSound.Play();
                }
                if (playConnect && !Velo.disable_grapple_sound(player))
                {
                    player.grapple.sound.PlaySound("Success");
                }

                ApplyCurrentFrame();
            }
            if (Type == EPlaybackType.CAPTURE)
            {
                ApplyCurrentFrame();
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

            playShoot = false;
            playConnect = false;

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
                if (recording[i].GetFlag(EFlag.GRAPPLING) && !recording[i - 1].GetFlag(EFlag.GRAPPLING))
                    playShoot = true;
                if (recording[i].GetFlag(EFlag.CONNECTED) && !recording[i - 1].GetFlag(EFlag.CONNECTED))
                    playConnect = true;
            }

            Frame frame = recording[i];

            frame.ApplyInputs(player);
            if (Type != EPlaybackType.VERIFY && Type != EPlaybackType.CAPTURE)
                frame.ApplyGrapple(player);

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
            if (Type == EPlaybackType.VERIFY && !RecordingAndReplay.Instance.DisableVerifyNotifications.Value)
            {
                Notifications.Instance.ForceNotification("Final time:\n" + time.ToString("0.00000000"));
            }
        }

        public bool SkipUpdateSprite(Player player)
        {
            if (!Running || Type == EPlaybackType.VERIFY)
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
