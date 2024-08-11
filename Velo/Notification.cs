using System.Collections.Generic;
using System;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Velo
{
    public struct Notification
    {
        public string Text;
        public Color Color;
        public TimeSpan Duration;
    }

    public class Notifications : Module
    {
        private readonly Queue<Notification> notificationQueue = new Queue<Notification>();
        private TimeSpan notificationBegin = TimeSpan.Zero;
        private CachedFont font;
        private CTextDrawComponent textDraw;

        public Notifications() : base("Notifications")
        {

        }

        public static Notifications Instance = new Notifications();

        public void PushNotification(string message, Color color, TimeSpan duration)
        {
            notificationQueue.Enqueue(new Notification { Text = message, Color = color, Duration = duration });
            if (notificationQueue.Count == 1)
                notificationBegin = Velo.Time;
        }

        public void PushNotification(string message)
        {
            PushNotification(message, Color.LightGreen, TimeSpan.FromSeconds(3d));
        }

        public void PopNotification()
        {
            notificationQueue.Dequeue();
            if (notificationQueue.Count >= 1)
                notificationBegin = Velo.Time;
            textDraw = null;
        }

        public void ForceNotification(string message, Color color, TimeSpan duration)
        {
            notificationQueue.Clear();
            notificationQueue.Enqueue(new Notification { Text = message, Color = color, Duration = duration });
            notificationBegin = Velo.Time;
            textDraw = null;
        }

        public void ForceNotification(string message)
        {
            ForceNotification(message, Color.LightGreen, TimeSpan.FromSeconds(3d));
        }

        public override void PostRender()
        {
            base.PostRender();

            if (notificationQueue.Count == 0)
                return;

            TimeSpan age = Velo.Time - notificationBegin;
            if (age > notificationQueue.Peek().Duration)
                PopNotification();

            if (notificationQueue.Count == 0)
                return;

            FontCache.Get(ref font, "UI\\Font\\NotoSans-Regular.ttf:24");

            if (textDraw == null)
            {
                textDraw = new CTextDrawComponent(notificationQueue.Peek().Text, font.Font, Vector2.Zero)
                {
                    Color = notificationQueue.Peek().Color,
                    HasDropShadow = true,
                    DropShadowColor = Color.Black,
                    DropShadowOffset = Vector2.One,
                    Align = Vector2.Zero,
                    IsVisible = true
                };
                textDraw.UpdateBounds();
            }
            textDraw.Opacity = age <= notificationQueue.Peek().Duration - TimeSpan.FromSeconds(1d) ? 
                1f : 
                (float)(notificationQueue.Peek().Duration - age).TotalSeconds;
            textDraw.Scale = CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height / 1080f * Vector2.One;

            float screenWidth = Velo.GraphicsDevice.Viewport.Width;
            float screenHeight = Velo.GraphicsDevice.Viewport.Height;

            float width = textDraw.Bounds.Width;
            float height = textDraw.Bounds.Height;

            textDraw.Position = EOrientation.CENTER.GetOrigin(width, height, screenWidth, screenHeight, Vector2.Zero);
            textDraw.Offset = new Vector2(0.0f, -height / 3.0f);
            
            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            textDraw.Draw(null);
            Velo.SpriteBatch.End();
        }
    }
}
