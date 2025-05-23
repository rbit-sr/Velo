﻿using CEngine.Graphics.Component;
using CEngine.Util.Draw;
using CEngine.World.Actor;
using CEngine.World.Collision;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using CEngine.World.Collision.Shape;

namespace Velo
{
    public class HitboxIndicator : MultiDisplayModule
    {
        public BoolListSetting IdList;
        public ColorTransitionSetting LocalPlayersColor;
        public ColorTransitionSetting RemotePlayersColor;
        public ColorTransitionSetting ItemsColor;
        public ColorTransitionSetting LevelObjectColor;
        public ColorTransitionSetting SuperBoostColor;
        public ColorTransitionSetting TriggerColor;

        // collision ID to IdList index
        private static readonly Dictionary<int, int> colToIndex = new Dictionary<int, int>
        {
            { 100, 0 },     // player
            { 101, 1 },     // hook
            { 102, 2 },     // fall tile
            { 104, 3 },     // saw / spike
            { 105, 4 },     // obstacle
            { 107, 5 },     // boost section
            { 108, 6 },     // super boost
            { 111, 7 },     // trigger
            { 117, 8 },     // item box
            { 119, 9 },     // dropped obstacle
            { 120, 10 },    // gate
            { 122, 11 },    // rocket
            { 123, 12 },    // bomb
            { 131, 13 },    // straight rocket
            { 132, 14 },    // fireball
            { 141, 15 },    // boosta coke
            { 142, 16 },    // bouncepad
            { -1, 17 }      // freeze ray
        };

        private readonly Dictionary<CActor, ICDrawComponent> hitboxes = new Dictionary<CActor, ICDrawComponent>();

        private HitboxIndicator() : base("Hitbox Indicator", true)
        {
            NewCategory("general");
            IdList = AddBoolList("actor list", new[]
                {
                    "player",
                    "hook",
                    "fall tile",
                    "saw / spike",
                    "obstacle",
                    "boost section",
                    "super boost",
                    "trigger",
                    "item box",
                    "dropped obstacle",
                    "gate",
                    "rocket",
                    "bomb",
                    "straight rocket",
                    "fireball",
                    "boosta coke",
                    "bouncepad",
                    "freeze ray"
                },
                new bool[] 
                { 
                    true,   // player
                    false,  // hook
                    false,  // fall tile
                    true,   // saw / spike
                    true,   // obstacle
                    true,   // boost section
                    true,   // super boost
                    false,  // trigger
                    true,   // item box
                    true,   // dropped obstacle
                    true,   // gate
                    false,  // rocket
                    false,  // bomb
                    false,  // straight rocket
                    true,   // fireball
                    false,  // boosta coke
                    true,   // bouncepad
                    false   // freeze ray
                });

            IdList.Tooltip =
                "list of actors to show the hitboxes of";

            NewCategory("color");
            LocalPlayersColor = AddColorTransition("local players", new ColorTransition(new Color(0, 255, 0, 128)));
            RemotePlayersColor = AddColorTransition("remote players", new ColorTransition(new Color(0, 0, 255, 128)));
            ItemsColor = AddColorTransition("items", new ColorTransition(new Color(255, 0, 0, 128)));
            LevelObjectColor = AddColorTransition("level objects", new ColorTransition(new Color(255, 0, 0, 128)));
            SuperBoostColor = AddColorTransition("super boosts", new ColorTransition(new Color(255, 0, 0, 128)));
            TriggerColor = AddColorTransition("triggers", new ColorTransition(new Color(255, 0, 0, 128)));
        }

        public static HitboxIndicator Instance = new HitboxIndicator();

        public override bool FixedPos => false;

        public override void UpdateComponents()
        {
            CCollisionEngine collisionEngine = CEngine.CEngine.Instance.World.CollisionEngine;

            int count = collisionEngine.ActorCount;

            // make a list of all currently added hitboxes and remove them from it
            // in the upcoming loop if visited
            List<CActor> unvisited = new List<CActor>();
            foreach (var pair in hitboxes)
                unvisited.Add(pair.Key);

            for (int i = 0; i < count; i++)
            {
                CActor actor = collisionEngine.actors[i];

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

                ICCollisionShape collision = actor.Collision;

                if (collision is CLineTrace)
                    continue;

                bool useRect = collision is CAABB;

                // boost sections and bounce pads use CConvexPolygons as their collision
                // detect if they are axis-aligned or rotated
                // for rotated boxes or other polygons we draw outlines instead
                if (actor.CollidableType == 107 || actor.CollidableType == 142)
                {
                    CConvexPolygon poly = collision as CConvexPolygon;
                    if (poly.GetVertex(0).X == poly.GetVertex(1).X || poly.GetVertex(0).Y == poly.GetVertex(1).Y)
                        useRect = true;
                }

                if (hitbox != null && hitbox is CRectangleDrawComponent != useRect)
                {
                    hitboxes.Remove(actor);
                    RemoveComponent(hitbox);
                    hitbox = null;
                }

                Color color;
                switch (actor.CollidableType)
                {
                    case 101:
                    case 119:
                    case 122:
                    case 123:
                    case 131:
                    case 132:
                    case -1:
                        color = ItemsColor.Value.Get();
                        break;
                    case 102:
                    case 104:
                    case 105:
                    case 107:
                    case 117:
                    case 120:
                    case 141:
                    case 142:
                        color = LevelObjectColor.Value.Get();
                        break;
                    case 108:
                        color = SuperBoostColor.Value.Get();
                        break;
                    case 111:
                        color = TriggerColor.Value.Get();
                        break;
                    case 100:
                        color = 
                            actor.Controller is Player player && player.slot.LocalPlayer && !player.slot.IsBot ? 
                            LocalPlayersColor.Value.Get() :
                            RemotePlayersColor.Value.Get();
                        break;
                    default:
                        color = Color.Transparent;
                        break;
                }
                    
                if (useRect)
                {
                    CAABB rect = collision is CAABB cAABB ? cAABB : actor.Bounds;

                    if (hitbox == null)
                    {
                        hitbox = new CRectangleDrawComponent(0, 0, 0, 0)
                        {
                            IsVisible = true,
                            FillEnabled = true,
                            OutlineThickness = 0
                        };
                        AddComponent(hitbox);
                        hitboxes.Add(actor, hitbox);
                    }

                    CRectangleDrawComponent rectDraw = hitbox as CRectangleDrawComponent;
                    rectDraw.SetPositionSize(rect.Position, rect.Size);
                    rectDraw.FillColor = color;
                }
                else if (collision is CConvexPolygon)
                {
                    CConvexPolygon poly = collision as CConvexPolygon;
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

                    lineDraw = hitbox as CLineDrawComponent;

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

            // remove unvisited hitboxes
            count = unvisited.Count;
            for (int i = 0; i < count; i++)
            {
                RemoveComponent(hitboxes[unvisited[i]]);
                hitboxes.Remove(unvisited[i]);
            }
        }
    }
}
