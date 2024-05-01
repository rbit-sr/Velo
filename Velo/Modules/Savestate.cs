using CEngine.Graphics.Camera;
using CEngine.Graphics.Camera.Modifier;
using CEngine.Graphics.Component;
using CEngine.Graphics.Library;
using CEngine.Util.Draw;
using CEngine.World;
using CEngine.World.Actor;
using CEngine.World.Collision;
using CEngine.World.Collision.Shape;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using XNATweener;

namespace Velo
{
    public class MemUtil
    {
        public static unsafe void* GetPtr(object obj)
        {
            return *(void**)Unsafe.AsPointer(ref obj);
        }
    }

    public class NullSafeDict<TKey, TValue> : Dictionary<TKey, TValue> where TValue : class
    {
        public new TValue this[TKey key]
        {
            get
            {
                if (!ContainsKey(key))
                    return null;
                else
                    return base[key];
            }
            set
            {
                if (!ContainsKey(key))
                    Add(key, value);
                else
                    base[key] = value;
            }
        }
    }
    public class Chunk
    {
        public byte[] Data;
        public int index;

        public Chunk() {
            Data = new byte[0x1000];
            index = 0;
        }

        private void Ensure(int size)
        {
            if (Data.Length < size)
                Array.Resize(ref Data, Data.Length * 2);
        }

        public void Start() { index = 0; }

        public unsafe void Write(object src, int off, int data_size)
        {
            Ensure(index + data_size);
            void* data_src = (byte*)MemUtil.GetPtr(src) + off;
            fixed(byte* data_dst = Data)
            {
                Buffer.MemoryCopy(data_src, data_dst + index, data_size, data_size);
            }
            index += data_size;
        }

        public unsafe void Read(object dst, int off, int data_size)
        {
            void* data_dst = (byte*)MemUtil.GetPtr(dst) + off;
            fixed (byte* data_src = Data)
            {
                Buffer.MemoryCopy(data_src + index, data_dst, data_size, data_size);
            }
            index += data_size;
        }

        public unsafe void WriteInt(int value)
        {
            Ensure(index + 4);
            fixed (byte* data_dst = Data)
            {
                *(int*)(data_dst + index) = value;
            }
            index += 4;
        }

        public unsafe int ReadInt()
        {
            int value;
            fixed (byte* data_src = Data)
            {
                value = *(int*)(data_src + index);
            }
            index += 4;
            return value;
        }

        public unsafe void WriteLong(long value)
        {
            Ensure(index + 8);
            fixed (byte* data_dst = Data)
            {
                *(long*)(data_dst + index) = value;
            }
            index += 8;
        }

        public unsafe long ReadLong()
        {
            long value;
            fixed (byte* data_src = Data)
            {
                value = *(long*)(data_src + index);
            }
            index += 8;
            return value;
        }

        public unsafe void WriteBytes(byte* bytes, int size)
        {
            Ensure(index + size);

            fixed (byte* data_dst = Data)
            {
                Buffer.MemoryCopy(bytes, data_dst + index, size, size);
            }
            index += size;
        }

        public unsafe void ReadBytes(byte* bytes, int size)
        {
            fixed (byte* data_src = Data)
            {
                Buffer.MemoryCopy(data_src + index, bytes, size, size);
            }
            index += size;
        }

        public unsafe void WriteByteArr(byte[] arr)
        {
            if (arr == null)
            {
                WriteInt(-1);
                return;
            }

            WriteInt(arr.Length);

            fixed (byte* bytes = arr)
            {
                WriteBytes((byte*)bytes, arr.Length * sizeof(byte));
            }
        }

        public unsafe byte[] ReadByteArr()
        {
            int size = ReadInt();
            if (size == -1)
                return null;
            byte[] bytes = new byte[size];

            fixed (byte* data_dst = bytes)
            {
                ReadBytes((byte*)data_dst, size * sizeof(byte));
            }
            return bytes;
        }

        public unsafe void WriteBoolArr(bool[] arr)
        {
            if (arr == null)
            {
                WriteInt(-1);
                return;
            }

            WriteInt(arr.Length);

            byte[] packed = new byte[(arr.Length + 7) >> 3];
            for (int i = 0; i < arr.Length; i++)
                packed[i >> 3] |= (byte)((arr[i] ? 1 : 0) << (i & 7));

            fixed (byte* bytes = packed)
            {
                WriteBytes((byte*)bytes, packed.Length * sizeof(byte));
            }
        }

        public unsafe bool[] ReadBoolArr()
        {
            int size = ReadInt();
            if (size == -1)
                return null;

            bool[] bytes = new bool[size];
            byte[] packed = new byte[(bytes.Length + 7) >> 3];

            fixed (byte* data_dst = packed)
            {
                ReadBytes((byte*)data_dst, packed.Length * sizeof(byte));
            }

            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = ((packed[i >> 3] >> (i & 7)) & 1) == 1;

            return bytes;
        }

        public unsafe void WriteVector2Arr(Vector2[] arr)
        {
            if (arr == null)
            {
                WriteInt(-1);
                return;
            }

            WriteInt(arr.Length);

            fixed (Vector2* bytes = arr)
            {
                WriteBytes((byte*)bytes, arr.Length * sizeof(Vector2));
            }
        }

        public unsafe Vector2[] ReadVector2Arr()
        {
            int size = ReadInt();
            if (size == -1)
                return null;
            Vector2[] bytes = new Vector2[size];

            fixed (Vector2* data_dst = bytes)
            {
                ReadBytes((byte*)data_dst, size * sizeof(Vector2));
            }
            return bytes;
        }

        public unsafe void WriteStr(string str)
        {
            byte[] bytes = str != null ? Encoding.ASCII.GetBytes(str) : null;
            WriteByteArr(bytes);
        }

        public unsafe string ReadStr()
        {
            byte[] bytes = ReadByteArr();
            if (bytes == null)
                return null;
            return Encoding.ASCII.GetString(bytes);
        }
    }

    public class Savestate
    {
        private static unsafe void Write(Chunk chunk, CAABB obj)
        {
            chunk.Write(obj, 0x4, 0x18);
        }

        private static unsafe void Read(Chunk chunk, CAABB obj)
        {
            chunk.Read(obj, 0x4, 0x18);
        }

        private static unsafe void Write(Chunk chunk, CConvexPolygon obj)
        {
            chunk.Write(obj, 0xC, 0x1C);
            chunk.WriteVector2Arr(obj.localVertices);
            chunk.WriteVector2Arr(obj.vertices);
        }

        private static unsafe void Read(Chunk chunk, CConvexPolygon obj)
        {
            chunk.Read(obj, 0xC, 0x1C);
            obj.localVertices = chunk.ReadVector2Arr();
            obj.vertices = chunk.ReadVector2Arr();
        }

        private static unsafe void Write(Chunk chunk, CActor obj)
        {
            chunk.Write(obj, 0x20, 0x40);
            Write(chunk, obj.bounds);
        }

        private static unsafe void Read(Chunk chunk, CActor obj)
        {
            chunk.Read(obj, 0x20, 0x40);
            Read(chunk, obj.bounds);
            fixedIdActors.Add(obj);
            fixedIds.Add(obj.id);
        }

        private static unsafe void Write(Chunk chunk, CSpriteDrawComponent obj)
        {
            chunk.Write(obj, 0x20, 0x6C);
            Write(chunk, obj.bounds);
        }

        private static unsafe void Read(Chunk chunk, CSpriteDrawComponent obj)
        {
            chunk.Read(obj, 0x20, 0x6C);
            Read(chunk, obj.bounds);
        }

        private static unsafe void Write(Chunk chunk, Dictionary<string, CAnimation> obj)
        {
            chunk.WriteInt(obj.Count);
            foreach (var pair in obj)
            {
                chunk.WriteStr(pair.Key);
                chunk.WriteInt((int)MemUtil.GetPtr(pair.Value));
            }
        }

        private static unsafe void Read(Chunk chunk, NullSafeDict<int, string> obj)
        {
            int count = chunk.ReadInt();
            for (int i = 0; i < count; i++)
            {
                string key = chunk.ReadStr();
                int value = chunk.ReadInt();
                obj.Add(value, key);
            }
        }

        private static unsafe void Write(Chunk chunk, CAnimatedSpriteDrawComponent obj)
        {
            chunk.Write(obj, 0x2C, 0x84);
            Write(chunk, obj.bounds);
            chunk.WriteStr(obj.nextAnimation);
            chunk.WriteStr(obj.animation.id);
        }

        private static unsafe void Read(Chunk chunk, CAnimatedSpriteDrawComponent obj)
        {
            chunk.Read(obj, 0x2C, 0x84);
            Read(chunk, obj.bounds);
            obj.nextAnimation = chunk.ReadStr();
            obj.timeSpan1 = new TimeSpan(obj.timeSpan1.Ticks + dt);
            string animationId = chunk.ReadStr();
            obj.animation = obj.animImage.GetAnimation(animationId);
        }

        private static unsafe void Write(Chunk chunk, CImageDrawComponent obj)
        {
            chunk.Write(obj, 0x1C, 0x58);
            Write(chunk, obj.bounds);
        }

        private static unsafe void Read(Chunk chunk, CImageDrawComponent obj)
        {
            chunk.Read(obj, 0x1C, 0x58);
            Read(chunk, obj.bounds);
        }

        private static unsafe void Write(Chunk chunk, CGroupDrawComponent obj)
        {
            chunk.Write(obj, 0x18, 0x58);
        }

        private static unsafe void Read(Chunk chunk, CGroupDrawComponent obj)
        {
            chunk.Read(obj, 0x18, 0x58);
        }

        private static unsafe void Write(Chunk chunk, CLine obj)
        {
            chunk.Write(obj, 0x4, 0x18);
        }

        private static unsafe void Read(Chunk chunk, CLine obj)
        {
            chunk.Read(obj, 0x4, 0x18);
        }

        private static unsafe void Write(Chunk chunk, CLineDrawComponent obj)
        {
            chunk.Write(obj, 0x18, 0x4);
            chunk.WriteInt(obj.lines.Count);
            foreach (CLine line in obj.lines)
                Write(chunk, line);
        }

        private static unsafe void Read(Chunk chunk, CLineDrawComponent obj)
        {
            chunk.Read(obj, 0x18, 0x4);
            int count = chunk.ReadInt();
            obj.lines = new List<CLine>(count);
            for (int i = 0; i < count; i++)
            {
                CLine line = new CLine(Vector2.Zero, Vector2.Zero, Color.Black);
                Read(chunk, line);
                obj.lines.Add(line);
            }
        }

        private static unsafe void Write(Chunk chunk, Tweener obj)
        {
            chunk.Write(obj, 0x10, 0x1C);
        }

        private static unsafe void Read(Chunk chunk, Tweener obj)
        {
            chunk.Read(obj, 0x10, 0x1C);
        }

        private static unsafe void Write(Chunk chunk, Slot obj)
        {
            chunk.Write(obj, 0x68, 0x3C);
        }

        private static unsafe void Read(Chunk chunk, Slot obj)
        {
            chunk.Read(obj, 0x68, 0x3C);
        }

        private static unsafe void Write(Chunk chunk, Random obj)
        {
            chunk.Write(obj, 0x8, 0x8);
            chunk.Write(obj, 0x1C, 0xE0);
        }

        private static unsafe void Read(Chunk chunk, Random obj)
        {
            chunk.Read(obj, 0x8, 0x8);
            chunk.Read(obj, 0x1C, 0xE0);
        }

        private static unsafe void Write(Chunk chunk, Grapple obj)
        {
            chunk.Write(obj, 0x24, 0x14);
            Write(chunk, obj.actor);
            Write(chunk, obj.animSpriteDrawComp1);
            Write(chunk, obj.spriteDrawComp1);
            Write(chunk, obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, Grapple obj)
        {
            chunk.Read(obj, 0x24, 0x14);
            Read(chunk, obj.actor);
            Read(chunk, obj.animSpriteDrawComp1);
            Read(chunk, obj.spriteDrawComp1);
            Read(chunk, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, Rope obj)
        {
            chunk.Write(obj, 0x20, 0x24);
            Write(chunk, obj.actor);
            Write(chunk, obj.line1);
            Write(chunk, obj.line2);
            Write(chunk, obj.line3);
            Write(chunk, obj.lineDrawComp1);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            if (obj.target == null)
                chunk.WriteInt(-1);
            else if (obj.target is Player)
                chunk.WriteInt((obj.target as Player).actor.Id);
            else if (obj.target is GoldenHook)
            {
                if (!include[GoldenHook.Id])
                    chunk.WriteInt(-2);
                else
                    chunk.WriteInt((obj.target as GoldenHook).actor.Id);
            }
            obj.target.Match<Grapple>(grapple => chunk.WriteInt(grapple.actor.Id));
            obj.target.Match<GoldenHook>(goldenHook => chunk.WriteInt(goldenHook.actor.Id));
        }

        private static unsafe void Read(Chunk chunk, Rope obj)
        {
            chunk.Read(obj, 0x20, 0x24);
            Read(chunk, obj.actor);
            Read(chunk, obj.line1);
            Read(chunk, obj.line2);
            Read(chunk, obj.line3);
            Read(chunk, obj.lineDrawComp1);
            if (obj.lineDrawComp1.lines.Count > 0)
            {
                obj.lineDrawComp1.lines.Clear();
                obj.lineDrawComp1.AddLine(obj.line1);
                obj.lineDrawComp1.AddLine(obj.line2);
            }
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            int targetId = chunk.ReadInt();
            if (targetId != -2)
                applyPtr.Add(() => obj.target = contrLookup[targetId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, DroppedObstacle obj)
        {
            chunk.Write(obj, 0x24, 0x24);
            Write(chunk, obj.actor);
            Write(chunk, obj.spriteDraw1);
            Write(chunk, obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            if (include[AIVolume.Id])
                chunk.WriteInt(obj.aiVolume != null ? obj.aiVolume.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, DroppedObstacle obj)
        {
            chunk.Read(obj, 0x24, 0x24);
            Read(chunk, obj.actor);
            Read(chunk, obj.spriteDraw1);
            Read(chunk, obj.bounds);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            if (include[AIVolume.Id])
            {
                int aiVolumeId = chunk.ReadInt();
                applyPtr.Add(() => obj.aiVolume = (AIVolume)contrLookup[aiVolumeId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, Fireball obj)
        {
            chunk.Write(obj, 0x3C, 0x3C);
            Write(chunk, obj.actor);
            Write(chunk, obj.animSpriteDraw);
            Write(chunk, obj.bounds);
            if (obj.animSpriteDraw.Sprite == null)
                chunk.WriteInt(0);
            else if (obj.animSpriteDraw.Sprite == obj.animImage1)
                chunk.WriteInt(1);
            else
                chunk.WriteInt(2);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            if (include[Shockwave.Id])
                chunk.WriteInt(obj.shockwave != null ? obj.shockwave.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, Fireball obj)
        {
            chunk.Read(obj, 0x3C, 0x3C);
            Read(chunk, obj.actor);
            Read(chunk, obj.animSpriteDraw);
            Read(chunk, obj.bounds);
            int sprite = chunk.ReadInt();
            if (sprite == 0)
                obj.animSpriteDraw.Sprite = null;
            else if (sprite == 1)
                obj.animSpriteDraw.Sprite = obj.animImage1;
            else
                obj.animSpriteDraw.Sprite = obj.animImage2;
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            if (include[Shockwave.Id])
            {
                int shockwaveId = chunk.ReadInt();
                applyPtr.Add(() => obj.shockwave = (Shockwave)contrLookup[shockwaveId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, Rocket obj)
        {
            chunk.Write(obj, 0x38, 0x38);
            Write(chunk, obj.actor);
            Write(chunk, obj.spriteDrawComp);
            Write(chunk, obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            chunk.WriteInt(obj.target != null ? obj.target.actor.Id : -1);
            chunk.WriteInt(obj.unknown != null ? obj.unknown.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, Rocket obj)
        {
            chunk.Read(obj, 0x38, 0x38);
            Read(chunk, obj.actor);
            Read(chunk, obj.spriteDrawComp);
            Read(chunk, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            int targetId = chunk.ReadInt();
            int unknownId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            applyPtr.Add(() => obj.unknown = (Player)contrLookup[unknownId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, GoldenHook obj)
        {
            chunk.Write(obj, 0x30, 0x1C);
            Write(chunk, obj.actor);
            Write(chunk, obj.spriteDraw);
            Write(chunk, obj.animSpriteDraw);
            Write(chunk, obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            chunk.WriteInt(obj.target != null ? obj.target.actor.Id : -1);
            chunk.WriteInt(obj.unknown != null ? obj.unknown.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, GoldenHook obj)
        {
            chunk.Read(obj, 0x30, 0x1C);
            Read(chunk, obj.actor);
            Read(chunk, obj.spriteDraw);
            Read(chunk, obj.animSpriteDraw);
            Read(chunk, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            int targetId = chunk.ReadInt();
            int unknownId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            applyPtr.Add(() => obj.unknown = (Player)contrLookup[unknownId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, Shockwave obj)
        {
            chunk.Write(obj, 0x2C, 0x30);
            Write(chunk, obj.actor);
            Write(chunk, obj.animSpriteDraw1);
            Write(chunk, obj.animSpriteDraw2);
            Write(chunk, obj.animSpriteDraw3);
            Write(chunk, obj.animSpriteDraw4);
            Write(chunk, obj.animSpriteDraw5);
            Write(chunk, obj.groupDraw);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, Shockwave obj)
        {
            chunk.Read(obj, 0x2C, 0x30);
            Read(chunk, obj.actor);
            Read(chunk, obj.animSpriteDraw1);
            Read(chunk, obj.animSpriteDraw2);
            Read(chunk, obj.animSpriteDraw3);
            Read(chunk, obj.animSpriteDraw4);
            Read(chunk, obj.animSpriteDraw5);
            Read(chunk, obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, DroppedBomb obj)
        {
            chunk.Write(obj, 0x28, 0x18);
            Write(chunk, obj.actor);
            Write(chunk, obj.bounds);
            Write(chunk, obj.animSpriteDraw1);
            Write(chunk, obj.animSpriteDraw2);
            Write(chunk, obj.groupDraw);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, DroppedBomb obj)
        {
            chunk.Read(obj, 0x28, 0x18);
            Read(chunk, obj.actor);
            Read(chunk, obj.bounds);
            Read(chunk, obj.animSpriteDraw1);
            Read(chunk, obj.animSpriteDraw2);
            Read(chunk, obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void WriteEa(Chunk chunk, EditableActor obj)
        {
            chunk.Write(obj, 0x30, 0x24);
            Write(chunk, obj.actor);
            Write(chunk, obj.bounds);
        }

        private static unsafe void ReadEa(Chunk chunk, EditableActor obj)
        {
            chunk.Read(obj, 0x30, 0x24);
            Read(chunk, obj.actor);
            Read(chunk, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
        }

        private static unsafe void Write(Chunk chunk, Obstacle obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x14, 0x20);
            Write(chunk, obj.spriteDraw);
            Write(chunk, obj.groupDraw);
            if (include[AIVolume.Id])
            {
                chunk.WriteInt(obj.aiVolume1 != null ? obj.aiVolume1.actor.Id : -1);
                chunk.WriteInt(obj.aiVolume2 != null ? obj.aiVolume2.actor.Id : -1);
            }
        }

        private static unsafe void Read(Chunk chunk, Obstacle obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x14, 0x20);
            Read(chunk, obj.spriteDraw);
            Read(chunk, obj.groupDraw);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            if (include[AIVolume.Id])
            {
                int aiVolume1Id = chunk.ReadInt();
                int aiVolume2Id = chunk.ReadInt();
                applyPtr.Add(() => obj.aiVolume1 = (AIVolume)contrLookup[aiVolume1Id]);
                applyPtr.Add(() => obj.aiVolume2 = (AIVolume)contrLookup[aiVolume2Id]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, FreezeRay obj)
        {
            chunk.Write(obj, 0x18, 0x18);
            Write(chunk, obj.actor);
            chunk.WriteInt(obj.animSpriteDraws.Length);
            foreach (var animSpriteDraw in obj.animSpriteDraws)
                Write(chunk, animSpriteDraw);
            Write(chunk, obj.groupDraw);
            Write(chunk, obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, FreezeRay obj)
        {
            chunk.Read(obj, 0x18, 0x18);
            Read(chunk, obj.actor);
            int count = chunk.ReadInt();
            for (int i = 0; i < count; i++)
            {
                if (i >= obj.animSpriteDraws.Length)
                {
                    CAnimatedSpriteDrawComponent dummy = new CAnimatedSpriteDrawComponent();
                    Read(chunk, dummy);
                    continue;
                }

                Read(chunk, obj.animSpriteDraws[i]);
            }
            Read(chunk, obj.groupDraw);
            Read(chunk, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, Pickup obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x10, 0xC);
            Write(chunk, obj.animSpriteDraw1);
            Write(chunk, obj.animSpriteDraw2);
            chunk.WriteInt(obj.imageDraw3 != null ? 1 : 0);
            if (obj.imageDraw3 != null)
            {
                Write(chunk, obj.imageDraw3);
                Write(chunk, obj.imageDraw4);
            }
            Write(chunk, obj.groupDraw);
        }

        private static unsafe void Read(Chunk chunk, Pickup obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x10, 0xC);
            Read(chunk, obj.animSpriteDraw1);
            Read(chunk, obj.animSpriteDraw2);
            int notNull = chunk.ReadInt();
            if (notNull != 0)
            {
                Read(chunk, obj.imageDraw3);
                Read(chunk, obj.imageDraw4);
            }
            Read(chunk, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void WriteRea(Chunk chunk, ResizableEditableActor obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x0, 0x14);
        }

        private static unsafe void ReadRea(Chunk chunk, ResizableEditableActor obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x0, 0x14);
        }

        private static unsafe void Write(Chunk chunk, Trigger obj)
        {
            WriteRea(chunk, obj);
            chunk.Write(obj, 0x68 + 0x1C, 0x8);
            chunk.WriteInt(obj.list1.Count);
            foreach (ICActorController contr in obj.list1)
            {
                if (contr == null)
                {
                    chunk.WriteInt(-1);
                    continue;
                }

                if (contr is Player)
                {
                    chunk.WriteInt((contr as Player).actor.Id);
                }
                else if (contr is DroppedBomb)
                {
                    if (include[DroppedBomb.Id])
                        chunk.WriteInt((contr as DroppedBomb).actor.Id);
                    else
                        chunk.WriteInt(-2);
                }
                else if (contr is Fireball)
                {
                    if (include[Fireball.Id])
                        chunk.WriteInt((contr as Fireball).actor.Id);
                    else
                        chunk.WriteInt(-2);
                }
                else if (contr is Rocket)
                {
                    if (include[Rocket.Id])
                        chunk.WriteInt((contr as Rocket).actor.Id);
                    else
                        chunk.WriteInt(-2);
                }
            }
            chunk.WriteInt(obj.list2.Count);
            foreach (ICActorController contr in obj.list2)
            {
                if (contr == null)
                {
                    chunk.WriteInt(-1);
                    continue;
                }

                if (contr is Player)
                {
                    chunk.WriteInt((contr as Player).actor.Id);
                }
                else if (contr is DroppedBomb)
                {
                    if (include[DroppedBomb.Id])
                        chunk.WriteInt((contr as DroppedBomb).actor.Id);
                    else
                        chunk.WriteInt(-2);
                }
                else if (contr is Fireball)
                {
                    if (include[Fireball.Id])
                        chunk.WriteInt((contr as Fireball).actor.Id);
                    else
                        chunk.WriteInt(-2);
                }
                else if (contr is Rocket)
                {
                    if (include[Rocket.Id])
                        chunk.WriteInt((contr as Rocket).actor.Id);
                    else
                        chunk.WriteInt(-2);
                }
            }
        }

        private static unsafe void Read(Chunk chunk, Trigger obj)
        {
            ReadRea(chunk, obj);
            chunk.Read(obj, 0x68 + 0x1C, 0x8);
            int count1 = chunk.ReadInt();
            while (obj.list1.Count < count1) obj.list1.Add(null);
            while (obj.list1.Count > count1) obj.list1.RemoveAt(obj.list1.Count - 1);
            for (int i = 0; i < count1; i++)
            {
                int contrId = chunk.ReadInt();
                if (contrId == -2)
                    continue;
                int j = i;
                applyPtr.Add(() => obj.list1[j] = contrLookup[contrId]);
            }
            int count2 = chunk.ReadInt();
            while (obj.list2.Count < count2) obj.list2.Add(null);
            while (obj.list2.Count > count2) obj.list2.RemoveAt(obj.list2.Count - 1);
            for (int i = 0; i < count2; i++)
            {
                int contrId = chunk.ReadInt();
                if (contrId == -2)
                    continue;
                int j = i;
                applyPtr.Add(() => obj.list2[j] = contrLookup[contrId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, SwitchBlock obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x24, 0x28);
            Write(chunk, obj.animSpriteDraw1);
            Write(chunk, obj.animSpriteDraw2);
            Write(chunk, (CConvexPolygon)obj.colShape);
            Write(chunk, obj.groupDraw);
        }

        private static unsafe void Read(Chunk chunk, SwitchBlock obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x24, 0x28);
            Read(chunk, obj.animSpriteDraw1);
            Read(chunk, obj.animSpriteDraw2);
            Read(chunk, (CConvexPolygon)obj.colShape);
            Read(chunk, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, FallTile obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x4, 0x18);
            Write(chunk, obj.animSpriteDraw);
            Write(chunk, obj.groupDraw);
        }

        private static unsafe void Read(Chunk chunk, FallTile obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x4, 0x18);
            Read(chunk, obj.animSpriteDraw);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            Read(chunk, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, TriggerSaw obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x18, 0x28);
            Write(chunk, obj.imageDraw);
            Write(chunk, obj.convexPoly);
            Write(chunk, obj.groupDraw);
        }

        private static unsafe void Read(Chunk chunk, TriggerSaw obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x18, 0x28);
            Read(chunk, obj.imageDraw);
            Read(chunk, obj.convexPoly);
            Read(chunk, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, RocketLauncher obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x1C, 0xC);
            Write(chunk, obj.animSpriteDraw1);
            Write(chunk, obj.animSpriteDraw2);
            Write(chunk, obj.groupDraw);
            chunk.WriteInt(obj.target != null ? obj.target.actor.Id : -1);
            if (include[Rocket.Id])
            {
                chunk.WriteInt(obj.rockets.Count);
                foreach (Rocket rocket in obj.rockets)
                    chunk.WriteInt(rocket != null ? rocket.actor.Id : -1);
            }
        }

        private static unsafe void Read(Chunk chunk, RocketLauncher obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x1C, 0xC);
            Read(chunk, obj.animSpriteDraw1);
            Read(chunk, obj.animSpriteDraw2);
            Read(chunk, obj.groupDraw);
            int targetId = chunk.ReadInt();
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            if (include[Rocket.Id])
            {
                int count = chunk.ReadInt();
                while (obj.rockets.Count < count) obj.rockets.Add(null);
                while (obj.rockets.Count > count) obj.rockets.RemoveAt(obj.rockets.Count - 1);
                for (int i = 0; i < count; i++)
                {
                    int rocketId = chunk.ReadInt();
                    int j = i;
                    applyPtr.Add(() => obj.rockets[j] = (Rocket)contrLookup[rocketId]);
                }
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, BoostaCoke obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x24, 0x44);
            Write(chunk, obj.animSpriteDraw1);
            Write(chunk, obj.animSpriteDraw2);
            Write(chunk, obj.animSpriteDraws[0]);
            Write(chunk, obj.animSpriteDraws[1]);
            Write(chunk, obj.animSpriteDraws[2]);
            Write(chunk, obj.animSpriteDraws[3]);
            Write(chunk, obj.tweener);
            Write(chunk, obj.random);
            Write(chunk, obj.groupDraw);
            chunk.WriteInt(obj.player != null ? obj.player.actor.Id : -1);
            fixed (bool* bytes = obj.bools)
                chunk.WriteBytes((byte*)bytes, 4);
        }

        private static unsafe void Read(Chunk chunk, BoostaCoke obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x24, 0x44);
            Read(chunk, obj.animSpriteDraw1);
            Read(chunk, obj.animSpriteDraw2);
            Read(chunk, obj.animSpriteDraws[0]);
            Read(chunk, obj.animSpriteDraws[1]);
            Read(chunk, obj.animSpriteDraws[2]);
            Read(chunk, obj.animSpriteDraws[3]);
            Read(chunk, obj.tweener);
            Read(chunk, obj.random);
            Read(chunk, obj.groupDraw);
            int playerId = chunk.ReadInt();
            applyPtr.Add(() => obj.player = (Player)contrLookup[playerId]);
            fixed (bool* bytes = obj.bools)
                chunk.ReadBytes((byte*)bytes, 4);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, Laser obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x2C, 0x3C);
            Write(chunk, obj.animSpriteDraw);
            Write(chunk, obj.lineDraw);
            Write(chunk, obj.line1);
            Write(chunk, obj.line2);
            Write(chunk, obj.groupDraw);
            if (include[AIVolume.Id])
                chunk.WriteInt(obj.aiVolume != null ? obj.aiVolume.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, Laser obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x2C, 0x3C);
            Read(chunk, obj.animSpriteDraw);
            Read(chunk, obj.lineDraw);
            Read(chunk, obj.line1);
            Read(chunk, obj.line2);
            Read(chunk, obj.groupDraw);
            if (include[AIVolume.Id])
            {
                int aiVolumeId = chunk.ReadInt();
                applyPtr.Add(() => obj.aiVolume = (AIVolume)contrLookup[aiVolumeId]);
            }
            obj.lineDraw.lines.Clear();
            obj.lineDraw.lines.Add(obj.line2);
            obj.lineDraw.lines.Add(obj.line1);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, AIVolume obj)
        {
            WriteRea(chunk, obj);
            chunk.Write(obj, 0x68 + 0x24, 0x4);
            chunk.WriteInt(obj.type.value);
            chunk.WriteInt(obj.defaultActive.value);
            chunk.WriteInt(obj.easy.value);
            chunk.WriteInt(obj.medium.value);
            chunk.WriteInt(obj.hard.value);
            chunk.WriteInt(obj.unfair.value);
        }

        private static unsafe void Read(Chunk chunk, AIVolume obj)
        {
            ReadRea(chunk, obj);
            chunk.Read(obj, 0x68 + 0x24, 0x4);
            obj.type.value = chunk.ReadInt();
            obj.defaultActive.value = chunk.ReadInt();
            obj.easy.value = chunk.ReadInt();
            obj.medium.value = chunk.ReadInt();
            obj.hard.value = chunk.ReadInt();
            obj.unfair.value = chunk.ReadInt();
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, Timer obj)
        {
            WriteRea(chunk, obj);
            chunk.Write(obj, 0x68 + 0x10, 0x14);
            chunk.WriteInt(obj.unknown.value);
            chunk.WriteInt(obj.list1.Count);
            foreach (ICActorController contr in obj.list1)
                chunk.WriteInt(contr != null ? ((Player)contr).actor.Id : -1);
            chunk.WriteInt(obj.list2.Count);
            foreach (ICActorController contr in obj.list2)
                chunk.WriteInt(contr != null ? ((Player)contr).actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, Timer obj)
        {
            ReadRea(chunk, obj);
            chunk.Read(obj, 0x68 + 0x10, 0x14);
            obj.unknown.value = chunk.ReadInt();
            int count1 = chunk.ReadInt();
            while (obj.list1.Count < count1) obj.list1.Add(null);
            while (obj.list1.Count > count1) obj.list1.RemoveAt(obj.list1.Count - 1);
            for (int i = 0; i < count1; i++)
            {
                int contrId = chunk.ReadInt();
                int j = i;
                applyPtr.Add(() => obj.list1[j] = contrLookup[contrId]);
            }
            int count2 = chunk.ReadInt();
            while (obj.list2.Count < count2) obj.list2.Add(null);
            while (obj.list2.Count > count2) obj.list2.RemoveAt(obj.list2.Count - 1);
            for (int i = 0; i < count2; i++)
            {
                int contrId = chunk.ReadInt();
                int j = i;
                applyPtr.Add(() => obj.list2[j] = contrLookup[contrId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, Checkpoint obj)
        {
            WriteEa(chunk, obj);
            chunk.Write(obj, 0x54 + 0x3C, 0x28);
            chunk.WriteInt(obj.helpers.Count);
            foreach (Checkpoint helper in obj.helpers)
                chunk.WriteInt(helper != null ? helper.actor.Id : -1);
            chunk.WriteInt(obj.checkpoint1 != null ? obj.checkpoint1.actor.Id : -1);
            chunk.WriteInt(obj.checkpoint2 != null ? obj.checkpoint2.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, Checkpoint obj)
        {
            ReadEa(chunk, obj);
            chunk.Read(obj, 0x54 + 0x3C, 0x28);
            int helpers = chunk.ReadInt();
            while (obj.helpers.Count < helpers) obj.helpers.Add(null);
            while (obj.helpers.Count > helpers) obj.helpers.RemoveAt(obj.helpers.Count - 1);
            for (int i = 0; i < helpers; i++)
            {
                int helperId = chunk.ReadInt();
                int j = i;
                applyPtr.Add(() => obj.helpers[j] = (Checkpoint)contrLookup[helperId]);
            }
            int checkpoint1Id = chunk.ReadInt();
            int checkpoint2Id = chunk.ReadInt();
            applyPtr.Add(() => obj.checkpoint1 = (Checkpoint)contrLookup[checkpoint1Id]);
            applyPtr.Add(() => obj.checkpoint2 = (Checkpoint)contrLookup[checkpoint2Id]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, StraightRocket obj)
        {
            chunk.Write(obj, 0x28, 0x18);
            Write(chunk, obj.actor);
            Write(chunk, obj.spriteDraw);
            Write(chunk, obj.bounds);
        }

        private static unsafe void Read(Chunk chunk, StraightRocket obj)
        {
            chunk.Read(obj, 0x28, 0x18);
            Read(chunk, obj.actor);
            Read(chunk, obj.spriteDraw);
            Read(chunk, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, Player obj)
        {
            chunk.Write(obj, 0x1D4, 0x214);
            Write(chunk, obj.actor);
            Write(chunk, obj.slot);
            Write(chunk, obj.random);
            Write(chunk, obj.groupDrawComp1);
            Write(chunk, obj.animSpriteDrawComp1);
            Write(chunk, obj.animSpriteDrawComp2);
            Write(chunk, obj.animSpriteDrawComp3);
            Write(chunk, obj.animSpriteDrawComp4);
            Write(chunk, obj.animSpriteDrawComp5);
            Write(chunk, obj.animSpriteDrawComp6);
            Write(chunk, obj.spriteDrawComp1);
            Write(chunk, obj.spriteDrawComp2);
            Write(chunk, obj.spriteDrawComp3);
            Write(chunk, obj.spriteDrawComp4);
            Write(chunk, obj.spriteDrawComp5);
            Write(chunk, obj.imageDrawComp1);
            Write(chunk, obj.imageDrawComp2);
            Write(chunk, obj.imageDrawComp3);
            Write(chunk, obj.imageDrawComp4);
            Write(chunk, obj.tweener1);
            Write(chunk, obj.tweener2);
            Write(chunk, obj.tweener3);
            Write(chunk, obj.hitboxStanding);
            Write(chunk, obj.hitboxSliding);
            chunk.WriteInt(obj.grapple != null ? obj.grapple.actor.Id : -1);
            chunk.WriteInt(obj.rope != null ? obj.rope.actor.Id : -1);
            if (include[GoldenHook.Id])
                chunk.WriteInt(obj.goldenHook != null ? obj.goldenHook.actor.Id : -1);
            if (include[Shockwave.Id])
                chunk.WriteInt(obj.shockwave != null ? obj.shockwave.actor.Id : -1);
            if (include[DroppedBomb.Id])
                chunk.WriteInt(obj.droppedBomb != null ? obj.droppedBomb.actor.Id : -1);
            if (include[FreezeRay.Id])
                chunk.WriteInt(obj.freezeRay != null ? obj.freezeRay.actor.Id : -1);
            chunk.WriteInt(obj.hooked != null ? obj.hooked.actor.Id : -1);
            chunk.WriteInt(obj.unknown1 != null ? obj.unknown1.actor.Id : -1);
            if (include[Trigger.Id])
            {
                chunk.WriteInt(obj.trigger1 != null ? obj.trigger1.actor.Id : -1);
                chunk.WriteInt(obj.trigger2 != null ? obj.trigger2.actor.Id : -1);
            }
            if (include[Checkpoint.Id])
            {
                chunk.WriteInt(obj.checkpoint1 != null ? obj.checkpoint1.actor.Id : -1);
                chunk.WriteInt(obj.checkpoint2 != null ? obj.checkpoint2.actor.Id : -1);
            }
            if (include[DroppedObstacle.Id])
            {
                chunk.WriteInt(obj.droppedObstacles.Count);
                foreach (DroppedObstacle droppedObstacle in obj.droppedObstacles)
                    chunk.WriteInt(droppedObstacle != null ? droppedObstacle.actor.Id : -1);
            }
            if (include[Fireball.Id])
            {
                chunk.WriteInt(obj.fireballs.Count);
                foreach (Fireball fireball in obj.fireballs)
                    chunk.WriteInt(fireball != null ? fireball.actor.Id : -1);
            }
            if (include[Rocket.Id])
            {
                chunk.WriteInt(obj.rockets.Count);
                foreach (Rocket rocket in obj.rockets)
                    chunk.WriteInt(rocket != null ? rocket.actor.Id : -1);
            }
            if (include[BoostaCoke.Id])
            {
                chunk.WriteInt(obj.boostaCokes.Count);
                foreach (BoostaCoke boostaCoke in obj.boostaCokes)
                    chunk.WriteInt(boostaCoke != null ? boostaCoke.actor.Id : -1);
            }
        }

        private static unsafe void Read(Chunk chunk, Player obj)
        {
            chunk.Read(obj, 0x1D4, 0x214);
            Read(chunk, obj.actor);
            Read(chunk, obj.slot);
            Read(chunk, obj.random);
            Read(chunk, obj.groupDrawComp1);
            Read(chunk, obj.animSpriteDrawComp1);
            Read(chunk, obj.animSpriteDrawComp2);
            Read(chunk, obj.animSpriteDrawComp3);
            Read(chunk, obj.animSpriteDrawComp4);
            Read(chunk, obj.animSpriteDrawComp5);
            Read(chunk, obj.animSpriteDrawComp6);
            Read(chunk, obj.spriteDrawComp1);
            Read(chunk, obj.spriteDrawComp2);
            Read(chunk, obj.spriteDrawComp3);
            Read(chunk, obj.spriteDrawComp4);
            Read(chunk, obj.spriteDrawComp5);
            Read(chunk, obj.imageDrawComp1);
            Read(chunk, obj.imageDrawComp2);
            Read(chunk, obj.imageDrawComp3);
            Read(chunk, obj.imageDrawComp4);
            Read(chunk, obj.tweener1);
            Read(chunk, obj.tweener2);
            Read(chunk, obj.tweener3);
            Read(chunk, obj.hitboxStanding);
            Read(chunk, obj.hitboxSliding);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            obj.timespan2 = new TimeSpan(obj.timespan2.Ticks + dt);
            obj.timespan3 = new TimeSpan(obj.timespan3.Ticks + dt);
            obj.timespan4 = new TimeSpan(obj.timespan4.Ticks + dt);
            obj.timespan5 = new TimeSpan(obj.timespan5.Ticks + dt);
            obj.timespan6 = new TimeSpan(obj.timespan6.Ticks + dt);
            obj.timespan7 = new TimeSpan(obj.timespan7.Ticks + dt);
            obj.timespan8 = new TimeSpan(obj.timespan8.Ticks + dt);
            obj.timespan9 = new TimeSpan(obj.timespan9.Ticks + dt);
            obj.timespan10 = new TimeSpan(obj.timespan10.Ticks + dt);
            obj.timespan11 = new TimeSpan(obj.timespan11.Ticks + dt);
            obj.timespan13 = new TimeSpan(obj.timespan13.Ticks + dt);
            obj.timespan14 = new TimeSpan(obj.timespan14.Ticks + dt);
            contrLookup.Add(obj.actor.Id, obj);
            

            int grappleId = chunk.ReadInt();
            applyPtr.Add(() => obj.grapple = (Grapple)contrLookup[grappleId]);
            int ropeId = chunk.ReadInt();
            applyPtr.Add(() => obj.rope = (Rope)contrLookup[ropeId]);
            if (include[GoldenHook.Id])
            {
                int goldenHookId = chunk.ReadInt();
                applyPtr.Add(() => obj.goldenHook = (GoldenHook)contrLookup[goldenHookId]);
            }
            if (include[Shockwave.Id])
            {
                int shockwaveId = chunk.ReadInt();
                applyPtr.Add(() => obj.shockwave = (Shockwave)contrLookup[shockwaveId]);
            }
            if (include[DroppedBomb.Id])
            {
                int droppedBombId = chunk.ReadInt();
                applyPtr.Add(() => obj.droppedBomb = (DroppedBomb)contrLookup[droppedBombId]);
            }
            if (include[FreezeRay.Id])
            {
                int freezeRayId = chunk.ReadInt();
                applyPtr.Add(() => obj.freezeRay = (FreezeRay)contrLookup[freezeRayId]);
            }
            int hookedId = chunk.ReadInt();
            applyPtr.Add(() => obj.hooked = (Player)contrLookup[hookedId]);
            int unknown1Id = chunk.ReadInt();
            applyPtr.Add(() => obj.unknown1 = (Player)contrLookup[unknown1Id]);
            if (include[Trigger.Id])
            {
                int trigger1Id = chunk.ReadInt();
                int trigger2Id = chunk.ReadInt();
                applyPtr.Add(() => obj.trigger1 = (Trigger)contrLookup[trigger1Id]);
                applyPtr.Add(() => obj.trigger2 = (Trigger)contrLookup[trigger2Id]);
            }
            if (include[Checkpoint.Id])
            {
                int checkpoint1Id = chunk.ReadInt();
                int checkpoint2Id = chunk.ReadInt();
                applyPtr.Add(() => obj.checkpoint1 = (Checkpoint)contrLookup[checkpoint1Id]);
                applyPtr.Add(() => obj.checkpoint1 = (Checkpoint)contrLookup[checkpoint2Id]);
            }
            if (include[DroppedObstacle.Id])
            {
                int droppedObstacles = chunk.ReadInt();
                while (obj.droppedObstacles.Count < droppedObstacles) obj.droppedObstacles.Add(null);
                while (obj.droppedObstacles.Count > droppedObstacles) obj.droppedObstacles.RemoveAt(obj.droppedObstacles.Count - 1);
                for (int i = 0; i < droppedObstacles; i++)
                {
                    int droppedObstacleId = chunk.ReadInt();
                    int j = i;
                    applyPtr.Add(() => obj.droppedObstacles[j] = (DroppedObstacle)contrLookup[droppedObstacleId]);
                }
            }
            if (include[Fireball.Id])
            {
                int fireballs = chunk.ReadInt();
                while (obj.fireballs.Count < fireballs) obj.fireballs.Add(null);
                while (obj.fireballs.Count > fireballs) obj.fireballs.RemoveAt(obj.fireballs.Count - 1);
                for (int i = 0; i < fireballs; i++)
                {
                    int fireballId = chunk.ReadInt();
                    int j = i;
                    applyPtr.Add(() => obj.fireballs[j] = (Fireball)contrLookup[fireballId]);
                }
            }
            if (include[Rocket.Id])
            {
                int rockets = chunk.ReadInt();
                while (obj.rockets.Count < rockets) obj.rockets.Add(null);
                while (obj.rockets.Count > rockets) obj.rockets.RemoveAt(obj.rockets.Count - 1);
                for (int i = 0; i < rockets; i++)
                {
                    int rocketId = chunk.ReadInt();
                    int j = i;
                    applyPtr.Add(() => obj.rockets[j] = (Rocket)contrLookup[rocketId]);
                }
            }
            if (include[BoostaCoke.Id])
            {
                int boostaCokes = chunk.ReadInt();
                while (obj.boostaCokes.Count < boostaCokes) obj.boostaCokes.Add(null);
                while (obj.boostaCokes.Count > boostaCokes) obj.boostaCokes.RemoveAt(obj.boostaCokes.Count - 1);
                for (int i = 0; i < boostaCokes; i++)
                {
                    int boostacokeId = chunk.ReadInt();
                    int j = i;
                    applyPtr.Add(() => obj.boostaCokes[j] = (BoostaCoke)contrLookup[boostacokeId]);
                }
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, PlayerBot obj)
        {
            Write(chunk, (Player)obj);
            chunk.Write(obj, 0x1D4 + 0x214 + 0x4, 0x48);
            if (include[GoldenHook.Id])
                chunk.WriteInt(obj.goldenHookBot != null ? obj.goldenHookBot.actor.Id : -1);
        }

        private static unsafe void Read(Chunk chunk, PlayerBot obj)
        {
            Read(chunk, (Player)obj);
            chunk.Read(obj, 0x1D4 + 0x214 + 0x4, 0x48);
            if (include[GoldenHook.Id])
            {
                int hookId = chunk.ReadInt();
                applyPtr.Add(() => obj.goldenHookBot = (GoldenHook)contrLookup[hookId]);
            }
            obj.timespanBot = new TimeSpan(obj.timespanBot.Ticks + dt);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(Chunk chunk, ShakeCameraModifier obj)
        {
            chunk.Write(obj, 0x14, 0x34);
            Write(chunk, obj.random);
        }

        private static unsafe void Read(Chunk chunk, ShakeCameraModifier obj)
        {
            chunk.Read(obj, 0x14, 0x34);
            Read(chunk, obj.random);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            obj.timespan2 = new TimeSpan(obj.timespan2.Ticks + dt);
        }

        private static unsafe void Write(Chunk chunk, ClampCameraModifier obj)
        {
            chunk.Write(obj, 0x8, 0x30);
        }

        private static unsafe void Read(Chunk chunk, ClampCameraModifier obj)
        {
            chunk.Read(obj, 0x8, 0x30);
        }

        private static unsafe void Write(Chunk chunk, Camera obj)
        {
            chunk.Write(obj, 0x10, 0x3C);
        }

        private static unsafe void Read(Chunk chunk, Camera obj)
        {
            chunk.Read(obj, 0x10, 0x3C);
        }

        private static unsafe void Write(Chunk chunk, CameraMP obj)
        {
            chunk.Write(obj, 0x14, 0x34);
        }

        private static unsafe void Read(Chunk chunk, CameraMP obj)
        {
            chunk.Read(obj, 0x14, 0x34);
        }

        private static unsafe void Write(Chunk chunk, CCamera obj)
        {
            chunk.Write(obj, 0x1C, 0xA8);
            Write(chunk, obj.shakeMod);
            Write(chunk, obj.clampMod);
            obj.mods[0].Match<Camera>(camera => Write(chunk, camera));
            obj.mods[0].Match<CameraMP>(cameraMP => Write(chunk, cameraMP));
        }

        private static unsafe void Read(Chunk chunk, CCamera obj)
        {
            chunk.Read(obj, 0x1C, 0xA8);
            Read(chunk, obj.shakeMod);
            Read(chunk, obj.clampMod);
            obj.mods[0].Match<Camera>(camera => Read(chunk, camera));
            obj.mods[0].Match<CameraMP>(cameraMP => Read(chunk, cameraMP));
        }

        private static unsafe void Write(Chunk chunk, ModuleSolo obj)
        {
            chunk.Write(obj, 0x74, 0x4C);
            Write(chunk, obj.random);
            Write(chunk, obj.camera);
        }

        private static unsafe void Read(Chunk chunk, ModuleSolo obj)
        {
            chunk.Read(obj, 0x74, 0x4C);
            Read(chunk, obj.random);
            Read(chunk, obj.camera);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
        }

        private static unsafe void Write(Chunk chunk, ModuleMP obj)
        {
            chunk.Write(obj, 0xA8, 0xB8);
            Write(chunk, obj.random);
            Write(chunk, obj.camera);
        }

        private static unsafe void Read(Chunk chunk, ModuleMP obj)
        {
            chunk.Read(obj, 0xA8, 0xB8);
            Read(chunk, obj.random);
            Read(chunk, obj.camera);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            obj.timespan4 = new TimeSpan(obj.timespan4.Ticks + dt);
        }

        private static readonly List<ActorType> actorTypes = new List<ActorType>();

        // don't ever change the order
        public static readonly ActorType Player = new ActorType<Player>(Write, Read);
        public static readonly ActorType PlayerBot = new ActorType<PlayerBot>(Write, Read);
        public static readonly ActorType Grapple = new ActorType<Grapple, GrappleDef>(Write, Read, () => new GrappleDef(0, null));
        public static readonly ActorType Rope = new ActorType<Rope, RopeDef>(Write, Read, () => new RopeDef());
        public static readonly ActorType Fireball = new ActorType<Fireball, FireballDef>(Write, Read, () => new FireballDef(Vector2.Zero));
        public static readonly ActorType DroppedObstacle = new ActorType<DroppedObstacle, DroppedObstacleDef>(Write, Read, () => new DroppedObstacleDef(null, false));
        public static readonly ActorType Rocket = new ActorType<Rocket, RocketDef>(Write, Read, () => new RocketDef(null, Main.game.stack.gameInfo));
        public static readonly ActorType GoldenHook = new ActorType<GoldenHook, GoldenHookDef>(Write, Read, () => new GoldenHookDef(0, null));
        public static readonly ActorType Shockwave = new ActorType<Shockwave, ShockwaveDef>(Write, Read, () => new ShockwaveDef(null));
        public static readonly ActorType DroppedBomb = new ActorType<DroppedBomb, DroppedBombDef>(Write, Read, () => new DroppedBombDef(Color.White, null));
        public static readonly ActorType Obstacle = new ActorType<Obstacle>(Write, Read);
        public static readonly ActorType FreezeRay = new ActorType<FreezeRay, FreezeRayDef>(Write, Read, () => new FreezeRayDef(Vector2.Zero, null));
        public static readonly ActorType Pickup = new ActorType<Pickup>(Write, Read);
        public static readonly ActorType Trigger = new ActorType<Trigger>(Write, Read);
        public static readonly ActorType SwitchBlock = new ActorType<SwitchBlock>(Write, Read);
        public static readonly ActorType FallTile = new ActorType<FallTile>(Write, Read);
        public static readonly ActorType TriggerSaw = new ActorType<TriggerSaw>(Write, Read);
        public static readonly ActorType RocketLauncher = new ActorType<RocketLauncher>(Write, Read);
        public static readonly ActorType BoostaCoke = new ActorType<BoostaCoke>(Write, Read);
        public static readonly ActorType Laser = new ActorType<Laser>(Write, Read);
        public static readonly ActorType AIVolume = new ActorType<AIVolume, AIVolumeDef>(Write, Read, () => new AIVolumeDef(Vector2.Zero, Vector2.Zero, 0));
        public static readonly ActorType Timer = new ActorType<Timer>(Write, Read);
        public static readonly ActorType Checkpoint = new ActorType<Checkpoint>(Write, Read);
        public static readonly ActorType StraightRocket = new ActorType<StraightRocket, StraightRocketDef>(Write, Read, () => new StraightRocketDef(Main.game.stack.gameInfo));

        private readonly Chunk chunk;

        private static long dt;
        private static bool[] include = new bool[actorTypes.Count];
        private static readonly NullSafeDict<int, ICActorController> contrLookup = new NullSafeDict<int, ICActorController>();
        private static readonly List<Action> applyPtr = new List<Action>();
        private static readonly HashSet<CActor> fixedIdActors = new HashSet<CActor>();
        private static readonly HashSet<int> fixedIds = new HashSet<int>();

        public Savestate()
        {
            chunk = new Chunk();
        }

        public Chunk Chunk { get { return chunk; } }

        public abstract class ActorType
        {
            private static int nextId = 0;

            public int Id;
            public Type Type;
            public Type DefType;

            public ActorType(Type type, Type defType)
            {
                Id = nextId++;
                Type = type;
                DefType = defType;
                actorTypes.Add(this);
            }

            public abstract void Write(Chunk chunk, ICActorController contr);
            public abstract void Read(Chunk chunk, ICActorController contr);
            public abstract CEngine.Definition.Actor.ICActorDef CreateDef();
        }

        private class ActorType<A> : ActorType
        {
            private Action<Chunk, A> write;
            private Action<Chunk, A> read;

            public ActorType(Action<Chunk, A> write, Action<Chunk, A> read) :
                base(typeof(A), null) 
            {
                this.write = write;
                this.read = read;
            }

            public override void Write(Chunk chunk, ICActorController contr)
            {
                write(chunk, (A)contr);
            }

            public override void Read(Chunk chunk, ICActorController contr)
            {
                read(chunk, (A)contr);
            }

            public override CEngine.Definition.Actor.ICActorDef CreateDef()
            {
                return null;
            }
        }

        private class ActorType<A, D> : ActorType<A> where D : CEngine.Definition.Actor.ICActorDef
        {
            Func<D> createDef;

            public ActorType(Action<Chunk, A> write, Action<Chunk, A> read, Func<D> createDef) :
                base(write, read)
            {
                this.createDef = createDef;
            }

            public override CEngine.Definition.Actor.ICActorDef CreateDef()
            {
                return createDef();
            }
        }

        public enum EListMode
        {
            EXCLUDE, INCLUDE
        }

        public void Save(List<ActorType> actors, EListMode listMode)
        {
            if (!Velo.Ingame || Velo.Online)
                return;

            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            CWorld world = CEngine.CEngine.Instance.World;
            Stack stack = Main.game.stack;

            int count = collisionEngine.ActorCount;

            chunk.Start();

            chunk.WriteLong(CEngine.CEngine.Instance.GameTime.TotalGameTime.Ticks);

            if (listMode == EListMode.EXCLUDE)
            {
                include.Fill(true);
                foreach (ActorType type in actors)
                    include[type.Id] = false;
            }
            else
            {
                include.Fill(false);
                foreach (ActorType type in actors)
                    include[type.Id] = true;
            }

            chunk.WriteBoolArr(include);

            for (int i = 0; i < count; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                if (actor.ghostOwnedItem)
                    continue;
                ICActorController controller = actor.Controller;

                if (controller is Player)
                {
                    Player player = controller as Player;
                    if (!player.actor.localPlayer && !player.slot.IsBot)
                        continue;
                    if (!include[Player.Id]) continue;
                    chunk.WriteInt(Player.Id);
                    chunk.WriteInt(player.slot.Index);
                    Player.Write(chunk, player);
                } 
                else if (controller is PlayerBot)
                {
                    Player player = controller as Player;
                    if (!player.actor.localPlayer && !player.slot.IsBot)
                        continue;
                    if (!include[PlayerBot.Id]) continue;
                    chunk.WriteInt(PlayerBot.Id);
                    chunk.WriteInt(player.slot.Index);
                    PlayerBot.Write(chunk, player);
                } 
                else
                {
                    foreach (ActorType type in actorTypes)
                    {
                    
                        if (controller.GetType() == type.Type)
                        {
                            if (!include[type.Id]) continue;
                            chunk.WriteInt(type.Id);
                            type.Write(chunk, controller);
                            break;
                        }
                    }
                }
            }

            chunk.WriteInt(-1);

            foreach (var module in stack.modules)
            {
                module.Match<ModuleSolo>(moduleSolo => Write(chunk, moduleSolo));
                module.Match<ModuleMP>(moduleMP => Write(chunk, moduleMP));
            }
        }

#if !VELO_OLD
#pragma warning disable IDE0034
#endif
        private CActor GetOfType(Type type, int n, Func<CActor, bool> func = null)
        {
            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            
            int c = 0;
            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                if (actor.Controller.GetType() == type && !actor.ghostOwnedItem && (func == null || func(actor)))
                {
                    if (c == n)
                        return actor;
                    else
                        c++;
                }
            }

            return default(CActor);
        }
#if !VELO_OLD
#pragma warning restore IDE0034
#endif

        public void DestroyAllAfter(Type type, int n)
        {
            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            CWorld world = CEngine.CEngine.Instance.World;

            int c = 0;
            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                if (actor.controller.GetType() == type && !actor.ghostOwnedItem)
                {
                    if (c >= n)
                        world.DestroyActor(actor);
                    else
                        c++;
                }
            }
        }

#if !VELO_OLD
#pragma warning disable IDE1006
#endif
        public static void actor_update(CActor actor)
        {
            ICActorController controller = actor.controller;
            if (controller is Grapple)
            {
                Grapple grapple = controller as Grapple;
                if (grapple.owner != null && !grapple.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Rope)
            {
                Rope rope = controller as Rope;
                if (rope.owner != null && !rope.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Fireball)
            {
                Fireball fireball = controller as Fireball;
                if (fireball.owner != null && !fireball.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is DroppedObstacle)
            {
                DroppedObstacle droppedObstacle = controller as DroppedObstacle;
                if (droppedObstacle.owner != null && !droppedObstacle.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Rocket)
            {
                Rocket rocket = controller as Rocket;
                if (rocket.owner != null && !rocket.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is GoldenHook)
            {
                GoldenHook goldenHook = controller as GoldenHook;
                if (goldenHook.owner != null && !goldenHook.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Shockwave)
            {
                Shockwave shockwave = controller as Shockwave;
                if (shockwave.owner != null && !shockwave.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is DroppedBomb)
            {
                DroppedBomb droppedBomb = controller as DroppedBomb;
                if (droppedBomb.owner != null && !droppedBomb.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is FreezeRay)
            {
                FreezeRay freezeRay = controller as FreezeRay;
                if (freezeRay.owner != null && !freezeRay.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
        }
#if !VELO_OLD
#pragma warning restore IDE1006
#endif

        public bool Load(bool setGlobalTime)
        {
            if (!Velo.Ingame || Velo.Online)
                return false;

            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            CWorld world = CEngine.CEngine.Instance.World;
            Stack stack = Main.game.stack;

            Savestates.Instance.savestateLoadTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            stack = Main.game.stack;

            chunk.Start();

            long time = chunk.ReadLong();
            if (setGlobalTime)
            {
                dt = 0;
                CEngine.CEngine.Instance.gameTime = new GameTime(new TimeSpan(time), CEngine.CEngine.Instance.GameTime.ElapsedGameTime);
            }
            else
            {
                dt = CEngine.CEngine.Instance.GameTime.TotalGameTime.Ticks - time;
            }

            include = chunk.ReadBoolArr();

            contrLookup.Add(-1, null);

            int[] counts = new int[actorTypes.Count];

            while (true)
            {
                int id = chunk.ReadInt();

                if (id == -1)
                    break;

                if (id == Player.Id)
                {
                    int index = chunk.ReadInt();
                    CActor actor = GetOfType(Player.Type, 0, (check) => (check.Controller as Player).slot.Index == index);
                    Player.Read(chunk, actor.Controller);
                    fixedIdActors.Add(actor);
                    fixedIds.Add(actor.id);
                }
                else if (id == PlayerBot.Id)
                {
                    int index = chunk.ReadInt();
                    CActor actor = GetOfType(PlayerBot.Type, 0, (check) => (check.Controller as Player).slot.Index == index);
                    PlayerBot.Read(chunk, actor.Controller);
                    fixedIdActors.Add(actor);
                    fixedIds.Add(actor.id);
                }
                else
                {
                    foreach (ActorType type in actorTypes)
                    {

                        if (id == type.Id)
                        {
                            CActor actor = GetOfType(type.Type, counts[type.Id]);
                            ICActorController controller = null;
                            if (actor != null)
                            {
                                controller = actor.Controller;
                            }
                            else
                            {
                                CEngine.Definition.Actor.ICActorDef def = type.CreateDef();
                                if (def != null)
                                    controller = world.SpawnActor(def);
                                else
                                    return true;
                                actor = GetOfType(type.Type, counts[type.Id]);
                            }
                            type.Read(chunk, controller);
                            fixedIdActors.Add(actor);
                            fixedIds.Add(actor.id);
                            counts[type.Id]++;
                        }
                    }
                }
            }

            foreach (ActorType type in actorTypes)
            {
                if (type.CreateDef() != null && include[type.Id])
                    DestroyAllAfter(type.Type, counts[type.Id]);
            }

            foreach (var module in stack.modules)
            {
                module.Match<ModuleSolo>(moduleSolo => Read(chunk, moduleSolo));
                module.Match<ModuleMP>(moduleMP => Read(chunk, moduleMP));
            }

            foreach (Action action in applyPtr)
                action();

            for (int i = 0; i < collisionEngine.actorsById.Count; i++)
                collisionEngine.actorsById[i] = null;

            int nextId = 0;

            for (int i = 0; i < collisionEngine.actors.Count; i++)
            {
                CActor actor = collisionEngine.actors[i];

                if (fixedIdActors.Contains(collisionEngine.actors[i]))
                {
                    while (collisionEngine.actorsById.Count <= actor.Id) collisionEngine.actorsById.Add(null);
                    collisionEngine.actorsById[actor.Id] = actor;
                    continue;
                }

                while (fixedIds.Contains(nextId)) nextId++;

                actor.id = nextId;
                while (collisionEngine.actorsById.Count <= actor.Id) collisionEngine.actorsById.Add(null);
                collisionEngine.actorsById[actor.Id] = actor;
            }

            collisionEngine.actors.Sort((actor1, actor2) => actor1.Id.CompareTo(actor2.Id));

            contrLookup.Clear();
            applyPtr.Clear();
            fixedIdActors.Clear();
            fixedIds.Clear();

            return true;
        }
    }

    public class Savestates : Module
    {
        public HotkeySetting SaveKey;
        public HotkeySetting LoadKey;
        public IntSetting LoadHaltDuration;
        public BoolSetting StoreAIVolumes;

        private readonly Savestate savestate;
        public long savestateLoadTime = 0;

        private Savestates()
            : base("Savestates")
        {
            NewCategory("general");
            SaveKey = AddHotkey("save key", 0x97);
            LoadKey = AddHotkey("load key", 0x97);
            LoadHaltDuration = AddInt("load halt duration", 0, 0, 2000);
            StoreAIVolumes = AddBool("store AI volumes", false);

            LoadHaltDuration.Tooltip =
                "Duration in milliseconds the game will run in slow motion after loading a savestate.";
            StoreAIVolumes.Tooltip =
                "Whether to store AI volumes or not. " +
                "Storing them should be unnecessary in most circumstances.";

            savestate = new Savestate();
        }

        public static Savestates Instance = new Savestates();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Keyboard.Pressed[SaveKey.Value])
            {
                if (StoreAIVolumes.Value)
                    savestate.Save(new List<Savestate.ActorType>{ Savestate.AIVolume }, Savestate.EListMode.EXCLUDE);
                else
                    savestate.Save(new List<Savestate.ActorType> { }, Savestate.EListMode.EXCLUDE);
            }

            if (Keyboard.Pressed[LoadKey.Value])
            {
                if (savestate.Load(TAS.Instance.DtFixed))
                    savestateLoadTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }
        }
    }
}
