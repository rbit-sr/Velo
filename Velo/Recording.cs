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
            LEFT_P, RIGHT_P, JUMP_P, GRAPPLE_P, SLIDE_P, BOOST_P, ITEM_P, ITEM_P2, TAUNT_P,
            MOVE_DIR, ON_GROUND, STUNNED, TAUNT, ON_WALL, IN_AIR, SLIDING,
            CONNECTED, GRAPPLING, SWINGING, 
            ROPE_ACTIVE, ROPE_BREAKING, ROPE_OWNER, ROPE_LINES
        }

        public long RealTime;
        public long Delta;
        public long Time;
        public long DeltaSum;
        public long JumpTime;
        public float PosX;
        public float PosY;
        public float VelX;
        public float VelY;
        public float Boost;
        public short JumpState;
        public short Unknown27;
        public float GrapPosX;
        public float GrapPosY;
        public float GrapRad;
        public int Flags;

        public Frame(Player player, TimeSpan realTime, TimeSpan gameDelta, TimeSpan gameTime, long deltaSum)
        {
            RealTime = realTime.Ticks;
            Delta = gameDelta.Ticks;
            Time = gameTime.Ticks;
            DeltaSum = deltaSum;
            JumpTime = player.timespan1.Ticks;
            PosX = player.actor.Position.X;
            PosY = player.actor.Position.Y;
            VelX = player.actor.Velocity.X;
            VelY = player.actor.Velocity.Y;
            Boost = player.boost;
            JumpState = (short)player.jump_state;
            Unknown27 = (short)player.unknown27;
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
                    (player.item_p2 ? (1 << (int)EFlags.ITEM_P2) : 0) |
                    (player.tauntPressed ? (1 << (int)EFlags.TAUNT_P) : 0) |
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
                    (player.rope.active ? (1 << (int)EFlags.ROPE_ACTIVE) : 0) |
                    (player.rope.breaking ? (1 << (int)EFlags.ROPE_BREAKING) : 0) |
                    (player.rope.owner != null ? (1 << (int)EFlags.ROPE_OWNER) : 0) |
                    (player.rope.lineDrawComp1.Lines.Count > 0 ? (1 << (int)EFlags.ROPE_LINES) : 0)
                );
        }

        public void Apply(Player player, long dt, bool setFlags = false, bool forceGrapple = false, float grapDirX = 0f)
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
                player.unknown27 = Unknown27;
                player.move_dir = (Flags & (1 << (int)EFlags.MOVE_DIR)) != 0 ? 1 : -1;
                player.on_ground = (Flags & (1 << (int)EFlags.ON_GROUND)) != 0;
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
                dir.X = grapDirX;
                if (player.grappling && !player.swinging)
                    player.grapple.Shoot(new Vector2(GrapPosX, GrapPosY), dir);
                else if (player.swinging)
                    player.grapple.Connect(new Vector2(GrapPosX, GrapPosY));
                else
                    player.grapple.Remove();
                player.rope.active = (Flags & (1 << (int)EFlags.ROPE_ACTIVE)) != 0;
                player.rope.breaking = (Flags & (1 << (int)EFlags.ROPE_BREAKING)) != 0;
                if (!player.rope.breaking)
                    player.rope.breakLength = 0f;
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
                player.rope.lineDrawComp1.Lines.Clear();
                if (ropeLines)
                {
                    player.rope.lineDrawComp1.Lines.Add(player.rope.line1);
                    player.rope.lineDrawComp1.Lines.Add(player.rope.line2);
                }
                player.rope.UpdateLines();
            }
            CheatEngineDetection.MatchValues();
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
            player.item_p2 = (Flags & (1 << (int)EFlags.ITEM_P2)) != 0;
            player.tauntPressed = (Flags & (1 << (int)EFlags.TAUNT_P)) != 0;
            player.unknown27 = Unknown27;
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
        public double Dist;
        public double GroundDist;
        public double SwingDist;
        public double ClimbDist;
        public double SpeedSum;
        public int Grapples;
        public int Jumps;
        public double BoostUsed;
    }

    public class Recording
    {
        public static readonly int VERSION = 2;

        public CircArray<Frame> Frames;
        public CircArray<Savestate> Savestates;
        public RunInfo Info;
        public int LapStart;
        public List<MacroDetection.Timing> Timings;
        public byte[] Sign;

        public RulesChecker Rules;
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
            LapStart = 0;
            ClearStats();
        }

        public void ClearStats()
        {
            Stats = default;
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

        public void Finish(float time)
        {
            TrimBeginningFromLapStart(4f);
            Rules.Finish(time);
            Info.PlayerId = Steamworks.SteamUser.GetSteamID().m_SteamID;
            Info.Category.MapId = Rules.MapId;
            Info.Category.TypeId = (ulong)Rules.CategoryType;
            Info.RunTime = (int)(time * 1000f);
            Info.CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            Info.NewGCD = OfflineGameMods.Instance.GrappleCooldown.Value == 0.2f ? (byte)1 : (byte)0;
            Info.Dist = (int)(Stats.Dist + 0.5);
            Info.GroundDist = (int)(Stats.GroundDist + 0.5);
            Info.SwingDist = (int)(Stats.SwingDist + 0.5);
            Info.ClimbDist = (int)(Stats.ClimbDist + 0.5);
            Info.AvgSpeed = (short)(Stats.SpeedSum / (Frames.Count - LapStart) + 0.5);
            Info.Jumps = (short)Stats.Jumps;
            Info.Grapples = (short)Stats.Grapples;
            Info.BoostUsed = (short)(Stats.BoostUsed * 50d + 0.5);
            Timings = MacroDetection.Instance.GetTimings();
        }

        public void TrimBeginning(int maxFrames)
        {
            while (Savestates.Count > 0)
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
            while (Savestates.Count > 0)
            {
                long time = Frames[0].Time;
                float duration = (float)(new TimeSpan(lapStartTime) - new TimeSpan(time)).TotalSeconds;
                if (Savestates[0] != null && duration <= seconds)
                    break;
                PopFront();
            }
        }

        [DllImport("VeloVerifier.dll", EntryPoint = "generate_sign")]
        private static extern void generate_sign(IntPtr salt, int salt_len, IntPtr sign);

        public byte[] ToBytes(bool compress = true)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(VERSION | (compress ? 0 : 1 << 31));

            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            Stream raw = compress ? new MemoryStream() : stream;

            raw.Write(Info);

            raw.Write(LapStart);

            raw.Write(Count * Marshal.SizeOf<Frame>());

            for (int i = 0; i < Count; i++)
            {
                raw.Write(Frames[i]);
            }

            raw.Write((int)Savestates[0].Stream.Length);
            byte[] buffer = new byte[Savestates[0].Stream.Length];
            Savestates[0].Stream.Position = 0;
            Savestates[0].Stream.Read(buffer, 0, buffer.Length);
            raw.Write(buffer, 0, buffer.Length);

            if (LapStart != 0 && Savestates[LapStart] != null)
            {
                raw.Write((int)Savestates[LapStart].Stream.Length);
                buffer = new byte[Savestates[LapStart].Stream.Length];
                Savestates[LapStart].Stream.Position = 0;
                Savestates[LapStart].Stream.Read(buffer, 0, buffer.Length);
                raw.Write(buffer, 0, buffer.Length);
            }

            raw.Write(Timings.Count);
            foreach (MacroDetection.Timing timing in Timings)
            {
                raw.Write(timing);
            }

            if (compress)
            {
                encoder.WriteCoderProperties(stream);
                stream.Write(BitConverter.GetBytes((int)raw.Length), 0, sizeof(int));
                try
                {
                    raw.Position = 0;
                    encoder.Code(raw, stream, raw.Length, -1, null);
                }
                catch (Exception) // 7zip can throw an exception for unknown reasons, try again without compression
                {
                    raw.Close();
                    stream.Seek(stream.Position - 13, SeekOrigin.Begin);
                    return ToBytes(compress: false);
                }
                raw.Close();
            }
            return stream.ToArray();
        }

        public void GenerateSign(byte[] bytes)
        {
            Sign = new byte[32];
            if (System.Diagnostics.Debugger.IsAttached)
                return;
            try
            {
                unsafe
                {
                    fixed (byte* dataBytes = bytes)
                    {
                        fixed (byte* signBytes = Sign)
                        {
                            generate_sign((IntPtr)dataBytes, bytes.Length, (IntPtr)signBytes);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public void Write(Stream stream, bool compress = true)
        {
            byte[] bytes = ToBytes(compress);
            stream.Write(bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        public void Read(Stream stream)
        {
            int prefix = stream.Read<int>(); // recording may or may not be prefixed by the filesize (a .srrec file from the server is, a local .srrec file is not), just a little flaw in this file format
            int versionInt;
            if ((prefix & ~(1 << 31)) < 128)
                versionInt = prefix;
            else
                versionInt = stream.Read<int>();

            bool compressed = (versionInt & (1 << 31)) == 0;
            versionInt &= ~(1 << 31);

            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
            if (compressed)
            {
                byte[] properties = new byte[5];
                stream.ReadExactly(properties, 0, properties.Length);
                decoder.SetDecoderProperties(properties);
            }

            Stream raw = compressed ? new MemoryStream() : stream;

            if (compressed)
            {
                int dataSize = stream.Read<int>();

                decoder.Code(stream, raw, stream.Length, dataSize, null);
                raw.Position = 0;
            }

            if (versionInt == 0)
                Info = raw.Read<RunInfoBWC1>().Get();
            else
                Info = raw.Read<RunInfo>();

            LapStart = raw.Read<int>();

            int size = raw.Read<int>();
            int frameCount = size;
            if (versionInt <= 1)
                frameCount /= Marshal.SizeOf<FrameBWC1>();
            else
                frameCount /= Marshal.SizeOf<Frame>();
            for (int i = 0; i < frameCount; i++)
            {
                Frame frame;
                if (versionInt <= 1)
                    frame = raw.Read<FrameBWC1>().Get();
                else
                    frame = raw.Read<Frame>();
                PushBack(frame, null);
            }

            Savestates[0] = new Savestate();

            size = raw.Read<int>();
            byte[] buffer = new byte[size];
            raw.ReadExactly(buffer, 0, size);
            Savestates[0].Stream.Position = 0;
            Savestates[0].Stream.Write(buffer, 0, size);

            if (LapStart != 0)
            {
                Savestates[LapStart] = new Savestate();

                size = raw.Read<int>();
                buffer = new byte[size];
                raw.ReadExactly(buffer, 0, size);
                Savestates[LapStart].Stream.Position = 0;
                Savestates[LapStart].Stream.Write(buffer, 0, size);
            }

            if (versionInt >= 2)
            {
                size = raw.Read<int>();
                Timings = new List<MacroDetection.Timing>();
                for (int i = 0; i < size; i++)
                {
                    Timings.Add(raw.Read<MacroDetection.Timing>());
                }

                if (raw.Position < raw.Length)
                {
                    Sign = new byte[32];
                    raw.ReadExactly(Sign, 0, Sign.Length);
                }
            }

            raw.Close();
        }
    }

    public class Recorder
    {
        private Recording recording;
        private long lastSavestate = 0;
        private Savestate nextSavestate = null;

        public int MaxFrames = 40 * 60 * 300;
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
            recording?.Clear();
            lastSavestate = 0;
        }

        public void Stop()
        {
            recording = null;
        }

        public void PreUpdate()
        {
            if (Velo.MainPlayer == null || Velo.MainPlayer.destroyed)
                return;
            prevPos = Velo.MainPlayer.actor.Position;
            prevBoost = Velo.MainPlayer.boost;
            prevJumpTime = Velo.MainPlayer.timespan1.Ticks;
            prevGrapTime = Velo.MainPlayer.timespan6.Ticks;
            if (recording == null)
                return;

            if (Velo.GameDelta.Ticks == 0 || Velo.PauseMenu)
                return;                

            if ((new TimeSpan(Velo.GameTime.Ticks) - new TimeSpan(lastSavestate)).TotalSeconds > SavestateInterval)
            {
                lastSavestate = Velo.GameTime.Ticks;
                nextSavestate = new Savestate();
                nextSavestate.Save(new List<Savestate.ActorType> { Savestate.ATAIVolume }, Savestate.EListMode.EXCLUDE, moduleProgressOnly: true);
            }
        }

        public void PostUpdate()
        {
            if (Velo.MainPlayer == null || Velo.MainPlayer.destroyed)
                return;
            if (recording == null)
                return;

            if (Velo.GameDelta.Ticks == 0 || Velo.PauseMenuPrev)
                return;

            Player player = Velo.MainPlayer;

            recording.PushBack(
                new Frame(
                    player: player, 
                    realTime: Velo.RealTime,
                    gameDelta: Velo.GameDelta,
                    gameTime: Velo.GameTime, 
                    deltaSum: recording.Count >= 1 ? recording[recording.Count - 1].DeltaSum + recording[recording.Count - 1].Delta : 0L
                ),
                nextSavestate
            );

            recording.Stats.Dist += (Velo.MainPlayer.actor.Position - prevPos).Length();
            if (Velo.MainPlayer.on_ground)
                recording.Stats.GroundDist += (Velo.MainPlayer.actor.Position - prevPos).Length();
            if (Velo.MainPlayer.swinging)
                recording.Stats.SwingDist += (Velo.MainPlayer.actor.Position - prevPos).Length();
            if (Velo.MainPlayer.on_wall)
                recording.Stats.ClimbDist += (Velo.MainPlayer.actor.Position - prevPos).Length();
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
                    recording.Stats.BoostUsed += Math.Min(0.85 * Velo.GameDelta.TotalSeconds, prevBoost);
            }

            nextSavestate = null;
            recording.TrimBeginning(MaxFrames);
        }

        public void SetLapStartToBack()
        {
            if (recording != null)
            {
                recording.LapStart = recording.Count;
                recording.ClearStats();
                lastSavestate = 0;
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
        private Player player;
        private int i = 0;
        private int startI = 0;
        private long deltaSum = 0;
        private float holdingTime = 0f;

        private double discrepancy = 0d;
        private TimeSpan lastNotificationUpdate = TimeSpan.Zero;

        public EPlaybackType Type;
        public int GhostIndex;

        public Action<Recording, EPlaybackType> OnFinish;

        private readonly Savestate savestatePreVerify = new Savestate();
        
        public Playback()
        {
            Finished = true;
        }

        public Recording Recording { get => recording; }

        public void Start(Recording recording, EPlaybackType type, int ghostIndex)
        {
            if (type == EPlaybackType.VERIFY && (Finished || Type != EPlaybackType.VERIFY))
                savestatePreVerify.Save(new List<Savestate.ActorType> { Savestate.ATAIVolume }, Savestate.EListMode.EXCLUDE);
            else if (Type == EPlaybackType.VERIFY && !Finished && type != EPlaybackType.VERIFY)
                savestatePreVerify.Load(true);

            this.recording = recording;
            Type = type;
            GhostIndex = ghostIndex;
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
                    player = Velo.MainPlayer;
                    break;
                case EPlaybackType.SET_GHOST:
                    player = Ghosts.Instance.Get(GhostIndex);
                    break;
            }

            if (player == null)
                return;

            Finished = false;
            startI = 0;
            if (Type == EPlaybackType.SET_GHOST)
                startI = recording.LapStart;
            i = startI;

            recording.Savestates[i].Load(setGlobalTime: Type == EPlaybackType.VERIFY, GhostIndex);
            if (recording.Info.Id >= 0)
                Savestate.LoadedVersion = Version.VERSION; // assume runs from the leaderboard are fine
            if (Type == EPlaybackType.VIEW_REPLAY || Type == EPlaybackType.VERIFY)
                Velo.ModuleSolo.camera1.position = Velo.MainPlayer.actor.Bounds.Center + new Vector2(0f, -100f);
            deltaSum = 0;
            discrepancy = 0d;
        }

        public void Stop()
        {
            recording = null;
            Finished = true;
            if (Type == EPlaybackType.VERIFY)
                savestatePreVerify.Load(true);
        }

        public void Finish()
        {
            Finished = true;
            if (Type == EPlaybackType.VERIFY)
                savestatePreVerify.Load(true);
            OnFinish?.Invoke(recording, Type);
        }

        public void Jump(float seconds, bool hold = false)
        {
            if (Finished)
                return;

            if (Type == EPlaybackType.VIEW_REPLAY || Type == EPlaybackType.SET_GHOST)
            {
                if (seconds < 0f)
                {
                    if (i == 0)
                    {
                        if (hold)
                            holdingTime = -seconds;
                        return;
                    }

                    long rewinded = 0;

                    while (recording[i].DeltaSum + recording[i].Delta - recording[startI].DeltaSum >= deltaSum - TimeSpan.FromSeconds(-seconds).Ticks)
                    {
                        i--;
                        rewinded += recording[i].Delta;
                        if (i == 0)
                            break;
                    }
                    deltaSum -= rewinded;
                }
                if (seconds > 0f)
                {
                    long forwarded = 0;

                    while (recording[i].DeltaSum + recording[i].Delta - recording[startI].DeltaSum < deltaSum + TimeSpan.FromSeconds(seconds).Ticks)
                    {
                        i++;
                        forwarded += recording[i].Delta;
                        if (i >= recording.Count)
                        {
                            Finish();
                            return;
                        }
                    }
                    deltaSum += forwarded;
                }
            }
            if (Type == EPlaybackType.VIEW_REPLAY)
            {
                Velo.ModuleSolo.camera1.position = Velo.MainPlayer.actor.Bounds.Center + new Vector2(0f, -100f);
            }
        }

        public bool DtFixed
        {
            get
            {
                return !Finished && Type == EPlaybackType.VERIFY;
            }
        }

        public bool Finished { get; set; }

        public void PreUpdate()
        {
            if (!Velo.Ingame)
                return;
            if (Finished)
                return;
            if (player.destroyed)
                return;

            if (Velo.GameDelta.Ticks == 0 || Velo.PauseMenu)
                return;

            if (holdingTime > 0f)
            {
                holdingTime = Math.Max(holdingTime - (float)Velo.GameDelta.TotalSeconds, 0f);
                return;
            }

            if (Type == EPlaybackType.VERIFY)
            {                
                CEngine.CEngine cengine = CEngine.CEngine.Instance;
                long delta = recording[i].Delta;
                long time = recording[i].Time;
                cengine.gameTime = new GameTime(new TimeSpan(time), new TimeSpan(delta));

                TimeSpan now = new TimeSpan(Velo.RealTime.Ticks);
                if ((now - lastNotificationUpdate).TotalSeconds > 0.25)
                {
                    lastNotificationUpdate = now;

                    double avgFramerate = i > 0 ? 1.0 / new TimeSpan((recording[i].DeltaSum - recording[startI].DeltaSum) / (i - startI)).TotalSeconds : 0d;

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
            if (player.destroyed)
                return;

            if (Velo.GameDelta.Ticks == 0 || Velo.PauseMenuPrev)
                return;

            if (holdingTime > 0f)
                return;

            if (Type == EPlaybackType.VERIFY)
            {
                double curDiscrepancy = (new Vector2(recording[i].PosX, recording[i].PosY) - player.actor.Position).Length();
                discrepancy += curDiscrepancy;
                if (curDiscrepancy >= 1d)
                    recording[i].Apply(player, 0);
                i++;
                if (i >= recording.Count)
                {
                    Finish();
                    return;
                }
            }
            else if ((Type == EPlaybackType.VIEW_REPLAY || Type == EPlaybackType.SET_GHOST) && i >= 1 && i < recording.Count)
            {
                long frameDelta = recording[i].Delta;
                long nowRel = deltaSum - (recording[i].DeltaSum - recording[startI].DeltaSum);
                if (nowRel < 0)
                    nowRel = 0;
                if (nowRel > frameDelta)
                    nowRel = frameDelta;

                Frame frame = Lerp(recording[i - 1], recording[i], (float)((double)nowRel / frameDelta));

                long dt = Velo.GameTime.Ticks - recording[i - 1].Time;
                bool forceGrapple =
                    (!player.grappling &&
                    (recording[i - 1].Flags & (1 << (int)EFlags.GRAPPLING)) != 0 &&
                    (recording[i].Flags & (1 << (int)EFlags.GRAPPLING)) != 0) ||
                    (!player.grapple.connected &&
                    (recording[i - 1].Flags & (1 << (int)EFlags.SWINGING)) != 0 &&
                    (recording[i].Flags & (1 << (int)EFlags.SWINGING)) != 0);

                float grapDirX = recording[i].GrapPosX > recording[i - 1].GrapPosX ? 1f : (recording[i].GrapPosX < recording[i - 1].GrapPosX ? -1f : 0f);
                frame.Apply(player, dt, setFlags: true, forceGrapple: forceGrapple || Type == EPlaybackType.SET_GHOST, grapDirX: grapDirX);

                Velo.measure("physics");
                player.UpdateHitbox();
                player.UpdateSprite(CEngine.CEngine.Instance.gameTime);
                Velo.measure("Velo");
            }
        }

        public bool SetInputs(Player player)
        {
            if (Finished)
                return false;
            if (player != this.player)
                return false;

            if (Type == EPlaybackType.VIEW_REPLAY || Type == EPlaybackType.SET_GHOST)
            {
                if (holdingTime > 0f)
                    return true;

                deltaSum += Velo.GameDelta.Ticks;

                while (recording[i].DeltaSum + recording[i].Delta - recording[startI].DeltaSum < deltaSum)
                {
                    i++;
                    if (i >= recording.Count)
                    {
                        Finish();
                        return false;
                    }
                }
            }

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
            return true;
        }

        public bool SkipUpdateSprite(Player player)
        {
            if (Finished || Type == EPlaybackType.VERIFY)
                return false;
            return player == this.player;
        }
    }
}
