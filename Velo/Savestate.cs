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
        public byte[] data;
        public int index;

        public Chunk() {
            data = new byte[0x1000];
            index = 0;
        }

        private void Ensure(int size)
        {
            if (data.Length < size)
                Array.Resize(ref data, data.Length * 2);
        }

        public void Start() { index = 0; }

        public unsafe void Write(object src, int off, int data_size)
        {
            Ensure(index + data_size);
            void* data_src = (byte*)MemUtil.GetPtr(src) + off;
            fixed(byte* data_dst = data)
            {
                Buffer.MemoryCopy(data_src, data_dst + index, data_size, data_size);
            }
            index += data_size;
        }

        public unsafe void Read(object dst, int off, int data_size)
        {
            void* data_dst = (byte*)MemUtil.GetPtr(dst) + off;
            fixed (byte* data_src = data)
            {
                Buffer.MemoryCopy(data_src + index, data_dst, data_size, data_size);
            }
            index += data_size;
        }

        public unsafe void WriteInt(int value)
        {
            Ensure(index + 4);
            fixed (byte* data_dst = data)
            {
                *(int*)(data_dst + index) = value;
            }
            index += 4;
        }

        public unsafe int ReadInt()
        {
            int integer;
            fixed (byte* data_src = data)
            {
                integer = *(int*)(data_src + index);
            }
            index += 4;
            return integer;
        }

        public unsafe void WriteBytes(byte* bytes, int size)
        {
            Ensure(index + size);

            fixed (byte* data_dst = data)
            {
                Buffer.MemoryCopy(bytes, data_dst + index, size, size);
            }
            index += size;
        }

        public unsafe void ReadBytes(byte* bytes, int size)
        {
            fixed (byte* data_src = data)
            {
                Buffer.MemoryCopy(data_src + index, bytes, size, size);
            }
            index += size;
        }

        public unsafe void WriteArr<T>(T[] arr) where T : struct
        {
            if (arr == null)
            {
                WriteInt(-1);
                return;
            }

            WriteInt(arr.Length);
#pragma warning disable CS8500
            fixed (T* bytes = arr)
            {
                WriteBytes((byte*)bytes, arr.Length * sizeof(T));
            }
#pragma warning restore CS8500
        }

        public unsafe T[] ReadArr<T>() where T : struct
        {
            int size = ReadInt();
            if (size == -1)
                return null;
            T[] bytes = new T[size];

#pragma warning disable CS8500
            fixed (T* data_dst = bytes)
            {
                ReadBytes((byte*)data_dst, size * sizeof(T));
            }
#pragma warning restore CS8500
            return bytes;
        }

        public unsafe void WriteStr(string str)
        {
            byte[] bytes = str != null ? Encoding.ASCII.GetBytes(str) : null;
            WriteArr(bytes);
        }

        public unsafe string ReadStr()
        {
            byte[] bytes = ReadArr<byte>();
            if (bytes == null)
                return null;
            return Encoding.ASCII.GetString(bytes);
        }
    }

    public class Savestate
    {
        private unsafe void Write(CAABB obj)
        {
            chunk.Write(obj, 0x4, 0x18);
        }

        private unsafe void Read(CAABB obj)
        {
            chunk.Read(obj, 0x4, 0x18);
        }

        private unsafe void Write(CConvexPolygon obj)
        {
            chunk.Write(obj, 0xC, 0x1C);
            chunk.WriteArr(obj.localVertices);
            chunk.WriteArr(obj.vertices);
        }

        private unsafe void Read(CConvexPolygon obj)
        {
            chunk.Read(obj, 0xC, 0x1C);
            obj.localVertices = chunk.ReadArr<Vector2>();
            obj.vertices = chunk.ReadArr<Vector2>();
        }

        private unsafe void Write(CActor obj)
        {
            chunk.Write(obj, 0x20, 0x40);
            Write(obj.bounds);
        }

        private unsafe void Read(CActor obj)
        {
            chunk.Read(obj, 0x20, 0x40);
            Read(obj.bounds);
            fixedIdActors.Add(obj);
            fixedIds.Add(obj.id);
        }

        private unsafe void Write(CSpriteDrawComponent obj)
        {
            chunk.Write(obj, 0x20, 0x6C);
            Write(obj.bounds);
        }

        private unsafe void Read(CSpriteDrawComponent obj)
        {
            chunk.Read(obj, 0x20, 0x6C);
            Read(obj.bounds);
        }

        private unsafe void Write(Dictionary<string, CAnimation> obj)
        {
            chunk.WriteInt(obj.Count);
            foreach (var pair in obj)
            {
                chunk.WriteStr(pair.Key);
                chunk.WriteInt((int)MemUtil.GetPtr(pair.Value));
            }
        }

        private unsafe void Read(NullSafeDict<int, string> obj)
        {
            int count = chunk.ReadInt();
            for (int i = 0; i < count; i++)
            {
                string key = chunk.ReadStr();
                int value = chunk.ReadInt();
                obj.Add(value, key);
            }
        }

        private unsafe void Write(CAnimatedSpriteDrawComponent obj)
        {
            chunk.Write(obj, 0x2C, 0x84);
            Write(obj.bounds);
            chunk.WriteStr(obj.nextAnimation);
            chunk.WriteStr(obj.animation.id);
        }

        private unsafe void Read(CAnimatedSpriteDrawComponent obj)
        {
            chunk.Read(obj, 0x2C, 0x84);
            Read(obj.bounds);
            obj.nextAnimation = chunk.ReadStr();
            obj.timeSpan1 = new TimeSpan(obj.timeSpan1.Ticks + dt);
            string animationId = chunk.ReadStr();
            obj.animation = obj.animImage.GetAnimation(animationId);
        }

        private unsafe void Write(CImageDrawComponent obj)
        {
            chunk.Write(obj, 0x1C, 0x58);
            Write(obj.bounds);
        }

        private unsafe void Read(CImageDrawComponent obj)
        {
            chunk.Read(obj, 0x1C, 0x58);
            Read(obj.bounds);
        }

        private unsafe void Write(CGroupDrawComponent obj)
        {
            chunk.Write(obj, 0x18, 0x58);
        }

        private unsafe void Read(CGroupDrawComponent obj)
        {
            chunk.Read(obj, 0x18, 0x58);
        }

        private unsafe void Write(CLine obj)
        {
            chunk.Write(obj, 0x4, 0x18);
        }

        private unsafe void Read(CLine obj)
        {
            chunk.Read(obj, 0x4, 0x18);
        }

        private unsafe void Write(CLineDrawComponent obj)
        {
            chunk.Write(obj, 0x18, 0x4);
            chunk.WriteInt(obj.lines.Count);
            foreach (CLine line in obj.lines)
                Write(line);
        }

        private unsafe void Read(CLineDrawComponent obj)
        {
            chunk.Read(obj, 0x18, 0x4);
            int count = chunk.ReadInt();
            obj.lines = new List<CLine>(count);
            for (int i = 0; i < count; i++)
            {
                CLine line = new CLine(Vector2.Zero, Vector2.Zero, Color.Black);
                Read(line);
                obj.lines.Add(line);
            }
        }

        private unsafe void Write(Tweener obj)
        {
            chunk.Write(obj, 0x10, 0x1C);
        }

        private unsafe void Read(Tweener obj)
        {
            chunk.Read(obj, 0x10, 0x1C);
        }

        private unsafe void Write(Slot obj)
        {
            chunk.Write(obj, 0x68, 0x3C);
        }

        private unsafe void Read(Slot obj)
        {
            chunk.Read(obj, 0x68, 0x3C);
        }

        private unsafe void Write(Random obj)
        {
            chunk.Write(obj, 0x8, 0x8);
            chunk.Write(obj, 0x1C, 0xE0);
        }

        private unsafe void Read(Random obj)
        {
            chunk.Read(obj, 0x8, 0x8);
            chunk.Read(obj, 0x1C, 0xE0);
        }

        private unsafe void Write(Grapple obj)
        {
            chunk.Write(obj, 0x24, 0x14);
            Write(obj.actor);
            Write(obj.animSpriteDrawComp1);
            Write(obj.spriteDrawComp1);
            Write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private unsafe void Read(Grapple obj)
        {
            chunk.Read(obj, 0x24, 0x14);
            Read(obj.actor);
            Read(obj.animSpriteDrawComp1);
            Read(obj.spriteDrawComp1);
            Read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(Rope obj)
        {
            chunk.Write(obj, 0x20, 0x24);
            Write(obj.actor);
            Write(obj.line1);
            Write(obj.line2);
            Write(obj.line3);
            Write(obj.lineDrawComp1);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            if (obj.target == null)
                chunk.WriteInt(-1);
            else if (obj.target is Grapple grapple)
                chunk.WriteInt(grapple.actor.Id);
             else if (obj.target is GoldenHook goldenHook)
                chunk.WriteInt(goldenHook.actor.Id);
        }

        private unsafe void Read(Rope obj)
        {
            chunk.Read(obj, 0x20, 0x24);
            Read(obj.actor);
            Read(obj.line1);
            Read(obj.line2);
            Read(obj.line3);
            Read(obj.lineDrawComp1);
            if (obj.lineDrawComp1.lines.Count > 0)
            {
                obj.lineDrawComp1.lines.Clear();
                obj.lineDrawComp1.AddLine(obj.line1);
                obj.lineDrawComp1.AddLine(obj.line2);
            }
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            int targetId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.target = contrLookup[targetId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(DroppedObstacle obj)
        {
            chunk.Write(obj, 0x24, 0x24);
            Write(obj.actor);
            Write(obj.spriteDraw1);
            Write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            if (storeAiVolumes)
                chunk.WriteInt(obj.aiVolume != null ? obj.aiVolume.actor.Id : -1);
        }

        private unsafe void Read(DroppedObstacle obj)
        {
            chunk.Read(obj, 0x24, 0x24);
            Read(obj.actor);
            Read(obj.spriteDraw1);
            Read(obj.bounds);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            int aiVolumeId = -1;
            if (storeAiVolumes)
                aiVolumeId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            if (storeAiVolumes) 
                applyPtr.Add(() => obj.aiVolume = (AIVolume)contrLookup[aiVolumeId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(Fireball obj)
        {
            chunk.Write(obj, 0x3C, 0x3C);
            Write(obj.actor);
            Write(obj.animSpriteDraw);
            Write(obj.bounds);
            if (obj.animSpriteDraw.Sprite == null)
                chunk.WriteInt(0);
            else if (obj.animSpriteDraw.Sprite == obj.animImage1)
                chunk.WriteInt(1);
            else
                chunk.WriteInt(2);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            chunk.WriteInt(obj.shockwave != null ? obj.shockwave.actor.Id : -1);
        }

        private unsafe void Read(Fireball obj)
        {
            chunk.Read(obj, 0x3C, 0x3C);
            Read(obj.actor);
            Read(obj.animSpriteDraw);
            Read(obj.bounds);
            int sprite = chunk.ReadInt();
            if (sprite == 0)
                obj.animSpriteDraw.Sprite = null;
            else if (sprite == 1)
                obj.animSpriteDraw.Sprite = obj.animImage1;
            else
                obj.animSpriteDraw.Sprite = obj.animImage2;
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            int shockwaveId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.shockwave = (Shockwave)contrLookup[shockwaveId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(Rocket obj)
        {
            chunk.Write(obj, 0x38, 0x38);
            Write(obj.actor);
            Write(obj.spriteDrawComp);
            Write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            chunk.WriteInt(obj.target != null ? obj.target.actor.Id : -1);
            chunk.WriteInt(obj.unknown != null ? obj.unknown.actor.Id : -1);
        }

        private unsafe void Read(Rocket obj)
        {
            chunk.Read(obj, 0x38, 0x38);
            Read(obj.actor);
            Read(obj.spriteDrawComp);
            Read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            int targetId = chunk.ReadInt();
            int unknownId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            applyPtr.Add(() => obj.unknown = (Player)contrLookup[unknownId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(GoldenHook obj)
        {
            chunk.Write(obj, 0x30, 0x1C);
            Write(obj.actor);
            Write(obj.spriteDraw);
            Write(obj.animSpriteDraw);
            Write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            chunk.WriteInt(obj.target != null ? obj.target.actor.Id : -1);
            chunk.WriteInt(obj.unknown != null ? obj.unknown.actor.Id : -1);
        }

        private unsafe void Read(GoldenHook obj)
        {
            chunk.Read(obj, 0x30, 0x1C);
            Read(obj.actor);
            Read(obj.spriteDraw);
            Read(obj.animSpriteDraw);
            Read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            int targetId = chunk.ReadInt();
            int unknownId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            applyPtr.Add(() => obj.unknown = (Player)contrLookup[unknownId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(Shockwave obj)
        {
            chunk.Write(obj, 0x2C, 0x30);
            Write(obj.actor);
            Write(obj.animSpriteDraw1);
            Write(obj.animSpriteDraw2);
            Write(obj.animSpriteDraw3);
            Write(obj.animSpriteDraw4);
            Write(obj.animSpriteDraw5);
            Write(obj.groupDraw);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private unsafe void Read(Shockwave obj)
        {
            chunk.Read(obj, 0x2C, 0x30);
            Read(obj.actor);
            Read(obj.animSpriteDraw1);
            Read(obj.animSpriteDraw2);
            Read(obj.animSpriteDraw3);
            Read(obj.animSpriteDraw4);
            Read(obj.animSpriteDraw5);
            Read(obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(DroppedBomb obj)
        {
            chunk.Write(obj, 0x28, 0x18);
            Write(obj.actor);
            Write(obj.bounds);
            Write(obj.animSpriteDraw1);
            Write(obj.animSpriteDraw2);
            Write(obj.groupDraw);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private unsafe void Read(DroppedBomb obj)
        {
            chunk.Read(obj, 0x28, 0x18);
            Read(obj.actor);
            Read(obj.bounds);
            Read(obj.animSpriteDraw1);
            Read(obj.animSpriteDraw2);
            Read(obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void WriteEa(EditableActor obj)
        {
            chunk.Write(obj, 0x30, 0x24);
            Write(obj.actor);
            Write(obj.bounds);
        }

        private unsafe void ReadEa(EditableActor obj)
        {
            chunk.Read(obj, 0x30, 0x24);
            Read(obj.actor);
            Read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
        }

        private unsafe void Write(Obstacle obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x14, 0x20);
            Write(obj.spriteDraw);
            Write(obj.groupDraw);
            if (storeAiVolumes)
            {
                chunk.WriteInt(obj.aiVolume1 != null ? obj.aiVolume1.actor.Id : -1);
                chunk.WriteInt(obj.aiVolume2 != null ? obj.aiVolume2.actor.Id : -1);
            }
        }

        private unsafe void Read(Obstacle obj)
        {
            ReadEa(obj);
            chunk.Read(obj, 0x54 + 0x14, 0x20);
            Read(obj.spriteDraw);
            Read(obj.groupDraw);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            if (storeAiVolumes)
            {
                int aiVolume1Id = chunk.ReadInt();
                int aiVolume2Id = chunk.ReadInt();
                applyPtr.Add(() => obj.aiVolume1 = (AIVolume)contrLookup[aiVolume1Id]);
                applyPtr.Add(() => obj.aiVolume2 = (AIVolume)contrLookup[aiVolume2Id]);
            }
        }

        private unsafe void Write(FreezeRay obj)
        {
            chunk.Write(obj, 0x18, 0x18);
            Write(obj.actor);
            chunk.WriteInt(obj.animSpriteDraws.Length);
            foreach (var animSpriteDraw in obj.animSpriteDraws)
                Write(animSpriteDraw);
            Write(obj.groupDraw);
            Write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private unsafe void Read(FreezeRay obj)
        {
            chunk.Read(obj, 0x18, 0x18);
            Read(obj.actor);
            int count = chunk.ReadInt();
            for (int i = 0; i < count; i++)
            {
                if (i >= obj.animSpriteDraws.Length)
                {
                    CAnimatedSpriteDrawComponent dummy = new CAnimatedSpriteDrawComponent();
                    Read(dummy);
                    continue;
                }

                Read(obj.animSpriteDraws[i]);
            }
            Read(obj.groupDraw);
            Read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(Pickup obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x10, 0xC);
            Write(obj.animSpriteDraw1);
            Write(obj.animSpriteDraw2);
            chunk.WriteInt(obj.imageDraw3 != null ? 1 : 0);
            if (obj.imageDraw3 != null)
            {
                Write(obj.imageDraw3);
                Write(obj.imageDraw4);
            }
            Write(obj.groupDraw);
        }

        private unsafe void Read(Pickup obj)
        {
            ReadEa(obj);
            chunk.Read(obj, 0x54 + 0x10, 0xC);
            Read(obj.animSpriteDraw1);
            Read(obj.animSpriteDraw2);
            int notNull = chunk.ReadInt();
            if (notNull != 0)
            {
                Read(obj.imageDraw3);
                Read(obj.imageDraw4);
            }
            Read(obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private unsafe void WriteRea(ResizableEditableActor obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x0, 0x14);
        }

        private unsafe void ReadRea(ResizableEditableActor obj)
        {
            ReadEa(obj);
            chunk.Read(obj, 0x54 + 0x0, 0x14);
        }

        private unsafe void Write(Trigger obj)
        {
            WriteRea(obj);
            chunk.Write(obj, 0x68 + 0x1C, 0x8);
            chunk.WriteInt(obj.list1.Count);
            foreach (ICActorController contr in obj.list1)
            {
                if (contr is Player player)
                    chunk.WriteInt(contr != null ? player.actor.Id : -1);
                else if (contr is DroppedBomb droppedBomb)
                    chunk.WriteInt(contr != null ? droppedBomb.actor.Id : -1);
                else if (contr is Fireball fireball)
                    chunk.WriteInt(contr != null ? fireball.actor.Id : -1);
                else if (contr is Rocket rocket)
                    chunk.WriteInt(contr != null ? rocket.actor.Id : -1);
                else
                    chunk.WriteInt(-1);
            }
            chunk.WriteInt(obj.list2.Count);
            foreach (ICActorController contr in obj.list2)
            {
                if (contr is Player player)
                    chunk.WriteInt(contr != null ? player.actor.Id : -1);
                else if (contr is DroppedBomb droppedBomb)
                    chunk.WriteInt(contr != null ? droppedBomb.actor.Id : -1);
                else if (contr is Fireball fireball)
                    chunk.WriteInt(contr != null ? fireball.actor.Id : -1);
                else if (contr is Rocket rocket)
                    chunk.WriteInt(contr != null ? rocket.actor.Id : -1);
                else
                    chunk.WriteInt(-1);
            }
        }

        private unsafe void Read(Trigger obj)
        {
            ReadRea(obj);
            chunk.Read(obj, 0x68 + 0x1C, 0x8);
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

        private unsafe void Write(SwitchBlock obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x24, 0x28);
            Write(obj.animSpriteDraw1);
            Write(obj.animSpriteDraw2);
            Write((CConvexPolygon)obj.colShape);
            Write(obj.groupDraw);
        }

        private unsafe void Read(SwitchBlock obj)
        {
            ReadEa(obj);
            chunk.Read(obj, 0x54 + 0x24, 0x28);
            Read(obj.animSpriteDraw1);
            Read(obj.animSpriteDraw2);
            Read((CConvexPolygon)obj.colShape);
            Read(obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(FallTile obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x4, 0x18);
            Write(obj.animSpriteDraw);
            Write(obj.groupDraw);
        }

        private unsafe void Read(FallTile obj)
        {
            ReadEa(obj);
            chunk.Read(obj, 0x54 + 0x4, 0x18);
            Read(obj.animSpriteDraw);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            Read(obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(TriggerSaw obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x18, 0x28);
            Write(obj.imageDraw);
            Write(obj.convexPoly);
            Write(obj.groupDraw);
        }

        private unsafe void Read(TriggerSaw obj)
        {
            ReadEa(obj);
            chunk.Read(obj, 0x54 + 0x18, 0x28);
            Read(obj.imageDraw);
            Read(obj.convexPoly);
            Read(obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(RocketLauncher obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x1C, 0xC);
            Write(obj.animSpriteDraw1);
            Write(obj.animSpriteDraw2);
            Write(obj.groupDraw);
            chunk.WriteInt(obj.target != null ? obj.target.actor.Id : -1);
            chunk.WriteInt(obj.rockets.Count);
            foreach (Rocket rocket in obj.rockets)
                chunk.WriteInt(rocket != null ? rocket.actor.Id : -1);
        }

        private unsafe void Read(RocketLauncher obj)
        {
            ReadEa(obj);
            chunk.Read(obj, 0x54 + 0x1C, 0xC);
            Read(obj.animSpriteDraw1);
            Read(obj.animSpriteDraw2);
            Read(obj.groupDraw);
            int targetId = chunk.ReadInt();
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            int count = chunk.ReadInt();
            while (obj.rockets.Count < count) obj.rockets.Add(null);
            while (obj.rockets.Count > count) obj.rockets.RemoveAt(obj.rockets.Count - 1);
            for (int i = 0; i < count; i++)
            {
                int rocketId = chunk.ReadInt();
                int j = i;
                applyPtr.Add(() => obj.rockets[j] = (Rocket)contrLookup[rocketId]);
            }
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(BoostaCoke obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x24, 0x44);
            Write(obj.animSpriteDraw1);
            Write(obj.animSpriteDraw2);
            Write(obj.animSpriteDraws[0]);
            Write(obj.animSpriteDraws[1]);
            Write(obj.animSpriteDraws[2]);
            Write(obj.animSpriteDraws[3]);
            Write(obj.tweener);
            Write(obj.random);
            Write(obj.groupDraw);
            chunk.WriteInt(obj.player != null ? obj.player.actor.Id : -1);
            fixed (bool* bytes = obj.bools)
                chunk.WriteBytes((byte*)bytes, 4);
        }

        private unsafe void Read(BoostaCoke obj)
        {
            ReadEa(obj);
            chunk.Read(obj, 0x54 + 0x24, 0x44);
            Read(obj.animSpriteDraw1);
            Read(obj.animSpriteDraw2);
            Read(obj.animSpriteDraws[0]);
            Read(obj.animSpriteDraws[1]);
            Read(obj.animSpriteDraws[2]);
            Read(obj.animSpriteDraws[3]);
            Read(obj.tweener);
            Read(obj.random);
            Read(obj.groupDraw);
            int playerId = chunk.ReadInt();
            applyPtr.Add(() => obj.player = (Player)contrLookup[playerId]); 
            fixed (bool* bytes = obj.bools)
                chunk.ReadBytes((byte*)bytes, 4);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(Laser obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x2C, 0x3C);
            Write(obj.animSpriteDraw);
            Write(obj.lineDraw);
            Write(obj.line1);
            Write(obj.line2);
            Write(obj.groupDraw);
            if (storeAiVolumes)
                chunk.WriteInt(obj.aiVolume != null ? obj.aiVolume.actor.Id : -1);
        }

        private unsafe void Read(Laser obj)
        {
            ReadEa(obj);
            chunk.Read(obj, 0x54 + 0x2C, 0x3C);
            Read(obj.animSpriteDraw);
            Read(obj.lineDraw);
            Read(obj.line1);
            Read(obj.line2);
            Read(obj.groupDraw);
            if (storeAiVolumes)
            {
                int aiVolumeId = chunk.ReadInt();
                applyPtr.Add(() => obj.aiVolume = (AIVolume)contrLookup[aiVolumeId]);
            }
            obj.lineDraw.lines.Clear();
            obj.lineDraw.lines.Add(obj.line2);
            obj.lineDraw.lines.Add(obj.line1);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(AIVolume obj)
        {
            WriteRea(obj);
            chunk.Write(obj, 0x68 + 0x24, 0x4);
            chunk.WriteInt(obj.type.value);
            chunk.WriteInt(obj.defaultActive.value);
            chunk.WriteInt(obj.easy.value);
            chunk.WriteInt(obj.medium.value);
            chunk.WriteInt(obj.hard.value);
            chunk.WriteInt(obj.unfair.value);
        }

        private unsafe void Read(AIVolume obj)
        {
            ReadRea(obj);
            chunk.Read(obj, 0x68 + 0x24, 0x4);
            obj.type.value = chunk.ReadInt();
            obj.defaultActive.value = chunk.ReadInt();
            obj.easy.value = chunk.ReadInt();
            obj.medium.value = chunk.ReadInt();
            obj.hard.value = chunk.ReadInt();
            obj.unfair.value = chunk.ReadInt();
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(Timer obj)
        {
            WriteRea(obj);
            chunk.Write(obj, 0x68 + 0x10, 0x14);
            chunk.WriteInt(obj.unknown.value);
            chunk.WriteInt(obj.list1.Count);
            foreach (ICActorController contr in obj.list1)
                chunk.WriteInt(contr != null ? ((Player)contr).actor.Id : -1);
            chunk.WriteInt(obj.list2.Count);
            foreach (ICActorController contr in obj.list2)
                chunk.WriteInt(contr != null ? ((Player)contr).actor.Id : -1);
        }

        private unsafe void Read(Timer obj)
        {
            ReadRea(obj);
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

        private unsafe void Write(Checkpoint obj)
        {
            WriteEa(obj);
            chunk.Write(obj, 0x54 + 0x3C, 0x28);
            chunk.WriteInt(obj.helpers.Count);
            foreach (Checkpoint helper in obj.helpers)
                chunk.WriteInt(helper != null ? helper.actor.Id : -1);
            chunk.WriteInt(obj.checkpoint1 != null ? obj.checkpoint1.actor.Id : -1);
            chunk.WriteInt(obj.checkpoint2 != null ? obj.checkpoint2.actor.Id : -1);
        }

        private unsafe void Read(Checkpoint obj)
        {
            ReadEa(obj);
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

        private unsafe void Write(StraightRocket obj)
        {
            chunk.Write(obj, 0x28, 0x18);
            Write(obj.actor);
            Write(obj.spriteDraw);
            Write(obj.bounds);
        }

        private unsafe void Read(StraightRocket obj)
        {
            chunk.Read(obj, 0x28, 0x18);
            Read(obj.actor);
            Read(obj.spriteDraw);
            Read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(Player obj)
        {
            chunk.Write(obj, 0x1D4, 0x214);
            Write(obj.actor);
            Write(obj.slot);
            Write(obj.random);
            Write(obj.groupDrawComp1);
            Write(obj.animSpriteDrawComp1);
            Write(obj.animSpriteDrawComp2);
            Write(obj.animSpriteDrawComp3);
            Write(obj.animSpriteDrawComp4);
            Write(obj.animSpriteDrawComp5);
            Write(obj.animSpriteDrawComp6);
            Write(obj.spriteDrawComp1);
            Write(obj.spriteDrawComp2);
            Write(obj.spriteDrawComp3);
            Write(obj.spriteDrawComp4);
            Write(obj.spriteDrawComp5);
            Write(obj.imageDrawComp1);
            Write(obj.imageDrawComp2);
            Write(obj.imageDrawComp3);
            Write(obj.imageDrawComp4);
            Write(obj.tweener1);
            Write(obj.tweener2);
            Write(obj.tweener3);
            Write(obj.hitboxStanding);
            Write(obj.hitboxSliding);
            chunk.WriteInt(obj.grapple != null ? obj.grapple.actor.Id : -1);
            chunk.WriteInt(obj.rope != null ? obj.rope.actor.Id : -1);
            chunk.WriteInt(obj.goldenHook != null ? obj.goldenHook.actor.Id : -1);
            chunk.WriteInt(obj.shockwave != null ? obj.shockwave.actor.Id : -1);
            chunk.WriteInt(obj.droppedBomb != null ? obj.droppedBomb.actor.Id : -1);
            chunk.WriteInt(obj.freezeRay != null ? obj.freezeRay.actor.Id : -1);
            chunk.WriteInt(obj.hooked != null ? obj.hooked.actor.Id : -1);
            chunk.WriteInt(obj.unknown1 != null ? obj.unknown1.actor.Id : -1);
            chunk.WriteInt(obj.trigger1 != null ? obj.trigger1.actor.Id : -1);
            chunk.WriteInt(obj.trigger2 != null ? obj.trigger2.actor.Id : -1);
            chunk.WriteInt(obj.checkpoint1 != null ? obj.checkpoint1.actor.Id : -1);
            chunk.WriteInt(obj.checkpoint2 != null ? obj.checkpoint2.actor.Id : -1);
            chunk.WriteInt(obj.droppedObstacles.Count);
            foreach (DroppedObstacle droppedObstacle in obj.droppedObstacles)
                chunk.WriteInt(droppedObstacle != null ? droppedObstacle.actor.Id : -1);
            chunk.WriteInt(obj.fireballs.Count);
            foreach (Fireball fireball in obj.fireballs)
                chunk.WriteInt(fireball != null ? fireball.actor.Id : -1);
            chunk.WriteInt(obj.rockets.Count);
            foreach (Rocket rocket in obj.rockets)
                chunk.WriteInt(rocket != null ? rocket.actor.Id : -1);
            chunk.WriteInt(obj.boostaCokes.Count);
            foreach (BoostaCoke boostaCoke in obj.boostaCokes)
                chunk.WriteInt(boostaCoke != null ? boostaCoke.actor.Id : -1);
        }

        private unsafe void Read(Player obj)
        {
            chunk.Read(obj, 0x1D4, 0x214);
            Read(obj.actor);
            Read(obj.slot);
            Read(obj.random);
            Read(obj.groupDrawComp1);
            Read(obj.animSpriteDrawComp1);
            Read(obj.animSpriteDrawComp2);
            Read(obj.animSpriteDrawComp3);
            Read(obj.animSpriteDrawComp4);
            Read(obj.animSpriteDrawComp5);
            Read(obj.animSpriteDrawComp6);
            Read(obj.spriteDrawComp1);
            Read(obj.spriteDrawComp2);
            Read(obj.spriteDrawComp3);
            Read(obj.spriteDrawComp4);
            Read(obj.spriteDrawComp5);
            Read(obj.imageDrawComp1);
            Read(obj.imageDrawComp2);
            Read(obj.imageDrawComp3);
            Read(obj.imageDrawComp4);
            Read(obj.tweener1);
            Read(obj.tweener2);
            Read(obj.tweener3);
            Read(obj.hitboxStanding);
            Read(obj.hitboxSliding);
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
            int ropeId = chunk.ReadInt();
            int goldenHookId = chunk.ReadInt();
            int shockwaveId = chunk.ReadInt();
            int droppedBombId = chunk.ReadInt();
            int freezeRayId = chunk.ReadInt();
            int hookedId = chunk.ReadInt();
            int unknown1Id = chunk.ReadInt();
            int trigger1Id = chunk.ReadInt();
            int trigger2Id = chunk.ReadInt();
            int checkpoint1Id = chunk.ReadInt();
            int checkpoint2Id = chunk.ReadInt();
            applyPtr.Add(() => obj.grapple = (Grapple)contrLookup[grappleId]);
            applyPtr.Add(() => obj.rope = (Rope)contrLookup[ropeId]);
            applyPtr.Add(() => obj.goldenHook = (GoldenHook)contrLookup[goldenHookId]);
            applyPtr.Add(() => obj.shockwave = (Shockwave)contrLookup[shockwaveId]);
            applyPtr.Add(() => obj.droppedBomb = (DroppedBomb)contrLookup[droppedBombId]);
            applyPtr.Add(() => obj.freezeRay = (FreezeRay)contrLookup[freezeRayId]);
            applyPtr.Add(() => obj.hooked = (Player)contrLookup[hookedId]);
            applyPtr.Add(() => obj.unknown1 = (Player)contrLookup[unknown1Id]);
            applyPtr.Add(() => obj.trigger1 = (Trigger)contrLookup[trigger1Id]);
            applyPtr.Add(() => obj.trigger2 = (Trigger)contrLookup[trigger2Id]);
            applyPtr.Add(() => obj.checkpoint1 = (Checkpoint)contrLookup[checkpoint1Id]);
            applyPtr.Add(() => obj.checkpoint1 = (Checkpoint)contrLookup[checkpoint2Id]);
            int droppedObstacles = chunk.ReadInt();
            while (obj.droppedObstacles.Count < droppedObstacles) obj.droppedObstacles.Add(null);
            while (obj.droppedObstacles.Count > droppedObstacles) obj.droppedObstacles.RemoveAt(obj.droppedObstacles.Count - 1);
            for (int i = 0; i < droppedObstacles; i++)
            {
                int droppedObstacleId = chunk.ReadInt();
                int j = i;
                applyPtr.Add(() => obj.droppedObstacles[j] = (DroppedObstacle)contrLookup[droppedObstacleId]);
            }
            int fireballs = chunk.ReadInt();
            while (obj.fireballs.Count < fireballs) obj.fireballs.Add(null);
            while (obj.fireballs.Count > fireballs) obj.fireballs.RemoveAt(obj.fireballs.Count - 1);
            for (int i = 0; i < fireballs; i++)
            {
                int fireballId = chunk.ReadInt();
                int j = i;
                applyPtr.Add(() => obj.fireballs[j] = (Fireball)contrLookup[fireballId]);
            }
            int rockets = chunk.ReadInt();
            while (obj.rockets.Count < rockets) obj.rockets.Add(null);
            while (obj.rockets.Count > rockets) obj.rockets.RemoveAt(obj.rockets.Count - 1);
            for (int i = 0; i < rockets; i++)
            {
                int rocketId = chunk.ReadInt();
                int j = i;
                applyPtr.Add(() => obj.rockets[j] = (Rocket)contrLookup[rocketId]);
            }
            int boostaCokes = chunk.ReadInt();
            while (obj.boostaCokes.Count < boostaCokes) obj.boostaCokes.Add(null);
            while (obj.boostaCokes.Count > boostaCokes) obj.boostaCokes.RemoveAt(obj.boostaCokes.Count - 1);
            for (int i = 0; i < boostaCokes; i++)
            {
                int boostacokeId = chunk.ReadInt();
                int j = i;
                applyPtr.Add(() => obj.boostaCokes[j] = (BoostaCoke)contrLookup[boostacokeId]);
            }
            obj.actor.UpdateCollision();
        }

        private unsafe void Write(PlayerBot obj)
        {
            Write((Player)obj);
            chunk.Write(obj, 0x1D4 + 0x214 + 0x4, 0x48);
            chunk.WriteInt(obj.goldenHookBot != null ? obj.goldenHookBot.actor.Id : -1);
        }

        private unsafe void Read(PlayerBot obj)
        {
            Read((Player)obj);
            chunk.Read(obj, 0x1D4 + 0x214 + 0x4, 0x48);
            int hookId = chunk.ReadInt();
            applyPtr.Add(() => obj.goldenHookBot = (GoldenHook)contrLookup[hookId]);
            obj.timespanBot = new TimeSpan(obj.timespanBot.Ticks + dt);
        }

        private unsafe void Write(ShakeCameraModifier obj)
        {
            chunk.Write(obj, 0x14, 0x34);
            Write(obj.random);
        }

        private unsafe void Read(ShakeCameraModifier obj)
        {
            chunk.Read(obj, 0x14, 0x34);
            Read(obj.random);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            obj.timespan2 = new TimeSpan(obj.timespan2.Ticks + dt);
        }

        private unsafe void Write(ClampCameraModifier obj)
        {
            chunk.Write(obj, 0x8, 0x30);
        }

        private unsafe void Read(ClampCameraModifier obj)
        {
            chunk.Read(obj, 0x8, 0x30);
        }

        private unsafe void Write(Camera obj)
        {
            chunk.Write(obj, 0x10, 0x3C);
        }

        private unsafe void Read(Camera obj)
        {
            chunk.Read(obj, 0x10, 0x3C);
        }

        private unsafe void Write(CameraMP obj)
        {
            chunk.Write(obj, 0x14, 0x34);
        }

        private unsafe void Read(CameraMP obj)
        {
            chunk.Read(obj, 0x14, 0x34);
        }

        private unsafe void Write(CCamera obj)
        {
            chunk.Write(obj, 0x1C, 0xA8);
            Write(obj.shakeMod);
            Write(obj.clampMod);
            if (obj.mods[0] is Camera camera)
                Write(camera);
            else if (obj.mods[0] is CameraMP cameraMP)
                Write(cameraMP);
        }

        private unsafe void Read(CCamera obj)
        {
            chunk.Read(obj, 0x1C, 0xA8);
            Read(obj.shakeMod);
            Read(obj.clampMod);
            if (obj.mods[0] is Camera camera)
                Read(camera);
            else if (obj.mods[0] is CameraMP cameraMP)
                Read(cameraMP);
        }

        private unsafe void Write(ModuleSolo obj)
        {
            chunk.Write(obj, 0x74, 0x4C);
            Write(obj.random);
            Write(obj.camera);
        }

        private unsafe void Read(ModuleSolo obj)
        {
            chunk.Read(obj, 0x74, 0x4C);
            Read(obj.random);
            Read(obj.camera);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
        }

        private unsafe void Write(ModuleMP obj)
        {
            chunk.Write(obj, 0xA8, 0xB8);
            Write(obj.random);
            Write(obj.camera);
        }

        private unsafe void Read(ModuleMP obj)
        {
            chunk.Read(obj, 0xA8, 0xB8);
            Read(obj.random);
            Read(obj.camera);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            obj.timespan4 = new TimeSpan(obj.timespan4.Ticks + dt);
        }

        private bool empty = true;
        private CCollisionEngine collisionEngine;
        private CWorld world;
        private Stack stack;
        private readonly Chunk chunk;
        private long time;
        private long dt;
        private bool storeAiVolumes;
        private readonly NullSafeDict<int, ICActorController> contrLookup = new NullSafeDict<int, ICActorController>();
        private readonly List<Action> applyPtr = new List<Action>();
        private readonly HashSet<CActor> fixedIdActors = new HashSet<CActor>();
        private readonly HashSet<int> fixedIds = new HashSet<int>();

        public Savestate()
        {
            chunk = new Chunk();
        }

        public void Save(bool storeAiVolumes)
        {
            if (!Velo.Ingame || Velo.Online)
                return;
            empty = false;
            this.storeAiVolumes = storeAiVolumes;
            collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            world = CEngine.CEngine.Instance.World;
            stack = Main.game.stack;
            int count = collisionEngine.ActorCount;
            chunk.Start();

            for (int i = 0; i < count; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                if (actor.ghostOwnedItem)
                    continue;
                ICActorController controller = actor.Controller;
                if (controller is Player player)
                {
                    if (!player.actor.localPlayer && !player.slot.IsBot)
                        continue;

                    if (player is PlayerBot playerBot)
                    {
                        chunk.WriteInt(-1);
                        chunk.WriteInt(playerBot.slot.Index);
                        Write(playerBot);
                        continue;
                    }
                    
                    chunk.WriteInt(0);
                    chunk.WriteInt(player.slot.Index);
                    Write(player);
                }
                else if (controller is Grapple grapple)
                {
                    chunk.WriteInt(1);
                    Write(grapple);
                }
                else if (controller is Rope rope)
                {
                    chunk.WriteInt(2);
                    Write(rope);
                }
                else if (controller is Fireball fireball)
                {
                    chunk.WriteInt(3);
                    Write(fireball);
                }
                else if (controller is DroppedObstacle droppedObstacle)
                {
                    chunk.WriteInt(4);
                    Write(droppedObstacle);
                }
                else if (controller is Rocket rocket)
                {
                    chunk.WriteInt(5);
                    Write(rocket);
                }
                else if (controller is GoldenHook goldenHook)
                {
                    chunk.WriteInt(6);
                    Write(goldenHook);
                }
                else if (controller is Shockwave shockwave)
                {
                    chunk.WriteInt(7);
                    Write(shockwave);
                }
                else if (controller is DroppedBomb droppedBomb)
                {
                    chunk.WriteInt(8);
                    Write(droppedBomb);
                }
                else if (controller is Obstacle obstacle)
                {
                    chunk.WriteInt(9);
                    Write(obstacle);
                }
                else if (controller is FreezeRay freezeRay)
                {
                    chunk.WriteInt(10);
                    Write(freezeRay);
                }
                else if (controller is Pickup pickup)
                {
                    chunk.WriteInt(11);
                    Write(pickup);
                }
                else if (controller is Trigger trigger)
                {
                    chunk.WriteInt(12);
                    Write(trigger);
                }
                else if (controller is SwitchBlock switchBlock)
                {
                    chunk.WriteInt(13);
                    Write(switchBlock);
                }
                else if (controller is FallTile fallTile)
                {
                    chunk.WriteInt(14);
                    Write(fallTile);
                }
                else if (controller is TriggerSaw triggerSaw)
                {
                    chunk.WriteInt(15);
                    Write(triggerSaw);
                }
                else if (controller is RocketLauncher rocketLauncher)
                {
                    chunk.WriteInt(16);
                    Write(rocketLauncher);
                }
                else if (controller is BoostaCoke boostaCoke)
                {
                    chunk.WriteInt(17);
                    Write(boostaCoke);
                }
                else if (controller is Laser laser)
                {
                    chunk.WriteInt(18);
                    Write(laser);
                }
                else if (controller is AIVolume aiVolume)
                {
                    if (!storeAiVolumes)
                        continue;
                    chunk.WriteInt(19);
                    Write(aiVolume);
                }
                else if (controller is Timer timer)
                {
                    chunk.WriteInt(20);
                    Write(timer);
                }
                else if (controller is Checkpoint checkpoint)
                {
                    chunk.WriteInt(21);
                    Write(checkpoint);
                }
                else if (controller is StraightRocket straightRocket)
                {
                    chunk.WriteInt(22);
                    Write(straightRocket);
                }
            }

            chunk.WriteInt(-2);

            foreach (var module in stack.modules)
            {
                if (module is ModuleSolo moduleSolo)
                {
                    Write(moduleSolo);
                    break;
                }
                else if (module is ModuleMP moduleMP)
                {
                    Write(moduleMP);
                    break;
                }
            }

            time = CEngine.CEngine.Instance.GameTime.TotalGameTime.Ticks;
        }

        private T GetOfType<T>(int n, Func<T, bool> func = null) where T : ICActorController
        {
            int c = 0;

            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                ICActorController controller = actor.controller;
                if (controller is T t && !actor.ghostOwnedItem && (func == null || func(t)))
                {
                    if (c == n)
                        return t;
                    else
                        c++;
                }
            }

            return default;
        }

        public void DestroyAllAfter<T>(int n)
        {
            int c = 0;

            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                if (actor.controller is T && !actor.ghostOwnedItem)
                {
                    if (c >= n)
                        world.DestroyActor(actor);
                    else
                        c++;
                }
            }
        }

#pragma warning disable IDE1006
        public static void actor_update(CActor actor)
        {
            ICActorController controller = actor.controller;
            if (controller is Grapple grapple)
            {
                if (grapple.owner != null && !grapple.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Rope rope)
            {
                if (rope.owner != null && !rope.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Fireball fireball)
            {
                if (fireball.owner != null && !fireball.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is DroppedObstacle droppedObstacle)
            {
                if (droppedObstacle.owner != null && !droppedObstacle.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Rocket rocket)
            {
                if (rocket.owner != null && !rocket.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is GoldenHook goldenHook)
            {
                if (goldenHook.owner != null && !goldenHook.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Shockwave shockwave)
            {
                if (shockwave.owner != null && !shockwave.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is DroppedBomb droppedBomb)
            {
                if (droppedBomb.owner != null && !droppedBomb.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is FreezeRay freezeRay)
            {
                if (freezeRay.owner != null && !freezeRay.owner.slot.LocalPlayer)
                    actor.ghostOwnedItem = true;
            }
        }
#pragma warning restore IDE1006

        public bool Load()
        {
            if (empty || !Velo.Ingame || Velo.Online)
                return false;
            collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            world = CEngine.CEngine.Instance.World;
            stack = Main.game.stack;
            dt = CEngine.CEngine.Instance.GameTime.TotalGameTime.Ticks - time;
            chunk.Start();

            contrLookup.Add(-1, null);

            int[] counts = new int[23];

            while (true)
            {
                int type = chunk.ReadInt();

                if (type == -2)
                    break;

                if (type == -1)
                {
                    int index = chunk.ReadInt();
                    Player player = GetOfType<Player>(0, (check) => check.slot.Index == index);
                    Read((PlayerBot)player);
                    fixedIdActors.Add(player.actor);
                }
                else if (type == 0)
                {
                    int index = chunk.ReadInt();
                    Player player = GetOfType<Player>(0, (check) => check.slot.Index == index);
                    Read(player);
                    fixedIdActors.Add(player.actor);
                }
                else if (type == 1)
                {
                    Grapple grapple = GetOfType<Grapple>(counts[1]++) ?? (Grapple)world.SpawnActor(new GrappleDef(0, null));
                    Read(grapple);
                    fixedIdActors.Add(grapple.actor);
                }
                else if (type == 2)
                {
                    Rope rope = GetOfType<Rope>(counts[2]++) ?? (Rope)world.SpawnActor(new RopeDef());
                    Read(rope);
                    fixedIdActors.Add(rope.actor);
                }
                else if (type == 3)
                {
                    Fireball fireball = GetOfType<Fireball>(counts[3]++) ?? (Fireball)world.SpawnActor(new FireballDef(Vector2.Zero));
                    Read(fireball);
                    fixedIdActors.Add(fireball.actor);
                }
                else if (type == 4)
                {
                    DroppedObstacle droppedObstacle = GetOfType<DroppedObstacle>(counts[4]++) ?? (DroppedObstacle)world.SpawnActor(new DroppedObstacleDef(null, false));
                    Read(droppedObstacle);
                    fixedIdActors.Add(droppedObstacle.actor);
                }
                else if (type == 5)
                {
                    Rocket rocket = GetOfType<Rocket>(counts[5]++) ?? (Rocket)world.SpawnActor(new RocketDef(null, stack.gameInfo));
                    Read(rocket);
                    fixedIdActors.Add(rocket.actor);
                }
                else if (type == 6)
                {
                    GoldenHook goldenHook = GetOfType<GoldenHook>(counts[6]++) ?? (GoldenHook)world.SpawnActor(new GoldenHookDef(0, null));
                    Read(goldenHook);
                    fixedIdActors.Add(goldenHook.actor);
                }
                else if (type == 7)
                {
                    Shockwave shockwave = GetOfType<Shockwave>(counts[7]++) ?? (Shockwave)world.SpawnActor(new ShockwaveDef(null));
                    Read(shockwave);
                    fixedIdActors.Add(shockwave.actor);
                }
                else if (type == 8)
                {
                    DroppedBomb droppedBomb = GetOfType<DroppedBomb>(counts[8]++) ?? (DroppedBomb)world.SpawnActor(new DroppedBombDef(Color.Black, null));
                    Read(droppedBomb);
                    fixedIdActors.Add(droppedBomb.actor);
                }
                else if (type == 9)
                {
                    Obstacle obstacle = GetOfType<Obstacle>(counts[9]++);
                    if (obstacle != null)
                        Read(obstacle);
                    fixedIdActors.Add(obstacle.actor);
                }
                else if (type == 10)
                {
                    FreezeRay freezeRay = GetOfType<FreezeRay>(counts[10]++) ?? (FreezeRay)world.SpawnActor(new FreezeRayDef(Vector2.Zero, null));
                    Read(freezeRay);
                    fixedIdActors.Add(freezeRay.actor);
                }
                else if (type == 11)
                {
                    Pickup pickup = GetOfType<Pickup>(counts[11]++);
                    if (pickup != null)
                        Read(pickup);
                    fixedIdActors.Add(pickup.actor);
                }
                else if (type == 12)
                {
                    Trigger trigger = GetOfType<Trigger>(counts[12]++);
                    if (trigger != null)
                        Read(trigger);
                    fixedIdActors.Add(trigger.actor);
                }
                else if (type == 13)
                {
                    SwitchBlock switchBlock = GetOfType<SwitchBlock>(counts[13]++);
                    if (switchBlock != null)
                        Read(switchBlock);
                    fixedIdActors.Add(switchBlock.actor);
                }
                else if (type == 14)
                {
                    FallTile fallTile = GetOfType<FallTile>(counts[14]++);
                    if (fallTile != null)
                        Read(fallTile);
                    fixedIdActors.Add(fallTile.actor);
                }
                else if (type == 15)
                {
                    TriggerSaw triggerSaw = GetOfType<TriggerSaw>(counts[15]++);
                    if (triggerSaw != null)
                        Read(triggerSaw);
                    fixedIdActors.Add(triggerSaw.actor);
                }
                else if (type == 16)
                {
                    RocketLauncher rocketLauncher = GetOfType<RocketLauncher>(counts[16]++);
                    if (rocketLauncher != null)
                        Read(rocketLauncher);
                    fixedIdActors.Add(rocketLauncher.actor);
                }
                else if (type == 17)
                {
                    BoostaCoke boostaCoke = GetOfType<BoostaCoke>(counts[17]++);
                    if (boostaCoke != null)
                        Read(boostaCoke);
                    fixedIdActors.Add(boostaCoke.actor);
                }
                else if (type == 18)
                {
                    Laser laser = GetOfType<Laser>(counts[18]++);
                    if (laser != null)
                        Read(laser);
                    fixedIdActors.Add(laser.actor);
                }
                else if (type == 19)
                {
                    AIVolume aiVolume = GetOfType<AIVolume>(counts[19]++) ?? (AIVolume)world.SpawnActor(new AIVolumeDef(Vector2.Zero, Vector2.Zero, 0));
                    Read(aiVolume);
                    fixedIdActors.Add(aiVolume.actor);
                }
                else if (type == 20)
                {
                    Timer timer = GetOfType<Timer>(counts[20]++);
                    if (timer != null)
                        Read(timer);
                    fixedIdActors.Add(timer.actor);
                }
                else if (type == 21)
                {
                    Checkpoint checkpoint = GetOfType<Checkpoint>(counts[21]++);
                    if (checkpoint != null)
                        Read(checkpoint);
                    fixedIdActors.Add(checkpoint.actor);
                }
                else if (type == 22)
                {
                    StraightRocket straightRocket = GetOfType<StraightRocket>(counts[22]++) ?? (StraightRocket)world.SpawnActor(new StraightRocketDef(stack.gameInfo));
                    Read(straightRocket);
                    fixedIdActors.Add(straightRocket.actor);
                }
            }

            foreach (var actor in fixedIdActors)
                fixedIds.Add(actor.Id);

            DestroyAllAfter<Grapple>(counts[1]);
            DestroyAllAfter<Rope>(counts[2]);
            DestroyAllAfter<Fireball>(counts[3]);
            DestroyAllAfter<DroppedObstacle>(counts[4]);
            DestroyAllAfter<Rocket>(counts[5]);
            DestroyAllAfter<GoldenHook>(counts[6]);
            DestroyAllAfter<Shockwave>(counts[7]);
            DestroyAllAfter<DroppedBomb>(counts[8]);
            DestroyAllAfter<FreezeRay>(counts[10]);
            if (storeAiVolumes)
                DestroyAllAfter<AIVolume>(counts[19]);
            DestroyAllAfter<StraightRocket>(counts[22]);

            foreach (var module in stack.modules)
            {
                if (module is ModuleSolo moduleSolo)
                {
                    Read(moduleSolo);
                    break;
                }
                else if (module is ModuleMP moduleMP)
                {
                    Read(moduleMP);
                    break;
                }
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

        private Savestates() : base("Savestates")
        {
            SaveKey = AddHotkey("save key", (ushort)Keys.NumPad0);
            LoadKey = AddHotkey("load key", (ushort)Keys.NumPad1);
            LoadHaltDuration = AddInt("load halt duration", 0, 0, 5000);
            StoreAIVolumes = AddBool("store AI volumes", false);

            savestate = new Savestate();
        }

        public static Savestates Instance = new Savestates();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Keyboard.Pressed[SaveKey.Value])
                savestate.Save(StoreAIVolumes.Value);

            if (Keyboard.Pressed[LoadKey.Value])
            {
                if (savestate.Load())
                    savestateLoadTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }
        }
    }
}
