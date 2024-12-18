using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using System.Linq;
using Microsoft.Xna.Framework.Input;

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

    public class WECharInput : WEvent
    {
        public char Char;

        public WECharInput(char c)
        {
            Char = c;
        }
    }

    public class WidgetContainer
    {
        private readonly IWidget root;
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
            events.OnChar = null;

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
            if (events.OnChar != null)
            {

            }

            root.Position = new Vector2(rec.X, rec.Y) + Offset;
            root.Size = new Vector2(rec.Width, rec.Height);

            root.UpdateBounds(new Rectangle(rec.X + (int)Offset.X, rec.Y + (int)Offset.Y, rec.Width, rec.Height));

            IWidget hovered = root.GetHovered(mousePos, true);

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
        public Action<WECharInput> OnChar;
    }

    public interface IWidget
    {
        bool MouseInside { get; set; }
        bool Visible { get; set; }
        Vector2 Position { get; set; }
        Vector2 Size { get; set; }
        Vector2 Offset { get; set; }
        float Opacity { get; set; }
        bool BackgroundVisible { get; set; }
        Func<Color> BackgroundColor { get; set; }
        bool DisableInput { get; set; }
        bool Hoverable { get; set; }
        bool BackgroundVisibleHovered { get; set; }
        Func<Color> BackgroundColorHovered { get; set; }
        Action<WEMouseClick> OnClick { get; set; }

        bool Crop { get; set; }
        IEnumerable<IWidget> Children { get; }
        void UpdateInput(bool mouseInsideParent, Events events);

        void UpdateBounds(Rectangle crop);

        IWidget GetHovered(Vector2 mousePos, bool mouseInsideParent);

        void Draw(IWidget hovered, float scale, float opacity);
    }

    public abstract class Widget : IWidget
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
        public Action OnLeftClick 
        { 
            set
            {
                OnClick = wevent =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                    {
                        value();
                    }
                };
            }
        }
        public bool Crop { get; set; }

        public virtual IEnumerable<IWidget> Children => Enumerable.Empty<IWidget>();

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

        public virtual IWidget GetHovered(Vector2 mousePos, bool mouseInsideParent)
        {
            if (!DisableInput)
            {
                MouseInside = CheckMouseInside(mousePos);
            }

            foreach (IWidget child in Children)
            {
                IWidget hovered = child.GetHovered(mousePos, MouseInside && mouseInsideParent);
                if (hovered != null)
                    return hovered;
            }

            if (MouseInside && mouseInsideParent && Hoverable)
                return this;

            return null;
        }

        public virtual void Draw(IWidget hovered, float scale, float opacity)
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
        public IWidget Widget;
        public float Size;

        public LayoutChild(IWidget widget, float size)
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

        public LayoutW(EOrientation orientation)
        {
            this.orientation = orientation;
            children = new List<LayoutChild>();
        }

        public int AddChild(Widget child, float size)
        {
            children.Add(new LayoutChild(child, size));
            return children.Count - 1;
        }

        public int AddSpace(float space)
        {
            return AddChild(null, space);
        }

        public void SetSize(int index, float size)
        {
            children[index].Size = size;
        }

        public void SetSize(Widget child, float size)
        {
            children.Find(test => test.Widget == child).Size = size;
        }

        public void ClearChildren()
        {
            children.Clear();
        }

        public override IEnumerable<IWidget> Children => children.Select(child => child.Widget).Where(child => child != null);

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            float weightedSize =
                orientation == EOrientation.HORIZONTAL ?
                Size.X : Size.Y;
            float weightSum = 0f;

            int lastWeighted = -1;

            int i = 0;
            foreach (var child in children)
            {
                if (child.Size < 0)
                {
                    weightSum += -child.Size;
                    lastWeighted = i;
                }
                else
                    weightedSize -= child.Size;
                i++;
            }

            float remainingWeightedSize = weightedSize;

            float p =
                orientation == EOrientation.HORIZONTAL ?
                Position.X : Position.Y;
            i = 0;
            foreach (var child in children)
            {
                float size = child.Size;
                if (size < 0)
                {
                    if (i == lastWeighted)
                        size = remainingWeightedSize;
                    else
                    {
                        size = weightedSize * -size / weightSum;
                        remainingWeightedSize -= size;
                    }
                }
                if (child.Widget != null)
                {
                    
                    if (orientation == EOrientation.HORIZONTAL)
                    {
                        child.Widget.Position = new Vector2(p, Position.Y) + Offset;
                        child.Widget.Size = new Vector2(size, Size.Y);
                    }
                    else
                    {
                        child.Widget.Position = new Vector2(Position.X, p) + Offset;
                        child.Widget.Size = new Vector2(Size.X, size);
                    }

                    child.Widget.UpdateBounds(this.crop);
                }

                p += size;
                i++;
            }
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
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
        public IWidget Widget;
        public Vector2 Position;
        public Vector2 Size;

        public StackChild(IWidget widget, Vector2 position, Vector2 size)
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

        public void AddChild(IWidget child, Vector2 position, Vector2 size)
        {
            children.Add(new StackChild(child, position, size));
        }

        public void ClearChildren()
        {
            children.Clear();
        }

        public override IEnumerable<IWidget> Children => children.Select(child => child.Widget);

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

        public override void Draw(IWidget hovered, float scale, float opacity)
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

    public class HolderW<W> : Widget where W : class, IWidget
    {
        public W Child;

        public HolderW()
        {

        }

        public override IEnumerable<IWidget> Children => Child != null ? new[] { (IWidget)Child } : Enumerable.Empty<IWidget>();

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

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            Child?.Draw(hovered, scale, opacity * Opacity);
        }
    }

    public class TransitionW<W> : StackW where W : class, IWidget
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
            AddChild(child, Vector2.Zero, -1f * Vector2.One);
            AddChild(childFadeout, Vector2.Zero, -1f * Vector2.One);
            R = 1f;
        }

        public override IEnumerable<IWidget> Children => (R < 1f ? new[] { (IWidget)child, childFadeout } : new[] { child }).Where(child => child != null);

        private static float Ease(float r)
        {
            return 1f - (1f - r) * (1f - r);
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            float dt = (float)(Velo.RealTime - lastTime).TotalSeconds;
            lastTime = Velo.RealTime;
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
            lastTime = Velo.RealTime;
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

    public class MoveW : HolderW<IWidget>
    {
        private TimeSpan lastTime;
        public float R;
        private float speed;
        private Vector2 offset;
        private Vector2 offsetTarget;
        private Action onFinish;

        public MoveW(IWidget child)
        {
            Child = child;
            R = 1f;
        }

        private static float Ease(float r)
        {
            return 1f - (1f - r) * (1f - r);
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            float dt = (float)(Velo.RealTime - lastTime).TotalSeconds;
            lastTime = Velo.RealTime;
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

            if (Child != null)
                Child.Offset = offset * (1f - Ease(R)) + offsetTarget * Ease(R);

            base.Draw(hovered, scale, opacity);
        }

        public void MoveTo(float speed, Vector2 offset, Action onFinish = null)
        {
            this.speed = speed;
            this.offset = this.offset * (1f - Ease(R)) + offsetTarget * Ease(R);
            offsetTarget = offset;
            lastTime = Velo.RealTime;
            R = 0f;
            this.onFinish = onFinish;
        }

        public bool Moving()
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

        public override IEnumerable<IWidget> Children => root != null ? new[] { root } : Enumerable.Empty<IWidget>();

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

                float dt = (float)Velo.RealDelta.TotalSeconds;
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

        public override IWidget GetHovered(Vector2 mousePos, bool mouseInsideParent)
        {
            IWidget hovered = base.GetHovered(mousePos, mouseInsideParent);

            if (!DisableInput && MouseInside && mouseInsideParent && root.Size.Y > Size.Y &&scrollBar.Contains((int)mousePos.X, (int)mousePos.Y))
            {
                return this;
            }

            return hovered;
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
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

        public IListEntryFactory<T> Factory;
        private readonly List<Entry> entries;
        private int firstEntry;
        private bool reachedEnd = false;

        public ListW(IListEntryFactory<T> factory)
        {
            Factory = factory;
            entries = new List<Entry>();
            firstEntry = 0;
        }

        public bool EntryBackgroundVisible { get; set; }
        public Func<Color> EntryBackgroundColor1 { get; set; }
        public Func<Color> EntryBackgroundColor2 { get; set; }
        public bool EntryHoverable { get; set; }
        public Func<Color> EntryBackgroundColorHovered { get; set; }

        public override IEnumerable<IWidget> Children => entries.Select(entry => entry.Widget);

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            firstEntry = 0;
            reachedEnd = false;

            IEnumerable<T> elems = Factory.GetElems();
            int elemsCount = elems.Count();

            float y = Position.Y;
            int i = 0;
            int j = 0;
            foreach (T elem in elems)
            {
                float height = Factory.Height(elem, i);
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
                            entries.Insert(j, new Entry { Widget = Factory.Create(elem, i), Elem = elem, Index = i });
                        else if (!entries[j].Elem.Equals(elem) || entries[j].Refresh)
                            entries[j] = new Entry { Widget = Factory.Create(elem, i), Elem = elem, Index = i };
                    }
                    else
                        entries.Add(new Entry { Widget = Factory.Create(elem, i), Elem = elem, Index = i });

                    Widget widget = entries[j].Widget;
                    widget.Position = new Vector2(Position.X, y) + Offset;
                    widget.Size = new Vector2(Size.X, height);
                    widget.Hoverable = EntryHoverable;
                    widget.UpdateBounds(this.crop);
                    if (i == elemsCount - 1)
                        reachedEnd = true;

                    j++;

                }
                y += height;
                i++;

                if (i == elemsCount)
                    break;
            }
            while (entries.Count > j)
                entries.RemoveAt(entries.Count - 1);
            Size = new Vector2(Size.X, y - Position.Y);
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
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
        private readonly TextDraw textDraw;
        private bool updateTextCrop = true;
        private string text;
        private Vector2 sizePrev = Vector2.Zero;

        public LabelW(string text, CachedFont font)
        {
            textDraw = new TextDraw()
            {
                IsVisible = true,
                Align = 0.5f * Vector2.One,
                HasDropShadow = true,
                DropShadowColor = Microsoft.Xna.Framework.Color.Black,
                DropShadowOffset = Vector2.One
            };
            textDraw.SetFont(font);
            this.text = text;
            CropText = true;
        }

        public bool CropText { get; set; }

        public Vector2 Align
        {
            get => textDraw.Align;
            set => textDraw.Align = value;
        }

        public Func<Color> Color { get; set; }
        
        public string Text 
        { 
            get => text; 
            set
            {
                text = value;
                updateTextCrop = true;
            }
        }

        public Vector2 MeasureTextSize => textDraw.Font.MeasureString(Text);

        public Vector2 Padding { get; set; }

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);

            Vector2 requestedSize = textDraw.Bounds.Size / textDraw.Scale + 2 * Padding;
            Size = new Vector2(Math.Max(Size.X, requestedSize.X), Math.Max(Size.Y, requestedSize.Y));
            if (Size != sizePrev)
            {
                sizePrev = Size;
                updateTextCrop = true;
            }
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!Visible)
                return;

            if (Text == null)
                Text = "";

            if (updateTextCrop)
            {
                if (CropText)
                {
                    string[] textLines = Text.Split('\n');

                    for (int i = 0; i < textLines.Length; i++)
                    {
                        while (textDraw.Font.MeasureString(textLines[i]).X > Size.X)
                        {
                            textLines[i] = textLines[i].Substring(0, textLines[i].Length - 1);
                        }
                    }

                    textDraw.Text = string.Join("\n", textLines);
                }
                updateTextCrop = false;
            }
            textDraw.Offset = new Vector2(Position.X + Size.X * Align.X, Position.Y + Size.Y * Align.Y) + Padding + Offset;
            textDraw.Scale = Vector2.One * scale;
            if (Color != null)
                textDraw.Color = Color();
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

        public override void Draw(IWidget hovered, float scale, float opacity)
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
        public int ShownCount;

        public SelectorButtonW(IEnumerable<string> labels, int selected, CachedFont font) :
            base(EOrientation.HORIZONTAL)
        {
            buttons = new List<LabelW>();
            Selected = selected;

            int i = 0;
            foreach (string text in labels)
            {
                LabelW button = new LabelW(text, font);
                int j = i;
                button.OnLeftClick = () =>
                    {
                        if (Selected == j)
                            return;
                        Selected = j;
                        selectedRecInitX = selectedRecLeft;
                        selectedRecR = 0f;
                        OnSelect?.Invoke(Selected);
                    };
                button.Hoverable = true;
                buttons.Add(button);
                AddChild(button, 0);
                i++;
            }
            ShownCount = buttons.Count;
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
            for (int i = 0; i < ShownCount; i++)
                SetSize(i, Size.X / ShownCount); 
            
            base.UpdateBounds(crop);

            selectedRecR += 8f * (float)(Velo.RealTime - lastTime).TotalSeconds;
            lastTime = Velo.RealTime;
            if (selectedRecR > 1f)
                selectedRecR = 1f;
        }

        private static float Ease(float r)
        {
            return 1f - (1f - r) * (1f - r);
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
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

            for (int i = 0; i < ShownCount; i++)
            {
                LabelW button = buttons[i];
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
            }

            base.Draw(hovered, scale, opacity);
        }
    }

    public class TabbedW<W> : TransitionW<W> where W : class, IWidget
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

    public class DualTabbedW<W> : TransitionW<W> where W : class, IWidget
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
        public ITableEntryFactory<T> Factory;
        private readonly CachedFont font;
        private readonly LayoutW headers;
        private readonly ListW<T> list;
        private readonly ScrollW scroll;
        private readonly List<TableColumn<T>> columns;

        public TableW(CachedFont font, float headerHeight, ITableEntryFactory<T> factory) :
            base(EOrientation.VERTICAL)
        {
            Factory = factory;
            this.font = font;

            headers = new LayoutW(EOrientation.HORIZONTAL);
            columns = new List<TableColumn<T>>();
            list = new ListW<T>(this);
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

        public Action<Widget, T, int> OnLeftClickRow { get; set; }

        public Func<T, int, LayoutW, Widget> Hook { get; set; }

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
            if (OnLeftClickRow != null)
                layout.OnLeftClick = () => OnLeftClickRow(layout, elem, i);

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

        public virtual IEnumerable<T> GetElems()
        {
            return Factory.GetElems();
        }

        public virtual float Height(T elem, int i)
        {
            return Factory.Height(elem, i);
        }
    }
}
