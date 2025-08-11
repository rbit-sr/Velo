using CEngine.Content;
using CEngine.World.Actor;
using CEngine.World.Collision;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    return 254;
                case EEvent.NONE:
                    return 255;
                case EEvent.SRENNUR_DEEPS:
                    return 2;
                case EEvent.SCREAM_RUNNERS:
                    return 11;
                case EEvent.WINTER:
                    return 14;
                default:
                    return 254;
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

        public Dictionary<KeyValuePair<ICharacter, int>, SkinConstants> SkinConstantsLookup = new Dictionary<KeyValuePair<ICharacter, int>, SkinConstants>();

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
        public IntSetting PlayerNumber;
        public BoolSetting OverwriteInputs;
        public BoolSetting LockCursor;
        public VectorSetting LockCursorPosition;

        public BoolSetting EnableLayering;
        public BoolSetting SelectFrontmostObject;
        public BoolSetting SelectFrontmostGraphic;
        public BoolSetting EnableBookcase;
        public BoolSetting EnableCastleWall;
        public BoolSetting EnableTunnel;
        public BoolSetting EnableDiscoLight;
        public BoolSetting EnableDiscoGlow;
        public BoolSetting EnableLeaves;
        public BoolSetting UseResetBind;
        public ColorSetting HoveredColor;
        public ColorSetting ResizingColor;

        public HotkeySetting GiveSmiley;
        public HotkeySetting GiveSunglasses;
        public HotkeySetting GiveTripleJump;

        public BoolSetting DisableSoloQuickchat;

        public bool contentsReloaded = false;

        private bool wasItemPressed = false;

        private Miscellaneous() : base("Miscellaneous")
        {
            NewCategory("skin constants");
            foreach (var character in Characters.Instance.characters)
            {
                SettingCategory categoryOuter = Add(new SettingCategory(this, character.Name));
                ColorTransitionSetting characterSelectBackgroundColor = new ColorTransitionSetting(this, "background color", new ColorTransition(character.CharacterSelectBackgroundColor));
                for (int i = 0; i < character.VariantCount; i++)
                {
                    SettingCategory categoryInner = categoryOuter.Add(new SettingCategory(this, "variant " + (i + 1)));
                    SkinConstants constants = new SkinConstants
                    {
                        Origin = categoryInner.Add(new VectorSetting(this, "origin", character.Origin, Vector2.Zero, Vector2.One * 500f)),
                        Position = categoryInner.Add(new VectorSetting(this, "position", character.Position, Vector2.One * -100f, Vector2.One * 100f)),
                        RopeOffset = categoryInner.Add(new VectorSetting(this, "rope offset", character.RopeOffset, Vector2.One * -500f, Vector2.One * 500f)),
                        SwingOrigin = categoryInner.Add(new VectorSetting(this, "swing origin", character.SwingOrigin, Vector2.Zero, Vector2.One * 500f)),
                        SwingPosition = categoryInner.Add(new VectorSetting(this, "swing position", character.SwingPosition, Vector2.One * -100f, Vector2.One * 100f)),
                        SwingOffset = categoryInner.Add(new VectorSetting(this, "swing target offset", character.SwingOffset, Vector2.One * -100f, Vector2.One * 100f)),
                        TauntOrigin = categoryInner.Add(new VectorSetting(this, "taunt origin", character.TauntOrigin, Vector2.Zero, Vector2.One * 500f)),
                        TauntPosition = categoryInner.Add(new VectorSetting(this, "taunt position", character.TauntPosition, Vector2.One * -100f, Vector2.One * 100f)),
                        ClimbOrigin = categoryInner.Add(new VectorSetting(this, "climb origin", character.ClimbOrigin, Vector2.Zero, Vector2.One * 500f)),
                        ClimbPosition = categoryInner.Add(new VectorSetting(this, "climb position", character.ClimbPosition, Vector2.One * -100f, Vector2.One * 100f)),
                        Scale = categoryInner.Add(new VectorSetting(this, "scale", character.Scale, Vector2.One * 0.2f, Vector2.One * 5f)),
                        CharacterSelectBackgroundColor = characterSelectBackgroundColor
                    };
                    constants.SwingOffset.Tooltip = "Offset increases based on angle. Gets mirrored depending on direction.";
                    if (i == 0)
                        constants.CharacterSelectBackgroundColor.Tooltip = "character select menu background color";
                    SkinConstantsLookup.Add(new KeyValuePair<ICharacter, int>(character, i), constants);
                }
                categoryOuter.Add(characterSelectBackgroundColor);
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
                "Allows you to play Pumpkin Cosmo even when the ScreamRunners event is not active.";
            BypassXl.Tooltip =
                "Allows you to play XL even on workdays.";
            BypassExcel.Tooltip =
                "Allows you to play Excel even on weekends.";

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
            PlayerNumber = AddInt("player number", 1, 1, 4);
            OverwriteInputs = AddBool("overwrite inputs", false);
            LockCursor = AddBool("lock cursor", false);
            LockCursorPosition = AddVector("lock cursor position", new Vector2(960f, 540f), Vector2.Zero, new Vector2(1920f * 2f, 1080f * 2f));

            PlayerNumber.Tooltip =
                "the player to control with mouse";
            OverwriteInputs.Tooltip =
                "Overwrites the original binds so they become unusable.";
            LockCursor.Tooltip =
                "Locks the mouse cursor to a fixed position while ingame. " +
                "It automatically unlocks when not ingame, entering chat or pausing.";
            LockCursorPosition.Tooltip =
                "the position to lock the mouse cursor at";

            NewCategory("level editor");
            EnableLayering = AddBool("enable layering", false);
            SelectFrontmostObject = AddBool("select frontmost object", false);
            SelectFrontmostGraphic = AddBool("select frontmost graphic", false);
            EnableBookcase = AddBool("enable bookcase", false);
            EnableCastleWall = AddBool("enable castle wall", false);
            EnableTunnel = AddBool("enable tunnel", false);
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
            EnableCastleWall.Tooltip =
                "Allows you to place castle wall objects when using Mansion theme that are otherwise inaccessible.";
            EnableTunnel.Tooltip =
                "Allows you to place tunnel objects when using Metro theme that are otherwise inaccessible.";
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

            NewCategory("items");
            GiveSmiley = AddHotkey("give smiley", 0x97);
            GiveSunglasses = AddHotkey("give sunglasses", 0x97);
            GiveTripleJump = AddHotkey("give triple jump", 0x97);

            NewCategory("quickchat");
            DisableSoloQuickchat = AddBool("disable solo quickchat", false);
        }

        public static Miscellaneous Instance = new Miscellaneous();

        public override void Init()
        {
            base.Init();
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (
                LockCursor.Value && 
                Velo.Ingame && 
                !Velo.PauseMenu && 
                !Main.game.stack.baseModule.chat.Enabled && 
                !Util.MouseInputsDisabled() &&
                Input.Focused &&
                Velo.ModuleLevelEditor == null)
            {
                Rectangle window = Velo.CEngineInst.Game.Window.ClientBounds;
                Vector2 position = new Vector2(window.X, window.Y) + LockCursorPosition.Value;
                if (position.X > window.Right)
                    position.X = window.Right;
                if (position.Y > window.Bottom)
                    position.Y = window.Bottom;
                Cursor.Position = new System.Drawing.Point((int)position.X, (int)position.Y);
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

            if (Velo.ModuleLevelEditor != null && EnableLayering.Value)
            {
                List<CActor> actors = Velo.CEngineInst.World.CollisionEngine.actors;
                int layer = int.MinValue;
                for (int i = 0; i <= 9; i++)
                {
                    if (Input.IsPressed((ushort)((ushort)Keys.D0 + (ushort)i)) && !Util.HotkeysDisabled())
                        layer = i;
                    if (Input.IsPressed((ushort)(((ushort)Keys.D0 + (ushort)i) | 0x100)) && !Util.HotkeysDisabled())
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
                                Velo.CEngineInst.LayerManager.RemoveDrawer(ea.selectionRectangle);
                                Velo.CEngineInst.LayerManager.RemoveDrawer(ea.selectionLines);
                                Velo.CEngineInst.LayerManager.AddDrawer(ea.selectionRectangle.LayerId, ea.selectionRectangle);
                                Velo.CEngineInst.LayerManager.AddDrawer(ea.selectionLines.LayerId, ea.selectionLines);
                            }
                        }
                    }
                }
            }

            if (Velo.ModuleLevelEditor != null)
            {
                Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Where(a => !(a is BookcaseDef)).ToArray();
                if (
                    EnableBookcase.Value && Velo.ModuleLevelEditor.levelData?.unknown2 == "StageUniversity" || 
                    EnableCastleWall.Value && Velo.ModuleLevelEditor.levelData?.unknown2 == "StageMansion"
                )
                    Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Append(new BookcaseDef(Vector2.Zero, 1f, 1, true)).ToArray();

                Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Where(a => !(a is TunnelDef)).ToArray();
                if (EnableTunnel.Value && Velo.ModuleLevelEditor.levelData?.unknown2 == "StageMetro")
                    Velo.ModuleLevelEditor.actorDefs = Velo.ModuleLevelEditor.actorDefs.Append(new TunnelDef(Vector2.Zero, true)).ToArray();

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

            if (!Velo.IsOnline() && Velo.MainPlayer != null)
            {
                if (GiveSmiley.Pressed())
                    Velo.MainPlayer.itemId = (byte)EItem.SMILEY;
                if (GiveSunglasses.Pressed())
                    Velo.MainPlayer.itemId = (byte)EItem.SUNGLASSES;
                if (GiveTripleJump.Pressed())
                {
                    Velo.MainPlayer.itemId = (byte)EItem.TRIPLE_JUMP;
                    Velo.MainPlayer.jumpCount = 3;
                }
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

        public bool ReloadContent(string id, bool tryCharacter = true)
        {
            if (id == "")
                return false;
            if (id.EndsWith(".xnb"))
                id = id.Replace(".xnb", "");
            if (id.StartsWith("Content\\"))
                id = id.Replace("Content\\", "");

            if (tryCharacter && !Directory.Exists("Content\\" + id) && !File.Exists("Content\\" + id + ".xnb"))
            {
                return ReloadContent("Content\\Characters\\" + id, tryCharacter: false);
            }

            if (Directory.Exists("Content\\" + id))
            {
                string[] contents = Directory.GetFiles("Content\\" + id);
                foreach (string content in contents)
                    ReloadContent(content);
                contents = Directory.GetDirectories("Content\\" + id);
                foreach (string content in contents)
                    ReloadContent(content);
                return true;
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
                        return true;
                    }
                }
            }
            return false;
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

        private static readonly bool[] mousePressed = new bool[9];

        public void UpdateMouseInputs()
        {
            mousePressed.Fill(false);
            if (LeftButton.Value != EInput.NONE)
                mousePressed[(int)LeftButton.Value - 1] = Input.IsKeyDown((byte)Keys.LButton);
            if (RightButton.Value != EInput.NONE)
                mousePressed[(int)RightButton.Value - 1] = Input.IsKeyDown((byte)Keys.RButton);
            if (MiddleButton.Value != EInput.NONE)
                mousePressed[(int)MiddleButton.Value - 1] = Input.IsKeyDown((byte)Keys.MButton);
            if (X1Button.Value != EInput.NONE)
                mousePressed[(int)X1Button.Value - 1] = Input.IsKeyDown((byte)Keys.XButton1);
            if (X2Button.Value != EInput.NONE)
                mousePressed[(int)X2Button.Value - 1] = Input.IsKeyDown((byte)Keys.XButton2);
        }

        private void SetInput(Player player, EInput input, bool held, bool pressed)
        {
            bool dummy = false;
            ref bool playerInput = ref dummy;

            bool mirrored = player.slot != null && player.gameInfo.isOption((int)EGameOptions.SRENNUR_DEEPS);
            switch (input)
            {
                case EInput.LEFT:
                    if (!mirrored)
                        playerInput = ref player.leftHeld;
                    else
                        playerInput = ref player.rightHeld;
                    break;
                case EInput.RIGHT:
                    if (!mirrored)
                        playerInput = ref player.rightHeld;
                    else
                        playerInput = ref player.leftHeld;
                    break;
                case EInput.JUMP:
                    playerInput = ref player.jumpHeld;
                    break;
                case EInput.GRAPPLE:
                    playerInput = ref player.grappleHeld;
                    break;
                case EInput.SLIDE:
                    playerInput = ref player.slideHeld;
                    break;
                case EInput.BOOST:
                    playerInput = ref player.boostHeld;
                    break;
                case EInput.ITEM:
                    if (OverwriteInputs.Value)
                        player.itemPressed = pressed && !wasItemPressed;
                    else
                        player.itemPressed |= pressed && !wasItemPressed;
                    playerInput = ref player.itemHeld;
                    break;
                case EInput.TAUNT:
                    playerInput = ref player.tauntHeld;
                    break;
                case EInput.SWAP_ITEM:
                    playerInput = ref player.swapItemHeld;
                    break;
            }
            if (OverwriteInputs.Value)
                playerInput = held;
            else
                playerInput |= held;
        }

        public void SetMouseInputsPrepare(Player player)
        {
            wasItemPressed = player.itemPressed;
        }

        public bool MouseInputsEnabled()
        {
            return
                !LeftButton.IsDefault() ||
                !RightButton.IsDefault() ||
                !MiddleButton.IsDefault() ||
                !X1Button.IsDefault() ||
                !X2Button.IsDefault();
        }

        public bool CheckMouseInputPlayerIndex(Player player)
        {
            Slot[] slots = Main.game.stack.gameInfo.slots;

            int j = 0;
            for (int i = 0; i < 4; i++)
            {
                if (slots[i].Player != null && slots[i].LocalPlayer)
                {
                    if (j == PlayerNumber.Value - 1 && player != slots[i].Player)
                        return false;
                    j++;
                }
            }
            return true;
        }

        public void SetMouseInputs(Player player)
        {
            if (!CheckMouseInputPlayerIndex(player))
                return;

            SetInput(player, LeftButton.Value, Input.IsKeyDown((byte)Keys.LButton), Input.IsPressed((byte)Keys.LButton));
            SetInput(player, RightButton.Value, Input.IsKeyDown((byte)Keys.RButton), Input.IsPressed((byte)Keys.RButton));
            SetInput(player, MiddleButton.Value, Input.IsKeyDown((byte)Keys.MButton), Input.IsPressed((byte)Keys.MButton));
            SetInput(player, X1Button.Value, Input.IsKeyDown((byte)Keys.XButton1), Input.IsPressed((byte)Keys.XButton1));
            SetInput(player, X2Button.Value, Input.IsKeyDown((byte)Keys.XButton2), Input.IsPressed((byte)Keys.XButton2));
        }

        public bool IsJumpPressed(Player player, bool isPressed)
        {
            if (!MouseInputsEnabled())
                return isPressed;

            if (!CheckMouseInputPlayerIndex(player))
                return isPressed;

            UpdateMouseInputs();
            if (OverwriteInputs.Value)
                return mousePressed[(int)EInput.JUMP - 1];
            else
                return isPressed || mousePressed[(int)EInput.JUMP - 1];
        }

        public bool IsSlidePressed(Player player, bool isPressed)
        {
            if (!MouseInputsEnabled())
                return isPressed;

            if (!CheckMouseInputPlayerIndex(player))
                return isPressed;

            UpdateMouseInputs();
            if (OverwriteInputs.Value)
                return mousePressed[(int)EInput.SLIDE - 1];
            else
                return isPressed || mousePressed[(int)EInput.SLIDE - 1];
        }

        public bool IsBoostPressed(Player player, bool isPressed)
        {
            if (!MouseInputsEnabled())
                return isPressed;

            if (!CheckMouseInputPlayerIndex(player))
                return isPressed;

            UpdateMouseInputs();
            if (OverwriteInputs.Value)
                return mousePressed[(int)EInput.BOOST - 1];
            else
                return isPressed || mousePressed[(int)EInput.BOOST - 1];
        }
    }
}
