using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace Velo
{
    public enum EEvent
    {
        DEFAULT, NONE, SRENNUR_DEEPS, SCREAM_RUNNERS, WINTER
    }

    public static class EEventExt
    {
        public static string Label(this EEvent _event)
        {
            switch (_event)
            {
                case EEvent.DEFAULT:
                    return "default";
                case EEvent.NONE:
                    return "none";
                case EEvent.SRENNUR_DEEPS:
                    return "srennuRdeepS";
                case EEvent.SCREAM_RUNNERS:
                    return "ScreamRunners";
                case EEvent.WINTER:
                    return "winter";
                default:
                    return "";
            }
        }

        public static int Id(this EEvent _event)
        {
            switch (_event)
            {
                case EEvent.DEFAULT:
                    return 255;
                case EEvent.NONE:
                    return 0;
                case EEvent.SRENNUR_DEEPS:
                    return 2;
                case EEvent.SCREAM_RUNNERS:
                    return 11;
                case EEvent.WINTER:
                    return 14;
                default:
                    return 255;
            }
        }
    }

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

            Event.Tooltip =
                "event (Requires a restart.)";

            NewCategory("character");
            BypassPumpkinCosmo = AddBool("Pumpkin Cosmo", false);
            BypassXl = AddBool("XL", false);
            BypassExcel = AddBool("Excel", false);
        }

        public static EventBypass Instance = new EventBypass();
    }
}
