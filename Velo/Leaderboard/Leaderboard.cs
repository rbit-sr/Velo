using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Buffers.Binary;

namespace Velo
{
    public class LeaderboardFonts
    {
        public CFont FontSmall;
        public CFont FontMedium;
        public CFont FontLarge;
    }

    public class Leaderboard : ToggleModule
    {
        private bool initialized = false;

        private CRectangleDrawComponent dimRecDraw;
        private LeaderboardFonts fonts;

        private LeaderboardMenu menu;

        private WidgetContainer container;

        private Leaderboard() : base("Leaderboard")
        {
            
        }

        public static Leaderboard Instance = new Leaderboard();

        private void Initialize()
        {
            dimRecDraw = new CRectangleDrawComponent(0, 0, 1920, 1080);
            dimRecDraw.IsVisible = true;
            dimRecDraw.OutlineEnabled = false;
            dimRecDraw.OutlineThickness = 0;
            dimRecDraw.FillEnabled = true;
            dimRecDraw.FillColor = new Color(0, 0, 0, 127);

            fonts = new LeaderboardFonts
            {
                FontSmall = FontCache.Get("UI\\Font\\NotoSans-Regular.ttf", 12),
                FontMedium = FontCache.Get("UI\\Font\\NotoSans-Regular.ttf", 18),
                FontLarge = FontCache.Get("UI\\Font\\Souses.ttf", 42)
            };
            Velo.ContentManager.Load(fonts.FontSmall, false);
            Velo.ContentManager.Load(fonts.FontMedium, false);
            Velo.ContentManager.Load(fonts.FontLarge, false);

            initialized = true;
        }

        public override void PostRender()
        {
            base.PostRender();

            if (Enabled.Modified())
            {
                CEngine.CEngine.Instance.Game.IsMouseVisible = Enabled.Value.Enabled;
            }
            
            if (!Enabled.Value.Enabled)
            {
                menu = null;
                return;
            }

            if (!initialized)
                Initialize();

            if (menu == null)
            {
                if (Velo.Ingame)
                    ChangeMenu(new BestForMapMenu(ChangeMenu, fonts, Map.GetCurrentMapId()));
                else
                    ChangeMenu(new TopRunsMenu(ChangeMenu, fonts));
            }

            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            dimRecDraw.Draw(null);
            Velo.SpriteBatch.End();

            container.Draw();
        }

        private void ChangeMenu(LeaderboardMenu newMenu)
        {
            if (newMenu == null)
            {
                menu = null;
                container = null;
                Enabled.Disable();
                return;
            }

            menu = newMenu;
            container = new WidgetContainer(menu, new Rectangle(400, 100, 1120, 880));
            menu.Refresh();
        }

        public void OnRunFinished(Recording run)
        {
            RecordingSubmitter.Instance.Submit(run);
        }
    }
}
