using CEngine.Graphics.Camera;
using CEngine.Graphics.Camera.Modifier;
using CEngine.Graphics.Component;
using CEngine.Util.Draw;
using CEngine.World;
using CEngine.World.Actor;
using CEngine.World.Collision;
using CEngine.World.Collision.Shape;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

    public class Savestate
    {
        private readonly int VERSION = 4;
        public static ushort LoadedVersion = Version.VERSION;

        private static unsafe void Write(MemoryStream stream, CAABB obj)
        {
            stream.Write(obj, 0x4, 0x18);
        }

        private static unsafe void Read(MemoryStream stream, CAABB obj)
        {
            stream.Read(obj, 0x4, 0x18);
        }

        private static unsafe void Write(MemoryStream stream, CConvexPolygon obj)
        {
            stream.Write(obj, 0xC, 0x1C);
            stream.WriteArr(obj.localVertices);
            stream.WriteArr(obj.vertices);
        }

        private static unsafe void Read(MemoryStream stream, CConvexPolygon obj)
        {
            stream.Read(obj, 0xC, 0x1C);
            obj.localVertices = stream.ReadArr<Vector2>();
            obj.vertices = stream.ReadArr<Vector2>();
        }

        private static unsafe void Write(MemoryStream stream, CActor obj)
        {
            stream.Write(obj, 0x20, 0x40);
            Write(stream, obj.bounds);
        }

        private static unsafe void Read(MemoryStream stream, CActor obj)
        {
            stream.Read(obj, 0x20, 0x40);
            Read(stream, obj.bounds);
        }

        private static unsafe void Write(MemoryStream stream, CSpriteDrawComponent obj)
        {
            stream.Write(obj, 0x20, 0x6C);
            Write(stream, obj.bounds);
        }

        private static unsafe void Read(MemoryStream stream, CSpriteDrawComponent obj)
        {
            stream.Read(obj, 0x20, 0x6C);
            Read(stream, obj.bounds);
        }

        private static unsafe void Write(MemoryStream stream, CAnimatedSpriteDrawComponent obj)
        {
            stream.Write(obj, 0x2C, 0x84);
            Write(stream, obj.bounds);
            stream.WriteStr(obj.nextAnimation);
            stream.WriteStr(obj.animation.id);
        }

        private static unsafe void Read(MemoryStream stream, CAnimatedSpriteDrawComponent obj, bool player = false)
        {
            Color color = obj.Color;
            float opacity = obj.Opacity;
            Vector2 offset = obj.Offset;
            Vector2 scale = obj.Scale;
            Vector2 size = obj.Size;
            Vector2 origin = obj.Origin;
            stream.Read(obj, 0x2C, 0x84);
            if (player)
            {
                obj.Color = color;
                obj.Opacity = opacity;
                obj.Offset = offset;
                obj.Scale = scale;
                obj.Size = size;
                obj.Origin = origin;
            }
            Read(stream, obj.bounds);
            obj.nextAnimation = stream.ReadStr();
            obj.timeSpan1 = new TimeSpan(obj.timeSpan1.Ticks + dt);
            string animationId = stream.ReadStr();
            obj.animation = obj.animImage.GetAnimation(animationId);
        }

        private static unsafe void Write(MemoryStream stream, CImageDrawComponent obj)
        {
            stream.Write(obj, 0x1C, 0x58);
            Write(stream, obj.bounds);
        }

        private static unsafe void Read(MemoryStream stream, CImageDrawComponent obj)
        {
            stream.Read(obj, 0x1C, 0x58);
            Read(stream, obj.bounds);
        }

        private static unsafe void Write(MemoryStream stream, CGroupDrawComponent obj)
        {
            stream.Write(obj, 0x18, 0x58);
        }

        private static unsafe void Read(MemoryStream stream, CGroupDrawComponent obj)
        {
            stream.Read(obj, 0x18, 0x58);
        }

        private static unsafe void Write(MemoryStream stream, CLine obj)
        {
            stream.Write(obj, 0x4, 0x18);
        }

        private static unsafe void Read(MemoryStream stream, CLine obj)
        {
            stream.Read(obj, 0x4, 0x18);
        }

        private static unsafe void Write(MemoryStream stream, CLineDrawComponent obj)
        {
            stream.Write(obj, 0x18, 0x4);
            stream.Write(obj.lines.Count);
            foreach (CLine line in obj.lines)
                Write(stream, line);
        }

        private static unsafe void Read(MemoryStream stream, CLineDrawComponent obj)
        {
            stream.Read(obj, 0x18, 0x4);
            int count = stream.Read<int>();
            obj.lines = new List<CLine>(count);
            for (int i = 0; i < count; i++)
            {
                CLine line = new CLine(Vector2.Zero, Vector2.Zero, Color.Black);
                Read(stream, line);
                obj.lines.Add(line);
            }
        }

        private static unsafe void Write(MemoryStream stream, Tweener obj)
        {
            stream.Write(obj, 0x10, 0x1C);
        }

        private static unsafe void Read(MemoryStream stream, Tweener obj)
        {
            stream.Read(obj, 0x10, 0x1C);
        }

        private static unsafe void Write(MemoryStream stream, Slot obj)
        {
            stream.Write(obj, 0x68, 0x3C);
        }

        private static unsafe void Read(MemoryStream stream, Slot obj)
        {
            byte charId = obj.charId;
            byte skinId = obj.skinId;
            stream.Read(obj, 0x68, 0x3C);
            obj.charId = charId;
            obj.skinId = skinId;
        }

        private static unsafe void Write(MemoryStream stream, Random obj)
        {
            stream.Write(obj, 0x8, 0x8);
            stream.Write(obj, 0x1C, 0xE0);
        }

        private static unsafe void Read(MemoryStream stream, Random obj)
        {
            stream.Read(obj, 0x8, 0x8);
            stream.Read(obj, 0x1C, 0xE0);
        }

        private static unsafe void Write(MemoryStream stream, Grapple obj)
        {
            stream.Write(obj, 0x24, 0x14);
            Write(stream, obj.actor);
            Write(stream, obj.animSpriteDrawComp1);
            Write(stream, obj.spriteDrawComp1);
            Write(stream, obj.bounds);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Grapple obj)
        {
            stream.Read(obj, 0x24, 0x14);
            Read(stream, obj.actor);
            Read(stream, obj.animSpriteDrawComp1);
            Read(stream, obj.spriteDrawComp1);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = stream.Read<int>();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Rope obj)
        {
            stream.Write(obj, 0x20, 0x24);
            Write(stream, obj.actor);
            Write(stream, obj.line1);
            Write(stream, obj.line2);
            Write(stream, obj.line3);
            Write(stream, obj.lineDrawComp1);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
            if (obj.target == null)
                stream.Write(-1);
            else if (obj.target is Player player)
                stream.Write(player.actor.Id);
            else if (obj.target is GoldenHook goldenHook)
            {
                if (!include[ATGoldenHook.Id])
                    stream.Write(-2);
                else
                    stream.Write(goldenHook.actor.Id);
            }

            if (obj.target is Grapple grapple)
                stream.Write(grapple.actor.Id);
            if (obj.target is GoldenHook goldenHook1)
                stream.Write(goldenHook1.actor.Id);
        }

        private static unsafe void Read(MemoryStream stream, Rope obj)
        {
            stream.Read(obj, 0x20, 0x24);
            Read(stream, obj.actor);
            Read(stream, obj.line1);
            Read(stream, obj.line2);
            Read(stream, obj.line3);
            Read(stream, obj.lineDrawComp1);
            if (obj.lineDrawComp1.lines.Count > 0)
            {
                obj.lineDrawComp1.lines.Clear();
                obj.lineDrawComp1.AddLine(obj.line1);
                obj.lineDrawComp1.AddLine(obj.line2);
            }
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = stream.Read<int>();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            int targetId = stream.Read<int>();
            if (targetId != -2)
                applyPtr.Add(() => obj.target = contrLookup[targetId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, DroppedObstacle obj)
        {
            stream.Write(obj, 0x24, 0x24);
            Write(stream, obj.actor);
            Write(stream, obj.spriteDraw1);
            Write(stream, obj.bounds);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
            if (include[ATAIVolume.Id])
                stream.Write(obj.aiVolume != null ? obj.aiVolume.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, DroppedObstacle obj)
        {
            stream.Read(obj, 0x24, 0x24);
            Read(stream, obj.actor);
            Read(stream, obj.spriteDraw1);
            Read(stream, obj.bounds);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = stream.Read<int>();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            if (include[ATAIVolume.Id])
            {
                int aiVolumeId = stream.Read<int>();
                applyPtr.Add(() => obj.aiVolume = (AIVolume)contrLookup[aiVolumeId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Fireball obj)
        {
            stream.Write(obj, 0x3C, 0x3C);
            Write(stream, obj.actor);
            Write(stream, obj.animSpriteDraw);
            Write(stream, obj.bounds);
            if (obj.animSpriteDraw.Sprite == null)
                stream.Write(0);
            else if (obj.animSpriteDraw.Sprite == obj.animImage1)
                stream.Write(1);
            else
                stream.Write(2);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
            if (include[ATShockwave.Id])
                stream.Write(obj.shockwave != null ? obj.shockwave.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Fireball obj)
        {
            stream.Read(obj, 0x3C, 0x3C);
            Read(stream, obj.actor);
            Read(stream, obj.animSpriteDraw);
            Read(stream, obj.bounds);
            int sprite = stream.Read<int>();
            if (sprite == 0)
                obj.animSpriteDraw.Sprite = null;
            else if (sprite == 1)
                obj.animSpriteDraw.Sprite = obj.animImage1;
            else
                obj.animSpriteDraw.Sprite = obj.animImage2;
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = stream.Read<int>();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            if (include[ATShockwave.Id])
            {
                int shockwaveId = stream.Read<int>();
                applyPtr.Add(() => obj.shockwave = (Shockwave)contrLookup[shockwaveId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Rocket obj)
        {
            stream.Write(obj, 0x38, 0x38);
            Write(stream, obj.actor);
            Write(stream, obj.spriteDrawComp);
            Write(stream, obj.bounds);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
            stream.Write(obj.target != null ? obj.target.actor.Id : -1);
            stream.Write(obj.unknown != null ? obj.unknown.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Rocket obj)
        {
            stream.Read(obj, 0x38, 0x38);
            Read(stream, obj.actor);
            Read(stream, obj.spriteDrawComp);
            Read(stream, obj.bounds);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = stream.Read<int>();
            int targetId = stream.Read<int>();
            int unknownId = stream.Read<int>();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            applyPtr.Add(() => obj.unknown = (Player)contrLookup[unknownId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, GoldenHook obj)
        {
            stream.Write(obj, 0x30, 0x1C);
            Write(stream, obj.actor);
            Write(stream, obj.spriteDraw);
            Write(stream, obj.animSpriteDraw);
            Write(stream, obj.bounds);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
            stream.Write(obj.target != null ? obj.target.actor.Id : -1);
            stream.Write(obj.unknown != null ? obj.unknown.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, GoldenHook obj)
        {
            stream.Read(obj, 0x30, 0x1C);
            Read(stream, obj.actor);
            Read(stream, obj.spriteDraw);
            Read(stream, obj.animSpriteDraw);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = stream.Read<int>();
            int targetId = stream.Read<int>();
            int unknownId = stream.Read<int>();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            applyPtr.Add(() => obj.unknown = (Player)contrLookup[unknownId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Shockwave obj)
        {
            stream.Write(obj, 0x2C, 0x30);
            Write(stream, obj.actor);
            Write(stream, obj.animSpriteDraw1);
            Write(stream, obj.animSpriteDraw2);
            Write(stream, obj.animSpriteDraw3);
            Write(stream, obj.animSpriteDraw4);
            Write(stream, obj.animSpriteDraw5);
            Write(stream, obj.groupDraw);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Shockwave obj)
        {
            stream.Read(obj, 0x2C, 0x30);
            Read(stream, obj.actor);
            Read(stream, obj.animSpriteDraw1);
            Read(stream, obj.animSpriteDraw2);
            Read(stream, obj.animSpriteDraw3);
            Read(stream, obj.animSpriteDraw4);
            Read(stream, obj.animSpriteDraw5);
            Read(stream, obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = stream.Read<int>();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, DroppedBomb obj)
        {
            stream.Write(obj, 0x28, 0x18);
            Write(stream, obj.actor);
            Write(stream, obj.bounds);
            Write(stream, obj.animSpriteDraw1);
            Write(stream, obj.animSpriteDraw2);
            Write(stream, obj.groupDraw);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, DroppedBomb obj)
        {
            stream.Read(obj, 0x28, 0x18);
            Read(stream, obj.actor);
            Read(stream, obj.bounds);
            Read(stream, obj.animSpriteDraw1);
            Read(stream, obj.animSpriteDraw2);
            Read(stream, obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = stream.Read<int>();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void WriteEa(MemoryStream stream, EditableActor obj)
        {
            stream.Write(obj, 0x30, 0x24);
            Write(stream, obj.actor);
            Write(stream, obj.bounds);
        }

        private static unsafe void ReadEa(MemoryStream stream, EditableActor obj)
        {
            stream.Read(obj, 0x30, 0x24);
            Read(stream, obj.actor);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
        }

        private static unsafe void Write(MemoryStream stream, Obstacle obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x14, 0x20);
            Write(stream, obj.spriteDraw);
            Write(stream, obj.groupDraw);
            if (include[ATAIVolume.Id])
            {
                stream.Write(obj.aiVolume1 != null ? obj.aiVolume1.actor.Id : -1);
                stream.Write(obj.aiVolume2 != null ? obj.aiVolume2.actor.Id : -1);
            }
        }

        private static unsafe void Read(MemoryStream stream, Obstacle obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x14, 0x20);
            Read(stream, obj.spriteDraw);
            Read(stream, obj.groupDraw);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            if (include[ATAIVolume.Id])
            {
                int aiVolume1Id = stream.Read<int>();
                int aiVolume2Id = stream.Read<int>();
                applyPtr.Add(() => obj.aiVolume1 = (AIVolume)contrLookup[aiVolume1Id]);
                applyPtr.Add(() => obj.aiVolume2 = (AIVolume)contrLookup[aiVolume2Id]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, FreezeRay obj)
        {
            stream.Write(obj, 0x18, 0x18);
            Write(stream, obj.actor);
            stream.Write(obj.animSpriteDraws.Length);
            foreach (var animSpriteDraw in obj.animSpriteDraws)
                Write(stream, animSpriteDraw);
            Write(stream, obj.groupDraw);
            Write(stream, obj.bounds);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, FreezeRay obj)
        {
            stream.Read(obj, 0x18, 0x18);
            Read(stream, obj.actor);
            int count = stream.Read<int>();
            for (int i = 0; i < count; i++)
            {
                if (i >= obj.animSpriteDraws.Length)
                {
                    CAnimatedSpriteDrawComponent dummy = new CAnimatedSpriteDrawComponent();
                    Read(stream, dummy);
                    continue;
                }

                Read(stream, obj.animSpriteDraws[i]);
            }
            Read(stream, obj.groupDraw);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            int ownerId = stream.Read<int>();
            applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Pickup obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x10, 0xC);
            Write(stream, obj.animSpriteDraw1);
            Write(stream, obj.animSpriteDraw2);
            stream.Write(obj.imageDraw3 != null ? 1 : 0);
            if (obj.imageDraw3 != null)
            {
                Write(stream, obj.imageDraw3);
                Write(stream, obj.imageDraw4);
            }
            Write(stream, obj.groupDraw);
        }

        private static unsafe void Read(MemoryStream stream, Pickup obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x10, 0xC);
            Read(stream, obj.animSpriteDraw1);
            Read(stream, obj.animSpriteDraw2);
            int notNull = stream.Read<int>();
            if (notNull != 0)
            {
                Read(stream, obj.imageDraw3);
                Read(stream, obj.imageDraw4);
            }
            Read(stream, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void WriteRea(MemoryStream stream, ResizableEditableActor obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x0, 0x14);
        }

        private static unsafe void ReadRea(MemoryStream stream, ResizableEditableActor obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x0, 0x14);
        }

        private static unsafe void Write(MemoryStream stream, Trigger obj)
        {
            WriteRea(stream, obj);
            stream.Write(obj, 0x68 + 0x1C, 0x8);
            stream.Write(obj.list1.Count);
            foreach (ICActorController contr in obj.list1)
            {
                if (contr == null)
                {
                    stream.Write(-1);
                    continue;
                }

                if (contr is Player player)
                {
                    stream.Write(player.actor.Id);
                }
                else if (contr is DroppedBomb dropped)
                {
                    if (include[ATDroppedBomb.Id])
                        stream.Write(dropped.actor.Id);
                    else
                        stream.Write(-2);
                }
                else if (contr is Fireball fireball)
                {
                    if (include[ATFireball.Id])
                        stream.Write(fireball.actor.Id);
                    else
                        stream.Write(-2);
                }
                else if (contr is Rocket rocket)
                {
                    if (include[ATRocket.Id])
                        stream.Write(rocket.actor.Id);
                    else
                        stream.Write(-2);
                }
            }
            stream.Write(obj.list2.Count);
            foreach (ICActorController contr in obj.list2)
            {
                if (contr == null)
                {
                    stream.Write(-1);
                    continue;
                }

                if (contr is Player player)
                {
                    stream.Write(player.actor.Id);
                }
                else if (contr is DroppedBomb droppedBomb)
                {
                    if (include[ATDroppedBomb.Id])
                        stream.Write(droppedBomb.actor.Id);
                    else
                        stream.Write(-2);
                }
                else if (contr is Fireball fireball)
                {
                    if (include[ATFireball.Id])
                        stream.Write(fireball.actor.Id);
                    else
                        stream.Write(-2);
                }
                else if (contr is Rocket rocket)
                {
                    if (include[ATRocket.Id])
                        stream.Write(rocket.actor.Id);
                    else
                        stream.Write(-2);
                }
            }
        }

        private static unsafe void Read(MemoryStream stream, Trigger obj)
        {
            ReadRea(stream, obj);
            stream.Read(obj, 0x68 + 0x1C, 0x8);
            int count1 = stream.Read<int>();
            while (obj.list1.Count < count1) obj.list1.Add(null);
            while (obj.list1.Count > count1) obj.list1.RemoveAt(obj.list1.Count - 1);
            for (int i = 0; i < count1; i++)
            {
                int contrId = stream.Read<int>();
                if (contrId == -2)
                    continue;
                int j = i;
                applyPtr.Add(() => obj.list1[j] = contrLookup[contrId]);
            }
            int count2 = stream.Read<int>();
            while (obj.list2.Count < count2) obj.list2.Add(null);
            while (obj.list2.Count > count2) obj.list2.RemoveAt(obj.list2.Count - 1);
            for (int i = 0; i < count2; i++)
            {
                int contrId = stream.Read<int>();
                if (contrId == -2)
                    continue;
                int j = i;
                applyPtr.Add(() => obj.list2[j] = contrLookup[contrId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, SwitchBlock obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x24, 0x28);
            Write(stream, obj.animSpriteDraw1);
            Write(stream, obj.animSpriteDraw2);
            Write(stream, (CConvexPolygon)obj.colShape);
            Write(stream, obj.groupDraw);
        }

        private static unsafe void Read(MemoryStream stream, SwitchBlock obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x24, 0x28);
            Read(stream, obj.animSpriteDraw1);
            Read(stream, obj.animSpriteDraw2);
            Read(stream, (CConvexPolygon)obj.colShape);
            Read(stream, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Lever obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x14, 0x4);
            Write(stream, obj.animSpriteDraw1);
            Write(stream, obj.animSpriteDraw2);
        }

        private static unsafe void Read(MemoryStream stream, Lever obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x14, 0x4);
            Read(stream, obj.animSpriteDraw1);
            Read(stream, obj.animSpriteDraw2);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, FallTile obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x4, 0x18);
            Write(stream, obj.animSpriteDraw);
            Write(stream, obj.groupDraw);
        }

        private static unsafe void Read(MemoryStream stream, FallTile obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x4, 0x18);
            Read(stream, obj.animSpriteDraw);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            Read(stream, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, TriggerSaw obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x18, 0x28);
            Write(stream, obj.imageDraw);
            Write(stream, obj.convexPoly);
            Write(stream, obj.groupDraw);
        }

        private static unsafe void Read(MemoryStream stream, TriggerSaw obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x18, 0x28);
            Read(stream, obj.imageDraw);
            Read(stream, obj.convexPoly);
            Read(stream, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, RocketLauncher obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x1C, 0xC);
            Write(stream, obj.animSpriteDraw1);
            Write(stream, obj.animSpriteDraw2);
            Write(stream, obj.groupDraw);
            stream.Write(obj.target != null ? obj.target.actor.Id : -1);
            if (include[ATRocket.Id])
            {
                stream.Write(obj.rockets.Count);
                foreach (Rocket rocket in obj.rockets)
                    stream.Write(rocket != null ? rocket.actor.Id : -1);
            }
        }

        private static unsafe void Read(MemoryStream stream, RocketLauncher obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x1C, 0xC);
            Read(stream, obj.animSpriteDraw1);
            Read(stream, obj.animSpriteDraw2);
            Read(stream, obj.groupDraw);
            int targetId = stream.Read<int>();
            applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            if (include[ATRocket.Id])
            {
                int count = stream.Read<int>();
                while (obj.rockets.Count < count) obj.rockets.Add(null);
                while (obj.rockets.Count > count) obj.rockets.RemoveAt(obj.rockets.Count - 1);
                for (int i = 0; i < count; i++)
                {
                    int rocketId = stream.Read<int>();
                    int j = i;
                    applyPtr.Add(() => obj.rockets[j] = (Rocket)contrLookup[rocketId]);
                }
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, BoostaCoke obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x24, 0x44);
            Write(stream, obj.animSpriteDraw1);
            Write(stream, obj.animSpriteDraw2);
            Write(stream, obj.animSpriteDraws[0]);
            Write(stream, obj.animSpriteDraws[1]);
            Write(stream, obj.animSpriteDraws[2]);
            Write(stream, obj.animSpriteDraws[3]);
            Write(stream, obj.tweener);
            Write(stream, obj.random);
            Write(stream, obj.groupDraw);
            stream.Write(obj.player != null ? obj.player.actor.Id : -1);
            //stream.WriteArrFixed(obj.bools.Select(b => b ? (byte)1 : (byte)0).ToArray(), 4);
            stream.WriteByte(obj.bools[0] ? (byte)1 : (byte)0);
            stream.WriteByte(obj.bools[1] ? (byte)1 : (byte)0);
            stream.WriteByte(obj.bools[2] ? (byte)1 : (byte)0);
            stream.WriteByte(obj.bools[3] ? (byte)1 : (byte)0);
        }

        private static unsafe void Read(MemoryStream stream, BoostaCoke obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x24, 0x44);
            Read(stream, obj.animSpriteDraw1);
            Read(stream, obj.animSpriteDraw2);
            Read(stream, obj.animSpriteDraws[0]);
            Read(stream, obj.animSpriteDraws[1]);
            Read(stream, obj.animSpriteDraws[2]);
            Read(stream, obj.animSpriteDraws[3]);
            Read(stream, obj.tweener);
            Read(stream, obj.random);
            Read(stream, obj.groupDraw);
            int playerId = stream.Read<int>();
            applyPtr.Add(() => obj.player = (Player)contrLookup[playerId]);
            //obj.bools = stream.ReadArrFixed<byte>(4).Select(b => b != 0).ToArray();
            obj.bools[0] = stream.ReadByte() != 0;
            obj.bools[1] = stream.ReadByte() != 0;
            obj.bools[2] = stream.ReadByte() != 0;
            obj.bools[3] = stream.ReadByte() != 0;
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Laser obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x2C, 0x3C);
            Write(stream, obj.animSpriteDraw);
            Write(stream, obj.lineDraw);
            Write(stream, obj.line1);
            Write(stream, obj.line2);
            Write(stream, obj.groupDraw);
            if (include[ATAIVolume.Id])
                stream.Write(obj.aiVolume != null ? obj.aiVolume.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Laser obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x2C, 0x3C);
            Read(stream, obj.animSpriteDraw);
            Read(stream, obj.lineDraw);
            Read(stream, obj.line1);
            Read(stream, obj.line2);
            Read(stream, obj.groupDraw);
            if (include[ATAIVolume.Id])
            {
                int aiVolumeId = stream.Read<int>();
                applyPtr.Add(() => obj.aiVolume = (AIVolume)contrLookup[aiVolumeId]);
            }
            obj.lineDraw.lines.Clear();
            obj.lineDraw.lines.Add(obj.line2);
            obj.lineDraw.lines.Add(obj.line1);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, AIVolume obj)
        {
            WriteRea(stream, obj);
            stream.Write(obj, 0x68 + 0x24, 0x4);
            stream.Write(obj.type.value);
            stream.Write(obj.defaultActive.value);
            stream.Write(obj.easy.value);
            stream.Write(obj.medium.value);
            stream.Write(obj.hard.value);
            stream.Write(obj.unfair.value);
        }

        private static unsafe void Read(MemoryStream stream, AIVolume obj)
        {
            ReadRea(stream, obj);
            stream.Read(obj, 0x68 + 0x24, 0x4);
            obj.type.value = stream.Read<int>();
            obj.defaultActive.value = stream.Read<int>();
            obj.easy.value = stream.Read<int>();
            obj.medium.value = stream.Read<int>();
            obj.hard.value = stream.Read<int>();
            obj.unfair.value = stream.Read<int>();
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Timer obj)
        {
            WriteRea(stream, obj);
            stream.Write(obj, 0x68 + 0x10, 0x14);
            stream.Write(obj.unknown.value);
            stream.Write(obj.list1.Count);
            foreach (ICActorController contr in obj.list1)
                stream.Write(contr != null ? ((Player)contr).actor.Id : -1);
            stream.Write(obj.list2.Count);
            foreach (ICActorController contr in obj.list2)
                stream.Write(contr != null ? ((Player)contr).actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Timer obj)
        {
            ReadRea(stream, obj);
            stream.Read(obj, 0x68 + 0x10, 0x14);
            obj.unknown.value = stream.Read<int>();
            int count1 = stream.Read<int>();
            while (obj.list1.Count < count1) obj.list1.Add(null);
            while (obj.list1.Count > count1) obj.list1.RemoveAt(obj.list1.Count - 1);
            for (int i = 0; i < count1; i++)
            {
                int contrId = stream.Read<int>();
                int j = i;
                applyPtr.Add(() => obj.list1[j] = contrLookup[contrId]);
            }
            int count2 = stream.Read<int>();
            while (obj.list2.Count < count2) obj.list2.Add(null);
            while (obj.list2.Count > count2) obj.list2.RemoveAt(obj.list2.Count - 1);
            for (int i = 0; i < count2; i++)
            {
                int contrId = stream.Read<int>();
                int j = i;
                applyPtr.Add(() => obj.list2[j] = contrLookup[contrId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Checkpoint obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x3C, 0x28);
            stream.Write(obj.helpers.Count);
            foreach (Checkpoint helper in obj.helpers)
                stream.Write(helper != null ? helper.actor.Id : -1);
            stream.Write(obj.checkpoint1 != null ? obj.checkpoint1.actor.Id : -1);
            stream.Write(obj.checkpoint2 != null ? obj.checkpoint2.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Checkpoint obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x3C, 0x28);
            int helpers = stream.Read<int>();
            while (obj.helpers.Count < helpers) obj.helpers.Add(null);
            while (obj.helpers.Count > helpers) obj.helpers.RemoveAt(obj.helpers.Count - 1);
            for (int i = 0; i < helpers; i++)
            {
                int helperId = stream.Read<int>();
                int j = i;
                applyPtr.Add(() => obj.helpers[j] = (Checkpoint)contrLookup[helperId]);
            }
            int checkpoint1Id = stream.Read<int>();
            int checkpoint2Id = stream.Read<int>();
            applyPtr.Add(() => obj.checkpoint1 = (Checkpoint)contrLookup[checkpoint1Id]);
            applyPtr.Add(() => obj.checkpoint2 = (Checkpoint)contrLookup[checkpoint2Id]);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, StraightRocket obj)
        {
            stream.Write(obj, 0x28, 0x18);
            Write(stream, obj.actor);
            Write(stream, obj.spriteDraw);
            Write(stream, obj.bounds);
        }

        private static unsafe void Read(MemoryStream stream, StraightRocket obj)
        {
            stream.Read(obj, 0x28, 0x18);
            Read(stream, obj.actor);
            Read(stream, obj.spriteDraw);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            obj.actor.UpdateCollision();
        }

        private static unsafe void WriteBb(MemoryStream stream, BossBase obj)
        {
            stream.Write(obj, 0x18, 0xC);
            Write(stream, obj.actor);
            Write(stream, obj.bounds);
        }

        private static unsafe void ReadBb(MemoryStream stream, BossBase obj)
        {
            stream.Read(obj, 0x18, 0xC);
            Read(stream, obj.actor);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
        }

        private static unsafe void Write(MemoryStream stream, Boss1 obj)
        {
            WriteBb(stream, obj);
            stream.Write(obj, 0x24 + 0x18, 0x10);
            Write(stream, obj.drawComp);
            foreach (CConvexPolygon polygon in obj.polygons)
                Write(stream, polygon);
        }

        private static unsafe void Read(MemoryStream stream, Boss1 obj)
        {
            ReadBb(stream, obj);
            stream.Read(obj, 0x24 + 0x18, 0x10);
            Read(stream, obj.drawComp);
            foreach (CConvexPolygon polygon in obj.polygons)
                Read(stream, polygon);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Boss2 obj)
        {
            WriteBb(stream, obj);
            stream.Write(obj, 0x24 + 0x14, 0x18);
            Write(stream, obj.groupDraw);
            Write(stream, obj.animSpriteDraw);
        }

        private static unsafe void Read(MemoryStream stream, Boss2 obj)
        {
            ReadBb(stream, obj);
            stream.Read(obj, 0x24 + 0x14, 0x18);
            Read(stream, obj.groupDraw);
            Read(stream, obj.animSpriteDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Boss4 obj)
        {
            WriteBb(stream, obj);
            stream.Write(obj, 0x24 + 0x18, 0x10);
            Write(stream, obj.groupDraw);
            foreach (CConvexPolygon polygon in obj.polygons)
                Write(stream, polygon);
        }

        private static unsafe void Read(MemoryStream stream, Boss4 obj)
        {
            ReadBb(stream, obj);
            stream.Read(obj, 0x24 + 0x18, 0x10);
            Read(stream, obj.groupDraw);
            foreach (CConvexPolygon polygon in obj.polygons)
                Read(stream, polygon);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, BossSaw obj)
        {
            stream.Write(obj, 0x20, 0x34);
            Write(stream, obj.actor);
            Write(stream, obj.bounds);
            Write(stream, obj.drawComp);
        }

        private static unsafe void Read(MemoryStream stream, BossSaw obj)
        {
            stream.Read(obj, 0x20, 0x34);
            Read(stream, obj.actor);
            Read(stream, obj.bounds);
            Read(stream, obj.drawComp);
            contrLookup.Add(obj.actor.Id, obj);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, FallBlock obj)
        {
            stream.Write(obj, 0x14, 0x28);
            Write(stream, obj.actor);
            Write(stream, obj.polygon);
            Write(stream, obj.imageDrawComp);
        }

        private static unsafe void Read(MemoryStream stream, FallBlock obj)
        {
            stream.Read(obj, 0x14, 0x28);
            Read(stream, obj.actor);
            Read(stream, obj.polygon);
            Read(stream, obj.imageDrawComp);
            obj.timespan = new TimeSpan(obj.timespan.Ticks + dt);
            contrLookup.Add(obj.actor.Id, obj);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, MovingPlatform obj)
        {
            stream.Write(obj, 0x24, 0x38);
            Write(stream, obj.actor);
            Write(stream, obj.bounds);
            Write(stream, obj.imageDrawComp);
            stream.Write(obj.player != null ? obj.player.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, MovingPlatform obj)
        {
            stream.Read(obj, 0x24, 0x38);
            Read(stream, obj.actor);
            Read(stream, obj.bounds);
            Read(stream, obj.imageDrawComp);
            int playerId = stream.Read<int>();
            applyPtr.Add(() => obj.player = (Player)contrLookup[playerId]);
            contrLookup.Add(obj.actor.Id, obj);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Player obj)
        {
            stream.Write(obj, 0x1D4, 0x214);
            Write(stream, obj.actor);
            Write(stream, obj.slot);
            Write(stream, obj.random);
            Write(stream, obj.groupDrawComp1);
            Write(stream, obj.animSpriteDrawComp1);
            Write(stream, obj.animSpriteDrawComp2);
            Write(stream, obj.animSpriteDrawComp3);
            Write(stream, obj.animSpriteDrawComp4);
            Write(stream, obj.animSpriteDrawComp5);
            Write(stream, obj.animSpriteDrawComp6);
            Write(stream, obj.spriteDrawComp1);
            Write(stream, obj.spriteDrawComp2);
            Write(stream, obj.spriteDrawComp3);
            Write(stream, obj.spriteDrawComp4);
            Write(stream, obj.spriteDrawComp5);
            Write(stream, obj.imageDrawComp1);
            Write(stream, obj.imageDrawComp2);
            Write(stream, obj.imageDrawComp3);
            Write(stream, obj.imageDrawComp4);
            Write(stream, obj.tweener1);
            Write(stream, obj.tweener2);
            Write(stream, obj.tweener3);
            Write(stream, obj.hitboxStanding);
            Write(stream, obj.hitboxSliding);
            if (
                obj.afterImagesParticleEmitterProvider is AfterImagesParticleEmitterProvider afterImagesParticleEmitterProvider &&
                afterImagesParticleEmitterProvider.afterImages != null)
            {
                stream.Write(1);
                stream.Write(afterImagesParticleEmitterProvider.afterImages.timespan1.Ticks);
            }
            else
            {
                stream.Write(-1);
            }

            stream.Write(obj.grapple != null ? obj.grapple.actor.Id : -1);
            stream.Write(obj.rope != null ? obj.rope.actor.Id : -1);
            if (include[ATGoldenHook.Id])
                stream.Write(obj.goldenHook != null ? obj.goldenHook.actor.Id : -1);
            if (include[ATShockwave.Id])
                stream.Write(obj.shockwave != null ? obj.shockwave.actor.Id : -1);
            if (include[ATDroppedBomb.Id])
                stream.Write(obj.droppedBomb != null ? obj.droppedBomb.actor.Id : -1);
            if (include[ATFreezeRay.Id])
                stream.Write(obj.freezeRay != null ? obj.freezeRay.actor.Id : -1);
            stream.Write(obj.hooked != null ? obj.hooked.actor.Id : -1);
            stream.Write(obj.unknown1 != null ? obj.unknown1.actor.Id : -1);
            if (include[ATTrigger.Id])
            {
                stream.Write(obj.trigger1 != null ? obj.trigger1.actor.Id : -1);
                stream.Write(obj.trigger2 != null ? obj.trigger2.actor.Id : -1);
            }
            if (include[ATCheckpoint.Id])
            {
                stream.Write(obj.checkpoint1 != null ? obj.checkpoint1.actor.Id : -1);
                stream.Write(obj.checkpoint2 != null ? obj.checkpoint2.actor.Id : -1);
            }
            if (include[ATDroppedObstacle.Id])
            {
                stream.Write(obj.droppedObstacles.Count);
                foreach (DroppedObstacle droppedObstacle in obj.droppedObstacles)
                    stream.Write(droppedObstacle != null ? droppedObstacle.actor.Id : -1);
            }
            if (include[ATFireball.Id])
            {
                stream.Write(obj.fireballs.Count);
                foreach (Fireball fireball in obj.fireballs)
                    stream.Write(fireball != null ? fireball.actor.Id : -1);
            }
            if (include[ATRocket.Id])
            {
                stream.Write(obj.rockets.Count);
                foreach (Rocket rocket in obj.rockets)
                    stream.Write(rocket != null ? rocket.actor.Id : -1);
            }
            if (include[ATBoostaCoke.Id])
            {
                stream.Write(obj.boostaCokes.Count);
                foreach (BoostaCoke boostaCoke in obj.boostaCokes)
                    stream.Write(boostaCoke != null ? boostaCoke.actor.Id : -1);
            }
        }

        private static unsafe void Read(MemoryStream stream, Player obj)
        {
            byte charId = obj.charId;
            byte skinId = obj.skinId;
            stream.Read(obj, 0x1D4, 0x214);
            obj.charId = charId;
            obj.skinId = skinId;
            Read(stream, obj.actor);
            if (ghostIndex == -1)
                Read(stream, obj.slot);
            else
                stream.Position += 0x3C;
            Read(stream, obj.random);
            Read(stream, obj.groupDrawComp1);
            Read(stream, obj.animSpriteDrawComp1, player: true);
            Read(stream, obj.animSpriteDrawComp2, player: true);
            Read(stream, obj.animSpriteDrawComp3, player: true);
            Read(stream, obj.animSpriteDrawComp4, player: true);
            Read(stream, obj.animSpriteDrawComp5, player: true);
            Read(stream, obj.animSpriteDrawComp6, player: true);
            Read(stream, obj.spriteDrawComp1);
            Read(stream, obj.spriteDrawComp2);
            Read(stream, obj.spriteDrawComp3);
            Read(stream, obj.spriteDrawComp4);
            Read(stream, obj.spriteDrawComp5);
            Read(stream, obj.imageDrawComp1);
            Read(stream, obj.imageDrawComp2);
            Read(stream, obj.imageDrawComp3);
            Read(stream, obj.imageDrawComp4);
            Read(stream, obj.tweener1);
            Read(stream, obj.tweener2);
            Read(stream, obj.tweener3);
            Read(stream, obj.hitboxStanding);
            Read(stream, obj.hitboxSliding);
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

            int hasAfterImages = stream.Read<int>();
            if (hasAfterImages == 1)
            {
                long time = stream.Read<long>();
                if (
                    obj.afterImagesParticleEmitterProvider is AfterImagesParticleEmitterProvider afterImagesParticleEmitterProvider &&
                    afterImagesParticleEmitterProvider.afterImages != null)
                {
                    afterImagesParticleEmitterProvider.afterImages.timespan1 = new TimeSpan(time + dt);
                }
            }
            
            int grappleId = stream.Read<int>();
            applyPtr.Add(() => obj.grapple = (Grapple)contrLookup[grappleId]);
            int ropeId = stream.Read<int>();
            applyPtr.Add(() => obj.rope = (Rope)contrLookup[ropeId]);
            if (include[ATGoldenHook.Id])
            {
                int goldenHookId = stream.Read<int>();
                applyPtr.Add(() => obj.goldenHook = (GoldenHook)contrLookup[goldenHookId]);
            }
            if (include[ATShockwave.Id])
            {
                int shockwaveId = stream.Read<int>();
                applyPtr.Add(() => obj.shockwave = (Shockwave)contrLookup[shockwaveId]);
            }
            if (include[ATDroppedBomb.Id])
            {
                int droppedBombId = stream.Read<int>();
                applyPtr.Add(() => obj.droppedBomb = (DroppedBomb)contrLookup[droppedBombId]);
            }
            if (include[ATFreezeRay.Id])
            {
                int freezeRayId = stream.Read<int>();
                applyPtr.Add(() => obj.freezeRay = (FreezeRay)contrLookup[freezeRayId]);
            }
            int hookedId = stream.Read<int>();
            applyPtr.Add(() => obj.hooked = (Player)contrLookup[hookedId]);
            int unknown1Id = stream.Read<int>();
            applyPtr.Add(() => obj.unknown1 = (Player)contrLookup[unknown1Id]);
            if (include[ATTrigger.Id])
            {
                int trigger1Id = stream.Read<int>();
                int trigger2Id = stream.Read<int>();
                applyPtr.Add(() => obj.trigger1 = (Trigger)contrLookup[trigger1Id]);
                applyPtr.Add(() => obj.trigger2 = (Trigger)contrLookup[trigger2Id]);
            }
            if (include[ATCheckpoint.Id])
            {
                int checkpoint1Id = stream.Read<int>();
                int checkpoint2Id = stream.Read<int>();
                applyPtr.Add(() => obj.checkpoint1 = (Checkpoint)contrLookup[checkpoint1Id]);
                applyPtr.Add(() => obj.checkpoint1 = (Checkpoint)contrLookup[checkpoint2Id]);
            }
            if (include[ATDroppedObstacle.Id])
            {
                int droppedObstacles = stream.Read<int>();
                while (obj.droppedObstacles.Count < droppedObstacles) obj.droppedObstacles.Add(null);
                while (obj.droppedObstacles.Count > droppedObstacles) obj.droppedObstacles.RemoveAt(obj.droppedObstacles.Count - 1);
                for (int i = 0; i < droppedObstacles; i++)
                {
                    int droppedObstacleId = stream.Read<int>();
                    int j = i;
                    applyPtr.Add(() => obj.droppedObstacles[j] = (DroppedObstacle)contrLookup[droppedObstacleId]);
                }
            }
            if (include[ATFireball.Id])
            {
                int fireballs = stream.Read<int>();
                while (obj.fireballs.Count < fireballs) obj.fireballs.Add(null);
                while (obj.fireballs.Count > fireballs) obj.fireballs.RemoveAt(obj.fireballs.Count - 1);
                for (int i = 0; i < fireballs; i++)
                {
                    int fireballId = stream.Read<int>();
                    int j = i;
                    applyPtr.Add(() => obj.fireballs[j] = (Fireball)contrLookup[fireballId]);
                }
            }
            if (include[ATRocket.Id])
            {
                int rockets = stream.Read<int>();
                while (obj.rockets.Count < rockets) obj.rockets.Add(null);
                while (obj.rockets.Count > rockets) obj.rockets.RemoveAt(obj.rockets.Count - 1);
                for (int i = 0; i < rockets; i++)
                {
                    int rocketId = stream.Read<int>();
                    int j = i;
                    applyPtr.Add(() => obj.rockets[j] = (Rocket)contrLookup[rocketId]);
                }
            }
            if (include[ATBoostaCoke.Id])
            {
                int boostaCokes = stream.Read<int>();
                while (obj.boostaCokes.Count < boostaCokes) obj.boostaCokes.Add(null);
                while (obj.boostaCokes.Count > boostaCokes) obj.boostaCokes.RemoveAt(obj.boostaCokes.Count - 1);
                for (int i = 0; i < boostaCokes; i++)
                {
                    int boostacokeId = stream.Read<int>();
                    int j = i;
                    applyPtr.Add(() => obj.boostaCokes[j] = (BoostaCoke)contrLookup[boostacokeId]);
                }
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, PlayerBot obj)
        {
            Write(stream, (Player)obj);
            stream.Write(obj, 0x1D4 + 0x214 + 0x4, 0x48);
            if (include[ATGoldenHook.Id])
                stream.Write(obj.goldenHookBot != null ? obj.goldenHookBot.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, PlayerBot obj)
        {
            Read(stream, (Player)obj);
            stream.Read(obj, 0x1D4 + 0x214 + 0x4, 0x48);
            if (include[ATGoldenHook.Id])
            {
                int hookId = stream.Read<int>();
                applyPtr.Add(() => obj.goldenHookBot = (GoldenHook)contrLookup[hookId]);
            }
            obj.timespanBot = new TimeSpan(obj.timespanBot.Ticks + dt);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, ShakeCameraModifier obj)
        {
            stream.Write(obj, 0x14, 0x34);
            Write(stream, obj.random);
        }

        private static unsafe void Read(MemoryStream stream, ShakeCameraModifier obj)
        {
            stream.Read(obj, 0x14, 0x34);
            Read(stream, obj.random);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
        }

        private static unsafe void Write(MemoryStream stream, ClampCameraModifier obj)
        {
            stream.Write(obj, 0x8, 0x30);
        }

        private static unsafe void Read(MemoryStream stream, ClampCameraModifier obj)
        {
            stream.Read(obj, 0x8, 0x30);
        }

        private static unsafe void Write(MemoryStream stream, Camera obj)
        {
            stream.Write(obj, 0x10, 0x3C);
        }

        private static unsafe void Read(MemoryStream stream, Camera obj)
        {
            stream.Read(obj, 0x10, 0x3C);
        }

        private static unsafe void Write(MemoryStream stream, CameraMP obj)
        {
            stream.Write(obj, 0x14, 0x34);
        }

        private static unsafe void Read(MemoryStream stream, CameraMP obj)
        {
            stream.Read(obj, 0x14, 0x34);
        }

        private static unsafe void Write(MemoryStream stream, CCamera obj)
        {
            stream.Write(obj, 0x1C, 0xA8);
            Write(stream, obj.shakeMod);
            Write(stream, obj.clampMod);
            if (obj.mods[0] is Camera camera)
                Write(stream, camera);
            if (obj.mods[0] is CameraMP cameraMP)
                Write(stream, cameraMP);
        }

        private static unsafe void Read(MemoryStream stream, CCamera obj)
        {
            stream.Read(obj, 0x1C, 0xA8);
            Read(stream, obj.shakeMod);
            Read(stream, obj.clampMod);
            if (obj.mods[0] is Camera camera)
                Read(stream, camera);
            if (obj.mods[0] is CameraMP cameraMP)
                Read(stream, cameraMP);
        }

        private static unsafe void Write(MemoryStream stream, ModuleSolo obj)
        {
            if (moduleProgressOnly)
            {
                stream.Write(1);
                stream.Write(obj.remainingProgress);
                stream.Write(obj.progress);
            }
            else
            {
                stream.Write(-1);
                stream.Write(obj, 0x74, 0x4C);
                Write(stream, obj.random);
                Write(stream, obj.camera);
            }
        }

        private static unsafe void Read(MemoryStream stream, ModuleSolo obj)
        {
            int progressOnly = stream.Read<int>();
            if (progressOnly == 1)
            {
                obj.remainingProgress = stream.Read<float>();
                obj.progress = stream.Read<float>();
            }
            else
            {
                stream.Read(obj, 0x74, 0x4C);
                Read(stream, obj.random);
                Read(stream, obj.camera);
            }
        }

        private static unsafe void Write(MemoryStream stream, ModuleMP obj)
        {
            stream.Write(obj, 0xA8, 0xB8);
            Write(stream, obj.random);
            Write(stream, obj.camera);
        }

        private static unsafe void Read(MemoryStream stream, ModuleMP obj)
        {
            stream.Read(obj, 0xA8, 0xB8);
            Read(stream, obj.random);
            Read(stream, obj.camera);
            obj.timespan1 = new TimeSpan(obj.timespan1.Ticks + dt);
            obj.timespan4 = new TimeSpan(obj.timespan4.Ticks + dt);
        }

        private static readonly List<ActorType> actorTypes = new List<ActorType>();

        // don't ever change the order
        public static readonly ActorType ATPlayer = new ActorType<Player>(Write, Read);
        public static readonly ActorType ATPlayerBot = new ActorType<PlayerBot>(Write, Read);
        public static readonly ActorType ATGrapple = new ActorType<Grapple, GrappleDef>(Write, Read, () => new GrappleDef(0, null));
        public static readonly ActorType ATRope = new ActorType<Rope, RopeDef>(Write, Read, () => new RopeDef());
        public static readonly ActorType ATFireball = new ActorType<Fireball, FireballDef>(Write, Read, () => new FireballDef(Vector2.Zero));
        public static readonly ActorType ATDroppedObstacle = new ActorType<DroppedObstacle, DroppedObstacleDef>(Write, Read, () => new DroppedObstacleDef(null, false));
        public static readonly ActorType ATRocket = new ActorType<Rocket, RocketDef>(Write, Read, () => new RocketDef(null, Main.game.stack.gameInfo));
        public static readonly ActorType ATGoldenHook = new ActorType<GoldenHook, GoldenHookDef>(Write, Read, () => new GoldenHookDef(0, null));
        public static readonly ActorType ATShockwave = new ActorType<Shockwave, ShockwaveDef>(Write, Read, () => new ShockwaveDef(null));
        public static readonly ActorType ATDroppedBomb = new ActorType<DroppedBomb, DroppedBombDef>(Write, Read, () => new DroppedBombDef(Microsoft.Xna.Framework.Color.White, null));
        public static readonly ActorType ATObstacle = new ActorType<Obstacle>(Write, Read);
        public static readonly ActorType ATFreezeRay = new ActorType<FreezeRay, FreezeRayDef>(Write, Read, () => new FreezeRayDef(Vector2.Zero, null));
        public static readonly ActorType ATPickup = new ActorType<Pickup>(Write, Read);
        public static readonly ActorType ATTrigger = new ActorType<Trigger>(Write, Read);
        public static readonly ActorType ATSwitchBlock = new ActorType<SwitchBlock>(Write, Read);
        public static readonly ActorType ATLever = new ActorType<Lever>(Write, Read);
        public static readonly ActorType ATFallTile = new ActorType<FallTile>(Write, Read);
        public static readonly ActorType ATTriggerSaw = new ActorType<TriggerSaw>(Write, Read);
        public static readonly ActorType ATRocketLauncher = new ActorType<RocketLauncher>(Write, Read);
        public static readonly ActorType ATBoostaCoke = new ActorType<BoostaCoke>(Write, Read);
        public static readonly ActorType ATLaser = new ActorType<Laser>(Write, Read);
        public static readonly ActorType ATAIVolume = new ActorType<AIVolume, AIVolumeDef>(Write, Read, () => new AIVolumeDef(Vector2.Zero, Vector2.Zero, 0));
        public static readonly ActorType ATTimer = new ActorType<Timer>(Write, Read);
        public static readonly ActorType ATCheckpoint = new ActorType<Checkpoint>(Write, Read);
        public static readonly ActorType ATStraightRocket = new ActorType<StraightRocket, StraightRocketDef>(Write, Read, () => new StraightRocketDef(Main.game.stack.gameInfo));
        public static readonly ActorType ATBoss1 = new ActorType<Boss1>(Write, Read);
        public static readonly ActorType ATBoss2 = new ActorType<Boss2>(Write, Read);
        public static readonly ActorType ATBoss4 = new ActorType<Boss4>(Write, Read);
        public static readonly ActorType ATBossSaw = new ActorType<BossSaw>(Write, Read);
        public static readonly ActorType ATFallBlock = new ActorType<FallBlock>(Write, Read);
        public static readonly ActorType ATMovingPlatform = new ActorType<MovingPlatform>(Write, Read);

        private MemoryStream stream;

        private readonly List<int> chunkOffsets = new List<int>();

        private static long dt;
        private static int ghostIndex;
        private static bool moduleProgressOnly;
        private static bool[] include = new bool[actorTypes.Count];
        private static readonly NullSafeDict<int, ICActorController> contrLookup = new NullSafeDict<int, ICActorController>();
        private static readonly List<Action> applyPtr = new List<Action>();
        private static readonly HashSet<CActor> fixedIndexActors = new HashSet<CActor>();
        private static readonly HashSet<int> fixedIds = new HashSet<int>();

        public Savestate()
        {
            stream = new MemoryStream();
            stream.Write(-1);
        }

        public MemoryStream Stream { get { return stream; } }

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

            public abstract void Write(MemoryStream stream, ICActorController contr);
            public abstract void Read(MemoryStream stream, ICActorController contr);
            public abstract CEngine.Definition.Actor.ICActorDef CreateDef();
        }

        private class ActorType<A> : ActorType
        {
            private readonly Action<MemoryStream, A> write;
            private readonly Action<MemoryStream, A> read;

            public ActorType(Action<MemoryStream, A> write, Action<MemoryStream, A> read) :
                base(typeof(A), null) 
            {
                this.write = write;
                this.read = read;
            }

            public override void Write(MemoryStream stream, ICActorController contr)
            {
                write(stream, (A)contr);
            }

            public override void Read(MemoryStream stream, ICActorController contr)
            {
                read(stream, (A)contr);
            }

            public override CEngine.Definition.Actor.ICActorDef CreateDef()
            {
                return null;
            }
        }

        private class ActorType<A, D> : ActorType<A> where D : CEngine.Definition.Actor.ICActorDef
        {
            readonly Func<D> createDef;

            public ActorType(Action<MemoryStream, A> write, Action<MemoryStream, A> read, Func<D> createDef) :
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

        public void Save(List<ActorType> actors, EListMode listMode, bool moduleProgressOnly = false)
        {
            if (!Velo.Ingame || Velo.Online)
                return;

            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            Stack stack = Main.game.stack;

            Savestate.moduleProgressOnly = moduleProgressOnly;

            int playerOffset = 0;

            stream.Position = 0;

            chunkOffsets.Clear();
            chunkOffsets.Add((int)stream.Position);

            int version = VERSION;

            stream.Write(version);
            stream.Write(Map.GetCurrentMapId());
            stream.Write(CEngine.CEngine.Instance.GameTime.TotalGameTime.Ticks);

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

            stream.WriteBoolArr(include);

            foreach (Player ghost in Ghosts.Instance.All())
            {
                if (ghost != null && ghost.grapple != null && ghost.rope != null)
                {
                    ghost.grapple.actor.ghostOwnedItem = true;
                    ghost.rope.actor.ghostOwnedItem = true;
                }
            }

            foreach (CActor actor in collisionEngine.actors)
            {
                if (actor.ghostOwnedItem)
                    continue;
                ICActorController controller = actor.Controller;

                if (controller is Player player && !(controller is PlayerBot))
                {
                    if (!player.slot.LocalPlayer && !player.slot.IsBot)
                        continue;
                    if (!include[ATPlayer.Id]) continue;
                    chunkOffsets.Add((int)stream.Position);
                    stream.Write(ATPlayer.Id);
                    stream.Write<int>(player.slot.Index);
                    if (player == Velo.MainPlayer)
                        playerOffset = (int)stream.Position;
                    ATPlayer.Write(stream, player);
                } 
                else if (controller is PlayerBot playerBot)
                {
                    if (!playerBot.slot.LocalPlayer && !playerBot.slot.IsBot)
                        continue;
                    if (!include[ATPlayerBot.Id]) continue;
                    chunkOffsets.Add((int)stream.Position);
                    stream.Write(ATPlayerBot.Id);
                    stream.Write<int>(playerBot.slot.Index);
                    ATPlayerBot.Write(stream, playerBot);
                } 
                else
                {
                    foreach (ActorType type in actorTypes)
                    {
                        if (controller.GetType() == type.Type)
                        {
                            if (!include[type.Id]) continue;
                            chunkOffsets.Add((int)stream.Position);
                            stream.Write(type.Id);
                            type.Write(stream, controller);
                            break;
                        }
                    }
                }
            }

            chunkOffsets.Add((int)stream.Position);
            stream.Write(-1);

            foreach (var module in stack.modules)
            {
                if (module is ModuleSolo moduleSolo)
                    Write(stream, moduleSolo);
                if (module is ModuleMP moduleMP)
                    Write(stream, moduleMP);
            }

            chunkOffsets.Add((int)stream.Position);
            stream.Write(Math.Min(LoadedVersion, Version.VERSION));

            var cooldowns = OfflineGameMods.Instance.CurrentRecording.Rules.Cooldowns;
            stream.Write(cooldowns.Count);
            foreach (var entry in cooldowns)
            {
                stream.Write((int)entry.Key);
                stream.Write(entry.Value.Value);
            }

            stream.Write(playerOffset);

            chunkOffsets.Add((int)stream.Position);
        }

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

            return default;
        }

        public void DestroyAllAfter(Type type, int n)
        {
            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            CWorld world = CEngine.CEngine.Instance.World;

            int c = 0;
            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                if (actor == null || actor.controller == null)
                    continue;
                if (actor.controller.GetType() == type && !actor.ghostOwnedItem)
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
            if (controller is Fireball fireball)
            {
                if (fireball.owner != null && !fireball.owner.slot.LocalPlayer && !fireball.owner.slot.IsBot)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is DroppedObstacle droppedObstacle)
            {
                if (droppedObstacle.owner != null && !droppedObstacle.owner.slot.LocalPlayer && !droppedObstacle.owner.slot.IsBot)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Rocket rocket)
            {
                if (rocket.owner != null && !rocket.owner.slot.LocalPlayer && !rocket.owner.slot.IsBot)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is GoldenHook goldenHook)
            {
                if (goldenHook.owner != null && !goldenHook.owner.slot.LocalPlayer && !goldenHook.owner.slot.IsBot)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is Shockwave shockwave)
            {
                if (shockwave.owner != null && !shockwave.owner.slot.LocalPlayer && !shockwave.owner.slot.IsBot)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is DroppedBomb droppedBomb)
            {
                if (droppedBomb.owner != null && !droppedBomb.owner.slot.LocalPlayer && !droppedBomb.owner.slot.IsBot)
                    actor.ghostOwnedItem = true;
            }
            else if (controller is FreezeRay freezeRay)
            {
                if (freezeRay.owner != null && !freezeRay.owner.slot.LocalPlayer && !freezeRay.owner.slot.IsBot)
                    actor.ghostOwnedItem = true;
            }
        }
#pragma warning restore IDE1006

        public bool Load(bool setGlobalTime, int ghostIndex = -1)
        {
            if (!Velo.Ingame || Velo.Online)
                return false;

            Savestate.ghostIndex = ghostIndex;

            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            CWorld world = CEngine.CEngine.Instance.World;
            Stack stack = Main.game.stack;

            stream.Position = 0;

            int version = stream.Read<int>();
            if (version == -1 || version > VERSION)
                return false;

            int prevIndex = (int)stream.Position;
            stream.Position = stream.Length - sizeof(int);
            int playerOffset = stream.Read<int>();
            stream.Position = prevIndex;

            if (version == 1)
            {
                int solo = stream.Read<int>();
                if ((solo == 1) != (Velo.ModuleSolo != null))
                    return false;
                if (solo == 1)
                {
                    string nameAuthor = stream.ReadStr();
                    if (nameAuthor != Velo.ModuleSolo.LevelData.name + "|" + Velo.ModuleSolo.LevelData.author)
                        return false;
                }
            }
            else if (version >= 2)
            {
                ulong mapId = stream.Read<ulong>();
                if (mapId != Map.GetCurrentMapId())
                    return false;
            }

            long time = stream.Read<long>();
            if (setGlobalTime)
            {
                dt = 0;
                CEngine.CEngine.Instance.gameTime = new GameTime(new TimeSpan(time), CEngine.CEngine.Instance.GameTime.ElapsedGameTime);
            }
            else
            {
                dt = CEngine.CEngine.Instance.GameTime.TotalGameTime.Ticks - time;
            }

            contrLookup.Add(-1, null);

            include = stream.ReadBoolArr();
            if (ghostIndex != -1)
            {
                stream.Position = playerOffset;
                int ghostId = Ghosts.Instance.Get(ghostIndex).actor.id;
                ATPlayer.Read(stream, Ghosts.Instance.Get(ghostIndex).actor.Controller);
                Ghosts.Instance.Get(ghostIndex).actor.id = ghostId;
                goto cleanup;
            }

            int[] counts = new int[actorTypes.Count];

            foreach (Player ghost in Ghosts.Instance.All())
            {
                if (ghost != null && ghost.grapple != null && ghost.rope != null)
                {
                    ghost.grapple.actor.ghostOwnedItem = true;
                    ghost.rope.actor.ghostOwnedItem = true;
                }
            }

            List<CActor> newActors = new List<CActor>();

            while (true)
            {
                int id = stream.Read<int>();

                if (id == -1)
                    break;

                CActor actor = null;

                if (id == ATPlayer.Id)
                {
                    int slotIndex = stream.Read<int>();
                    actor = GetOfType(ATPlayer.Type, 0, (check) => (check.Controller as Player).slot.Index == slotIndex);
                    ATPlayer.Read(stream, actor.Controller);
                }
                else if (id == ATPlayerBot.Id)
                {
                    int slotIndex = stream.Read<int>();
                    actor = GetOfType(ATPlayerBot.Type, 0, (check) => (check.Controller as Player).slot.Index == slotIndex);
                    ATPlayerBot.Read(stream, actor.Controller);
                }
                else
                {
                    foreach (ActorType type in actorTypes)
                    {
                        if (id == type.Id)
                        {
                            actor = GetOfType(type.Type, counts[type.Id]);
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
                                    goto cleanup;
                                actor = GetOfType(type.Type, counts[type.Id]);
                            }
                            type.Read(stream, controller);
                            counts[type.Id]++;
                        }
                    }
                }

                if (actor != null)
                {
                    newActors.Add(actor);
                    fixedIndexActors.Add(actor);
                    fixedIds.Add(actor.id);
                }
            }

            foreach (ActorType type in actorTypes)
            {
                if (type.CreateDef() != null && include[type.Id])
                    DestroyAllAfter(type.Type, counts[type.Id]);
            }

            foreach (var module in stack.modules)
            {
                if (module is ModuleSolo moduleSolo)
                    Read(stream, moduleSolo);
                if (module is ModuleMP moduleMP)
                    Read(stream, moduleMP);
            }

            if (version < 4)
            {
                stream.Read<float>();
                stream.Read<float>();
                stream.Read<float>();
                if (version >= 3)
                    stream.Read<float>();
            }

            if (version >= 4)
                LoadedVersion = stream.Read<ushort>();
            else
                LoadedVersion = 34;

            if (version >= 4)
            {
                var cooldowns = OfflineGameMods.Instance.CurrentRecording.Rules.Cooldowns;
                int count = stream.Read<int>();
                for (int i = 0; i < count; i++)
                {
                    EViolations violation = (EViolations)stream.Read<int>();
                    float value = stream.Read<float>();
                    cooldowns[violation].Value = value;
                }
            }

            CheatEngineDetection.MatchValues();

            foreach (Action action in applyPtr)
                action();

            for (int i = 0; i < collisionEngine.actorsById.Count; i++)
                collisionEngine.actorsById[i] = null;

            int nextId = 0;

            for (int i = 0; i < collisionEngine.actors.Count; i++)
            {
                CActor actor = collisionEngine.actors[i];
                if (!fixedIndexActors.Contains(actor))
                {
                    while (fixedIds.Contains(nextId)) nextId++;
                    newActors.Add(actor);
                    actor.id = nextId++;
                }

                while (collisionEngine.actorsById.Count <= actor.Id) collisionEngine.actorsById.Add(null);
                collisionEngine.actorsById[actor.Id] = actor;
            }

            collisionEngine.actors = newActors;
            world.nextActorId = Math.Max(nextId, fixedIds.Max());

        cleanup:
            contrLookup.Clear();
            applyPtr.Clear();
            fixedIndexActors.Clear();
            fixedIds.Clear();

            return true;
        }

        public Savestate Clone()
        {
            Savestate savestate = new Savestate();
            stream.Position = 0;
            savestate.stream.Position = 0;
            stream.CopyTo(savestate.stream);
            return savestate;
        }

        public void Compress(Savestate reference)
        {
            MemoryStream compressed = new MemoryStream();
            stream.Position = 0;
            reference.stream.Position = 0;

            const bool COPY_MODE = false;
            const bool DIFF_MODE = true;
            bool mode = COPY_MODE;

            while (stream.Position < stream.Length)
            {
                if (mode == COPY_MODE)
                {
                    int count = 0;
                    while (count < 255 && stream.Position < stream.Length && reference.stream.Position < reference.stream.Length)
                    {
                        if (stream.ReadByte() != reference.stream.ReadByte())
                        {
                            stream.Position--;
                            reference.stream.Position--;
                            break;
                        }
                        count++;
                    }
                    compressed.WriteByte((byte)count);
                }
                if (mode == DIFF_MODE)
                {
                    compressed.WriteByte(0);
                    int count = 0;
                    while (count < 255 && stream.Position < stream.Length)
                    {
                        int b = stream.ReadByte();
                        if (reference.stream.Position < reference.stream.Length && b == reference.stream.ReadByte())
                        {
                            stream.Position--;
                            reference.stream.Position--;
                            break;
                        }
                        count++;
                        compressed.WriteByte((byte)b);
                    }
                    compressed.Position -= count + 1;
                    compressed.WriteByte((byte)count);
                    compressed.Position += count;
                }
                mode = !mode;
            }
            stream.Close();
            stream = compressed;
        }

        public void Decompress(Savestate reference)
        {
            MemoryStream uncompressed = new MemoryStream();
            stream.Position = 0;
            reference.stream.Position = 0;

            const bool COPY_MODE = false;
            const bool DIFF_MODE = true;
            bool mode = COPY_MODE;

            while (stream.Position < stream.Length)
            {
                if (mode == COPY_MODE)
                {
                    int count = stream.ReadByte();
                    for (int i = 0; i < count; i++)
                    {
                        uncompressed.WriteByte((byte)reference.stream.ReadByte());
                    }
                }
                if (mode == DIFF_MODE)
                {
                    int count = stream.ReadByte();
                    for (int i = 0; i < count; i++)
                    {
                        uncompressed.WriteByte((byte)stream.ReadByte());
                    }
                    reference.stream.Position += count;
                }
                mode = !mode;
            }
            stream.Close();
            stream = uncompressed;
        }
    }

    public class SavestateStream
    {
        public static readonly int CHUNK_SIZE = 64;

        public class Chunk
        {
            public Savestate[] Savestates = new Savestate[CHUNK_SIZE];
            public int Count = 0;
            public bool Compressed = false;

            public void Clean()
            {
                for (int i = Count; i < CHUNK_SIZE; i++)
                {
                    if (Savestates[i] == null)
                        break;
                    Savestates[i] = null;
                }
            }

            public void Compress()
            {
                if (Compressed)
                    return;
                for (int i = Count - 1; i >= 1; i--)
                {
                    Savestates[i].Compress(Savestates[i - 1]);
                }
                Compressed = true;
            }

            public void Decompress()
            {
                if (!Compressed)
                    return;
                for (int i = 1; i < Count; i++)
                {
                    Savestates[i].Decompress(Savestates[i - 1]);
                }
                Compressed = false;
            }
        }

        private readonly List<Chunk> chunks = new List<Chunk>();
        private int position = 0;
        public int Position
        {
            get => position;
            set
            {
                int oldChunkIndex = position / CHUNK_SIZE;
                int newChunkIndex = value / CHUNK_SIZE;
                if (oldChunkIndex != newChunkIndex)
                {
                    chunks[oldChunkIndex].Compress();
                    if (chunks.Count > newChunkIndex)
                        chunks[newChunkIndex].Decompress();
                }
                position = value;
            }
        }
        public int Length => chunks.Count == 0 ? 0 : CHUNK_SIZE * (chunks.Count - 1) + chunks.Last().Count;

        public SavestateStream()
        {

        }

        public void Write(Savestate savestate)
        {
            int chunkIndex = Position / CHUNK_SIZE;
            int savestateIndex = Position % CHUNK_SIZE;
            if (chunks.Count <= chunkIndex)
            {
                chunks.Add(new Chunk());
            }
            Chunk chunk = chunks[chunkIndex];
            chunk.Savestates[savestateIndex] = savestate;
            chunk.Count = savestateIndex + 1;
            chunk.Clean();
            chunks.RemoveRange(chunkIndex + 1, chunks.Count - chunkIndex - 1);

            Position++;
        }

        public Savestate Read()
        {
            int chunkIndex = Position / CHUNK_SIZE;
            int savestateIndex = Position % CHUNK_SIZE;
            Chunk chunk = chunks[chunkIndex];
            Savestate savestate = chunk.Savestates[savestateIndex];

            Position++;
            return savestate;
        }

        public void Clear()
        {
            chunks.Clear();
        }
    }
}
