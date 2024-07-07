using System.Collections.Generic;
using System;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Velo
{
    public class Notifications : Module
    {
        private readonly Queue<string> notificationQueue = new Queue<string>();
        private TimeSpan notificationBegin = TimeSpan.Zero;
        private CachedFont font;
        private CTextDrawComponent textDraw;

        public Notifications() : base("Notifications")
        {

        }

        public static Notifications Instance = new Notifications();

        public void PushNotification(string message)
        {
            notificationQueue.Enqueue(message);
            if (notificationQueue.Count == 1)
                notificationBegin = Velo.Time;
        }

        public void PopNotification()
        {
            notificationQueue.Dequeue();
            if (notificationQueue.Count >= 1)
                notificationBegin = Velo.Time;
            textDraw = null;
        }

        public void ForceNotification(string message)
        {
            notificationQueue.Clear();
            notificationQueue.Enqueue(message);
            notificationBegin = Velo.Time;
            textDraw = null;
        }

        public override void PostRender()
        {
            base.PostRender();

            if (notificationQueue.Count == 0)
                return;

            TimeSpan age = Velo.Time - notificationBegin;
            if (age > TimeSpan.FromSeconds(3.0))
                PopNotification();

            if (notificationQueue.Count == 0)
                return;

            FontCache.Get(ref font, "UI\\Font\\NotoSans-Regular.ttf:24");

            if (textDraw == null)
            {
                textDraw = new CTextDrawComponent(notificationQueue.Peek(), font.Font, Vector2.Zero)
                {
                    Color = Color.LightGreen,
                    HasDropShadow = true,
                    DropShadowColor = Color.Black,
                    DropShadowOffset = Vector2.One,
                    Align = Vector2.Zero,
                    IsVisible = true
                };
                textDraw.UpdateBounds();
            }
            textDraw.Opacity = age.TotalSeconds <= 2.0 ? 1f : (float)(3.0 - age.TotalSeconds);
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
