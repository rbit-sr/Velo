using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Velo
{
    public struct Frame
    {
        public enum EFlags : int
        {
            LEFT_P, RIGHT_P, JUMP_P, GRAPPLE_P, SLIDE_P, BOOST_P, ITEM_P,
            MOVE_DIR,
            CONNECTED, GRAPPLING, SWINGING, 
            ROPE_VISIBLE
        }

        public long Delta;
        public long Time;
        public long DeltaSum;
        public float PosX;
        public float PosY;
        public float VelX;
        public float VelY;
        public float Boost;
        public int JumpState;
        public float GrapPosX;
        public float GrapPosY;
        public int Flags;

        public static void GetBytes(Frame frame, byte[] bytes)
        {
            int off = 0;
            Bytes.Write(frame.Delta, bytes, ref off);
            Bytes.Write(frame.Time, bytes, ref off);
            Bytes.Write(frame.DeltaSum, bytes, ref off);
            Bytes.Write(frame.PosX, bytes, ref off);
            Bytes.Write(frame.PosY, bytes, ref off);
            Bytes.Write(frame.VelX, bytes, ref off);
            Bytes.Write(frame.VelY, bytes, ref off);
            Bytes.Write(frame.Boost, bytes, ref off);
            Bytes.Write(frame.JumpState, bytes, ref off);
            Bytes.Write(frame.GrapPosX, bytes, ref off);
            Bytes.Write(frame.GrapPosY, bytes, ref off);
            Bytes.Write(frame.Flags, bytes, ref off);
        }

        public static Frame FromBytes(byte[] bytes)
        {
            Frame frame = new Frame();
            int off = 0;
            Bytes.Read(ref frame.Delta, bytes, ref off);
            Bytes.Read(ref frame.Time, bytes, ref off);
            Bytes.Read(ref frame.DeltaSum, bytes, ref off);
            Bytes.Read(ref frame.PosX, bytes, ref off);
            Bytes.Read(ref frame.PosY, bytes, ref off);
            Bytes.Read(ref frame.VelX, bytes, ref off);
            Bytes.Read(ref frame.VelY, bytes, ref off);
            Bytes.Read(ref frame.Boost, bytes, ref off);
            Bytes.Read(ref frame.JumpState, bytes, ref off);
            Bytes.Read(ref frame.GrapPosX, bytes, ref off);
            Bytes.Read(ref frame.GrapPosY, bytes, ref off);
            Bytes.Read(ref frame.Flags, bytes, ref off);
            return frame;
        }

        public Frame(Player player, GameTime gameTime, long prevDeltaSum)
        {
            Delta = gameTime.ElapsedGameTime.Ticks;
            Time = gameTime.TotalGameTime.Ticks;
            DeltaSum = prevDeltaSum + Delta;
            PosX = player.actor.Position.X;
            PosY = player.actor.Position.Y;
            VelX = player.actor.Velocity.X;
            VelY = player.actor.Velocity.Y;
            Boost = player.boost;
            JumpState = player.jump_state;
            GrapPosX = player.grapple.actor.Position.X;
            GrapPosY = player.grapple.actor.Position.Y;
            Flags =
                (
                    (player.leftPressed ? (1 << (int)EFlags.LEFT_P) : 0) |
                    (player.rightPressed ? (1 << (int)EFlags.RIGHT_P) : 0) |
                    (player.jumpPressed ? (1 << (int)EFlags.JUMP_P) : 0) |
                    (player.grapplePressed ? (1 << (int)EFlags.GRAPPLE_P) : 0) |
                    (player.slidePressed ? (1 << (int)EFlags.SLIDE_P) : 0) |
                    (player.boostPressed ? (1 << (int)EFlags.BOOST_P) : 0) |
                    (player.itemPressed ? (1 << (int)EFlags.ITEM_P) : 0) |
                    (player.move_dir == 1 ? (1 << (int)EFlags.MOVE_DIR) : 0) |
                    (player.grapple.connected ? (1 << (int)EFlags.CONNECTED) : 0) |
                    (player.grappling ? (1 << (int)EFlags.GRAPPLING) : 0) |
                    (player.swinging ? (1 << (int)EFlags.SWINGING) : 0) |
                    (player.rope.owner != null ? (1 << (int)EFlags.ROPE_VISIBLE) : 0)
                );
        }

        public void Apply(Player player)
        {
            player.actor.Position = new Vector2(PosX, PosY);
            player.actor.Velocity = new Vector2(VelX, VelY);
            player.boost = Boost;
            player.jump_state = JumpState;
            player.can_grapple = true;
            /*player.move_dir = (Flags & (1 << (int)EFlags.CONNECTED)) != 0 ? 1 : -1;
            player.grappling = (Flags & (1 << (int)EFlags.GRAPPLING)) != 0;
            player.swinging = (Flags & (1 << (int)EFlags.SWINGING)) != 0;
            Vector2 dummy = Vector2.Zero;
            Vector2 dir = Vector2.Zero;
            player.PrepareGrapPosDir(ref dummy, ref dir);
            if (player.grappling && !player.swinging)
                player.grapple.Shoot(new Vector2(GrapPosX, GrapPosY), dir);
            else if (player.swinging)
                player.grapple.Connect(new Vector2(GrapPosX, GrapPosY));
            else
                player.grapple.Remove();
            bool ropeVisible = (Flags & (1 << (int)EFlags.ROPE_VISIBLE)) != 0;
            if (ropeVisible && player.rope.owner == null)
            {
                player.rope.active = false;
                player.rope.Create(player, player.grapple);
            }
            if (!ropeVisible && player.rope.owner != null)
            {
                player.rope.Remove();
                player.rope.active = false;
            }*/
        }

        public void ApplyInputs(Player player)
        {
            player.leftPressed = (Flags & (1 << (int)EFlags.LEFT_P)) != 0;
            player.rightPressed = (Flags & (1 << (int)EFlags.RIGHT_P)) != 0;
            player.jumpPressed = (Flags & (1 << (int)EFlags.JUMP_P)) != 0;
            player.grapplePressed = (Flags & (1 << (int)EFlags.GRAPPLE_P)) != 0;
            player.slidePressed = (Flags & (1 << (int)EFlags.SLIDE_P)) != 0;
            player.boostPressed = (Flags & (1 << (int)EFlags.BOOST_P)) != 0;
            player.itemPressed = (Flags & (1 << (int)EFlags.ITEM_P)) != 0;
        }

        public static Frame Lerp(Frame frame1, Frame frame2, float r)
        {
            bool grappleShoot =
                (frame1.Flags & (1 << (int)EFlags.GRAPPLE_P)) == 0 &&
                (frame2.Flags & (1 << (int)EFlags.GRAPPLE_P)) != 0;
            return new Frame
            {
                PosX = (1f - r) * frame1.PosX + r * frame2.PosX,
                PosY = (1f - r) * frame1.PosY + r * frame2.PosY,
                VelX = (1f - r) * frame1.VelX + r * frame2.VelX,
                VelY = (1f - r) * frame1.VelY + r * frame2.VelY,
                Boost = (1f - r) * frame1.Boost + r * frame2.Boost,
                GrapPosX = grappleShoot ? frame2.GrapPosX : (1f - r) * frame1.GrapPosX + r * frame2.GrapPosX,
                GrapPosY = grappleShoot ? frame2.GrapPosY : (1f - r) * frame1.GrapPosY + r * frame2.GrapPosY,
                JumpState = frame2.JumpState,
                Flags = frame2.Flags
            };
        }
    }

    public class Recording
    {
        public CircArray<Frame> Frames;
        public CircArray<Savestate> Savestates;
        public RunInfo Info;
        public RulesChecker Rules;

        public Recording()
        {
            Frames = new CircArray<Frame>();
            Savestates = new CircArray<Savestate>();
            Info = new RunInfo();
            Rules = new RulesChecker();
            Clear();
        }

        public Frame this[int i]
        {
            get
            {
                return Frames[i];
            }
            set
            {
                Frames[i] = value;
            }
        }

        public void Clear()
        {
            Frames.Clear();
            Savestates.Clear();
            Info.Id = -1;
            Info.PlayerId = 0;
            Info.MapId = -1;
            Info.Category = -1;
        }

        public void PushBack(Frame frame, Savestate savestate)
        {
            Frames.PushBack(frame);
            Savestates.PushBack(savestate);
        }

        public void PopFront()
        {
            Frames.PopFront();
            Savestates.PopFront();
        }

        public int Count { get { return Frames.Count; } }
    }

    public class Recorder
    {
        private Recording recording;
        private long lastSavestate = 0;
        private Savestate nextSavestate = null;

        public int MaxFrames = 120 * 300;
        public float SavestateInterval = 1.0f;

        public Recorder()
        {
            
        }

        public void Start(Recording recording)
        {
            this.recording = recording;
            lastSavestate = 0;
            recording.Clear();
        }

        public void Stop()
        {
            recording = null;
        }

        public void PreUpdate()
        {
            if (recording == null)
                return;

            recording.Rules.Update();

            GameTime gameTime = CEngine.CEngine.Instance.gameTime;
            if (gameTime.ElapsedGameTime.Ticks == 0)
                return;

            bool lapFinish = !Velo.MainPlayerReset && Velo.Timer < Velo.TimerPrev;

            if (Velo.MainPlayerReset)
                recording.Clear();

            if (
                Velo.MainPlayerReset ||
                (new TimeSpan(gameTime.TotalGameTime.Ticks) - new TimeSpan(lastSavestate)).TotalSeconds > SavestateInterval
                )
            {
                lastSavestate = gameTime.TotalGameTime.Ticks;
                nextSavestate = new Savestate();
                nextSavestate.Save(new List<Savestate.ActorType>
                    {
                        Savestate.Player,
                        Savestate.PlayerBot,
                        Savestate.Grapple,
                        Savestate.Rope,
                        Savestate.Timer,
                        Savestate.Trigger,
                        Savestate.TriggerSaw,
                        Savestate.Checkpoint,
                        Savestate.SwitchBlock,
                        Savestate.FallTile,
                        Savestate.RocketLauncher
                    }, Savestate.EListMode.INCLUDE);
            }
        }

        public void PostUpdate()
        {
            if (recording == null)
                return;

            GameTime gameTime = CEngine.CEngine.Instance.gameTime;
            if (gameTime.ElapsedGameTime.Ticks == 0)
                return;

            Player player = Velo.MainPlayer;

            recording.PushBack(
                new Frame(
                    player: player, 
                    gameTime: gameTime, 
                    prevDeltaSum: recording.Count >= 1 ? recording[recording.Count - 1].DeltaSum : 0L
                ),
                nextSavestate
            );
                
            nextSavestate = null;
            TrimBeginning();
        }

        private void TrimBeginning()
        {
            while (true)
            {
                if (recording.Savestates[0] != null && recording.Count <= MaxFrames)
                    break;
                recording.PopFront();
            }
        }
    }

    public class Playback
    {
        private Recording recording;
        private int i = 0;
        private long deltaSum = 0;

        public bool Interpolate;
        public bool UseGhost;

        public Playback()
        {

        }

        public void Start(Recording recording)
        {
            this.recording = recording;
            if (!UseGhost)
                recording.Savestates[0].Load(!Interpolate);
            deltaSum = 0;
            i = 0;
        }

        public void Stop()
        {
            recording = null;
        }

        public bool DtFixed
        {
            get
            {
                return !Finished && !Interpolate;
            }
        }

        public bool Finished
        {
            get
            {
                return recording == null;
            }
        }

        public void PreUpdate()
        {
            if (Finished)
                return;

            GameTime gameTime = CEngine.CEngine.Instance.gameTime;
            if (gameTime.ElapsedGameTime.Ticks == 0)
                return;

            if (Velo.MainPlayerReset)
            {
                Start(recording);
            }
            if (!Interpolate)
            {
                CEngine.CEngine cengine = CEngine.CEngine.Instance;
                long delta = recording[i].Delta;
                long time = recording[i].Time;
                cengine.gameTime = new GameTime(new TimeSpan(time), new TimeSpan(delta));
            }
        }

        public void PostUpdate()
        {
            if (Finished)
                return;

            GameTime gameTime = CEngine.CEngine.Instance.gameTime;
            if (gameTime.ElapsedGameTime.Ticks == 0)
                return;

            if (!Interpolate)
            {
                i++;
                if (i >= recording.Count)
                {
                    Stop();
                    return;
                }
            }
            else
            {
                deltaSum += gameTime.ElapsedGameTime.Ticks;

                while (recording[i].DeltaSum < deltaSum)
                {
                    i++;
                    if (i >= recording.Count)
                    {
                        Stop();
                        return;
                    }
                }
            }
            if (Interpolate && i >= 1)
            {
                Player player = UseGhost ? Velo.Ghost : Velo.MainPlayer;

                long frameDelta = recording[i].Delta;
                long nowRel = deltaSum - recording[i - 1].DeltaSum;
                if (nowRel < 0)
                    nowRel = 0;
                if (nowRel > frameDelta)
                    nowRel = frameDelta;

                Frame frame = Frame.Lerp(recording[i - 1], recording[i], (float)((double)nowRel / (double)frameDelta));
                frame.Apply(player);

                if (UseGhost)
                    SetInputs(player);
            }
        }

        public bool SetInputs()
        {
            if (Finished || UseGhost)
                return false;

            SetInputs(Velo.MainPlayer);

            return true;
        }

        public void SetInputs(Player player)
        {
            bool flag = player.leftPressed;
            bool flag2 = player.rightPressed;

            recording[i].ApplyInputs(player);

            if (player.leftPressed && player.rightPressed)
            {
                if (flag2 && !flag)
                {
                    player.unknown27 = -1;
                }
                else if (flag && !flag2)
                {
                    player.unknown27 = 1;
                }
            }
            else
            {
                player.unknown27 = 0;
            }
        }

        public bool SkipIfGhost()
        {
            return !Finished && UseGhost;
        }
    }
}
