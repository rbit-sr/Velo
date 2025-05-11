using System.Collections.Generic;
using System;
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

            if (!Velo.Ingame)
            {
                RemoveAll(destroy: false);
            }
        }

        public void MainGhostSpawned(Player ghost)
        {
            ghosts[0] = ghost;
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
                ghostSpawnTimes[0] = Velo.RealTime;
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
                ghostSpawnTimes[index] = Velo.RealTime;
                return ghosts[index];
            }
        }

        // waits until the specified ghost object has reached a lifetime of at least 1 second
        // this is to ensure that the addresses are more stable and to minimize the chance of crashing
        public void WaitForGhost(int index)
        {
            TimeSpan age = Velo.RealTime - ghostSpawnTimes[index];
            TimeSpan waitTime = TimeSpan.Zero;
            if (age < TimeSpan.FromSeconds(1))
                waitTime = TimeSpan.FromSeconds(1) - age;
            if (waitTime > TimeSpan.Zero)
                Task.Delay(waitTime).Wait();
        }

        public void RemoveAll(bool destroy)
        {
            while (ghosts.Count >= 2)
            {
                if (destroy)
                    ghosts[1].Destroy();
                ghosts.RemoveAt(1);
                ghostSpawnTimes.RemoveAt(1);
            }
            
            ghosts[0] = null;
            ghostSpawnTimes[0] = TimeSpan.Zero;
        }
    }
}
