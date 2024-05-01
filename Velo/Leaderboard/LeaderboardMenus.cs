using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public LeaderboardMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts, LayoutW.EOrientation orientation) :
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
        protected TableW table;

        protected LayoutW buttonRow;

        private float loadingRotation = 0f;
        private TimeSpan lastFrameTime = TimeSpan.Zero;

        public LeaderboardTableMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts, string title) :
            base(onMenuChange, fonts, LayoutW.EOrientation.VERTICAL)
        {
            this.title = new LabelW(title, fonts.FontLarge);
            this.title.Align = new Vector2(0f, 0.5f);
            this.title.Color = new Color(185, 253, 224);

            table = new TableW(fonts.FontMedium, 0, 40, (i) => 40f);
            table.HeaderAlign = new Vector2(0f, 0.5f);
            table.HeaderColor = new Color(185, 253, 224);
            table.EntryBackgroundVisible = true;
            table.EntryBackgroundColor1 = Style.ENTRY_COLOR1;
            table.EntryBackgroundColor2 = Style.ENTRY_COLOR2;
            table.EntryHoverable = true;
            table.EntryBackgroundColorHovered = Style.ENTRY_COLOR_HOVERED;
            table.AddSpace(5f);

            buttonRow = new LayoutW(LayoutW.EOrientation.HORIZONTAL);

            AddChild(this.title, 80f);
            AddSpace(10f);
            AddChild(table, -1);
            AddSpace(5);
            AddChild(buttonRow, 35);
        }

        public override void Draw(Widget hovered)
        {
            base.Draw(hovered);

            if (
                RunsDatabase.Instance.GetRunHandlers[0].Status == ERequestStatus.PENDING ||
                RunsDatabase.Instance.GetRecordingHandler.Status == ERequestStatus.PENDING
                )
            {
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
                Velo.SpriteBatch.Draw(LoadSymbol.Get(), pos, new Rectangle?(), Color.White, loadingRotation, Vector2.One * LoadSymbol.SIZE / 2f, 1f, SpriteEffects.None, 0f);
            }
            else
            {
                loadingRotation = 0f;
            }
        }
    }

    public class BestForMapMenu : LeaderboardTableMenu
    {
        public class PlaceEntry : LabelW
        {
            private int i;

            public PlaceEntry(int i, CFont font) :
                base("", font)
            {
                this.i = i;
                Align = new Vector2(0f, 0.5f);
            }

            public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
            {
                base.Update(mouseInsideParent, events, crop);
                Text = "" + (i + 1);
            }
        }

        public class PlayerEntry : LayoutW
        {
            private List<RunInfo> runs;

            private ImageW image;
            private LabelW label;

            private int i;

            public PlayerEntry(int i, CFont font, List<RunInfo> runs) :
                base(LayoutW.EOrientation.HORIZONTAL)
            {
                this.i = i;
                this.runs = runs;
                image = new ImageW(null);
                label = new LabelW("", font);
                label.Align = new Vector2(0f, 0.5f);

                AddChild(image, 33f);
                AddSpace(4f);
                AddChild(label, LayoutW.FILL);
            }

            public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
            {
                base.Update(mouseInsideParent, events, crop);

                image.Image = SteamCache.GetAvatar(runs[i].PlayerId);
                label.Text = SteamCache.GetName(runs[i].PlayerId);
            }
        }

        public class TimeEntry : LabelW
        {
            private List<RunInfo> runs;
            private int i;

            public TimeEntry(int i, CFont font, List<RunInfo> runs) :
                base("", font)
            {
                this.i = i;
                this.runs = runs;
                Align = new Vector2(0f, 0.5f);
            }

            public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
            {
                base.Update(mouseInsideParent, events, crop);

                long time = runs[i].RunTime;
                Text = Util.FormatTime(time);
            }
        }

        public class AgeEntry : LabelW
        {
            private List<RunInfo> runs;
            private int i;

            public AgeEntry(int i, CFont font, List<RunInfo> runs) :
                base("", font)
            {
                this.i = i;
                this.runs = runs;
                Align = new Vector2(0f, 0.5f);
            }

            public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
            {
                base.Update(mouseInsideParent, events, crop);

                long now = DateTimeOffset.Now.ToUnixTimeSeconds(); 
                long diff = now - runs[i].CreateTime;
                Text = Util.ApproxTime(diff);
            }
        }

        private enum ERecordingRequestType
        {
            NONE, ASSIGN_GHOST, VIEW_REPLAY, VERIFY
        }

        private List<RunInfo> runs;

        private LabelW topRunsButton;
        private MultiSelectButton categorySelect;

        private int mapId = -1;
        private ECategory category = ECategory.NEW_LAP;

        private int expanded = -1;

        private int requestedId = -1;

        public BestForMapMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts, int mapId) :
            base(onMenuChange, fonts, Map.MapIdToName[mapId])
        {
            runs = new List<RunInfo>();

            this.mapId = mapId;

            RunsDatabase.Instance.RequestPBsForMapCat(mapId, category, Refresh);

            table.AddColumn("#", 50, (i) => new PlaceEntry(i, fonts.FontMedium));
            table.AddColumn("Player", TableW.FILL, (i) => new PlayerEntry(i, fonts.FontMedium, runs));
            table.AddColumn("Time", 200, (i) => new TimeEntry(i, fonts.FontMedium, runs));
            table.AddColumn("Age", 200, (i) => new AgeEntry(i, fonts.FontMedium, runs));
            table.EntryHeight = (i) =>
                {
                    if (i == expanded)
                        return 88f;
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
                   
                    LabelW assignGhost = new LabelW("Assign ghost", fonts.FontMedium);
                    Style.ApplyButton(assignGhost);
                    assignGhost.OnClick = (wevent) =>
                        {
                            if (wevent.Button == WEMouseClick.EButton.LEFT)
                                RequestRecording(runs[i].Id, ERecordingRequestType.ASSIGN_GHOST);
                        };
                    LabelW viewReplay = new LabelW("View replay", fonts.FontMedium);
                    Style.ApplyButton(viewReplay);
                    viewReplay.OnClick = (wevent) =>
                    {
                        if (wevent.Button == WEMouseClick.EButton.LEFT)
                            RequestRecording(runs[i].Id, ERecordingRequestType.VIEW_REPLAY);
                    };
                    LabelW verify = new LabelW("Verify", fonts.FontMedium);
                    Style.ApplyButton(verify);
                    verify.OnClick = (wevent) =>
                    {
                        if (wevent.Button == WEMouseClick.EButton.LEFT)
                            RequestRecording(runs[i].Id, ERecordingRequestType.VERIFY);
                    };

                    LayoutW buttons = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                    buttons.AddSpace(10f);
                    buttons.AddChild(assignGhost, 240f);
                    buttons.AddSpace(10f);
                    buttons.AddChild(viewReplay, 240f);
                    buttons.AddSpace(10f);
                    buttons.AddChild(verify, 240f);

                    LayoutW layout = new LayoutW(LayoutW.EOrientation.VERTICAL);
                    layout.AddChild(widget, 40f);
                    layout.AddSpace(4f);
                    layout.AddChild(buttons, 40f);
                    layout.AddSpace(4f);
                    layout.OnClick = widget.OnClick;
                    return layout;
                };

            topRunsButton = new LabelW("Top runs", fonts.FontMedium);
            Style.ApplyButton(topRunsButton);
            topRunsButton.OnClick = click =>
                {
                    RunsDatabase.Instance.GetRecordingHandler.Cancel();
                    if (click.Button == WEMouseClick.EButton.LEFT)
                        onMenuChange(new TopRunsMenu(onMenuChange, fonts));
                };
            
            categorySelect = new MultiSelectButton(new string[] { "New Lap", "1 Lap", "New Lap (Skip)", "1 Lap (Skip)" }, 0, fonts.FontMedium);
            categorySelect.OnSelect = newCategory =>
                {
                    if (category == (ECategory)newCategory)
                        return;
                    category = (ECategory)newCategory;
                    RunsDatabase.Instance.GetRecordingHandler.Cancel();
                    RunsDatabase.Instance.RequestPBsForMapCat(mapId, category, Refresh);
                    Refresh();
                    expanded = -1;
                };
            Style.ApplyMultiSelectButton(categorySelect);

            buttonRow.AddChild(topRunsButton, 200);
            buttonRow.AddSpace(LayoutW.FILL);
            buttonRow.AddChild(categorySelect, 800);
        }

        public override void Refresh()
        {
            runs.Clear();
           
            RunsDatabase.Instance.GetPBsForMapCat(mapId, category, runs);
            
            table.RowCount = runs.Count;
        }

        private float Height(int i)
        {
            return 40f;
        }

        private void RequestRecording(int id, ERecordingRequestType type)
        {
            if (id == requestedId)
                return;

            requestedId = id;

            RunsDatabase.Instance.RequestRecording(id, (recording) =>
                {
                    onMenuChange(null);
                    Velo.AddOnPreUpdate(() =>
                    {
                        TAS.Instance.StartPlayback(recording);
                    });
                });
        }
    }

    public class TopRunsMenu : LeaderboardTableMenu
    {
        public class MapEntry : LabelW
        {
            private int i;

            public MapEntry(int i, CFont font) :
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
            private RunInfo[,] runs;

            private ImageW image;
            private LabelW player;
            private LayoutW imageAndPlayer;

            private LabelW time;

            private int i;
            private ECategory category;

            public PlayerTimeEntry(int i, ECategory category, CFont font, RunInfo[,] runs) :
                base(LayoutW.EOrientation.VERTICAL)
            {
                this.i = i;
                this.category = category;
                this.runs = runs;
                image = new ImageW(null);
                player = new LabelW("", font);
                player.Align = new Vector2(0f, 0.5f);

                time = new LabelW("", font);
                time.Align = new Vector2(0f, 0.5f);
                
                imageAndPlayer = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                imageAndPlayer.AddChild(image, 33f);
                imageAndPlayer.AddSpace(4f);
                imageAndPlayer.AddChild(player, LayoutW.FILL);

                AddChild(time, 40);
                AddSpace(4);
                AddChild(imageAndPlayer, 40);
            }

            public override void Update(bool mouseInsideParent, Events events, Rectangle crop)
            {
                base.Update(mouseInsideParent, events, crop);

                if (runs[i, (int)category].Id == -1)
                {
                    image.Visible = false;
                    player.Visible = false;
                    time.Visible = false;
                    return;
                }

                image.Visible = true;
                player.Visible = true;
                time.Visible = true;
                image.Image = SteamCache.GetAvatar(runs[i, (int)category].PlayerId);
                player.Text = SteamCache.GetName(runs[i, (int)category].PlayerId);
                time.Text = Util.FormatTime(runs[i, (int)category].RunTime);
            }
        }

        private RunInfo[,] runs = new RunInfo[Map.COUNT, (int)ECategory.COUNT];

        private LabelW topPlayersButton;

        public TopRunsMenu(Action<LeaderboardMenu> onMenuChange, LeaderboardFonts fonts) :
            base(onMenuChange, fonts, "Top runs")
        {
            RunsDatabase.Instance.RequestWRs(Refresh);

            table.AddColumn("Map", TableW.FILL, (i) => new MapEntry(i, fonts.FontMedium));
            table.AddColumn("New Lap", 220, (i) => new PlayerTimeEntry(i, ECategory.NEW_LAP, fonts.FontMedium, runs));
            table.AddColumn("1 Lap", 220, (i) => new PlayerTimeEntry(i, ECategory.ONE_LAP, fonts.FontMedium, runs));
            table.AddColumn("New Lap (Skip)", 220, (i) => new PlayerTimeEntry(i, ECategory.NEW_LAP_SKIPS, fonts.FontMedium, runs));
            table.AddColumn("1 Lap (Skip)", 220, (i) => new PlayerTimeEntry(i, ECategory.ONE_LAP_SKIPS, fonts.FontMedium, runs));
            table.EntryHeight = (i) => 84f;
            table.OnClickRow = (wevent, i) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                        onMenuChange(new BestForMapMenu(onMenuChange, fonts, i));
                };

            topPlayersButton = new LabelW("Top players", fonts.FontMedium);
            Style.ApplyButton(topPlayersButton);

            buttonRow.AddChild(topPlayersButton, 200f);
        }

        public override void Refresh()
        {
            RunsDatabase.Instance.GetWRs(runs);

            table.RowCount = Map.AllowedMaps.Count;
        }
    }
}
