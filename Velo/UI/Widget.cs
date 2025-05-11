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
            LEFT, MIDDLE, RIGHT, LEFT_REPEATED
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

    public class WEKeyPress : WEvent
    {
        public ushort Key;

        public WEKeyPress(ushort key)
        {
            Key = key;
        }
    }

    public class WidgetContainer
    {
        private readonly IWidget root;
        private readonly Rectangle rec;
        private readonly Events events;
        private MouseState mousePrev;
        private MouseState mouseCurr;
        private KeyboardState keyboardPrev;
        private KeyboardState keyboardCurr;
        private AutorepeatContext arLeft;
        private readonly Queue<char> charInputQueue = new Queue<char>();

        public float Opacity { get; set; }
        public Vector2 Offset { get; set; }

        private void InputChar(char c)
        {
            charInputQueue.Enqueue(c);
        }

        public WidgetContainer(Widget root, Rectangle rec)
        {
            this.root = root;
            this.rec = rec;
            events = new Events();
            Opacity = 1f;
            TextInputEXT.TextInput += InputChar;
            arLeft = new AutorepeatContext(true);
        }

        ~WidgetContainer()
        {
            TextInputEXT.TextInput -= InputChar;
        }

        private struct UnfocusAllVisitor : IWidgetVisitor
        {
            public bool Visit(IWidget widget)
            {
                widget.Focused = false;
                return true;
            }
        }

        private struct GetHoveredVisitor : IWidgetVisitor
        {
            public Vector2 MousePos;
            public bool MouseInsideParent;
            public Action<IWidget> SetHovered;

            public bool Visit(IWidget widget)
            {
                bool mouseInside = MouseInsideParent && widget.CheckMouseInside(MousePos);
                MouseInsideParent = mouseInside;

                if (mouseInside && widget.Hoverable)
                    SetHovered(widget);

                return mouseInside;
            }
        }

        public void Draw()
        {
            GraphicsDevice graphicsDevice = Velo.GraphicsDevice;
            int vWidth = graphicsDevice.Viewport.Width;
            int vHeight = graphicsDevice.Viewport.Height;
            float scale = vHeight / 1080f;

            mousePrev = mouseCurr;
            mouseCurr = Mouse.GetState();
            Vector2 mousePos = new Vector2(mouseCurr.X, mouseCurr.Y) / scale;

            keyboardPrev = keyboardCurr;
            keyboardCurr = Keyboard.GetState();

            events.MousePos = mousePos;
            events.MouseState = mouseCurr;
            events.KeyboardState = keyboardCurr;
            events.OnClick = null;
            events.OnScroll = null;
            events.OnChar = null;

            root.UpdateInput(true, events);

            if (events.OnClick != null)
            {
                AutorepeatEvent leftEvent = arLeft.PressEvent(mouseCurr.LeftButton == ButtonState.Pressed);
                if (leftEvent != AutorepeatEvent.NONE)
                {
                    root.Visit(new UnfocusAllVisitor(), true);
                    if (leftEvent == AutorepeatEvent.PRESS)
                        events.OnClick(new WEMouseClick(WEMouseClick.EButton.LEFT));
                    else if (leftEvent == AutorepeatEvent.REPEAT)
                        events.OnClick(new WEMouseClick(WEMouseClick.EButton.LEFT_REPEATED));
                }
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
                while (charInputQueue.Count > 0)
                    events.OnChar(new WECharInput(charInputQueue.Dequeue()));
            }
            else
                charInputQueue.Clear();
            if (events.OnKey != null)
            {
                foreach (Keys key in keyboardCurr.GetPressedKeys())
                {
                    if (!keyboardPrev.IsKeyDown(key))
                        events.OnKey(new WEKeyPress((ushort)key));
                }
            }

            root.Position = new Vector2(rec.X, rec.Y) + Offset;
            root.Size = new Vector2(rec.Width, rec.Height);

            root.UpdateBounds(new Bounds { Position = root.Position, Size = root.Size });

            IWidget hovered = null;
            GetHoveredVisitor getHoveredVisitor = new GetHoveredVisitor { MousePos = events.MousePos, MouseInsideParent = true, SetHovered = h => hovered = h };
            root.Visit(getHoveredVisitor, true);

            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            root.Draw(hovered, new Rectangle(0, 0, vWidth, vHeight), scale, Opacity);
            Velo.SpriteBatch.End();
        }
    }

    public class Events
    {
        public Vector2 MousePos;
        public MouseState MouseState;
        public KeyboardState KeyboardState;

        public Action<WEMouseClick> OnClick;
        public Action<WEMouseScroll> OnScroll;
        public Action<WECharInput> OnChar;
        public Action<WEKeyPress> OnKey;
    }

    public struct Bounds
    {
        public Vector2 Position;
        public Vector2 Size;

        public Rectangle ToRec(float scale)
        {
            return new Rectangle(
                (int)(Position.X * scale), 
                (int)(Position.Y * scale), 
                (int)(Size.X * scale), 
                (int)(Size.Y * scale)
                );
        }
    }

    public interface IWidgetVisitor
    {
        bool Visit(IWidget widget);
    }

    public interface IWidget
    {
        bool Visible { get; set; }
        Vector2 Position { get; set; }
        Vector2 Size { get; set; }
        Bounds Bounds { get; }
        Vector2 Offset { get; set; }
        float Opacity { get; set; }
        bool BackgroundVisible { get; set; }
        Func<Color> BackgroundColor { get; set; }
        bool DisableInput { get; set; }
        bool Hoverable { get; set; }
        bool BackgroundVisibleHovered { get; set; }
        Func<Color> BackgroundColorHovered { get; set; }
        bool Crop { get; set; }
        bool Focused { get; set; }
        Vector2 RequestedSize { get; }
        IEnumerable<IWidget> Children { get; }
        bool CheckMouseInside(Vector2 mousePos);
        void Visit<V>(V visitor, bool visitDecorators) where V : IWidgetVisitor;
        void UpdateInput(bool mouseInsideParent, Events events);
        void UpdateBounds(Bounds parentBounds);
        void Draw(IWidget hovered, Rectangle parentCrop, float scale, float opacity);
        void Refresh();
        void Reset();
    }

    public abstract class Widget : IWidget
    {
        public CRectangleDrawComponent recDraw;
        protected Rectangle cropRec;

        public bool CheckMouseInside(Vector2 mousePos)
        {
            return
                Bounds.ToRec(1f).
                Contains((int)mousePos.X, (int)mousePos.Y);
        }

        public Rectangle GetCropRec(Rectangle parentCropRec, float scale)
        {
            return 
                Crop ?
                Rectangle.Intersect(
                    Bounds.ToRec(scale),
                    parentCropRec
                ) :
                parentCropRec;
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

        public bool Visible { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public Vector2 Offset { get; set; }
        public Bounds Bounds => new Bounds { Position = Position + Offset, Size = Size };
        public float Opacity { get; set; }
        public bool BackgroundVisible { get; set; }
        public Func<Color> BackgroundColor { get; set; }
        public bool DisableInput { get; set; }
        public bool Hoverable { get; set; }
        public bool BackgroundVisibleHovered { get; set; }
        public Func<Color> BackgroundColorHovered { get; set; }
        public bool Crop { get; set; }
        public bool Focused { get; set; }

        public abstract Vector2 RequestedSize { get; }
        public abstract IEnumerable<IWidget> Children { get; }

        public virtual void Visit<V>(V visitor, bool visitDecorators) where V : IWidgetVisitor
        {
            if (visitor.Visit(this))
                Children.ForEach(child => child.Visit(visitor, visitDecorators));
        }

        public virtual void UpdateInput(bool mouseInsideParent, Events events)
        {
            
        }

        public void UpdateInputChildren(bool mouseInsideParent, Events events)
        {
            bool mouseInside = CheckMouseInside(events.MousePos);
            
            Children.ForEach(child => child.UpdateInput(mouseInside && mouseInsideParent, events));
        }

        public virtual void UpdateBounds(Bounds parentBounds)
        {
            
        }

        protected void UpdateScissorRec(Rectangle parentCropRec, float scale)
        {
            GraphicsDevice graphicsDevice = CEngine.CEngine.Instance.GraphicsDevice;
            
            Rectangle cropRec = GetCropRec(parentCropRec, scale);

            if (graphicsDevice.ScissorRectangle != cropRec)
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

                graphicsDevice.ScissorRectangle = cropRec;

                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, state, CEffect.None.Effect);
            }
        }

        public void BaseDraw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            if (!Visible)
                return;

            UpdateScissorRec(parentCropRec, scale);

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

        public virtual void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            BaseDraw(hovered, parentCropRec, scale, opacity);
        }

        public void DrawChildren(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            foreach (var child in Children)
            {
                child.Draw(hovered, GetCropRec(parentCropRec, scale), scale, opacity * Opacity);
            }

            UpdateScissorRec(parentCropRec, scale);
        }

        public virtual void Refresh()
        {
            
        }

        public void RefreshChildren()
        {
            Children.ForEach(child => child.Refresh());
        }

        public virtual void Reset()
        {
            
        }

        public void ResetChildren()
        {
            Children.ForEach(child => child.Reset());
        }
    }

    public interface IDecoratorW<W> : IWidget where W : class, IWidget
    {
        W Child { get; set; }
    }

    public interface IDecoratorW : IDecoratorW<IWidget>
    {
        
    }

    public class DecoratedW<W> : HolderW where W : class, IWidget
    {
        private readonly List<IDecoratorW> decorators = new List<IDecoratorW>();
        public IEnumerable<IDecoratorW> Decorators => decorators;

        private W childWidget;

        public W ChildWidget
        {
            get => childWidget;
            set
            {
                childWidget = value;
                RefreshChain();
            }
        }
        
        public DecoratedW(W childWidget = null)
        {
            this.childWidget = childWidget;
        }

        private void RefreshChain()
        {
            if (decorators.Count == 0)
            {
                Child = childWidget;
                return;
            }

            Child = decorators.First();
            for (int i = 0; i < decorators.Count - 1; i++)
            {
                decorators[i].Child = decorators[i + 1];
            }
            decorators.Last().Child = childWidget;
        }

        public D GetDecorator<D>(int index = 0) where D : class, IDecoratorW
        {
            return decorators.OfType<D>().ElementAtOrDefault(index);
        }

        public int GetDecoratorCount<D>() where D : class, IDecoratorW
        {
            return decorators.OfType<D>().Count();
        }

        public D AddDecorator<D>(D decorator, int index = 0) where D : class, IDecoratorW
        {
            decorators.Insert(index, decorator);
            RefreshChain();
            return decorator;
        }

        public void RemoveDecorator(IDecoratorW decorator)
        {
            decorators.Remove(decorator);
            RefreshChain();
        }

        public D ExtractDecorator<D>(int index = 0) where D : class, IDecoratorW
        {
            D decorator = decorators.OfType<D>().ElementAtOrDefault(index);
            if (decorator != null)
            {
                decorators.Remove(decorator);
                RefreshChain();
            }
            return decorator;
        }

        public void ClearDecorators()
        {
            decorators.Clear();
            RefreshChain();
        }
    }

    public class DecoratedW : DecoratedW<IWidget>
    {
        public DecoratedW(IWidget childWidget = null) : base(childWidget)
        {
        }
    }

    public class EmptyW : Widget
    {
        public override Vector2 RequestedSize => Vector2.Zero;
        public override IEnumerable<IWidget> Children => Enumerable.Empty<IWidget>();
    }

    public class HolderW<W> : Widget where W : class, IWidget
    {
        public W Child { get; set; }

        public HolderW(W child = null)
        {
            Child = child;
        }

        public override Vector2 RequestedSize => Child != null ? Child.RequestedSize : Vector2.Zero;
        public override IEnumerable<IWidget> Children => Child != null ? new[] { Child } : Enumerable.Empty<IWidget>();

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);
            UpdateInputChildren(mouseInsideParent, events);
        }

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);

            if (Child != null)
            {
                Child.Position = Position + Offset;
                Child.Size = Size;
                Child.UpdateBounds(Bounds);
            }
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            base.Draw(hovered, parentCropRec, scale, opacity);
            DrawChildren(hovered, parentCropRec, scale, opacity);
        }
    }

    public class HolderW : HolderW<IWidget>
    {
        public HolderW(IWidget child = null) : base(child)
        {
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

        public const float FILL = -1f;
        public const float WEIGHT = -1f;
        public const float REQUESTED_SIZE = float.NegativeInfinity;

        private readonly EOrientation orientation;
        private readonly List<LayoutChild> children;

        public LayoutW(EOrientation orientation)
        {
            this.orientation = orientation;
            children = new List<LayoutChild>();
        }

        public LayoutChild AddChild(Widget child, float size = REQUESTED_SIZE)
        {
            children.Add(new LayoutChild(child, size));
            return children.Last();
        }

        public LayoutChild AddSpace(float space)
        {
            return AddChild(null, space);
        }

        public void RemoveChild(LayoutChild child)
        {
            children.Remove(child);
        }

        public LayoutChild GetChild(int index)
        {
            return children[index];
        }

        public void ClearChildren()
        {
            children.Clear();
        }

        public override Vector2 RequestedSize
        {
            get
            {
                float fixedSizeSum = 0f;
                float maxSize = 0f;
                foreach (LayoutChild child in children)
                {
                    Vector2 requestedSize =
                        child.Widget != null ?
                        child.Widget.RequestedSize :
                        Vector2.Zero;

                    if (child.Size == REQUESTED_SIZE)
                    {
                        fixedSizeSum += 
                            orientation == EOrientation.HORIZONTAL ?
                            requestedSize.X :
                            requestedSize.Y;
                    }
                    else if (child.Size >= 0f)
                    {
                        fixedSizeSum += child.Size;
                    }
                    if (child.Widget != null)
                    {
                        maxSize = Math.Max(maxSize,
                            orientation == EOrientation.HORIZONTAL ?
                            requestedSize.Y :
                            requestedSize.X);
                    }
                }
                return
                    orientation == EOrientation.HORIZONTAL ?
                    new Vector2(fixedSizeSum, maxSize) :
                    new Vector2(maxSize, fixedSizeSum);
            }
        }
        public override IEnumerable<IWidget> Children => children.Select(child => child.Widget).Where(child => child != null);

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);
            UpdateInputChildren(mouseInsideParent, events);
        }

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);

            float weightedSize =
                orientation == EOrientation.HORIZONTAL ?
                Size.X : Size.Y;
            float weightSum = 0f;

            int lastWeighted = -1;

            int i = 0;
            foreach (var child in children)
            {
                if (child.Size == REQUESTED_SIZE)
                {
                    weightedSize -= 
                        orientation == EOrientation.HORIZONTAL ? 
                        child.Widget.RequestedSize.X : 
                        child.Widget.RequestedSize.Y;
                }
                else if (child.Size < 0f)
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
                if (size == REQUESTED_SIZE)
                {
                    size =
                        orientation == EOrientation.HORIZONTAL ?
                        child.Widget.RequestedSize.X :
                        child.Widget.RequestedSize.Y;
                }
                else if (size < 0f)
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

                    child.Widget.UpdateBounds(Bounds);
                }

                p += size;
                i++;
            }
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            base.Draw(hovered, parentCropRec, scale, opacity);
            DrawChildren(hovered, parentCropRec, scale, opacity);
        }
    }

    public class HLayoutW : LayoutW
    {
        public HLayoutW() : base(EOrientation.HORIZONTAL)
        {
        }
    }

    public class VLayoutW : LayoutW
    {
        public VLayoutW() : base(EOrientation.VERTICAL)
        {
        }
    }

    public class GridW : Widget
    {
        public int Columns;
        public float RowHeight;

        private float columnWidth;
        private int rows;

        public float Padding = 5f;
        public float Gap = 5f;

        private Vector2 CurrentPos;
        private Vector2 CurrentSize;

        private readonly List<Widget> Cells = new List<Widget>();

        public GridW(int columns, float rowHeight)
        {
            Columns = columns;
            RowHeight = rowHeight;

            CurrentPos = new Vector2();
            CurrentSize = new Vector2();
        }

        public void AddCell(Widget cell)
        {
            Cells.Add(cell);
        }

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);
            UpdateInputChildren(mouseInsideParent, events);
        }

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);

            if (Columns <= 0)
            {
                Columns = 1;
            }

            columnWidth = (Size.X - 2f * Padding - (Columns - 1) * Gap) / Columns;
            CurrentPos = new Vector2(Padding, Padding) + Position;
            CurrentSize = new Vector2(columnWidth, RowHeight);

            rows = (int)Math.Ceiling((float)Cells.Count / Columns);

            foreach (var cell in Cells)
            {
                cell.Size = CurrentSize;
                cell.Position = CurrentPos;

                CurrentPos.X += columnWidth + Gap;
                if (CurrentPos.X + columnWidth > Size.X - Padding + Position.X)
                {
                    CurrentPos.X = Padding + Position.X;
                    CurrentPos.Y += RowHeight + Gap;
                }
            }
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            base.Draw(hovered, parentCropRec, scale, opacity);
            DrawChildren(hovered, parentCropRec, scale, opacity);
        }

        public override Vector2 RequestedSize => new Vector2(0, rows * RowHeight + (rows - 1) * Gap + 2 * Padding);
        public override IEnumerable<IWidget> Children => Cells;
    }

    public class StackChild
    {
        public IWidget Widget;
        public Vector2 Align;
        public Vector2 Offset;
        public Vector2 Size;

        public StackChild(IWidget widget, Vector2 align, Vector2 offset, Vector2 size)
        {
            Widget = widget;
            Align = align;
            Offset = offset;
            Size = size;
        }

        public Vector2 GetSize(Vector2 rootSize)
        {
            Vector2 size = Vector2.Zero;

            if (Size.X == float.NegativeInfinity)
                size.X = Widget.RequestedSize.X;
            else if (Size.X < 0f)
                size.X = rootSize.X * (-Size.X);
            else
                size.X = Size.X;

            if (Size.Y == float.NegativeInfinity)
                size.Y = Widget.RequestedSize.Y;
            else if (Size.Y < 0f)
                size.Y = rootSize.Y * (-Size.Y);
            else
                size.Y = Size.Y;

            return size;
        }

        public Vector2 GetPosition(Vector2 rootSize)
        {
            if (Align == Vector2.Zero)
                return Offset;
            Vector2 size = GetSize(rootSize);
            return (rootSize - size) * Align + Offset;
        }
    }

    public class StackW : Widget
    {
        private readonly List<StackChild> children;

        public const float FILL_X = -1f;
        public const float FILL_Y = -1f;
        public static readonly Vector2 FILL = Vector2.One * -1f;

        public const float REQUESTED_SIZE_X = float.NegativeInfinity;
        public const float REQUESTED_SIZE_Y = float.NegativeInfinity;
        public static readonly Vector2 REQUESTED_SIZE = Vector2.One * float.NegativeInfinity;

        public static readonly Vector2 TOP_LEFT = Vector2.Zero;
        public static readonly Vector2 TOP_RIGHT = new Vector2(1f, 0f);
        public static readonly Vector2 BOTTOM_LEFT = new Vector2(0f, 1f);
        public static readonly Vector2 BOTTOM_RIGHT = Vector2.One;

        public StackW()
        {
            children = new List<StackChild>();
        }

        public StackChild AddChild(IWidget child, Vector2 align, Vector2 offset, Vector2 size)
        {
            children.Add(new StackChild(child, align, offset, size));
            return children.Last();
        }

        public StackChild AddChild(IWidget child, Vector2 align, Vector2 offset)
        {
            return AddChild(child, align, offset, REQUESTED_SIZE);
        }

        public void ClearChildren()
        {
            children.Clear();
        }

        public override Vector2 RequestedSize
        {
            get
            {
                Vector2 max = Vector2.Zero;
                foreach (StackChild child in children)
                {
                    max = Vector2.Max(max, child.GetPosition(Size) + child.GetSize(Size));
                }
                return max;
            }
        }
        public override IEnumerable<IWidget> Children => children.Select(child => child.Widget);

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);
            UpdateInputChildren(mouseInsideParent, events);
        }

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);

            foreach (StackChild child in children)
            {
                child.Widget.Position = child.GetPosition(Size) + Position + Offset;
                child.Widget.Size = child.GetSize(Size);
                   
                child.Widget.UpdateBounds(Bounds);
            }
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            base.Draw(hovered, parentCropRec, scale, opacity);
            DrawChildren(hovered, parentCropRec, scale, opacity);
        }
    }

    public struct Ease
    {
        public float R;
        public float Speed;
        public Action OnFinish;

        public Ease(float r, float speed)
        {
            R = r;
            Speed = speed;
            OnFinish = null;
        }

        public void Start(float speed, Action onFinish = null)
        {
            R = 0f;
            Speed = speed;
            OnFinish = onFinish;
        }

        public void Reverse(float speed, Action onFinish = null)
        {
            R = 1f - R;
            Speed = speed;
            OnFinish = onFinish;
        }

        public void Finish()
        {
            R = 1f;
        }

        public float Get()
        {
            return 1f - (1f - R) * (1f - R);
        }

        public float Lerp(float start, float target)
        {
            return (1f - Get()) * start + Get() * target;
        }

        public Vector2 Lerp(Vector2 start, Vector2 target)
        {
            return (1f - Get()) * start + Get() * target;
        }

        public bool Finished()
        {
            return R == 1f;
        }

        public void Update()
        {
            float dt = (float)Velo.RealDelta.TotalSeconds;
            if (dt > 1f)
                dt = 1f;

            R += Speed * dt;

            if (R > 1f)
            {
                R = 1f;
                OnFinish?.Invoke();
                OnFinish = null;
            }
        }
    }

    public class TransitionW<W> : StackW where W : class, IWidget
    {
        public Ease Ease;
        private HolderW<W> child;
        private HolderW<W> childFadeout;
        private Vector2 childOffsetStart;
        private Vector2 childFadeoutOffsetTarget;

        public W Child => child.Child;

        public TransitionW(W child = null)
        {
            this.child = new HolderW<W>(child);
            childFadeout = new HolderW<W>()
            {
                DisableInput = true
            };
            AddChild(this.child, Vector2.Zero, Vector2.Zero, FILL);
            AddChild(childFadeout, Vector2.Zero, Vector2.Zero, FILL);
        }

        public override IEnumerable<IWidget> Children => base.Children;

        private void Update()
        {
            child.Opacity = Ease.Lerp(0f, 1f);
            child.Offset = Ease.Lerp(childOffsetStart, Vector2.Zero);
            childFadeout.Opacity = Ease.Lerp(1f, 0f);
            childFadeout.Offset = Ease.Lerp(Vector2.Zero, childFadeoutOffsetTarget);
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            BaseDraw(hovered, parentCropRec, scale, opacity);

            Ease.Update();

            Update();

            if (Ease.Finished())
                childFadeout.Child = null;

            DrawChildren(hovered, parentCropRec, scale, opacity);
        }

        public void TransitionTo(W widget, float speed, Vector2 offset, bool opposite = false, Action onFinish = null)
        {
            (child, childFadeout) = (childFadeout, child);
            child.Child = widget;

            float oppositeS = opposite ? -1f : 1f;
            childOffsetStart = offset;
            childFadeoutOffsetTarget = offset * oppositeS;

            Ease.Reverse(speed, onFinish);
            Update();

            widget?.Refresh();
        }

        public void GoTo(W widget)
        {
            childFadeout.Child = null;
            child.Child = widget;

            Ease.Finish();
            Update();

            widget?.Refresh();
        }

        public bool Transitioning()
        {
            return !Ease.Finished();
        }
    }

    public class TransitionW : TransitionW<IWidget>
    {
        public TransitionW(IWidget child = null) : base(child)
        {
        }
    }

    public class FadeW<W> : HolderW<W>, IDecoratorW<W> where W : class, IWidget
    {
        public Ease Ease;
        public float OpacityStart;
        public float OpacityTarget;
        public Vector2 OffsetStart;
        public Vector2 OffsetTarget;

        public FadeW(W child = null) :
            base(child)
        {
            Ease = new Ease(1f, 0f);
        }

        private void Update()
        {
            if (Child != null)
            {
                Child.Opacity = Ease.Lerp(OpacityStart, OpacityTarget);
                Child.Offset = Ease.Lerp(OffsetStart, OffsetTarget);
            }
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            Ease.Update();

            Update();

            base.Draw(hovered, parentCropRec, scale, opacity);
        }

        public void FadeTo(float speed, float opacity, Vector2 offset, Action onFinish = null)
        {
            OpacityStart = Ease.Lerp(OpacityStart, OpacityTarget);
            OpacityTarget = opacity;
            OffsetStart = Ease.Lerp(OffsetStart, OffsetTarget);
            OffsetTarget = offset;

            Ease.Start(speed, onFinish);
            Update();

            Child?.Refresh();
        }

        public void GoTo(float opacity, Vector2 offset)
        {
            OpacityStart = opacity;
            OpacityTarget = opacity;
            OffsetStart = offset;
            OffsetTarget = offset;

            Ease.Finish();
            Update();

            Child?.Refresh();
        }

        public bool Fading()
        {
            return !Ease.Finished();
        }
    }

    public class FadeW : FadeW<IWidget>, IDecoratorW
    {
        public FadeW(IWidget child = null) : base(child)
        {
        }
    }

    public class ClickableW<W> : HolderW<W>, IDecoratorW<W> where W : class, IWidget
    {
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

        public ClickableW(W child = null) :
            base(child)
        {

        }

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);
            
            bool mouseInside = CheckMouseInside(events.MousePos);

            if (!DisableInput)
            {
                if (mouseInside && mouseInsideParent && OnClick != null)
                {
                    events.OnClick = OnClick;
                }
            }

            UpdateInputChildren(mouseInsideParent, events);
        }
    }

    public class ClickableW : ClickableW<IWidget>, IDecoratorW
    {
        public ClickableW(IWidget child = null) : base(child)
        {
        }
    }

    public class ScrollW<W> : Widget, IDecoratorW<W> where W : class, IWidget
    {
        public enum EOrientation
        {
            HORIZONTAL, VERTICAL
        }

        public W Child { get; set; }
        private readonly EOrientation orientation;
        private readonly EmptyW scrollBar;
        private float scroll = 0f;
        private float targetScroll = 0f;
        private bool scrollBarPicked = false;
        private float scrollBarPickXY;
        private float scrollBarPickScroll;
        private float mouseXY;

        public float AutoscrollDelay = -1f;
        public float AutoscrollSpeed = 0f;
        private float waitedTime = 0f;
        private bool autoscrollState = false;

        public ScrollW(EOrientation orientation, W child = null)
        {
            Child = child;
            this.orientation = orientation;
            scrollBar = new EmptyW
            {
                BackgroundVisible = true,
                BackgroundVisibleHovered = true
            };
            Crop = true;
        }

        public Func<Color> ScrollBarColor 
        { 
            get => scrollBar.BackgroundColor; 
            set => scrollBar.BackgroundColor = value; 
        }
        public int ScrollBarWidth { get; set; }

        private Vector2 childRequestedSize = Vector2.One * float.NegativeInfinity;
        public override Vector2 RequestedSize
        {
            get
            {
                childRequestedSize = Child.RequestedSize;
                return childRequestedSize;
            }
        }
        public override IEnumerable<IWidget> Children => Child != null ? new[] { (IWidget)Child, scrollBar } : new[] { scrollBar };

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);
            UpdateInputChildren(mouseInsideParent, events);

            if (DisableInput || events.MouseState.LeftButton == ButtonState.Released)
                scrollBarPicked = false;

            bool mouseInside = CheckMouseInside(events.MousePos);

            mouseXY = orientation == EOrientation.HORIZONTAL ? events.MousePos.X : events.MousePos.Y;

            bool canScroll = 
                orientation == EOrientation.HORIZONTAL ? 
                Child.Size.X > Size.X : 
                Child.Size.Y > Size.Y;
            
            if (!DisableInput && mouseInside && mouseInsideParent && canScroll)
            {
                events.OnScroll = wevent =>
                {
                    targetScroll += wevent.Amount;
                };
                if (scrollBar.Bounds.ToRec(1f).Contains((int)events.MousePos.X, (int)events.MousePos.Y))
                {
                    events.OnClick = wevent =>
                    {
                        scrollBarPicked = true;
                        scrollBarPickXY = mouseXY;
                        scrollBarPickScroll = scroll;
                    };
                }
            }
        }

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);

            if (childRequestedSize == Vector2.One * float.NegativeInfinity)
                childRequestedSize = Child.RequestedSize;
            Vector2 requestedSize = childRequestedSize;
            childRequestedSize = Vector2.One * float.NegativeInfinity;

            float maxScroll =
                    orientation == EOrientation.HORIZONTAL ?
                    requestedSize.X - Size.X :
                    requestedSize.Y - Size.Y;

            float dt = (float)Velo.RealDelta.TotalSeconds;
            if (dt > 1f)
                dt = 1f;

            waitedTime += dt;
            if (AutoscrollDelay != -1f && maxScroll > 0f && waitedTime >= AutoscrollDelay)
            {
                if (!autoscrollState)
                {
                    scroll += AutoscrollSpeed * dt;
                    if (scroll > maxScroll)
                    {
                        scroll = maxScroll;
                        waitedTime = 0f;
                        autoscrollState = true;
                    }
                }
                else
                {
                    scroll = 0f;
                    waitedTime = 0f;
                    autoscrollState = false;
                }
                targetScroll = scroll;
            }
            else if (!scrollBarPicked)
            {
                if (targetScroll > maxScroll)
                {
                    targetScroll = maxScroll;
                }
                if (targetScroll < 0)
                {
                    targetScroll = 0;
                }
                if (scroll > maxScroll)
                {
                    scroll = maxScroll;
                }
                if (scroll < 0)
                {
                    scroll = 0;
                }

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
                float ratio =
                    orientation == EOrientation.HORIZONTAL ?
                    requestedSize.X / Size.X :
                    requestedSize.Y / Size.Y;
                scroll = scrollBarPickScroll + (mouseXY - scrollBarPickXY) * ratio;
                if (scroll > maxScroll)
                {
                    scroll = maxScroll;
                }
                if (scroll < 0)
                {
                    scroll = 0;
                }
                targetScroll = scroll;
            }

            if (maxScroll > 0f)
            {
                scrollBar.Visible = true;
                scrollBar.Hoverable = true;
                scrollBar.Position = Position + (
                    orientation == EOrientation.HORIZONTAL ?
                    new Vector2(Size.X * scroll / requestedSize.X, Size.Y - ScrollBarWidth) :
                    new Vector2(Size.X - ScrollBarWidth, Size.Y * scroll / requestedSize.Y)
                    );
                scrollBar.Size = 
                    orientation == EOrientation.HORIZONTAL ?
                    new Vector2(Size.X * Size.X / requestedSize.X, ScrollBarWidth) :
                    new Vector2(ScrollBarWidth, Size.Y * Size.Y / requestedSize.Y);
                scrollBar.UpdateBounds(Bounds);
            }
            else
            {
                scrollBar.Visible = false;
                scrollBar.Hoverable = false;
            }

            Child.Position = 
                (orientation == EOrientation.HORIZONTAL ?
                new Vector2(Position.X - scroll, Position.Y) :
                new Vector2(Position.X, Position.Y - scroll))
                + Offset;
            Child.Size = 
                orientation == EOrientation.HORIZONTAL ?
                new Vector2(requestedSize.X, Size.Y) :
                new Vector2(Size.X, requestedSize.Y);
            Child.UpdateBounds(Bounds);
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            if (!Visible)
                return; 
            
            base.Draw(hovered, parentCropRec, scale, opacity);

            scrollBar.BackgroundColorHovered = ScrollBarColor;

            DrawChildren(hovered, parentCropRec, scale, opacity);
        }

        public void ResetScrollState()
        {
            scroll = 0f;
            targetScroll = 0f;
        }

        public override void Reset()
        {
            base.Reset();
            ResetScrollState();
        }
    }

    public class HScrollW<W> : ScrollW<W> where W : class, IWidget
    {
        public HScrollW(W child) : base(EOrientation.HORIZONTAL, child)
        {
        }
    }

    public class VScrollW<W> : ScrollW<W> where W : class, IWidget
    {
        public VScrollW(W child) : base(EOrientation.VERTICAL, child)
        {
        }
    }

    public class ScrollW : ScrollW<IWidget>, IDecoratorW
    {
        public ScrollW(EOrientation orientation, IWidget child) : base(orientation, child)
        {
        }
    }

    public class HScrollW : ScrollW
    {
        public HScrollW(IWidget child) : base(EOrientation.HORIZONTAL, child)
        {
        }
    }

    public class VScrollW : ScrollW
    {
        public VScrollW(IWidget child) : base(EOrientation.VERTICAL, child)
        {
        }
    }

    public interface IListEntryFactory<T>
    {
        IEnumerable<T> GetElems();
        float Height(T elem, int i);
        IWidget Create(T elem, int i);
    }

    public class ListW<T> : Widget where T : struct
    {
        private struct Entry
        {
            public IWidget Widget;
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

        public override Vector2 RequestedSize
        {
            get
            {
                IEnumerable<T> elems = Factory.GetElems();

                float sum = 0f;
                int i = 0;
                foreach (T elem in elems)
                {
                    sum += Factory.Height(elem, i);
                    i++;
                }
                float max = 0f;
                foreach (Entry entry in entries)
                {
                    max = Math.Max(max, entry.Widget.RequestedSize.X);
                }
                return new Vector2(max, sum);
            }
        }
        public override IEnumerable<IWidget> Children => entries.Select(entry => entry.Widget);

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);
            UpdateInputChildren(mouseInsideParent, events);
        }

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);

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
                if (y + height <= parentBounds.Position.Y)
                {
                    firstEntry++;
                }
                else if (y <= parentBounds.Position.Y + parentBounds.Size.Y)
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

                    IWidget widget = entries[j].Widget;
                    widget.Position = new Vector2(Position.X, y) + Offset;
                    widget.Size = new Vector2(Size.X, height);
                    widget.Hoverable = EntryHoverable;
                    widget.UpdateBounds(Bounds);
                    if (i == elemsCount - 1)
                        reachedEnd = true;

                    j++;

                }
                y += height;
                i++;
            }
            while (entries.Count > j)
                entries.RemoveAt(entries.Count - 1);
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            base.Draw(hovered, parentCropRec, scale, opacity);
            
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
                i++;
            }

            DrawChildren(hovered, parentCropRec, scale, opacity);
        }

        public void RefreshEntry(int index)
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

        public override Vector2 RequestedSize => textDraw.Bounds.Size / textDraw.Scale + 2 * Padding;
        public override IEnumerable<IWidget> Children => Enumerable.Empty<IWidget>();

        public Vector2 MeasureTextSize => textDraw.Font.MeasureString(Text);

        public Vector2 Padding { get; set; }

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);

            if (Size != sizePrev)
            {
                sizePrev = Size;
                updateTextCrop = true;
            }
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            if (!Visible)
                return;

            base.Draw(hovered, parentCropRec, scale, opacity);

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
                else
                    textDraw.Text = Text;
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

    public class ButtonW : LabelW
    {
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

        public ButtonW(string text, CachedFont font) :
            base(text, font)
        {

        }

        public override void UpdateInput(bool mouseInsideParent, Events events)
        {
            base.UpdateInput(mouseInsideParent, events);
            
            bool mouseInside = CheckMouseInside(events.MousePos);

            if (!DisableInput)
            {
                if (mouseInside && mouseInsideParent && OnClick != null)
                {
                    events.OnClick = OnClick;
                }
            }
        }
    }

    public class ImageW : Widget
    {
        public Texture2D Image;

        public float Rotation = 0f;
        public float RotationSpeed = 0f;

        public ImageW(Texture2D image)
        {
            Image = image;
        }

        public override Vector2 RequestedSize => Image != null ? new Vector2(Image.Width, Image.Height) : Vector2.Zero;
        public override IEnumerable<IWidget> Children => Enumerable.Empty<IWidget>();

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            if (!Visible)
                return; 
            
            base.Draw(hovered, parentCropRec, scale, opacity);

            float dt = (float)Velo.RealDelta.TotalSeconds;
            if (dt > 1f)
                dt = 1f;

            Rotation += RotationSpeed * dt;

            Vector2 size = Vector2.Zero;
            if (Size.X / Size.Y > Image.Bounds.Width / (float)Image.Bounds.Height)
            {
                size.X = Image.Bounds.Width * Size.Y / Image.Bounds.Height;
                size.Y = Size.Y;
            }
            else
            {
                size.X = Size.X;
                size.Y = Image.Bounds.Height * Size.X / Image.Bounds.Width;
            }

            Vector2 position = (Position + (Size - size) / 2 + Offset) * scale;
            float drawScale = size.X / Image.Bounds.Width * scale;
            Vector2 drawOrigin = Size / 2;
            Velo.SpriteBatch.Draw(Image, position + drawOrigin * drawScale, Image.Bounds, Color.White * opacity * Opacity, Rotation, drawOrigin, drawScale, SpriteEffects.None, 0f);
        }
    }

    public class SelectorButtonW : HLayoutW
    {
        private readonly List<ButtonW> buttons;

        private Ease Ease;
        private float selectedRecLeft = 0f;
        private float selectedRecInitX = 0f;
        public int ShownCount;

        public SelectorButtonW(IEnumerable<string> labels, int selected, CachedFont font)
        {
            Ease = new Ease(1f, 0f);

            buttons = new List<ButtonW>();
            Selected = selected;

            int i = 0;
            foreach (string text in labels)
            {
                ButtonW button = new ButtonW(text, font);
                int j = i;
                button.OnLeftClick = () =>
                    {
                        if (Selected == j)
                            return;
                        Selected = j;
                        selectedRecInitX = selectedRecLeft;
                        Ease.Start(8f);
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

        public override void UpdateBounds(Bounds parentBounds)
        {
            for (int i = 0; i < ShownCount; i++)
                GetChild(i).Size = Size.X / ShownCount; 
            
            base.UpdateBounds(parentBounds);
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            if (!Visible)
                return;

            base.Draw(hovered, parentCropRec, scale, opacity);
            
            CRectangleDrawComponent recDraw = new CRectangleDrawComponent(0f, 0f, 0f, 0f)
            {
                IsVisible = true,
                FillEnabled = true,
                OutlineEnabled = false,
                OutlineThickness = 0
            };

            Ease.Update();

            float selectedRecTargetLeft = buttons[Selected].Position.X + buttons[Selected].Offset.X;
            selectedRecLeft = Ease.Lerp(selectedRecInitX, selectedRecTargetLeft);
            float selectedRecRight = selectedRecLeft + buttons[Selected].Size.X;

            recDraw.SetPositionSize(new Vector2(selectedRecLeft, Position.Y + Offset.Y) * scale, new Vector2(buttons[Selected].Size.X, Size.Y) * scale);
            recDraw.FillColor = ButtonBackgroundColorSelected() * opacity * Opacity;
            recDraw.Draw(null);

            for (int i = 0; i < ShownCount; i++)
            {
                ButtonW button = buttons[i];
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

            DrawChildren(hovered, parentCropRec, scale, opacity);
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

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);

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

        public override void Reset()
        {
            tabs.ForEach(tab => tab.Reset());
        }
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

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);

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

        public override void Reset()
        {
            tabs.ForEach(tab => tab.Reset());
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

    public class TableW<T> : VLayoutW, IListEntryFactory<T> where T : struct
    {
        public ITableEntryFactory<T> Factory;
        private readonly CachedFont font;
        private readonly HLayoutW headers;
        private readonly ListW<T> list;
        private readonly ScrollW scroll;
        private readonly List<TableColumn<T>> columns;

        public TableW(CachedFont font, float headerHeight, ITableEntryFactory<T> factory)
        {
            Factory = factory;
            this.font = font;

            headers = new HLayoutW();
            columns = new List<TableColumn<T>>();
            list = new ListW<T>(this);
            scroll = new VScrollW(list);

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

        public Action<DecoratedW<HLayoutW>, T, int> OnLeftClickRow { get; set; }

        public Func<T, int, DecoratedW<HLayoutW>, IWidget> Hook { get; set; }

        public IWidget Create(T elem, int i)
        {
            HLayoutW layout = new HLayoutW();
            DecoratedW<HLayoutW> decorated = new DecoratedW<HLayoutW>(layout);
            foreach (var column in columns)
            {
                if (column.Factory != null)
                    layout.AddChild(column.Factory(elem), column.Size);
                else
                    layout.AddSpace(column.Size);
            }
            if (OnLeftClickRow != null)
            {
                decorated.AddDecorator(new ClickableW { OnLeftClick = () => OnLeftClickRow(decorated, elem, i) });
            }

            if (Hook != null)
                return Hook(elem, i, decorated);

            return decorated;
        }

        public bool ReachedEnd => list.ReachedEnd;
        
        public void ResetScrollState()
        {
            scroll.ResetScrollState();
        }

        public void RefreshEntry(int index)
        {
            list.RefreshEntry(index);
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

    public class BackstackW<W> : TransitionW<W> where W : class, IWidget
    {
        private readonly Stack<W> backStack = new Stack<W>();

        public new void TransitionTo(W widget, float speed, Vector2 offset, bool opposite = false, Action onFinish = null)
        {
            if (Child != null)
                backStack.Push(Child);
            base.TransitionTo(widget, speed, offset, opposite, onFinish);
        }

        public void TransitionBack(float speed, Vector2 offset, bool opposite = false, Action onFinish = null)
        {
            base.TransitionTo(backStack.Pop(), speed, offset, opposite, onFinish);
        }

        public new void GoTo(W widget)
        {
            if (Child != null)
                backStack.Push(Child);
            base.GoTo(widget);
        }

        public void GoBack()
        {
            base.GoTo(backStack.Pop());
        }

        public void Push(W widget)
        {
            backStack.Push(widget);
        }

        public void PopLast()
        {
            backStack.Pop();
        }

        public void Clear()
        {
            backStack.Clear();
            base.GoTo(null);
        }
    }

    public class BackstackW : BackstackW<IWidget>
    {
    }
}
