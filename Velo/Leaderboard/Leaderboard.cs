using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace Velo
{
    public class Leaderboard : MenuModule
    {
        public BoolSetting DisableLeaderboard;
        public BoolSetting PreciseTimer;
        public BoolSetting ShowCheckpoints;

        public HotkeySetting RefreshHotkey;
        public HotkeySetting ShowAppliedRulesHotkey;

        public EnumSetting<ETimeFormat> TimeFormat;

        public BoolSetting ShowRunStatus;
        public FloatSetting RunStatusScale;
        public EnumSetting<EOrientation> RunStatusOrientation;
        public VectorSetting RunStatusOffset;
        public ColorTransitionSetting RunStatusColor;

        private TextDraw runStatus;

        private LabelW profileButton;
        private LabelW versionText;
        private PopularWindow popularWindow;

        private Leaderboard() : base("Leaderboard")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F2));

            NewCategory("general");
            DisableLeaderboard = AddBool("disable leaderboard", false);
            PreciseTimer = AddBool("precise timer", false);
            
            ShowCheckpoints = AddBool("show checkpoints", false);
            TimeFormat = AddEnum("time format", ETimeFormat.UNITS, new[] { "(XXm )(XXs )XXXms", "XX:XX:XXX", "XX.XX:XXX", "(XX:)XX:XXX", "(XX.)XX:XXX" });

            DisableLeaderboard.Tooltip = 
                "Disabling the leaderboard will disable the automatic submission of new PB runs and any network communication with the leaderboard server.";
            PreciseTimer.Tooltip =
                "Makes the timer fully show all milliseconds (XX.XX:XXX).";
            ShowCheckpoints.Tooltip = "Show the primary and secondary checkpoints of Velo's rule checking system.";

            NewCategory("hotkeys");
            RefreshHotkey = AddHotkey("refresh", 0x97);
            ShowAppliedRulesHotkey = AddHotkey("show applied rules", 0x97);

            RefreshHotkey.Tooltip = 
                "Clears the runs cache.";
            ShowAppliedRulesHotkey.Tooltip = 
                "Shows the rules that have been applied to the previous run leading to its categorization and validation.";
            
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

        public override void Init()
        {
            base.Init();

            profileButton = new LabelW("My profile", Fonts.FontMedium);
            Style.ApplyButton(profileButton);
            profileButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    if (Page is LbPlayerMenuPage playerMenu && playerMenu.PlayerId == Steamworks.SteamUser.GetSteamID().m_SteamID)
                        return;

                    ChangePage(new LbPlayerMenuPage(this, Steamworks.SteamUser.GetSteamID().m_SteamID));
                }
            };

            versionText = new LabelW(Version.VERSION_NAME + " - " + Version.AUTHOR, Fonts.FontSmall);
            Style.ApplyText(versionText);
            versionText.Align = new Vector2(0f, 0.5f);
            versionText.Color = () => Color.Gray * 0.5f;
            popularWindow = new PopularWindow(this);
            AddElem(profileButton, new Vector2(20f, 20f), new Vector2(180f, 35f));
            AddElem(versionText, new Vector2(20f, 1035f), new Vector2(180f, 25f));
            AddElem(popularWindow, new Vector2(1620f, 672f), new Vector2(300f, 408f));
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (DisableLeaderboard.Value)
                return;

            if (ShowAppliedRulesHotkey.Pressed())
            {
                OfflineGameMods.Instance.ShowLastAppliedRules();
            }
        }

        public override void PostRender()
        {
            if (DisableLeaderboard.Value || AutoUpdate.Instance.Enabled.Value.Enabled)
                Enabled.Disable(); 
            
            if (RefreshHotkey.Pressed() && Enabled.Value.Enabled)
            {
                RunsDatabase.Instance.CancelAll();
                RunsDatabase.Instance.Clear();
                RunsDatabase.Instance.PushRequestRuns(new GetPlayerPBsRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), null);
                RunsDatabase.Instance.PushRequestRuns(new GetPlayerPBsNonCuratedRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), null);
                Reset();
                Refresh();
                OnChange();
            }

            base.PostRender();

            int status = OfflineGameMods.Instance.CurrentRunStatus();
            if (
                ShowRunStatus.Value && status != 0 && 
                !OfflineGameMods.Instance.IsPlaybackRunning() &&
                Velo.get_time_scale() == 1f &&
                !OfflineGameMods.Instance.IsModded() && 
                !BlindrunSimulator.Instance.Enabled.Value.Enabled &&
                !OfflineGameMods.Instance.DtFixed
                )
            {
                if (runStatus == null)
                {
                    runStatus = new TextDraw()
                    {
                        IsVisible = true,
                        HasDropShadow = true,
                        DropShadowOffset = Vector2.One
                    };
                    runStatus.SetFont("UI\\Font\\ariblk.ttf:24");
                }

                runStatus.Text = (status == 1 ? "1" : "X");
                runStatus.Color = RunStatusColor.Value.Get();
                runStatus.Offset = RunStatusOffset.Value / (CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height / 1080f) * Vector2.One;
                runStatus.Scale = RunStatusScale.Value * CEngine.CEngine.Instance.GraphicsDevice.Viewport.Height / 1080f * Vector2.One;
                runStatus.DropShadowColor = Color.Black * (runStatus.Color.A / 255f);

                float screenWidth = Velo.SpriteBatch.GraphicsDevice.Viewport.Width;
                float screenHeight = Velo.SpriteBatch.GraphicsDevice.Viewport.Height;

                float width = runStatus.Bounds.Width;
                float height = runStatus.Bounds.Height;

                runStatus.Position = RunStatusOrientation.Value.GetOrigin(width, height, screenWidth, screenHeight, Velo.MainPlayer != null ? Velo.MainPlayer.actor.Position : Vector2.Zero);

                runStatus.Draw();
            }
        }

        public void OnRunFinished(Recording run)
        {
            if (DisableLeaderboard.Value)
                return;

            RecordingSubmitter.Instance.Submit(run);
        }

        public override void OnChange()
        {
            RunsDatabase.Instance.CancelAll();
            (Page as IRequestable).PushRequests();
            popularWindow.PushRequests();
            RunsDatabase.Instance.RunRequestRuns(Refresh, error => Error = error.Message);
        }

        public override Menu GetStartMenu()
        {
            ulong mapId = Map.GetCurrentMapId();
            if (Velo.Ingame && Velo.ModuleSolo != null && mapId != ulong.MaxValue)
            {
                PushBackStack(new LbMainMenuPage(this));
                return new LbMapMenuPage(this, mapId);
            }
            else
            {
                return new LbMainMenuPage(this);
            }
        }
    }
}
