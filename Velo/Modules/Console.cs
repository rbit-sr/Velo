using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Velo
{
    public class ConsoleContext : MenuContext
    {
        private readonly ConsoleWindow consoleWindow;
        public ConsoleWindow ConsoleWindow => consoleWindow;

        public ConsoleContext(ToggleSetting enabled) :
            base(enabled, enableDim: false, cursor: false) 
        {
            consoleWindow = new ConsoleWindow(true);

            AddElem(consoleWindow, StackW.TOP_LEFT, Vector2.Zero, new Vector2(StackW.REQUESTED_SIZE_X, StackW.REQUESTED_SIZE_Y));
        }

        public override void EnterMenu(bool animation = true)
        {
            base.EnterMenu(false);
        }

        public override void ExitMenu(bool animation = true)
        {
            base.ExitMenu(false);
        }

        public override bool Draw()
        {
            return base.Draw();
        }
    }

    public static class StatsMessage
    {
        private static string[] parts = null;
        public static string[] Parts
        {
            get
            {
                if (parts == null)
                    Reload();
                return parts;
            }
        }

        public static bool Reload()
        {
            if (!File.Exists("Velo\\_statsMessage.txt"))
                return false;

            string statsMessage = File.ReadAllText("Velo\\_statsMessage.txt").Replace("\r", "");

            List<string> parts = new List<string>();

            int i = 0;
            while (true)
            {
                int start = statsMessage.IndexOf("${", i);
                if (start == -1)
                {
                    parts.Add(statsMessage.Substring(i).Replace("\\$", "$"));
                    break;
                }
                if (start > 0 && statsMessage[start - 1] == '\\')
                {
                    parts.Add(statsMessage.Substring(i, start + 2 - i).Replace("\\$", "$"));
                    i = start + 2;
                    continue;
                }
                int end = statsMessage.IndexOf('}', start);
                if (end == -1)
                {
                    parts.Add(statsMessage.Substring(i).Replace("\\$", "$"));
                    break;
                }
                parts.Add(statsMessage.Substring(i, start - i).Replace("\\$", "$"));
                parts.Add(statsMessage.Substring(start + 2, end - start - 2));
                i = end + 1;
            }

            StatsMessage.parts = parts.ToArray();
            return true;
        }

        public static string Get()
        {
            if (Parts == null)
                return "";

            StringBuilder sb = new StringBuilder();

            const bool MODE_TEXT = false;
            const bool MODE_FIELD = true;

            bool mode = MODE_TEXT;
            foreach (string part in parts)
            {
                if (mode == MODE_TEXT)
                    sb.Append(part);
                else if (mode == MODE_FIELD)
                {
                    string[] split = part.Split(' ');
                    if (split.Length == 0 || split.Length > 3)
                    {
                        sb.Append($"${{{part}}}");
                    }
                    else
                    {
                        string field = split[0];
                        int precision = 6;
                        int padding = 0;
                        try
                        {
                            if (split.Length > 1)
                                precision = ParseHelper.Parse_int(split[1]);
                            if (split.Length > 2)
                                padding = ParseHelper.Parse_int(split[2]);

                            sb.Append(Fields.Get(field, precision, padding));
                        }
                        catch (CommandException e)
                        {
                            sb.Append($"${{{e.Message}}}");
                        }
                    }
                }
                mode = !mode;
            }

            return sb.ToString();
        }
    }

    public class ConsoleM : ToggleModule
    {
        public BoolSetting PrintEnteredLine;
        public HotkeySetting PrintStats;

        public VectorSetting Size;
        public EnumSetting<EOrientation> Orientation;
        public VectorSetting Offset;
        public FloatSetting Opacity;
        public IntSetting FontSize;
        public ColorTransitionSetting TextColor;
        public ColorTransitionSetting WriteColor;
        public ColorTransitionSetting BackgroundColor;

        public ConsoleContext context;

        private bool prolongDisableEnter = false;
        private bool prolongDisableEscape = false;

        private readonly List<string> history = new List<string>();
        private int historyIndex = -1;

        public ConsoleM() : base("Console")
        {
            Enabled.SetValueAndDefault(new Toggle(0x200 | (ushort)Keys.Z));

            NewCategory("general");
            PrintEnteredLine = AddBool("print entered line", true);
            PrintStats = AddHotkey("print stats", 0x97);

            PrintEnteredLine.Tooltip =
                "Prints the entered commands to the console.";
            PrintStats.Tooltip =
                "Prints the stats message to the console. " +
                "You can customize this message under \"Velo/_statsMessage.txt\". " +
                "Wrap the arguments you would otherwise pass to the \"get\" command into the expression \"${...}\" as placeholder for field values. " +
                "Type \"help get\" for more information.";

            NewCategory("style");
            Size = AddVector("size", new Vector2(800f, 450f), new Vector2(100f, 75f), new Vector2(1920f, 1080f));
            Orientation = AddEnum("orientation", EOrientation.TOP_RIGHT - 1,
                Enum.GetValues(typeof(EOrientation)).Cast<EOrientation>().
                Skip(1). // no PLAYER orientation
                Select(o => o.Label()).ToArray());
            Offset = AddVector("offset", Vector2.Zero, new Vector2(-1920f, -1080f), new Vector2(1920f, 1080f));
            Opacity = AddFloat("opacity", 1f, 0f, 1f);
            FontSize = AddInt("font size", 14, 1, 50);
            TextColor = AddColorTransition("text color", new ColorTransition(new Color(0, 180, 0)));
            WriteColor = AddColorTransition("write color", new ColorTransition(new Color(255, 255, 255)));
            BackgroundColor = AddColorTransition("background color", new ColorTransition(new Color(0, 0, 0, 127)));

            NewCategory("stats window");
            AddSubmodule(StatsWindow.Instance);
        }

        public static ConsoleM Instance = new ConsoleM();

        public override void Init()
        {
            base.Init();

            context = new ConsoleContext(Enabled);
            AppendLine("Welcome! Type \"help\" for a list of commands.");
        }

        private static void ClearKey(ref KeyboardState state, Keys key)
        {
            state = new KeyboardState(state.GetPressedKeys().Where(k => k != key).ToArray());
        }

        private static string EscapeString(string text)
        {
            return text.Replace("\\", "\\\\").Replace("$", "\\$");
        }

        public override void PostRender()
        {
            base.PostRender();

            if (PrintStats.Pressed())
            {
                AppendLine("\n" + StatsMessage.Get());
            }

            ConsoleWindow console = context.ConsoleWindow;

            console.Opacity = Opacity.Value;
            console.ForceSize = Size.Value;
            console.Offset = 
                (Orientation.Value + 1).GetOrigin(Size.Value.X, Size.Value.Y, 1920f, 1080f, Vector2.Zero) +
                Offset.Value;
            console.Text.Color = TextColor.Value.Get;
            console.Write.Color = WriteColor.Value.Get;
            console.BackgroundColor = BackgroundColor.Value.Get;
            console.FontSize = FontSize.Value;

            if (!Enabled.Value.Enabled)
                console.Write.Editing = false;
            else
            {
                if (Input.IsPressed((ushort)Keys.Enter))
                {
                    if (!console.Write.Editing)
                    {
                        console.Write.Editing = true;
                    }
                    else
                    {
                        string result;
                        try
                        {
                            result = Commands.Execute(console.Write.Text.Substring(1));
                        }
                        catch (CommandException e)
                        {
                            result = e.Message;
                        }

                        if (PrintEnteredLine.Value && console.Write.Text.Substring(1).Trim(new[] { ' ' }) != "")
                        {
                            console.Text.AppendLine(EscapeString(console.Write.Text), () => WriteColor.Value.Get());
                            historyIndex = -1;
                            history.Insert(0, console.Write.Text);
                        }
                        if (result != "")
                            console.Text.AppendLine(result, () => TextColor.Value.Get());
                        console.Text.ScrollDown();

                        prolongDisableEnter = true;
                        console.Write.Editing = false;
                    }
                }
                if (Input.IsPressed((ushort)Keys.Escape))
                {
                    if (console.Write.Editing)
                    {
                        console.Write.Editing = false;
                        prolongDisableEscape = true;
                    }
                }
            }

            if (console.Write.Editing || prolongDisableEnter)
            {
                ClearKey(ref Velo.CEngineInst.input_manager.keyboard_state1, Keys.Enter);
                ClearKey(ref Velo.CEngineInst.input_manager.keyboard_state2, Keys.Enter);
            }
            if (console.Write.Editing || prolongDisableEscape)
            {
                ClearKey(ref Velo.CEngineInst.input_manager.keyboard_state1, Keys.Escape);
                ClearKey(ref Velo.CEngineInst.input_manager.keyboard_state2, Keys.Escape);
            }

            if (!Input.IsDown((ushort)Keys.Enter))
                prolongDisableEnter = false;
            if (!Input.IsDown((ushort)Keys.Escape))
                prolongDisableEscape = false;

            if (console.Write.Editing)
            {
                if (Input.IsPressed((ushort)Keys.Up) && history.Count >= 1)
                {
                    historyIndex = Math.Min(historyIndex + 1, history.Count - 1);
                    console.Write.Text = history[historyIndex];
                    console.Write.EditPos = console.Write.Text.Length;
                }
                if (Input.IsPressed((ushort)Keys.Down) && historyIndex != -1)
                {
                    historyIndex = Math.Max(historyIndex - 1, -1);
                    if (historyIndex >= 0)
                        console.Write.Text = history[historyIndex];
                    else
                        console.Write.Text = ">";
                    console.Write.EditPos = console.Write.Text.Length;
                }
            }

            context.Draw();
        }

        public void AppendLine(string text)
        {
            context.ConsoleWindow.Text.AppendLine(text, () => TextColor.Value.Get());
            context.ConsoleWindow.Text.ScrollDown();
        }

        public void Clear()
        {
            context.ConsoleWindow.Text.Clear();
        }
    }

    public class StatsWindowContext : MenuContext
    {
        private readonly ConsoleWindow consoleWindow;
        public ConsoleWindow ConsoleWindow => consoleWindow;

        public StatsWindowContext(ToggleSetting enabled) :
            base(enabled, enableDim: false, cursor: false)
        {
            consoleWindow = new ConsoleWindow(false);

            AddElem(consoleWindow, StackW.TOP_LEFT, Vector2.Zero, new Vector2(StackW.REQUESTED_SIZE_X, StackW.REQUESTED_SIZE_Y));
        }

        public override void EnterMenu(bool animation = true)
        {
            base.EnterMenu(false);
        }

        public override void ExitMenu(bool animation = true)
        {
            base.ExitMenu(false);
        }

        public override bool Draw()
        {
            return base.Draw();
        }
    }

    public class StatsWindow : ToggleModule
    {
        public IntSetting UpdateInterval;

        public VectorSetting Size;
        public EnumSetting<EOrientation> Orientation;
        public VectorSetting Offset;
        public FloatSetting Opacity;
        public IntSetting FontSize;
        public ColorTransitionSetting TextColor;
        public ColorTransitionSetting BackgroundColor;

        public StatsWindowContext context;

        private TimeSpan updatePrev = TimeSpan.Zero;

        public StatsWindow() : base("Stats Window")
        {
            NewCategory("general");
            UpdateInterval = AddInt("update interval", 50, 0, 2000);

            UpdateInterval.Tooltip =
                "update interval in milliseconds";

            NewCategory("style");
            Size = AddVector("size", new Vector2(475f, 110f), new Vector2(100f, 75f), new Vector2(1920f, 1080f));
            Orientation = AddEnum("orientation", EOrientation.RIGHT - 1,
                Enum.GetValues(typeof(EOrientation)).Cast<EOrientation>().
                Skip(1). // no PLAYER orientation
                Select(o => o.Label()).ToArray());
            Offset = AddVector("offset", Vector2.Zero, new Vector2(-1920f, -1080f), new Vector2(1920f, 1080f));
            Opacity = AddFloat("opacity", 1f, 0f, 1f);
            FontSize = AddInt("font size", 14, 1, 50);
            TextColor = AddColorTransition("text color", new ColorTransition(new Color(0, 180, 0)));
            BackgroundColor = AddColorTransition("background color", new ColorTransition(new Color(0, 0, 0, 127)));
        }

        public static StatsWindow Instance = new StatsWindow();

        public override void Init()
        {
            base.Init();

            context = new StatsWindowContext(Enabled);
        }

        public override void PostRender()
        {
            base.PostRender();

            if (!Velo.Ingame)
                return;

            ConsoleWindow console = context.ConsoleWindow;

            if ((Velo.RealTime - updatePrev) >= TimeSpan.FromMilliseconds(UpdateInterval.Value))
            {
                updatePrev = Velo.RealTime;
                console.Text.Clear();
                console.Text.AppendLine(StatsMessage.Get(), TextColor.Value.Get);
            }

            console.Opacity = Opacity.Value;
            console.ForceSize = Size.Value;
            console.Offset =
                (Orientation.Value + 1).GetOrigin(Size.Value.X, Size.Value.Y, 1920f, 1080f, Vector2.Zero) +
                Offset.Value;
            console.Text.Color = TextColor.Value.Get;
            console.BackgroundColor = BackgroundColor.Value.Get;
            console.FontSize = FontSize.Value;

            context.Draw();
        }
    }

    public static class ConsoleFont
    {
        public static readonly int REGULAR = 0;
        public static readonly int BOLD = 1;
        public static readonly int ITALICS = 2;
        public static readonly int BOLD_ITALICS = 3;

        public static CachedFont[] Get(int fontSize)
        {
            CachedFont[] fonts;
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Fonts) + "\\consola.ttf"))
            {
                fonts = new CachedFont[4];

                string originalRoot = Velo.CEngineInst.ContentBundleManager.contentTracker.RootDirectory;
                Velo.CEngineInst.ContentBundleManager.contentTracker.RootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                FontCache.Get(ref fonts[REGULAR], "consola.ttf:" + fontSize);
                FontCache.Get(ref fonts[BOLD], "consolab.ttf:" + fontSize);
                FontCache.Get(ref fonts[ITALICS], "consolai.ttf:" + fontSize);
                FontCache.Get(ref fonts[BOLD_ITALICS], "consolaz.ttf:" + fontSize);
                Velo.CEngineInst.ContentBundleManager.contentTracker.RootDirectory = originalRoot;
            }
            else
            {
                fonts = new CachedFont[1];

                FontCache.Get(ref fonts[REGULAR], "CEngine\\Debug\\FreeMonoBold.ttf:" + fontSize);
            }
            return fonts;
        }
    }

    public class ConsoleWindow : VLayoutW
    {
        private readonly bool editable;
        public readonly ConsoleTextW Text;
        public readonly ConsoleEditW Write;
        private readonly LayoutChild editChild;
        private CachedFont[] fonts;

        private int fontSize = 14;
        public int FontSize
        {
            get => fontSize;
            set
            {
                bool changed = fontSize != value;
                fontSize = value;
                if (changed)
                    UpdateFonts();
            }
        }

        public Vector2 ForceSize { get; set; }

        private void UpdateFonts()
        {
            fonts = ConsoleFont.Get(FontSize);
            if (Text != null)
                Text.Fonts = fonts;
            if (Write != null && editable)
            {
                Write.SetFont(fonts[ConsoleFont.REGULAR]);
                editChild.Size = fonts[ConsoleFont.REGULAR].Font.LineSpacing + 5f;
            }
        }

        public ConsoleWindow(bool editable)
        {
            this.editable = editable;
            UpdateFonts();

            Text = new ConsoleTextW(fonts);
            if (editable)
                Write = new ConsoleEditW(fonts[ConsoleFont.REGULAR]);

            AddChild(Text, FILL);
            if (editable)
                editChild = AddChild(Write, fonts[ConsoleFont.REGULAR].Font.LineSpacing + 5f);

            BackgroundVisible = true;

            if (editable)
            {
                Util.EnableCursorOn(() => Write.Editing);
                Util.DisableKeyInputsOn(() => Write.Editing);
                Util.DisableHotkeysOn(() => Write.Editing);
            }
        }

        public override Vector2 RequestedSize => ForceSize;
    }

    public class ConsoleTextW : ScrollW, IListEntryFactory<int>
    {
        private struct Mode
        {
            public Func<Color> Color;
            public int Indent;
            public bool Bold;
            public bool Italics;
        }

        private struct Part
        {
            public Mode Mode;
            public string Text;
        }

        private readonly ListW<int> linesLabels;
        private readonly List<Part[]> lines = new List<Part[]>();
        private CachedFont[] fonts;

        private float previousParentBounds = 0f;

        private int parsedTo = 0;
        private string text = "";
        private readonly List<KeyValuePair<int, Mode>> modeChanges = new List<KeyValuePair<int, Mode>>();

        private float charWidth;

        public Func<Color> Color { get; set; }

        public ConsoleTextW(CachedFont[] fonts) :
            base(EOrientation.VERTICAL, null)
        {
            Color = () => Microsoft.Xna.Framework.Color.White;
            Clear();

            this.fonts = fonts;

            linesLabels = new ListW<int>(this)
            {
                //FixedEntryHeight = fonts[ConsoleFont.REGULAR].Font.LineSpacing
            };
            Child = linesLabels;

            charWidth = fonts[ConsoleFont.REGULAR].Font.MeasureString("A").X;
        }

        public CachedFont[] Fonts
        {
            get => fonts;
            set
            {
                fonts = value;
                lines.Clear();
                parsedTo = 0;
                charWidth = fonts[ConsoleFont.REGULAR].Font.MeasureString("A").X;
                linesLabels.RefreshAllEntries();
            }
        }

        private static string Unescape(string text)
        {
            return text.Replace("\\$", "$").Replace("\\\\", "\\");
        }

        private void AddModeChange(int i, Mode mode)
        {
            if (modeChanges.Count == 0 || modeChanges.Last().Key < i)
                modeChanges.Add(new KeyValuePair<int, Mode>(i, mode));
            else
                modeChanges[modeChanges.Count - 1] = new KeyValuePair<int, Mode>(i, mode);
        }

        public void AppendLine(string text, Func<Color> color)
        {
            AddModeChange(this.text.Length, new Mode { Color = color });

            StringBuilder sb = new StringBuilder();
            Mode currentMode = modeChanges.Last().Value;

            int i = 0;
            int offset = this.text.Length;
            while (true)
            {
                string append;

                int start = text.IndexOf("$[", i);
                if (start == -1)
                {
                    sb.Append(Unescape(text.Substring(i, text.Length - i)));
                    break;
                }
                if (start > 0 && text[start - 1] == '\\')
                {
                    append = Unescape(text.Substring(i, start + 2 - i));
                    sb.Append(append);
                    offset += append.Length;
                    i = start + 2;
                    continue;
                }
                int end = text.IndexOf(']', start);
                if (end == -1)
                {
                    sb.Append(Unescape(text.Substring(i, text.Length - i)));
                    break;
                }
                append = text.Substring(i, start - i);
                sb.Append(append);
                offset += append.Length;
                i = end + 1;

                string escape = text.Substring(start + 2, end - start - 2);
                string[] parts = escape.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                {
                    append = $"$[{escape}]";
                    sb.Append(append);
                    offset += append.Length;
                    continue;
                }
                try
                {
                    if (parts[0] == "c")
                    {
                        Color color_ = ParseHelper.Parse_Color(parts[1]);
                        currentMode.Color = () => color_;
                    }
                    else if (parts[0] == "in")
                    {
                        currentMode.Indent = ParseHelper.Parse_int(parts[1]);
                    }
                    else if (parts[0] == "b")
                    {
                        currentMode.Bold = ParseHelper.Parse_bool(parts[1]);
                    }
                    else if (parts[0] == "i")
                    {
                        currentMode.Italics = ParseHelper.Parse_bool(parts[1]);
                    }
                    AddModeChange(offset, currentMode);
                }
                catch (CommandException e)
                {
                    append = $"$[{e.Message}]";
                    sb.Append(append);
                    offset += append.Length;
                }
            }
            sb.Append('\n');

            this.text += sb.ToString();
        }

        public void Clear()
        {
            parsedTo = 0;
            text = "";
            lines.Clear();
            modeChanges.Clear();
            linesLabels?.RefreshAllEntries();
        }

        public IWidget Create(int elem, int i)
        {
            HLayoutW layout = new HLayoutW();
            foreach (Part part in lines[i])
            {
                if (part.Text == null)
                    layout.AddSpace(part.Mode.Indent * charWidth);
                else
                {
                    CachedFont font = fonts[ConsoleFont.REGULAR];
                    if (fonts.Length > 1)
                    {
                        if (part.Mode.Bold && part.Mode.Italics)
                            font = fonts[ConsoleFont.BOLD_ITALICS];
                        else if (part.Mode.Bold)
                            font = fonts[ConsoleFont.BOLD];
                        else if (part.Mode.Italics)
                            font = fonts[ConsoleFont.ITALICS];
                    }

                    layout.AddChild(new LabelW(part.Text, font)
                    {
                        Align = new Vector2(0f, 0.5f),
                        Color = part.Mode.Color
                    }, part.Text.Length * charWidth);
                }
            }

            return layout;
        }

        public IEnumerable<int> GetElems(int start)
        {
            return Enumerable.Range(start, lines.Count);
        }

        public int Length => lines.Count;

        public float Height(int elem, int i)
        {
            return fonts[ConsoleFont.REGULAR].Font.LineSpacing;
        }

        public override void UpdateBounds(Bounds parentBounds)
        {
            if (parentBounds.Size.X != previousParentBounds)
            {
                parsedTo = 0;
                lines.Clear();
                linesLabels.RefreshAllEntries();
                previousParentBounds = parentBounds.Size.X;
            }

            int currentMode = 0;

            int charsPerLine = Math.Max((int)(parentBounds.Size.X / charWidth), 1);
            while (parsedTo < text.Length)
            {
                currentMode = modeChanges.FindLastIndex(p => p.Key <= parsedTo);

                int cut = text.Length;
                int nextBegin = cut;

                int lineBreak = text.IndexOf('\n', parsedTo);
                if (lineBreak != -1 && lineBreak < cut)
                {
                    cut = lineBreak;
                    nextBegin = cut + 1;
                }

                int charsPerLineMinusIndent = Math.Max(charsPerLine - modeChanges[currentMode].Value.Indent, 1);
                int indent = charsPerLine - charsPerLineMinusIndent;

                if (parsedTo + charsPerLineMinusIndent < cut)
                {
                    int proposedCut = text.LastIndexOfAny(new[] { ' ', '\n' }, parsedTo + charsPerLineMinusIndent);
                    if (proposedCut < parsedTo)
                    {
                        cut = parsedTo + charsPerLineMinusIndent;
                        nextBegin = cut;
                        if (text[cut] == ' ' || text[cut] == '\n')
                            nextBegin++;
                        goto end;
                    }
                    int proposedNextBegin = proposedCut + 1;

                    int wordEnd = text.IndexOfAny(new[] { ' ', '\n' }, parsedTo + charsPerLineMinusIndent);
                    if (wordEnd == -1)
                        wordEnd = text.Length;

                    if (wordEnd - proposedNextBegin <= charsPerLineMinusIndent)
                    {
                        cut = proposedCut;
                        nextBegin = proposedNextBegin;
                    }
                    else
                    {
                        cut = parsedTo + charsPerLineMinusIndent;
                        nextBegin = cut;
                    }
                }
            end:

                List<Part> parts = new List<Part>();
                if (indent > 0)
                    parts.Add(new Part { Mode = modeChanges[currentMode].Value, Text = null });
                while (parsedTo < cut)
                {
                    Mode mode = modeChanges[currentMode].Value;

                    int nextModeBegin = modeChanges.Count > currentMode + 1 ? modeChanges[currentMode + 1].Key : text.Length;
                    int end = cut;
                    if (nextModeBegin <= end)
                    {
                        end = nextModeBegin;
                        currentMode++;
                    }

                    parts.Add(new Part { Mode = mode, Text = text.Substring(parsedTo, end - parsedTo) });
                    parsedTo = end;
                }
                lines.Add(parts.ToArray());
                
                parsedTo = nextBegin;
            }

            base.UpdateBounds(parentBounds);
        }
    }

    public class ConsoleEditW : LabelW
    {
        private readonly HotkeySetting leftKey = new HotkeySetting(null, "", (ushort)Keys.Left, autoRepeat: true);
        private readonly HotkeySetting rightKey = new HotkeySetting(null, "", (ushort)Keys.Right, autoRepeat: true);

        public int EditPos = 0;

        private float cursorBlinkTimer = 0f;

        private bool editing;
        public bool Editing 
        { 
            get => editing; 
            set
            {
                editing = value;
                if (editing)
                {
                    EditPos = 1;
                    Text = ">";
                    cursorBlinkTimer = 0f;
                }
                else
                {
                    EditPos = 0;
                    Text = "";
                }
            }
        }

        public ConsoleEditW(CachedFont font) :
            base("", font)
        {
            TextInputEXT.TextInput += InputChar;
            Align = new Vector2(0.0f, 0.5f);
        }

        ~ConsoleEditW()
        {
            TextInputEXT.TextInput -= InputChar;
        }

        private void InputChar(char c)
        {
            if (!Editing)
                return;
            if (c == (char)Keys.Back)
            {
                if (EditPos > 1)
                {
                    EditPos--;
                    Text = Text.Remove(EditPos, 1);
                }
            }
            else if (c != (char)Keys.Enter && c != 22)
            {
                Text = Text.Insert(EditPos, c.ToString());
                EditPos++;
            }
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            base.Draw(hovered, parentCropRec, scale, opacity);

            if (!Visible)
                return;

            cursorBlinkTimer += Math.Min((float)Velo.RealDelta.TotalSeconds, 1f);
            if (cursorBlinkTimer >= 1f)
                cursorBlinkTimer -= 1f;

            if (Editing)
            {
                if (leftKey.Pressed(bypassHotkeysDisabled: true))
                {
                    EditPos--;
                    if (EditPos < 1)
                        EditPos = 1;
                    cursorBlinkTimer = 0f;
                }
                else if (rightKey.Pressed(bypassHotkeysDisabled: true))
                {
                    EditPos++;
                    if (EditPos > Text.Length)
                        EditPos = Text.Length;
                    cursorBlinkTimer = 0f;
                }
                if (Input.IsPressed(0x200 | (ushort)Keys.V))
                {
                    string clipboard = "";
                    Thread staThread = new Thread(
                        delegate ()
                        {
                            try
                            {
                                clipboard = System.Windows.Forms.Clipboard.GetText();
                            }
                            catch {}
                        });
                    staThread.SetApartmentState(ApartmentState.STA);
                    staThread.Start();
                    staThread.Join();
                    Text = Text.Insert(EditPos, clipboard);
                    EditPos += clipboard.Length;
                }

                TextInputEXT.StartTextInput();

                CRectangleDrawComponent cursor = new CRectangleDrawComponent(0f, 0f, 0f, 0f)
                {
                    IsVisible = cursorBlinkTimer < 0.5f,
                    FillColor = Color != null ? Color() : Microsoft.Xna.Framework.Color.White,
                    FillEnabled = true,
                    OutlineEnabled = false,
                    OutlineThickness = 0,
                };

                Vector2 cursorOff = Font.MeasureString(Text.Substring(0, EditPos));
                cursor.SetPositionSize((Position + new Vector2(cursorOff.X, 5f)) * scale, new Vector2(2f, Font.LineSpacing - 5f) * scale);
                cursor.Draw(null);
            }
        }
    }
}
