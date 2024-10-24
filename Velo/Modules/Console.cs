using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Velo
{
    public class ConsoleM : MenuContext
    {
        public ConsoleM() : base("Console", addEnabledSetting: true, enableDim: false)
        {

        }

        public static ConsoleM Instance/* = new ConsoleM()*/;

        public override Menu GetStartMenu()
        {
            return new ConsoleWindow(this);
        }
    }

    public class ConsoleWindow : Menu
    {
        private readonly LayoutW layout;
        private readonly ConsoleTextW test;
        private readonly ConsoleEditW test2;

        public ConsoleWindow(MenuContext context) :
            base(context)
        {
            test = new ConsoleTextW(context.Fonts.FontMedium, (text, off) =>
            {
                int space = text.IndexOfAny(new char[] { ' ', '\n' }, off);
                if (space == -1)
                    space = text.Length;
                int length = space - off + 1;
                return new KeyValuePair<Color, int>(text[off] == '"' ? Color.Red : Color.Blue, length);
            })
            {
                BackgroundVisible = true,
                BackgroundColor = () => new Color(0, 0, 0, 50)
            };
            test.Text = test.Text + test.Text + test.Text + test.Text + test.Text;
            test.Text = test.Text + test.Text + test.Text + test.Text + test.Text;
            
            test2 = new ConsoleEditW(context.Fonts.FontMedium, (text, off) => new KeyValuePair<Color, int>(Color.Red, text.Length));

            layout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            layout.AddChild(test, LayoutW.FILL);
            layout.AddChild(test2, 35f);

            Child = layout;
        }

        public override void Refresh()
        {

        }
    }

    public class ConsoleTextW : Widget
    {
        private readonly List<TextDraw> textDraws = new List<TextDraw>();
        private string text;
        private readonly CachedFont font;
        private readonly Func<string, int, KeyValuePair<Color, int>> colorFormatter;
        private bool update = false;
        private int scroll = 0;

        public ConsoleTextW(CachedFont font, Func<string, int, KeyValuePair<Color, int>> colorFormatter)
        {
            this.font = font;
            this.colorFormatter = colorFormatter;
            text = "";
        }

        public string Text
        {
            get => text;
            set
            {
                text = value;
                update = true;
            }
        }

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);

            if (!DisableInput && MouseInside && mouseInsideParent)
            {
                events.OnScroll = wevent =>
                {
                    scroll += wevent.Amount > 0 ? 1 : -1;
                    update = true;
                    if (scroll < 0)
                        scroll = 0;
                };
            }
        }

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            if (update)
            {
                textDraws.Clear();

                float charWidth = font.Font.MeasureString("a").X;

                List<string> lines = text.Split('\n').ToList();

                int pos = 0;
                int length = 0;
                Color color = Color.White;
                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i];

                    bool autoLineBreak = false;
                    if (line.Length * charWidth > Size.X)
                    {
                        int fittingChars = (int)(Size.X / charWidth);
                        lines.Insert(i + 1, line.Substring(fittingChars));
                        lines[i] = line.Substring(0, fittingChars);
                        line = lines[i];
                        autoLineBreak = true;
                    }

                    /*bool autoLineBreak = false;
                    if (font.Font.MeasureString(lines[i]).X > Size.X) // line is too long, binary search to find the fitting length and split the line
                    {
                        int stride = lines[i].Length / 2;
                        int testLength = lines[i].Length - stride;
                        do
                        {
                            stride = (stride + 1) / 2;
                            if (font.Font.MeasureString(lines[i].Substring(0, testLength)).X > Size.X)
                                testLength -= stride;
                            else
                                testLength += stride;
                        } while (stride > 1);
                        if (font.Font.MeasureString(lines[i].Substring(0, testLength)).X > Size.X)
                            testLength--;
                        else if (font.Font.MeasureString(lines[i].Substring(0, testLength + 1)).X <= Size.X)
                            testLength++;
                        lines.Insert(i + 1, lines[i].Substring(testLength));
                        lines[i] = lines[i].Substring(0, testLength);
                        autoLineBreak = true;
                    }*/
                    Vector2 offset = new Vector2(0f, (i - scroll) * font.Font.LineSpacing);
                    if (offset.Y + font.Font.LineSpacing > Size.Y)
                        break;
                    for (int j = 0; j < line.Length;)
                    {
                        if (length == 0)
                        {
                            KeyValuePair<Color, int> format = colorFormatter(text, pos);
                            length = format.Value;
                            color = format.Key;
                        }
                        int partLength = Math.Min(length, line.Length - j);
                        if (offset.Y + font.Font.LineSpacing <= Size.Y)
                        {
                            TextDraw textDraw = new TextDraw()
                            {
                                IsVisible = true,
                                Align = Vector2.Zero,
                                HasDropShadow = true,
                                DropShadowOffset = Vector2.One,
                                DropShadowColor = Color.Black,
                                Position = offset,
                                Color = color,
                                Text = line.Substring(j, partLength)
                            };
                            textDraw.SetFont(font);
                            textDraw.UpdateBounds();
                            if (i >= scroll)
                                textDraws.Add(textDraw);
                            offset += new Vector2(textDraw.Bounds.Size.X, 0f);
                        }
                        j += partLength;
                        pos += partLength;
                        length -= partLength;
                    }
                    if (!autoLineBreak)
                    {
                        pos++;
                        length--;
                    }
                }

                update = false;
            }
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            foreach (TextDraw textDraw in textDraws)
            {
                textDraw.Offset = new Vector2(Position.X, Position.Y) + Offset;
                textDraw.Scale = Vector2.One * scale;
                textDraw.Opacity = opacity * Opacity;
                textDraw.Draw(null);
            }
        }
    }

    public class ConsoleEditW : Widget
    {
        private readonly List<TextDraw> textDraws = new List<TextDraw>();
        private string text;
        private readonly CachedFont font;
        private readonly Func<string, int, KeyValuePair<Color, int>> colorFormatter;

        private int editPos = 0;

        private bool update = false;

        public ConsoleEditW(CachedFont font, Func<string, int, KeyValuePair<Color, int>> colorFormatter)
        {
            this.font = font;
            this.colorFormatter = colorFormatter;
            text = "";
            TextInputEXT.TextInput += InputChar;
        }

        ~ConsoleEditW()
        {
            TextInputEXT.TextInput -= InputChar;
        }

        private void InputChar(char c)
        {
            text = text.Insert(editPos, c.ToString());
            editPos++;
            update = true;
        }

        public string Text
        {
            get => text;
            set
            {
                text = value;
                update = true;
            }
        }

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            if (update)
            {
                textDraws.Clear();

                int pos = 0;
                int length = 0;
                Color color = Color.White;

                string line = Text;

                Vector2 offset = new Vector2(0f, 0f);
                for (int j = 0; j < line.Length;)
                {
                    if (length == 0)
                    {
                        KeyValuePair<Color, int> format = colorFormatter(text, pos);
                        length = format.Value;
                        color = format.Key;
                    }
                    int partLength = Math.Min(length, line.Length - j);

                    TextDraw textDraw = new TextDraw()
                    {
                        IsVisible = true,
                        Align = Vector2.Zero,
                        HasDropShadow = true,
                        DropShadowOffset = Vector2.One,
                        DropShadowColor = Color.Black,
                        Position = offset,
                        Color = color,
                        Text = line.Substring(j, partLength)
                    };
                    textDraw.SetFont(font);
                    textDraw.UpdateBounds();
                    textDraws.Add(textDraw);
                    offset += new Vector2(textDraw.Bounds.Size.X, 0f);

                    j += partLength;
                    pos += partLength;
                    length -= partLength;
                }
                
                update = false;
            }
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            if (Input.IsPressed((ushort)Keys.Left))
            {
                editPos--;
                if (editPos < 0)
                    editPos = 0;
            }
            else if (Input.IsPressed((ushort)Keys.Right))
            {
                editPos++;
                if (editPos > text.Length)
                    editPos = text.Length;
            }

            TextInputEXT.StartTextInput();

            foreach (TextDraw textDraw in textDraws)
            {
                textDraw.Offset = new Vector2(Position.X, Position.Y) + Offset;
                textDraw.Scale = Vector2.One * scale;
                textDraw.Opacity = opacity * Opacity;
                textDraw.Draw(null);
            }

            CRectangleDrawComponent cursor = new CRectangleDrawComponent(0f, 0f, 2f, 20f)
            {
                IsVisible = true,
                FillColor = Color.White,
                FillEnabled = true,
                OutlineEnabled = false,
                OutlineThickness = 0
            };

            Vector2 cursorOff = font.Font.MeasureString(text.Substring(0, editPos));
            cursor.Position = Position + new Vector2(cursorOff.X, 0f);
            cursor.Draw(null);
        }
    }
}
