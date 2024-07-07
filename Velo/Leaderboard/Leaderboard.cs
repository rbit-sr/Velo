using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Velo
{
    public class LeaderboardFonts
    {
        public CachedFont FontSmall;
        public CachedFont FontMedium;
        public CachedFont FontLarge;
    }

    public class LbMenuContext
    {
        private readonly WidgetContainer container;
        private readonly TransitionW<Widget> menu;
        private readonly StackW menuStack;
        private readonly TransitionW<LbMenu> page;
        private readonly LabelW profileButton;
        private readonly LabelW versionText;

        private readonly Stack<LbMenu> backStack;
        public LeaderboardFonts Fonts;
        public string Error;

        public LbMenu Menu => page.Child; 

        public LbMenuContext()
        {
            Fonts = new LeaderboardFonts();

            FontCache.Get(ref Fonts.FontSmall, "UI\\Font\\NotoSans-Regular.ttf:15");
            FontCache.Get(ref Fonts.FontMedium, "UI\\Font\\NotoSans-Regular.ttf:18,UI\\Font\\NotoSansCJKtc-Regular.otf:18,UI\\Font\\NotoSansCJKkr-Regular.otf:18");
            FontCache.Get(ref Fonts.FontLarge, "UI\\Font\\Souses.ttf:42,UI\\Font\\NotoSansCJKtc-Regular.otf:42,UI\\Font\\NotoSansCJKkr-Regular.otf:42");

            backStack = new Stack<LbMenu>();

            profileButton = new LabelW("My profile", Fonts.FontMedium.Font);
            Style.ApplyButton(profileButton);
            profileButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    if (page.Child is LbPlayerMenuPage playerMenu && playerMenu.PlayerId == Steamworks.SteamUser.GetSteamID().m_SteamID)
                        return;

                    ChangePage(new LbPlayerMenuPage(this, Steamworks.SteamUser.GetSteamID().m_SteamID));
                }
            };

            versionText = new LabelW(Version.VERSION_NAME, Fonts.FontSmall.Font);
            Style.ApplyText(versionText);
            versionText.Align = new Vector2(0f, 0.5f);
            versionText.Color = () => Color.Gray * 0.5f;
            page = new TransitionW<LbMenu>();
            menuStack = new StackW();
            menuStack.AddChild(page, new Vector2(400f, 100f), new Vector2(1120f, 880f));
            menuStack.AddChild(profileButton, new Vector2(20f, 20f), new Vector2(180f, 35f));
            menuStack.AddChild(versionText, new Vector2(20f, 1035f), new Vector2(180f, 25f));
            menu = new TransitionW<Widget>();
            container = new WidgetContainer(menu, new Rectangle(0, 0, 1920, 1080));
        }

        public void EnterMenu(LbMenu menu)
        {
            this.menu.TransitionTo(menuStack, 4f, new Vector2(-500f, 0f));
            this.page.GoTo(menu);
            ResetStateRerequest(menu);
        }

        public void ExitMenu(bool animation = true)
        {
            Leaderboard.Instance.Enabled.Disable();
            backStack.Clear();
            if (animation)
                menu.TransitionTo(null, 4f, new Vector2(-500f, 0f));
            else
                menu.GoTo(null);
        }

        public void ChangePage(LbMenu newPage)
        {
            if (page.Child != null)
                backStack.Push(page.Child);
            page.TransitionTo(newPage, 8f, Vector2.Zero);

            ResetStateRerequest(page.Child);
        }

        public void PushBackStack(LbMenu menu)
        {
            backStack.Push(menu);
        }

        public void PopPage()
        {
            page.TransitionTo(backStack.Pop(), 8f, Vector2.Zero);
            Rerequest(page.Child);
        }

        public void ResetStateRerequest(LbMenu menu)
        {
            RunsDatabase.Instance.CancelAll();
            menu.ResetState();
            menu.Rerequest();
            menu.Refresh();
        }

        public void Rerequest(LbMenu menu)
        {
            RunsDatabase.Instance.CancelAll();
            menu.Rerequest();
            menu.Refresh();
        }

        public void ClearCacheRerequest(LbMenu menu)
        {
            RunsDatabase.Instance.CancelAll();
            RunsDatabase.Instance.Clear();
            RunsDatabase.Instance.PushRequestRuns(new GetPlayerPBsRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), null);
            menu.ResetState();
            menu.Rerequest();
            menu.Refresh();
        }

        public void Draw()
        {
            if (menu.Child == null && !menu.Transitioning())
                return;

            CRectangleDrawComponent dimRecDraw = new CRectangleDrawComponent(0, 0, CEngine.CEngine.Instance.GraphicsDevice.Viewport.Width, CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height)
            {
                IsVisible = true,
                OutlineEnabled = false,
                OutlineThickness = 0,
                FillEnabled = true,
                FillColor = Leaderboard.Instance.DimColor.Value.Get() * (menu.Child != null ? menu.R : 1f - menu.R)
            };
            Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
            dimRecDraw.Draw(null);
            Velo.SpriteBatch.End();

            container.Draw();
        }
    }

    public class Leaderboard : ToggleModule
    {
        public BoolSetting DisableLeaderboard;
        public BoolSetting PreciseTimer;
        public BoolSetting ShowRunStatus;
        public FloatSetting GhostOffsetTime;
        public BoolSetting LoopReplay;
        public BoolSetting ShowCheckpoints;

        public HotkeySetting Refresh;
        public HotkeySetting StopReplay;
        public HotkeySetting Rewind1Second;
        public HotkeySetting ShowAppliedRules;
        public HotkeySetting SaveRun;
        public HotkeySetting ReplaySavedRun;
        public HotkeySetting VerifySavedRun;
        public HotkeySetting SetGhostSavedRun;

        public ColorTransitionSetting TextColor;
        public ColorTransitionSetting HeaderTextColor;
        public ColorTransitionSetting HighlightTextColor;
        public ColorTransitionSetting EntryColor1;
        public ColorTransitionSetting EntryColor2;
        public ColorTransitionSetting EntryHoveredColor;
        public ColorTransitionSetting ButtonColor;
        public ColorTransitionSetting ButtonHoveredColor;
        public ColorTransitionSetting ButtonSelectedColor;
        public ColorTransitionSetting DimColor;
        public IntSetting ScrollBarWidth;
        public EnumSetting<ETimeFormat> TimeFormat;

        private bool initialized = false;

        private LbMenuContext context;

        private Leaderboard() : base("Leaderboard")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F2));

            NewCategory("general");
            DisableLeaderboard = AddBool("disable leaderboard", false);
            PreciseTimer = AddBool("precise timer", false);
            ShowRunStatus = AddBool("show run status", false);
            GhostOffsetTime = AddFloat("ghost offset", 0f, -2f, 2f);
            LoopReplay = AddBool("loop replay", false);
            ShowCheckpoints = AddBool("show checkpoints", false);

            DisableLeaderboard.Tooltip = 
                "Disabling the leaderboard will disable the automatic submission of new PB runs and any network communication with the leaderboard server.";
            PreciseTimer.Tooltip =
                "Makes the timer fully show all milliseconds (XX.XX:XXX)";
            ShowRunStatus.Tooltip = 
                "Show the categorization and validation status of the current run in the top left corner. " +
                "'1' means 1 lap and 'X' means invalid.";
            GhostOffsetTime.Tooltip = "ghost offset time in seconds";
            ShowCheckpoints.Tooltip = "Show the primary and secondary checkpoints of Velo's rule checking system.";

            NewCategory("hotkeys");
            Refresh = AddHotkey("refresh", 0x97);
            ShowAppliedRules = AddHotkey("show applied rules", 0x97);
            StopReplay = AddHotkey("stop replay", 0x97);
            Rewind1Second = AddHotkey("rewind 1 second", 0x97, autoRepeat: true);
            SaveRun = AddHotkey("save last run", 0x97);
            ReplaySavedRun = AddHotkey("replay saved run", 0x97);
            VerifySavedRun = AddHotkey("verify saved run", 0x97);
            SetGhostSavedRun = AddHotkey("set ghost saved run", 0x97);

            Refresh.Tooltip = 
                "Clears the runs cache.";
            Rewind1Second.Tooltip =
                "Rewinds playback by 1 second.";
            ShowAppliedRules.Tooltip = 
                "Shows the rules that have been applied to the previous run leading to its categorization and validation.";
            SaveRun.Tooltip = 
                "Saves a recording of the previous run to \"Velo\\saved run\". If you believe your run to be wrongly categorized or invalidated, send the file to a leaderboard moderator.";

            NewCategory("style");
            TextColor = AddColorTransition("text color", new ColorTransition(Color.White));
            HeaderTextColor = AddColorTransition("header text color", new ColorTransition(new Color(185, 253, 224)));
            HighlightTextColor = AddColorTransition("highlight text color", new ColorTransition(Color.Gold));
            EntryColor1 = AddColorTransition("entry color 1", new ColorTransition(new Color(40, 40, 40, 150)));
            EntryColor2 = AddColorTransition("entry color 2", new ColorTransition(new Color(30, 30, 30, 150)));
            EntryHoveredColor = AddColorTransition("entry hovered color", new ColorTransition(new Color(100, 100, 100, 150)));
            ButtonColor = AddColorTransition("button color", new ColorTransition(new Color(150, 150, 150, 150)));
            ButtonHoveredColor = AddColorTransition("button hovered color", new ColorTransition(new Color(200, 200, 200, 150)));
            ButtonSelectedColor = AddColorTransition("button selected color", new ColorTransition(new Color(240, 70, 100, 200)));
            ScrollBarWidth = AddInt("scroll bar width", 10, 0, 20);
            DimColor = AddColorTransition("dim color", new ColorTransition(new Color(0, 0, 0, 127)));
            TimeFormat = AddEnum("time format", ETimeFormat.UNITS, new[] { "(XXm )(XXs )XXXms", "XX:XX:XXX", "XX.XX:XXX", "(XX:)XX:XXX", "(XX.)XX:XXX" });
        }

        public static Leaderboard Instance = new Leaderboard();

        private void Initialize()
        {
            context = new LbMenuContext();
            
            initialized = true;
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (DisableLeaderboard.Value)
                return;

            if (ShowAppliedRules.Pressed())
            {
                LocalGameMods.Instance.ShowLastAppliedRules();
            }
            if (StopReplay.Pressed() && LocalGameMods.Instance.IsPlaybackRunning())
            {
                LocalGameMods.Instance.StopPlayback();
            }
            if (SaveRun.Pressed())
            {
                LocalGameMods.Instance.SaveLast();
            }
            if (ReplaySavedRun.Pressed())
            {
                Recording rec = LocalGameMods.Instance.LoadSaved();
                if (rec != null)
                    LocalGameMods.Instance.StartPlayback(rec, Playback.EPlaybackType.VIEW_REPLAY, showTime: true);
            }
            if (VerifySavedRun.Pressed())
            {
                Recording rec = LocalGameMods.Instance.LoadSaved();
                if (rec != null)
                    LocalGameMods.Instance.StartPlayback(rec, Playback.EPlaybackType.VERIFY);
            }
            if (SetGhostSavedRun.Pressed())
            {
                Recording rec = LocalGameMods.Instance.LoadSaved();
                if (rec != null)
                    LocalGameMods.Instance.StartPlayback(rec, Playback.EPlaybackType.SET_GHOST);
            }
        }

        public override void PostRender()
        {
            base.PostRender();

            if (DisableLeaderboard.Value || AutoUpdate.Instance.Enabled)
                Enabled.Disable();

            bool modified = Enabled.Modified();

            if (modified)
            {
                if (Enabled.Value.Enabled)
                    Velo.EnableCursor(this);
                else
                    Velo.DisableCursor(this);
            }

            if (Enabled.Value.Enabled && !initialized)
                Initialize();

            if (Refresh.Pressed() && Enabled.Value.Enabled)
            {
                context.ClearCacheRerequest(context.Menu);
            }

            if (!Enabled.Value.Enabled && !initialized)
                return;

            if (modified)
            {
                if (Enabled.Value.Enabled)
                {
                    int mapId = Map.GetCurrentMapId();
                    if (Velo.Ingame && Velo.ModuleSolo != null && mapId != -1)
                    {
                        context.PushBackStack(new LbMainMenuPage(context));
                        context.EnterMenu(new LbMapMenuPage(context, mapId));
                    }
                    else
                    {
                        context.EnterMenu(new LbMainMenuPage(context));
                    }
                }
                else
                {
                    context.ExitMenu(animation: true);
                }
            }

            context.Draw();
        }

        public void OnRunFinished(Recording run)
        {
            if (DisableLeaderboard.Value)
                return;

            RecordingSubmitter.Instance.Submit(run);
        }
    }
}
