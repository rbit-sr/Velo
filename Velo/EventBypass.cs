using System;
using System.Linq;

namespace Velo
{
    public class EventBypass : Module
    {
        public EnumSetting<EEvent> Event;
        public BoolSetting BypassPumpkinCosmo;
        public BoolSetting BypassXl;
        public BoolSetting BypassExcel;

        private EventBypass() : base("Event Bypass")
        {
            NewCategory("event");
            Event = AddEnum("event", EEvent.DEFAULT,
                Enum.GetValues(typeof(EEvent)).Cast<EEvent>().Select(_event => _event.Label()).ToArray());
            NewCategory("character");
            BypassPumpkinCosmo = AddBool("Pumpkin Cosmo", false);
            BypassXl = AddBool("XL", false);
            BypassExcel = AddBool("Excel", false);
            EndCategory();
        }

        public static EventBypass Instance = new EventBypass();
    }
}
