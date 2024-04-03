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
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using XNATweener;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

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

        private void ensure(int size)
        {
            if (data.Length < size)
                Array.Resize(ref data, data.Length * 2);
        }

        public void Start() { index = 0; }

        public unsafe void Write(object src, int off, int data_size)
        {
            ensure(index + data_size);
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
            ensure(index + 4);
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
            ensure(index + size);

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

        public unsafe void WriteArr<T>(T[] arr)
        {
            if (arr == null)
            {
                WriteInt(-1);
                return;
            }

            WriteInt(arr.Length);
            fixed (T* bytes = arr)
            {
                WriteBytes((byte*)bytes, arr.Length * sizeof(T));
            }
        }

        public unsafe T[] ReadArr<T>()
        {
            int size = ReadInt();
            if (size == -1)
                return null;
            T[] bytes = new T[size];

            fixed (T* data_dst = bytes)
            {
                ReadBytes((byte*)data_dst, size * sizeof(T));
            }
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
        private unsafe void write(CAABB obj)
        {
            chunk.Write(obj, 0x4, 0x18);
        }

        private unsafe void read(CAABB obj)
        {
            chunk.Read(obj, 0x4, 0x18);
        }

        private unsafe void write(CConvexPolygon obj)
        {
            chunk.Write(obj, 0xC, 0x1C);
            chunk.WriteArr(obj.localVertices);
            chunk.WriteArr(obj.vertices);
        }

        private unsafe void read(CConvexPolygon obj)
        {
            chunk.Read(obj, 0xC, 0x1C);
            obj.localVertices = chunk.ReadArr<Vector2>();
            obj.vertices = chunk.ReadArr<Vector2>();
        }

        private unsafe void write(CActor obj)
        {
            chunk.Write(obj, 0x20, 0x40);
            write(obj.bounds);
        }

        private unsafe void read(CActor obj)
        {
            chunk.Read(obj, 0x20, 0x40);
            read(obj.bounds);
            fixedIdActors.Add(obj);
            fixedIds.Add(obj.id);
        }

        private unsafe void write(CSpriteDrawComponent obj)
        {
            chunk.Write(obj, 0x20, 0x6C);
            write(obj.bounds);
        }

        private unsafe void read(CSpriteDrawComponent obj)
        {
            chunk.Read(obj, 0x20, 0x6C);
            read(obj.bounds);
        }

        private unsafe void write(Dictionary<string, CAnimation> obj)
        {
            chunk.WriteInt(obj.Count);
            foreach (var pair in obj)
            {
                chunk.WriteStr(pair.Key);
                chunk.WriteInt((int)MemUtil.GetPtr(pair.Value));
            }
        }

        private unsafe void read(NullSafeDict<int, string> obj)
        {
            int count = chunk.ReadInt();
            for (int i = 0; i < count; i++)
            {
                string key = chunk.ReadStr();
                int value = chunk.ReadInt();
                obj.Add(value, key);
            }
        }

        private unsafe void write(CAnimatedSpriteDrawComponent obj)
        {
            chunk.Write(obj, 0x2C, 0x84);
            write(obj.bounds);
            chunk.WriteStr(obj.nextAnimation);
            chunk.WriteStr(obj.animation.id);
        }

        private unsafe void read(CAnimatedSpriteDrawComponent obj)
        {
            chunk.Read(obj, 0x2C, 0x84);
            read(obj.bounds);
            obj.nextAnimation = chunk.ReadStr();
            obj.timeSpan1 = new TimeSpan(obj.timeSpan1.Ticks + dt);
            string animationId = chunk.ReadStr();
            obj.animation = obj.animImage.GetAnimation(animationId);
        }

        private unsafe void write(CImageDrawComponent obj)
        {
            chunk.Write(obj, 0x1C, 0x58);
            write(obj.bounds);
        }

        private unsafe void read(CImageDrawComponent obj)
        {
            chunk.Read(obj, 0x1C, 0x58);
            read(obj.bounds);
        }

        private unsafe void write(CGroupDrawComponent obj)
        {
            chunk.Write(obj, 0x18, 0x58);
        }

        private unsafe void read(CGroupDrawComponent obj)
        {
            chunk.Read(obj, 0x18, 0x58);
        }

        private unsafe void write(CLine obj)
        {
            chunk.Write(obj, 0x4, 0x18);
        }

        private unsafe void read(CLine obj)
        {
            chunk.Read(obj, 0x4, 0x18);
        }

        private unsafe void write(CLineDrawComponent obj)
        {
            chunk.Write(obj, 0x18, 0x4);
            chunk.WriteInt(obj.lines.Count);
            foreach (CLine line in obj.lines)
                write(line);
        }

        private unsafe void read(CLineDrawComponent obj)
        {
            chunk.Read(obj, 0x18, 0x4);
            int count = chunk.ReadInt();
            obj.lines = new List<CLine>(count);
            for (int i = 0; i < count; i++)
            {
                CLine line = new CLine(Vector2.Zero, Vector2.Zero, Color.Black);
                read(line);
                obj.lines.Add(line);
            }
        }

        private unsafe void write(Tweener obj)
        {
            chunk.Write(obj, 0x10, 0x1C);
        }

        private unsafe void read(Tweener obj)
        {
            chunk.Read(obj, 0x10, 0x1C);
        }

        private unsafe void write(Slot obj)
        {
            chunk.Write(obj, 0x68, 0x3C);
        }

        private unsafe void read(Slot obj)
        {
            chunk.Read(obj, 0x68, 0x3C);
        }

        private unsafe void write(Random obj)
        {
            chunk.Write(obj, 0x8, 0x8);
            chunk.Write(obj, 0x1C, 0xE0);
        }

        private unsafe void read(Random obj)
        {
            chunk.Read(obj, 0x8, 0x8);
            chunk.Read(obj, 0x1C, 0xE0);
        }

        private unsafe void write(Grapple obj)
        {
            chunk.Write(obj, 0x24, 0x14);
            write(obj.actor);
            write(obj.animSpriteDrawComp1);
            write(obj.spriteDrawComp1);
            write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private unsafe void read(Grapple obj)
        {
            chunk.Read(obj, 0x24, 0x14);
            read(obj.actor);
            read(obj.animSpriteDrawComp1);
            read(obj.spriteDrawComp1);
            read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(Rope obj)
        {
            chunk.Write(obj, 0x20, 0x24);
            write(obj.actor);
            write(obj.line1);
            write(obj.line2);
            write(obj.line3);
            write(obj.lineDrawComp1);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            if (obj.target == null)
                chunk.WriteInt(-1);
            else if (obj.target is Grapple grapple)
                chunk.WriteInt(grapple.actor.Id);
             else if (obj.target is GoldenHook goldenHook)
                chunk.WriteInt(goldenHook.actor.Id);
        }

        private unsafe void read(Rope obj)
        {
            chunk.Read(obj, 0x20, 0x24);
            read(obj.actor);
            read(obj.line1);
            read(obj.line2);
            read(obj.line3);
            read(obj.lineDrawComp1);
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

        private unsafe void write(DroppedObstacle obj)
        {
            chunk.Write(obj, 0x24, 0x24);
            write(obj.actor);
            write(obj.spriteDraw1);
            write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            if (storeAiVolumes)
                chunk.WriteInt(obj.aiVolume != null ? obj.aiVolume.actor.Id : -1);
        }

        private unsafe void read(DroppedObstacle obj)
        {
            chunk.Read(obj, 0x24, 0x24);
            read(obj.actor);
            read(obj.spriteDraw1);
            read(obj.bounds);
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

        private unsafe void write(Fireball obj)
        {
            chunk.Write(obj, 0x3C, 0x3C);
            write(obj.actor);
            write(obj.animSpriteDraw);
            write(obj.bounds);
            if (obj.animSpriteDraw.Sprite == null)
                chunk.WriteInt(0);
            else if (obj.animSpriteDraw.Sprite == obj.animImage1)
                chunk.WriteInt(1);
            else
                chunk.WriteInt(2);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            chunk.WriteInt(obj.shockwave != null ? obj.shockwave.actor.Id : -1);
        }

        private unsafe void read(Fireball obj)
        {
            chunk.Read(obj, 0x3C, 0x3C);
            read(obj.actor);
            read(obj.animSpriteDraw);
            read(obj.bounds);
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

        private unsafe void write(Rocket obj)
        {
            chunk.Write(obj, 0x38, 0x38);
            write(obj.actor);
            write(obj.spriteDrawComp);
            write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            chunk.WriteInt(obj.target != null ? obj.target.actor.Id : -1);
            chunk.WriteInt(obj.unknown != null ? obj.unknown.actor.Id : -1);
        }

        private unsafe void read(Rocket obj)
        {
            chunk.Read(obj, 0x38, 0x38);
            read(obj.actor);
            read(obj.spriteDrawComp);
            read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            int targetId = chunk.ReadInt();
            int unknownId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            applyPtr.Add(() => obj.unknown = (Player)contrLookup[unknownId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(GoldenHook obj)
        {
            chunk.Write(obj, 0x30, 0x1C);
            write(obj.actor);
            write(obj.spriteDraw);
            write(obj.animSpriteDraw);
            write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
            chunk.WriteInt(obj.target != null ? obj.target.actor.Id : -1);
            chunk.WriteInt(obj.unknown != null ? obj.unknown.actor.Id : -1);
        }

        private unsafe void read(GoldenHook obj)
        {
            chunk.Read(obj, 0x30, 0x1C);
            read(obj.actor);
            read(obj.spriteDraw);
            read(obj.animSpriteDraw);
            read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            int targetId = chunk.ReadInt();
            int unknownId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            applyPtr.Add(() => obj.unknown = (Player)contrLookup[unknownId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(Shockwave obj)
        {
            chunk.Write(obj, 0x2C, 0x30);
            write(obj.actor);
            write(obj.animSpriteDraw1);
            write(obj.animSpriteDraw2);
            write(obj.animSpriteDraw3);
            write(obj.animSpriteDraw4);
            write(obj.animSpriteDraw5);
            write(obj.groupDraw);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private unsafe void read(Shockwave obj)
        {
            chunk.Read(obj, 0x2C, 0x30);
            read(obj.actor);
            read(obj.animSpriteDraw1);
            read(obj.animSpriteDraw2);
            read(obj.animSpriteDraw3);
            read(obj.animSpriteDraw4);
            read(obj.animSpriteDraw5);
            read(obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(DroppedBomb obj)
        {
            chunk.Write(obj, 0x28, 0x18);
            write(obj.actor);
            write(obj.bounds);
            write(obj.animSpriteDraw1);
            write(obj.animSpriteDraw2);
            write(obj.groupDraw);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private unsafe void read(DroppedBomb obj)
        {
            chunk.Read(obj, 0x28, 0x18);
            read(obj.actor);
            read(obj.bounds);
            read(obj.animSpriteDraw1);
            read(obj.animSpriteDraw2);
            read(obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void writeEa(EditableActor obj)
        {
            chunk.Write(obj, 0x30, 0x24);
            write(obj.actor);
            write(obj.bounds);
        }

        private unsafe void readEa(EditableActor obj)
        {
            chunk.Read(obj, 0x30, 0x24);
            read(obj.actor);
            read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
        }

        private unsafe void write(Obstacle obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x14, 0x20);
            write(obj.spriteDraw);
            write(obj.groupDraw);
            if (storeAiVolumes)
            {
                chunk.WriteInt(obj.aiVolume1 != null ? obj.aiVolume1.actor.Id : -1);
                chunk.WriteInt(obj.aiVolume2 != null ? obj.aiVolume2.actor.Id : -1);
            }
        }

        private unsafe void read(Obstacle obj)
        {
            readEa(obj);
            chunk.Read(obj, 0x54 + 0x14, 0x20);
            read(obj.spriteDraw);
            read(obj.groupDraw);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            if (storeAiVolumes)
            {
                int aiVolume1Id = chunk.ReadInt();
                int aiVolume2Id = chunk.ReadInt();
                applyPtr.Add(() => obj.aiVolume1 = (AIVolume)contrLookup[aiVolume1Id]);
                applyPtr.Add(() => obj.aiVolume2 = (AIVolume)contrLookup[aiVolume2Id]);
            }
        }

        private unsafe void write(FreezeRay obj)
        {
            chunk.Write(obj, 0x18, 0x18);
            write(obj.actor);
            chunk.WriteInt(obj.animSpriteDraws.Length);
            foreach (var animSpriteDraw in obj.animSpriteDraws)
                write(animSpriteDraw);
            write(obj.groupDraw);
            write(obj.bounds);
            chunk.WriteInt(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private unsafe void read(FreezeRay obj)
        {
            chunk.Read(obj, 0x18, 0x18);
            read(obj.actor);
            int count = chunk.ReadInt();
            for (int i = 0; i < count; i++)
            {
                if (i >= obj.animSpriteDraws.Length)
                {
                    CAnimatedSpriteDrawComponent dummy = new CAnimatedSpriteDrawComponent();
                    read(dummy);
                    continue;
                }

                read(obj.animSpriteDraws[i]);
            }
            read(obj.groupDraw);
            read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = chunk.ReadInt();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(Pickup obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x10, 0xC);
            write(obj.animSpriteDraw1);
            write(obj.animSpriteDraw2);
            chunk.WriteInt(obj.imageDraw3 != null ? 1 : 0);
            if (obj.imageDraw3 != null)
            {
                write(obj.imageDraw3);
                write(obj.imageDraw4);
            }
            write(obj.groupDraw);
        }

        private unsafe void read(Pickup obj)
        {
            readEa(obj);
            chunk.Read(obj, 0x54 + 0x10, 0xC);
            read(obj.animSpriteDraw1);
            read(obj.animSpriteDraw2);
            int notNull = chunk.ReadInt();
            if (notNull != 0)
            {
                read(obj.imageDraw3);
                read(obj.imageDraw4);
            }
            read(obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private unsafe void writeRea(ResizableEditableActor obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x0, 0x14);
        }

        private unsafe void readRea(ResizableEditableActor obj)
        {
            readEa(obj);
            chunk.Read(obj, 0x54 + 0x0, 0x14);
        }

        private unsafe void write(Trigger obj)
        {
            writeRea(obj);
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

        private unsafe void read(Trigger obj)
        {
            readRea(obj);
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

        private unsafe void write(SwitchBlock obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x24, 0x28);
            write(obj.animSpriteDraw1);
            write(obj.animSpriteDraw2);
            write((CConvexPolygon)obj.colShape);
            write(obj.groupDraw);
        }

        private unsafe void read(SwitchBlock obj)
        {
            readEa(obj);
            chunk.Read(obj, 0x54 + 0x24, 0x28);
            read(obj.animSpriteDraw1);
            read(obj.animSpriteDraw2);
            read((CConvexPolygon)obj.colShape);
            read(obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(FallTile obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x4, 0x18);
            write(obj.animSpriteDraw);
            write(obj.groupDraw);
        }

        private unsafe void read(FallTile obj)
        {
            readEa(obj);
            chunk.Read(obj, 0x54 + 0x4, 0x18);
            read(obj.animSpriteDraw);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            read(obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(TriggerSaw obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x18, 0x28);
            write(obj.imageDraw);
            write(obj.convexPoly);
            write(obj.groupDraw);
        }

        private unsafe void read(TriggerSaw obj)
        {
            readEa(obj);
            chunk.Read(obj, 0x54 + 0x18, 0x28);
            read(obj.imageDraw);
            read(obj.convexPoly);
            read(obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(RocketLauncher obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x1C, 0xC);
            write(obj.animSpriteDraw1);
            write(obj.animSpriteDraw2);
            write(obj.groupDraw);
            chunk.WriteInt(obj.target != null ? obj.target.actor.Id : -1);
            chunk.WriteInt(obj.rockets.Count);
            foreach (Rocket rocket in obj.rockets)
                chunk.WriteInt(rocket != null ? rocket.actor.Id : -1);
        }

        private unsafe void read(RocketLauncher obj)
        {
            readEa(obj);
            chunk.Read(obj, 0x54 + 0x1C, 0xC);
            read(obj.animSpriteDraw1);
            read(obj.animSpriteDraw2);
            read(obj.groupDraw);
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

        private unsafe void write(BoostaCoke obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x24, 0x44);
            write(obj.animSpriteDraw1);
            write(obj.animSpriteDraw2);
            write(obj.animSpriteDraws[0]);
            write(obj.animSpriteDraws[1]);
            write(obj.animSpriteDraws[2]);
            write(obj.animSpriteDraws[3]);
            write(obj.tweener);
            write(obj.random);
            write(obj.groupDraw);
            chunk.WriteInt(obj.player != null ? obj.player.actor.Id : -1);
            fixed (bool* bytes = obj.bools)
                chunk.WriteBytes((byte*)bytes, 4);
        }

        private unsafe void read(BoostaCoke obj)
        {
            readEa(obj);
            chunk.Read(obj, 0x54 + 0x24, 0x44);
            read(obj.animSpriteDraw1);
            read(obj.animSpriteDraw2);
            read(obj.animSpriteDraws[0]);
            read(obj.animSpriteDraws[1]);
            read(obj.animSpriteDraws[2]);
            read(obj.animSpriteDraws[3]);
            read(obj.tweener);
            read(obj.random);
            read(obj.groupDraw);
            int playerId = chunk.ReadInt();
            applyPtr.Add(() => obj.player = (Player)contrLookup[playerId]); 
            fixed (bool* bytes = obj.bools)
                chunk.ReadBytes((byte*)bytes, 4);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(Laser obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x2C, 0x3C);
            write(obj.animSpriteDraw);
            write(obj.lineDraw);
            write(obj.line1);
            write(obj.line2);
            write(obj.groupDraw);
            if (storeAiVolumes)
                chunk.WriteInt(obj.aiVolume != null ? obj.aiVolume.actor.Id : -1);
        }

        private unsafe void read(Laser obj)
        {
            readEa(obj);
            chunk.Read(obj, 0x54 + 0x2C, 0x3C);
            read(obj.animSpriteDraw);
            read(obj.lineDraw);
            read(obj.line1);
            read(obj.line2);
            read(obj.groupDraw);
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

        private unsafe void write(AIVolume obj)
        {
            writeRea(obj);
            chunk.Write(obj, 0x68 + 0x24, 0x4);
            chunk.WriteInt(obj.type.value);
            chunk.WriteInt(obj.defaultActive.value);
            chunk.WriteInt(obj.easy.value);
            chunk.WriteInt(obj.medium.value);
            chunk.WriteInt(obj.hard.value);
            chunk.WriteInt(obj.unfair.value);
        }

        private unsafe void read(AIVolume obj)
        {
            readRea(obj);
            chunk.Read(obj, 0x68 + 0x24, 0x4);
            obj.type.value = chunk.ReadInt();
            obj.defaultActive.value = chunk.ReadInt();
            obj.easy.value = chunk.ReadInt();
            obj.medium.value = chunk.ReadInt();
            obj.hard.value = chunk.ReadInt();
            obj.unfair.value = chunk.ReadInt();
            obj.actor.UpdateCollision();
        }

        private unsafe void write(Timer obj)
        {
            writeRea(obj);
            chunk.Write(obj, 0x68 + 0x10, 0x14);
            chunk.WriteInt(obj.unknown.value);
            chunk.WriteInt(obj.list1.Count);
            foreach (ICActorController contr in obj.list1)
                chunk.WriteInt(contr != null ? ((Player)contr).actor.Id : -1);
            chunk.WriteInt(obj.list2.Count);
            foreach (ICActorController contr in obj.list2)
                chunk.WriteInt(contr != null ? ((Player)contr).actor.Id : -1);
        }

        private unsafe void read(Timer obj)
        {
            readRea(obj);
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

        private unsafe void write(Checkpoint obj)
        {
            writeEa(obj);
            chunk.Write(obj, 0x54 + 0x3C, 0x28);
            chunk.WriteInt(obj.helpers.Count);
            foreach (Checkpoint helper in obj.helpers)
                chunk.WriteInt(helper != null ? helper.actor.Id : -1);
            chunk.WriteInt(obj.checkpoint1 != null ? obj.checkpoint1.actor.Id : -1);
            chunk.WriteInt(obj.checkpoint2 != null ? obj.checkpoint2.actor.Id : -1);
        }

        private unsafe void read(Checkpoint obj)
        {
            readEa(obj);
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

        private unsafe void write(StraightRocket obj)
        {
            chunk.Write(obj, 0x28, 0x18);
            write(obj.actor);
            write(obj.spriteDraw);
            write(obj.bounds);
        }

        private unsafe void read(StraightRocket obj)
        {
            chunk.Read(obj, 0x28, 0x18);
            read(obj.actor);
            read(obj.spriteDraw);
            read(obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            obj.actor.UpdateCollision();
        }

        private unsafe void write(Player obj)
        {
            chunk.Write(obj, 0x1D4, 0x214);
            write(obj.actor);
            write(obj.slot);
            write(obj.random);
            write(obj.groupDrawComp1);
            write(obj.animSpriteDrawComp1);
            write(obj.animSpriteDrawComp2);
            write(obj.animSpriteDrawComp3);
            write(obj.animSpriteDrawComp4);
            write(obj.animSpriteDrawComp5);
            write(obj.animSpriteDrawComp6);
            write(obj.spriteDrawComp1);
            write(obj.spriteDrawComp2);
            write(obj.spriteDrawComp3);
            write(obj.spriteDrawComp4);
            write(obj.spriteDrawComp5);
            write(obj.imageDrawComp1);
            write(obj.imageDrawComp2);
            write(obj.imageDrawComp3);
            write(obj.imageDrawComp4);
            write(obj.tweener1);
            write(obj.tweener2);
            write(obj.tweener3);
            write(obj.hitboxStanding);
            write(obj.hitboxSliding);
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

        private unsafe void read(Player obj)
        {
            chunk.Read(obj, 0x1D4, 0x214);
            read(obj.actor);
            read(obj.slot);
            read(obj.random);
            read(obj.groupDrawComp1);
            read(obj.animSpriteDrawComp1);
            read(obj.animSpriteDrawComp2);
            read(obj.animSpriteDrawComp3);
            read(obj.animSpriteDrawComp4);
            read(obj.animSpriteDrawComp5);
            read(obj.animSpriteDrawComp6);
            read(obj.spriteDrawComp1);
            read(obj.spriteDrawComp2);
            read(obj.spriteDrawComp3);
            read(obj.spriteDrawComp4);
            read(obj.spriteDrawComp5);
            read(obj.imageDrawComp1);
            read(obj.imageDrawComp2);
            read(obj.imageDrawComp3);
            read(obj.imageDrawComp4);
            read(obj.tweener1);
            read(obj.tweener2);
            read(obj.tweener3);
            read(obj.hitboxStanding);
            read(obj.hitboxSliding);
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

        private unsafe void write(PlayerBot obj)
        {
            write((Player)obj);
            chunk.Write(obj, 0x1D4 + 0x214 + 0x4, 0x48);
            chunk.WriteInt(obj.goldenHookBot != null ? obj.goldenHookBot.actor.Id : -1);
        }

        private unsafe void read(PlayerBot obj)
        {
            read((Player)obj);
            chunk.Read(obj, 0x1D4 + 0x214 + 0x4, 0x48);
            int hookId = chunk.ReadInt();
            applyPtr.Add(() => obj.goldenHookBot = (GoldenHook)contrLookup[hookId]);
            obj.timespanBot = new TimeSpan(obj.timespanBot.Ticks + dt);
        }

        private unsafe void write(ShakeCameraModifier obj)
        {
            chunk.Write(obj, 0x14, 0x34);
            write(obj.random);
        }

        private unsafe void read(ShakeCameraModifier obj)
        {
            chunk.Read(obj, 0x14, 0x34);
            read(obj.random);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            obj.timespan2 = new TimeSpan(obj.timespan2.Ticks + dt);
        }

        private unsafe void write(ClampCameraModifier obj)
        {
            chunk.Write(obj, 0x8, 0x30);
        }

        private unsafe void read(ClampCameraModifier obj)
        {
            chunk.Read(obj, 0x8, 0x30);
        }

        private unsafe void write(Camera obj)
        {
            chunk.Write(obj, 0x10, 0x3C);
        }

        private unsafe void read(Camera obj)
        {
            chunk.Read(obj, 0x10, 0x3C);
        }

        private unsafe void write(CameraMP obj)
        {
            chunk.Write(obj, 0x14, 0x34);
        }

        private unsafe void read(CameraMP obj)
        {
            chunk.Read(obj, 0x14, 0x34);
        }

        private unsafe void write(CCamera obj)
        {
            chunk.Write(obj, 0x1C, 0xA8);
            write(obj.shakeMod);
            write(obj.clampMod);
            if (obj.mods[0] is Camera camera)
                write(camera);
            else if (obj.mods[0] is CameraMP cameraMP)
                write(cameraMP);
        }

        private unsafe void read(CCamera obj)
        {
            chunk.Read(obj, 0x1C, 0xA8);
            read(obj.shakeMod);
            read(obj.clampMod);
            if (obj.mods[0] is Camera camera)
                read(camera);
            else if (obj.mods[0] is CameraMP cameraMP)
                read(cameraMP);
        }

        private unsafe void write(ModuleSolo obj)
        {
            chunk.Write(obj, 0x74, 0x4C);
            write(obj.random);
            write(obj.camera);
        }

        private unsafe void read(ModuleSolo obj)
        {
            chunk.Read(obj, 0x74, 0x4C);
            read(obj.random);
            read(obj.camera);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
        }

        private unsafe void write(ModuleMP obj)
        {
            chunk.Write(obj, 0xA8, 0xB8);
            write(obj.random);
            write(obj.camera);
        }

        private unsafe void read(ModuleMP obj)
        {
            chunk.Read(obj, 0xA8, 0xB8);
            read(obj.random);
            read(obj.camera);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            obj.timespan4 = new TimeSpan(obj.timespan4.Ticks + dt);
        }

        bool empty = true;
        CCollisionEngine collisionEngine;
        CWorld world;
        Stack stack;
        Chunk chunk;
        long time;
        long dt;
        bool storeAiVolumes;
        NullSafeDict<int, ICActorController> contrLookup = new NullSafeDict<int, ICActorController>();
        List<Action> applyPtr = new List<Action>();
        HashSet<CActor> fixedIdActors = new HashSet<CActor>();
        HashSet<int> fixedIds = new HashSet<int>();

        public Savestate()
        {
            chunk = new Chunk();
        }

        public void save(bool storeAiVolumes)
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
                        write(playerBot);
                        continue;
                    }
                    
                    chunk.WriteInt(0);
                    chunk.WriteInt(player.slot.Index);
                    write(player);
                }
                else if (controller is Grapple grapple)
                {
                    chunk.WriteInt(1);
                    write(grapple);
                }
                else if (controller is Rope rope)
                {
                    chunk.WriteInt(2);
                    write(rope);
                }
                else if (controller is Fireball fireball)
                {
                    chunk.WriteInt(3);
                    write(fireball);
                }
                else if (controller is DroppedObstacle droppedObstacle)
                {
                    chunk.WriteInt(4);
                    write(droppedObstacle);
                }
                else if (controller is Rocket rocket)
                {
                    chunk.WriteInt(5);
                    write(rocket);
                }
                else if (controller is GoldenHook goldenHook)
                {
                    chunk.WriteInt(6);
                    write(goldenHook);
                }
                else if (controller is Shockwave shockwave)
                {
                    chunk.WriteInt(7);
                    write(shockwave);
                }
                else if (controller is DroppedBomb droppedBomb)
                {
                    chunk.WriteInt(8);
                    write(droppedBomb);
                }
                else if (controller is Obstacle obstacle)
                {
                    chunk.WriteInt(9);
                    write(obstacle);
                }
                else if (controller is FreezeRay freezeRay)
                {
                    chunk.WriteInt(10);
                    write(freezeRay);
                }
                else if (controller is Pickup pickup)
                {
                    chunk.WriteInt(11);
                    write(pickup);
                }
                else if (controller is Trigger trigger)
                {
                    chunk.WriteInt(12);
                    write(trigger);
                }
                else if (controller is SwitchBlock switchBlock)
                {
                    chunk.WriteInt(13);
                    write(switchBlock);
                }
                else if (controller is FallTile fallTile)
                {
                    chunk.WriteInt(14);
                    write(fallTile);
                }
                else if (controller is TriggerSaw triggerSaw)
                {
                    chunk.WriteInt(15);
                    write(triggerSaw);
                }
                else if (controller is RocketLauncher rocketLauncher)
                {
                    chunk.WriteInt(16);
                    write(rocketLauncher);
                }
                else if (controller is BoostaCoke boostaCoke)
                {
                    chunk.WriteInt(17);
                    write(boostaCoke);
                }
                else if (controller is Laser laser)
                {
                    chunk.WriteInt(18);
                    write(laser);
                }
                else if (controller is AIVolume aiVolume)
                {
                    if (!storeAiVolumes)
                        continue;
                    chunk.WriteInt(19);
                    write(aiVolume);
                }
                else if (controller is Timer timer)
                {
                    chunk.WriteInt(20);
                    write(timer);
                }
                else if (controller is Checkpoint checkpoint)
                {
                    chunk.WriteInt(21);
                    write(checkpoint);
                }
                else if (controller is StraightRocket straightRocket)
                {
                    chunk.WriteInt(22);
                    write(straightRocket);
                }
            }

            chunk.WriteInt(-2);

            foreach (var module in stack.modules)
            {
                if (module is ModuleSolo)
                {
                    write((ModuleSolo)module);
                    break;
                }
                else if (module is ModuleMP)
                {
                    write((ModuleMP)module);
                    break;
                }
            }

            time = CEngine.CEngine.Instance.GameTime.TotalGameTime.Ticks;
        }

        private T getOfType<T>(int n, Func<T, bool> func = null) where T : ICActorController
        {
            int c = 0;

            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                ICActorController controller = actor.controller;
                if (controller is T && !actor.ghostOwnedItem && (func == null || func((T)controller)))
                {
                    if (c == n)
                        return (T)controller;
                    else
                        c++;
                }
            }

            return default;
        }

        public void destroyAllAfter<T>(int n)
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

        public bool load()
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
                    Player player = getOfType<Player>(0, (check) => check.slot.Index == index);
                    read((PlayerBot)player);
                    fixedIdActors.Add(player.actor);
                }
                else if (type == 0)
                {
                    int index = chunk.ReadInt();
                    Player player = getOfType<Player>(0, (check) => check.slot.Index == index);
                    read(player);
                    fixedIdActors.Add(player.actor);
                }
                else if (type == 1)
                {
                    Grapple grapple = getOfType<Grapple>(counts[1]++);
                    if (grapple == null)
                        grapple = (Grapple)world.SpawnActor(new GrappleDef(0, null));
                    read(grapple);
                    fixedIdActors.Add(grapple.actor);
                }
                else if (type == 2)
                {
                    Rope rope = getOfType<Rope>(counts[2]++);
                    if (rope == null)
                        rope = (Rope)world.SpawnActor(new RopeDef());
                    read(rope);
                    fixedIdActors.Add(rope.actor);
                }
                else if (type == 3)
                {
                    Fireball fireball = getOfType<Fireball>(counts[3]++);
                    if (fireball == null)
                        fireball = (Fireball)world.SpawnActor(new FireballDef(Vector2.Zero));
                    read(fireball);
                    fixedIdActors.Add(fireball.actor);
                }
                else if (type == 4)
                {
                    DroppedObstacle droppedObstacle = getOfType<DroppedObstacle>(counts[4]++);
                    if (droppedObstacle == null)
                        droppedObstacle = (DroppedObstacle)world.SpawnActor(new DroppedObstacleDef(null, false));
                    read(droppedObstacle);
                    fixedIdActors.Add(droppedObstacle.actor);
                }
                else if (type == 5)
                {
                    Rocket rocket = getOfType<Rocket>(counts[5]++);
                    if (rocket == null)
                        rocket = (Rocket)world.SpawnActor(new RocketDef(null, stack.gameInfo));
                    read(rocket);
                    fixedIdActors.Add(rocket.actor);
                }
                else if (type == 6)
                {
                    GoldenHook goldenHook = getOfType<GoldenHook>(counts[6]++);
                    if (goldenHook == null)
                        goldenHook = (GoldenHook)world.SpawnActor(new GoldenHookDef(0, null));
                    read(goldenHook);
                    fixedIdActors.Add(goldenHook.actor);
                }
                else if (type == 7)
                {
                    Shockwave shockwave = getOfType<Shockwave>(counts[7]++);
                    if (shockwave == null)
                        shockwave = (Shockwave)world.SpawnActor(new ShockwaveDef(null));
                    read(shockwave);
                    fixedIdActors.Add(shockwave.actor);
                }
                else if (type == 8)
                {
                    DroppedBomb droppedBomb = getOfType<DroppedBomb>(counts[8]++);
                    if (droppedBomb == null)
                        droppedBomb = (DroppedBomb)world.SpawnActor(new DroppedBombDef(Color.Black, null));
                    read(droppedBomb);
                    fixedIdActors.Add(droppedBomb.actor);
                }
                else if (type == 9)
                {
                    Obstacle obstacle = getOfType<Obstacle>(counts[9]++);
                    if (obstacle != null)
                        read(obstacle);
                    fixedIdActors.Add(obstacle.actor);
                }
                else if (type == 10)
                {
                    FreezeRay freezeRay = getOfType<FreezeRay>(counts[10]++);
                    if (freezeRay == null)
                        freezeRay = (FreezeRay)world.SpawnActor(new FreezeRayDef(Vector2.Zero, null));
                    read(freezeRay);
                    fixedIdActors.Add(freezeRay.actor);
                }
                else if (type == 11)
                {
                    Pickup pickup = getOfType<Pickup>(counts[11]++);
                    if (pickup != null)
                        read(pickup);
                    fixedIdActors.Add(pickup.actor);
                }
                else if (type == 12)
                {
                    Trigger trigger = getOfType<Trigger>(counts[12]++);
                    if (trigger != null)
                        read(trigger);
                    fixedIdActors.Add(trigger.actor);
                }
                else if (type == 13)
                {
                    SwitchBlock switchBlock = getOfType<SwitchBlock>(counts[13]++);
                    if (switchBlock != null)
                        read(switchBlock);
                    fixedIdActors.Add(switchBlock.actor);
                }
                else if (type == 14)
                {
                    FallTile fallTile = getOfType<FallTile>(counts[14]++);
                    if (fallTile != null)
                        read(fallTile);
                    fixedIdActors.Add(fallTile.actor);
                }
                else if (type == 15)
                {
                    TriggerSaw triggerSaw = getOfType<TriggerSaw>(counts[15]++);
                    if (triggerSaw != null)
                        read(triggerSaw);
                    fixedIdActors.Add(triggerSaw.actor);
                }
                else if (type == 16)
                {
                    RocketLauncher rocketLauncher = getOfType<RocketLauncher>(counts[16]++);
                    if (rocketLauncher != null)
                        read(rocketLauncher);
                    fixedIdActors.Add(rocketLauncher.actor);
                }
                else if (type == 17)
                {
                    BoostaCoke boostaCoke = getOfType<BoostaCoke>(counts[17]++);
                    if (boostaCoke != null)
                        read(boostaCoke);
                    fixedIdActors.Add(boostaCoke.actor);
                }
                else if (type == 18)
                {
                    Laser laser = getOfType<Laser>(counts[18]++);
                    if (laser != null)
                        read(laser);
                    fixedIdActors.Add(laser.actor);
                }
                else if (type == 19)
                {
                    AIVolume aiVolume = getOfType<AIVolume>(counts[19]++);
                    if (aiVolume == null)
                        aiVolume = (AIVolume)world.SpawnActor(new AIVolumeDef(Vector2.Zero, Vector2.Zero, 0));
                    read(aiVolume);
                    fixedIdActors.Add(aiVolume.actor);
                }
                else if (type == 20)
                {
                    Timer timer = getOfType<Timer>(counts[20]++);
                    if (timer != null)
                        read(timer);
                    fixedIdActors.Add(timer.actor);
                }
                else if (type == 21)
                {
                    Checkpoint checkpoint = getOfType<Checkpoint>(counts[21]++);
                    if (checkpoint != null)
                        read(checkpoint);
                    fixedIdActors.Add(checkpoint.actor);
                }
                else if (type == 22)
                {
                    StraightRocket straightRocket = getOfType<StraightRocket>(counts[22]++);
                    if (straightRocket == null)
                        straightRocket = (StraightRocket)world.SpawnActor(new StraightRocketDef(stack.gameInfo));
                    read(straightRocket);
                    fixedIdActors.Add(straightRocket.actor);
                }
            }

            foreach (var actor in fixedIdActors)
                fixedIds.Add(actor.Id);

            destroyAllAfter<Grapple>(counts[1]);
            destroyAllAfter<Rope>(counts[2]);
            destroyAllAfter<Fireball>(counts[3]);
            destroyAllAfter<DroppedObstacle>(counts[4]);
            destroyAllAfter<Rocket>(counts[5]);
            destroyAllAfter<GoldenHook>(counts[6]);
            destroyAllAfter<Shockwave>(counts[7]);
            destroyAllAfter<DroppedBomb>(counts[8]);
            destroyAllAfter<FreezeRay>(counts[10]);
            if (storeAiVolumes)
                destroyAllAfter<AIVolume>(counts[19]);
            destroyAllAfter<StraightRocket>(counts[22]);

            foreach (var module in stack.modules)
            {
                if (module is ModuleSolo)
                {
                    read((ModuleSolo)module);
                    break;
                }
                else if (module is ModuleMP)
                {
                    read((ModuleMP)module);
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

        private Savestate savestate;
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
                savestate.save(StoreAIVolumes.Value);

            if (Keyboard.Pressed[LoadKey.Value])
            {
                if (savestate.load())
                    savestateLoadTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }
        }
    }
}
