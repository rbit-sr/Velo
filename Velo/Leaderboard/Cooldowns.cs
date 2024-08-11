namespace Velo
{
    public class Cooldowns : Module
    {
        public float ItemCooldown = 0f;
        public float LastUsedItemId = (byte)EItem.NONE;
        public float DrillCooldown = 0f;
        public float ModCooldown = 0f;
        public float SavestateCooldown = 0f;

        private Cooldowns() : base("Cooldowns")
        {

        }

        public static Cooldowns Instance = new Cooldowns();

        public void Clear()
        {
            ItemCooldown = 0f;
            DrillCooldown = 0f;
            ModCooldown = 0f;
            SavestateCooldown = 0f;
        }

        public void SavestateLoaded()
        {
            SavestateCooldown = 1f;
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (!Velo.IngamePrev && Velo.Ingame)
                Clear();

            float delta = (float)Velo.Delta.TotalSeconds;
            ItemCooldown -= delta;
            DrillCooldown -= delta;
            ModCooldown -= delta;
            SavestateCooldown -= delta;

            if (Velo.MainPlayer != null && Velo.ItemIdPrev != (byte)EItem.NONE && Velo.ItemIdPrev != Velo.MainPlayer.item_id)
            {
                ItemCooldown = 5f;
                if (Velo.ItemIdPrev == (byte)EItem.BOMB || Velo.ItemIdPrev == (byte)EItem.TRIGGER)
                    ItemCooldown = float.PositiveInfinity;
                LastUsedItemId = Velo.ItemIdPrev;
            }
            if (Velo.MainPlayer != null && Velo.MainPlayer.using_drill)
                DrillCooldown = 5f;
            if (LocalGameMods.Instance.IsPlaybackRunning())
                SavestateCooldown = 1f;
            else if (
                LocalGameMods.Instance.IsModded() ||
                LocalGameMods.Instance.DtFixed ||
                BlindrunSimulator.Instance.Enabled.Value.Enabled
                )
                ModCooldown = float.PositiveInfinity;
        }
    }
}
