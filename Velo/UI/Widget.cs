using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using System.Linq;

using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Linq;

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
        private readonly Widget root;
        private readonly Rectangle rec;
        private readonly Events events;
        private MouseState mousePrev;
        private MouseState mouseCurr;

        public float Opacity { get; set; }
        public Vector2 Offset { get; set; }

        public WidgetContainer(Widget root, Rectangle rec)
        {
            this.root = root;
            this.rec = rec;
            events = new Events();
            Opacity = 1f;
        }

        public void Draw()
        {
            GraphicsDevice graphicsDevice = Velo.GraphicsDevice;
            int vHeight = graphicsDevice.Viewport.Height;
            float scale = vHeight / 1080f;

            mousePrev = mouseCurr;
            mouseCurr = Mouse.GetState();
            Vector2 mousePos = new Vector2(mouseCurr.X, mouseCurr.Y) / scale;

            events.MousePos = mousePos;
            events.MouseState = mouseCurr;
            events.OnClick = null;
            events.OnScroll = null;

            root.UpdateInput(true, events);

            if (events.OnClick != null)
            {
                if (mousePrev.LeftButton == ButtonState.Released && mouseCurr.LeftButton == ButtonState.Pressed)
                    events.OnClick(new WEMouseClick(WEMouseClick.EButton.LEFT));
                if (mousePrev.MiddleButton == ButtonState.Released && mouseCurr.MiddleButton == ButtonState.Pressed)
                    events.OnClick(new WEMouseClick(WEMouseClick.EButton.MIDDLE));
                if (mousePrev.RightButton == ButtonState.Released && mouseCurr.RightButton == ButtonState.Pressed)
                    events.OnClick(new WEMouseClick(WEMouseClick.EButton.RIGHT));
            }
            if (events.OnScroll != null)
            {
                if (mousePrev.ScrollWheelValue != mouseCurr.ScrollWheelValue)
                    events.OnScroll(new WEMouseScroll(mousePrev.ScrollWheelValue - mouseCurr.ScrollWheelValue));
            }

            root.Position = new Vector2(rec.X, rec.Y) + Offset;
            root.Size = new Vector2(rec.Width, rec.Height);

            root.UpdateBounds(new Rectangle(rec.X + (int)Offset.X, rec.Y + (int)Offset.Y, rec.Width, rec.Height));

            Widget hovered = root.GetHovered(mousePos, true);

            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            root.Draw(hovered, scale, Opacity);
            Velo.SpriteBatch.End();
        }
    }

    public class Events
    {
        public Vector2 MousePos;
        public MouseState MouseState;
        public Action<WEMouseClick> OnClick;
        public Action<WEMouseScroll> OnScroll;
    }

    public abstract class Widget
    {
        public CRectangleDrawComponent recDraw;
        protected Rectangle crop;

        public bool CheckMouseInside(Vector2 mousePos)
        {
            return
                new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y).
                Contains((int)mousePos.X, (int)mousePos.Y);
        }

        public Widget()
        {
            Visible = true;
            recDraw = new CRectangleDrawComponent(0, 0, 0, 0)
            {
                IsVisible = true
            };
            Opacity = 1f;
        }

        public bool MouseInside { get; set; }
        public bool Visible { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public Vector2 Offset { get; set; }
        public float Opacity { get; set; }
        public bool BackgroundVisible { get; set; }
        public Func<Color> BackgroundColor { get; set; }
        public bool DisableInput { get; set; }
        public bool Hoverable { get; set; }
        public bool BackgroundVisibleHovered { get; set; }
        public Func<Color> BackgroundColorHovered { get; set; }
        public Action<WEMouseClick> OnClick { get; set; }
        public bool Crop { get; set; }

        public virtual IEnumerable<Widget> Children => Enumerable.Empty<Widget>();

        public virtual void UpdateInput(bool mouseInsideParent, Events events)
        {
            if (!DisableInput)
            {
                MouseInside = CheckMouseInside(events.MousePos);

                if (MouseInside && mouseInsideParent && OnClick != null)
                {
                    events.OnClick = OnClick;
                }
            }

            Children.ForEach(child => child.UpdateInput(MouseInside && mouseInsideParent, events));
        }

        public virtual void UpdateBounds(Rectangle crop)
        {
            if (!Crop)
                this.crop = crop;
            else
                this.crop = Rectangle.Intersect(crop, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y));
        }

        public virtual Widget GetHovered(Vector2 mousePos, bool mouseInsideParent)
        {
            if (!DisableInput)
            {
                MouseInside = CheckMouseInside(mousePos);
            }

            foreach (Widget child in Children)
            {
                Widget hovered = child.GetHovered(mousePos, MouseInside && mouseInsideParent);
                if (hovered != null)
                    return hovered;
            }

            if (MouseInside && mouseInsideParent && Hoverable)
                return this;

            return null;
        }

        public virtual void Draw(Widget hovered, float scale, float opacity)
        {
            if (!Visible)
                return;

            GraphicsDevice graphicsDevice = CEngine.CEngine.Instance.GraphicsDevice;
            int vWidth = graphicsDevice.Viewport.Width;
            int vHeight = graphicsDevice.Viewport.Height;
            Rectangle scaledCrop = new Rectangle(
                (int)(crop.X * scale), 
                (int)(crop.Y * scale), 
                (int)(crop.Width * scale), 
                (int)(crop.Height * scale)
            );
            scaledCrop = Rectangle.Intersect(scaledCrop, new Rectangle(0, 0, vWidth, vHeight));
            if (graphicsDevice.ScissorRectangle != scaledCrop)
            {
                Velo.SpriteBatch.End();

                RasterizerState state = new RasterizerState
                {
                    CullMode = RasterizerState.CullCounterClockwise.CullMode,
                    DepthBias = RasterizerState.CullCounterClockwise.DepthBias,
                    FillMode = RasterizerState.CullCounterClockwise.FillMode,
                    MultiSampleAntiAlias = RasterizerState.CullCounterClockwise.MultiSampleAntiAlias,
                    ScissorTestEnable = true,
                    SlopeScaleDepthBias = RasterizerState.CullCounterClockwise.SlopeScaleDepthBias
                };

                graphicsDevice.ScissorRectangle = scaledCrop;

                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, state, CEffect.None.Effect);
            }

            recDraw.SetPositionSize((Position + Offset) * scale, Size * scale);

            if (!Hoverable || this != hovered)
            {
                recDraw.FillEnabled = BackgroundVisible;
                if (BackgroundColor != null)
                    recDraw.FillColor = BackgroundColor() * opacity * Opacity;
                recDraw.OutlineEnabled = false;
                recDraw.OutlineThickness = 0;
            }
            else
            {
                recDraw.FillEnabled = BackgroundVisibleHovered;
                if (BackgroundColorHovered != null)
                    recDraw.FillColor = BackgroundColorHovered() * opacity * Opacity;
                recDraw.OutlineEnabled = false;
                recDraw.OutlineThickness = 0;
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

        private readonly EOrientation orientation;
        private readonly List<LayoutChild> children;
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

        public override IEnumerable<Widget> Children => children.Select(child => child.Widget).Where(child => child != null);

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

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

                    child.Widget.UpdateBounds(this.crop);
                }

                p += child.Size;
            }
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

             if (!Visible)
                return;

            foreach (var child in children)
            {
                if (child.Widget == null)
                    continue;

                child.Widget.Draw(hovered, scale, opacity * Opacity);
            }
        }
    }

    public class StackChild
    {
        public Widget Widget;
        public Vector2 Position;
        public Vector2 Size;

        public StackChild(Widget widget, Vector2 position, Vector2 size)
        {
            Widget = widget;
            Position = position;
            Size = size;
        }
    }

    public class StackW : Widget
    {
        private readonly List<StackChild> children;

        public StackW()
        {
            children = new List<StackChild>();
        }

        public void AddChild(Widget child, Vector2 position, Vector2 size)
        {
            children.Add(new StackChild(child, position, size));
        }

        public void ClearChildren()
        {
            children.Clear();
        }

        public override IEnumerable<Widget> Children => children.Select(child => child.Widget);

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            foreach (var child in children)
            {
                child.Widget.Position = child.Position + Position + Offset;
                child.Widget.Size = new Vector2(child.Size.X != -1 ? child.Size.X : Size.X, child.Size.Y != -1 ? child.Size.Y : Size.Y);
                   
                child.Widget.UpdateBounds(this.crop);
            }
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            foreach (var child in children)
            {
                child.Widget.Draw(hovered, scale, opacity * Opacity);
            }
        }
    }

    public class HolderW<W> : Widget where W : Widget
    {
        public W Child;

        public HolderW()
        {

        }

        public override IEnumerable<Widget> Children => Child != null ? new[] { Child } : Enumerable.Empty<Widget>();

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            if (Child != null)
            {
                Child.Position = Position + Offset;
                Child.Size = Size;
                Child.UpdateBounds(this.crop);
            }
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            Child?.Draw(hovered, scale, opacity * Opacity);
        }
    }

    public class TransitionW<W> : StackW where W : Widget
    {
        private readonly HolderW<W> child;
        private readonly HolderW<W> childFadeout;
        private TimeSpan lastTime;
        public float R;
        private float speed;
        private Vector2 offset;
        private Vector2 offsetFadeout;
        private Action onFinish;

        public W Child => child.Child;

        public TransitionW()
        {
            child = new HolderW<W>();
            childFadeout = new HolderW<W>
            {
                DisableInput = true
            };
            AddChild(child, Vector2.Zero, -1 * Vector2.One);
            AddChild(childFadeout, Vector2.Zero, -1 * Vector2.One);
        }

        public override IEnumerable<Widget> Children => (R < 1f ? new[] { child, childFadeout } : new[] { child }).Where(child => child != null);

        private static float Ease(float r)
        {
            return 1f - (1f - r) * (1f - r);
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            float dt = (float)(Velo.Time - lastTime).TotalSeconds;
            lastTime = Velo.Time;
            if (dt > 1f)
                dt = 1f;

            R += speed * dt;
            if (R > 1f)
            {
                R = 1f;
                if (onFinish != null)
                {
                    onFinish();
                    onFinish = null;
                }
            }

            child.Opacity = Ease(R);
            childFadeout.Opacity = 1f - Ease(R);
            child.Offset = offset * (1f - Ease(R));
            childFadeout.Offset = offsetFadeout * Ease(R);

            if (R == 1f)
                childFadeout.Child = null;

            base.Draw(hovered, scale, opacity);
        }

        public void TransitionTo(W widget, float speed, Vector2 offset, bool opposite = false, Action onFinish = null)
        {
            this.speed = speed;
            this.offset = offset;
            offsetFadeout = (opposite ? -1f : 1f) * offset; 
            bool transitionBack = childFadeout.Child == widget;
            childFadeout.Child = child.Child;
            child.Child = widget;
            lastTime = Velo.Time;
            R = transitionBack ? 1f - R : 0f;
            child.Opacity = R;
            childFadeout.Opacity = 1f - R;
            child.Offset = offset * (1 - R);
            childFadeout.Offset = offsetFadeout * R;
            this.onFinish = onFinish;
        }

        public void GoTo(W widget)
        {
            childFadeout.Child = null;
            child.Child = widget;
            R = 1f;
            child.Opacity = 1f;
            childFadeout.Opacity = 0f;
        }

        public bool Transitioning()
        {
            return R < 1f;
        }
    }

    public class ScrollW : Widget
    {
        private readonly Widget root;
        private float scroll = 0f;
        private float targetScroll = 0f;
        private Rectangle scrollBar;
        private bool scrollBarPicked = false;
        private float scrollBarPickY;
        private float scrollBarPickScroll;
        private float mouseY;

        public ScrollW(Widget root)
        {
            this.root = root;
            Crop = true;
        }

        public Func<Color> ScrollBarColor { get; set; }
        public int ScrollBarWidth { get; set; }

        public override IEnumerable<Widget> Children => root != null ? new[] { root } : Enumerable.Empty<Widget>();

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);

            if (DisableInput || events.MouseState.LeftButton == ButtonState.Released)
                scrollBarPicked = false;

            root?.UpdateInput(MouseInside && mouseInsideParent, events);
            
            if (!DisableInput && MouseInside && mouseInsideParent && root.Size.Y > Size.Y)
            {
                events.OnScroll = wevent =>
                {
                    targetScroll += wevent.Amount;
                };
                if (scrollBar.Contains((int)events.MousePos.X, (int)events.MousePos.Y))
                {
                    events.OnClick = wevent =>
                    {
                        scrollBarPicked = true;
                        scrollBarPickY = events.MousePos.Y;
                        scrollBarPickScroll = scroll;
                    };
                }
            }

            mouseY = events.MousePos.Y;
        }

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            if (!scrollBarPicked)
            {
                if (targetScroll > root.Size.Y - Size.Y)
                {
                    targetScroll = root.Size.Y - Size.Y;
                }
                if (targetScroll < 0)
                {
                    targetScroll = 0;
                }
                if (scroll > root.Size.Y - Size.Y)
                {
                    scroll = root.Size.Y - Size.Y;
                }
                if (scroll < 0)
                {
                    scroll = 0;
                }

                float dt = (float)Velo.Delta.TotalSeconds;
                if (dt > 1f)
                    dt = 1f;

                if (scroll != targetScroll)
                {
                    if (scroll < targetScroll)
                        scroll = Math.Min(scroll + dt * 3000.0f, targetScroll);
                    else
                        scroll = Math.Max(scroll - dt * 3000.0f, targetScroll);
                }
            }
            else
            {
                scroll = scrollBarPickScroll + (mouseY - scrollBarPickY) * root.Size.Y / Size.Y;
                if (scroll > root.Size.Y - Size.Y)
                {
                    scroll = root.Size.Y - Size.Y;
                }
                if (scroll < 0)
                {
                    scroll = 0;
                }
                targetScroll = scroll;
            }

            if (root.Size.Y > Size.Y)
            {
                scrollBar = new Rectangle((int)(Position.X + Size.X - ScrollBarWidth), (int)(Position.Y + Size.Y * scroll / root.Size.Y), ScrollBarWidth, (int)(Size.Y * Size.Y / root.Size.Y));
            }
            else
            {
                scrollBar = new Rectangle();
            }

            root.Position = new Vector2(Position.X, Position.Y - scroll) + Offset;
            root.Size = new Vector2(Size.X, root.Size.Y);
            root.UpdateBounds(this.crop);
        }

        public override Widget GetHovered(Vector2 mousePos, bool mouseInsideParent)
        {
            Widget hovered = base.GetHovered(mousePos, mouseInsideParent);

            if (!DisableInput && MouseInside && mouseInsideParent && root.Size.Y > Size.Y &&scrollBar.Contains((int)mousePos.X, (int)mousePos.Y))
            {
                return this;
            }

            return hovered;
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            root.Draw(hovered, scale, opacity * Opacity);

            CRectangleDrawComponent scrollBarRec = new CRectangleDrawComponent(scrollBar.X * scale, scrollBar.Y * scale, scrollBar.Width * scale, scrollBar.Height * scale)
            {
                IsVisible = true,
                FillEnabled = true,
                OutlineEnabled = false,
                OutlineThickness = 0
            };
            if (ScrollBarColor != null)
                scrollBarRec.FillColor = ScrollBarColor() * opacity * Opacity;

            base.Draw(hovered, scale, 0f); // restore scissor rectangle
            scrollBarRec.Draw(null);
        }

        public void ResetScrollState()
        {
            scroll = 0f;
            targetScroll = 0f;
        }
    }

    public interface IListEntryFactory<T>
    {
        IEnumerable<T> GetElems();
        float Height(T elem, int i);
        Widget Create(T elem, int i);
    }

    public class ListW<T> : Widget where T : struct
    {
        private struct Entry
        {
            public Widget Widget;
            public T Elem;
            public int Index;
            public bool Refresh;
        }

        private readonly IListEntryFactory<T> factory;
        private readonly List<Entry> entries;
        private int firstEntry;
        private bool reachedEnd = false;

        public int EntryCount { get; set; }

        public ListW(int entryCount, IListEntryFactory<T> factory)
        {
            this.factory = factory;
            entries = new List<Entry>();
            firstEntry = 0;
            EntryCount = entryCount;
        }

        public bool EntryBackgroundVisible { get; set; }
        public Func<Color> EntryBackgroundColor1 { get; set; }
        public Func<Color> EntryBackgroundColor2 { get; set; }
        public bool EntryHoverable { get; set; }
        public Func<Color> EntryBackgroundColorHovered { get; set; }

        public override IEnumerable<Widget> Children => entries.Select(entry => entry.Widget);

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            firstEntry = 0;
            reachedEnd = false;

            IEnumerable<T> elems = factory.GetElems();

            float y = Position.Y;
            int i = 0;
            int j = 0;
            foreach (T elem in elems)
            {
                float height = factory.Height(elem, i);
                if (y + height <= crop.Top)
                {
                    firstEntry++;
                }
                else if (y <= crop.Bottom)
                {
                    while (entries.Count > j && entries[j].Index < i)
                        entries.RemoveAt(j);

                    if (entries.Count > j)
                    {
                        if (entries[j].Index > i)
                            entries.Insert(j, new Entry { Widget = factory.Create(elem, i), Elem = elem, Index = i });
                        else if (!entries[j].Elem.Equals(elem) || entries[j].Refresh)
                            entries[j] = new Entry { Widget = factory.Create(elem, i), Elem = elem, Index = i };
                    }
                    else
                        entries.Add(new Entry { Widget = factory.Create(elem, i), Elem = elem, Index = i });

                    Widget widget = entries[j].Widget;
                    widget.Position = new Vector2(Position.X, y) + Offset;
                    widget.Size = new Vector2(Size.X, height);
                    widget.Hoverable = EntryHoverable;
                    widget.UpdateBounds(this.crop);
                    if (i == EntryCount - 1)
                        reachedEnd = true;

                    j++;

                }
                y += height;
                i++;

                if (i == EntryCount)
                    break;
            }
            while (entries.Count > j)
                entries.RemoveAt(entries.Count - 1);
            Size = new Vector2(Size.X, y - Position.Y);
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            int i = firstEntry;
            foreach (Entry entry in entries)
            {
                entry.Widget.BackgroundVisible = EntryBackgroundVisible;
                if (EntryBackgroundVisible)
                {
                    if (i % 2 == 0)
                        entry.Widget.BackgroundColor = EntryBackgroundColor1;
                    else
                        entry.Widget.BackgroundColor = EntryBackgroundColor2;
                }
                entry.Widget.BackgroundVisibleHovered = true;
                entry.Widget.BackgroundColorHovered = EntryBackgroundColorHovered;
                entry.Widget.Draw(hovered, scale, opacity * Opacity);
                i++;
            }
        }

        public void Refresh(int index)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Index == index)
                {
                    Entry entry = entries[i];
                    entry.Refresh = true;
                    entries[i] = entry;
                }
            }
        }

        public bool ReachedEnd => reachedEnd;
    }

    public class LabelW : Widget
    {
        private readonly CTextDrawComponent textDraw;

        public LabelW(string text, CFont font)
        {
            textDraw = new CTextDrawComponent("", font, Vector2.Zero)
            {
                IsVisible = true,
                Align = 0.5f * Vector2.One,
                HasDropShadow = true,
                DropShadowColor = Microsoft.Xna.Framework.Color.Black,
                DropShadowOffset = Vector2.One
            };
            Text = text;
        }

        public Vector2 Align
        {
            get => textDraw.Align;
            set => textDraw.Align = value;
        }

        public Func<Color> Color { get; set; }
        
        public string Text { get; set; }

        public Vector2 Padding { get; set; }

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            Vector2 requestedSize = textDraw.Size + 2 * Padding;
            Size = new Vector2(Math.Max(Size.X, requestedSize.X), Math.Max(Size.Y, requestedSize.Y));
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            if (Text == null)
                Text = "";

            string[] textLines = Text.Split('\n');

            for (int i = 0; i < textLines.Length; i++)
            {
                while (textDraw.Font.MeasureString(textLines[i]).X > Size.X)
                {
                    textLines[i] = textLines[i].Substring(0, textLines[i].Length - 1);
                }
            }

            textDraw.StringText = string.Join("\n", textLines);
            textDraw.color_replace = false;
            textDraw.Offset = new Vector2(Position.X + Size.X * Align.X, Position.Y + Size.Y * Align.Y) + Padding + Offset;
            textDraw.Scale = Vector2.One * scale;
            if (this.Color != null)
                textDraw.Color = this.Color();
            textDraw.Opacity = opacity * Opacity;
            textDraw.Draw(null);
        }
    }

    public class ImageW : Widget
    {
        private readonly CImageDrawComponent imageDraw;

        public ImageW(Texture2D image)
        {
            imageDraw = new CImageDrawComponent(image != null ? new CImage(image) : null, Vector2.Zero, Vector2.Zero)
            {
                IsVisible = true
            };
        }

        public Texture2D Image
        {
            get { return imageDraw.Sprite?.Image; }
            set { imageDraw.Sprite = new CImage(value); }
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            imageDraw.color_replace = false;
            float min = Math.Min(Size.X, Size.Y);
            imageDraw.Size = new Vector2(min, min);
            imageDraw.Position = (Position + (Size - imageDraw.Size) / 2 + Offset) * scale;
            imageDraw.Scale = Vector2.One * scale;
            imageDraw.Opacity = opacity * Opacity;
            imageDraw.Draw(null);
        }
    }

    public class SelectorButtonW : LayoutW
    {
        private readonly List<LabelW> buttons;

        private TimeSpan lastTime;
        private float selectedRecR = 1f;
        private float selectedRecLeft = 0f;
        private float selectedRecInitX = 0f;

        public SelectorButtonW(IEnumerable<string> labels, int selected, CFont font) :
            base(EOrientation.HORIZONTAL)
        {
            buttons = new List<LabelW>();
            Selected = selected;

            int i = 0;
            foreach (string text in labels)
            {
                LabelW button = new LabelW(text, font);
                int j = i;
                button.OnClick = click =>
                    {
                        if (click.Button == WEMouseClick.EButton.LEFT)
                        {
                            if (Selected == j)
                                return;
                            Selected = j;
                            selectedRecInitX = selectedRecLeft;
                            selectedRecR = 0f;
                            OnSelect?.Invoke(Selected);
                        }
                    };
                button.Hoverable = true;
                buttons.Add(button);
                AddChild(button, 0);
                i++;
            }
        }

        public Action<int> OnSelect { get; set; }
        public Func<Color> Color { get; set; }
        public bool ButtonBackgroundVisible { get; set; }
        public Func<Color> ButtonBackgroundColor { get; set; }
        public bool ButtonOutlineVisible { get; set; }
        public int ButtonOutlineThickness { get; set; }
        public Func<Color> ButtonOutlineColor { get; set; }
        public bool ButtonBackgroundVisibleHovered { get; set; }
        public Func<Color> ButtonBackgroundColorHovered { get; set; }
        public bool ButtonBackgroundVisibleSelected { get; set; }
        public Func<Color> ButtonBackgroundColorSelected { get; set; }
        public int Selected { get; set; }
        public int Count => buttons.Count;

        public override void UpdateBounds(Rectangle crop)
        {
            for (int i = 0; i < buttons.Count; i++)
                SetSize(i, Size.X / buttons.Count); 
            
            base.UpdateBounds(crop);

            selectedRecR += 8f * (float)(Velo.Time - lastTime).TotalSeconds;
            lastTime = Velo.Time;
            if (selectedRecR > 1f)
                selectedRecR = 1f;
        }

        private static float Ease(float r)
        {
            return 1f - (1f - r) * (1f - r);
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            CRectangleDrawComponent recDraw = new CRectangleDrawComponent(0f, 0f, 0f, 0f)
            {
                IsVisible = true,
                FillEnabled = true,
                OutlineEnabled = false,
                OutlineThickness = 0
            };

            float selectedRecTargetLeft = buttons[Selected].Position.X + buttons[Selected].Offset.X;
            selectedRecLeft = (1f - Ease(selectedRecR)) * selectedRecInitX + Ease(selectedRecR) * selectedRecTargetLeft;
            float selectedRecRight = selectedRecLeft + buttons[Selected].Size.X;

            recDraw.SetPositionSize(new Vector2(selectedRecLeft, Position.Y + Offset.Y) * scale, new Vector2(buttons[Selected].Size.X, Size.Y) * scale);
            recDraw.FillColor = ButtonBackgroundColorSelected() * opacity * Opacity;
            recDraw.Draw(null);

            int i = 0;
            foreach (LabelW button in buttons)
            {
                button.Color = Color;
                float recLeft = button.Position.X + button.Offset.X;
                float recRight = recLeft + button.Size.X;

                if (recLeft >= selectedRecLeft && recLeft <= selectedRecRight)
                    recLeft = selectedRecRight;
                if (recRight >= selectedRecLeft && recRight <= selectedRecRight)
                    recRight = selectedRecLeft;

                if (recLeft > recRight)
                    recRight = recLeft;

                recDraw.SetPositionSize(new Vector2(recLeft, Position.Y + Offset.Y) * scale, new Vector2(recRight - recLeft, Size.Y) * scale);

                if (button != hovered)
                    recDraw.FillColor = ButtonBackgroundColor() * opacity * Opacity;
                else
                    recDraw.FillColor = ButtonBackgroundColorHovered() * opacity * Opacity;
                
                recDraw.Draw(null);
                i++;
            }

            base.Draw(hovered, scale, opacity);
        }
    }

    public class TabbedW<W> : TransitionW<W> where W : Widget
    {
        private readonly W[] tabs;
        private readonly SelectorButtonW selector;

        private int selectedPrev;
        private int selected;

        public TabbedW(SelectorButtonW selector)
        {
            tabs = new W[selector.Count];
            this.selector = selector;
            selected = selector.Selected;
            selectedPrev = selected;
        }

        public void SetTab(int i, W tab)
        {
            tabs[i] = tab;

            if (i == selected)
                GoTo(tab);
        }

        public IEnumerable<W> Tabs => tabs;
        public Action<W> OnSwitch { get; set; }

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            if (selected != selector.Selected)
            {
                selectedPrev = selected;
                selected = selector.Selected;
                float s = selected > selectedPrev ? 1f : -1f;
                TransitionTo(tabs[selected], 8f, s * new Vector2(Size.X, 0), opposite: true);
                OnSwitch?.Invoke(tabs[selected]);
            }
        }

        public W Current => tabs[selected];
    }

    public class DualTabbedW<W> : TransitionW<W> where W : Widget
    {
        private readonly W[] tabs;
        private readonly SelectorButtonW selector1;
        private readonly SelectorButtonW selector2;

        private int selectedPrev;
        private int selected;

        public DualTabbedW(SelectorButtonW selector1, SelectorButtonW selector2)
        {
            tabs = new W[selector1.Count * selector2.Count];
            this.selector1 = selector1;
            this.selector2 = selector2;
            selected = selector1.Selected * selector2.Count + selector2.Selected;
            selectedPrev = selected;
        }

        public void SetTab(int i1, int i2, W tab)
        {
            tabs[i1 * selector2.Count + i2] = tab;

            if (i1 * selector2.Count + i2 == selected)
                GoTo(tab);
        }

        public IEnumerable<W> Tabs => tabs;
        public Action<W> OnSwitch { get; set; }

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            if (selected != selector1.Selected * selector2.Count + selector2.Selected)
            {
                selectedPrev = selected;
                selected = selector1.Selected * selector2.Count + selector2.Selected;
                float s = selected > selectedPrev ? 1f : -1f;
                TransitionTo(tabs[selected], 8f, s * new Vector2(Size.X, 0), opposite: true);
                OnSwitch?.Invoke(tabs[selected]);
            }
        }

        public W Current => tabs[selected];
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

    public interface ITableEntryFactory<T>
    {
        IEnumerable<T> GetElems();
        float Height(T elem, int i);
    }

    public class TableColumn<T>
    {
        public string Header;
        public float Size;
        public Func<T, Widget> Factory;

        public TableColumn(string header, float size, Func<T, Widget> factory)
        {
            Header = header;
            Size = size;
            Factory = factory;
        }
    }

    public class TableW<T> : LayoutW, IListEntryFactory<T> where T : struct
    {
        private readonly ITableEntryFactory<T> factory;
        private readonly CFont font;
        private readonly LayoutW headers;
        private readonly ListW<T> list;
        private readonly ScrollW scroll;
        private readonly List<TableColumn<T>> columns;

        public TableW(CFont font, int entryCount, float headerHeight, ITableEntryFactory<T> factory) :
            base(EOrientation.VERTICAL)
        {
            this.factory = factory;
            this.font = font;

            headers = new LayoutW(EOrientation.HORIZONTAL);
            columns = new List<TableColumn<T>>();
            list = new ListW<T>(entryCount, this);
            scroll = new ScrollW(list);

            AddChild(headers, headerHeight);
            AddChild(scroll, FILL);
        }

        public Vector2 HeaderAlign { get; set; }
        public Func<Color> HeaderColor { get; set; }
        public bool EntryBackgroundVisible
        {
            get => list.EntryBackgroundVisible;
            set => list.EntryBackgroundVisible = value;
        }
        public Func<Color> EntryBackgroundColor1
        {
            get => list.EntryBackgroundColor1;
            set => list.EntryBackgroundColor1 = value;
        }
        public Func<Color> EntryBackgroundColor2
        {
            get => list.EntryBackgroundColor2;
            set => list.EntryBackgroundColor2 = value;
        }
        public bool EntryHoverable
        {
            get => list.EntryHoverable;
            set => list.EntryHoverable = value;
        }
        public Func<Color> EntryBackgroundColorHovered
        {
            get => list.EntryBackgroundColorHovered;
            set => list.EntryBackgroundColorHovered = value;
        }
        public Func<Color> ScrollBarColor
        {
            get => scroll.ScrollBarColor;
            set => scroll.ScrollBarColor = value;
        }
        public int ScrollBarWidth
        {
            get => scroll.ScrollBarWidth;
            set => scroll.ScrollBarWidth = value;
        }

        public void AddColumn(string header, float size, Func<T, Widget> factory)
        {
            LabelW headerLabel = new LabelW(header, font)
            {
                Align = HeaderAlign,
                Color = HeaderColor
            };
            headers.AddChild(headerLabel, size);
            columns.Add(new TableColumn<T>(header, size, factory));
        }

        public new void AddSpace(float space)
        {
            headers.AddSpace(space);
            columns.Add(new TableColumn<T>("", space, null));
        }

        public int RowCount
        {
            get => list.EntryCount;
            set => list.EntryCount = value;
        }

        public Action<WEMouseClick, Widget, T, int> OnClickRow { get; set; }

        public Func<T, int, LayoutW, Widget> Hook { get; set; }

        public IEnumerable<T> GetElems()
        {
            return factory.GetElems();
        }

        public float Height(T elem, int i)
        {
            return factory.Height(elem, i);
        }

        public Widget Create(T elem, int i)
        {
            LayoutW layout = new LayoutW(EOrientation.HORIZONTAL);
            foreach (var column in columns)
            {
                if (column.Factory != null)
                    layout.AddChild(column.Factory(elem), column.Size);
                else
                    layout.AddSpace(column.Size);
            }
            if (OnClickRow != null)
                layout.OnClick = (wevent) => OnClickRow(wevent, layout, elem, i);

            if (Hook != null)
                return Hook(elem, i, layout);

            return layout;
        }

        public bool ReachedEnd => list.ReachedEnd;
        
        public void ResetScrollState()
        {
            scroll.ResetScrollState();
        }

        public void Refresh(int index)
        {
            list.Refresh(index);
        }
    }
}
