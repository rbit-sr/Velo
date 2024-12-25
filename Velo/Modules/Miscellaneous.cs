using CEngine.Content;
using CEngine.World.Actor;
using CEngine.World.Collision;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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

    public enum EInput
    {
        NONE, LEFT, RIGHT, JUMP, GRAPPLE, SLIDE, BOOST, ITEM, TAUNT, SWAP_ITEM
    }

    public static class EInputExt
    {
        public static string Label(this EInput input)
        {
            switch (input)
            {
                case EInput.NONE:
                    return "none";
                case EInput.LEFT:
                    return "left";
                case EInput.RIGHT:
                    return "right";
                case EInput.JUMP:
                    return "jump";
                case EInput.GRAPPLE:
                    return "grapple";
                case EInput.SLIDE:
                    return "slide";
                case EInput.BOOST:
                    return "boost";
                case EInput.ITEM:
                    return "item";
                case EInput.TAUNT:
                    return "taunt";
                case EInput.SWAP_ITEM:
                    return "swap item";
                default:
                    return "";
            }
        }
    }

    public class Miscellaneous : Module
    {
        public struct SkinConstants
        {
            public VectorSetting Origin;
            public VectorSetting Position;
            public VectorSetting RopeOffset;
            public VectorSetting SwingOrigin;
            public VectorSetting SwingPosition;
            public VectorSetting SwingOffset;
            public VectorSetting TauntOrigin;
            public VectorSetting TauntPosition;
            public VectorSetting ClimbOrigin;
            public VectorSetting ClimbPosition;
            public VectorSetting Scale;
            public ColorTransitionSetting CharacterSelectBackgroundColor;
        }

        public Dictionary<ICharacter, SkinConstants> SkinConstantsLookup = new Dictionary<ICharacter, SkinConstants>();

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

        public EnumSetting<EInput> LeftButton;
        public EnumSetting<EInput> RightButton;
        public EnumSetting<EInput> MiddleButton;
        public EnumSetting<EInput> X1Button;
        public EnumSetting<EInput> X2Button;
        public BoolSetting OverwriteInputs;

        public BoolSetting EnableLayering;
        public BoolSetting SelectFrontmostObject;
        public BoolSetting SelectFrontmostGraphic;
        public BoolSetting EnableBookcase;
        public BoolSetting EnableDiscoLight;
        public BoolSetting EnableDiscoGlow;
        public BoolSetting EnableLeaves;
        public BoolSetting UseResetBind;
        public ColorSetting HoveredColor;
        public ColorSetting ResizingColor;

        public bool contentsReloaded = false;

        private bool wasItemPressed = false;

        private Miscellaneous() : base("Miscellaneous")
        {
            NewCategory("skin constants");
            foreach (var character in Characters.Instance.characters)
            {
                SettingCategory category = Add(new SettingCategory(this, character.Name));
                SkinConstants constants = new SkinConstants
                {
                    Origin = category.Add(new VectorSetting(this, "origin", character.Origin, Vector2.Zero, Vector2.One * 500f)),
                    Position = category.Add(new VectorSetting(this, "position", character.Position, Vector2.One * -100f, Vector2.One * 100f)),
                    RopeOffset = category.Add(new VectorSetting(this, "rope offset", character.RopeOffset, Vector2.One * -500f, Vector2.One * 500f)),
                    SwingOrigin = category.Add(new VectorSetting(this, "swing origin", character.SwingOrigin, Vector2.Zero, Vector2.One * 500f)),
                    SwingPosition = category.Add(new VectorSetting(this, "swing position", character.SwingPosition, Vector2.One * -100f, Vector2.One * 100f)),
                    SwingOffset = category.Add(new VectorSetting(this, "swing target offset", character.SwingOffset, Vector2.One * -100f, Vector2.One * 100f)),
                    TauntOrigin = category.Add(new VectorSetting(this, "taunt origin", character.TauntOrigin, Vector2.Zero, Vector2.One * 500f)),
                    TauntPosition = category.Add(new VectorSetting(this, "taunt position", character.TauntPosition, Vector2.One * -100f, Vector2.One * 100f)),
                    ClimbOrigin = category.Add(new VectorSetting(this, "climb origin", character.ClimbOrigin, Vector2.Zero, Vector2.One * 500f)),
                    ClimbPosition = category.Add(new VectorSetting(this, "climb position", character.ClimbPosition, Vector2.One * -100f, Vector2.One * 100f)),
                    Scale = category.Add(new VectorSetting(this, "scale", character.Scale, Vector2.One * 0.2f, Vector2.One * 5f)),
                    CharacterSelectBackgroundColor = category.Add(new ColorTransitionSetting(this, "background color", new ColorTransition(character.CharacterSelectBackgroundColor)))
                };
                constants.SwingOffset.Tooltip = "Offset increases based on angle. Gets mirrored depending on direction.";
                constants.CharacterSelectBackgroundColor.Tooltip = "character select menu background color";
                SkinConstantsLookup.Add(character, constants);
            }

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
            Origins.Instance.Enabled = OriginsMenu;

            NewCategory("mouse inputs");
            string[] labels = Enum.GetValues(typeof(EInput)).Cast<EInput>().Select(input => input.Label()).ToArray();
            LeftButton = AddEnum("left button", EInput.NONE, labels);
            RightButton = AddEnum("right button", EInput.NONE, labels);
            MiddleButton = AddEnum("middle button", EInput.NONE, labels);
            X1Button = AddEnum("X1 button", EInput.NONE, labels);
            X2Button = AddEnum("X2 button", EInput.NONE, labels);
            OverwriteInputs = AddBool("overwrite inputs", false);

            NewCategory("level editor");
            EnableLayering = AddBool("enable layering", false);
            SelectFrontmostObject = AddBool("select frontmost object", false);
            SelectFrontmostGraphic = AddBool("select frontmost graphic", false);
            EnableBookcase = AddBool("enable bookcase", false);
            EnableDiscoLight = AddBool("enable disco light", false);
            EnableDiscoGlow = AddBool("enable disco glow", false);
            EnableLeaves = AddBool("enable leaves", false);
            UseResetBind = AddBool("use reset bind", false);
            HoveredColor = AddColor("hovered color", Color.Yellow * 0.4f);
            ResizingColor = AddColor("resizing color", Color.Yellow * 0.8f);

            EnableLayering.Tooltip =
                "Hover over an object and press a number key (0-9) to move that object to the corresponding layer. " +
                "Without holding SHIFT, 0 moves it behind all objects. " +
                "With holding SHIFT, the situation is reversed and 0 moves it in front of all objects.";
            SelectFrontmostObject.Tooltip =
                "When hovering over objects, always select the frontmost one (highest layer / last placed).";
            SelectFrontmostGraphic.Tooltip =
                "When hovering over graphics, always select the frontmost one (highest layer / last placed).";
            EnableBookcase.Tooltip =
                "Allows you to place bookcase objects when using Library theme that are otherwise inaccessible.";
            EnableDiscoLight.Tooltip =
                "Allows you to place disco light objects when using Nightclub theme that are otherwise inaccessible.";
            EnableDiscoGlow.Tooltip =
                "Allows you to place disco glow objects when using Nightclub theme that are otherwise inaccessible.";
            EnableLeaves.Tooltip =
                "Allows you to place leaves that are otherwise inaccessible.";
            UseResetBind.Tooltip =
                "Make your reset bind match the bind you assigned for resetting practice laps instead of R only.";
            HoveredColor.Tooltip =
                "object highlighting color while hovering over it";
            ResizingColor.Tooltip =
                "graphic highlighting color while resizing it";
        }

        public static Miscellaneous Instance = new Miscellaneous();

        public override void Init()
        {
            base.Init();
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

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

            if (Velo.ModuleLevelEditor != null && EnableLayering.Value)
            {
                List<CActor> actors = Velo.CEngineInst.World.CollisionEngine.actors;
                int layer = int.MinValue;
                for (int i = 0; i <= 9; i++)
                {
                    if (Input.IsPressed((ushort)((ushort)Keys.D0 + (ushort)i)))
                        layer = i;
                    if (Input.IsPressed((ushort)(((ushort)Keys.D0 + (ushort)i) | 0x100)))
                        layer = actors.Count - 1 - i;
                }

                if (layer != int.MinValue && Velo.ModuleLevelEditor.hovered != null)
                {
                    layer = Math.Max(0, Math.Min(actors.Count, layer));
                    int index = actors.FindIndex(cactor => cactor == Velo.ModuleLevelEditor.hovered.actor);
                    if (index != -1)
                    {
                        if (layer < index)
                        {
                            for (int i = index; i >= layer + 1; i--)
                            {
                                if (actors[i].controller is EditableActor ea1 && actors[i - 1].controller is EditableActor ea2)
                                    ea1.Swap(ea2);
                                else
                                    Velo.CEngineInst.World.SwapActors(actors[i], actors[i - 1]);
                            }
                        }
                        else if (layer > index)
                        {
                            for (int i = index; i <= layer - 1; i++)
                            {
                                if (actors[i].controller is EditableActor ea1 && actors[i + 1].controller is EditableActor ea2)
                                    ea1.Swap(ea2);
                                else
                                    Velo.CEngineInst.World.SwapActors(actors[i], actors[i + 1]);
                            }
                        }

                        for (int i = 0; i < actors.Count; i++)
                        {
                            if (actors[i].controller is EditableActor ea)
                            {
                                Velo.CEngineInst.LayerManager.RemoveDrawer(ea.rect);
                                Velo.CEngineInst.LayerManager.RemoveDrawer(ea.lines);
                                Velo.CEngineInst.LayerManager.AddDrawer(ea.rect.LayerId, ea.rect);
                                Velo.CEngineInst.LayerManager.AddDrawer(ea.lines.LayerId, ea.lines);
                            }
                        }
                    }
                }
            }

            if (Velo.ModuleLevelEditor != null)
            {
                Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Where(a => !(a is BookcaseDef)).ToArray();
                if (EnableBookcase.Value && Velo.ModuleLevelEditor.levelData?.unknown2 == "StageUniversity" || EnableBookcase.Value && Velo.ModuleLevelEditor.levelData?.unknown2 == "StageMansion")
                    Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Append(new BookcaseDef(Vector2.Zero, 1f, 1, true)).ToArray();

                Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Where(a => !(a is DecoLightDef)).ToArray();
                if (EnableDiscoLight.Value && Velo.ModuleLevelEditor.levelData?.unknown2 == "StageNightclub")
                    Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Append(new DecoLightDef(Vector2.Zero, true)).ToArray();

                Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Where(a => !(a is DecoGlowDef)).ToArray();
                if (EnableDiscoGlow.Value && Velo.ModuleLevelEditor.levelData?.unknown2 == "StageNightclub")
                    Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Append(new DecoGlowDef(Vector2.Zero, true)).ToArray();

                Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Where(a => !(a is LeaveDef)).ToArray();
                if (EnableLeaves.Value)
                    Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Append(new LeaveDef(Vector2.Zero, true)).ToArray();
            }
        }

        public override void PostUpdate()
        {
            base.PostUpdate();

            CCollisionEngine col = CEngine.CEngine.Instance.World.CollisionEngine;
            
            if (DisableSawSound.Value || DisableLaserSound.Value)
            {
                int count = col.actors.Count;
                for (int i = 0; i < count; i++)
                {
                    if (DisableSawSound.Value && col.actors[i].Controller is TriggerSaw saw) 
                    {
                        saw.soundEmitter.Pause();
                    }
                    if (DisableLaserSound.Value && col.actors[i].Controller is Laser laser)
                    {
                        laser.soundEmitter.Pause();
                    }
                }
            }

            if (DisableSawSound.Modified() && !DisableSawSound.Value)
            {
                int count = col.actors.Count;
                for (int i = 0; i < count; i++)
                {
                    if (col.actors[i].Controller is TriggerSaw saw)
                    {
                        saw.soundEmitter.Unpause();
                    }
                }
            }
            if (DisableLaserSound.Modified() && !DisableLaserSound.Value)
            {
                int count = col.actors.Count;
                for (int i = 0; i < count; i++)
                {
                    if (col.actors[i].Controller is Laser laser)
                    {
                        laser.soundEmitter.Unpause();
                    }
                }
            }
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
            if (player.slot.LocalPlayer && !player.slot.IsBot)
                return false;

            if (!Velo.Online)
                return DisableGhostGrappleSound.Value;
            else
                return DisableRemoteGrappleSound.Value;
        }

        private void SetInput(Player player, EInput input, bool pressed)
        {
            bool dummy = false;
            ref bool playerInput = ref dummy;

            bool mirrored = player.slot != null && player.gameInfo.isOption(2);
            switch (input)
            {
                case EInput.LEFT:
                    if (!mirrored)
                        playerInput = ref player.leftPressed;
                    else
                        playerInput = ref player.rightPressed;
                    break;
                case EInput.RIGHT:
                    if (!mirrored)
                        playerInput = ref player.rightPressed;
                    else
                        playerInput = ref player.leftPressed;
                    break;
                case EInput.JUMP:
                    playerInput = ref player.jumpPressed;
                    break;
                case EInput.GRAPPLE:
                    playerInput = ref player.grapplePressed;
                    break;
                case EInput.SLIDE:
                    playerInput = ref player.slidePressed;
                    break;
                case EInput.BOOST:
                    playerInput = ref player.boostPressed;
                    break;
                case EInput.ITEM:
                    if (OverwriteInputs.Value)
                        player.item_p2 = pressed && !wasItemPressed;
                    else
                        player.item_p2 |= pressed && !wasItemPressed;
                    playerInput = ref player.itemPressed;
                    break;
                case EInput.TAUNT:
                    playerInput = ref player.tauntPressed;
                    break;
                case EInput.SWAP_ITEM:
                    playerInput = ref player.swapItemPressed;
                    break;
            }
            if (OverwriteInputs.Value)
                playerInput = pressed;
            else
                playerInput |= pressed;
        }

        public void SetMouseInputsPrepare(Player player)
        {
            wasItemPressed = player.itemPressed;
        }

        public void SetMouseInputs(Player player)
        {
            //if (!Util.IsFocused())
               // return;
            SetInput(player, LeftButton.Value, Input.IsKeyDown((byte)Keys.LButton));
            SetInput(player, RightButton.Value, Input.IsKeyDown((byte)Keys.RButton));
            SetInput(player, MiddleButton.Value, Input.IsKeyDown((byte)Keys.MButton));
            SetInput(player, X1Button.Value, Input.IsKeyDown((byte)Keys.XButton1));
            SetInput(player, X2Button.Value, Input.IsKeyDown((byte)Keys.XButton2));
        }
    }
}
