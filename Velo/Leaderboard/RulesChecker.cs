using System.Collections.Generic;
using System.Linq;
using CEngine.World.Actor;
using CEngine.World.Collision.Shape;
using System.Text.RegularExpressions;
using CEngine.Graphics.Component;

namespace Velo
{
    // reasons for a run to be counted as "1 Lap" instead of "New Lap"
    public enum E1LapReasons
    {
        NON_RESET, // lap is started by finishing a previous lap (not by pressing the reset key)
        HAS_BOOST, // player has boost upon starting the lap
        HAS_BOOSTACOKE, // player has boostacoke upon starting the lap
        GATE_NOT_CLOSED, // a gate was not closed upon starting the lap
        FALL_TILE_BROKEN, // a fall tile was broken upon starting the lap
        MOVING_LASER_NON_MENU_RESET, // player did not start the new lap from the menu on a map with moving lasers
        WALL_RESET_BOOST, // player reset the lap while on a wall and jumped at the beginning to boost themself
        COUNT
    }

    // reasons for a run to be counted as "Skip" instead of non-"Skip"
    public enum ESkipReasons
    {
        SECONDARY_CHECKPOINT_MISSED, // player missed a secondary checkpoint
        COUNT
    }

    // reasons for a run to be invalid
    public enum EViolations
    {
        NON_OFFICIAL_OR_CURATED, // map is neither official nor curated workshop
        ILLEGAL_GAME_OPTION, // illegal game option
        OBSTACLE_BROKEN, // an obstacle was broken upon starting the lap
        ITEM_USED, // player used an item
        DRILL_USED, // player used a drill on previous lap that was still active
        TRIPLE_JUMP_ITEM, // player has triple jump item (hacked)
        ITEM_STILL_ALIVE, // player used an item on a previous lap that is still alive
        HAS_IMPOSSIBLE_BOOSTACOKE, // player has boostacoke on a map that does not provide any
        BOOSTACOKE_MODIFIED, // player has modified their boostacoke (pressing + or -)
        GHOST_BLOCKING, // ghost has blocked a laser
        VELO_MOD, // a Velo mod was used
        PRIMARY_CHECKPOINT_MISSED, // player missed a primary checkpoint
        COUNT
    }

    public readonly struct RectangleF
    {
        private readonly float minX;
        private readonly float minY;
        private readonly float maxX;
        private readonly float maxY;

        public RectangleF(float x, float y, float width, float height)
        {
            minX = x;
            minY = y;
            maxX = minX + width;
            maxY = minY + height;
        }

        public bool Overlaps(RectangleF other)
        {
            return
                minX <= other.maxX &&
                maxX >= other.minX &&
                minY <= other.maxY &&
                maxY >= other.minY;
        }

        public float X { get { return minX; } }
        public float Y { get { return minY; } }
        public float W { get { return maxX - minX; } }
        public float H { get { return maxY - minY; } }
    }

    public class Checkpoints
    {
        public List<RectangleF> Primary;
        public List<RectangleF> Secondary;

        public Checkpoints(List<RectangleF> primary, List<RectangleF> secondary)
        {
            Primary = primary;
            Secondary = secondary;
        }
    }

    public class RulesChecker
    {
        public static readonly string CHECKPOINTS_STR = @"
            0: { (1050, 2080, 100, 600), (9450, 920, 100, 450) }, { (860, 1115, 100, 70) },
            1: { (890, 1370, 100, 380), (5080, 1220, 100, 350), (1730, -10000, 100, 11330) }, { (5900, 1920, 100, 300) },
            2: { (1600, -10000, 100, 11190), (10600, 1290, 200, 100), (880, 1370, 100, 280) }, { },
            3: { (1680, -10000, 100, 11200), (9080, 1450, 400, 100), (880, 980, 100, 250) }, { (7200, 2980, 100, 210) },
            4: { (1900, 3250, 100, 500) }, { (7260, 3680, 100, 200), (7250, -10000, 100, 12880), (2770, 2300, 100, 350), (1410, 3290, 100, 300) },
            5: { (1400, 2000, 100, 200), (840, 1910, 100, 300) }, { (8100, 950, 100, 100) },
            6: { (520, 3530, 100, 300), (7330, -10000, 100, 13520), (590, -10000, 100, 13180) }, { },
            7: { (1015, 4180, 200, 100), (5230, 740, 100, 460), (2040, 4210, 100, 350) }, { },
            8: { (1500, 3510, 100, 350), (1385, 1910, 100, 100) }, { (12120, 1850, 200, 100) },
            9: { (2600, -10000, 100, 11380), (11880, 1370, 500, 100), (1310, 1360, 250, 100) }, { },
            10: { (2950, 1830, 100, 300), (4250, 3160, 100, 200), (2400, 1600, 100, 530) }, { (10250, 1520, 100, 400) },
            11: { (3850, -10000, 100, 11860), (12990, 1920, 320, 100), (3225, 1940, 170, 100) }, { },
            12: { (5400, 2900, 100, 450), (11470, 1480, 100, 150), (4750, 3000, 100, 400) }, { },
            13: { (960, 3920, 100, 800), (7150, 2580, 100, 140), (650, 3300, 100, 600) }, { (11940, 1250, 100, 400), (6160, 3420, 100, 180) },
            14: { (1530, 1390, 100, 800), (14730, 2100, 270, 100), (1160, 930, 100, 450) }, { },
            15: { (480, 3280, 100, 400), (13705, 1900, 200, 100), (780, 2900, 200, 300) }, { },
            16: { (2270, 3680, 100, 400), (9260, 3040, 200, 100), (8970, 900, 350, 100), (1440, 2470, 100, 180), (1230, 3380, 100, 300) }, { },
            17: { (1300, -10000, 100, 11160), (11510, 920, 100, 450), (740, 1465, 1300, 100) }, { },
            18: { (7800, 20, 100, 480), (400, 850, 300, 100), (7880, 1580, 300, 100), (1940, 2060, 100, 600), (8940, 240, 100, 100) }, { },
            19: { (3950, 450, 100, 700), (14260, 2700, 700, 100), (3300, 300, 100, 850) }, { },
            20: { (3500, -10000, 100, 10800), (10700, 1560, 500, 100), (2970, 810, 200, 100) }, { (1140, 3400, 250, 100) },
            21: { (5400, 3160, 100, 320), (13880, 2940, 100, 380), (7440, 1720, 250, 100), (4830, 3160, 100, 350) }, { },
            22: { (3240, 25, 100, 350), (10210, 550, 400, 100), (2560, -10000, 100, 10340) }, { },
            23: { (4050, -10000, 100, 10860), (11380, 2300, 480, 100), (2090, 3810, 180, 100), (2800, 830, 220, 100) }, { (6510, 1510, 540, 100) },
            24: { (2100, 3400, 100, 760), (11580, 1120, 100, 350), (5060, 1170, 550, 100), (7575, 2150, 100, 400), (3520, 330, 100, 450), (3250, 2650, 100, 330), (1350, 3400, 100, 760) }, { },
            25: { (9250, 2900, 100, 750), (12960, 3860, 650, 100), (2750, 1640, 300, 100), (8950, 2120, 600, 100) }, { },
            26: { (8550, 2500, 100, 800), (8200, 600, 100, 450) }, { (1260, 2690, 700, 100), (7250, 2500, 100, 950) },
            27: { (3450, -10000, 100, 11450), (11520, 2030, 580, 100), (2550, -10000, 100, 11450) }, { },
            28: { (2400, 5550, 100, 400), (13100, 3400, 100, 200), (1510, 5660, 100, 320) }, { },
            29: { (6650, 3900, 100, 400), (14190, 2050, 250, 100), (4500, 3700, 100, 550) }, { },
            30: { (3150, 850, 100, 700), (11600, 4500, 100, 400), (3870, 3870, 250, 100), (1960, 920, 100, 430) }, { },
            31: { (8400, 1400, 100, 500), (7830, 2300, 100, 200), (2600, 655, 100, 310), (14200, 1630, 100, 350), (9425, 1940, 500, 100) }, { },
            32: { (3140, 1000, 100, 1050), (12250, 1060, 300, 100), (1550, 1000, 100, 1200) }, { },
            33: { (4380, 1150, 100, 550), (13270, 2500, 100, 500), (2300, 1350, 100, 450) }, { },
            34: { (3900, 2020, 100, 430), (4380, 3700, 100, 400), (12030, 1900, 100, 450), (4850, 1880, 100, 600) }, { },
            35: { (1850, -10000, 100, 10650), (9330, 1200, 100, 450), (5570, 2420, 100, 400), (1300, -10000, 100, 10650) }, { },
            36: { (5950, 3350, 100, 400), (12860, 3400, 100, 400), (3500, 3550, 200, 100), (5300, 3350, 100, 400) }, { },
            37: { (2540, 4450, 100, 600), (11540, 3290, 200, 100), (3920, 2820, 300, 100), (6670, 1300, 100, 550), (1250, 4350, 100, 600) }, { },
            38: { (2300, -10000, 100, 12950), (10200, 4500, 100, 500), (870, 3600, 100, 400), (1640, 2800, 200, 100) }, { },
            39: { (5050, 1500, 100, 700), (11230, 900, 430, 100), (1000, 1280, 650, 100), (4200, 1500, 100, 800) }, { },
            40: { (3110, 1350, 230, 100), (7700, 2550, 100, 400), (1050, 920, 100, 300), (8200, 890, 250, 100), (3040, 500, 100, 500) }, { },
            41: { (3800, -10000, 100, 12850), (12160, 2300, 500, 100), (1070, 2400, 600, 100), (2500, 1400, 100, 900) }, { },
            42: { (10750, 2500, 100, 420), (12900, 4000, 100, 400), (6990, 3600, 400, 100), (5970, 1030, 100, 500), (10000, 2450, 100, 500) }, { },
            43: { (3450, 1500, 100, 600), (13480, 2150, 100, 460), (6320, 3230, 100, 400), (9880, 5100, 100, 350), (4000, 2900, 200, 100), (2520, 1050, 100, 400) }, { },
            44: { (5250, 1650, 100, 400), (2500, 3100, 100, 500), (6900, 3600, 300, 100) }, { (10000, 1750, 100, 600), (6450, 1600, 100, 500) }";

        public static List<Checkpoints> checkpoints = InitCheckpoints(CHECKPOINTS_STR);

        public string[] OneLapReasons = new string[(int)E1LapReasons.COUNT];
        public string[] SkipReasons = new string[(int)ESkipReasons.COUNT];
        public string[] Violations = new string[(int)EViolations.COUNT];

        private byte itemPrev = 0;
        private int mapId = -1;

        private int primaryI = 0;
        private int secondaryI = 0;

        public RulesChecker()
        {

        }

        public RulesChecker Clone()
        {
            return new RulesChecker
            {
                OneLapReasons = (string[])OneLapReasons.Clone(),
                SkipReasons = (string[])SkipReasons.Clone(),
                Violations = (string[])Violations.Clone(),
                itemPrev = itemPrev,
                mapId = mapId,
                primaryI = primaryI,
                secondaryI = secondaryI
            };
        }

        public ECategory GetCategory()
        {
            if (OneLapReasons.Count((s) => s != null) == 0)
            {
                if (SkipReasons.Count((s) => s != null) == 0)
                    return ECategory.NEW_LAP;
                else
                    return ECategory.NEW_LAP_SKIPS;
            }
            else
            {
                if (SkipReasons.Count((s) => s != null) == 0)
                    return ECategory.ONE_LAP;
                else
                    return ECategory.ONE_LAP_SKIPS;
            }
        }

        public bool Valid()
        {
            return Violations.Count((s) => s != null) == 0;
        }

        public void Finish()
        {
            if (mapId != -1)
            {
                if (primaryI < checkpoints[mapId].Primary.Count)
                {
                    Violations[(int)EViolations.PRIMARY_CHECKPOINT_MISSED] = "missed primary checkpoint #" + primaryI;
                }
                if (secondaryI < checkpoints[mapId].Secondary.Count)
                {
                    SkipReasons[(int)ESkipReasons.SECONDARY_CHECKPOINT_MISSED] = "missed secondary checkpoint #" + secondaryI;
                }
            }
        }

        public void Update()
        {
            if (!Velo.Ingame)
                return;

            if (mapId == -1)
                Violations[(int)EViolations.NON_OFFICIAL_OR_CURATED] = "non-official and non-RWS map";
            if (Velo.MainPlayer.gameInfo.options[(int)EGameOptions.SUPER_SPEED_RUNNERS])
                Violations[(int)EViolations.ILLEGAL_GAME_OPTION] = "illegal game option SuperSpeedRunners";
            if (Velo.MainPlayer.gameInfo.options[(int)EGameOptions.SPEED_RAPTURE])
                Violations[(int)EViolations.ILLEGAL_GAME_OPTION] = "illegal game option SpeedRapture";
            if (Velo.MainPlayer.gameInfo.options[(int)EGameOptions.DESTRUCTIBLE_ENVIRONMENT])
                Violations[(int)EViolations.ILLEGAL_GAME_OPTION] = "illegal game option Destructible Environment";

            if (itemPrev != (byte)EItem.NONE && Velo.MainPlayer.itemPressed)
                Violations[(int)EViolations.ITEM_USED] = "item " + itemPrev + " used";
            if (Velo.MainPlayer.using_drill)
                Violations[(int)EViolations.DRILL_USED] = "drill used";
            if (Velo.MainPlayer.item_id == (byte)EItem.TRIPLE_JUMP)
                Violations[(int)EViolations.ITEM_USED] = "triple jump item use";
            if (!Map.HasBoostaCoke(mapId) && Velo.MainPlayer.boostacoke.Value > 0f)
                Violations[(int)EViolations.HAS_IMPOSSIBLE_BOOSTACOKE] = "player has unobtainable boostacoke";
            if (Velo.BoostaCokeModified)
                Violations[(int)EViolations.BOOSTACOKE_MODIFIED] = "boostacoke was modified by + or -";
            if (Velo.GhostLaserCollision)
                Violations[(int)EViolations.GHOST_BLOCKING] = "ghost has blocked a laser";
            if (LocalGameMods.Instance.IsModded() || LocalGameMods.Instance.DtFixed || BlindrunSimulator.Instance.Enabled.Value.Enabled)
                Violations[(int)EViolations.VELO_MOD] = "an illegal Velo mod was used";
                           
            CAABB hitboxAABB = (CAABB)Velo.MainPlayer.actor.Collision;
            RectangleF hitbox = new RectangleF(hitboxAABB.MinX, hitboxAABB.MinY, hitboxAABB.Width, hitboxAABB.Height);

            if (mapId != -1)
            {
                Checkpoints cp = checkpoints[mapId];
                if (primaryI < cp.Primary.Count && cp.Primary[primaryI].Overlaps(hitbox))
                    primaryI++;

                if (secondaryI < cp.Secondary.Count && cp.Secondary[secondaryI].Overlaps(hitbox))
                    secondaryI++;
            }

            itemPrev = Velo.MainPlayer.item_id;
        }

        public void LapStart(bool reset)
        {
            OneLapReasons.Fill(null);
            SkipReasons.Fill(null);
            Violations.Fill(null);
            mapId = Map.GetCurrentMapId();
            primaryI = 0;
            secondaryI = 0;

            CEngine.World.Collision.CCollisionEngine col = CEngine.CEngine.Instance.World.CollisionEngine;
            foreach (CActor actor in col.actors)
            {
                if (actor.Controller is Obstacle)
                {
                    Obstacle obstacle = (Obstacle)actor.Controller;
                    if (obstacle.broken)
                        Violations[(int)EViolations.OBSTACLE_BROKEN] = "broken obstacle at " + obstacle.actor.Position.X + ", " + obstacle.actor.Position.Y;
                }
                else if (actor.Controller is Rocket)
                {
                    Rocket rocket = (Rocket)actor.Controller;
                    if (
                        rocket.actor.IsCollisionActive &&
                        rocket.actor.Position.Y > -100f // let's not be too strict with rockets that just fly endlessly through space
                        )
                        Violations[(int)EViolations.ITEM_STILL_ALIVE] = "rocket still alive at " + rocket.actor.Position.X + ", " + rocket.actor.Position.Y;
                }
                else if (actor.Controller is DroppedObstacle)
                {
                    DroppedObstacle droppedObstacle = (DroppedObstacle)actor.Controller;
                    if (droppedObstacle.actor.IsCollisionActive)
                        Violations[(int)EViolations.ITEM_STILL_ALIVE] = "dropped obstacle still alive at " + droppedObstacle.actor.Position.X + ", " + droppedObstacle.actor.Position.Y;
                }
                else if (actor.Controller is DroppedBomb)
                {
                    DroppedBomb droppedBomb = (DroppedBomb)actor.Controller;
                    if (droppedBomb.actor.IsCollisionActive)
                        Violations[(int)EViolations.ITEM_STILL_ALIVE] = "dropped bomb still alive at " + droppedBomb.actor.Position.X + ", " + droppedBomb.actor.Position.Y;
                }
                else if (actor.Controller is Fireball)
                {
                    Fireball fireball = (Fireball)actor.Controller;
                    if (fireball.actor.IsCollisionActive)
                        Violations[(int)EViolations.ITEM_STILL_ALIVE] = "fireball still alive at " + fireball.actor.Position.X + ", " + fireball.actor.Position.Y;
                }
                else if (actor.Controller is Shockwave)
                {
                    Shockwave shockwave = (Shockwave)actor.Controller;
                    if (shockwave.actor.IsCollisionActive)
                        Violations[(int)EViolations.ITEM_STILL_ALIVE] = "shockwave still alive at " + shockwave.actor.Position.X + ", " + shockwave.actor.Position.Y;
                }
                else if (actor.Controller is SwitchBlock)
                {
                    SwitchBlock gate = (SwitchBlock)actor.Controller;
                    if (gate.rotating || gate.state == gate.onState)
                        OneLapReasons[(int)E1LapReasons.GATE_NOT_CLOSED] = "gate not closed on start at " + gate.actor.Position.X + ", " + gate.actor.Position.Y;
                }
                else if (actor.Controller is FallTile)
                {
                    FallTile fallTile = (FallTile)actor.Controller;
                    if (!fallTile.animSpriteDraw.IsVisible)
                        OneLapReasons[(int)E1LapReasons.FALL_TILE_BROKEN] = "broken fall tile at " + fallTile.actor.Position.X + ", " + fallTile.actor.Position.Y;
                }
            }

            if (!reset)
                OneLapReasons[(int)E1LapReasons.NON_RESET] = "no lap reset";

            if (Velo.MainPlayer.boost != 0.0f)
                OneLapReasons[(int)E1LapReasons.HAS_BOOST] = "boost on start";

            if (Map.HasBoostaCoke(mapId) && Velo.MainPlayer.boostacoke.Value > 0f)
                OneLapReasons[(int)E1LapReasons.HAS_BOOSTACOKE] = "boostacoke on start";

            if (Map.HasMovingLaser(mapId) && Velo.IngamePrev && !LocalGameMods.Instance.ResetLasers.Value)
                OneLapReasons[(int)E1LapReasons.MOVING_LASER_NON_MENU_RESET] = "not starting from countdown on a map with moving lasers";

            if (Velo.MainPlayer.wall_cd > 0f)
                OneLapReasons[(int)E1LapReasons.WALL_RESET_BOOST] = "lap reset whilst on wall";
        }

        private static RectangleF ParseRecF(string str, ref int off)
        {
            off++;
            float x, y, w, h;

            int end = str.IndexOf(',', off);
            string number = str.Substring(off, end - off);
            float.TryParse(number, out x);
            off = end + 1;

            end = str.IndexOf(',', off);
            number = str.Substring(off, end - off);
            float.TryParse(number, out y);
            off = end + 1;

            end = str.IndexOf(',', off);
            number = str.Substring(off, end - off);
            float.TryParse(number, out w);
            off = end + 1;

            end = str.IndexOf(')', off);
            number = str.Substring(off, end - off);
            float.TryParse(number, out h);
            off = end + 1;

            return new RectangleF(x, y, w, h);
        }

        public static List<CRectangleDrawComponent> drawComps = new List<CRectangleDrawComponent>();

        // expected string format:
        // [MapID]: {(x, y, w, h), ...}, {(x, y, w, h), ...},
        // ...
        public static List<Checkpoints> InitCheckpoints(string checkpoints)
        {
            List<Checkpoints> list = new List<Checkpoints>();
            checkpoints = Regex.Replace(checkpoints, "[ \n\r\t]", "");
             
            int i = 0;
            while (i < checkpoints.Length)
            {
                int colon = checkpoints.IndexOf(':', i);
                int mapId;
                int.TryParse(checkpoints.Substring(i, colon - i), out mapId);
                i = colon + 1;

                while (list.Count <= mapId)
                    list.Add(null);

                List<RectangleF> primary = new List<RectangleF>();
                i++;
                if (checkpoints[i++] != '}')
                {
                    i--;
                    do
                    {
                        primary.Add(ParseRecF(checkpoints, ref i));
                    } 
                    while (checkpoints[i++] != '}');
                }
                i++;

                List<RectangleF> secondary = new List<RectangleF>();
                i++;
                if (checkpoints[i++] != '}')
                {
                    i--;
                    do
                    {
                        secondary.Add(ParseRecF(checkpoints, ref i));
                    }
                    while (checkpoints[i++] != '}');
                }
                i++;

                list[mapId] = new Checkpoints(primary, secondary);
            }

            /*foreach (CRectangleDrawComponent drawComp in drawComps)
            {
                CEngine.CEngine.Instance.LayerManager.RemoveDrawer(drawComp);
            }

            int curMapId = Map.GetCurrentMapId();
            if (curMapId != -1)
            {
                Checkpoints current = list[curMapId];
                for (int j = 0; j < current.Primary.Count; j++)
                {
                    CRectangleDrawComponent drawComp = new CRectangleDrawComponent(
                        current.Primary[j].X,
                        current.Primary[j].Y,
                        current.Primary[j].W,
                        current.Primary[j].H
                        )
                    {
                        IsVisible = true,
                        FillEnabled = true,
                        FillColor = Color.Red * 0.5f,
                        OutlineEnabled = false,
                        OutlineThickness = 0
                    };
                    drawComp.UpdateBounds();
                    CEngine.CEngine.Instance.LayerManager.AddDrawer("LocalPlayersLayer", drawComp);
                    drawComps.Add(drawComp);
                }
                for (int j = 0; j < current.Secondary.Count; j++)
                {
                    CRectangleDrawComponent drawComp = new CRectangleDrawComponent(
                        current.Secondary[j].X,
                        current.Secondary[j].Y,
                        current.Secondary[j].W,
                        current.Secondary[j].H
                        )
                    {
                        IsVisible = true,
                        FillEnabled = true,
                        FillColor = Color.Blue * 0.5f,
                        OutlineEnabled = false,
                        OutlineThickness = 0
                    };
                    drawComp.UpdateBounds();
                    CEngine.CEngine.Instance.LayerManager.AddDrawer("LocalPlayersLayer", drawComp);
                    drawComps.Add(drawComp);
                }
            }*/

            return list;
        }
    }
}
