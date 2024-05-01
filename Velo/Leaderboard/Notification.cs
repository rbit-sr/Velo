﻿using System.Collections.Generic;
using System;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Velo
{
    public class Notifications : Module
    {
        private Queue<string> notificationQueue = new Queue<string>();
        private TimeSpan notificationBegin = TimeSpan.Zero;
        private CTextDrawComponent textDraw;

        public Notifications() : base("Notifications")
        {

        }

        public static Notifications Instance = new Notifications();

        public void PushNotification(string message)
        {
            notificationQueue.Enqueue(message);
            if (notificationQueue.Count == 1)
                notificationBegin = new TimeSpan(DateTime.Now.Ticks);
        }

        public void PopNotification()
        {
            notificationQueue.Dequeue();
            if (notificationQueue.Count >= 1)
                notificationBegin = new TimeSpan(DateTime.Now.Ticks);
            textDraw = null;
        }

        public override void PostRender()
        {
            base.PostRender();

            if (notificationQueue.Count == 0)
                return;

            TimeSpan age = new TimeSpan(DateTime.Now.Ticks) - notificationBegin;
            if (age > TimeSpan.FromSeconds(3.0))
                PopNotification();

            if (notificationQueue.Count == 0)
                return;

            if (textDraw == null)
            {
                textDraw = new CTextDrawComponent(notificationQueue.Peek(), FontCache.Get("UI\\Font\\NotoSans-Regular.ttf", 24), Vector2.Zero);
                textDraw.Color = Color.LightGreen;
                textDraw.HasDropShadow = true;
                textDraw.DropShadowColor = Color.Black;
                textDraw.DropShadowOffset = Vector2.One;
                textDraw.Align = Vector2.Zero;
                textDraw.IsVisible = true;
                textDraw.UpdateBounds();
            }
            textDraw.Opacity = age.TotalSeconds <= 2.0 ? 1f : (float)(3.0 - age.TotalSeconds);

            float screenWidth = Velo.SpriteBatch.GraphicsDevice.Viewport.Width;
            float screenHeight = Velo.SpriteBatch.GraphicsDevice.Viewport.Height;

            float width = textDraw.Bounds.Width;
            float height = textDraw.Bounds.Height;

            textDraw.Position = EOrientation.CENTER.GetOrigin(width, height, screenWidth, screenHeight, Velo.PlayerPos);
            textDraw.Offset = new Vector2(0.0f, -height / 3.0f);
            
            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            textDraw.Draw(null);
            Velo.SpriteBatch.End();
        }
    }
}
