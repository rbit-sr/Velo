using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Velo
{
    public struct StatsTracker
    {
        private float dist;
        private float groundDist;
        private float swingDist;
        private float climbDist;
        private float speedSum;
        private int grapples;
        private int jumps;
        private float boostUsed;

        public void Apply(ref RunInfo info, int frames)
        {
            info.Dist = (int)(dist + 0.5f);
            info.GroundDist = (int)(groundDist + 0.5f);
            info.SwingDist = (int)(swingDist + 0.5f);
            info.ClimbDist = (int)(climbDist + 0.5f);
            info.AvgSpeed = (short)(speedSum / frames + 0.5f);
            info.Jumps = (short)jumps;
            info.Grapples = (short)grapples;
            info.BoostUsed = (short)(boostUsed * 50f + 0.5f);
        }

        public void Init(Player player)
        {
            this = default;
            if (player.grappling)
                grapples++;
            if ((player.game_time.TotalGameTime - player.jumpTime).TotalSeconds < Velo.get_jump_duration())
                jumps++;
            speedSum += player.actor.Velocity.Length();
        }

        public void Update(Player player, Frame prevFrame)
        {
            dist += (player.actor.Position - new Vector2(prevFrame.PosX, prevFrame.PosY)).Length();
            if (player.onGround)
                groundDist += (player.actor.Position - new Vector2(prevFrame.PosX, prevFrame.PosY)).Length();
            if (player.swinging)
                swingDist += (player.actor.Position - new Vector2(prevFrame.PosX, prevFrame.PosY)).Length();
            if (player.climbing)
                climbDist += (player.actor.Position - new Vector2(prevFrame.PosX, prevFrame.PosY)).Length();
            speedSum += player.actor.Velocity.Length();
            if (prevFrame.JumpTime != player.jumpTime)
                jumps++;
            if (!prevFrame.GetFlag(Frame.EFlag.GRAPPLING) && player.grappling)
                grapples++;
            if (player.boostHeld && !player.climbing)
            {
                bool usedBoost = false;
                if (
                    player.onGround &&
                    !(player.rightHeld && !player.leftHeld && player.actor.Velocity.X < 0f) &&
                    !(player.leftHeld && !player.rightHeld && player.actor.Velocity.X > 0f))
                    usedBoost = true;
                if (
                    player.inAir &&
                    !player.grapple.connected &&
                    (player.boostCooldown <= 0f || Math.Abs(player.actor.Velocity.X) <= 600f || player.usingBoost) &&
                    player.wallJumpBonusTimer <= 0f)
                    usedBoost = true;
                if (usedBoost)
                    boostUsed += (float)Math.Min(0.85f * Velo.GameDelta.TotalSeconds, prevFrame.Boost);
            }
        }
    }

    public class Recorder : IRecorder
    {
        private Recording recording;
        private TimeSpan lastSavestate = TimeSpan.Zero;
        public StatsTracker StatsTracker;

        public int MaxFrames = 40 * 60 * 300;
        public float SavestateInterval = 1.0f;

        public TimeSpan Time => recording[recording.Count - 1].DeltaSum - recording[recording.LapStart].DeltaSum;
        public int Frame => recording.Count - 1 - recording.LapStart;
        public int AbsoluteFrame => recording.Count - 1;

        private bool running = false;
        public bool Running => running;
        public bool DtFixed => false;

        public IReplayable Recording => recording;

        public Recorder()
        {

        }

        public void Set(Recording recording)
        {
            this.recording = recording;
        }

        public void Resume()
        {
            if (recording == null)
                return;
            recording.Clear();
            StatsTracker = default;
            lastSavestate = TimeSpan.Zero;
            Capture();
            running = true;
            recording.Rules.LapStart(true);
        }

        public void Stop()
        {
            running = false;
        }

        public void Close()
        {
            running = false;
            recording = null;
        }

        private void Capture()
        {
            Savestate savestate = null;

            if ((Velo.GameTime - lastSavestate).TotalSeconds > SavestateInterval)
            {
                lastSavestate = Velo.GameTime;
                savestate = new Savestate();
                savestate.Save(new List<Savestate.ActorType> { Savestate.ATAIVolume }, Savestate.EListMode.EXCLUDE, progressOnly: true);
            }

            Frame frame = new Frame(
                   player: Velo.MainPlayer,
                   realTime: Velo.RealTime,
                   gameDelta: TimeSpan.Zero, // to be filled in next PostUpdate
                   gameTime: TimeSpan.Zero, // to be filled in next PostUpdate
                   deltaSum: recording.Count >= 1 ? recording[recording.Count - 1].DeltaSum + recording[recording.Count - 1].Delta : TimeSpan.Zero
               );
            frame.SetInputs(Velo.MainPlayer);

            if (recording.Count == 0)
                StatsTracker.Init(Velo.MainPlayer);
            else
                StatsTracker.Update(Velo.MainPlayer, recording[recording.Count - 1]);

            recording.PushBack(
               frame,
               savestate
           );
        }

        public void PreUpdate()
        {

        }

        public void PostUpdate()
        {
            if (Velo.MainPlayer == null || Velo.MainPlayer.destroyed)
                return;
            if (recording == null)
                return;

            Frame frame = recording.Frames[recording.Count - 1];
            frame.Delta = Velo.GameDelta;
            frame.GlobalTime = Velo.GameTime;
            recording.Frames[recording.Count - 1] = frame;

            Capture();

            recording.TrimBeginning(MaxFrames);

            recording.Rules.Update();
        }

        public void JumpToTime(TimeSpan time)
        {

        }

        public void JumpToFrame(int frame)
        {

        }

        public void MainPlayerReset()
        {
            Resume();
            recording.Rules.LapStart(true);
        }

        public void LapFinish(float time)
        {
            recording.LapStart = recording.Count - 1;
            StatsTracker.Init(Velo.MainPlayer);
            lastSavestate = TimeSpan.Zero;
            recording.Rules.LapStart(reset: false);
        }
    }
}
