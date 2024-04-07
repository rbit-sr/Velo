using CEngine.Graphics.Component;
using CEngine.World.Actor;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace Velo
{
    public class ColorsAndAppearance : Module
    {
        public FloatSetting PopupScale;
        public FloatSetting PopupOpacity;
        public VectorSetting PopupOffset; 
        public ColorTransitionSetting PopupColor;
        public BoolSetting LocalOnly;
        public IntSetting GrappleRopeThickness;
        public IntSetting GoldenHookRopeThickness;
        public ColorTransitionSetting GrappleRopeColor;
        public ColorTransitionSetting GrappleRopeBreakColor;
        public ColorTransitionSetting GrappleHookColor;
        public ColorTransitionSetting GrappleHookBreakColor;
        public ColorTransitionSetting GoldenHookRopeColor;
        public ColorTransitionSetting GoldenHookRopeBreakColor;
        public ColorTransitionSetting GoldenHookColor;
        public ColorTransitionSetting GoldenHookBreakColor;
        public ColorTransitionSetting PlayerColor;
        public ColorTransitionSetting BackgroundColor;
        public ColorTransitionSetting WinStarColor;
        public ColorTransitionSetting BubbleColor;
        public ColorTransitionSetting SawColor;
        public ColorTransitionSetting LaserLethalInnerColor;
        public ColorTransitionSetting LaserLethalOuterColor;
        public ColorTransitionSetting LaserLethalParticleColor;
        public ColorTransitionSetting LaserLethalSmokeColor;
        public ColorTransitionSetting LaserNonLethalInnerColor;
        public ColorTransitionSetting LaserNonLethalOuterColor;
        public ColorTransitionSetting LaserNonLethalParticleColor;
        public ColorTransitionSetting LaserNonLethalSmokeColor;
        public ColorTransitionSetting ChatTextColor;
        public ColorTransitionSetting ChatNameColor;
        public ColorTransitionSetting ChatSystemColor;
        public ColorTransitionSetting ChatWriteColor;
        public BoolSetting EnableUIColorReplacements;
        public ColorTransitionSetting UIWhiteColor;
        public ColorTransitionSetting UIGrayColor;
        public ColorTransitionSetting UIBlackColor;
        public ColorTransitionSetting UIYellowColor;
        public ColorTransitionSetting[] UIBlueColors;

        private readonly ConditionalWeakTable<object, ColorTransitionSetting> chatColors = new ConditionalWeakTable<object, ColorTransitionSetting>();
        
        private readonly Dictionary<uint, ColorTransitionSetting> colorReplLookup;

        private readonly ConditionalWeakTable<CTextDrawComponent, ColorReplacement> uiTextRepls = new ConditionalWeakTable<CTextDrawComponent, ColorReplacement>();
        private readonly ConditionalWeakTable<CTextDrawComponent, ColorReplacement> uiTextShadowRepls = new ConditionalWeakTable<CTextDrawComponent, ColorReplacement>();
        private readonly ConditionalWeakTable<CImageDrawComponent, ColorReplacement> uiImageRepls = new ConditionalWeakTable<CImageDrawComponent, ColorReplacement>();
        private readonly ConditionalWeakTable<CSpriteDrawComponent, ColorReplacement> uiSpriteRepls = new ConditionalWeakTable<CSpriteDrawComponent, ColorReplacement>();

        private ColorsAndAppearance() : base("Colors and Appearance")
        {
            NewCategory("popup");
            PopupScale = AddFloat("scale", 1.0f, 0.0f, 10.0f);
            PopupOpacity = AddFloat("opacity", 1.0f, 0.0f, 10.0f);
            PopupOffset = AddVector("offset", new Vector2(7.0f, -60.0f), new Vector2(-500.0f, -500.0f), new Vector2(500.0f, 500.0f));
            PopupColor = AddColorTransition("color", new ColorTransition(Color.Yellow));
            
            NewCategory("grapple");
            LocalOnly = AddBool("local only", false);
            GrappleRopeThickness = AddInt("rope thickness", 1, 0, 10);
            GrappleRopeColor = AddColorTransition("rope color", new ColorTransition(Color.Black));
            GrappleRopeBreakColor = AddColorTransition("rope break color", new ColorTransition(Color.White));
            GrappleHookColor = AddColorTransition("hook color", new ColorTransition(new Color(0, 252, 255)));
            GrappleHookBreakColor = AddColorTransition("hook break color", new ColorTransition(Color.White));

            LocalOnly.Tooltip =
                "whether to only affect grapples of local players or not";

            NewCategory("golden hook");
            GoldenHookRopeThickness = AddInt("rope thickness", 1, 0, 10);
            GoldenHookRopeColor = AddColorTransition("rope color", new ColorTransition(Color.Black));
            GoldenHookRopeBreakColor = AddColorTransition("rope break color", new ColorTransition(Color.White));
            GoldenHookColor = AddColorTransition("hook color", new ColorTransition(new Color(255, 241, 16)));
            GoldenHookBreakColor = AddColorTransition("hook break color", new ColorTransition(Color.White));
            
            NewCategory("lethal laser");
            LaserLethalInnerColor = AddColorTransition("inner color", new ColorTransition(new Color(255, 255, 0)));
            LaserLethalOuterColor = AddColorTransition("outer color", new ColorTransition(new Color(191, 51, 191)));
            LaserLethalParticleColor = AddColorTransition("particle color", new ColorTransition(new Color(255, 69, 255)));
            LaserLethalSmokeColor = AddColorTransition("smoke color", new ColorTransition(new Color(255, 255, 0)));
            
            NewCategory("non lethal laser");
            LaserNonLethalInnerColor = AddColorTransition("inner color", new ColorTransition(new Color(255, 255, 255)));
            LaserNonLethalOuterColor = AddColorTransition("outer color", new ColorTransition(new Color(191, 191, 0)));
            LaserNonLethalParticleColor = AddColorTransition("particle color", new ColorTransition(new Color(255, 255, 255)));
            LaserNonLethalSmokeColor = AddColorTransition("smoke color", new ColorTransition(new Color(255, 255, 150)));
            
            NewCategory("chat");
            ChatTextColor = AddColorTransition("text color", new ColorTransition(Color.White));
            ChatNameColor = AddColorTransition("name color", new ColorTransition(Color.Yellow));
            ChatSystemColor = AddColorTransition("system color", new ColorTransition(Color.Yellow));
            ChatWriteColor = AddColorTransition("write color", new ColorTransition(Color.White));
            
            NewCategory("other");
            PlayerColor = AddColorTransition("player color", new ColorTransition(Color.White));
            BackgroundColor = AddColorTransition("background color", new ColorTransition(Color.White));
            WinStarColor = AddColorTransition("win star color", new ColorTransition(new Color(12, 106, 201)));
            BubbleColor = AddColorTransition("bubble color", new ColorTransition(Color.White));
            SawColor = AddColorTransition("saw color", new ColorTransition(Color.White));
            
            NewCategory("UI color replacements");
            EnableUIColorReplacements = AddBool("enable", false);
            UIWhiteColor = AddColorTransition("UI white", new ColorTransition(Color.White));
            UIGrayColor = AddColorTransition("UI gray", new ColorTransition(Color.Gray));
            UIBlackColor = AddColorTransition("UI black", new ColorTransition(Color.Black));
            UIYellowColor = AddColorTransition("UI yellow", new ColorTransition(Color.Yellow));

            colorReplLookup = new Dictionary<uint, ColorTransitionSetting>
            {
                { Color.White.PackedValue, UIWhiteColor },
                { Color.Gray.PackedValue, UIGrayColor },
                { Color.Black.PackedValue, UIBlackColor },
                { Color.Yellow.PackedValue, UIYellowColor }
            };

            Color[] blueTones = new Color[]
            {
                new Color(16, 18, 75),
                new Color(17, 17, 77),
                new Color(17, 17, 78),
                new Color(29, 56, 114),
                new Color(21, 73, 165),
                new Color(15, 86, 174),
                new Color(40, 116, 145),
                new Color(41, 116, 175),
                new Color(0, 102, 204),
                new Color(12, 106, 201),
                new Color(36, 131, 226),
                new Color(0, 93, 255),
                new Color(0, 102, 255),
                new Color(48, 155, 243),
                new Color(49, 155, 246),
                new Color(51, 135, 255),
                new Color(51, 136, 255),
                new Color(49, 150, 252),
                new Color(49, 151, 255),
                new Color(51, 153, 254),
                new Color(54, 141, 255),
                new Color(48, 150, 251),
                new Color(48, 150, 254),
                new Color(51, 153, 255),
                new Color(35, 186, 254),
                new Color(20, 198, 252),
                new Color(58, 195, 255),
                new Color(59, 195, 255),
                new Color(0, 204, 255),
                new Color(1, 204, 254),
                new Color(101, 204, 255),
                new Color(102, 204, 255),
                new Color(163, 238, 247),
                new Color(167, 242, 251)
            };
            
            UIBlueColors = new ColorTransitionSetting[34];
            for (int i = 0; i < 34; i++)
            {
                UIBlueColors[i] = AddColorTransition("UI blue tone " + i, new ColorTransition(blueTones[i]), true);
                colorReplLookup.Add(blueTones[i].PackedValue, UIBlueColors[i]);
            }

            CurrentCategory.Tooltip =
                "Replaces specific colors from the game's UI. " +
                "To find the right blue tone of a specific UI element, " +
                "refer to the tooltip or make a screenshot and use a color picker.";

            EnableUIColorReplacements.Tooltip =
                "Restart your game for changes to fully take effect. " +
                "Enabling this can lead to a slight loss in FPS.";

            UIBlueColors[0].Tooltip = "level select \"Workshop Levels\" bar and scroll bar";
            UIBlueColors[1].Tooltip = "story level select menu title text shadow";
            UIBlueColors[2].Tooltip = "level select workshop list separator bar and main menu news text background";
            UIBlueColors[3].Tooltip = "level select workshop list background";
            UIBlueColors[4].Tooltip = "main menu text shadow";
            UIBlueColors[5].Tooltip = "level select map description and \"Game options\"";
            UIBlueColors[6].Tooltip = "level select workshop list separator text";
            UIBlueColors[7].Tooltip = "level select \"Workshop Levels\" text";
            UIBlueColors[8].Tooltip = "story chapter select menu text";
            UIBlueColors[9].Tooltip = "chat \"SUPERCHAT\" text and \"SEND\" text";
            UIBlueColors[10].Tooltip = "main menu news image background";
            UIBlueColors[11].Tooltip = "XP Level text";
            UIBlueColors[12].Tooltip = "edit controls menu text";
            UIBlueColors[13].Tooltip = "leaderboard player names, league, points text";
            UIBlueColors[14].Tooltip = "leaderboard \"Rank Progress\", your points and season text";
            UIBlueColors[15].Tooltip = "trail editor menu trail selected background";
            UIBlueColors[16].Tooltip = "story level select menu level description text and level icon background";
            UIBlueColors[17].Tooltip = "XP screen XP event points text";
            UIBlueColors[18].Tooltip = "story villain speech bubble text";
            UIBlueColors[19].Tooltip = "leaderboard \"Back\" text";
            UIBlueColors[20].Tooltip = "main menu news text";
            UIBlueColors[21].Tooltip = "XP Level \"XP\" text";
            UIBlueColors[22].Tooltip = "confirmation popup \"Cancel\" and \"Ok\" text";
            UIBlueColors[23].Tooltip = "main menu text";
            UIBlueColors[24].Tooltip = "story level select menu title text";
            UIBlueColors[25].Tooltip = "trail editor menu trail name";
            UIBlueColors[26].Tooltip = "level select workshop list selected background";
            UIBlueColors[27].Tooltip = "leaderboard \"Friends\", \"My League\" and \"World\" text";
            UIBlueColors[28].Tooltip = "story chapter select difficulty text";
            UIBlueColors[29].Tooltip = "XP screen \"Points!!!\" text and XP event name text";
            UIBlueColors[30].Tooltip = "level select menu level name text and \"Press Y to save this replay!\" text";
            UIBlueColors[31].Tooltip = "level select menu level icon background";
            UIBlueColors[32].Tooltip = "main menu button tooltip text";
            UIBlueColors[33].Tooltip = "level editor info text";

            EndCategory();
        }

        public static ColorsAndAppearance Instance = new ColorsAndAppearance();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (!EnableUIColorReplacements.Value)
                return;

            // disable all color replacements for actor drawers
            var actors = CEngine.CEngine.Instance.World.CollisionEngine.actors;
            foreach (CActor actor in actors)
            {
                ICDrawComponent drawer = actor.Controller.Drawer;
                if (drawer is CSpriteDrawComponent sprite)
                {
                    sprite.color_replace = false;
                }
                if (drawer is CImageDrawComponent image)
                {
                    image.color_replace = false;
                }
                if (drawer is CGroupDrawComponent group)
                {
                    foreach (ICDrawComponent child in group.Children)
                    {
                        if (child is CSpriteDrawComponent sprite1)
                        {
                            sprite1.color_replace = false;
                        }
                        if (child is CImageDrawComponent image1)
                        {
                            image1.color_replace = false;
                        }
                        if (child is CTextDrawComponent text)
                        {
                            text.color_replace = false;
                        }
                    }
                }
            }
        }

        private static bool PopupEnabled()
        {
            return 
                (!Speedometer.Instance.Enabled.Value.Enabled || !Speedometer.Instance.DisablePopup.Value) &&
                (!AngleDisplay.Instance.Enabled.Value.Enabled || !AngleDisplay.Instance.DisablePopup.Value) &&
                (!JumpHoldingDisplay.Instance.Enabled.Value.Enabled || !JumpHoldingDisplay.Instance.DisablePopup.Value);
        }

        public void UpdatePopup(Player player)
        {
            player.popup.Offset = PopupOffset.Value;
            player.popup.Color = PopupColor.Value.Get();
            if (!PopupEnabled())
            {
                player.popup.IsVisible = false;
            }
            // opacity and scale values require a more sophisticated treatment and
            // are done in the Player class itself
        }

        public void UpdateGrappleColor(Grapple grapple)
        {
            if (
                !LocalOnly.Value ||
                (grapple.owner.slot.LocalPlayer && !grapple.owner.slot.IsBot))
            {
                grapple.spriteDrawComp1.Color = GrappleHookColor.Value.Get();
                grapple.animSpriteDrawComp1.Color = GrappleHookBreakColor.Value.Get();
            }
            else
            {
                grapple.spriteDrawComp1.Color = new Color(0, 252, 255);
                grapple.animSpriteDrawComp1.Color = new Color(255, 255, 255);
            }
        }

        public void UpdateGoldenHookColor(GoldenHook goldenHook)
        {
            goldenHook.spriteDraw.Color = GoldenHookColor.Value.Get();
            goldenHook.animSpriteDraw.Color = GoldenHookBreakColor.Value.Get();
        }

        public void UpdateRopeColor(Rope rope)
        {
            if (rope.owner == null)
                return;

            if (rope.target is Grapple)
            {
                if (
                    !LocalOnly.Value ||
                    (rope.owner.slot.LocalPlayer && !rope.owner.slot.IsBot))
                {
                    if (!rope.breaking)
                    {
                        rope.line1.color = GrappleRopeColor.Value.Get();
                        rope.line2.color = rope.line1.color;
                    }
                    else
                    {
                        rope.line1.color = GrappleRopeBreakColor.Value.Get();
                        rope.line2.color = rope.line1.color;
                    }
                    rope.line1.thickness = GrappleRopeThickness.Value;
                    rope.line2.thickness = GrappleRopeThickness.Value + 2;
                }
                else if (!rope.breaking)
                {
                    rope.line1.color = Color.Black;
                    rope.line2.color = Color.Black;
                    rope.line1.thickness = 1;
                    rope.line2.thickness = 3;
                }
                else
                {
                    rope.line1.color = Color.White;
                    rope.line2.color = Color.White;
                    rope.line1.thickness = 1;
                    rope.line2.thickness = 3;
                }
            }
            else if (!rope.breaking)
            {
                rope.line1.color = GoldenHookRopeColor.Value.Get();
                rope.line2.color = rope.line1.color;
                rope.line1.thickness = GoldenHookRopeThickness.Value;
                rope.line2.thickness = GoldenHookRopeThickness.Value + 2;
            }
            else
            {
                rope.line1.color = GoldenHookRopeBreakColor.Value.Get();
                rope.line2.color = rope.line1.color;
                rope.line1.thickness = GoldenHookRopeThickness.Value;
                rope.line2.thickness = GoldenHookRopeThickness.Value + 2;
            }
        }

        public void AddChatComp(object obj, string type)
        {
            chatColors.Remove(obj);

            if (type == "chat_name")
                chatColors.Add(obj, ChatNameColor);
            else if (type == "chat_text")
                chatColors.Add(obj, Instance.ChatTextColor);
            else if (type == "chat_system")
                chatColors.Add(obj, Instance.ChatSystemColor);
            else if (type == "chat_write")
                chatColors.Add(obj, Instance.ChatWriteColor);

            if (obj is CEngine.Util.UI.Widget.CTextWidget text)
                text.draw_comp.color_replace = false;
            if (obj is CEngine.Util.UI.Widget.CEditableTextWidget editText)
                editText.draw_comp.color_replace = false;

        }

        public void UpdateChatColor(object obj)
        {
            if (!chatColors.TryGetValue(obj, out ColorTransitionSetting color) || color == null)
                return;

            if (obj is CEngine.Util.UI.Widget.CTextWidget text)
                text.Color = color.Value.Get();
            if (obj is CEngine.Util.UI.Widget.CEditableTextWidget editText)
                editText.Color = color.Value.Get();
        }

        private class ColorReplacement
        {
            public ColorTransitionSetting color;
            public Color originalColor;
            public float multiplier;

            public ColorReplacement(ColorTransitionSetting color, Color originalColor)
            {
                this.color = color;
                this.originalColor = originalColor;
                multiplier = 1.0f;
            }
        }

        private static Color PreserveAlpha(Color newColor, Color oldColor)
        {
            newColor.A = oldColor.A;
            return newColor;
        }

        public void TextColorUpdated(CTextDrawComponent text)
        {
            if (!text.color_replace || !EnableUIColorReplacements.Value)
                return;

            uiTextRepls.Remove(text);

            uint key = Util.FullAlpha(text.color).PackedValue;
            if (colorReplLookup.ContainsKey(key))
                uiTextRepls.Add(text, new ColorReplacement(colorReplLookup[key], text.color));
        }

        public void TextShadowColorUpdated(CTextDrawComponent text)
        {
            if (!text.color_replace || !EnableUIColorReplacements.Value)
                return;

            uiTextShadowRepls.Remove(text);

            uint key = Util.FullAlpha(text.shadow_color).PackedValue;
            if (colorReplLookup.ContainsKey(key))
                uiTextShadowRepls.Add(text, new ColorReplacement(colorReplLookup[key], text.shadow_color));
        }

        public void ImageColorUpdated(CImageDrawComponent image)
        {
            if (!image.color_replace || !EnableUIColorReplacements.Value)
                return;

            if (uiImageRepls.TryGetValue(image, out ColorReplacement colorReplacement) && colorReplacement != null)
            {
                Color compare = colorReplacement.originalColor * ((float)image.color.A / (float)colorReplacement.originalColor.A);

                if (
                    (int)image.color.R >= (int)compare.R - 1 && (int)image.color.R <= (int)compare.R + 1 &&
                    (int)image.color.G >= (int)compare.G - 1 && (int)image.color.G <= (int)compare.G + 1 &&
                    (int)image.color.B >= (int)compare.B - 1 && (int)image.color.B <= (int)compare.B + 1
                )
                {
                    colorReplacement.multiplier = (float)image.color.A / (float)colorReplacement.originalColor.A;
                    return;
                }
            }

            if (Util.FullAlpha(image.color) == Color.White)
                return;

            if (Util.FullAlpha(image.color) == new Color(128, 128, 128))
                return;

            if (Util.FullAlpha(image.color) == Color.Black)
                return;
            uiImageRepls.Remove(image);

            uint key = Util.FullAlpha(image.color).PackedValue;
            if (colorReplLookup.ContainsKey(key))
                uiImageRepls.Add(image, new ColorReplacement(colorReplLookup[key], image.color));
        }

        public void SpriteColorUpdated(CSpriteDrawComponent sprite)
        {
            if (!sprite.color_replace || !EnableUIColorReplacements.Value)
                return;

            if (uiSpriteRepls.TryGetValue(sprite, out ColorReplacement colorReplacement) && colorReplacement != null)
            {
                Color compare = colorReplacement.originalColor * ((float)sprite.color.A / (float)colorReplacement.originalColor.A);

                if (
                    (int)sprite.color.R >= (int)compare.R - 1 && (int)sprite.color.R <= (int)compare.R + 1 &&
                    (int)sprite.color.G >= (int)compare.G - 1 && (int)sprite.color.G <= (int)compare.G + 1 &&
                    (int)sprite.color.B >= (int)compare.B - 1 && (int)sprite.color.B <= (int)compare.B + 1
                )
                {
                    colorReplacement.multiplier = (float)sprite.color.A / (float)colorReplacement.originalColor.A;
                    return;
                }
            }

            if (Util.FullAlpha(sprite.color) == Color.White)
                return;

            if (Util.FullAlpha(sprite.color) == new Color(128, 128, 128))
                return;

            if (Util.FullAlpha(sprite.color) == Color.Black)
                return;

            uiSpriteRepls.Remove(sprite);

            uint key = Util.FullAlpha(sprite.color).PackedValue;
            if (colorReplLookup.ContainsKey(key))
                uiSpriteRepls.Add(sprite, new ColorReplacement(colorReplLookup[key], sprite.color));
        }

        public void UpdateTextColor(CTextDrawComponent text)
        {
            if (!text.color_replace || !EnableUIColorReplacements.Value)
                return;

            if (uiTextRepls.TryGetValue(text, out ColorReplacement colorReplacement) && colorReplacement != null)
            {
                Color newColor = colorReplacement.color.Value.Get();
                text.color = PreserveAlpha(newColor, text.color);
                text.color *= newColor.A / 255.0f * colorReplacement.multiplier;
            }

            if (uiTextShadowRepls.TryGetValue(text, out colorReplacement) && colorReplacement != null)
            {
                Color newColor = colorReplacement.color.Value.Get();
                text.shadow_color = PreserveAlpha(newColor, text.shadow_color);
                text.shadow_color *= newColor.A / 255.0f * colorReplacement.multiplier;
            }
        }

        public void UpdateImageColor(CImageDrawComponent image)
        {
            if (!image.color_replace || !EnableUIColorReplacements.Value)
                return;

            if (uiImageRepls.TryGetValue(image, out ColorReplacement colorReplacement) && colorReplacement != null)
            {
                Color newColor = colorReplacement.color.Value.Get();
                image.color = PreserveAlpha(newColor, image.color);
                image.color *= newColor.A / 255.0f * colorReplacement.multiplier;
            }
        }

        public void UpdateSpriteColor(CSpriteDrawComponent sprite)
        {
            if (!sprite.color_replace || !EnableUIColorReplacements.Value)
                return;

            if (uiSpriteRepls.TryGetValue(sprite, out ColorReplacement colorReplacement) && colorReplacement != null)
            {
                Color newColor = colorReplacement.color.Value.Get();
                sprite.color = PreserveAlpha(newColor, sprite.color);
                sprite.color *= newColor.A / 255.0f * colorReplacement.multiplier;
            }
        }
    }
}
