using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Velo
{
    public class TASeditorContext : MenuContext
    {
        private readonly TASeditorWindow editorWindow;
        public TASeditorWindow EditorWindow => editorWindow;

        public TASeditorContext(ToggleSetting enabled) : base(enabled, enableDim: false, cursor: false)
        {
            editorWindow = new TASeditorWindow();

            AddElem(editorWindow, StackW.TOP_LEFT, Vector2.Zero, new Vector2(StackW.REQUESTED_SIZE_X, StackW.REQUESTED_SIZE_Y));
        }

        public override void EnterMenu(bool animation = true)
        {
            base.EnterMenu(false);
        }

        public override void ExitMenu(bool animation = true)
        {
            base.ExitMenu(false);
        }
    }

    public class TASeditor : ToggleModule
    {
        private TASeditorContext context;

        public IntSetting Rows;

        public EnumSetting<EOrientation> Orientation;
        public VectorSetting Offset;
        public FloatSetting Opacity;
        public IntSetting FontSize;
        public VectorSetting EntrySize;
        public FloatSetting FrameWidth;
        public ColorTransitionSetting GreenColor;
        public ColorTransitionSetting RedColor;

        public TASeditorWindow EditorWindow => context.EditorWindow;

        public TASeditor() : base("TAS editor")
        {
            Rows = AddInt("rows", 48, 1, 100);
            
            NewCategory("style");
            Orientation = AddEnum("orientation", EOrientation.TOP_LEFT - 1,
                Enum.GetValues(typeof(EOrientation)).Cast<EOrientation>().
                Skip(1). // no PLAYER orientation
                Select(o => o.Label()).ToArray());
            Offset = AddVector("offset", Vector2.Zero, new Vector2(-1920f, -1080f), new Vector2(1920f, 1080f));
            Opacity = AddFloat("opacity", 1f, 0f, 1f);
            FontSize = AddInt("font size", 14, 1, 50);
            EntrySize = AddVector("entry size", new Vector2(25f, 20f), new Vector2(5f, 5f), new Vector2(50f, 50f));
            FrameWidth = AddFloat("frame width", 70f, 25f, 300f);
            GreenColor = AddColorTransition("green color", new ColorTransition(new Color(0, 180, 0, 127)));
            RedColor = AddColorTransition("red color", new ColorTransition(new Color(180, 0, 0, 127)));
        }

        public static TASeditor Instance = new TASeditor();

        public override void Init()
        {
            base.Init();

            Util.EnableCursorOn(() => Enabled.Value.Enabled && Velo.Ingame);

            context = new TASeditorContext(Enabled);
        }

        public override void PostRender()
        {
            base.PostRender();

            if (!Velo.Ingame || RecordingAndReplay.Instance.PrimarySeekable?.Recording == null)
                return;

            context.EditorWindow.Opacity = Opacity.Value;
            context.EditorWindow.Offset =
                (Orientation.Value + 1).GetOrigin(FrameWidth.Value + EntrySize.Value.X * 8, (Rows.Value + 2) * EntrySize.Value.Y + 5f, 1920f, 1080f, Vector2.Zero) +
                Offset.Value;
            context.EditorWindow.FontSize = FontSize.Value;

            context.Draw();
        }
    }

    public class FrameButton : ButtonW
    {
        public FrameButton(int frame, bool collapsed, int collapsedCount, bool colorParity, CachedFont font) : base("", font)
        {
            IReplayable recording = RecordingAndReplay.Instance.PrimarySeekable.Recording;

            if (!collapsed)
                Align = new Vector2(0f, 0.5f);
            else
                Align = new Vector2(1f, 0.5f);
            Hoverable = !collapsed;
            BackgroundVisible = true;
            BackgroundVisibleHovered = !collapsed;
            Color = () => SettingsUI.Instance.TextColor.Value.Get();
            BackgroundColor = () => colorParity ? SettingsUI.Instance.EntryColor1.Value.Get() : SettingsUI.Instance.EntryColor2.Value.Get();
            BackgroundColorHovered = () => SettingsUI.Instance.EntryHoveredColor.Value.Get();
            if (!collapsed)
                OnLeftClick = () => RecordingAndReplay.Instance.JumpToFrame(frame - recording.LapStart);

            Text = !collapsed ? $"{frame - recording.LapStart}" : $"+{collapsedCount}";
        }
    }

    public class InputButton : ButtonW
    {
        public static string GetLabel(Frame.EFlag input)
        {
            switch (input)
            {
                case Frame.EFlag.LEFT_H:
                    return "<";
                case Frame.EFlag.RIGHT_H:
                    return ">";
                case Frame.EFlag.JUMP_H:
                    return "J";
                case Frame.EFlag.GRAPPLE_H:
                    return "G";
                case Frame.EFlag.SLIDE_H:
                    return "S";
                case Frame.EFlag.BOOST_H:
                    return "B";
                case Frame.EFlag.ITEM_H:
                    return "I";
                case Frame.EFlag.RESET_LAP:
                    return "R";
                default:
                    return "";
            }
        }

        public static readonly int ALL_FLAGS_MASK = 0b1111111 | (1 << (int)Frame.EFlag.RESET_LAP);
        public static IEnumerable<Frame.EFlag> AllFlags => Enumerable.Range(0, (int)Frame.EFlag.ITEM_H + 1).Select(i => (Frame.EFlag)i).Append(Frame.EFlag.RESET_LAP);

        public InputButton(int frame, Frame.EFlag input, bool collapsed, bool colorParity, CachedFont font) : base("", font)
        {
            IReplayable recording = RecordingAndReplay.Instance.PrimarySeekable.Recording;
            
            Align = new Vector2(0.5f, 0.5f);
            Hoverable = false;
            BackgroundVisible = true;
            Color = () => SettingsUI.Instance.TextColor.Value.Get();
            BackgroundColor = () => colorParity ? SettingsUI.Instance.EntryColor1.Value.Get() : SettingsUI.Instance.EntryColor2.Value.Get();
            BackgroundColorHovered = () => SettingsUI.Instance.EntryHoveredColor.Value.Get();

            if (frame >= 0 && frame < recording.Count && recording[frame].GetFlag(input))
                Text = !collapsed ? GetLabel(input) : (font.Font.Name.EndsWith("consola.ttf") ? "⁞" : "|");
            else
                Text = "";

            if (recording is Timeline timeline && !collapsed && frame != 0)
            {
                Hoverable = true;
                BackgroundVisibleHovered = true;
                OnClick = wevent =>
                {
                    if (wevent.Button == WEMouseClick.EButton.MIDDLE)
                        return;

                    bool changed = false;
                    if (frame >= timeline.Count)
                    {
                        timeline.InsertNew(timeline.Count, frame - timeline.Count, new TimeSpan(OfflineGameMods.Instance.DeltaTime.Value));
                        changed = true;
                    }

                    Frame frame_ = timeline[frame];
                    if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    {
                        if (!frame_.GetFlag(input))
                        {
                            frame_.SetFlag(input, true);
                            changed = true;
                        }
                        if (Origins.Instance.IsOrigins())
                            frame_.SetFlag(Frame.EFlag.BOOST_H, true);
                        TASeditor.Instance.EditorWindow.Changed();
                    }
                    else if (wevent.Button == WEMouseClick.EButton.RIGHT || wevent.Button == WEMouseClick.EButton.RIGHT_REPEATED)
                    {
                        if (frame_.GetFlag(input))
                        {
                            frame_.SetFlag(input, false);
                            changed = true;
                        }
                        if (timeline.IsOrigins)
                            frame_.SetFlag(Frame.EFlag.BOOST_H, true);
                        TASeditor.Instance.EditorWindow.Changed();
                    }
                    if (changed)
                    {
                        timeline[frame] = frame_;
                        (RecordingAndReplay.Instance.Recorder as TASRecorder).SetGreenPosition(frame - 1);
                    }
                };
            }
        }
    }

    public class TASeditorWindow : VLayoutW
    {
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

        public float ForceHeight { get; set; }

        private int prevPosition = 0;
        private int prevCount = 0;
        private int scroll = 0;
        private int expandedBegin = -1;
        private int expandedEnd = -1;

        private void UpdateFonts()
        {
            fonts = ConsoleFont.Get(fontSize);
        }

        public TASeditorWindow()
        {
            UpdateFonts();
            Crop = true;
        }

        public void Changed()
        {
            expandedBegin = -1;
            expandedEnd = -1;
        }

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);
            UpdateInputChildren(mouseInsideParent, events);

            bool mouseInside = CheckMouseInside(events.MousePos);

            if (!DisableInput && mouseInside && mouseInsideParent)
            {
                events.OnScroll = wevent =>
                {
                    int magnitude = 1;
                    if (Input.IsKeyDown((byte)Keys.LShiftKey) || Input.IsKeyDown((byte)Keys.RShiftKey))
                        magnitude = 10;
                    if (Input.IsKeyDown((byte)Keys.LControlKey) || Input.IsKeyDown((byte)Keys.RControlKey))
                        magnitude = 100;

                    for (int i = 0; i < magnitude; i++)
                    {
                        if (wevent.Amount > 0)
                            ScrollDownOne();
                        else
                            ScrollUpOne();
                    }
                };
            }
        }

        private static int RepeatEnd(int frame)
        {
            IReplayable recording = RecordingAndReplay.Instance.PrimarySeekable.Recording;
            if (recording == null || frame < 0 || frame >= recording.Count)
                return frame + 1;

            int flags = recording[frame].Flags & InputButton.ALL_FLAGS_MASK;

            for (frame++; frame < recording.Count; frame++)
            {
                if ((recording[frame].Flags & InputButton.ALL_FLAGS_MASK) != flags)
                    break;
            }
            return frame;
        }

        private static int RepeatBegin(int frame)
        {
            IReplayable recording = RecordingAndReplay.Instance.PrimarySeekable.Recording;
            if (recording == null || frame < 0 || frame >= recording.Count)
                return frame;

            int flags = recording[frame].Flags & InputButton.ALL_FLAGS_MASK;

            for (frame--; frame >= 0; frame--)
            {
                if ((recording[frame].Flags & InputButton.ALL_FLAGS_MASK) != flags)
                    break;
            }
            return frame + 1;
        }

        private void ScrollDownOne()
        {
            if (scroll >= expandedBegin && scroll < expandedEnd)
                scroll++;
            else
            {
                int repeatBegin = RepeatBegin(scroll);
                int repeatEnd = RepeatEnd(scroll);
                if (repeatEnd - repeatBegin <= 3)
                    scroll++;
                else if (scroll == repeatBegin)
                    scroll = repeatBegin + 1;
                else if (scroll < repeatEnd - 1)
                    scroll = repeatEnd - 1;
                else
                    scroll = repeatEnd;
            }
            IReplayable recording = RecordingAndReplay.Instance.PrimarySeekable.Recording;
            scroll = Math.Min(scroll, recording.Count - 1);
        }

        private void ScrollUpOne()
        {
            if (scroll >= expandedBegin && scroll < expandedEnd)
                scroll--;
            else
            {
                int repeatBegin = RepeatBegin(scroll);
                int repeatEnd = RepeatEnd(scroll);
                if (repeatEnd - repeatBegin <= 3)
                    scroll--;
                else if (scroll == repeatBegin)
                    scroll = repeatBegin - 1;
                else if (scroll < repeatEnd - 1)
                    scroll = repeatBegin;
                else
                    scroll = repeatBegin + 1;
            }
            scroll = Math.Max(scroll, 0);
        }

        private HLayoutW CreateRegularRow(int frame, bool colorParity)
        {
            HLayoutW row = new HLayoutW();
            row.AddChild(new FrameButton(frame, false, 0, colorParity, fonts[ConsoleFont.REGULAR]), TASeditor.Instance.FrameWidth.Value);
            foreach (Frame.EFlag f in InputButton.AllFlags)
            {
                colorParity = !colorParity;
                row.AddChild(new InputButton(frame, f, false, colorParity, fonts[ConsoleFont.REGULAR]), TASeditor.Instance.EntrySize.Value.X);
            }
            return row;
        }

        private HLayoutW CreateCollapsedRow(int frame, int collapsedCount, bool colorParity)
        {
            HLayoutW row = new HLayoutW();
            row.AddChild(new FrameButton(frame, true, collapsedCount, colorParity, fonts[ConsoleFont.REGULAR]), TASeditor.Instance.FrameWidth.Value);
            foreach (Frame.EFlag f in InputButton.AllFlags)
            {
                colorParity = !colorParity;
                row.AddChild(new InputButton(frame, f, true, colorParity, fonts[ConsoleFont.REGULAR]), TASeditor.Instance.EntrySize.Value.X);
            }
            return row;
        }

        private struct Row
        {
            public int Frame;
            public int CollapsedCount;
            public bool ColorParity;

            public Row(int frame, int collapsedCount, bool colorParity)
            {
                Frame = frame;
                CollapsedCount = collapsedCount;
                ColorParity = colorParity;
            }

            public int End => CollapsedCount == 0 ? Frame + 1 : Frame + CollapsedCount;
        }

        private LabelW CreateHeaderLabel(string text)
        {
            return new LabelW(text, fonts[ConsoleFont.BOLD])
            {
                Color = SettingsUI.Instance.HeaderTextColor.Value.Get,
                Align = new Vector2(0.5f, 0.5f)
            };
        }

        private ButtonW CreateControlButton(string text)
        {
            return new ButtonW(text, fonts[ConsoleFont.REGULAR])
            {
                Color = SettingsUI.Instance.TextColor.Value.Get,
                BackgroundVisible = true,
                BackgroundColor = SettingsUI.Instance.ButtonColor.Value.Get,
                Hoverable = true,
                BackgroundVisibleHovered = true,
                BackgroundColorHovered = SettingsUI.Instance.ButtonHoveredColor.Value.Get,
                Align = new Vector2(0.5f, 0.5f)
            };
        }

        public override void UpdateBounds(Bounds parentBounds)
        {
            ClearChildren();

            HLayoutW header = new HLayoutW();
            header.AddChild(CreateHeaderLabel("Frame"), TASeditor.Instance.FrameWidth.Value);
            foreach (Frame.EFlag f in InputButton.AllFlags)
            {
                header.AddChild(CreateHeaderLabel(InputButton.GetLabel(f)), TASeditor.Instance.EntrySize.Value.X);
            }
            AddChild(header, TASeditor.Instance.EntrySize.Value.Y);

            ISeekable seekable = RecordingAndReplay.Instance.PrimarySeekable;
            Timeline timeline = seekable.Recording is Timeline timeline_ ? timeline_ : null;

            int repeatBeginExpanded = RepeatBegin(expandedBegin);
            int repeatEndExpanded = RepeatEnd(expandedBegin);
            if (repeatBeginExpanded != expandedBegin || repeatEndExpanded != expandedEnd)
                Changed();
            if (Input.IsKeyDown((byte)Keys.Escape))
                Changed();

            if (seekable.Recording.Count != prevCount)
            {
                prevCount = seekable.Recording.Count;
                Changed();
            }

            bool restoreScroll = seekable.AbsoluteFrame != prevPosition;
            if (restoreScroll)
                prevPosition = seekable.AbsoluteFrame;

            if (restoreScroll && scroll > seekable.AbsoluteFrame)
                scroll = seekable.AbsoluteFrame;

            List<Row> rows = new List<Row>();

            int frame = scroll;
            int repeatBegin = RepeatBegin(frame);
            int repeatEnd = RepeatEnd(frame);
            if (repeatEnd - repeatBegin > 3)
                frame = repeatBegin;
            bool colorParity = frame % 2 == 0;
            int rowCount = TASeditor.Instance.Rows.Value;
            while (true)
            {
                int repeat = RepeatEnd(frame) - frame;

                if (repeat <= 3 || (frame >= expandedBegin && frame < expandedEnd))
                {
                    for (int j = 0; j < repeat; j++)
                    {
                        if (frame + 1 > scroll) rows.Add(new Row(frame, 0, colorParity));
                        frame++;
                    }
                }
                else
                {
                    if (frame + 1 > scroll) rows.Add(new Row(frame, 0, colorParity));
                    frame++;
                    if (frame + repeat - 2 > scroll) rows.Add(new Row(frame, repeat - 2, colorParity));
                    frame += repeat - 2;
                    if (frame + 1 > scroll) rows.Add(new Row(frame, 0, colorParity));
                    frame++;
                }
                colorParity = !colorParity;

                if (rows.Count >= rowCount)
                {
                    if (restoreScroll && frame <= seekable.AbsoluteFrame)
                        while (rows.Count >= rowCount) { rows.RemoveAt(0); ScrollDownOne(); }
                    else if (restoreScroll && frame > seekable.AbsoluteFrame)
                    {
                        while (rows[rows.Count - 1].Frame > seekable.AbsoluteFrame && rows.Count > rowCount)
                            rows.RemoveAt(rows.Count - 1);
                        while (rows.Count > rowCount)
                        {
                            rows.RemoveAt(0);
                            ScrollDownOne();
                        }
                        break;
                    }
                    else
                    {
                        while (rows.Count > rowCount)
                            rows.RemoveAt(rows.Count - 1);
                        break;
                    }
                }
            }

            foreach (Row row in rows)
            {
                HLayoutW layout;

                if (row.CollapsedCount == 0)
                    layout = CreateRegularRow(row.Frame, row.ColorParity);
                else
                    layout = CreateCollapsedRow(row.Frame, row.CollapsedCount, row.ColorParity);
                if (row.Frame <= seekable.AbsoluteFrame && row.End > seekable.AbsoluteFrame)
                {
                    layout.OutlineThickness = 2;
                    layout.OutlineColor = SettingsUI.Instance.HighlightTextColor.Value.Get;
                }
                if (row.Frame <= timeline?.GreenPosition)
                {
                    layout.BackgroundVisible = true;
                    layout.BackgroundColor = TASeditor.Instance.GreenColor.Value.Get;
                }
                if (row.Frame > timeline?.GreenPosition && row.Frame < timeline?.Count)
                {
                    layout.BackgroundVisible = true;
                    layout.BackgroundColor = TASeditor.Instance.RedColor.Value.Get;
                }

                IWidget child = layout;
                if (row.CollapsedCount != 0)
                {
                    ClickableW<IWidget> clickable = new ClickableW<IWidget>(child)
                    {
                        Hoverable = true,
                        BackgroundVisibleHovered = true,
                        BackgroundColorHovered = SettingsUI.Instance.EntryHoveredColor.Value.Get,
                        OnLeftClick = () =>
                        {
                            expandedBegin = row.Frame - 1;
                            expandedEnd = expandedBegin + row.CollapsedCount + 2;
                        }
                    };
                    child = clickable;
                }
                AddChild(child, TASeditor.Instance.EntrySize.Value.Y);
            }

            AddSpace(5f);

            HLayoutW buttonRow = new HLayoutW();

            ButtonW jumpBack1 = CreateControlButton("<");
            jumpBack1.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    RecordingAndReplay.Instance.OffsetFrames(-1);
            };
            ButtonW jumpBack10 = CreateControlButton("<<");
            jumpBack10.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    RecordingAndReplay.Instance.OffsetFrames(-10);
            };
            ButtonW jump1 = CreateControlButton(">");
            jump1.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    RecordingAndReplay.Instance.OffsetFrames(1);
            };
            ButtonW jump10 = CreateControlButton(">>");
            jump10.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT || wevent.Button == WEMouseClick.EButton.LEFT_REPEATED)
                    RecordingAndReplay.Instance.OffsetFrames(10);
            };
            ButtonW pause = CreateControlButton("■");
            pause.OnLeftClick = () =>
            {
                if (!OfflineGameMods.Instance.Paused())
                    OfflineGameMods.Instance.Pause();
                else
                    OfflineGameMods.Instance.Unpause();
            };

            buttonRow.AddChild(jumpBack10, TASeditor.Instance.EntrySize.Value.X);
            buttonRow.AddSpace(5f);
            buttonRow.AddChild(jumpBack1, TASeditor.Instance.EntrySize.Value.X);
            buttonRow.AddSpace(5f);
            buttonRow.AddChild(pause, TASeditor.Instance.EntrySize.Value.X);
            buttonRow.AddSpace(5f);
            buttonRow.AddChild(jump1, TASeditor.Instance.EntrySize.Value.X);
            buttonRow.AddSpace(5f);
            buttonRow.AddChild(jump10, TASeditor.Instance.EntrySize.Value.X);
            AddChild(buttonRow, TASeditor.Instance.EntrySize.Value.Y);

            base.UpdateBounds(parentBounds);
        }
    }
}
