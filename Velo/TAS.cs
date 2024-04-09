using System;
using System.Windows.Forms;

namespace Velo
{
    public class TAS : Module
    {
        public IntSetting DeltaTime;
        public BoolSetting FixDeltaTime;
        public HotkeySetting FreezeKey;
        public HotkeySetting Step1Key;
        public HotkeySetting Step10Key;

        public bool frozen = false;
        public bool dtFixed = false;
        public long delta = 33333;
        public long time = 0;
        public int stepCount = 0;

        private TAS() : base("TAS")
        {
            NewCategory("delta time");
            FixDeltaTime = AddBool("fix delta time", false);
            DeltaTime = AddInt("delta time", 33333, 0, 666666);

            CurrentCategory.Tooltip =
                "The delta time is the measured time difference between subsequent frames. " +
                "This value is used for physics calculations to determine for example how far " +
                "to move the player on the current frame. " +
                "As the delta times depends on measurements, " +
                "they are highly inconsistent and lead to indeterminism in the game's physics.";
            FixDeltaTime.Tooltip =
                "Fixes the delta time to a constant value, making the game's physics 100% deterministic " +
                "(excluding randomized events like item pickups and bot behavior).";

            NewCategory("hotkeys");
            FreezeKey = AddHotkey("freeze key", (ushort)Keys.Pause);
            Step1Key = AddHotkey("step 1 key", (ushort)Keys.NumPad2);
            Step10Key = AddHotkey("step 10 key", (ushort)Keys.NumPad3);

            FreezeKey.Tooltip =
                "Toggles the game between a frozen and unfrozen state. " +
                "When frozen, the game stops doing any physics updates.";
            Step1Key.Tooltip =
                "Steps 1 frame forward. Automatically freezes the game.";
            Step10Key.Tooltip =
                "Steps 10 frames forward. Automatically freezes the game.";
        }

        public static TAS Instance = new TAS();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Keyboard.Pressed[FreezeKey.Value])
            {
                if (!frozen)
                    Freeze();
                else
                    Unfreeze();
            }

            if (Keyboard.Pressed[Step1Key.Value])
                stepCount = 1;

            if (Keyboard.Pressed[Step10Key.Value])
                stepCount = 10;

            if (!Velo.Ingame || Velo.Online)
            {
                frozen = false;
                dtFixed = false;
                stepCount = 0;
            }

            if (stepCount > 0)
            {
                dtFixed = true;
                delta = DeltaTime.Value;
                frozen = false;
            }
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            if (stepCount > 0)
            {
                stepCount--;
                if (stepCount == 0)
                    Freeze();
            }
        }

        public void Freeze()
        {
            frozen = true;
            delta = 0;
            dtFixed = true;
        }

        public void Unfreeze()
        {
            frozen = false;
            delta = DeltaTime.Value;
            dtFixed = FixDeltaTime.Value;
            if (dtFixed == false)
                CEngine.CEngine.Instance.lastMeasuredTime = new TimeSpan(DateTime.UtcNow.Ticks);
        }
    }
}
