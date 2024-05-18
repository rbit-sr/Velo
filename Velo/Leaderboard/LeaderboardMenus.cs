using System;
using System.Collections.Generic;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Velo
{
    public static class Style
    {
        public static readonly Color ENTRY_COLOR1 = new Color(40, 40, 40, 150);
        public static readonly Color ENTRY_COLOR2 = new Color(30, 30, 30, 150);
        public static readonly Color ENTRY_COLOR_HOVERED = new Color(100, 100, 100, 150);

        public static readonly Color BUTTON_COLOR = new Color(100, 100, 100, 100);
        public static readonly Color BUTTON_HOVERED_COLOR = new Color(150, 150, 150, 100);
        public static readonly Color BUTTON_ENABLED_COLOR = new Color(220, 50, 60, 100);

        public static void ApplyButton(LabelW button)
        {
            button.Hoverable = true;
            button.BackgroundVisible = true;
            button.BackgroundVisibleHovered = true;
            button.BackgroundColor = BUTTON_COLOR;
            button.BackgroundColorHovered = BUTTON_HOVERED_COLOR;
        }

        public static void ApplyMultiSelectButton(MultiSelectButton button)
        {
            button.ButtonBackgroundVisible = true;
            button.ButtonBackgroundVisibleHovered = true;
            button.ButtonBackgroundVisibleSelected = true;
            button.ButtonBackgroundColor = BUTTON_COLOR;
            button.ButtonBackgroundColorHovered = BUTTON_HOVERED_COLOR;
            button.ButtonBackgroundColorSelected = BUTTON_ENABLED_COLOR;
        }
    }

    public static class LoadSymbol
    {
        public static readonly int SIZE = 50;
        private static readonly float RADIUS = 20f;
        private static readonly float WIDTH = 4f;
        private static Texture2D texture;

        public static Texture2D Get()
        {
            if (texture != null)
                return texture;

            texture = new Texture2D(CEngine.CEngine.Instance.GraphicsDevice, SIZE, SIZE);
            Color[] pixels = new Color[SIZE * SIZE];

            for (int y = 0; y < SIZE; y++)
            {
                for (int x = 0; x < SIZE; x++)
                {
                    Vector2 diff = new Vector2(x, y) + 0.5f * Vector2.One - new Vector2(SIZE, SIZE) / 2;
                    float length = diff.Length();
                    if (length <= RADIUS + 0.5 && length >= RADIUS - WIDTH - 0.5)
                    {
                        float factor = 1f;
                        if (length >= RADIUS - 0.5f)
                        {
                            factor *= RADIUS - length + 0.5f;
                        }
                        if (length <= RADIUS - WIDTH + 0.5)
                        {
                            factor *= length - RADIUS + WIDTH + 0.5f;
                        }

                        pixels[x + SIZE * y] = Color.Lerp(Color.Black, Color.White, (float)(Math.Atan2(diff.Y, diff.X) / (2.0 * Math.PI) + 0.5)) * factor;
                    }
                    else
                    {
                        pixels[x + SIZE * y] = Color.Transparent;
                    }
                }
            }
            texture.SetData(pixels);
            return texture;
        }
    }

    public abstract class LeaderboardMenu : LayoutW
    {
        protected Action<LeaderboardMenu> onMenuChange;
        protected LeaderboardFonts fonts;

        public LeaderboardMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts, EOrientation orientation) :
            base(orientation)
        {
            this.onMenuChange = onMenuChange;
            this.fonts = fonts;
        }

        public abstract void Refresh();
    }

    public abstract class LeaderboardTableMenu : LeaderboardMenu
    {
        protected LabelW title;
        protected LayoutW titleBar;
        protected TableW table;

        protected LayoutW buttonRow;

        private float loadingRotation = -(float)Math.PI / 2f;
        private TimeSpan lastFrameTime = TimeSpan.Zero;

        protected string Error { get; set; }

        public LeaderboardTableMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts, string title) :
            base(onMenuChange, fonts, EOrientation.VERTICAL)
        {
            this.title = new LabelW(title, fonts.FontLarge.Font)
            {
                Align = new Vector2(0f, 0.5f),
                Color = new Color(185, 253, 224)
            };
            titleBar = new LayoutW(EOrientation.HORIZONTAL);
            titleBar.AddChild(this.title, FILL);

            table = new TableW(fonts.FontMedium.Font, 0, 40, (i) => 40f)
            {
                HeaderAlign = new Vector2(0f, 0.5f),
                HeaderColor = new Color(185, 253, 224),
                EntryBackgroundVisible = true,
                EntryBackgroundColor1 = Style.ENTRY_COLOR1,
                EntryBackgroundColor2 = Style.ENTRY_COLOR2,
                EntryHoverable = true,
                EntryBackgroundColorHovered = Style.ENTRY_COLOR_HOVERED
            };
            table.AddSpace(5f);

            buttonRow = new LayoutW(EOrientation.HORIZONTAL);

            AddChild(titleBar, 80f);
            AddSpace(10f);
            AddChild(table, FILL);
            AddSpace(5f);
            AddChild(buttonRow, 35f);
        }

        public override void Draw(Widget hovered, float scale)
        {
            base.Draw(hovered, scale);

            if (
                RunsDatabase.Instance.GetRunHandlers[0].Status == ERequestStatus.PENDING ||
                RunsDatabase.Instance.GetRecordingHandler.Status == ERequestStatus.PENDING
                )
            {
                Error = null;

                TimeSpan now = new TimeSpan(DateTime.Now.Ticks);

                float dt = (float)(now - lastFrameTime).TotalSeconds;
                if (dt > 1f)
                    dt = 1f;

                lastFrameTime = now;

                loadingRotation += 3f * dt;

                Velo.SpriteBatch.End();
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);

                Vector2 pos =
                    buttonRow.Position +
                    new Vector2(buttonRow.Size.X + 8f, (buttonRow.Size.Y - LoadSymbol.SIZE) / 2f) +
                    Vector2.One * LoadSymbol.SIZE / 2f;
                Velo.SpriteBatch.Draw(LoadSymbol.Get(), pos * scale, new Rectangle?(), Color.White, loadingRotation, Vector2.One * LoadSymbol.SIZE / 2f, scale, SpriteEffects.None, 0f);
            }
            else
            {
                loadingRotation = -(float)Math.PI / 2f;
            }
            if (Error != "" && Error != null)
            {
                Velo.SpriteBatch.End();
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);

                CTextDrawComponent errorDraw = new CTextDrawComponent("", fonts.FontMedium.Font, Vector2.Zero)
                {
                    IsVisible = true,
                    StringText = Util.LineBreaks("Error: " + Error, 30),
                    Color = Color.Red,
                    HasDropShadow = true,
                    DropShadowColor = Color.Black,
                    DropShadowOffset = Vector2.One
                };
                errorDraw.UpdateBounds();

                errorDraw.Position =
                    (buttonRow.Position +
                    new Vector2(buttonRow.Size.X + 8f, (buttonRow.Size.Y - errorDraw.Size.Y) / 2f)) * scale;
                errorDraw.Scale = Vector2.One * scale;
                errorDraw.Draw(null);
            }
        }
    }

    public class PlaceEntry : LabelW
    {
        private readonly Func<int, RunInfo> runs;

        private readonly int i;

        public PlaceEntry(int i, CFont font, Func<int, RunInfo> runs) :
            base("", font)
        {
            this.i = i;
            this.runs = runs;
            Align = new Vector2(0f, 0.5f);
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);
            if (runs(i).Id == -1)
                Text = "";
            else if (runs(i).Place != -1)
                Text = "" + (runs(i).Place + 1);
            else
                Text = "-";
        }
    }

    public class PlayerEntry : LayoutW
    {
        private readonly Func<int, RunInfo> runs;

        private readonly ImageW image;
        private readonly LabelW label;

        private readonly int i;

        public PlayerEntry(int i, CFont font, Func<int, RunInfo> runs) :
            base(EOrientation.HORIZONTAL)
        {
            this.i = i;
            this.runs = runs;
            image = new ImageW(null);
            label = new LabelW("", font)
            {
                Align = new Vector2(0f, 0.5f)
            };

            AddChild(image, 33f);
            AddSpace(4f);
            AddChild(label, FILL);
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            if (runs(i).Id == -1)
            {
                image.Visible = false;
                label.Visible = false;
                return;
            }

            image.Visible = true;
            label.Visible = true;
            image.Image = SteamCache.GetAvatar(runs(i).PlayerId);
            label.Text = SteamCache.GetName(runs(i).PlayerId);
        }
    }

    public class TimeEntry : LabelW
    {
        private readonly Func<int, RunInfo> runs;
        private readonly int i;

        public TimeEntry(int i, CFont font, Func<int, RunInfo> runs) :
            base("", font)
        {
            this.i = i;
            this.runs = runs;
            Align = new Vector2(0f, 0.5f);
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            if (runs(i).Id == -1)
            {
                Text = "";
            }
            else
            {
                long time = runs(i).RunTime;
                Text = Util.FormatTime(time);
            }
        }
    }

    public class AgeEntry : LabelW
    {
        private readonly Func<int, RunInfo> runs;
        private readonly int i;

        public AgeEntry(int i, CFont font, Func<int, RunInfo> runs) :
            base("", font)
        {
            this.i = i;
            this.runs = runs;
            Align = new Vector2(0f, 0.5f);
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            if (runs(i).Id == -1)
            {
                Text = "";
            }
            else
            {
                long now = DateTimeOffset.Now.ToUnixTimeSeconds();
                long diff = now - runs(i).CreateTime;
                Text = Util.ApproxTime(diff);
            }
        }
    }

    public class MapEntry : LabelW
    {
        private readonly Func<int, RunInfo> runs;

        private readonly int i;

        public MapEntry(int i, CFont font, Func<int, RunInfo> runs) :
            base("", font)
        {
            this.i = i;
            this.runs = runs;
            Align = new Vector2(0f, 0.5f);
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            if (runs(i).Id == -1)
            {
                Text = "";
            }
            else
            {
                Text = Map.MapIdToName[runs(i).MapId];
            }
        }
    }

    public class CategoryEntry : LabelW
    {
        private readonly Func<int, RunInfo> runs;

        private readonly int i;

        public CategoryEntry(int i, CFont font, Func<int, RunInfo> runs) :
            base("", font)
        {
            this.i = i;
            this.runs = runs;
            Align = new Vector2(0f, 0.5f);
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            if (runs(i).Id == -1)
            {
                Text = "";
            }
            else
            {
                Text = ((ECategory)runs(i).Category).Label();
            }
        }
    }

    public class AllMapEntry : LabelW
    {
        private readonly int i;

        public AllMapEntry(int i, CFont font) :
            base("", font)
        {
            this.i = i;
            Align = new Vector2(0f, 0.5f);
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            Text = Map.MapIdToName[i];
        }
    }

    public class PlayerTimeEntry : LayoutW
    {
        private readonly Func<int, RunInfo> runs;

        private readonly PlayerEntry player;

        private readonly LabelW time;

        private readonly int i;

        public PlayerTimeEntry(int i, CFont font, Func<int, RunInfo> runs) :
            base(EOrientation.VERTICAL)
        {
            this.i = i;
            this.runs = runs;
            player = new PlayerEntry(i, font, (i_) => runs(i_));

            time = new LabelW("", font)
            {
                Align = new Vector2(0f, 0.5f)
            };

            AddChild(time, 40);
            AddSpace(4);
            AddChild(player, 40);
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            player.Update(mouseInsideParent, events, crop);

            if (runs(i).Id == -1)
            {
                time.Text = "";
            }
            else
            {
                time.Text = Util.FormatTime(runs(i).RunTime);
            }
        }
    }

    public class TimePlaceEntry : LabelW
    {
        private readonly Func<int, RunInfo> runs;
        private readonly int i;

        public TimePlaceEntry(int i, CFont font, Func<int, RunInfo> runs) :
            base("", font)
        {
            this.i = i;
            this.runs = runs;
            Align = new Vector2(0f, 0.5f);
        }

        public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
        {
            base.Update(mouseInsideParent, events, crop);

            if (runs(i).Id == -1)
            {
                Text = "";
            }
            else
            {
                long time = runs(i).RunTime;
                Text = Util.FormatTime(time) + " (#" + (runs(i).Place + 1) + ")";
            }
        }
    }

    public abstract class LeaderboardRunsMenu : LeaderboardTableMenu
    {
        protected List<RunInfo> runs;

        protected int expanded = -1;
        private int requestedId = -1;
        private Playback.EPlaybackType requestedPlaybackType = default;

        public LeaderboardRunsMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts, string title) :
            base(onMenuChange, fonts, title)
        {
            runs = new List<RunInfo>();

            table.EntryHeight = (i) =>
                {
                    if (i == expanded)
                        return 300f;
                    else
                        return 40f;
                };
            table.OnClickRow = (wevent, i) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                    {
                        if (expanded != -1)
                            table.Refresh(expanded);
                        table.Refresh(i);
                        
                        if (i == expanded)
                            expanded = -1;
                        else
                            expanded = i;
                    }
                };
            table.Hook = (i, widget) =>
                {
                    if (i != expanded)
                        return widget;

                    LabelW generalLabels = new LabelW("", fonts.FontMedium.Font)
                    {
                        Align = Vector2.Zero,
                        Color = new Color(185, 253, 224),
                        Text =
                            "Player:\nMap:\nCategory:\nTime:\nPlace:\nWas WR:\nDate:\nID:"
                    };

                    LabelW generalValues = new LabelW("", fonts.FontMedium.Font)
                    {
                        Align = Vector2.Zero,
                        Text =
                            SteamCache.GetName(runs[i].PlayerId) + "\n" +
                            Map.MapIdToName[runs[i].MapId] + "\n" +
                            ((ECategory)runs[i].Category).Label() + "\n" +
                            Util.FormatTime(runs[i].RunTime) + "\n" +
                            (runs[i].Place != -1 ? "" + (runs[i].Place + 1) : "-") + "\n" +
                            (runs[i].WasWR == 1 ? "Yes" : "No") + "\n" +
                            DateTimeOffset.FromUnixTimeSeconds(runs[i].CreateTime).LocalDateTime.ToString("dd MMM yyyy") + "\n" +
                            runs[i].Id
                    };

                    LabelW statsLabels = new LabelW("", fonts.FontMedium.Font)
                    {
                        Align = Vector2.Zero,
                        Color = new Color(185, 253, 224),
                        Text =
                            "Jumps:\nGrapples:\nDistance:\nAvg. speed:\nBoost used:"
                    };

                    LabelW statsValues = new LabelW("", fonts.FontMedium.Font)
                    {
                        Align = Vector2.Zero,
                        Text =
                            runs[i].Jumps + "\n" +
                            runs[i].Grapples + "\n" +
                            runs[i].TravDist + "\n" +
                            runs[i].AvgSpeed + "\n" +
                            runs[i].BoostUsed + "%"
                    };

                    ImageW avatar = new ImageW(SteamCache.GetAvatar(runs[i].PlayerId));

                    LabelW viewProfile = new LabelW("View profile", fonts.FontMedium.Font);
                    Style.ApplyButton(viewProfile);
                    viewProfile.OnClick = (wevent) =>
                        {
                            if (wevent.Button == WEMouseClick.EButton.LEFT)
                                onMenuChange(new PlayerMenu(onMenuChange, fonts, runs[i].PlayerId));
                        };

                    LayoutW avatarLayout = new LayoutW(EOrientation.VERTICAL);
                    avatarLayout.AddChild(avatar, 180f);
                    avatarLayout.AddSpace(10f);
                    avatarLayout.AddChild(viewProfile, 40f);
                    avatarLayout.AddSpace(FILL);

                    LayoutW infos = new LayoutW(EOrientation.HORIZONTAL);
                    infos.AddSpace(40f);
                    infos.AddChild(generalLabels, 120f);
                    infos.AddChild(generalValues, 150f);
                    infos.AddSpace(100f);
                    infos.AddChild(statsLabels, 140f);
                    infos.AddChild(statsValues, 150f);
                    infos.AddSpace(FILL);
                    infos.AddChild(avatarLayout, 180f);
                    infos.AddSpace(20f);

                    LabelW assignGhost = new LabelW("Set ghost", fonts.FontMedium.Font);
                    Style.ApplyButton(assignGhost);
                    assignGhost.OnClick = (wevent) =>
                        {
                            if (wevent.Button == WEMouseClick.EButton.LEFT)
                                RequestRecording(runs[i].Id, Playback.EPlaybackType.SET_GHOST);
                        };
                    LabelW viewReplay = new LabelW("View replay", fonts.FontMedium.Font);
                    Style.ApplyButton(viewReplay);
                    viewReplay.OnClick = (wevent) =>
                    {
                        if (wevent.Button == WEMouseClick.EButton.LEFT)
                            RequestRecording(runs[i].Id, Playback.EPlaybackType.VIEW_REPLAY);
                    };
                    LabelW verify = new LabelW("Verify", fonts.FontMedium.Font);
                    Style.ApplyButton(verify);
                    verify.OnClick = (wevent) =>
                    {
                        if (wevent.Button == WEMouseClick.EButton.LEFT)
                            RequestRecording(runs[i].Id, Playback.EPlaybackType.VERIFY);
                    };

                    LayoutW buttons = new LayoutW(EOrientation.HORIZONTAL);
                    buttons.AddSpace(40f);
                    buttons.AddChild(assignGhost, 240f);
                    buttons.AddSpace(10f);
                    buttons.AddChild(viewReplay, 240f);
                    buttons.AddSpace(10f);
                    buttons.AddChild(verify, 240f);
                    buttons.AddSpace(FILL);

                    LayoutW layout = new LayoutW(EOrientation.VERTICAL);
                    layout.AddSpace(4f);
                    layout.AddChild(infos, FILL);
                    layout.AddSpace(4f);
                    layout.AddChild(buttons, 40f);
                    layout.AddSpace(4f);
                    layout.OnClick = widget.OnClick;
                    return layout;
                };
        }

        private void RequestRecording(int id, Playback.EPlaybackType type)
        {
            if (id == requestedId && type == requestedPlaybackType)
                return;

            requestedId = id;
            requestedPlaybackType = type;

            RunsDatabase.Instance.GetRunHandlers[0].Cancel();
            int currentMapId = Map.GetCurrentMapId();
            if (currentMapId == -1 || RunsDatabase.Instance.Get(id).MapId != currentMapId)
            {
                Error = "Please enter the respective map first!";
                return;
            }
            RunsDatabase.Instance.RequestRecording(id, 
                (recording) =>
                {
                    onMenuChange(null);
                    LocalGameMods.Instance.StartPlayback(recording, type);
                },
                (error) => Error = error.Message
            );
        }
    }

    public class BestForMapMenu : LeaderboardRunsMenu
    {
        private readonly LabelW backButton;
        private readonly MultiSelectButton categorySelect;

        private readonly int mapId = -1;
        private ECategory category = ECategory.NEW_LAP;

        public BestForMapMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts, int mapId) :
            base(onMenuChange, fonts, Map.MapIdToName[mapId])
        {
            runs = new List<RunInfo>();

            this.mapId = mapId;

            RunsDatabase.Instance.GetRecordingHandler.Cancel();
            RunsDatabase.Instance.RequestPBsForMapCat(mapId, category, Refresh, (error) => Error = error.Message);

            table.AddColumn("#", 50, (i) => new PlaceEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));
            table.AddColumn("Player", FILL, (i) => new PlayerEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));
            table.AddColumn("Time", 200, (i) => new TimeEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));
            table.AddColumn("Age", 200, (i) => new AgeEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));
            
            backButton = new LabelW("Back", fonts.FontMedium.Font);
            Style.ApplyButton(backButton);
            backButton.OnClick = click =>
                {
                    RunsDatabase.Instance.GetRecordingHandler.Cancel();
                    if (click.Button == WEMouseClick.EButton.LEFT)
                        onMenuChange(new TopRunsMenu(onMenuChange, fonts));
                };

            string[] categories = Map.HasSkip(mapId) ?
                new string[] { "New Lap", "1 Lap", "New Lap (Skip)", "1 Lap (Skip)" } :
                new string[] { "New Lap", "1 Lap" };

            categorySelect = new MultiSelectButton(categories, 0, fonts.FontMedium.Font)
            {
                OnSelect = newCategory =>
                {
                    if (category == (ECategory)newCategory)
                        return;
                    category = (ECategory)newCategory;
                    RunsDatabase.Instance.GetRecordingHandler.Cancel();
                    RunsDatabase.Instance.RequestPBsForMapCat(mapId, category, Refresh, (error) => Error = error.Message);
                    Refresh();
                    expanded = -1;
                }
            };
            Style.ApplyMultiSelectButton(categorySelect);

            buttonRow.AddChild(backButton, 200);
            buttonRow.AddSpace(FILL);
            buttonRow.AddChild(categorySelect, Map.HasSkip(mapId) ? 800 : 400);
        }

        public override void Refresh()
        {
            runs.Clear();

            RunsDatabase.Instance.GetPBsForMapCat(mapId, category, runs);
            
            table.RowCount = runs.Count;
        }
    }

    public class PlayerMenu : LeaderboardTableMenu
    {
        private readonly ImageW avatar;
        
        private readonly RunInfo[,] runs = new RunInfo[Map.COUNT, (int)ECategory.COUNT];

        private readonly ulong playerId;

        public PlayerMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts, ulong playerId) :
            base(onMenuChange, fonts, SteamCache.GetName(playerId))
        {
            this.playerId = playerId;

            RunsDatabase.Instance.GetRecordingHandler.Cancel();
            RunsDatabase.Instance.RequestPlayerPBs(playerId, Refresh, (error) => Error = error.Message);

            table.AddColumn("Map", FILL, (i) => new AllMapEntry(i, fonts.FontMedium.Font));
            table.AddColumn("New Lap", 220, (i) => new TimePlaceEntry(i, fonts.FontMedium.Font, (i_) => runs[i_, (int)ECategory.NEW_LAP]));
            table.AddColumn("1 Lap", 220, (i) => new TimePlaceEntry(i, fonts.FontMedium.Font, (i_) => runs[i_, (int)ECategory.ONE_LAP]));
            table.AddColumn("New Lap (Skip)", 220, (i) => new TimePlaceEntry(i, fonts.FontMedium.Font, (i_) => runs[i_, (int)ECategory.NEW_LAP_SKIPS]));
            table.AddColumn("1 Lap (Skip)", 220, (i) => new TimePlaceEntry(i, fonts.FontMedium.Font, (i_) => runs[i_, (int)ECategory.ONE_LAP_SKIPS]));
            table.OnClickRow = (wevent, i) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                    onMenuChange(new BestForMapMenu(onMenuChange, fonts, i));
            };
            titleBar.ClearChildren();
            avatar = new ImageW(SteamCache.GetAvatar(playerId));
            titleBar.AddChild(avatar, 60f);
            titleBar.AddSpace(10f);
            titleBar.AddChild(title, FILL);

            PageSelect.Add(buttonRow, 0, onMenuChange, fonts);
        }

        public override void Refresh()
        {
            RunsDatabase.Instance.GetPlayerPBs(playerId, runs);

            table.RowCount = Map.AllowedMaps.Count;
        }
    }

    public static class PageSelect
    {
        public static void Add(LayoutW row, int index, Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts)
        {
            MultiSelectButton pageSelect = new MultiSelectButton(new[] { "Top runs", "Recent runs", "Top players" }, index, fonts.FontMedium.Font);
            Style.ApplyMultiSelectButton(pageSelect);
            pageSelect.OnSelect = (i) =>
                {
                    switch (i)
                    {
                        case 0:
                            onMenuChange(new TopRunsMenu(onMenuChange, fonts));
                            break;
                        case 1:
                            onMenuChange(new RecentRuns(onMenuChange, fonts));
                            break;
                        case 2:
                            onMenuChange(new TopPlayersMenu(onMenuChange, fonts));
                            break;
                    }
                };

            row.AddChild(pageSelect, 600f);
        }
    }

    public class TopRunsMenu : LeaderboardTableMenu
    {
        private readonly RunInfo[,] runs = new RunInfo[Map.COUNT, (int)ECategory.COUNT];

        public TopRunsMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts) :
            base(onMenuChange, fonts, "Top runs")
        {
            RunsDatabase.Instance.GetRecordingHandler.Cancel();
            RunsDatabase.Instance.RequestWRs(Refresh, (error) => Error = error.Message);

            table.AddColumn("Map", FILL, (i) => new AllMapEntry(i, fonts.FontMedium.Font));
            table.AddColumn("New Lap", 220, (i) => new PlayerTimeEntry(i, fonts.FontMedium.Font, (i_) => runs[i_, (int)ECategory.NEW_LAP]));
            table.AddColumn("1 Lap", 220, (i) => new PlayerTimeEntry(i, fonts.FontMedium.Font, (i_) => runs[i_, (int)ECategory.ONE_LAP]));
            table.AddColumn("New Lap (Skip)", 220, (i) => new PlayerTimeEntry(i, fonts.FontMedium.Font, (i_) => runs[i_, (int)ECategory.NEW_LAP_SKIPS]));
            table.AddColumn("1 Lap (Skip)", 220, (i) => new PlayerTimeEntry(i, fonts.FontMedium.Font, (i_) => runs[i_, (int)ECategory.ONE_LAP_SKIPS]));
            table.EntryHeight = (i) => 84f;
            table.OnClickRow = (wevent, i) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                        onMenuChange(new BestForMapMenu(onMenuChange, fonts, i));
                };

            PageSelect.Add(buttonRow, 0, onMenuChange, fonts);
        }

        public override void Refresh()
        {
            RunsDatabase.Instance.GetWRs(runs);

            table.RowCount = Map.AllowedMaps.Count;
        }
    }

    public class RecentRuns : LeaderboardRunsMenu
    {
        public RecentRuns(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts) :
            base(onMenuChange, fonts, "Recent runs")
        {
            RunsDatabase.Instance.GetRecordingHandler.Cancel();
            RunsDatabase.Instance.RequestRecent(Refresh, (error) => Error = error.Message);

            table.AddColumn("Map", FILL, (i) => new MapEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));
            table.AddColumn("Category", 170f, (i) => new CategoryEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));
            table.AddColumn("Player", 350f, (i) => new PlayerEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));
            table.AddColumn("Time", 170f, (i) => new TimeEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));
            table.AddColumn("Age", 170f, (i) => new AgeEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));
            table.AddColumn("#", 40f, (i) => new PlaceEntry(i, fonts.FontMedium.Font, (i_) => runs[i_]));

            PageSelect.Add(buttonRow, 1, onMenuChange, fonts);
        }

        public override void Refresh()
        {
            runs.Clear();
            
            RunsDatabase.Instance.GetRecent(runs);

            table.RowCount = runs.Count;
        }
    }

    public class TopPlayersMenu : LeaderboardTableMenu
    {
        public class PlayerEntry : LayoutW
        {
            private readonly List<PlayerInfo> players;

            private readonly ImageW image;
            private readonly LabelW label;

            private readonly int i;

            public PlayerEntry(int i, CFont font, List<PlayerInfo> players) :
                base(EOrientation.HORIZONTAL)
            {
                this.i = i;
                this.players = players;
                image = new ImageW(null);
                label = new LabelW("", font)
                {
                    Align = new Vector2(0f, 0.5f)
                };

                AddChild(image, 33f);
                AddSpace(4f);
                AddChild(label, FILL);
            }

            public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
            {
                base.Update(mouseInsideParent, events, crop);

                image.Image = SteamCache.GetAvatar(players[i].PlayerId);
                label.Text = SteamCache.GetName(players[i].PlayerId);
            }
        }

        public class WrCountEntry : LabelW
        {
            private readonly List<PlayerInfo> players;

            private readonly int i;

            public WrCountEntry(int i, CFont font, List<PlayerInfo> players) :
                base("", font)
            {
                this.i = i;
                this.players = players;

                Align = new Vector2(0f, 0.5f);
            }

            public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
            {
                base.Update(mouseInsideParent, events, crop);

                Text = "" + players[i].WrCount;
            }
        }

        private readonly List<PlayerInfo> players = new List<PlayerInfo>();

        public TopPlayersMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts) :
            base(onMenuChange, fonts, "Top players")
        {
            RunsDatabase.Instance.GetRecordingHandler.Cancel();
            RunsDatabase.Instance.RequestWRs(Refresh, (error) => Error = error.Message);

            table.AddColumn("Player", FILL, (i) => new PlayerEntry(i, fonts.FontMedium.Font, players));
            table.AddColumn("WRs", 80f, (i) => new WrCountEntry(i, fonts.FontMedium.Font, players));

            table.OnClickRow = (wevent, i) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                    onMenuChange(new PlayerMenu(onMenuChange, fonts, players[i].PlayerId));
            };

            PageSelect.Add(buttonRow, 2, onMenuChange, fonts);
        }

        public override void Refresh()
        {
            players.Clear();

            RunsDatabase.Instance.GetTopPlayers(players);

            table.RowCount = players.Count;
        }
    }
}
