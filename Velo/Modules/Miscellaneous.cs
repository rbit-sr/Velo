using CEngine.Content;
using System;
using System.IO;
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

    public class Miscellaneous : Module
    {
        public EnumSetting<EEvent> Event;
        public BoolSetting BypassPumpkinCosmo;
        public BoolSetting BypassXl;
        public BoolSetting BypassExcel;

        public HotkeySetting ReloadKey;
        public StringListSetting Contents;

        public BoolSetting DisableGhostGrappleSound;
        public BoolSetting DisableRemoteGrappleSound;

        public bool contentsReloaded = false;

        private Miscellaneous() : base("Miscellaneous")
        {
            NewCategory("event bypass");
            Event = AddEnum("event", EEvent.DEFAULT,
                Enum.GetValues(typeof(EEvent)).Cast<EEvent>().Select(_event => _event.Label()).ToArray());

            Event.Tooltip =
                "event (Requires a restart.)";

            BypassPumpkinCosmo = AddBool("Pumpkin Cosmo", false);
            BypassXl = AddBool("XL", false);
            BypassExcel = AddBool("Excel", false);

            NewCategory("hot reload");
            ReloadKey = AddHotkey("reload key", 0x97);
            Contents = AddStringList("contents", new string[] { "Speedrunner", "Moonraker", "Cosmonaut" });

            CurrentCategory.Tooltip =
                "Allows you to reload sprite and sound files while ingame.";

            Contents.Tooltip =
                "Provide a list of content files or directories you want to reload. " +
                "For characters, you can just use the folder name in the \"Characters\" folder." +
                "Otherwise, use the file's path, starting in the \"Content\" folder, " +
                "like \"UI\\MultiplayerHUD\\PowerUp\".\n" +
                "Refreshing some specific contents may crash the game.";

            NewCategory("sounds");
            DisableGhostGrappleSound = AddBool("disable ghost grapple", false);
            DisableRemoteGrappleSound = AddBool("disable remote grapple", false);

            DisableGhostGrappleSound.Tooltip = "Disable ghost players' grapple sounds.";
            DisableRemoteGrappleSound.Tooltip = "Disable remote players' grapple sounds.";
        }

        public static Miscellaneous Instance = new Miscellaneous();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (ReloadKey.Pressed())
                ReloadContents();
        }

        public override void PostRender()
        {
            base.PostRender();

            contentsReloaded = false;
        }

        private void ReloadContents()
        {
            contentsReloaded = true;

            foreach (string content in Contents.Value)
            {
                ReloadContent(content);
            }
        }

        private void ReloadContent(string id)
        {
            if (id == "")
                return;
            if (id.EndsWith(".xnb"))
                id = id.Replace(".xnb", "");
            if (id.StartsWith("Content\\"))
                id = id.Replace("Content\\", "");

            if (!Directory.Exists("Content\\" + id) && !File.Exists("Content\\" + id + ".xnb"))
            {
                ReloadContent("Content\\Characters\\" + id);
                return;
            }

            if (Directory.Exists("Content\\" + id))
            {
                string[] contents = Directory.GetFiles("Content\\" + id);
                foreach (string content in contents)
                    ReloadContent(content);
                contents = Directory.GetDirectories("Content\\" + id);
                foreach (string content in contents)
                    ReloadContent(content);
                return;
            }

            foreach (var entry in Velo.ContentManager.dict)
            {
                if (entry.Value == null)
                    continue;
                for (int i = 0; i < entry.Value.ContentCount; i++)
                {
                    ICContent content = entry.Value.GetContent(i);
                    if (content == null || content.Name == null)
                        continue;
                    if (content.Name.ToLower() == id.ToLower())
                    {
                        Velo.ContentManager.Release(content);
                        Velo.ContentManager.Load(content, false);
                        return;
                    }
                }
            }
        }

        public bool DisableGrappleSound(Player player)
        {
            if (player == Velo.Ghost)
                return DisableGhostGrappleSound.Value;
            else if (!player.slot.LocalPlayer || player.slot.IsBot)
                return DisableRemoteGrappleSound.Value;
            return false;
        }
    }
}
