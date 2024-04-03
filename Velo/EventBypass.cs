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
            BypassPumpkinCosmo = AddBool("bypass Pumpkin Cosmo", false);
            BypassXl = AddBool("bypass XL", false);
            BypassExcel = AddBool("bypass Excel", false);
        }

        public static EventBypass Instance = new EventBypass();
    }
}
