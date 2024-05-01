using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CEngine.World.Actor;

namespace Velo
{
    // reasons for a run to be counted as "1 Lap" instead of "New Lap"
    public enum E1LapReasons
    {
        NON_RESET, // lap is started by finishing a previous lap (not by pressing the reset key)
        HAS_BOOST, // player has boost upon starting the lap
        GATE_NOT_CLOSED, // a gate was not closed upon starting the lap
        FALL_TILE_BROKEN, // a fall tile was broken upon starting the lap
        MOVING_LASER_NON_MENU_RESET, // player did not start the new lap from the menu on a map with moving lasers
        WALL_RESET_BOOST // player reset the lap while on a wall and jumped at the beginning to boost themself
    }

    // reasons for a run to be counted as "Skip" instead of non-"Skip"
    public enum ESkipReasons
    {
        SECONDARY_CHECKPOINT_MISSED // player missed a secondary checkpoint
    }

    // reasons for a run to be invalid
    public enum EViolations
    {
        NON_OFFICIAL_OR_CURATED, // map is neither official nor curated workshop
        OBSTACLE_BROKEN, // an obstacle was broken upon starting the lap
        ITEM_USED, // player used an item
        DRILL_USED, // player used a drill on previous lap that was still active
        TRIPLE_JUMP_ITEM, // player has triple jump item (hacked)
        ITEM_STILL_ALIVE, // player used an item on a previous lap that is still alive
        PRIMARY_CHECKPOINT_MISSED // player missed a primary checkpoint
    }

    public enum EItem : byte
    {
        NONE,
        GOLDEN_HOOK,
        BOX,
        DRILL,
        ROCKET,
        BOMB,
        TRIGGER,
        TRIPLE_JUMP,
        TWO_BOXES,
        THREE_BOXES,
        SUNGLASSES,
        ONE_ROCKET,
        TWO_ROCKETS,
        THREE_ROCKETS,
        SHOCKWAVE,
        FIREBALL,
        FREEZE,
        SMILEY
    }

    public class Checkpoints
    {
        public
    }

    public class RulesChecker
    {
        public static List<Checkpoints> checkpoints = new List<Checkpoints>();

        public List<KeyValuePair<E1LapReasons, string>> OneLapReasons = new List<KeyValuePair<E1LapReasons, string>>();
        public List<KeyValuePair<ESkipReasons, string>> SkipReasons = new List<KeyValuePair<ESkipReasons, string>>();
        public List<KeyValuePair<EViolations, string>> Violations = new List<KeyValuePair<EViolations, string>>();

        private bool wasIngame = false;

        public RulesChecker()
        {

        }

        public ECategory GetCategory()
        {
            if (OneLapReasons.Count == 0)
            {
                if (SkipReasons.Count == 0)
                    return ECategory.NEW_LAP;
                else
                    return ECategory.NEW_LAP_SKIPS;
            }
            else
            {
                if (SkipReasons.Count == 0)
                    return ECategory.ONE_LAP;
                else
                    return ECategory.ONE_LAP_SKIPS;
            }
        }

        public bool Valid()
        {
            return Violations.Count == 0;
        }

        public void Update()
        {
            if (Velo.Timer < Velo.TimerPrev || !Velo.Ingame)
            {
                OneLapReasons.Clear();
                SkipReasons.Clear();
                Violations.Clear();
            }

            if (!Velo.Ingame)
                return;

            if (Velo.MainPlayer.itemPressed)
                Violations.Add(new KeyValuePair<EViolations, string>(EViolations.ITEM_USED, "item use"));
            if (Velo.MainPlayer.item_id == (byte)EItem.TRIPLE_JUMP)
                Violations.Add(new KeyValuePair<EViolations, string>(EViolations.ITEM_USED, "triple jump item use"));
            if (Velo.Ingame && !wasIngame && Map.GetCurrentMapId() == -1)
                Violations.Add(new KeyValuePair<EViolations, string>(EViolations.NON_OFFICIAL_OR_CURATED, "non-official and non-RWS map"));
            if (Velo.Timer < Velo.TimerPrev)
            {
                CEngine.World.Collision.CCollisionEngine col = CEngine.CEngine.Instance.World.CollisionEngine;
                foreach (CActor actor in col.actors)
                {
                    if (actor.Controller is Obstacle)
                    {
                        Obstacle obstacle = (Obstacle)actor.Controller;
                        if (obstacle.broken)
                            Violations.Add(new KeyValuePair<EViolations, string>(EViolations.ITEM_USED, "broken obstacle at " + obstacle.actor.Position.X + ", " + obstacle.actor.Position.Y));
                    }
                }
            }
            if (Velo.MainPlayer.using_drill)
                Violations.Add(new KeyValuePair<EViolations, string>(EViolations.DRILL_USED, "drill use"));

            if (Velo.Timer < Velo.TimerPrev)
            {
                CEngine.World.Collision.CCollisionEngine col = CEngine.CEngine.Instance.World.CollisionEngine;
                foreach (CActor actor in col.actors)
                {
                    if (actor.Controller is Rocket)
                    {
                        Rocket rocket = (Rocket)actor.Controller;
                        if (
                            rocket.actor.IsCollisionActive &&
                            rocket.actor.Position.Y > -100f // let's not be too strict with rockets that just fly endlessly through space
                            )
                            Violations.Add(new KeyValuePair<EViolations, string>(EViolations.ITEM_STILL_ALIVE, "rocket still alive at " + rocket.actor.Position.X + ", " + rocket.actor.Position.Y));
                    }
                    else if (actor.Controller is DroppedObstacle)
                    {
                        DroppedObstacle droppedObstacle = (DroppedObstacle)actor.Controller;
                        if (droppedObstacle.actor.IsCollisionActive)
                            Violations.Add(new KeyValuePair<EViolations, string>(EViolations.ITEM_STILL_ALIVE, "dropped obstacle still alive at " + droppedObstacle.actor.Position.X + ", " + droppedObstacle.actor.Position.Y));
                    }
                    else if (actor.Controller is Fireball)
                    {
                        Fireball fireball = (Fireball)actor.Controller;
                        if (fireball.actor.IsCollisionActive)
                            Violations.Add(new KeyValuePair<EViolations, string>(EViolations.ITEM_STILL_ALIVE, "fireball still alive at " + fireball.actor.Position.X + ", " + fireball.actor.Position.Y));
                    }
                    else if (actor.Controller is Shockwave)
                    {
                        Shockwave shockwave = (Shockwave)actor.Controller;
                        if (shockwave.actor.IsCollisionActive)
                            Violations.Add(new KeyValuePair<EViolations, string>(EViolations.ITEM_STILL_ALIVE, "shockwave still alive at " + shockwave.actor.Position.X + ", " + shockwave.actor.Position.Y));
                    }
                }
            }

            if (Velo.Timer < Velo.TimerPrev && !Velo.MainPlayerReset)
                OneLapReasons.Add(new KeyValuePair<E1LapReasons, string>(E1LapReasons.NON_RESET, "no lap reset"));
            if (Velo.Timer < Velo.TimerPrev && Velo.MainPlayer.boost != 0.0f)
                OneLapReasons.Add(new KeyValuePair<E1LapReasons, string>(E1LapReasons.HAS_BOOST, "boost on start"));
            if (Velo.Timer < Velo.TimerPrev)
            {
                CEngine.World.Collision.CCollisionEngine col = CEngine.CEngine.Instance.World.CollisionEngine;
                foreach (CActor actor in col.actors)
                {
                    if (actor.Controller is SwitchBlock)
                    {
                        SwitchBlock gate = (SwitchBlock)actor.Controller;
                        if (gate.rotating || gate.state == gate.onState)
                            OneLapReasons.Add(new KeyValuePair<E1LapReasons, string>(E1LapReasons.GATE_NOT_CLOSED, "gate not closed on start at " + gate.actor.Position.X + ", " + gate.actor.Position.Y));
                    }
                }
            }
            if (Velo.Timer < Velo.TimerPrev)
            {
                CEngine.World.Collision.CCollisionEngine col = CEngine.CEngine.Instance.World.CollisionEngine;
                foreach (CActor actor in col.actors)
                {
                    if (actor.Controller is FallTile)
                    {
                        FallTile fallTile = (FallTile)actor.Controller;
                        if (!fallTile.animSpriteDraw.IsVisible)
                            OneLapReasons.Add(new KeyValuePair<E1LapReasons, string>(E1LapReasons.FALL_TILE_BROKEN, "broken fall tile at " + fallTile.actor.Position.X + ", " + fallTile.actor.Position.Y));
                    }
                }
            }

            wasIngame = Velo.Ingame;
        }

        public RulesChecker Clone()
        {
            RulesChecker clone = new RulesChecker();
            clone.OneLapReasons = OneLapReasons.ToList();
            clone.SkipReasons = SkipReasons.ToList();
            clone.Violations = Violations.ToList();
            return clone;
        }
    }
}
