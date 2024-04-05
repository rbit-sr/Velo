namespace Velo
{
    public class EventBypass : Module
    {
        public IntSetting EventId;
        public BoolSetting BypassPumpkinCosmo;
        public BoolSetting BypassXl;
        public BoolSetting BypassExcel;

        private EventBypass() : base("Event Bypass")
        {
            EventId = AddInt("event ID", 255, -1, 255);
            NewCategory("bypass");
            BypassPumpkinCosmo = AddBool("Pumpkin Cosmo", false);
            BypassXl = AddBool("XL", false);
            BypassExcel = AddBool("Excel", false);
            EndCategory();
        }

        public static EventBypass Instance = new EventBypass();
    }
}
