using CEngine.Content;
using CEngine.World.Actor;
using CEngine.World.Collision;
using Microsoft.Xna.Framework;
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
        public BoolSetting DisableSawSound;
        public BoolSetting DisableLaserSound;

        public BoolSetting FixDanceHallGate;

        public ToggleSetting OriginsMenu;

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

            BypassPumpkinCosmo.Tooltip =
                "Allows you to play Pumpkin Cosmo even when it's not ScreamRunners.";
            BypassXl.Tooltip =
                "Allows you to play XL even when it's not weekend.";
            BypassExcel.Tooltip =
                "Allows you to play Excel even when it's weekend.";

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
            DisableSawSound = AddBool("disable saw", false);
            DisableLaserSound = AddBool("disable laser", false);

            DisableGhostGrappleSound.Tooltip = "Disable ghost players' grapple sounds.";
            DisableRemoteGrappleSound.Tooltip = "Disable remote players' grapple sounds.";

            NewCategory("bug fixes");
            FixDanceHallGate = AddBool("fix dance hall gate", true);

            FixDanceHallGate.Tooltip =
                "The map Dance Hall has an oversight where players in multiplayer mode can get stuck indefinitely in the top right section right after the booster. " +
                "This fix increases size of the trigger for the gate in the top right section, " +
                "so that you are still able to reopen the gate after respawning right behind it.";

            NewCategory("Origins");
            OriginsMenu = AddToggle("menu", new Toggle());
        }

        public static Miscellaneous Instance = new Miscellaneous();

        public override void Init()
        {
            base.Init();

            Origins.Instance.Init();
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Input.Pressed(OriginsMenu.Value.Hotkey))
            {
                OriginsMenu.ToggleState();
            }

            if (ReloadKey.Pressed())
                ReloadContents();

            if (FixDanceHallGate.Value && !Velo.IngamePrev && Velo.Ingame && Map.GetCurrentMapId() == 22)
            {
                CCollisionEngine col = CEngine.CEngine.Instance.World.CollisionEngine;
                foreach (CActor actor in col.actors)
                {
                    if (actor.Controller is Trigger)
                    {
                        if (actor.Position.X >= 10000f)
                        {
                            actor.Position -= new Vector2(0f, 1000f);
                            actor.Size += new Vector2(0f, 1000f);
                            CEngine.World.Collision.Shape.CAABB rec = (CEngine.World.Collision.Shape.CAABB)actor.Collision;
                            rec.MinY -= 1000f;
                            actor.SetBoundsFromShape(rec);
                            actor.UpdateCollision();
                        }
                    }
                }
            }
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            Origins.Instance.PostUpdate();

            CCollisionEngine col = CEngine.CEngine.Instance.World.CollisionEngine;
            
            if (DisableSawSound.Value || DisableLaserSound.Value)
            {
                foreach (CActor actor in col.actors)
                {
                    if (DisableSawSound.Value && actor.Controller is TriggerSaw saw) 
                    {
                        saw.soundEmitter.Pause();
                    }
                    if (DisableLaserSound.Value && actor.Controller is Laser laser)
                    {
                        laser.soundEmitter.Pause();
                    }
                }
            }

            if (DisableSawSound.Modified() && !DisableSawSound.Value)
            {
                foreach (CActor actor in col.actors)
                {
                    if (actor.Controller is TriggerSaw saw)
                    {
                        saw.soundEmitter.Unpause();
                    }
                }
            }
            if (DisableLaserSound.Modified() && !DisableLaserSound.Value)
            {
                foreach (CActor actor in col.actors)
                {
                    if (actor.Controller is Laser laser)
                    {
                        laser.soundEmitter.Unpause();
                    }
                }
            }
        }

        public override void PostRender()
        {
            base.PostRender();

            Origins.Instance.PostRender();

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
            if (player.slot.LocalPlayer && !player.slot.IsBot)
                return false;

            if (!Velo.Online)
                return DisableGhostGrappleSound.Value;
            else
                return DisableRemoteGrappleSound.Value;
        }
    }
}
