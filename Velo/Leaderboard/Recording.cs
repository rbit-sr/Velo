using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.IO;
using static Velo.Frame;

namespace Velo
{
    public struct Frame
    {
        public enum EFlags : int
        {
            LEFT_P, RIGHT_P, JUMP_P, GRAPPLE_P, SLIDE_P, BOOST_P, ITEM_P,
            MOVE_DIR, ON_GROUND, STUNNED, TAUNT, ON_WALL, IN_AIR, SLIDING,
            CONNECTED, GRAPPLING, SWINGING, 
            ROPE_VISIBLE, ROPE_BREAKING, ROPE_OWNER, ROPE_LINES
        }

        public long Delta;
        public long Time;
        public long DeltaSum;
        public long JumpTime;
        public float PosX;
        public float PosY;
        public float VelX;
        public float VelY;
        public float Boost;
        public int JumpState;
        public float GrapPosX;
        public float GrapPosY;
        public float GrapRad;
        public int Flags;

        public static void GetBytes(Frame frame, byte[] bytes)
        {
            int off = 0;
            Bytes.Write(frame.Delta, bytes, ref off);
            Bytes.Write(frame.Time, bytes, ref off);
            Bytes.Write(frame.DeltaSum, bytes, ref off);
            Bytes.Write(frame.JumpTime, bytes, ref off);
            Bytes.Write(frame.PosX, bytes, ref off);
            Bytes.Write(frame.PosY, bytes, ref off);
            Bytes.Write(frame.VelX, bytes, ref off);
            Bytes.Write(frame.VelY, bytes, ref off);
            Bytes.Write(frame.Boost, bytes, ref off);
            Bytes.Write(frame.JumpState, bytes, ref off);
            Bytes.Write(frame.GrapPosX, bytes, ref off);
            Bytes.Write(frame.GrapPosY, bytes, ref off);
            Bytes.Write(frame.GrapRad, bytes, ref off);
            Bytes.Write(frame.Flags, bytes, ref off);
        }

        public static Frame FromBytes(byte[] bytes)
        {
            Frame frame = new Frame();
            int off = 0;
            Bytes.Read(ref frame.Delta, bytes, ref off);
            Bytes.Read(ref frame.Time, bytes, ref off);
            Bytes.Read(ref frame.DeltaSum, bytes, ref off);
            Bytes.Read(ref frame.JumpTime, bytes, ref off);
            Bytes.Read(ref frame.PosX, bytes, ref off);
            Bytes.Read(ref frame.PosY, bytes, ref off);
            Bytes.Read(ref frame.VelX, bytes, ref off);
            Bytes.Read(ref frame.VelY, bytes, ref off);
            Bytes.Read(ref frame.Boost, bytes, ref off);
            Bytes.Read(ref frame.JumpState, bytes, ref off);
            Bytes.Read(ref frame.GrapPosX, bytes, ref off);
            Bytes.Read(ref frame.GrapPosY, bytes, ref off);
            Bytes.Read(ref frame.GrapRad, bytes, ref off);
            Bytes.Read(ref frame.Flags, bytes, ref off);
            return frame;
        }

        public Frame(Player player, GameTime gameTime, long deltaSum)
        {
            Delta = gameTime.ElapsedGameTime.Ticks;
            Time = gameTime.TotalGameTime.Ticks;
            DeltaSum = deltaSum;
            JumpTime = player.timespan1.Ticks;
            PosX = player.actor.Position.X;
            PosY = player.actor.Position.Y;
            VelX = player.actor.Velocity.X;
            VelY = player.actor.Velocity.Y;
            Boost = player.boost;
            JumpState = player.jump_state;
            GrapPosX = player.grapple.actor.Position.X;
            GrapPosY = player.grapple.actor.Position.Y;
            GrapRad = player.grap_rad;
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
                    (player.on_ground ? (1 << (int)EFlags.ON_GROUND) : 0) |
                    (player.stunned ? (1 << (int)EFlags.STUNNED) : 0) |
                    (player.taunt ? (1 << (int)EFlags.TAUNT) : 0) |
                    (player.on_wall ? (1 << (int)EFlags.ON_WALL) : 0) |
                    (player.in_air ? (1 << (int)EFlags.IN_AIR) : 0) |
                    (player.sliding ? (1 << (int)EFlags.SLIDING) : 0) |
                    (player.grapple.connected ? (1 << (int)EFlags.CONNECTED) : 0) |
                    (player.grappling ? (1 << (int)EFlags.GRAPPLING) : 0) |
                    (player.swinging ? (1 << (int)EFlags.SWINGING) : 0) |
                    (player.rope.active ? (1 << (int)EFlags.ROPE_VISIBLE) : 0) |
                    (player.rope.breaking ? (1 << (int)EFlags.ROPE_BREAKING) : 0) |
                    (player.rope.owner != null ? (1 << (int)EFlags.ROPE_OWNER) : 0) |
                    (player.rope.lineDrawComp1.Lines.Count > 0 ? (1 << (int)EFlags.ROPE_LINES) : 0)
                );
        }

        public void Apply(Player player, long dt, bool setFlags = false, bool forceGrapple = false)
        {
            player.actor.Position = new Vector2(PosX, PosY);
            player.actor.Velocity = new Vector2(VelX, VelY);
            if (GrapPosX != float.NaN && GrapPosY != float.NaN)
                player.grapple.actor.Position = new Vector2(GrapPosX, GrapPosY);
            player.grap_rad = GrapRad;
            player.boost = Boost;
            player.timespan1 = new TimeSpan(JumpTime + dt);

            if (setFlags)
            {
                player.jump_state = JumpState;
                player.move_dir = (Flags & (1 << (int)EFlags.MOVE_DIR)) != 0 ? 1 : -1;
                player.on_ground = (Flags & (1 << (int)EFlags.ON_GROUND)) != 0;
                player.can_grapple = true;
                player.on_wall = (Flags & (1 << (int)EFlags.ON_WALL)) != 0;
                player.in_air = (Flags & (1 << (int)EFlags.IN_AIR)) != 0;
                player.sliding = (Flags & (1 << (int)EFlags.SLIDING)) != 0;
                player.stunned = (Flags & (1 << (int)EFlags.STUNNED)) != 0;
                player.taunt = (Flags & (1 << (int)EFlags.TAUNT)) != 0;
            }
            if (forceGrapple)
            {
                player.grapple.connected = (Flags & (1 << (int)EFlags.CONNECTED)) != 0;
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
                player.rope.active = (Flags & (1 << (int)EFlags.ROPE_VISIBLE)) != 0;
                player.rope.breaking = (Flags & (1 << (int)EFlags.ROPE_BREAKING)) != 0;
                bool ropeOwner = (Flags & (1 << (int)EFlags.ROPE_OWNER)) != 0;
                if (ropeOwner)
                {
                    player.rope.owner = player;
                    player.rope.target = player.grapple;
                }
                else
                {
                    player.rope.owner = null;
                    player.rope.target = null;
                }
                bool ropeLines = (Flags & (1 << (int)EFlags.ROPE_LINES)) != 0;
                player.rope.UpdateLines();
                player.rope.lineDrawComp1.Lines.Clear();
                if (ropeLines)
                {
                    player.rope.lineDrawComp1.Lines.Add(player.rope.line1);
                    player.rope.lineDrawComp1.Lines.Add(player.rope.line2);
                }
            }
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
            bool grappleJustShot =
                (frame1.Flags & (1 << (int)EFlags.GRAPPLE_P)) == 0 &&
                (frame2.Flags & (1 << (int)EFlags.GRAPPLE_P)) != 0;
            return new Frame
            {
                PosX = (1f - r) * frame1.PosX + r * frame2.PosX,
                PosY = (1f - r) * frame1.PosY + r * frame2.PosY,
                VelX = (1f - r) * frame1.VelX + r * frame2.VelX,
                VelY = (1f - r) * frame1.VelY + r * frame2.VelY,
                Boost = (1f - r) * frame1.Boost + r * frame2.Boost,
                GrapPosX = grappleJustShot ? float.NaN : (1f - r) * frame1.GrapPosX + r * frame2.GrapPosX,
                GrapPosY = grappleJustShot ? float.NaN : (1f - r) * frame1.GrapPosY + r * frame2.GrapPosY,
                GrapRad = frame2.GrapRad,
                JumpState = frame2.JumpState,
                JumpTime = frame2.JumpTime,
                Flags = frame2.Flags
            };
        }
    }

    public struct Stats
    {
        public double TravDist;
        public double SpeedSum;
        public int Grapples;
        public int Jumps;
        public double BoostUsed;
    }

    public class Recording
    {
        public static readonly int VERSION = 0;

        public CircArray<Frame> Frames;
        public CircArray<Savestate> Savestates;
        public RunInfo Info;
        public RulesChecker Rules;
        public int LapStart;

        public Stats Stats;

        public Recording()
        {
            Frames = new CircArray<Frame>();
            Savestates = new CircArray<Savestate>();
            Info = new RunInfo();
            Rules = new RulesChecker();
            Clear();
        }

        public Recording Clone()
        {
            return new Recording
            {
                Frames = Frames.Clone(),
                Savestates = Savestates.Clone(),
                Info = Info,
                Rules = Rules.Clone(),
                LapStart = LapStart,
                Stats = Stats
            };
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
            LapStart = 0;
            ClearStats();
        }

        public void ClearStats()
        {
            Stats = default(Stats);
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
            LapStart--;
        }

        public int Count { get { return Frames.Count; } }

        public void Finish()
        {
            TrimBeginningFromLapStart(4f);
            Rules.Finish();
            Info.PlayerId = Steamworks.SteamUser.GetSteamID().m_SteamID;
            Info.MapId = Map.GetCurrentMapId();
            Info.Category = (int)Rules.GetCategory();
            Info.RunTime = (int)(Velo.TimerPrev * 1000f);
            Info.CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            Info.TravDist = (int)(Stats.TravDist + 0.5);
            Info.AvgSpeed = (short)(Stats.SpeedSum / (Frames.Count - LapStart) + 0.5);
            Info.Jumps = (short)Stats.Jumps;
            Info.Grapples = (short)Stats.Grapples;
            Info.BoostUsed = (short)(Stats.BoostUsed * 50d + 0.5);
            
            foreach (var reason in Rules.OneLapReasons)
            {
                if (reason != null)
                    Console.WriteLine(reason);
            }
            foreach (var reason in Rules.SkipReasons)
            {
                if (reason != null)
                    Console.WriteLine(reason);
            }
            foreach (var reason in Rules.Violations)
            {
                if (reason != null)
                    Console.WriteLine(reason);
            }
        }

        public void TrimBeginning(int maxFrames)
        {
            if (Savestates.Count == 0)
                return;
            while (true)
            {
                if (Savestates[0] != null && Count <= maxFrames)
                    break;
                PopFront();
            }
        }

        public void TrimBeginningFromLapStart(float seconds)
        {
            if (LapStart < 0 || Count <= LapStart)
                return;
            long lapStartTime = Frames[LapStart].Time;
            while (true)
            {
                long time = Frames[0].Time;
                float duration = (float)(new TimeSpan(lapStartTime) - new TimeSpan(time)).TotalSeconds;
                if (Savestates[0] != null && duration <= seconds)
                    break;
                PopFront();
            }
        }

        public void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(VERSION), 0, sizeof(int));

            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            MemoryStream raw = new MemoryStream();

            byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
            RunInfo.GetBytes(Info, buffer);
            raw.Write(buffer, 0, buffer.Length);

            raw.Write(BitConverter.GetBytes(Count * Marshal.SizeOf<Frame>()), 0, sizeof(int));

            buffer = new byte[Marshal.SizeOf<Frame>()];
            for (int i = 0; i < Count; i++)
            {
                Frame.GetBytes(Frames[i], buffer);
                raw.Write(buffer, 0, buffer.Length);
            }

            raw.Write(BitConverter.GetBytes(Savestates[0].Chunk.Size), 0, sizeof(int));
            raw.Write(Savestates[0].Chunk.Data, 0, Savestates[0].Chunk.Size);

            raw.Position = 0;

            encoder.WriteCoderProperties(stream);
            stream.Write(BitConverter.GetBytes(raw.Length), 0, sizeof(int));
            encoder.Code(raw, stream, raw.Length, -1, null);
            raw.Close();
        }

        public void Read(Stream stream)
        {
            byte[] version = new byte[sizeof(int)];
            stream.ReadExactly(version, 0, version.Length); // unused for now

            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
            byte[] properties = new byte[5];
            stream.ReadExactly(properties, 0, properties.Length);
            decoder.SetDecoderProperties(properties);

            byte[] dataSize = new byte[sizeof(int)];
            stream.ReadExactly(dataSize, 0, dataSize.Length);

            MemoryStream raw = new MemoryStream();
            decoder.Code(stream, raw, stream.Length, BitConverter.ToInt32(dataSize, 0), null);

            raw.Position = 0;

            byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
            raw.Read(buffer, 0, buffer.Length);
            Info = RunInfo.FromBytes(buffer);

            buffer = new byte[sizeof(int)];
            raw.Read(buffer, 0, sizeof(int));
            int size = BitConverter.ToInt32(buffer, 0);
            buffer = new byte[Marshal.SizeOf<Frame>()];
            int frameCount = size / buffer.Length;
            for (int i = 0; i < frameCount; i++)
            {
                raw.Read(buffer, 0, buffer.Length);
                PushBack(Frame.FromBytes(buffer), null);
            }

            Savestates[0] = new Savestate();
            buffer = new byte[sizeof(int)];
            raw.Read(buffer, 0, sizeof(int));
            size = BitConverter.ToInt32(buffer, 0);
            Savestates[0].Chunk.Size = size;
            Savestates[0].Chunk.Data = new byte[size];
            raw.Read(Savestates[0].Chunk.Data, 0, size);

            raw.Close();
        }
    }

    public class Recorder
    {
        private Recording recording;
        private long lastSavestate = 0;
        private Savestate nextSavestate = null;

        public int MaxFrames = 120 * 300;
        public float SavestateInterval = 1.0f;

        private Vector2 prevPos = Vector2.Zero;
        private float prevBoost = 0f;
        private long prevJumpTime = 0;
        private long prevGrapTime = 0;

        public Recorder()
        {
            
        }

        public void Start(Recording recording)
        {
            this.recording = recording;
            Restart();
        }

        public void Restart()
        {
            if (recording != null)
                recording.Clear();
            lastSavestate = 0;
        }

        public void Stop()
        {
            recording = null;
        }

        public void PreUpdate()
        {
            if (!Velo.Ingame)
                return;
            prevPos = Velo.MainPlayer.actor.Position;
            prevBoost = Velo.MainPlayer.boost;
            prevJumpTime = Velo.MainPlayer.timespan1.Ticks;
            prevGrapTime = Velo.MainPlayer.timespan6.Ticks;
            if (recording == null)
                return;

            GameTime gameTime = CEngine.CEngine.Instance.gameTime;
            if (gameTime.ElapsedGameTime.Ticks == 0)
                return;                

            if ((new TimeSpan(gameTime.TotalGameTime.Ticks) - new TimeSpan(lastSavestate)).TotalSeconds > SavestateInterval)
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
                    }, Savestate.EListMode.INCLUDE, saveModule: false);
            }
        }

        public void PostUpdate()
        {
            if (!Velo.Ingame)
                return;
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
                    deltaSum: recording.Count >= 1 ? recording[recording.Count - 1].DeltaSum + recording[recording.Count - 1].Delta : 0L
                ),
                nextSavestate
            );

            recording.Stats.TravDist += (Velo.MainPlayer.actor.Position - prevPos).Length();
            recording.Stats.SpeedSum += Velo.MainPlayer.actor.Velocity.Length();
            if (prevJumpTime != Velo.MainPlayer.timespan1.Ticks)
                recording.Stats.Jumps++;
            if (prevGrapTime != Velo.MainPlayer.timespan6.Ticks)
                recording.Stats.Grapples++;
            if (Velo.MainPlayer.boostPressed && !Velo.MainPlayer.on_wall)
            {
                bool usedBoost = false;
                if (
                    Velo.MainPlayer.on_ground &&
                    !(Velo.MainPlayer.rightPressed && !Velo.MainPlayer.leftPressed && Velo.MainPlayer.actor.Velocity.X < 0f) &&
                    !(Velo.MainPlayer.leftPressed && !Velo.MainPlayer.rightPressed && Velo.MainPlayer.actor.Velocity.X > 0f))
                    usedBoost = true;
                if (
                    Velo.MainPlayer.in_air &&
                    !Velo.MainPlayer.grapple.connected &&
                    (Velo.MainPlayer.boost_cd <= 0f || Math.Abs(Velo.MainPlayer.actor.Velocity.X) <= 600f || Velo.MainPlayer.using_boost) &&
                    Velo.MainPlayer.wall_cd <= 0f)
                    usedBoost = true;
                if (usedBoost)
                    recording.Stats.BoostUsed += Math.Min(0.85 * CEngine.CEngine.Instance.gameTime.ElapsedGameTime.TotalSeconds, prevBoost);
            }

            nextSavestate = null;
            recording.TrimBeginning(MaxFrames);
        }

        public void SetLapStartToBack()
        {
            if (recording != null)
            {
                recording.LapStart = recording.Count - 1;
                recording.ClearStats();
            }
        }
    }

    public class Playback
    {
        public enum EPlaybackType
        {
            SET_GHOST, VIEW_REPLAY, VERIFY
        }

        private Recording recording;
        private int i = 0;
        private long deltaSum = 0;

        private double discrepancy = 0d;
        private TimeSpan lastNotificationUpdate = TimeSpan.Zero;

        public EPlaybackType Type;

        public Playback()
        {

        }

        public void Start(Recording recording, EPlaybackType type)
        {
            this.recording = recording;
            Type = type;
            Restart();
        }

        public void Restart()
        {
            if (recording != null)
                recording.Savestates[0].Load(setGlobalTime: Type == EPlaybackType.VERIFY, ghostOnly: Type == EPlaybackType.SET_GHOST);
            Velo.ModuleSolo.camera1.position = Velo.MainPlayer.actor.Bounds.Center + new Vector2(0f, -100f);
            deltaSum = 0;
            i = 0;
            discrepancy = 0d;
        }

        public void Stop()
        {
            recording = null;
        }

        public bool DtFixed
        {
            get
            {
                return !Finished && Type == EPlaybackType.VERIFY;
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
            if (!Velo.Ingame)
                return;
            if (Finished)
                return;

            GameTime gameTime = CEngine.CEngine.Instance.gameTime;
            if (gameTime.ElapsedGameTime.Ticks == 0)
                return;

            if (Type == EPlaybackType.VERIFY)
            {                
                CEngine.CEngine cengine = CEngine.CEngine.Instance;
                long delta = recording[i].Delta;
                long time = recording[i].Time;
                cengine.gameTime = new GameTime(new TimeSpan(time), new TimeSpan(delta));

                TimeSpan now = new TimeSpan(DateTime.Now.Ticks);
                if ((now - lastNotificationUpdate).TotalSeconds > 0.25)
                {
                    lastNotificationUpdate = now;

                    double avgFramerate = i > 0 ? 1.0 / new TimeSpan((recording[i].DeltaSum - recording[0].DeltaSum) / i).TotalSeconds : 0d;

                    Notifications.Instance.ForceNotification("discrepancy: " + (int)discrepancy + "\navg. framerate: " + (int)avgFramerate);
                }
            }
        }

        public void PostUpdate()
        {
            if (!Velo.Ingame)
                return;
            if (Finished)
                return;

            GameTime gameTime = CEngine.CEngine.Instance.gameTime;
            if (gameTime.ElapsedGameTime.Ticks == 0)
                return;

            if (Type == EPlaybackType.VERIFY)
            {
                double curDiscrepancy = (new Vector2(recording[i].PosX, recording[i].PosY) - Velo.MainPlayer.actor.Position).Length();
                discrepancy += curDiscrepancy;
                if (curDiscrepancy >= 1d)
                    recording[i].Apply(Velo.MainPlayer, 0);
                i++;
                if (i >= recording.Count)
                {
                    Stop();
                    return;
                }
            }
            else if ((Type == EPlaybackType.VIEW_REPLAY || Type == EPlaybackType.SET_GHOST) && i >= 1)
            {
                Player player = Type == EPlaybackType.SET_GHOST ? Velo.Ghost : Velo.MainPlayer;

                long frameDelta = recording[i].Delta;
                long nowRel = deltaSum - recording[i].DeltaSum;
                if (nowRel < 0)
                    nowRel = 0;
                if (nowRel > frameDelta)
                    nowRel = frameDelta;

                Frame frame = Lerp(recording[i - 1], recording[i], (float)((double)nowRel / frameDelta));

                long dt = CEngine.CEngine.Instance.gameTime.TotalGameTime.Ticks - recording[i - 1].Time;
                bool forceGrapple =
                    (!player.grappling &&
                    (recording[i - 1].Flags & (1 << (int)EFlags.GRAPPLING)) != 0 &&
                    (recording[i].Flags & (1 << (int)EFlags.GRAPPLING)) != 0) ||
                    (!player.grapple.connected &&
                    (recording[i - 1].Flags & (1 << (int)EFlags.SWINGING)) != 0 &&
                    (recording[i].Flags & (1 << (int)EFlags.SWINGING)) != 0);

                frame.Apply(player, dt, setFlags: Type == EPlaybackType.SET_GHOST, forceGrapple: forceGrapple || Type == EPlaybackType.SET_GHOST);

                player.UpdateHitbox();
                player.UpdateSprite(CEngine.CEngine.Instance.gameTime);
            }
        }

        public bool SetInputs()
        {
            if (Finished)
                return false;

            if (Type == EPlaybackType.VIEW_REPLAY || Type == EPlaybackType.SET_GHOST)
            {
                deltaSum += CEngine.CEngine.Instance.gameTime.ElapsedGameTime.Ticks;

                while (recording[i].DeltaSum + recording[i].Delta - recording[0].DeltaSum < deltaSum)
                {
                    i++;
                    if (i >= recording.Count)
                    {
                        Stop();
                        return false;
                    }
                }
            }

            SetInputs(Type == EPlaybackType.SET_GHOST ? Velo.Ghost : Velo.MainPlayer);

            return Type != EPlaybackType.SET_GHOST;
        }

        private void SetInputs(Player player)
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
            return !Finished && Type == EPlaybackType.SET_GHOST;
        }

        public bool SkipUpdateSprite(Player player)
        {
            if (Finished)
                return false;
            if (Type == EPlaybackType.SET_GHOST)
                return player == Velo.Ghost;
            if (Type == EPlaybackType.VIEW_REPLAY)
                return player == Velo.MainPlayer;
            return false;
        }
    }
}
