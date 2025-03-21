using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace Velo
{
    public class LbContext : MenuContext
    {
        public BackstackW<ILbWidget> Page;
        public ButtonW ProfileButton;
        public ButtonW EventsButton;
        public LabelW VersionText;
        public PopularWindow PopularWindow;
        public ImageW LoadingSymbol;
        public LabelW ErrorMessage;

        public string Error;

        public LbContext(ToggleSetting enabled) : 
            base(enabled, enableDim: true)
        {
            Page = new BackstackW<ILbWidget>();

            ProfileButton = new ButtonW("My profile", Fonts.FontMedium);
            Style.ApplyButton(ProfileButton);
            ProfileButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    if (Page.Child is LbPlayerMenuPage playerMenu && playerMenu.PlayerId == Steamworks.SteamUser.GetSteamID().m_SteamID)
                        return;

                    Page.TransitionTo(new LbPlayerMenuPage(this, Steamworks.SteamUser.GetSteamID().m_SteamID), 8f, Vector2.Zero);
                    Request();
                }
            };

            EventsButton = new ButtonW("Events", Fonts.FontMedium);
            Style.ApplyButton(EventsButton);
            EventsButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    if (Page.Child is LbEventsMenuPage)
                        return;

                    Page.TransitionTo(new LbEventsMenuPage(this), 8f, Vector2.Zero);
                    Request();
                }
            };

            VersionText = new LabelW(Version.VERSION_NAME + " - " + Version.AUTHOR, Fonts.FontSmall);
            Style.ApplyText(VersionText);
            VersionText.Align = new Vector2(0f, 0.5f);
            VersionText.Color = () => Color.Gray * 0.5f;
            PopularWindow = new PopularWindow(this);
            LoadingSymbol = new ImageW(LoadSymbol.Get())
            {
                RotationSpeed = 3f,
                Rotation = (float)Math.PI / 2f
            };
            ErrorMessage = new LabelW("", Fonts.FontMedium)
            {
                Color = () => Color.Red,
                Align = new Vector2(0f, 0.5f)
            };

            Vector2 PAGE_POS = new Vector2(375f, 100f);
            Vector2 PAGE_SIZE = new Vector2(1170f, 880f);
            float BOTTOM_ROW_HEIGHT = 35f;
            Vector2 ERROR_MESSAGE_SIZE = new Vector2(1920f - PAGE_SIZE.X - 8f, 200f);

            AddElem(Page, StackW.TOP_LEFT, PAGE_POS, PAGE_SIZE);
            AddElem(ProfileButton, StackW.TOP_LEFT, new Vector2(20f, 20f), new Vector2(180f, 35f));
            AddElem(EventsButton, StackW.TOP_LEFT, new Vector2(20f, 65f), new Vector2(180f, 35f));
            AddElem(VersionText, StackW.BOTTOM_LEFT, new Vector2(20f, -20f), new Vector2(180f, 25f));
            AddElem(PopularWindow, StackW.BOTTOM_RIGHT, Vector2.Zero, new Vector2(300f, 408f));
            AddElem(LoadingSymbol, StackW.TOP_LEFT, PAGE_POS + new Vector2(PAGE_SIZE.X + 8f, PAGE_SIZE.Y - (LoadSymbol.SIZE + BOTTOM_ROW_HEIGHT) / 2), new Vector2(LoadSymbol.SIZE, LoadSymbol.SIZE));
            AddElem(ErrorMessage, StackW.TOP_LEFT, PAGE_POS + new Vector2(PAGE_SIZE.X + 8f, PAGE_SIZE.Y - 35f / 2 - ERROR_MESSAGE_SIZE.Y / 2), ERROR_MESSAGE_SIZE);
        }

        public override void EnterMenu()
        {
            Page.Clear();
            
            ulong mapId = Map.GetCurrentMapId();
            if (Velo.Ingame && Velo.ModuleSolo != null && mapId != ulong.MaxValue)
            {
                Page.Push(new LbMainMenuPage(this));
                Page.TransitionTo(new LbMapMenuPage(this, mapId), 8f, Vector2.Zero);
            }
            else
            {
                Page.TransitionTo(new LbMainMenuPage(this), 8f, Vector2.Zero);
            }

            base.EnterMenu();

            Request();
        }

        public override void ExitMenu(bool animation = true)
        {
            base.ExitMenu(animation);
        }

        public void ChangePage(ILbWidget page)
        {
            page.Reset();
            Page.TransitionTo(page, 8f, Vector2.Zero);
            Request();
        }

        public void ChangeBack()
        {
            Page.TransitionBack(8f, Vector2.Zero);
            Request();
        }

        public void Request()
        {
            RunsDatabase.Instance.CancelAll();
            Page.Child.PushRequests();
            PopularWindow.PushRequests();
            RunsDatabase.Instance.RunRequestRuns(Refresh, error => Error = error.Message);
        }

        public override bool Draw()
        {
            if (!base.Draw())
            {
                Page.Clear();
                return false;
            }

            if (RunsDatabase.Instance.Pending())
            {
                LoadingSymbol.Visible = true;
                Error = "";
                ErrorMessage.Text = "";
            }
            else
            {
                LoadingSymbol.Visible = false;
                LoadingSymbol.Rotation = (float)Math.PI / 2f;
            }
            if (Error != "" && Error != null)
                ErrorMessage.Text = Util.LineBreaks("Error: " + Error, 30);

            return true;
        }
    }

    public class Leaderboard : ToggleModule
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

        public LbContext context;

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

            context = new LbContext(Enabled);
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
                RunsDatabase.Instance.PushRequestRuns(new GetPlayerEventPBsRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), null);
                RunsDatabase.Instance.PushRequestRuns(new GetPlayerPBsNonCuratedRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), null);
                context.Reset();
                context.Refresh();
                context.Request();
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
