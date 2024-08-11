using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace Velo
{
    public class LbMenuContext : MenuContext
    {
        private readonly LabelW profileButton;
        private readonly LabelW versionText;

        public LbMenuContext()
        {
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
            menuStack.AddChild(profileButton, new Vector2(20f, 20f), new Vector2(180f, 35f));
            menuStack.AddChild(versionText, new Vector2(20f, 1035f), new Vector2(180f, 25f));
        }

        public override void OnExit()
        {
            Leaderboard.Instance.Enabled.Disable();
        }

        public override void OnCancelAllRequests()
        {
            RunsDatabase.Instance.CancelAll();
        }

        public override void OnClearCache()
        {
            RunsDatabase.Instance.Clear();
            RunsDatabase.Instance.PushRequestRuns(new GetPlayerPBsRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), null);
            RunsDatabase.Instance.PushRequestRuns(new GetPlayerPBsNonCuratedRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), null);
        }
    }

    public class Leaderboard : ToggleModule
    {
        public BoolSetting DisableLeaderboard;
        public BoolSetting PreciseTimer;
        public FloatSetting GhostOffsetTime;
        public BoolSetting EnableMultiGhost;
        public BoolSetting GhostDifferentColors;
        public BoolSetting LoopReplay;
        public BoolSetting ShowCheckpoints;
        public BoolSetting DisableReplayNotifications;

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

        public BoolSetting ShowRunStatus;
        public FloatSetting RunStatusScale;
        public EnumSetting<EOrientation> RunStatusOrientation;
        public VectorSetting RunStatusOffset;
        public ColorTransitionSetting RunStatusColor;

        private bool initialized = false;

        private LbMenuContext context;

        private CachedFont runStatusFont;

        private Leaderboard() : base("Leaderboard")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F2));

            NewCategory("general");
            DisableLeaderboard = AddBool("disable leaderboard", false);
            PreciseTimer = AddBool("precise timer", false);
            GhostOffsetTime = AddFloat("ghost offset", 0f, -2f, 2f);
            EnableMultiGhost = AddBool("enable multighost", false);
            GhostDifferentColors = AddBool("ghosts different colors", true);
            LoopReplay = AddBool("loop replay", false);
            ShowCheckpoints = AddBool("show checkpoints", false);
            DisableReplayNotifications = AddBool("disable replay notifications", false);

            DisableLeaderboard.Tooltip = 
                "Disabling the leaderboard will disable the automatic submission of new PB runs and any network communication with the leaderboard server.";
            PreciseTimer.Tooltip =
                "Makes the timer fully show all milliseconds (XX.XX:XXX).";
            GhostOffsetTime.Tooltip = "ghost offset time in seconds";
            EnableMultiGhost.Tooltip = "Allows you to have multiple ghosts at the same time.";
            GhostDifferentColors.Tooltip = "Color multiple ghosts differently or use the same color.";
            ShowCheckpoints.Tooltip = "Show the primary and secondary checkpoints of Velo's rule checking system.";
            DisableReplayNotifications.Tooltip = "Disables \"replay start/stop\" notifications.";

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

            NewCategory("run status");
            ShowRunStatus = AddBool("show run status", false);
            RunStatusScale = AddFloat("scale", 1f, 0f, 10f);
            RunStatusOrientation = AddEnum("orientation", EOrientation.TOP_LEFT,
                Enum.GetValues(typeof(EOrientation)).Cast<EOrientation>().Where(orientation => orientation != EOrientation.PLAYER).Select(orientation => orientation.Label()).ToArray());
            RunStatusOffset = AddVector("offset", new Vector2(32, 32), new Vector2(-500f, -500f), new Vector2(500f, 500f));
            RunStatusColor = AddColorTransition("color", new ColorTransition(Color.Red));

            CurrentCategory.Tooltip =
                "Show the categorization and validation status of the current run in the top left corner. " +
                "'1' means 1 lap and 'X' means invalid.";
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
                LocalGameMods.Instance.StopPlayback(notification: !DisableReplayNotifications.Value);
            }
            if (SaveRun.Pressed())
            {
                LocalGameMods.Instance.SaveLast();
            }
            if (ReplaySavedRun.Pressed())
            {
                Recording rec = LocalGameMods.Instance.LoadSaved();
                if (rec != null)
                    LocalGameMods.Instance.StartPlayback(rec, Playback.EPlaybackType.VIEW_REPLAY, notification: !DisableReplayNotifications.Value, showTime: true);
            }
            if (VerifySavedRun.Pressed())
            {
                Recording rec = LocalGameMods.Instance.LoadSaved();
                if (rec != null)
                    LocalGameMods.Instance.StartPlayback(rec, Playback.EPlaybackType.VERIFY);
            }
            if (SetGhostSavedRun.Pressed())
            {
                int ghostIndex = !EnableMultiGhost.Value ? 0 : LocalGameMods.Instance.GhostPlaybackCount();
                Ghosts.Instance.GetOrSpawn(ghostIndex, Instance.GhostDifferentColors.Value);
                Recording rec = LocalGameMods.Instance.LoadSaved();
                Ghosts.Instance.WaitForGhost(ghostIndex);
                if (rec != null)
                    LocalGameMods.Instance.StartPlayback(rec, Playback.EPlaybackType.SET_GHOST);
            }
        }

        public override void PostRender()
        {
            base.PostRender();

            FontCache.Get(ref runStatusFont, "UI\\Font\\ariblk.ttf:24");
            CTextDrawComponent runStatus = new CTextDrawComponent("", runStatusFont.Font, Vector2.Zero)
            {
                color_replace = false,
                IsVisible = true
            };

            int status = LocalGameMods.Instance.CurrentRunStatus();
            if (ShowRunStatus.Value && status != 0 && !LocalGameMods.Instance.IsPlaybackRunning())
            {
                runStatus.StringText = (status == 1 ? "1" : "X");
                runStatus.Color = RunStatusColor.Value.Get();
                runStatus.Offset = RunStatusOffset.Value / (CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height / 1080f) * Vector2.One;
                runStatus.Scale = RunStatusScale.Value * CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height / 1080f * Vector2.One;
                runStatus.HasDropShadow = true;
                runStatus.DropShadowColor = Color.Black;
                runStatus.DropShadowOffset = Vector2.One;
                runStatus.DropShadowColor *= runStatus.Color.A / 255f;
                runStatus.UpdateBounds();

                float screenWidth = Velo.SpriteBatch.GraphicsDevice.Viewport.Width;
                float screenHeight = Velo.SpriteBatch.GraphicsDevice.Viewport.Height;

                float width = runStatus.Bounds.Width;
                float height = runStatus.Bounds.Height;

                runStatus.Position = RunStatusOrientation.Value.GetOrigin(width, height, screenWidth, screenHeight, Velo.MainPlayer != null ? Velo.MainPlayer.actor.Position : Vector2.Zero);
                runStatus.UpdateBounds();

                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);
                runStatus.Draw(null);
                Velo.SpriteBatch.End();
            }

            if (DisableLeaderboard.Value || AutoUpdate.Instance.Enabled)
                Enabled.Disable();

            bool modified = Enabled.Modified();

            if (modified)
            {
                if (Enabled.Value.Enabled)
                    Cursor.EnableCursor(this);
                else
                    Cursor.DisableCursor(this);
            }

            if (Enabled.Value.Enabled && !initialized)
                Initialize();

            if (Refresh.Pressed() && Enabled.Value.Enabled)
            {
                context.ClearCacheRerequest();
            }

            if (!Enabled.Value.Enabled && !initialized)
                return;

            if (modified)
            {
                if (Enabled.Value.Enabled)
                {
                    ulong mapId = Map.GetCurrentMapId();
                    if (Velo.Ingame && Velo.ModuleSolo != null && mapId != ulong.MaxValue)
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
