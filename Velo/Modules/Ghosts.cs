using System.Collections.Generic;
using System;
using CEngine.World.Actor;
using CEngine.World.Collision;
using System.Threading.Tasks;

namespace Velo
{
    public class Ghosts : Module
    {
        private readonly List<Player> ghosts = new List<Player> { null };
        private readonly List<TimeSpan> ghostSpawnTimes = new List<TimeSpan> { TimeSpan.Zero };

        private Ghosts() : base("Ghosts")
        {

        }

        public static Ghosts Instance = new Ghosts();

        public override void PostUpdate()
        {
            base.PostUpdate();

            ghosts[0] = GetMainGhost();

            if (!Velo.Ingame)
            {
                while (ghosts.Count >= 2)
                {
                    ghosts.RemoveAt(1);
                    ghostSpawnTimes.RemoveAt(1);
                }
            }
        }

        private Player GetMainGhost()
        {
            if (Velo.ModuleSolo == null)
                return null;
            CCollisionEngine collisionEngine = Velo.CEngineInst.World.CollisionEngine;

            for (int i = 0; i < collisionEngine.ActorCount; i++)
            {
                CActor actor = collisionEngine.GetActor(i);
                ICActorController controller = actor.controller;
                if (controller is Player)
                {
                    Player player = controller as Player;
                    if (!player.slot.LocalPlayer && player.charId == 0)
                        return player;
                }
            }

            return null;
        }

        public Player Get(int index)
        {
            if (index < ghosts.Count)
                return ghosts[index];
            return null;
        }

        public IEnumerable<Player> All()
        {
            return ghosts;
        }

        public Player GetOrSpawn(int index, bool differentColor)
        {
            if (!Velo.Ingame)
                return null;

            while (index >= ghosts.Count)
            {
                ghosts.Add(null);
                ghostSpawnTimes.Add(TimeSpan.Zero);
            }

            if (ghosts[index] != null)
                return ghosts[index];

            if (index == 0)
            {
                Velo.ModuleSolo.SpawnGhost();
                ghosts[0] = Velo.ModuleSolo.gameInfo.slots[1].Player;
                ghostSpawnTimes[0] = Velo.Time;
                return ghosts[0];
            }
            else
            {
                byte skinId = differentColor ? Characters.Instance.GetNextSkinId(Velo.MainPlayer.slot.charId, ghosts[index - 1].slot.SkinId, true, false) : ghosts[index - 1].slot.SkinId;
                CharacterBase character = (CharacterBase)Characters.Instance.Get(Velo.MainPlayer.slot.charId);
                string contentId = character.GetContentId(skinId);
                Velo.ContentManager.LoadBundle(contentId, false);
                Slot slot = new Slot(Main.game.stack.gameInfo, 1, Main.game.stack, false)
                {
                    CharId = Velo.MainPlayer.slot.charId,
                    SkinId = skinId,
                    unknown2 = true,
                    unknown3 = 254
                };
                PlayerDef ghostDef = new PlayerDef(Velo.MainPlayer.actor.Position, Main.game.stack, slot, 6, false);
                Player ghost = (Player)Velo.CEngineInst.World.SpawnActor(ghostDef);
                slot.l4yokIuZOlldyR5VFwaPFbo(ghost, false);
                ghost.charId = (byte)index;
                ghosts[index] = ghost;
                ghostSpawnTimes[index] = Velo.Time;
                return ghosts[index];
            }
        }

        // waits until the specified ghost object has reached a lifetime of at least 1 second
        // this is to ensure that the addresses are more stable and to minimize the chance of crashing
        public void WaitForGhost(int index)
        {
            TimeSpan age = Velo.Time - ghostSpawnTimes[index];
            TimeSpan waitTime = TimeSpan.Zero;
            if (age < TimeSpan.FromSeconds(1))
                waitTime = TimeSpan.FromSeconds(1) - age;
            if (waitTime > TimeSpan.Zero)
                Task.Delay(waitTime).Wait();
        }

        public void RemoveAll()
        {
            while (ghosts.Count >= 2)
            {
                ghosts[1].Destroy();
                ghosts.RemoveAt(1);
                ghostSpawnTimes.RemoveAt(1);
            }
            if (Velo.ModuleSolo == null)
                return;
            
            ghosts[0] = null;
            ghostSpawnTimes[0] = TimeSpan.Zero;
        }
    }
}
