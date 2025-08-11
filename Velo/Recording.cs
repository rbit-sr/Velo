using CEngine.Graphics.Layer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Velo
{
    public struct Frame
    {
        public enum EFlag : int
        {
            LEFT_H, RIGHT_H, JUMP_H, GRAPPLE_H, SLIDE_H, BOOST_H, ITEM_H, UNUSED, TAUNT_H,
            MOVE_DIR, ON_GROUND, STUNNED, TAUNT, CLIMBING, IN_AIR, SLIDING,
            CONNECTED, GRAPPLING, SWINGING, 
            ROPE_ACTIVE, ROPE_BREAKING, ROPE_OWNER, ROPE_LINES,
            RESET_LAP, GRAPPLE_DIR
        }

        public TimeSpan RealTime;
        public TimeSpan Delta;
        public TimeSpan GlobalTime;
        public TimeSpan DeltaSum;
        public TimeSpan JumpTime;
        public float PosX;
        public float PosY;
        public float VelX;
        public float VelY;
        public float Boost;
        public short JumpState;
        public short DominatingDirection;
        public float GrapPosX;
        public float GrapPosY;
        public float SwingRadius;
        public int Flags;

        public bool GetFlag(EFlag flag)
        {
            return (Flags & (1 << (int)flag)) != 0;
        }

        public void SetFlag(EFlag flag, bool value)
        {
            if (value)
                Flags |= 1 << (int)flag;
            else
                Flags &= ~(1 << (int)flag);
        }

        public Frame(Player player, TimeSpan realTime, TimeSpan gameDelta, TimeSpan gameTime, TimeSpan deltaSum)
        {
            RealTime = realTime;
            Delta = gameDelta;
            GlobalTime = gameTime;
            DeltaSum = deltaSum;
            JumpTime = player.jumpTime;
            PosX = player.actor.Position.X;
            PosY = player.actor.Position.Y;
            VelX = player.actor.Velocity.X;
            VelY = player.actor.Velocity.Y;
            Boost = player.boost;
            JumpState = (short)player.jumpState;
            DominatingDirection = (short)player.dominatingDirection;
            GrapPosX = player.grapple.actor.Position.X;
            GrapPosY = player.grapple.actor.Position.Y;
            SwingRadius = player.swingRadius;
            Flags = (
                (player.moveDirection == 1 ? (1 << (int)EFlag.MOVE_DIR) : 0) |
                (player.onGround ? (1 << (int)EFlag.ON_GROUND) : 0) |
                (player.stunned ? (1 << (int)EFlag.STUNNED) : 0) |
                (player.taunting ? (1 << (int)EFlag.TAUNT) : 0) |
                (player.climbing ? (1 << (int)EFlag.CLIMBING) : 0) |
                (player.inAir ? (1 << (int)EFlag.IN_AIR) : 0) |
                (player.sliding ? (1 << (int)EFlag.SLIDING) : 0) |
                (player.grapple.connected ? (1 << (int)EFlag.CONNECTED) : 0) |
                (player.grappling ? (1 << (int)EFlag.GRAPPLING) : 0) |
                (player.swinging ? (1 << (int)EFlag.SWINGING) : 0) |
                (player.rope.active ? (1 << (int)EFlag.ROPE_ACTIVE) : 0) |
                (player.rope.breaking ? (1 << (int)EFlag.ROPE_BREAKING) : 0) |
                (player.rope.owner != null ? (1 << (int)EFlag.ROPE_OWNER) : 0) |
                (player.rope.lineDrawComp1.Lines.Count > 0 ? (1 << (int)EFlag.ROPE_LINES) : 0) |
                (player.grapple.direction.X > 0f ? (1 << (int)EFlag.GRAPPLE_DIR) : 0)
            );
        }

        public void Apply(Player player, TimeSpan dt)
        {
            player.actor.Position = new Vector2(PosX, PosY);
            player.actor.Velocity = new Vector2(VelX, VelY);
            
            player.swingRadius = SwingRadius;
            player.boost = Boost;
            player.jumpTime = JumpTime + dt;

            player.jumpState = JumpState;
            player.dominatingDirection = DominatingDirection;
            player.moveDirection = GetFlag(EFlag.MOVE_DIR) ? 1 : -1;
            player.onGround = GetFlag(EFlag.ON_GROUND);
            player.climbing = GetFlag(EFlag.CLIMBING);
            player.inAir = GetFlag(EFlag.IN_AIR);
            player.sliding = GetFlag(EFlag.SLIDING);
            player.stunned = GetFlag(EFlag.STUNNED);
            player.taunting = GetFlag(EFlag.TAUNT);
            //player.grapple.connected = GetFlag(EFlag.CONNECTED);
            //player.grappling = GetFlag(EFlag.GRAPPLING);
            //player.swinging = GetFlag(EFlag.SWINGING);
            if (player.climbing)
            {
                player.onWall = true;
                player.wallGetOffTime = TimeSpan.Zero;
            }

            CheatEngineDetection.MatchValues();
        }

        public void ApplyGrapple(Player player)
        {
            if (!float.IsNaN(GrapPosX) && !float.IsNaN(GrapPosY))
            {
                player.grapple.actor.Position = new Vector2(GrapPosX, GrapPosY);
                player.grapple.sprite.Position = player.grapple.actor.Position + (GetFlag(EFlag.SWINGING) ? new Vector2(0f, -5f) : Vector2.Zero);
            }

            Vector2 dir = new Vector2(GetFlag(EFlag.GRAPPLE_DIR) ? 0.707f : -0.707f, -0.707f);

            if (GetFlag(EFlag.GRAPPLING) && !player.grappling)
            {
                player.grappling = true;
                player.grappleTime = player.game_time.TotalGameTime;
                player.canGrapple = false;
                player.grapple.Shoot(new Vector2(GrapPosX, GrapPosY), dir);
            }
            if (GetFlag(EFlag.CONNECTED) && !player.grapple.connected)
            {
                player.grappling = true;
                player.grapple.Connect(new Vector2(GrapPosX, GrapPosY));
            }
            if (!GetFlag(EFlag.GRAPPLING) && player.grappling)
            {
                player.grappling = false;
                player.swinging = false;
                player.grapple.Remove();
            }

            player.grapple.connected = GetFlag(EFlag.CONNECTED);
            player.grapple.sprite.IsVisible = GetFlag(EFlag.GRAPPLING);
            player.grapple.sprite.Rotation = GetFlag(EFlag.CONNECTED) ? 0f : (float)(Math.Atan2(dir.Y, dir.X) - Math.Atan2(-1d, 0d));

            player.swinging = GetFlag(EFlag.SWINGING);

            player.rope.active = GetFlag(EFlag.ROPE_ACTIVE);
            player.rope.breaking = GetFlag(EFlag.ROPE_BREAKING);
            if (!player.rope.breaking)
                player.rope.breakLength = 0f;
            bool ropeOwner = GetFlag(EFlag.ROPE_OWNER);
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
            bool ropeLines = GetFlag(EFlag.ROPE_LINES);
            player.rope.lineDrawComp1.Lines.Clear();
            if (ropeLines)
            {
                player.rope.lineDrawComp1.Lines.Add(player.rope.line1);
                player.rope.lineDrawComp1.Lines.Add(player.rope.line2);
            }
            player.rope.UpdateLines();
            player.grapple.Update(null);
            Velo.update_rope_color(player.rope);
        }

        public void SetInputs(Player player)
        {
            Flags |=
                (player.leftHeld ? (1 << (int)EFlag.LEFT_H) : 0) |
                (player.rightHeld ? (1 << (int)EFlag.RIGHT_H) : 0) |
                (player.jumpHeld ? (1 << (int)EFlag.JUMP_H) : 0) |
                (player.grappleHeld ? (1 << (int)EFlag.GRAPPLE_H) : 0) |
                (player.slideHeld ? (1 << (int)EFlag.SLIDE_H) : 0) |
                (player.boostHeld ? (1 << (int)EFlag.BOOST_H) : 0) |
                (player.itemHeld ? (1 << (int)EFlag.ITEM_H) : 0) |
                (player.tauntHeld ? (1 << (int)EFlag.TAUNT_H) : 0);
        }

        public void ApplyInputs(Player player)
        {
            bool wasItemHeld = player.itemHeld;

            player.leftHeld = GetFlag(EFlag.LEFT_H);
            player.rightHeld = GetFlag(EFlag.RIGHT_H);
            player.jumpHeld = GetFlag(EFlag.JUMP_H);
            player.grappleHeld = GetFlag(EFlag.GRAPPLE_H);
            player.slideHeld = GetFlag(EFlag.SLIDE_H);
            player.boostHeld = GetFlag(EFlag.BOOST_H);
            player.itemHeld = GetFlag(EFlag.ITEM_H);
            player.itemPressed = player.itemHeld && !wasItemHeld;
            player.tauntHeld = GetFlag(EFlag.TAUNT_H);
            player.dominatingDirection = DominatingDirection;
        }

        public static Frame Lerp(Frame frame1, Frame frame2, float r)
        {
            bool grappleJustShot =
                !frame1.GetFlag(EFlag.GRAPPLING) &&
                frame1.GetFlag(EFlag.GRAPPLE_H);
            Frame frame = new Frame
            {
                PosX = (1f - r) * frame1.PosX + r * frame2.PosX,
                PosY = (1f - r) * frame1.PosY + r * frame2.PosY,
                //VelX = (1f - r) * frame1.VelX + r * frame2.VelX,
                //VelY = (1f - r) * frame1.VelY + r * frame2.VelY,
                VelX = frame1.VelX,
                VelY = frame1.VelY,
                Boost = (1f - r) * frame1.Boost + r * frame2.Boost,
                GrapPosX = grappleJustShot ? float.NaN : frame1.GetFlag(EFlag.CONNECTED) ? frame1.GrapPosX : (1f - r) * frame1.GrapPosX + r * frame2.GrapPosX,
                GrapPosY = grappleJustShot ? float.NaN : frame1.GetFlag(EFlag.CONNECTED) ? frame1.GrapPosY : (1f - r) * frame1.GrapPosY + r * frame2.GrapPosY,
                SwingRadius = frame1.SwingRadius,
                JumpState = frame1.JumpState,
                JumpTime = frame1.JumpTime,
                Flags = frame1.Flags
            };
            
            if (frame1.GetFlag(EFlag.CLIMBING))
            {
                frame.PosX = frame1.PosX;
            }

            return frame;
        }
    }

    public class Recording : IReplayable
    {
        public static readonly int VERSION = 4;

        public CircArray<Frame> Frames;
        public CircArray<Savestate> Savestates;
        private RunInfo info;
        public RunInfo Info
        {
            get => info;
            set { info = value; }
        }
        public int LapStart { get; set; }
        public List<MacroDetection.Timing> Timings;
        public byte[] Sign;

        public RulesChecker Rules;

        public Recording()
        {
            Frames = new CircArray<Frame>();
            Savestates = new CircArray<Savestate>();
            info = default;
            Rules = new RulesChecker();
            Clear();
        }

        public Recording Clone()
        {
            return new Recording
            {
                Frames = Frames.Clone(),
                Savestates = Savestates.Clone(),
                info = info,
                Rules = Rules.Clone(),
                LapStart = LapStart
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
            RunInfo info = Info;
            info.Id = -1;
            LapStart = 0;
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
            LapStart = Math.Max(LapStart - 1, 0);
        }

        public bool LoadSavestate(int i, bool setGlobalTime, int ghostIndex = -1, Savestate.ResultFlags resultFlags = null)
        {
            if (Savestates[i] == null)
                return false;
            return Savestates[i].Load(setGlobalTime, ghostIndex, resultFlags);
        }

        public int Count { get { return Frames.Count; } }

        public void Finish(float time, StatsTracker statsTracker)
        {
            TrimBeginningFromLapStart(4f);
            Rules.Finish(time);
            info.PlayerId = Steamworks.SteamUser.GetSteamID().m_SteamID;
            info.Category.MapId = Rules.MapId;
            info.Category.TypeId = (ulong)Rules.CategoryType;
            info.RunTime = (int)(time * 1000f);
            info.CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (OfflineGameMods.Instance.GetGrappleCooldown() == 0.2f)
                info.PhysicsFlags |= RunInfo.FLAG_NEW_GCD;
            if (OfflineGameMods.Instance.GetFixBounceGlitch())
                info.PhysicsFlags |= RunInfo.FLAG_FIX_BOUNCE_GLITCH;
            statsTracker.Apply(ref info, Count - LapStart);
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
            TimeSpan lapStartTime = Frames[LapStart].GlobalTime;
            while (Savestates.Count > 0)
            {
                TimeSpan time = Frames[0].GlobalTime;
                float duration = (float)(lapStartTime - time).TotalSeconds;
                if (Savestates[0] != null && duration <= seconds)
                    break;
                PopFront();
            }
        }

        [DllImport("VeloVerifier.dll", EntryPoint = "generate_sign")]
        private static extern void generate_sign(IntPtr salt, int salt_len, IntPtr sign);

        public byte[] ToBytes(out int size, bool compress = true)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(VERSION | (compress ? 0 : 1 << 31));

            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            Stream raw = compress ? new MemoryStream() : stream;

            raw.Write(Info);

            raw.Write(LapStart);

            raw.Write(Count);

            for (int i = 0; i < Count; i++)
            {
                raw.Write(Frames[i]);
            }

            raw.Write((int)Savestates[0].Stream.Length);
            byte[] buffer = StreamUtil.GetBuffer((int)Savestates[0].Stream.Length);
            Savestates[0].Stream.Position = 0;
            Savestates[0].Stream.Read(buffer, 0, (int)Savestates[0].Stream.Length);
            raw.Write(buffer, 0, (int)Savestates[0].Stream.Length);

            if (LapStart != 0 && Savestates[LapStart] != null)
            {
                raw.Write((int)Savestates[LapStart].Stream.Length);
                buffer = StreamUtil.GetBuffer((int)Savestates[LapStart].Stream.Length);
                Savestates[LapStart].Stream.Position = 0;
                Savestates[LapStart].Stream.Read(buffer, 0, (int)Savestates[LapStart].Stream.Length);
                raw.Write(buffer, 0, (int)Savestates[LapStart].Stream.Length);
            }

            if (Timings == null)
                raw.Write(0);
            else
            {
                raw.Write(Timings.Count);
                foreach (MacroDetection.Timing timing in Timings)
                {
                    raw.Write(timing);
                }
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
                    stream.Close();
                    return ToBytes(out size, compress: false);
                }
                raw.Close();
            }
            size = (int)stream.Length;
            stream.Position = 0;
            buffer = StreamUtil.GetBuffer(size);
            stream.Read(buffer, 0, size);
            return buffer;
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
            byte[] bytes = ToBytes(out int size, compress);
            stream.Write(size);
            stream.Write(bytes, 0, size);
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
                info = raw.Read<RunInfoBWC1>().Get();
            else
                info = raw.Read<RunInfo>();

            LapStart = raw.Read<int>();

            int size = raw.Read<int>();
            int frameCount;
            if (versionInt <= 1)
                frameCount = size / Marshal.SizeOf<FrameBWC1>();
            else if (versionInt <= 2)
                frameCount = size / Marshal.SizeOf<Frame>();
            else
                frameCount = size;

            // In version 2 and prior, every frame stored the post update data
            // In the current version, every frame stores the pre update data
            if (versionInt <= 2)
                PushBack(new Frame { PosX = float.NaN }, null);
            for (int i = 0; i < frameCount; i++)
            {
                Frame frame;
                if (versionInt <= 1)
                    frame = raw.Read<FrameBWC1>().Get();
                else
                    frame = raw.Read<Frame>();

                PushBack(frame, null);
            }

            if (versionInt <= 2)
            {
                for (int i = 0; i < frameCount; i++)
                {
                    Frame frame = Frames[i];
                    frame.Delta = Frames[i + 1].Delta;
                    frame.GlobalTime = Frames[i + 1].GlobalTime;
                    frame.DeltaSum = Frames[i + 1].DeltaSum;
                    Frames[i] = frame;
                }
                frameCount++;
            }

            if (versionInt <= 3)
            {
                bool grappleDir = true;
                bool wasGrappling = false;
                for (int i = 0; i < frameCount; i++)
                {
                    Frame frame = Frames[i];
                    if (!wasGrappling && frame.GetFlag(Frame.EFlag.GRAPPLING))
                    {
                        grappleDir = frame.GetFlag(Frame.EFlag.MOVE_DIR);
                        if (frame.GetFlag(Frame.EFlag.LEFT_H) && !frame.GetFlag(Frame.EFlag.RIGHT_H))
                            grappleDir = false;
                        else if (frame.GetFlag(Frame.EFlag.RIGHT_H) && !frame.GetFlag(Frame.EFlag.LEFT_H))
                            grappleDir = true;
                        else if (frame.GetFlag(Frame.EFlag.RIGHT_H) && frame.GetFlag(Frame.EFlag.LEFT_H) && frame.DominatingDirection != 0)
                            grappleDir = frame.DominatingDirection == 1;
                    }
                    frame.SetFlag(Frame.EFlag.GRAPPLE_DIR, grappleDir);
                    Frames[i] = frame;
                }
            }

            Savestates[0] = new Savestate();

            size = raw.Read<int>();
            byte[] buffer = StreamUtil.GetBuffer(size);
            raw.ReadExactly(buffer, 0, size);
            Savestates[0].Stream.Position = 0;
            Savestates[0].Stream.Write(buffer, 0, size);

            if (LapStart != 0)
            {
                Savestates[LapStart] = new Savestate();

                size = raw.Read<int>();
                buffer = StreamUtil.GetBuffer(size);
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

            if (compressed)
                raw.Close();
        }

        public static Recording Load(string name)
        {
            string filename = $"Velo\\recordings\\{name}.srrec";
            if (!File.Exists(filename))
                return null;
            Recording recording = new Recording();

            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                recording.Read(stream);
            }
            return recording;
        }
    }
}
