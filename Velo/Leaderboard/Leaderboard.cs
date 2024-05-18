using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Velo
{
    public class LeaderboardFonts
    {
        public CachedFont FontSmall;
        public CachedFont FontMedium;
        public CachedFont FontLarge;
    }

    public class Leaderboard : ToggleModule
    {
        public BoolSetting EnableSubmissions;
        public HotkeySetting StopPlayback; 

        private bool initialized = false;

        private CRectangleDrawComponent dimRecDraw;
        private LeaderboardFonts fonts;

        private LeaderboardMenu menu;

        private WidgetContainer container;

        private Leaderboard() : base("Leaderboard")
        {
            NewCategory("general");
            EnableSubmissions = AddBool("enable submissions", true);
            EnableSubmissions.Tooltip = "Disabling this will stop the automatic submission of new PB runs.";

            NewCategory("playback");
            StopPlayback = AddHotkey("stop playback", 0x97);
        }

        public static Leaderboard Instance = new Leaderboard();

        private void Initialize()
        {
            dimRecDraw = new CRectangleDrawComponent(0, 0, 1920, 1080)
            {
                IsVisible = true,
                OutlineEnabled = false,
                OutlineThickness = 0,
                FillEnabled = true,
                FillColor = new Color(0, 0, 0, 127)
            };

            fonts = new LeaderboardFonts();

            FontCache.Get(ref fonts.FontSmall, "UI\\Font\\NotoSans-Regular.ttf", 12);
            FontCache.Get(ref fonts.FontMedium, "UI\\Font\\NotoSans-Regular.ttf", 18);
            FontCache.Get(ref fonts.FontLarge, "UI\\Font\\Souses.ttf", 42);
            
            initialized = true;
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Keyboard.Pressed[StopPlayback.Value])
            {
                LocalGameMods.Instance.StopPlayback();
            }
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
