﻿using System;
using CEngine.Graphics.Component;
using CEngine.Graphics.Library;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Velo
{
    public class Watermark : Module
    {
        public CFont font_watermark;

        private Watermark() : base("Watermark") { }

        public static Watermark Instance = new Watermark();

        public override void PostRender()
        {
            base.PostRender();

            if (!Velo.Ingame || Velo.Online)
                return;

            if (font_watermark == null)
            {
                font_watermark = new CFont("UI\\Font\\ariblk.ttf", 24);
                Velo.ContentManager.Load(font_watermark, false);
            }

            CTextDrawComponent watermark = new CTextDrawComponent("", font_watermark, new Vector2(32, 32))
            {
                color_replace = false,
                Color = Color.Red,
                IsVisible = false
            };

            string text = "";

            if (Velo.get_time_scale() != 1.0f)
                text += "\nx" + Velo.get_time_scale();
           
            if (LocalGameModifications.Instance.IsModded() || BlindrunSimulator.Instance.Enabled.Value.Enabled)
                text += "\nmodded";

            if (TAS.Instance.dtFixed)
                text += "\nTAS";

            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (milliseconds - Savestates.Instance.savestateLoadTime <= 500)
                watermark.StringText += "\nload";

            if (text.Length > 0)
            {
                text = text.Remove(0, 1);
                watermark.StringText = text;
                watermark.IsVisible = true;
                watermark.UpdateBounds();
            
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
                watermark.Draw(null);
                Velo.SpriteBatch.End();
            }
        }
    }
}
