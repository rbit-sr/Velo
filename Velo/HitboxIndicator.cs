using CEngine.Graphics.Component;
using CEngine.Util.Draw;
using CEngine.World.Actor;
using CEngine.World.Collision;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Velo
{
    public class HitboxIndicator : MultiDisplayModule
    {
        public HitboxListSetting IdList;
        public FloatSetting LocalPlayersOpacity;
        public FloatSetting RemotePlayersOpacity;
        public FloatSetting ObjectsOpacity;
        public ColorTransitionSetting LocalPlayersColor;
        public ColorTransitionSetting RemotePlayersColor;
        public ColorTransitionSetting ObjectsColor;

        public Dictionary<int, int> colToIndex = new Dictionary<int, int>
        {
            { 100, 0 },
            { 101, 1 },
            { 102, 2 },
            { 104, 3 },
            { 105, 4 },
            { 107, 5 },
            { 108, 6 },
            { 111, 7 },
            { 117, 8 },
            { 119, 9 },
            { 120, 10 },
            { 122, 11 },
            { 123, 12 },
            { 131, 13 },
            { 132, 14 },
            { 141, 15 },
            { 142, 16 },
            { -1, 17 }
        };

        public Dictionary<CActor, ICDrawComponent> hitboxes = new Dictionary<CActor, ICDrawComponent>();

        private HitboxIndicator() : base("Hitbox Indicator", true)
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F4));

            IdList = AddHitboxList("ID list", new bool[] { true, false, false, true, true, true, true, false, true, true, true, false, false, false, true, false, true, false });
            LocalPlayersOpacity = AddFloat("local players opacity", 0.5f, 0.0f, 1.0f);
            RemotePlayersOpacity = AddFloat("remote players opacity", 0.5f, 0.0f, 1.0f);
            ObjectsOpacity = AddFloat("objects opacity", 0.5f, 0.0f, 1.0f);
            LocalPlayersColor = AddColorTransition("local players color", new ColorTransition(Microsoft.Xna.Framework.Color.Green));
            RemotePlayersColor = AddColorTransition("remote players color", new ColorTransition(Microsoft.Xna.Framework.Color.Blue));
            ObjectsColor = AddColorTransition("objects color", new ColorTransition(Microsoft.Xna.Framework.Color.Red));
        }

        public static HitboxIndicator Instance = new HitboxIndicator();

        public override bool FixedPos()
        {
            return false;
        }

        public override void UpdateComponents()
        {
            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;

            int count = collisionEngine.ActorCount;

            List<CActor> unvisited = new List<CActor>();
            foreach (var pair in hitboxes)
                unvisited.Add(pair.Key);

            for (int i = 0; i < count; i++)
            {
                CActor actor = collisionEngine.GetActor(i);

                if (
                    !colToIndex.ContainsKey(actor.CollidableType) ||
                    !IdList.Value[colToIndex[actor.CollidableType]] ||
                    !actor.IsCollisionActive
                    )
                    continue;

                unvisited.Remove(actor);

                ICDrawComponent hitbox = null;

                if (hitboxes.ContainsKey(actor))
                {
                    hitbox = hitboxes[actor];
                }

                CEngine.World.Collision.Shape.ICCollisionShape collision = actor.Collision;

                if (collision is CEngine.World.Collision.Shape.CLineTrace)
                    continue;

                bool useRect = collision is CEngine.World.Collision.Shape.CAABB;

                if (actor.CollidableType == 107 || actor.CollidableType == 142)
                {
                    CEngine.World.Collision.Shape.CConvexPolygon poly = (CEngine.World.Collision.Shape.CConvexPolygon)collision;
                    if (poly.GetVertex(0).X == poly.GetVertex(1).X || poly.GetVertex(0).Y == poly.GetVertex(1).Y)
                        useRect = true;
                }

                if (hitbox != null && hitbox is CRectangleDrawComponent != useRect)
                {
                    hitboxes.Remove(actor);
                    RemoveComponent(hitbox);
                    hitbox = null;
                }

                Color color = 
                    actor.CollidableType != 100 ? ObjectsColor.Value.Get() : 
                    actor.localPlayer ? LocalPlayersColor.Value.Get() : 
                    RemotePlayersColor.Value.Get();

                float opacity = 
                    actor.CollidableType != 100 ? ObjectsOpacity.Value : 
                    actor.localPlayer ? LocalPlayersOpacity.Value : 
                    RemotePlayersOpacity.Value;

                color = new Color(color.R, color.G, color.B) * opacity;

                if (useRect)
                {
                    CEngine.World.Collision.Shape.CAABB rect = collision is CEngine.World.Collision.Shape.CAABB cAABB ? cAABB : actor.Bounds;

                    if (hitbox == null)
                    {
                        hitbox = new CRectangleDrawComponent(0, 0, 0, 0);
                        AddComponent(hitbox);
                        hitboxes.Add(actor, hitbox);
                    }

                    CRectangleDrawComponent rectDraw = (CRectangleDrawComponent)hitbox;
                    rectDraw.SetPositionSize(rect.Position, rect.Size);
                    rectDraw.IsVisible = true;
                    rectDraw.FillEnabled = true;
                    rectDraw.FillColor = color;
                    rectDraw.OutlineThickness = 0;
                    rectDraw.UpdateBounds();
                }
                else if (collision is CEngine.World.Collision.Shape.CConvexPolygon poly)
                {
                    CLineDrawComponent lineDraw;

                    if (hitbox == null)
                    {
                        lineDraw = new CLineDrawComponent
                        {
                            IsVisible = true
                        };

                        for (int j = poly.VertexCount - 1, k = 0; k < poly.VertexCount; j = k++)
                        {
                            CLine line = new CLine(poly.GetVertex(j), poly.GetVertex(k), color)
                            {
                                thickness = 6
                            };
                            lineDraw.AddLine(line);
                        }

                        hitbox = lineDraw;
                        AddComponent(hitbox);
                        hitboxes.Add(actor, hitbox);
                    }

                    lineDraw = (CLineDrawComponent)hitbox;

                    for (int j = poly.VertexCount - 1, k = 0; k < poly.VertexCount; j = k++)
                    {
                        lineDraw.Lines[k].start = poly.GetVertex(j);
                        lineDraw.Lines[k].end = poly.GetVertex(k);

                        Vector2 edge = lineDraw.Lines[k].end - lineDraw.Lines[k].start;
                        Vector2 normal = new Vector2(edge.Y, -edge.X);
                        normal.Normalize();
                        normal *= 3;
                        lineDraw.Lines[k].start -= normal;
                        lineDraw.Lines[k].end -= normal;
                        lineDraw.Lines[k].color = color;
                    }
                }
            }

            foreach (CActor actor in unvisited)
            {
                RemoveComponent(hitboxes[actor]);
                hitboxes.Remove(actor);
            }
        }
    }
}
