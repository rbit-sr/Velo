using CEngine.Graphics.Camera;
using CEngine.Graphics.Camera.Modifier;
using CEngine.Graphics.Component;
using CEngine.Util.Draw;
using CEngine.World;
using CEngine.World.Actor;
using CEngine.World.Collision;
using CEngine.World.Collision.Shape;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private readonly int VERSION = 7;

        public static ushort LoadedVeloVersion = Version.VERSION;

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
            stream.WriteStr(obj.nextAnimation, 16);
            stream.WriteStr(obj.animation.id, 16);
        }

        private static unsafe void Read(MemoryStream stream, CAnimatedSpriteDrawComponent obj, bool playerPreserve = false)
        {
            Color color = obj.Color;
            float opacity = obj.Opacity;
            Vector2 offset = obj.Offset;
            Vector2 scale = obj.Scale;
            Vector2 size = obj.Size;
            Vector2 origin = obj.Origin;
            stream.Read(obj, 0x2C, 0x84);
            if (playerPreserve)
            {
                obj.Color = color;
                obj.Opacity = opacity;
                obj.Offset = offset;
                obj.Scale = scale;
                obj.Size = size;
                obj.Origin = origin;
            }
            Read(stream, obj.bounds);
            obj.nextAnimation = stream.ReadStr(version >= 7 ? 16 : 0);
            obj.time = new TimeSpan(obj.time.Ticks + dt);
            string animationId = stream.ReadStr(version >= 5 ? 16 : 0);
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

        private static unsafe void Write(MemoryStream stream, CLineDrawComponent obj, int minCount = 0)
        {
            stream.Write(obj, 0x18, 0x4);
            stream.Write(obj.lines.Count);
            foreach (CLine line in obj.lines)
                Write(stream, line);
            for (int i = obj.Lines.Count; i < minCount; i++)
                Write(stream, new CLine(Vector2.Zero, Vector2.Zero, Color.Transparent));
        }

        private static unsafe void Read(MemoryStream stream, CLineDrawComponent obj, int minCount = 0)
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
            for (int i = count; i < minCount; i++)
            {
                CLine dummy = new CLine(Vector2.Zero, Vector2.Zero, Color.Black);
                Read(stream, dummy);
            }
        }

        private static unsafe void Write(MemoryStream stream, Tweener obj)
        {
            stream.Write(obj, 0x10, 0x1C, typeof(BaseTweener<float>));
        }

        private static unsafe void Read(MemoryStream stream, Tweener obj)
        {
            stream.Read(obj, 0x10, 0x1C, typeof(BaseTweener<float>));
        }

        private static unsafe void Write(MemoryStream stream, Slot obj)
        {
            stream.Write(obj, 0x6C, 0x3C);
        }

        private static unsafe void Read(MemoryStream stream, Slot obj)
        {
            byte charId = obj.charId;
            byte skinId = obj.skinId;
            if (version <= 5)
                stream.Position += 4;
            if (version <= 5)
                stream.Read(obj, 0x6C, 0x38);
            else
                stream.Read(obj, 0x6C, 0x3C);
            obj.charId = charId;
            obj.skinId = skinId;
        }

        private static unsafe void Write(MemoryStream stream, Random obj)
        {
            stream.Write(obj, 0x8, 0x8);
            FieldInfo array = typeof(Random).
                GetFields(BindingFlags.NonPublic | BindingFlags.Instance).
                Where(f => f.FieldType.IsArray).
                First();
            stream.WriteArrFixed((int[])array.GetValue(obj), 56);

            //stream.Write(obj, 0x1C, 0xE0);
        }

        private static unsafe void Read(MemoryStream stream, Random obj)
        {
            stream.Read(obj, 0x8, 0x8);
            FieldInfo array = typeof(Random).
                GetFields(BindingFlags.NonPublic | BindingFlags.Instance).
                Where(f => f.FieldType.IsArray).
                First();
            int[] value = stream.ReadArrFixed<int>(56);
            array.SetValue(obj, value);

            //stream.Read(obj, 0x1C, 0xE0);
        }

        private static unsafe void Write(MemoryStream stream, Grapple obj)
        {
            stream.Write(obj, 0x24, 0x14);
            Write(stream, obj.actor);
            Write(stream, obj.breakSprite);
            Write(stream, obj.sprite);
            Write(stream, obj.bounds);
            if (include[ATPlayer.Id])
                stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Grapple obj)
        {
            stream.Read(obj, 0x24, 0x14);
            Read(stream, obj.actor);
            Read(stream, obj.breakSprite);
            Read(stream, obj.sprite);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            if (include[ATPlayer.Id])
            {
                int ownerId = stream.Read<int>();
                applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Rope obj)
        {
            stream.Write(obj, 0x20, 0x24);
            Write(stream, obj.actor);
            Write(stream, obj.line1);
            Write(stream, obj.line2);
            Write(stream, obj.line3);
            Write(stream, obj.lineDrawComp1, 2);
            stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
            if (obj.target == null)
                stream.Write(-1);
            else if (obj.target is Player player)
            {
                if (!include[ATPlayer.Id])
                    stream.Write(-2);
                else
                    stream.Write(player.actor.Id);
            }
            else if (obj.target is GoldenHook goldenHook)
            {
                if (!include[ATGoldenHook.Id])
                    stream.Write(-2);
                else
                    stream.Write(goldenHook.actor.Id);
            }
            else if (obj.target is Grapple grapple)
            {
                if (!include[ATGrapple.Id])
                    stream.Write(-2);
                else
                    stream.Write(grapple.actor.Id);
            }
        }

        private static unsafe void Read(MemoryStream stream, Rope obj)
        {
            stream.Read(obj, 0x20, 0x24);
            Read(stream, obj.actor);
            Read(stream, obj.line1);
            Read(stream, obj.line2);
            Read(stream, obj.line3);
            Read(stream, obj.lineDrawComp1, version >= 5 ? 2 : 0);
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
            Write(stream, obj.sprite);
            Write(stream, obj.bounds);
            if (include[ATPlayer.Id])
                stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
            if (include[ATAIVolume.Id])
                stream.Write(obj.aiVolume != null ? obj.aiVolume.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, DroppedObstacle obj)
        {
            stream.Read(obj, 0x24, 0x24);
            Read(stream, obj.actor);
            Read(stream, obj.sprite);
            Read(stream, obj.bounds);
            obj.breakTime = new TimeSpan(obj.breakTime.Ticks + dt);
            contrLookup.Add(obj.actor.Id, obj);
            if (include[ATPlayer.Id])
            {
                int ownerId = stream.Read<int>();
                applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            }
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
            Write(stream, obj.sprite);
            Write(stream, obj.bounds);
            if (obj.sprite.Sprite == null)
                stream.Write(0);
            else if (obj.sprite.Sprite == obj.animImage1)
                stream.Write(1);
            else
                stream.Write(2);
            if (include[ATPlayer.Id])
                stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
            if (include[ATShockwave.Id])
                stream.Write(obj.shockwave != null ? obj.shockwave.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Fireball obj)
        {
            stream.Read(obj, 0x3C, 0x3C);
            Read(stream, obj.actor);
            Read(stream, obj.sprite);
            Read(stream, obj.bounds);
            int sprite = stream.Read<int>();
            if (sprite == 0)
                obj.sprite.Sprite = null;
            else if (sprite == 1)
                obj.sprite.Sprite = obj.animImage1;
            else
                obj.sprite.Sprite = obj.animImage2;
            contrLookup.Add(obj.actor.Id, obj);
            if (include[ATPlayer.Id])
            {
                int ownerId = stream.Read<int>();
                applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            }
            if (include[ATShockwave.Id])
            {
                int shockwaveId = stream.Read<int>();
                applyPtr.Add(() => obj.shockwave = (Shockwave)contrLookup[shockwaveId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Rocket obj)
        {
            stream.Write(obj, 0x38, 0x3C);
            Write(stream, obj.actor);
            Write(stream, obj.sprite);
            Write(stream, obj.bounds);
            if (include[ATPlayer.Id])
            {
                stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
                stream.Write(obj.target != null ? obj.target.actor.Id : -1);
                stream.Write(obj.unknown != null ? obj.unknown.actor.Id : -1);
            }
        }

        private static unsafe void Read(MemoryStream stream, Rocket obj)
        {
            if (version <= 5)
                stream.Read(obj, 0x38, 0x38);
            else
                stream.Read(obj, 0x38, 0x3C);
            Read(stream, obj.actor);
            Read(stream, obj.sprite);
            Read(stream, obj.bounds);
            obj.shootTime = new TimeSpan(obj.shootTime.Ticks + dt);
            contrLookup.Add(obj.actor.Id, obj);
            if (include[ATPlayer.Id])
            {
                int ownerId = stream.Read<int>();
                int targetId = stream.Read<int>();
                int unknownId = stream.Read<int>();
                applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
                applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
                applyPtr.Add(() => obj.unknown = (Player)contrLookup[unknownId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, GoldenHook obj)
        {
            stream.Write(obj, 0x30, 0x1C);
            Write(stream, obj.actor);
            Write(stream, obj.sprite);
            Write(stream, obj.breakSprite);
            Write(stream, obj.bounds);
            if (include[ATPlayer.Id])
            {
                stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
                stream.Write(obj.target != null ? obj.target.actor.Id : -1);
                stream.Write(obj.grabbed != null ? obj.grabbed.actor.Id : -1);
            }
        }

        private static unsafe void Read(MemoryStream stream, GoldenHook obj)
        {
            stream.Read(obj, 0x30, 0x1C);
            Read(stream, obj.actor);
            Read(stream, obj.sprite);
            Read(stream, obj.breakSprite);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            if (include[ATPlayer.Id])
            {
                int ownerId = stream.Read<int>();
                int targetId = stream.Read<int>();
                int grabbedId = stream.Read<int>();
                applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
                applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
                applyPtr.Add(() => obj.grabbed = (Player)contrLookup[grabbedId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Shockwave obj)
        {
            stream.Write(obj, 0x2C, 0x30);
            Write(stream, obj.actor);
            Write(stream, obj.anticipationSprite);
            Write(stream, obj.spriteNeg180);
            Write(stream, obj.spriteNeg90);
            Write(stream, obj.sprite0);
            Write(stream, obj.sprit90);
            Write(stream, obj.groupDraw);
            if (include[ATPlayer.Id])
                stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, Shockwave obj)
        {
            stream.Read(obj, 0x2C, 0x30);
            Read(stream, obj.actor);
            Read(stream, obj.anticipationSprite);
            Read(stream, obj.spriteNeg180);
            Read(stream, obj.spriteNeg90);
            Read(stream, obj.sprite0);
            Read(stream, obj.sprit90);
            Read(stream, obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            if (include[ATPlayer.Id])
            {
                int ownerId = stream.Read<int>();
                applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, DroppedBomb obj)
        {
            stream.Write(obj, 0x28, 0x18);
            Write(stream, obj.actor);
            Write(stream, obj.bounds);
            Write(stream, obj.sprite);
            Write(stream, obj.blinkingSprite);
            Write(stream, obj.groupDraw);
            if (include[ATPlayer.Id])
                stream.Write(obj.owner != null ? obj.owner.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, DroppedBomb obj)
        {
            stream.Read(obj, 0x28, 0x18);
            Read(stream, obj.actor);
            Read(stream, obj.bounds);
            Read(stream, obj.sprite);
            Read(stream, obj.blinkingSprite);
            Read(stream, obj.groupDraw);
            contrLookup.Add(obj.actor.Id, obj);
            if (include[ATPlayer.Id])
            {
                int ownerId = stream.Read<int>();
                applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void WriteEa(MemoryStream stream, EditableActor obj)
        {
            stream.Write(obj, 0x30, 0x24, typeof(EditableActor));
            Write(stream, obj.actor);
            Write(stream, obj.bounds);
        }

        private static unsafe void ReadEa(MemoryStream stream, EditableActor obj)
        {
            stream.Read(obj, 0x30, 0x24, typeof(EditableActor));
            Read(stream, obj.actor);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
        }

        private static unsafe void Write(MemoryStream stream, Obstacle obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x14, 0x20);
            Write(stream, obj.sprite);
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
            Read(stream, obj.sprite);
            Read(stream, obj.groupDraw);
            obj.breakTime = new TimeSpan(obj.breakTime.Ticks + dt);
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
            if (include[ATPlayer.Id])
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
            if (include[ATPlayer.Id])
            {
                int ownerId = stream.Read<int>();
                applyPtr.Add(() => obj.owner = (Player)contrLookup[ownerId]);
            }
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Pickup obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x10, 0xC);
            Write(stream, obj.sprite);
            Write(stream, obj.boxPopSprite);
            stream.Write(obj.frontStarImage != null ? 1 : 0);
            if (obj.frontStarImage != null)
            {
                Write(stream, obj.frontStarImage);
                Write(stream, obj.backStarImage);
            }
            Write(stream, obj.groupDraw);
        }

        private static unsafe void Read(MemoryStream stream, Pickup obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x10, 0xC);
            Read(stream, obj.sprite);
            Read(stream, obj.boxPopSprite);
            int notNull = stream.Read<int>();
            if (notNull != 0)
            {
                Read(stream, obj.frontStarImage);
                Read(stream, obj.backStarImage);
            }
            Read(stream, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void WriteRea(MemoryStream stream, ResizableEditableActor obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x0, 0x14, typeof(ResizableEditableActor));
        }

        private static unsafe void ReadRea(MemoryStream stream, ResizableEditableActor obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x0, 0x14, typeof(ResizableEditableActor));
        }

        private static unsafe void WriteTriggerContrList(MemoryStream stream, List<ICActorController> list, int minCount = 0)
        {
            stream.Write(list.Count);
            foreach (ICActorController contr in list)
            {
                if (contr == null)
                {
                    stream.Write(-1);
                    continue;
                }

                if (contr is Player player)
                {
                    if (include[ATPlayer.Id])
                        stream.Write(player.actor.Id);
                    else
                        stream.Write(-2);
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
            for (int i = list.Count; i < minCount; i++)
            {
                stream.Write(0);
            }
        }

        private static unsafe void Write(MemoryStream stream, Trigger obj)
        {
            WriteRea(stream, obj);
            stream.Write(obj, 0x68 + 0x1C, 0x8);
            WriteTriggerContrList(stream, obj.list1, 8);
            WriteTriggerContrList(stream, obj.list2, 8);
        }

        private static unsafe void ReadTriggerContrList(MemoryStream stream, List<ICActorController> list, int minCount = 0)
        {
            int count = stream.Read<int>();
            while (list.Count < count) list.Add(null);
            while (list.Count > count) list.RemoveAt(list.Count - 1);
            for (int i = 0; i < count; i++)
            {
                int contrId = stream.Read<int>();
                if (contrId == -2)
                    continue;
                int j = i;
                applyPtr.Add(() => list[j] = contrLookup[contrId]);
            }
            for (int i = list.Count; i < minCount; i++)
            {
                stream.Read<int>();
            }
        }

        private static unsafe void Read(MemoryStream stream, Trigger obj)
        {
            ReadRea(stream, obj);
            stream.Read(obj, 0x68 + 0x1C, 0x8);
            ReadTriggerContrList(stream, obj.list1, version >= 7 ? 8 : 0);
            ReadTriggerContrList(stream, obj.list2, version >= 7 ? 8 : 0);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, SwitchBlock obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x24, 0x28);
            Write(stream, obj.sprite);
            Write(stream, obj.triggerSparkSprite);
            Write(stream, (CConvexPolygon)obj.colShape);
            Write(stream, obj.groupDraw);
        }

        private static unsafe void Read(MemoryStream stream, SwitchBlock obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x24, 0x28);
            Read(stream, obj.sprite);
            Read(stream, obj.triggerSparkSprite);
            Read(stream, (CConvexPolygon)obj.colShape);
            Read(stream, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Lever obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x14, 0x4);
            Write(stream, obj.sprite);
            Write(stream, obj.triggerSparkSprite);
        }

        private static unsafe void Read(MemoryStream stream, Lever obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x14, 0x4);
            Read(stream, obj.sprite);
            Read(stream, obj.triggerSparkSprite);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, FallTile obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x4, 0x18);
            Write(stream, obj.sprite);
            Write(stream, obj.groupDraw);
        }

        private static unsafe void Read(MemoryStream stream, FallTile obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x4, 0x18);
            Read(stream, obj.sprite);
            obj.steppedOnOrBrokenTime = new TimeSpan(obj.steppedOnOrBrokenTime.Ticks + dt);
            Read(stream, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, TriggerSaw obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x18, 0x28);
            Write(stream, obj.image);
            Write(stream, obj.convexPoly);
            Write(stream, obj.groupDraw);
        }

        private static unsafe void Read(MemoryStream stream, TriggerSaw obj)
        {
            ReadEa(stream, obj);
            stream.Read(obj, 0x54 + 0x18, 0x28);
            Read(stream, obj.image);
            Read(stream, obj.convexPoly);
            Read(stream, obj.groupDraw);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, RocketLauncher obj)
        {
            WriteEa(stream, obj);
            stream.Write(obj, 0x54 + 0x1C, 0xC);
            Write(stream, obj.sprite);
            Write(stream, obj.gearSprite);
            Write(stream, obj.groupDraw);
            if (include[ATPlayer.Id])
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
            Read(stream, obj.sprite);
            Read(stream, obj.gearSprite);
            Read(stream, obj.groupDraw);
            if (include[ATPlayer.Id])
            {
                int targetId = stream.Read<int>();
                applyPtr.Add(() => obj.target = (Player)contrLookup[targetId]);
            }
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
            if (include[ATPlayer.Id])
                stream.Write(obj.player != null ? obj.player.actor.Id : -1);
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
            if (include[ATPlayer.Id])
            {
                int playerId = stream.Read<int>();
                applyPtr.Add(() => obj.player = (Player)contrLookup[playerId]);
            }
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
            Write(stream, obj.sprite);
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
            Read(stream, obj.sprite);
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

        private static unsafe void WriteTimerContrList(MemoryStream stream, List<ICActorController> list, int minCount = 0)
        {
            stream.Write(list.Count);
            foreach (ICActorController contr in list)
                stream.Write(contr != null ? ((Player)contr).actor.Id : -1);
            for (int i = list.Count; i < minCount; i++)
            {
                stream.Write(0);
            }
        }

        private static unsafe void Write(MemoryStream stream, Timer obj)
        {
            WriteRea(stream, obj);
            stream.Write(obj, 0x68 + 0x10, 0x14);
            stream.Write(obj.unknown.value);
            if (include[ATPlayer.Id])
            {
                WriteTimerContrList(stream, obj.list1, 4);
                WriteTimerContrList(stream, obj.list2, 4);
            }
        }

        private static unsafe void ReadTimerContrList(MemoryStream stream, List<ICActorController> list, int minCount = 0)
        {
            int count = stream.Read<int>();
            while (list.Count < count) list.Add(null);
            while (list.Count > count) list.RemoveAt(list.Count - 1);
            for (int i = 0; i < count; i++)
            {
                int contrId = stream.Read<int>();
                int j = i;
                applyPtr.Add(() => list[j] = contrLookup[contrId]);
            }
            for (int i = count; i < minCount; i++)
            {
                stream.Read<int>();
            }
        }

        private static unsafe void Read(MemoryStream stream, Timer obj)
        {
            ReadRea(stream, obj);
            stream.Read(obj, 0x68 + 0x10, 0x14);
            obj.unknown.value = stream.Read<int>();
            if (include[ATPlayer.Id])
            {
                ReadTimerContrList(stream, obj.list1, version >= 7 ? 4 : 0);
                ReadTimerContrList(stream, obj.list2, version >= 7 ? 4 : 0);
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
            Write(stream, obj.sprite);
            Write(stream, obj.bounds);
        }

        private static unsafe void Read(MemoryStream stream, StraightRocket obj)
        {
            stream.Read(obj, 0x28, 0x18);
            Read(stream, obj.actor);
            Read(stream, obj.sprite);
            Read(stream, obj.bounds);
            contrLookup.Add(obj.actor.Id, obj);
            obj.actor.UpdateCollision();
        }

        private static unsafe void WriteBb(MemoryStream stream, BossBase obj)
        {
            stream.Write(obj, 0x18, 0xC, typeof(BossBase));
            Write(stream, obj.actor);
            Write(stream, obj.bounds);
        }

        private static unsafe void ReadBb(MemoryStream stream, BossBase obj)
        {
            stream.Read(obj, 0x18, 0xC, typeof(BossBase));
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
            if (include[ATPlayer.Id])
                stream.Write(obj.player != null ? obj.player.actor.Id : -1);
        }

        private static unsafe void Read(MemoryStream stream, MovingPlatform obj)
        {
            stream.Read(obj, 0x24, 0x38);
            Read(stream, obj.actor);
            Read(stream, obj.bounds);
            Read(stream, obj.imageDrawComp);
            if (include[ATPlayer.Id])
            {
                int playerId = stream.Read<int>();
                applyPtr.Add(() => obj.player = (Player)contrLookup[playerId]);
            }
            contrLookup.Add(obj.actor.Id, obj);
            obj.actor.UpdateCollision();
        }

        private static unsafe void Write(MemoryStream stream, Player obj)
        {
            stream.Write(obj, 0x1D4, 0x214, typeof(Player));
            Write(stream, obj.actor);
            Write(stream, obj.slot);
            Write(stream, obj.random);
            Write(stream, obj.groupDrawComp1);
            Write(stream, obj.sprite);
            Write(stream, obj.drillSprite);
            Write(stream, obj.starsSprite);
            Write(stream, obj.puffSprite);
            Write(stream, obj.bombTriggerSprite);
            Write(stream, obj.pushWaveReactionSprite);
            Write(stream, obj.itemBalloonSprite);
            Write(stream, obj.itemSprite);
            Write(stream, obj.playerArrowSprite);
            Write(stream, obj.iceBlockSprite);
            Write(stream, obj.iceBlockSpriteShaded);
            Write(stream, obj.exclamationMarkImage);
            Write(stream, obj.badConnectionImage);
            Write(stream, obj.wrongWayImage);
            Write(stream, obj.winStarImage);
            Write(stream, obj.tweener1);
            Write(stream, obj.tweener2);
            Write(stream, obj.winStarExpansionTweener);
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
                stream.Write((long)0);
            }

            if (include[ATGrapple.Id])
                stream.Write(obj.grapple != null ? obj.grapple.actor.Id : -1);
            if (include[ATRope.Id])
                stream.Write(obj.rope != null ? obj.rope.actor.Id : -1);
            if (include[ATGoldenHook.Id])
                stream.Write(obj.goldenHook != null ? obj.goldenHook.actor.Id : -1);
            if (include[ATShockwave.Id])
                stream.Write(obj.shockwave != null ? obj.shockwave.actor.Id : -1);
            if (include[ATDroppedBomb.Id])
                stream.Write(obj.droppedBomb != null ? obj.droppedBomb.actor.Id : -1);
            if (include[ATFreezeRay.Id])
                stream.Write(obj.freezeRay != null ? obj.freezeRay.actor.Id : -1);
            stream.Write(obj.hookedBy != null ? obj.hookedBy.actor.Id : -1);
            stream.Write(obj.unknown1_ != null ? obj.unknown1_.actor.Id : -1);
            if (include[ATTrigger.Id])
            {
                stream.Write(obj.triggerToReactivateOnRespawn != null ? obj.triggerToReactivateOnRespawn.actor.Id : -1);
                stream.Write(obj.activatedTrigger != null ? obj.activatedTrigger.actor.Id : -1);
            }
            if (include[ATCheckpoint.Id])
            {
                stream.Write(obj.currentCheckpoint != null ? obj.currentCheckpoint.actor.Id : -1);
                stream.Write(obj.nextCheckpoint != null ? obj.nextCheckpoint.actor.Id : -1);
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
            byte charId = obj.characterId;
            byte skinId = obj.skinId;
            stream.Read(obj, 0x1D4, 0x214, typeof(Player));
            bool charIdChanged = charId != obj.characterId;
            obj.characterId = charId;
            obj.skinId = skinId;
            Read(stream, obj.actor);
            if (ghostIndex == -1)
                Read(stream, obj.slot);
            else
                stream.Position += 0x3C;
            Read(stream, obj.random);
            Read(stream, obj.groupDrawComp1);
            Read(stream, obj.sprite, playerPreserve: charIdChanged);
            Read(stream, obj.drillSprite);
            Read(stream, obj.starsSprite);
            Read(stream, obj.puffSprite);
            Read(stream, obj.bombTriggerSprite);
            Read(stream, obj.pushWaveReactionSprite);
            Read(stream, obj.itemBalloonSprite);
            Read(stream, obj.itemSprite);
            Read(stream, obj.playerArrowSprite);
            Read(stream, obj.iceBlockSprite);
            Read(stream, obj.iceBlockSpriteShaded);
            Read(stream, obj.exclamationMarkImage);
            Read(stream, obj.badConnectionImage);
            Read(stream, obj.wrongWayImage);
            Read(stream, obj.winStarImage);
            Read(stream, obj.tweener1);
            Read(stream, obj.tweener2);
            Read(stream, obj.winStarExpansionTweener);
            Read(stream, obj.hitboxStanding);
            Read(stream, obj.hitboxSliding);
            obj.jumpTime = new TimeSpan(obj.jumpTime.Ticks + dt);
            obj.repressJumpTime = new TimeSpan(obj.repressJumpTime.Ticks + dt);
            obj.wallGetOffTime = new TimeSpan(obj.wallGetOffTime.Ticks + dt);
            obj.stunnedTime = new TimeSpan(obj.stunnedTime.Ticks + dt);
            obj.stumbleTime = new TimeSpan(obj.stumbleTime.Ticks + dt);
            obj.grappleTime = new TimeSpan(obj.grappleTime.Ticks + dt);
            obj.slideTime = new TimeSpan(obj.slideTime.Ticks + dt);
            obj.slideTime2 = new TimeSpan(obj.slideTime2.Ticks + dt);
            obj.drillTime = new TimeSpan(obj.drillTime.Ticks + dt);
            obj.roundStartTime = new TimeSpan(obj.roundStartTime.Ticks + dt);
            obj.hookedTime = new TimeSpan(obj.hookedTime.Ticks + dt);
            obj.itemHitTime = new TimeSpan(obj.itemHitTime.Ticks + dt);
            obj.stillAliveTime = new TimeSpan(obj.stillAliveTime.Ticks + dt);
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
            else if (version >= 7)
                stream.Read<long>();

            if (include[ATGrapple.Id])
            {
                int grappleId = stream.Read<int>();
                applyPtr.Add(() => obj.grapple = (Grapple)contrLookup[grappleId]);
            }
            if (include[ATRope.Id])
            {
                int ropeId = stream.Read<int>();
                applyPtr.Add(() => obj.rope = (Rope)contrLookup[ropeId]);
            }
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
            applyPtr.Add(() => obj.hookedBy = (Player)contrLookup[hookedId]);
            int unknown1Id = stream.Read<int>();
            applyPtr.Add(() => obj.unknown1_ = (Player)contrLookup[unknown1Id]);
            if (include[ATTrigger.Id])
            {
                int trigger1Id = stream.Read<int>();
                int trigger2Id = stream.Read<int>();
                applyPtr.Add(() => obj.triggerToReactivateOnRespawn = (Trigger)contrLookup[trigger1Id]);
                applyPtr.Add(() => obj.activatedTrigger = (Trigger)contrLookup[trigger2Id]);
            }
            if (include[ATCheckpoint.Id])
            {
                int checkpoint1Id = stream.Read<int>();
                int checkpoint2Id = stream.Read<int>();
                applyPtr.Add(() => obj.currentCheckpoint = (Checkpoint)contrLookup[checkpoint1Id]);
                applyPtr.Add(() => obj.nextCheckpoint = (Checkpoint)contrLookup[checkpoint2Id]);
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
            obj.startTime = new TimeSpan(obj.startTime.Ticks + dt);
        }

        private static unsafe void Write(MemoryStream stream, ClampCameraModifier obj)
        {
            stream.Write(obj, 0x8, 0x30);
        }

        private static unsafe void Read(MemoryStream stream, ClampCameraModifier obj)
        {
            stream.Read(obj, 0x8, 0x30);
        }

        private static unsafe void Write(MemoryStream stream, SoloCameraModifier obj)
        {
            stream.Write(obj, 0x10, 0x3C);
        }

        private static unsafe void Read(MemoryStream stream, SoloCameraModifier obj)
        {
           // float heightRatioTo720 = obj.heightRatioTo720;
            stream.Read(obj, 0x10, 0x3C);
           // obj.heightRatioTo720 = heightRatioTo720;
        }

        private static unsafe void Write(MemoryStream stream, MultiplayerCameraModifier obj)
        {
            stream.Write(obj, 0x14, 0x34);
        }

        private static unsafe void Read(MemoryStream stream, MultiplayerCameraModifier obj)
        {
            //float heightRatioTo720 = obj.heightRatioTo720;
            stream.Read(obj, 0x14, 0x34);
            //obj.heightRatioTo720 = heightRatioTo720;
        }

        private static unsafe void Write(MemoryStream stream, CCamera obj)
        {
            stream.Write(obj, 0x1C, 0xA8);
            Write(stream, obj.shakeCameraModifier);
            Write(stream, obj.clampCameraModifier);
            if (obj.mods[0] is SoloCameraModifier solo)
                Write(stream, solo);
            if (obj.mods[0] is MultiplayerCameraModifier multiplayer)
                Write(stream, multiplayer);
        }

        private static unsafe void Read(MemoryStream stream, CCamera obj)
        {
            //Viewport viewport = obj.viewport;
            stream.Read(obj, 0x1C, 0xA8);
            //obj.viewport = viewport;
            Read(stream, obj.shakeCameraModifier);
            Read(stream, obj.clampCameraModifier);
            if (obj.mods[0] is SoloCameraModifier solo)
                Read(stream, solo);
            if (obj.mods[0] is MultiplayerCameraModifier multiplayer)
                Read(stream, multiplayer);
        }

        private static unsafe void Write(MemoryStream stream, ModuleSolo obj, bool progressOnly)
        {
            if (progressOnly)
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

        private static unsafe void Read(MemoryStream stream, ModuleSolo obj, ref bool progressOnly)
        {
            progressOnly = stream.Read<int>() == 1;
            if (progressOnly)
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

        public static readonly List<ActorType> ActorTypes = new List<ActorType>();

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
        public static readonly ActorType ATDroppedBomb = new ActorType<DroppedBomb, DroppedBombDef>(Write, Read, () => new DroppedBombDef(Color.White, null));
        public static readonly ActorType ATObstacle = new ActorType<Obstacle, ObstacleDef>(Write, Read, () => new ObstacleDef(Vector2.Zero, null, null, false, false, false));
        public static readonly ActorType ATFreezeRay = new ActorType<FreezeRay, FreezeRayDef>(Write, Read, () => new FreezeRayDef(Vector2.Zero, null));
        public static readonly ActorType ATPickup = new ActorType<Pickup, PickupDef>(Write, Read, () => new PickupDef(Vector2.Zero, null, true, false, false, false));
        public static readonly ActorType ATTrigger = new ActorType<Trigger, TriggerDef>(Write, Read, () => new TriggerDef(Vector2.Zero, null, false, false));
        public static readonly ActorType ATSwitchBlock = new ActorType<SwitchBlock, SwitchBlockDef>(Write, Read, () => new SwitchBlockDef(Vector2.Zero, null, false));
        public static readonly ActorType ATLever = new ActorType<Lever, LeverDef>(Write, Read, () => new LeverDef(Vector2.Zero, null, false));
        public static readonly ActorType ATFallTile = new ActorType<FallTile, FallTileDef>(Write, Read, () => new FallTileDef(Vector2.Zero, false));
        public static readonly ActorType ATTriggerSaw = new ActorType<TriggerSaw, TriggerSawDef>(Write, Read, () => new TriggerSawDef(Vector2.Zero, null, false));
        public static readonly ActorType ATRocketLauncher = new ActorType<RocketLauncher, RocketLauncherDef>(Write, Read, () => new RocketLauncherDef(Vector2.Zero, Main.game.stack, false));
        public static readonly ActorType ATBoostaCoke = new ActorType<BoostaCoke, BoostaCokeDef>(Write, Read, () => new BoostaCokeDef(Vector2.Zero, false));
        public static readonly ActorType ATLaser = new ActorType<Laser, LaserDef>(Write, Read, () => new LaserDef(Vector2.Zero, false));
        public static readonly ActorType ATAIVolume = new ActorType<AIVolume, AIVolumeDef>(Write, Read, () => new AIVolumeDef(Vector2.Zero, Vector2.Zero, 0));
        public static readonly ActorType ATTimer = new ActorType<Timer>(Write, Read);
        public static readonly ActorType ATCheckpoint = new ActorType<Checkpoint, CheckpointDef>(Write, Read, () => new CheckpointDef(Vector2.Zero, null, false));
        public static readonly ActorType ATStraightRocket = new ActorType<StraightRocket, StraightRocketDef>(Write, Read, () => new StraightRocketDef(Main.game.stack.gameInfo));
        public static readonly ActorType ATBoss1 = new ActorType<Boss1>(Write, Read);
        public static readonly ActorType ATBoss2 = new ActorType<Boss2>(Write, Read);
        public static readonly ActorType ATBoss4 = new ActorType<Boss4>(Write, Read);
        public static readonly ActorType ATBossSaw = new ActorType<BossSaw>(Write, Read);
        public static readonly ActorType ATFallBlock = new ActorType<FallBlock>(Write, Read);
        public static readonly ActorType ATMovingPlatform = new ActorType<MovingPlatform>(Write, Read);

        private readonly MemoryStream stream;

        private static long dt;
        private static int ghostIndex;
        private static readonly bool[] include = new bool[ActorTypes.Count];
        private static readonly NullSafeDict<int, ICActorController> contrLookup = new NullSafeDict<int, ICActorController>();
        private static readonly List<Action> applyPtr = new List<Action>();
        private static readonly HashSet<CActor> fixedIndexActors = new HashSet<CActor>();
        private static readonly HashSet<int> fixedIds = new HashSet<int>();
        private static int version;

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
                ActorTypes.Add(this);
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

        public void Save(IEnumerable<ActorType> actors, EListMode listMode, bool progressOnly = false)
        {
            if (!Velo.Ingame || Velo.Online)
                return;

            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            Stack stack = Main.game.stack;

            int playerOffset = 0;

            stream.Position = 0;

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
                    ghost.grapple.actor.ghostOwnedItem_ = true;
                    ghost.rope.actor.ghostOwnedItem_ = true;
                }
            }

            foreach (CActor actor in collisionEngine.actors)
            {
                if (actor.ghostOwnedItem_)
                    continue;
                ICActorController controller = actor.Controller;

                if (controller is Player player && !(controller is PlayerBot))
                {
                    if (!player.slot.LocalPlayer && !player.slot.IsBot)
                        continue;
                    if (!include[ATPlayer.Id]) continue;
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
                    stream.Write(ATPlayerBot.Id);
                    stream.Write<int>(playerBot.slot.Index);
                    ATPlayerBot.Write(stream, playerBot);
                } 
                else
                {
                    foreach (ActorType type in ActorTypes)
                    {
                        if (controller.GetType() == type.Type)
                        {
                            if (!include[type.Id]) continue;
                            stream.Write(type.Id);
                            type.Write(stream, controller);
                            break;
                        }
                    }
                }
            }

            stream.Write(-1);

            foreach (var module in stack.modules)
            {
                if (module is ModuleSolo moduleSolo)
                    Write(stream, moduleSolo, progressOnly);
                if (module is ModuleMP moduleMP)
                    Write(stream, moduleMP);
            }

            stream.Write(Math.Min(LoadedVeloVersion, Version.VERSION));
            stream.Write(Velo.Poisoned);

            var cooldowns = RecordingAndReplay.Instance.CurrentNormalRecording?.Rules?.Cooldowns;
            if (cooldowns != null)
            {
                stream.Write(cooldowns.Count);
                foreach (var entry in cooldowns)
                {
                    stream.Write((int)entry.Key);
                    stream.Write(entry.Value.Value);
                }
            }
            else
                stream.Write(0);

            stream.Write(playerOffset);
            stream.SetLength(stream.Position);
        }

        private CActor GetOfType(Type type, int n, Func<CActor, bool> func = null)
        {
            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            
            int c = 0;
            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                if (actor.Controller.GetType() == type && !actor.ghostOwnedItem_ && (func == null || func(actor)))
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
                if (actor.controller.GetType() == type && !actor.ghostOwnedItem_)
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
                    actor.ghostOwnedItem_ = true;
            }
            else if (controller is DroppedObstacle droppedObstacle)
            {
                if (droppedObstacle.owner != null && !droppedObstacle.owner.slot.LocalPlayer && !droppedObstacle.owner.slot.IsBot)
                    actor.ghostOwnedItem_ = true;
            }
            else if (controller is Rocket rocket)
            {
                if (rocket.owner != null && !rocket.owner.slot.LocalPlayer && !rocket.owner.slot.IsBot)
                    actor.ghostOwnedItem_ = true;
            }
            else if (controller is GoldenHook goldenHook)
            {
                if (goldenHook.owner != null && !goldenHook.owner.slot.LocalPlayer && !goldenHook.owner.slot.IsBot)
                    actor.ghostOwnedItem_ = true;
            }
            else if (controller is Shockwave shockwave)
            {
                if (shockwave.owner != null && !shockwave.owner.slot.LocalPlayer && !shockwave.owner.slot.IsBot)
                    actor.ghostOwnedItem_ = true;
            }
            else if (controller is DroppedBomb droppedBomb)
            {
                if (droppedBomb.owner != null && !droppedBomb.owner.slot.LocalPlayer && !droppedBomb.owner.slot.IsBot)
                    actor.ghostOwnedItem_ = true;
            }
            else if (controller is FreezeRay freezeRay)
            {
                if (freezeRay.owner != null && !freezeRay.owner.slot.LocalPlayer && !freezeRay.owner.slot.IsBot)
                    actor.ghostOwnedItem_ = true;
            }
        }
#pragma warning restore IDE1006

        public class ResultFlags
        {
            public bool progressOnly;
        }

        public bool Load(bool setGlobalTime, int ghostIndex = -1, ResultFlags resultFlags = null)
        {
            if (!Velo.Ingame || Velo.Online)
                return false;

            Savestate.ghostIndex = ghostIndex;

            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;
            CWorld world = CEngine.CEngine.Instance.World;
            Stack stack = Main.game.stack;

            stream.Position = 0;

            version = stream.Read<int>();
            if (version == -1 || version > VERSION)
                return false;

            int prevIndex = (int)stream.Position;
            stream.Position = stream.Length - sizeof(int);
            int playerOffset = stream.Read<int>();
            stream.Position = prevIndex;

            if (version == 1)
            {
                int solo = stream.Read<int>();
                if (solo == 1 != (Velo.ModuleSolo != null))
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

            include.Fill(false);
            bool[] readIncludes = stream.ReadBoolArr();
            Array.Copy(readIncludes, include, readIncludes.Length);
            if (ghostIndex != -1)
            {
                stream.Position = playerOffset;
                int ghostId = Ghosts.Instance.Get(ghostIndex).actor.id;
                ATPlayer.Read(stream, Ghosts.Instance.Get(ghostIndex).actor.Controller);
                Ghosts.Instance.Get(ghostIndex).actor.id = ghostId;
                goto cleanup2;
            }

            int[] counts = new int[ActorTypes.Count];

            foreach (Player ghost in Ghosts.Instance.All())
            {
                if (ghost != null && ghost.grapple != null && ghost.rope != null)
                {
                    ghost.grapple.actor.ghostOwnedItem_ = true;
                    ghost.rope.actor.ghostOwnedItem_ = true;
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
                    foreach (ActorType type in ActorTypes)
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
                                if (type.Id == ATPickup.Id && Origins.Instance.IsOrigins())
                                    def = new PickupDef(Vector2.Zero, null, false, false, false, false);

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

            foreach (ActorType type in ActorTypes)
            {
                if (type.CreateDef() != null && include[type.Id])
                    DestroyAllAfter(type.Type, counts[type.Id]);
            }

            bool progressOnly = false;
            foreach (var module in stack.modules)
            {
                if (module is ModuleSolo moduleSolo)
                    Read(stream, moduleSolo, ref progressOnly);
                if (module is ModuleMP moduleMP)
                    Read(stream, moduleMP);
            }
            if (resultFlags != null)
                resultFlags.progressOnly = progressOnly;

            if (version < 4)
            {
                stream.Read<float>();
                stream.Read<float>();
                stream.Read<float>();
                if (version >= 3)
                    stream.Read<float>();
            }

            if (version >= 4)
                LoadedVeloVersion = stream.Read<ushort>();
            else
                LoadedVeloVersion = 34;

            if (version >= 7)
                Velo.Poisoned = stream.Read<bool>();

            if (version >= 4)
            {
                var cooldowns = RecordingAndReplay.Instance.CurrentNormalRecording?.Rules?.Cooldowns;
                int count = stream.Read<int>();
                for (int i = 0; i < count; i++)
                {
                    EViolation violation = (EViolation)stream.Read<int>();
                    float value = stream.Read<float>();
                    if (cooldowns != null)
                        cooldowns[violation].Value = value;
                }
            }

            CheatEngineDetection.MatchValues();

        cleanup:
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
            world.nextActorId = fixedIds.Count > 0 ? 
                Math.Max(nextId, fixedIds.Max()) : nextId;

        cleanup2:
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

        public void Compressed(Savestate reference, Stream destination)
        {
            Stream compressed = destination;
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
                    while (count < 32768 && stream.Position < stream.Length && reference.stream.Position < reference.stream.Length)
                    {
                        if (stream.ReadByte() != reference.stream.ReadByte())
                        {
                            stream.Position--;
                            reference.stream.Position--;
                            break;
                        }
                        count++;
                    }
                    if (count < 128)
                    {
                        compressed.WriteByte((byte)count);
                    }
                    else
                    {
                        compressed.WriteByte((byte)((count >> 8) | 0x80));
                        compressed.WriteByte((byte)(count & 0xFF));
                    }
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
            compressed.SetLength(compressed.Position);
        }

        public void Decompressed(Savestate reference, Stream destination)
        {
            Stream uncompressed = destination;
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
                    if ((count & 0x80) != 0)
                    {
                        count &= 0x7F;
                        count <<= 8;
                        count |= stream.ReadByte();
                    }
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
            uncompressed.SetLength(uncompressed.Position);
        }
    }

    public class SavestateStack
    {
        public struct Slot
        {
            public Savestate Savestate;
            public int UncompressedSize;
            public int KeySavestate;
            public int LossSum;
        }

        private List<Slot> savestates = new List<Slot>();
        public int Length => savestates.Count;

        public SavestateStack()
        {

        }

        private void GetUncompressed(int i, Stream target)
        {
            Slot slot = savestates[i];
            if (slot.KeySavestate != i)
            {
                slot.Savestate.Decompressed(savestates[slot.KeySavestate].Savestate, target);
            }
            else
            {
                slot.Savestate.Stream.Position = 0;
                slot.Savestate.Stream.CopyTo(target);
            }
        }

        public void Set(int i, Savestate savestate)
        {
            if (i == 0)
            {
                savestates.Clear();
                savestates.Add(new Slot()
                {
                    Savestate = savestate.Clone(),
                    UncompressedSize = (int)savestate.Stream.Length,
                    KeySavestate = 0,
                    LossSum = 0
                });
                return;
            }

            Slot newSavestate;
            int keySavestate = savestates[i - 1].KeySavestate;
            int currentLoss = savestates[i - 1].LossSum;
            if (
                savestates[i - 1].UncompressedSize == savestate.Stream.Length &&
                currentLoss < savestate.Stream.Length * 16
            )
            {
                Savestate compressed = new Savestate();
                compressed.Stream.Position = 0;
                savestate.Compressed(savestates[keySavestate].Savestate, compressed.Stream);
                int loss = 
                    i > keySavestate + 1 ? 
                    (int)compressed.Stream.Length - (int)savestates[keySavestate + 1].Savestate.Stream.Length : 
                    0;
                newSavestate = new Slot
                {
                    Savestate = compressed,
                    UncompressedSize = (int)savestate.Stream.Length,
                    KeySavestate = keySavestate,
                    LossSum = currentLoss + loss
                };
            }
            else
            {
                newSavestate = new Slot
                {
                    Savestate = savestate.Clone(),
                    UncompressedSize = (int)savestate.Stream.Length,
                    KeySavestate = i
                };
            }

            if (savestates.Count <= i)
                savestates.Add(newSavestate);
            else
                savestates[i] = newSavestate;

            savestates.RemoveRange(i + 1, savestates.Count - i - 1);
        }

        public void Get(int i, Savestate target)
        {
            target.Stream.Position = 0;
            GetUncompressed(i, target.Stream);
            target.Stream.SetLength(savestates[i].UncompressedSize);
        }

        public void Get(int i, Stream target)
        {
            GetUncompressed(i, target);
        }

        public void SetCompressed(int i, Slot slot)
        {
            if (savestates.Count <= i)
                savestates.Add(slot);
            else
                savestates[i] = slot;

            savestates.RemoveRange(i + 1, savestates.Count - i - 1);
        }

        public Slot GetCompressed(int i)
        {
            return savestates[i];
        }

        public void Clear()
        {
            savestates.Clear();
        }

        public SavestateStack ShallowClone()
        {
            return new SavestateStack()
            {
                savestates = savestates.ToList()
            };
        }
    }
}
