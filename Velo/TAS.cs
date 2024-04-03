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
            DeltaTime = AddInt("delta time", 33333, 0, 666666);
            FixDeltaTime = AddBool("fix delta time", false);
            FreezeKey = AddHotkey("freeze key", (ushort)Keys.Pause);
            Step1Key = AddHotkey("step 1 key", (ushort)Keys.NumPad2);
            Step10Key = AddHotkey("step 10 key", (ushort)Keys.NumPad3);
        }

        public static TAS Instance = new TAS();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Keyboard.Pressed[FreezeKey.Value])
            {
                if (!frozen)
                    freeze();
                else
                    unfreeze();
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
                    freeze();
            }
        }

        public void freeze()
        {
            frozen = true;
            delta = 0;
            dtFixed = true;
        }

        public void unfreeze()
        {
            frozen = false;
            delta = DeltaTime.Value;
            dtFixed = FixDeltaTime.Value;
            if (dtFixed == false)
                CEngine.CEngine.Instance.lastMeasuredTime = new TimeSpan(DateTime.UtcNow.Ticks);
        }
    }
}
