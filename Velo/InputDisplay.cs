using System;
using Microsoft.Xna.Framework;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using System.Windows.Forms;
using System.Linq;

namespace Velo
{
    public class InputDisplay : MultiDisplayModule
    {
        public FloatSetting Scale;
        public EnumSetting<EOrientation> Orientation;
        public VectorSetting Offset;
        public StringSetting Font;
        public IntSetting FontSize;
        public FloatSetting Opacity;
        public ColorTransitionSetting PressedBoxColor;
        public ColorTransitionSetting PressedTextColor;
        public ColorTransitionSetting PressedOutlineColor;
        public IntSetting PressedOutlineWidth;
        public ColorTransitionSetting ReleasedBoxColor;
        public ColorTransitionSetting ReleasedTextColor;
        public ColorTransitionSetting ReleasedOutlineColor;
        public IntSetting ReleasedOutlineWidth;
        public InputBoxSetting LeftBox;
        public InputBoxSetting RightBox;
        public InputBoxSetting JumpBox;
        public InputBoxSetting GrappleBox;
        public InputBoxSetting SlideBox;
        public InputBoxSetting BoostBox;
        public InputBoxSetting ItemBox;

        private CFont font;

        private CRectangleDrawComponent[] boxComps;
        private CTextDrawComponent[] textComps;
        
        private InputDisplay() : base("Input Display", true)
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F3));

            NewCategory("style");
            Scale = AddFloat("scale", 1.0f, 0.0f, 10.0f);
            Orientation = AddEnum("orientation", EOrientation.BOTTOM_RIGHT,
                Enum.GetValues(typeof(EOrientation)).Cast<EOrientation>().Select(orientation => orientation.Label()).ToArray());
            Offset = AddVector("offset", new Vector2(-64.0f, -64.0f), new Vector2(-500.0f, -500.0f), new Vector2(500.0f, 500.0f));
            Font = AddString("font", "CEngine\\Debug\\FreeMonoBold.ttf");
            FontSize = AddInt("font size", 18, 1, 100);
            Opacity = AddFloat("opacity", 1.0f, 0.0f, 1.0f);

            NewCategory("pressed");
            PressedBoxColor = AddColorTransition("box color", new ColorTransition(new Color(255, 100, 100, 175)), true);
            PressedTextColor = AddColorTransition("text color", new ColorTransition(Color.Black), true);
            PressedOutlineColor = AddColorTransition("outline color", new ColorTransition(Color.Transparent), true);
            PressedOutlineWidth = AddInt("outline width", 0, 0, 10);
            
            NewCategory("released");
            ReleasedBoxColor = AddColorTransition("box color", new ColorTransition(new Color(160, 160, 160, 175)), true);
            ReleasedTextColor = AddColorTransition("text color", new ColorTransition(Color.Black), true);
            ReleasedOutlineColor = AddColorTransition("outline color", new ColorTransition(Color.Transparent), true);
            ReleasedOutlineWidth = AddInt("outline width", 0, 0, 10);
            
            NewCategory("boxes");
            LeftBox = AddInputBox("left box", new InputBox("<", new Vector2(0.0f, 64.0f), new Vector2(64.0f, 64.0f)));
            RightBox = AddInputBox("right box", new InputBox(">", new Vector2(64.0f, 64.0f), new Vector2(64.0f, 64.0f)));
            JumpBox = AddInputBox("jump box", new InputBox("jump", new Vector2(64.0f, 0.0f), new Vector2(64.0f, 64.0f)));
            GrappleBox = AddInputBox("grapple box", new InputBox("grap", new Vector2(0.0f, 0.0f), new Vector2(64.0f, 64.0f)));
            SlideBox = AddInputBox("slide box", new InputBox("slid", new Vector2(128.0f, 0.0f), new Vector2(64.0f, 64.0f)));
            BoostBox = AddInputBox("boost box", new InputBox("bst", new Vector2(128.0f, 64.0f), new Vector2(64.0f, 64.0f)));
            ItemBox = AddInputBox("item box", new InputBox("item", new Vector2(192.0f, 64.0f), new Vector2(64.0f, 64.0f)));
        }

        public static InputDisplay Instance = new InputDisplay();

        public override bool FixedPos()
        {
            return Orientation.Value != EOrientation.PLAYER;
        }

        public override void UpdateComponents()
        {
            if (Font.Modified() || FontSize.Modified() || Scale.Modified())
            {
                if (font != null)
                    Velo.ContentManager.Release(font);
                font = null;
            }

            if (font == null)
            {
                font = new CFont(Font.Value, (int)(FontSize.Value * Scale.Value));
                Velo.ContentManager.Load(font, false);
            }

            Player player = Velo.MainPlayer;

            if (player == null)
                return;

            if (boxComps == null)
            {
                boxComps = new CRectangleDrawComponent[7];
                for (int j = 0; j < 7; j++)
                {
                    boxComps[j] = new CRectangleDrawComponent(0.0f, 0.0f, 0.0f, 0.0f)
                    {
                        IsVisible = true,
                        FillEnabled = true,
                        OutlineEnabled = true
                    };
                    AddComponent(boxComps[j]);
                }
            }

            if (textComps == null)
            {
                textComps = new CTextDrawComponent[7];
                for (int j = 0; j < 7; j++)
                {
                    textComps[j] = new CTextDrawComponent("", font, Vector2.Zero)
                    {
                        color_replace = false,
                        IsVisible = true
                    };
                    AddComponent(textComps[j]);
                }
            }

            bool[] pressed = new bool[] 
            { 
                player.leftPressed, 
                player.rightPressed, 
                player.jumpPressed, 
                player.grapplePressed, 
                player.slidePressed, 
                player.boostPressed, 
                player.itemPressed 
            };
            InputBox[] inputBoxes = new InputBox[]
            {
                LeftBox.Value,
                RightBox.Value,
                JumpBox.Value,
                GrappleBox.Value,
                SlideBox.Value,
                BoostBox.Value,
                ItemBox.Value
            };

            Color releasedBoxColor = ReleasedBoxColor.Value.Get();
            Color pressedBoxColor = PressedBoxColor.Value.Get();
            Color releasedOutlineColor = ReleasedOutlineColor.Value.Get();
            Color pressedOutlineColor = PressedOutlineColor.Value.Get();
            Color releasedTextColor = ReleasedTextColor.Value.Get();
            Color pressedTextColor = PressedTextColor.Value.Get();

            float screenWidth = Velo.SpriteBatch.GraphicsDevice.Viewport.Width;
            float screenHeight = Velo.SpriteBatch.GraphicsDevice.Viewport.Height;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (InputBox inputBox in inputBoxes)
            {
                minX = Math.Min(minX, inputBox.position.X);
                minY = Math.Min(minY, inputBox.position.Y);
                maxX = Math.Max(maxX, inputBox.position.X + inputBox.size.X);
                maxY = Math.Max(maxY, inputBox.position.Y + inputBox.size.Y);
            }

            float width = (maxX - minX) * Scale.Value;
            float height = (maxY - minY) * Scale.Value;

            Vector2 origin = 
                Offset.Value + Orientation.Value.GetOrigin(width, height, screenWidth, screenHeight, Velo.PlayerPos);

            for (int i = 0; i < 7; i++)
            {
                bool isPressed = pressed[i];

                boxComps[i].FillColor = (isPressed ? pressedBoxColor : releasedBoxColor) * Opacity.Value;
                boxComps[i].OutlineColor = (isPressed ? pressedOutlineColor : releasedOutlineColor) * Opacity.Value;
                boxComps[i].OutlineThickness = isPressed ? PressedOutlineWidth.Value : ReleasedOutlineWidth.Value;
                boxComps[i].SetPositionSize(origin + inputBoxes[i].position * Scale.Value, inputBoxes[i].size * Scale.Value);

                textComps[i].StringText = inputBoxes[i].text;
                textComps[i].Font = font;
                textComps[i].Color = isPressed ? pressedTextColor : releasedTextColor;
                textComps[i].Opacity = Opacity.Value;
                textComps[i].UpdateBounds();
                textComps[i].Position = origin + inputBoxes[i].position * Scale.Value + (inputBoxes[i].size * Scale.Value - textComps[i].Bounds.Size) / 2.0f;
            }
        }
    }
}
