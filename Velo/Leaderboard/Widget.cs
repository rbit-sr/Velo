using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using CEngine.Util.Input;
using CEngine.Util.Input.SDLInput;

namespace Velo
{
    public abstract class WEvent
    {

    }

    public class WEMouseClick : WEvent
    {
        public enum EButton
        {
            LEFT, MIDDLE, RIGHT
        }

        public EButton Button;

        public WEMouseClick(EButton button)
        {
            Button = button;
        }
    }

    public class WEMouseScroll : WEvent
    {
        public int Amount;

        public WEMouseScroll(int amount)
        {
            Amount = amount;
        }
    }

    public class WidgetContainer
    {
        private Widget root;
        private Rectangle crop;
        private Events events;

        public WidgetContainer(Widget root, Rectangle rec)
        {
            this.root = root;
            crop = rec;
            root.Position = new Vector2(rec.X, rec.Y);
            root.Size = new Vector2(rec.Width, rec.Height);
            events = new Events();
        }

        public void Draw()
        {
            CInputManager input = CEngine.CEngine.Instance.input_manager;
            Vector2 mousePos = new Vector2(input.mouse_state2.X, input.mouse_state2.Y);

            if (events.OnClick != null)
            {
                if (input.mouse_state1.LeftButton == ButtonState.Released && input.mouse_state2.LeftButton == ButtonState.Pressed)
                    events.OnClick(new WEMouseClick(WEMouseClick.EButton.LEFT));
                if (input.mouse_state1.MiddleButton == ButtonState.Released && input.mouse_state2.MiddleButton == ButtonState.Pressed)
                    events.OnClick(new WEMouseClick(WEMouseClick.EButton.MIDDLE));
                if (input.mouse_state1.RightButton == ButtonState.Released && input.mouse_state2.RightButton == ButtonState.Pressed)
                    events.OnClick(new WEMouseClick(WEMouseClick.EButton.RIGHT));
            }
            if (events.OnScroll != null)
            {
                if (input.mouse_state1.ScrollWheelValue != input.mouse_state2.ScrollWheelValue)
                    events.OnScroll(new WEMouseScroll(input.mouse_state1.ScrollWheelValue - input.mouse_state2.ScrollWheelValue));
            }

            events.MousePos = mousePos;
            events.Hovered = null;
            events.OnClick = null;
            events.OnScroll = null;
            root.Update(true, events, crop);

            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            root.Draw(events.Hovered);
            Velo.SpriteBatch.End();
        }
    }

    public class Events
    {
        public Vector2 MousePos;
        public Widget Hovered;
        public Action<WEMouseClick> OnClick;
        public Action<WEMouseScroll> OnScroll;
    }

    public abstract class Widget
    {
        public CRectangleDrawComponent recDraw;
        private Rectangle crop;

        public bool CheckMouseInside(Vector2 mousePos)
        {
            return
                new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y).
                Contains(CEngine.CEngine.Instance.input_manager.mouse_state2.X, CEngine.CEngine.Instance.input_manager.mouse_state2.Y);
        }

        public Widget()
        {
            Visible = true;
            recDraw = new CRectangleDrawComponent(0, 0, 0, 0);
            recDraw.IsVisible = true;
        }

        public bool MouseInside { get; set; }
        public bool Visible { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public Vector2 Offset { get; set; }
        public bool BackgroundVisible { get; set; }
        public Color BackgroundColor { get; set; }
        public bool OutlineVisible { get; set; }
        public int OutlineThickness { get; set; }
        public Color OutlineColor { get; set; }
        public bool Hoverable { get; set; }
        public bool BackgroundVisibleHovered { get; set; }
        public Color BackgroundColorHovered { get; set; }
        public bool OutlineVisibleHovered { get; set; }
        public int OutlineThicknessHovered { get; set; }
        public Color OutlineColorHovered { get; set; }

        public Action<WEMouseClick> OnClick { get; set; }

        public virtual void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            MouseInside = CheckMouseInside(events.MousePos);

            if (MouseInside && mouseInsideParent && OnClick != null)
            {
                events.OnClick = OnClick;
            }

            if (MouseInside && Hoverable)
            {
                events.Hovered = this;
            }

            this.crop = crop;
        }

        public virtual void Draw(Widget hovered)
        {
            if (!Visible)
                return;

            GraphicsDevice graphicsDevice = CEngine.CEngine.Instance.GraphicsDevice;
            if (graphicsDevice.ScissorRectangle != crop)
            {
                Velo.SpriteBatch.End();

                RasterizerState state = new RasterizerState();
                state.CullMode = RasterizerState.CullCounterClockwise.CullMode;
                state.DepthBias = RasterizerState.CullCounterClockwise.DepthBias;
                state.FillMode = RasterizerState.CullCounterClockwise.FillMode;
                state.MultiSampleAntiAlias = RasterizerState.CullCounterClockwise.MultiSampleAntiAlias;
                state.ScissorTestEnable = true;
                state.SlopeScaleDepthBias = RasterizerState.CullCounterClockwise.SlopeScaleDepthBias;

                graphicsDevice.ScissorRectangle = crop;

                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, state, CEffect.None.Effect);
            }

            recDraw.SetPositionSize(Position + Offset, Size);

            if (!Hoverable || this != hovered)
            {
                recDraw.FillEnabled = BackgroundVisible;
                recDraw.FillColor = BackgroundColor;
                recDraw.OutlineEnabled = OutlineVisible;
                recDraw.OutlineThickness = OutlineThickness;
                recDraw.OutlineColor = OutlineColor;
            }
            else
            {
                recDraw.FillEnabled = BackgroundVisibleHovered;
                recDraw.FillColor = BackgroundColorHovered;
                recDraw.OutlineEnabled = OutlineVisibleHovered;
                recDraw.OutlineThickness = OutlineThicknessHovered;
                recDraw.OutlineColor = OutlineColorHovered;
            }

            recDraw.IsVisible = Visible;
            recDraw.Draw(null);
        }
    }

    public class LayoutChild
    {
        public Widget Widget;
        public float Size;

        public LayoutChild(Widget widget, float size)
        {
            Widget = widget;
            Size = size;
        }
    }

    public class LayoutW : Widget
    {
        public enum EOrientation
        {
            HORIZONTAL, VERTICAL
        }

        public static readonly float FILL = -1;

        private EOrientation orientation;
        private List<LayoutChild> children;
        private int fillChild = -1;

        public LayoutW(EOrientation orientation)
        {
            this.orientation = orientation;
            children = new List<LayoutChild>();
        }

        public void AddChild(Widget child, float size)
        {
            children.Add(new LayoutChild(child, size));
            if (size == FILL)
                fillChild = children.Count - 1;
        }

        public void AddSpace(float space)
        {
            AddChild(null, space);
        }

        public void SetSize(int index, float size)
        {
            children[index].Size = size;
        }

        public void ClearChildren()
        {
            children.Clear();
            fillChild = -1;
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            if (fillChild != -1)
            {
                float fillSize =
                    orientation == EOrientation.HORIZONTAL ?
                    Size.X : Size.Y;

                int i = 0;
                foreach (var child in children)
                {
                    if (i++ == fillChild)
                        continue;
                    fillSize -= child.Size;
                }
                children[fillChild].Size = fillSize;
            }

            float p =
                orientation == EOrientation.HORIZONTAL ?
                Position.X : Position.Y;
            foreach (var child in children)
            {
                if (child.Widget != null)
                {
                    if (orientation == EOrientation.HORIZONTAL)
                    {
                        child.Widget.Position = new Vector2(p, Position.Y) + Offset;
                        child.Widget.Size = new Vector2(child.Size, Size.Y);
                    }
                    else
                    {
                        child.Widget.Position = new Vector2(Position.X, p) + Offset;
                        child.Widget.Size = new Vector2(Size.X, child.Size);
                    }

                    child.Widget.Update(MouseInside && mouseInsideParent, events, crop);
                }

                p += child.Size;
            }
        }

        public override void Draw(Widget hovered)
        {
            base.Draw(hovered);

             if (!Visible)
                return;

            foreach (var child in children)
            {
                if (child.Widget == null)
                    continue;

                child.Widget.Draw(hovered);
            }
        }
    }

    public class ScrollW : Widget
    {
        private Widget root;
        private float scroll = 0;
        private float targetScroll = 0;
        private TimeSpan lastFrameTime = TimeSpan.Zero;

        public ScrollW(Widget root)
        {
            this.root = root;
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            if (MouseInside && mouseInsideParent)
            {
                events.OnScroll = wevent =>
                    {
                        targetScroll += wevent.Amount;
                    };
            }

            if (targetScroll > root.Size.Y - Size.Y)
                targetScroll = root.Size.Y - Size.Y;
            if (targetScroll < 0)
                targetScroll = 0;

            TimeSpan now = new TimeSpan(DateTime.Now.Ticks);

            float dt = (float)(now - lastFrameTime).TotalSeconds;
            if (dt > 1f)
                dt = 1f;

            lastFrameTime = now;

            if (scroll != targetScroll)
            {
                if (scroll < targetScroll)
                    scroll = Math.Min(scroll + dt * 3000.0f, targetScroll);
                else
                    scroll = Math.Max(scroll - dt * 3000.0f, targetScroll);
            }

            root.Position = new Vector2(Position.X, Position.Y - scroll) + Offset;
            root.Size = new Vector2(Size.X, root.Size.Y);
            root.Update(MouseInside && mouseInsideParent, events, Rectangle.Intersect(crop, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y)));
        }

        public override void Draw(Widget hovered)
        {
            base.Draw(hovered);

            if (!Visible)
                return;

            root.Draw(hovered);
        }
    }

    public interface IListEntryFactory
    {
        float Height(int i);
        Widget Create(int i);
    }

    public class ListW : Widget
    {
        private IListEntryFactory factory;
        private int firstEntry;
        private List<Widget> entries;
        public int EntryCount { get; set; }

        public ListW(int entryCount, IListEntryFactory factory)
        {
            this.factory = factory;
            firstEntry = 0;
            entries = new List<Widget>();
            EntryCount = entryCount;
        }

        public bool EntryBackgroundVisible { get; set; }
        public Color EntryBackgroundColor1 { get; set; }
        public Color EntryBackgroundColor2 { get; set; }
        public bool EntryHoverable { get; set; }
        public Color EntryBackgroundColorHovered { get; set; }

        public void Refresh(int i = -1)
        {
            if (i == -1)
            {
                entries.Clear();
                firstEntry = 0;
            }
            else if (i - firstEntry >= 0 && i - firstEntry < entries.Count)
            {
                entries[i - firstEntry] = null;
            }
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            int newFirstEntry = 0;
            int newEntriesCount = 0;
            float y = Position.Y;
            for (int i = 0; i < EntryCount; i++)
            {
                float height = factory.Height(i);
                if (y + height <= crop.Top)
                {
                    newFirstEntry++;
                }
                else if (y <= crop.Bottom)
                {
                    newEntriesCount++;
                }
                y += height;
            }

            while (firstEntry > newFirstEntry)
            {
                firstEntry--;
                entries.Insert(0, factory.Create(firstEntry));
            }
            while (firstEntry < newFirstEntry)
            {
                firstEntry++;
                entries.RemoveAt(0);
            }
            while (entries.Count < newEntriesCount)
            {
                entries.Add(factory.Create(firstEntry + entries.Count));
            }
            while (entries.Count > newEntriesCount)
            {
                entries.RemoveAt(newEntriesCount);
            }

            y = Position.Y;
            for (int i = 0; i < EntryCount; i++)
            {
                float height = factory.Height(i);

                if (i >= firstEntry && i < firstEntry + entries.Count)
                {
                    if (entries[i - firstEntry] == null)
                        entries[i - firstEntry] = factory.Create(i);
                    Widget widget = entries[i - firstEntry];
                    widget.Position = new Vector2(Position.X, y) + Offset;
                    widget.Size = new Vector2(Size.X, height);
                    widget.Update(MouseInside && mouseInsideParent, events, crop);
                }
      
                y += height;
            }
            Size = new Vector2(Size.X, y - Position.Y);
        }

        public override void Draw(Widget hovered)
        {
            base.Draw(hovered);

            if (!Visible)
                return;

            int i = firstEntry;
            foreach (Widget widget in entries)
            {
                widget.BackgroundVisible = EntryBackgroundVisible;
                if (EntryBackgroundVisible)
                {
                    if (i % 2 == 0)
                        widget.BackgroundColor = EntryBackgroundColor1;
                    else
                        widget.BackgroundColor = EntryBackgroundColor2;
                }
                widget.Hoverable = EntryHoverable;
                widget.BackgroundVisibleHovered = true;
                widget.BackgroundColorHovered = EntryBackgroundColorHovered;
                widget.Draw(hovered);
                i++;
            }
        }
    }

    public class LabelW : Widget
    {
        private CTextDrawComponent textDraw;

        public LabelW(string text, CFont font)
        {
            textDraw = new CTextDrawComponent(text, font, Vector2.Zero);
            textDraw.IsVisible = true;
            textDraw.Align = 0.5f * Vector2.One;
            textDraw.HasDropShadow = true;
            textDraw.DropShadowColor = Color.Black;
            textDraw.DropShadowOffset = Vector2.One;
        }

        public Vector2 Align
        {
            get { return textDraw.Align; }
            set { textDraw.Align = value; }
        }

        public Color Color
        {
            get { return textDraw.Color; }
            set { textDraw.Color = value; }
        }

        public string Text
        {
            get { return textDraw.StringText; }
            set { textDraw.StringText = value; }
        }

        public override void Draw(Widget hovered)
        {
            base.Draw(hovered);

            if (!Visible)
                return;

            textDraw.UpdateBounds();

            textDraw.Position = new Vector2(Position.X + Size.X * Align.X, Position.Y + Size.Y * Align.Y) + Offset;
            textDraw.Draw(null);
        }
    }

    public class ImageW : Widget
    {
        private CImageDrawComponent imageDraw;

        public ImageW(Texture2D image)
        {
            imageDraw = new CImageDrawComponent(image != null ? new CImage(image) : null, Vector2.Zero, Vector2.Zero);
            imageDraw.IsVisible = true;
        }

        public Texture2D Image
        {
            get { return imageDraw.Sprite != null ? imageDraw.Sprite.Image : null; }
            set { imageDraw.Sprite = new CImage(value); }
        }

        public override void Draw(Widget hovered)
        {
            base.Draw(hovered);

            if (!Visible)
                return;

            float min = Math.Min(Size.X, Size.Y);
            imageDraw.Size = new Vector2(min, min);
            imageDraw.Position = Position + (Size - imageDraw.Size) / 2 + Offset;
            imageDraw.Draw(null);
        }
    }


    public class MultiSelectButton : LayoutW
    {
        private List<LabelW> buttons;
        private int selected;

        public MultiSelectButton(IEnumerable<string> labels, int selected, CFont font) :
            base(LayoutW.EOrientation.HORIZONTAL)
        {
            buttons = new List<LabelW>();
            selected = 0;

            int i = 0;
            foreach (string text in labels)
            {
                LabelW button = new LabelW(text, font);
                int j = i;
                button.OnClick = click =>
                    {
                        if (click.Button == WEMouseClick.EButton.LEFT)
                        {
                            if (this.selected == j)
                                return;
                            this.selected = j;
                            if (OnSelect != null)
                                OnSelect(this.selected);
                        }
                    };
                button.Hoverable = true;
                buttons.Add(button);
                AddChild(button, 0);
                i++;
            }
        }

        public Action<int> OnSelect { get; set; }
        public bool ButtonBackgroundVisible { get; set; }
        public Color ButtonBackgroundColor { get; set; }
        public bool ButtonOutlineVisible { get; set; }
        public int ButtonOutlineThickness { get; set; }
        public Color ButtonOutlineColor { get; set; }
        public bool ButtonBackgroundVisibleHovered { get; set; }
        public Color ButtonBackgroundColorHovered { get; set; }
        public bool ButtonOutlineVisibleHovered { get; set; }
        public int ButtonOutlineThicknessHovered { get; set; }
        public Color ButtonOutlineColorHovered { get; set; }
        public bool ButtonBackgroundVisibleSelected { get; set; }
        public Color ButtonBackgroundColorSelected { get; set; }
        public bool ButtonOutlineVisibleSelected { get; set; }
        public int ButtonOutlineThicknessSelected { get; set; }
        public Color ButtonOutlineColorSelected { get; set; }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            for (int i = 0; i < buttons.Count; i++)
                SetSize(i, Size.X / buttons.Count);
        }

        public override void Draw(Widget hovered)
        {
            int i = 0;
            foreach (LabelW button in buttons)
            {
                if (i != selected)
                {
                    button.BackgroundVisible = ButtonBackgroundVisible;
                    button.BackgroundColor = ButtonBackgroundColor;
                    button.OutlineVisible = ButtonOutlineVisible;
                    button.OutlineColor = ButtonOutlineColor;
                    button.OutlineThickness = ButtonOutlineThickness;
                    button.BackgroundVisibleHovered = ButtonBackgroundVisibleHovered;
                    button.BackgroundColorHovered = ButtonBackgroundColorHovered;
                    button.OutlineVisibleHovered = ButtonOutlineVisibleHovered;
                    button.OutlineColorHovered = ButtonOutlineColorHovered;
                    button.OutlineThicknessHovered = ButtonOutlineThicknessHovered;
                }
                else
                {
                    button.BackgroundVisible = ButtonBackgroundVisibleSelected;
                    button.BackgroundColor = ButtonBackgroundColorSelected;
                    button.OutlineVisible = ButtonOutlineVisibleSelected;
                    button.OutlineColor = ButtonOutlineColorSelected;
                    button.OutlineThickness = ButtonOutlineThicknessSelected;
                    button.BackgroundVisibleHovered = ButtonBackgroundVisibleSelected;
                    button.BackgroundColorHovered = ButtonBackgroundColorSelected;
                    button.OutlineVisibleHovered = ButtonOutlineVisibleSelected;
                    button.OutlineColorHovered = ButtonOutlineColorSelected;
                    button.OutlineThicknessHovered = ButtonOutlineThicknessSelected;
                }
                i++;
            }

            base.Draw(hovered);
        }
    }

    public class TableColumn
    {
        public string Header;
        public float Size;
        public Func<int, Widget> Factory;

        public TableColumn(string header, float size, Func<int, Widget> factory)
        {
            Header = header;
            Size = size;
            Factory = factory;
        }
    }

    public class TableW : LayoutW, IListEntryFactory
    {
        private CFont font;
        private LayoutW headers;
        private ListW list;
        private ScrollW scroll;
        private List<TableColumn> columns;
        
        public TableW(CFont font, int entryCount, float headerHeight, Func<int, float> entryHeight) :
            base(LayoutW.EOrientation.VERTICAL)
        {
            this.font = font;
            EntryHeight = entryHeight;

            headers = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
            columns = new List<TableColumn>();
            list = new ListW(entryCount, this);
            scroll = new ScrollW(list);

            AddChild(headers, headerHeight);
            AddChild(scroll, LayoutW.FILL);
        }

        public Func<int, float> EntryHeight { get; set; }
        public Vector2 HeaderAlign { get; set; }
        public Color HeaderColor { get; set; }
        public bool EntryBackgroundVisible 
        {
            get { return list.EntryBackgroundVisible; }
            set { list.EntryBackgroundVisible = value; }
        }
        public Color EntryBackgroundColor1
        {
            get { return list.EntryBackgroundColor1; }
            set { list.EntryBackgroundColor1 = value; }
        }
        public Color EntryBackgroundColor2
        {
            get { return list.EntryBackgroundColor2; }
            set { list.EntryBackgroundColor2 = value; }
        }
        public bool EntryHoverable
        {
            get { return list.EntryHoverable; }
            set { list.EntryHoverable = value; }
        }
        public Color EntryBackgroundColorHovered
        {
            get { return list.EntryBackgroundColorHovered; }
            set { list.EntryBackgroundColorHovered = value; }
        }

        public void AddColumn(string header, float size, Func<int, Widget> factory)
        {
            LabelW headerLabel = new LabelW(header, font);
            headerLabel.Align = HeaderAlign;
            headerLabel.Color = HeaderColor;
            headers.AddChild(headerLabel, size);
            columns.Add(new TableColumn(header, size, factory));
        }

        public void AddSpace(float space)
        {
            headers.AddSpace(space);
            columns.Add(new TableColumn("", space, null));
        }

        public int RowCount
        {
            get { return list.EntryCount; }
            set { list.EntryCount = value; }
        }

        public Action<WEMouseClick, int> OnClickRow { get; set; }

        public void Refresh(int i = -1)
        {
            list.Refresh(i);
        }

        public Func<int, LayoutW, Widget> Hook { get; set; }

        public float Height(int i)
        {
            return EntryHeight(i);
        }

        public Widget Create(int i)
        {
            LayoutW layout = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
            foreach (var column in columns)
            {
                if (column.Factory != null)
                    layout.AddChild(column.Factory(i), column.Size);
                else
                    layout.AddSpace(column.Size);
            }
            if (OnClickRow != null)
                layout.OnClick = (wevent) => OnClickRow(wevent, i);

            if (Hook != null)
                return Hook(i, layout);

            return layout;
        }
    }
}
