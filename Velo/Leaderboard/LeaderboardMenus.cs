using System;
using System.Collections.Generic;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Steamworks;
using CEngine.Util.UI.Widget;
using System.Runtime.CompilerServices;
using CEngine.Graphics.Layer;

namespace Velo
{
    public static class Style
    {
        public static void ApplyText(LabelW text)
        {
            text.Color = () => Leaderboard.Instance.TextColor.Value.Get();
        }

        public static void ApplyTextHeader(LabelW text)
        {
            text.Color = () => Leaderboard.Instance.HeaderTextColor.Value.Get();
        }

        public static void ApplyButton(LabelW button)
        {
            button.Hoverable = true;
            button.BackgroundVisible = true;
            button.BackgroundVisibleHovered = true;
            button.Color = () => Leaderboard.Instance.TextColor.Value.Get();
            button.BackgroundColor = () => Leaderboard.Instance.ButtonColor.Value.Get();
            button.BackgroundColorHovered = () => Leaderboard.Instance.ButtonHoveredColor.Value.Get();
        }

        public static void ApplySelectorButton(SelectorButtonW button)
        {
            button.ButtonBackgroundVisible = true;
            button.ButtonBackgroundVisibleHovered = true;
            button.ButtonBackgroundVisibleSelected = true;
            button.Color = () => Leaderboard.Instance.TextColor.Value.Get();
            button.ButtonBackgroundColor = () => Leaderboard.Instance.ButtonColor.Value.Get();
            button.ButtonBackgroundColorHovered = () => Leaderboard.Instance.ButtonHoveredColor.Value.Get();
            button.ButtonBackgroundColorSelected = () => Leaderboard.Instance.ButtonSelectedColor.Value.Get();
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

    public abstract class LbMenu : HolderW<Widget>
    {
        protected LbMenuContext context;

        public LbMenu(LbMenuContext context) :
            base()
        {
            this.context = context;
        }

        public abstract void Refresh();
        public abstract void Rerequest();
        public abstract void ResetState();
    }

    public abstract class LbMenuPage : LbMenu
    {
        protected LabelW title;
        protected LayoutW titleBar;
        protected HolderW<Widget> content;
        protected readonly LayoutW buttonRowUpper;
        protected readonly LayoutW buttonRowLower;
        protected LayoutW layout;

        private readonly bool showStatus;

        private float loadingRotation = -(float)Math.PI / 2f;

        public LbMenuPage(LbMenuContext context, string title, bool buttonRowUpper = false, bool buttonRowLower = false, bool buttonRowSpace = true, bool showStatus = true) :
            base(context)
        {
            this.showStatus = showStatus;

            content = new HolderW<Widget>();

            layout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            if (title != "")
            {
                this.title = new LabelW(title, context.Fonts.FontLarge.Font)
                {
                    Align = new Vector2(0f, 0.5f),
                    Color = () => Leaderboard.Instance.HeaderTextColor.Value.Get()
                };
                titleBar = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                titleBar.AddChild(this.title, LayoutW.FILL);
                layout.AddChild(titleBar, 80f);
                layout.AddSpace(10f);
            }
            layout.AddChild(content, LayoutW.FILL);
            if (buttonRowSpace && (buttonRowLower || buttonRowUpper))
                layout.AddSpace(10f);
            if (buttonRowUpper)
            {
                this.buttonRowUpper = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                layout.AddChild(this.buttonRowUpper, 35f);
            }
            if (buttonRowLower)
            {
                this.buttonRowLower = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                layout.AddChild(this.buttonRowLower, 35f);
            }

            Child = layout;
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!showStatus)
                return;

            Widget lowest = layout.Children.Last();
            if (lowest == null)
                return;

            if (RunsDatabase.Instance.Pending())
            {
                context.Error = null;

                float dt = (float)Velo.Delta.TotalSeconds;
                if (dt > 1f)
                    dt = 1f;

                loadingRotation += 3f * dt;

                Velo.SpriteBatch.End();
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);

                Vector2 pos =
                    lowest.Position +
                    new Vector2(lowest.Size.X + 8f, (lowest.Size.Y - LoadSymbol.SIZE) / 2f) +
                    Vector2.One * LoadSymbol.SIZE / 2f;
                Velo.SpriteBatch.Draw(LoadSymbol.Get(), pos * scale, new Rectangle?(), Color.White * opacity, loadingRotation, Vector2.One * LoadSymbol.SIZE / 2f, scale, SpriteEffects.None, 0f);
            }
            else
            {
                loadingRotation = -(float)Math.PI / 2f;
            }
            if (context.Error != "" && context.Error != null)
            {
                Velo.SpriteBatch.End();
                Velo.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, CEffect.None.Effect);

                CTextDrawComponent errorDraw = new CTextDrawComponent("", context.Fonts.FontMedium.Font, Vector2.Zero)
                {
                    IsVisible = true,
                    StringText = Util.LineBreaks("Error: " + context.Error, 30),
                    Color = Color.Red,
                    HasDropShadow = true,
                    DropShadowColor = Color.Black,
                    DropShadowOffset = Vector2.One,
                    Opacity = opacity
                };
                errorDraw.UpdateBounds();

                errorDraw.Position =
                    (lowest.Position +
                    new Vector2(lowest.Size.X + 8f, (lowest.Size.Y - errorDraw.Size.Y) / 2f)) * scale;
                errorDraw.Scale = Vector2.One * scale;
                errorDraw.Draw(null);
            }
        }
    }

    public abstract class LbTable<T> : LbMenu, ITableEntryFactory<T> where T : struct
    {
        protected readonly TableW<T> table;

        public LbTable(LbMenuContext context) :
            base(context)
        {
            table = new TableW<T>(context.Fonts.FontMedium.Font, 0, 40, this)
            {
                HeaderAlign = new Vector2(0f, 0.5f),
                HeaderColor = () => Leaderboard.Instance.HeaderTextColor.Value.Get(),
                EntryBackgroundVisible = true,
                EntryBackgroundColor1 = () => Leaderboard.Instance.EntryColor1.Value.Get(),
                EntryBackgroundColor2 = () => Leaderboard.Instance.EntryColor2.Value.Get(),
                EntryHoverable = true,
                EntryBackgroundColorHovered = () => Leaderboard.Instance.EntryHoveredColor.Value.Get(),
                ScrollBarColor = () => Leaderboard.Instance.ButtonColor.Value.Get(),
                ScrollBarWidth = Leaderboard.Instance.ScrollBarWidth.Value
            };
            table.AddSpace(10f);

            Child = table;
        }

        public override void ResetState()
        {
            table.ResetScrollState();
        }
        public abstract IEnumerable<T> GetElems();
        public abstract float Height(T elem, int i);
    }

    public class PlaceEntry : LabelW
    {
        public PlaceEntry(CFont font, RunInfo run) :
            base("", font)
        {
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = Leaderboard.Instance.HighlightTextColor.Value.Get;
            if (run.Id == -1)
                Text = "";
            else if (run.Place != -1)
                Text = "" + (run.Place + 1);
            else
                Text = "-";
        }
    }

    public class PlayerEntry : LayoutW
    {
        private readonly ImageW image;
        private readonly LabelW label;

        private readonly RunInfo run;

        public PlayerEntry(CFont font, RunInfo run, int maxNameLength = 100) :
            base(EOrientation.HORIZONTAL)
        {
            this.run = run;
            
            if (run.Id == -1)
                return;

            image = new ImageW(null);
            label = new LabelW("", font)
            {
                Align = new Vector2(0f, 0.5f)
            };

            AddChild(image, 33f);
            AddSpace(4f);
            AddChild(label, FILL);
            Style.ApplyText(label);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                label.Color = Leaderboard.Instance.HighlightTextColor.Value.Get;

            image.Image = SteamCache.GetAvatar(run.PlayerId);
            label.Text = SteamCache.GetName(run.PlayerId);
            if (label.Text.Length > maxNameLength)
                label.Text = label.Text.Substring(0, maxNameLength) + "...";
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            if (run.Id == -1)
                return;

            image.Image = SteamCache.GetAvatar(run.PlayerId);
            label.Text = SteamCache.GetName(run.PlayerId);

            base.Draw(hovered, scale, opacity);
        }
    }

    public class TimeEntry : LabelW
    {
        public TimeEntry(CFont font, RunInfo run) :
            base("", font)
        {
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = Leaderboard.Instance.HighlightTextColor.Value.Get;

            if (run.Id == -1)
            {
                Text = "";
            }
            else
            {
                long time = run.RunTime;
                Text = Util.FormatTime(time, Leaderboard.Instance.TimeFormat.Value);
            }
        }
    }

    public class AgeEntry : LabelW
    {
        private RunInfo run;

        public AgeEntry(CFont font, RunInfo run) :
            base("", font)
        {
            this.run = run;

            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = Leaderboard.Instance.HighlightTextColor.Value.Get;
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            if (run.Id == -1)
            {
                Text = "";
            }
            else
            {
                long now = DateTimeOffset.Now.ToUnixTimeSeconds();
                long diff = now - run.CreateTime;
                Text = Util.ApproxTime(diff);
            }

            base.Draw(hovered, scale, opacity);
        }
    }

    public class MapEntry : LabelW
    {
        public MapEntry(CFont font, RunInfo run) :
            base("", font)
        {
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = Leaderboard.Instance.HighlightTextColor.Value.Get;

            if (run.Id == -1)
            {
                Text = "";
            }
            else
            {
                Text = Map.MapIdToName[run.Category.MapId];
            }
        }
    }

    public class CategoryEntry : LabelW
    {
        public CategoryEntry(CFont font, RunInfo run) :
            base("", font)
        {
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = Leaderboard.Instance.HighlightTextColor.Value.Get;

            if (run.Id == -1)
            {
                Text = "";
            }
            else
            {
                Text = ((ECategoryType)run.Category.TypeId).Label();
            }
        }
    }

    public class AllMapEntry : LabelW
    {
        public AllMapEntry(CFont font, int mapId) :
            base("", font)
        {
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            Text = Map.MapIdToName[mapId];
        }
    }

    public class PlayerTimeEntry : LayoutW
    {
        private readonly PlayerEntry player;

        private readonly LabelW time;

        public PlayerTimeEntry(CFont font, RunInfo run) :
            base(EOrientation.VERTICAL)
        {
            player = new PlayerEntry(font, run, 13);

            time = new LabelW("", font)
            {
                Align = new Vector2(0f, 0.5f)
            };

            AddChild(time, 40);
            AddSpace(4);
            AddChild(player, 40);
            Style.ApplyText(time);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                time.Color = Leaderboard.Instance.HighlightTextColor.Value.Get;

            if (run.Id == -1)
            {
                time.Text = "";
            }
            else
            {
                time.Text = Util.FormatTime(run.RunTime, Leaderboard.Instance.TimeFormat.Value);
            }
        }
    }

    public class TimePlaceEntry : LabelW
    {
        public TimePlaceEntry(CFont font, RunInfo run) :
            base("", font)
        {
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            
            if (run.Id == -1)
            {
                Text = "";
            }
            else
            {
                long time = run.RunTime;
                Text = Util.FormatTime(time, Leaderboard.Instance.TimeFormat.Value) + " (" + (run.Place != -1 ? "#" + (run.Place + 1) : "-") + ")";
                if (run.Place == 0)
                    Color = Leaderboard.Instance.HighlightTextColor.Value.Get;
            }
        }
    }

    public abstract class LbRunsTable : LbTable<RunInfo>
    {
        private class ExpandedRow : TransitionW<Widget>
        {
            private readonly Widget original;
            private readonly LabelW viewProfile;
            private readonly LabelW setGhost;
            private readonly LabelW viewReplay;
            private readonly LabelW verify;
            private readonly LayoutW layout;

            public Widget Original => original;
            public Widget Expanded => layout;

            public ExpandedRow(Widget original, LbMenuContext context, RunInfo run, Action<int, Playback.EPlaybackType> requestRecording)
            {
                this.original = original;

                LabelW generalLabels = new LabelW("", context.Fonts.FontMedium.Font)
                {
                    Align = Vector2.Zero,
                    Text =
                        "Player:\nMap:\nCategory:\nTime:\nPlace:\nWas WR:\nDate:\nID:"
                };
                Style.ApplyTextHeader(generalLabels);

                LabelW generalValues = new LabelW("", context.Fonts.FontMedium.Font)
                {
                    Align = Vector2.Zero,
                    Text =
                        SteamCache.GetName(run.PlayerId) + "\n" +
                        Map.MapIdToName[run.Category.MapId] + "\n" +
                        ((ECategoryType)run.Category.TypeId).Label() + "\n" +
                        Util.FormatTime(run.RunTime, Leaderboard.Instance.TimeFormat.Value) + "\n" +
                        (run.Place != -1 ? "" + (run.Place + 1) : "-") + "\n" +
                        (run.WasWR == 1 ? "Yes" : "No") + "\n" +
                        DateTimeOffset.FromUnixTimeSeconds(run.CreateTime).LocalDateTime.ToString("dd MMM yyyy") + "\n" +
                        run.Id
                };
                Style.ApplyText(generalValues);

                LabelW statsLabels = new LabelW("", context.Fonts.FontMedium.Font)
                {
                    Align = Vector2.Zero,
                    Text =
                        "Jumps:\nGrapples:\nDistance:\nGround distance:\nSwing distance:\nClimb distance:\nAvg. speed:\nBoost used:"
                };
                Style.ApplyTextHeader(statsLabels);

                LabelW statsValues = new LabelW("", context.Fonts.FontMedium.Font)
                {
                    Align = Vector2.Zero,
                    Text =
                        run.Jumps + "\n" +
                        run.Grapples + "\n" +
                        run.Dist + "\n" +
                        run.GroundDist + "\n" +
                        run.SwingDist + "\n" +
                        run.ClimbDist + "\n" +
                        run.AvgSpeed + "\n" +
                        run.BoostUsed + "%"
                };
                Style.ApplyText(statsValues);

                ImageW avatar = new ImageW(SteamCache.GetAvatar(run.PlayerId));

                viewProfile = new LabelW("View profile", context.Fonts.FontMedium.Font);
                Style.ApplyButton(viewProfile);
                viewProfile.Hoverable = false;
                viewProfile.OnClick = (wevent) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                    {
                        context.ChangePage(new LbPlayerMenuPage(context, run.PlayerId));
                    }
                };

                LayoutW avatarLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
                avatarLayout.AddChild(avatar, 180f);
                avatarLayout.AddSpace(10f);
                avatarLayout.AddChild(viewProfile, 40f);
                avatarLayout.AddSpace(LayoutW.FILL);

                LayoutW infos = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                infos.AddSpace(40f);
                infos.AddChild(generalLabels, 120f);
                infos.AddChild(generalValues, 150f);
                infos.AddSpace(100f);
                infos.AddChild(statsLabels, 200f);
                infos.AddChild(statsValues, 150f);
                infos.AddSpace(LayoutW.FILL);
                infos.AddChild(avatarLayout, 180f);
                infos.AddSpace(20f);

                setGhost = new LabelW("Set ghost", context.Fonts.FontMedium.Font);
                Style.ApplyButton(setGhost);
                setGhost.Hoverable = false;
                setGhost.OnClick = (wevent) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                        requestRecording(run.Id, Playback.EPlaybackType.SET_GHOST);
                };
                viewReplay = new LabelW("Watch replay", context.Fonts.FontMedium.Font);
                Style.ApplyButton(viewReplay);
                viewReplay.Hoverable = false;
                viewReplay.OnClick = (wevent) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                        requestRecording(run.Id, Playback.EPlaybackType.VIEW_REPLAY);
                };
                verify = new LabelW("Verify", context.Fonts.FontMedium.Font);
                Style.ApplyButton(verify);
                verify.Hoverable = false;
                verify.OnClick = (wevent) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                        requestRecording(run.Id, Playback.EPlaybackType.VERIFY);
                };

                LayoutW buttons = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                buttons.AddSpace(40f);
                buttons.AddChild(setGhost, 240f);
                buttons.AddSpace(10f);
                buttons.AddChild(viewReplay, 240f);
                buttons.AddSpace(10f);
                buttons.AddChild(verify, 240f);
                buttons.AddSpace(LayoutW.FILL);

                layout = new LayoutW(LayoutW.EOrientation.VERTICAL);
                layout.AddSpace(10f);
                layout.AddChild(infos, 250f);
                layout.AddSpace(10f);
                if (run.HasComments != 0)
                {
                    LabelW comments = new LabelW(RunsDatabase.Instance.GetComment(run.Id), context.Fonts.FontMedium.Font);
                    Style.ApplyText(comments);
                    comments.Align = Vector2.Zero;
                    comments.Padding = 10f * Vector2.One;
                    ScrollW scroll = new ScrollW(comments)
                    {
                        BackgroundColor = () => new Color(20, 20, 20, 150),
                        BackgroundVisible = true,
                        ScrollBarColor = () => Leaderboard.Instance.ButtonColor.Value.Get()
                    };

                    LayoutW commentsLayout = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                    commentsLayout.AddSpace(10f);
                    commentsLayout.AddChild(scroll, LayoutW.FILL);
                    commentsLayout.AddSpace(10f);

                    layout.AddChild(commentsLayout, 200f);
                    layout.AddSpace(10f);
                }
                layout.AddChild(buttons, 40f);
                layout.AddSpace(10f);

                Crop = true;

                original.BackgroundVisible = false;
                original.Hoverable = false;
                GoTo(original);

                Hoverable = true;
            }

            public void TransitionTo(Widget widget, Action onFinish = null)
            {
                if (widget == Original)
                {
                    viewProfile.Hoverable = false;
                    setGhost.Hoverable = false;
                    viewReplay.Hoverable = false;
                    verify.Hoverable = false;
                }
                TransitionTo(widget, 8f, Vector2.Zero, onFinish: () =>
                {
                    if (widget == Expanded)
                    {
                        viewProfile.Hoverable = true;
                        setGhost.Hoverable = true;
                        viewReplay.Hoverable = true;
                        verify.Hoverable = true;
                    }
                    onFinish?.Invoke();
                });
            }
        }

        private int count;
        private int requestCount;

        private int expandedId = -1;
        private readonly Dictionary<int, ExpandedRow> expanded = new Dictionary<int, ExpandedRow>();

        private int requestedId = -1;
        private Playback.EPlaybackType requestedPlaybackType = default;

        public LbRunsTable(LbMenuContext context) :
            base(context)
        {
            table.OnClickRow = (wevent, row, elem, i) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    if (expandedId != -1)
                    {
                        int id = expandedId;
                        expanded[expandedId].TransitionTo(expanded[expandedId].Original, onFinish: () => expanded.Remove(id));
                    }
                    
                    if (expandedId == elem.Id)
                    {
                        expandedId = -1;
                    }
                    else
                    {
                        expandedId = elem.Id;
                        if (!expanded.TryGetValue(expandedId, out ExpandedRow expandedRow) || expandedRow == null)
                        {
                            expandedRow = new ExpandedRow(row, context, elem, RequestRecording)
                            {
                                OnClick = row.OnClick
                            };
                            expanded.Add(expandedId, expandedRow);
                        }

                        expandedRow.TransitionTo(expandedRow.Expanded);
                    }

                    if (expandedId != -1 && elem.HasComments != 0)
                        RunsDatabase.Instance.RequestComment(elem.Id, null, (error) => context.Error = error.Message);
                    else
                        RunsDatabase.Instance.CancelRequestComment();

                    table.Refresh(i);
                }
            };
            table.Hook = (elem, i, widget) =>
            {
                if (expanded.TryGetValue(elem.Id, out ExpandedRow expandedRow) && expandedRow != null)
                    return expandedRow;
                return widget;
            };
        }

        private void RequestRecording(int id, Playback.EPlaybackType type)
        {
            if (id == requestedId && type == requestedPlaybackType)
                return;

            requestedId = id;
            requestedPlaybackType = type;

            RunsDatabase.Instance.CancelRequestRecording();
            int currentMapId = Map.GetCurrentMapId();
            if (currentMapId == -1 || RunsDatabase.Instance.Get(id).Category.MapId != currentMapId)
            {
                context.Error = "Please enter the respective map first!";
                return;
            }
            if (type == Playback.EPlaybackType.SET_GHOST && Velo.Ghost == null)
            {
                context.Error = "Please spawn a ghost by finishing a lap first!";
                return;
            }

            Recording cachedRecording = RunsDatabase.Instance.GetRecording(id);
            if (cachedRecording != null)
            {
                Velo.AddOnPreUpdate(() =>
                {
                    context.ExitMenu(false);
                    LocalGameMods.Instance.StartPlayback(cachedRecording, type);
                });
            }
            else
            {
                RunsDatabase.Instance.RequestRecording(id,
                    (recording) =>
                    {
                        context.ExitMenu(false);
                        LocalGameMods.Instance.StartPlayback(recording, type);
                    },
                    (error) => context.Error = error.Message
                );
            }
        }

        public override void Refresh()
        {
            table.RowCount = Math.Min(GetElems().Count(), count);
        }

        public override void Rerequest()
        {
            if (count == 0)
            {
                count = 100;
                requestCount = 100;
            }
            Request(Math.Max(0, count - 100), 100, Refresh, (error) => context.Error = error.Message);
        }

        public override void ResetState()
        {
            base.ResetState();
            count = 0;
            requestCount = 0;
        }

        public override float Height(RunInfo elem, int i)
        {
            if (expanded.TryGetValue(elem.Id, out ExpandedRow expandedRow) && expandedRow != null)
            {
                float R = expandedRow.R;
                if (elem.Id != expandedId)
                    R = 1f - R;
                return (1f - R) * 40f + R * (320f + (elem.PlayerId == 0 || elem.HasComments != 0 ? 210f : 0f));
            }
            else
                return 40f;
        }

        public override void UpdateBounds(Rectangle crop)
        {
            base.UpdateBounds(crop);
            
            if (table.ReachedEnd && count == requestCount && count <= GetElems().Count())
            {
                requestCount += 100;
                Request(count, 100, () =>
                {
                    count += 100;
                    Refresh();
                },
                (error) => context.Error = error.Message);
            }
        }

        protected abstract void Request(int start, int count, Action onSuccess, Action<Exception> onFailure);
    }

    public class LbMapRunsTable : LbRunsTable
    {
        public enum EFilter
        {
            PBS_ONLY, WR_HISTORY, ALL,
            COUNT
        }

        private readonly Category category;
        private readonly EFilter filter;

        public LbMapRunsTable(LbMenuContext context, Category category, EFilter filter) :
            base(context)
        {
            this.category = category;
            this.filter = filter;

            table.AddColumn("#", 50, (run) => new PlaceEntry(context.Fonts.FontMedium.Font, run));
            table.AddColumn("Player", LayoutW.FILL, (run) => new PlayerEntry(context.Fonts.FontMedium.Font, run));
            table.AddColumn("Time", 200, (run) => new TimeEntry(context.Fonts.FontMedium.Font, run));
            table.AddColumn("Age", 200, (run) => new AgeEntry(context.Fonts.FontMedium.Font, run));
        }

        public override IEnumerable<RunInfo> GetElems()
        {
            switch (filter)
            {
                case EFilter.PBS_ONLY:
                    return RunsDatabase.Instance.GetPBsForCategory(category);
                case EFilter.WR_HISTORY:
                    return RunsDatabase.Instance.GetWRHistoryForCategory(category);
                case EFilter.ALL:
                    return RunsDatabase.Instance.GetAllForCategory(category);
                default:
                    return null;
            }
        }

        protected override void Request(int start, int count, Action onSuccess, Action<Exception> onFailure)
        {
            switch (filter)
            {
                case EFilter.PBS_ONLY:
                    RunsDatabase.Instance.PushRequestRuns(new GetPBsForCategoryRequest(category, start, count), onSuccess);
                    break;
                case EFilter.WR_HISTORY:
                    RunsDatabase.Instance.PushRequestRuns(new GetWRHistoryForCategoryRequest(category, start, count), onSuccess);
                    break;
                case EFilter.ALL:
                    RunsDatabase.Instance.PushRequestRuns(new GetAllForCategoryRequest(category, start, count), onSuccess);
                    break;
            }
            RunsDatabase.Instance.RunRequestRuns(onFailure);
        }
    }

    public class LbMapMenuPage : LbMenuPage
    {
        private readonly SelectorButtonW categorySelect;
        private readonly SelectorButtonW filterSelect;
        private readonly DualTabbedW<LbMapRunsTable> tables;
        private readonly LabelW backButton;
        private readonly LabelW workshopPageButton;
        private readonly LayoutW workshopPageButtonLayout;

        public LbMapMenuPage(LbMenuContext context, int mapId) :
            base(context, Map.MapIdToName[mapId], buttonRowUpper: true, buttonRowLower: true)
        {
            string[] categories = Map.HasSkip(mapId) ?
                new string[] { "New Lap", "1 Lap", "New Lap (Skip)", "1 Lap (Skip)" } :
                new string[] { "New Lap", "1 Lap" };

            categorySelect = new SelectorButtonW(categories, 0, context.Fonts.FontMedium.Font);
            Style.ApplySelectorButton(categorySelect);
           
            string[] filters = new string[] { "PBs only", "WR history", "All" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium.Font);
            Style.ApplySelectorButton(filterSelect);

            tables = new DualTabbedW<LbMapRunsTable>(categorySelect, filterSelect);
            for (int i1 = 0; i1 < categories.Length; i1++)
            {
                for (int i2 = 0; i2 < filters.Length; i2++)
                {
                    tables.SetTab(i1, i2, new LbMapRunsTable(context, new Category { MapId = (byte)mapId, TypeId = (byte)i1 }, (LbMapRunsTable.EFilter)i2));
                }
            }
            tables.OnSwitch = context.Rerequest;

            content.Child = tables;
            
            backButton = new LabelW("Back", context.Fonts.FontMedium.Font);
            Style.ApplyButton(backButton);
            backButton.OnClick = click =>
            {
                if (click.Button == WEMouseClick.EButton.LEFT)
                {
                    context.PopPage();
                }
            };

            buttonRowUpper.AddSpace(LayoutW.FILL);
            buttonRowUpper.AddChild(filterSelect, 600);

            buttonRowLower.AddChild(backButton, 200);
            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(categorySelect, Map.HasSkip(mapId) ? 800 : 400);

            if (Map.MapIdToFileId.TryGetValue(mapId, out ulong fileId))
            {
                workshopPageButton = new LabelW("Workshop page", context.Fonts.FontMedium.Font);
                Style.ApplyButton(workshopPageButton);
                workshopPageButton.OnClick = click =>
                {
                    if (click.Button == WEMouseClick.EButton.LEFT)
                    {
                        SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/sharedfiles/filedetails/?id=" + fileId);
                    }
                };
                workshopPageButtonLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
                workshopPageButtonLayout.AddSpace(LayoutW.FILL);
                workshopPageButtonLayout.AddChild(workshopPageButton, 35f);

                titleBar.AddSpace(LayoutW.FILL);
                titleBar.AddChild(workshopPageButtonLayout, 220f);
            }
        }

        public override void Refresh()
        {
            tables.Current.Refresh();
        }

        public override void Rerequest()
        {
            tables.Current.Rerequest();
        }

        public override void ResetState()
        {
            tables.Tabs.ForEach(tab => tab.ResetState());
        }
    }

    public class LbPlayerRunsTable : LbTable<MapRunInfos>
    {
        public enum EFilter
        {
            OFFICIAL, RWS, OLD_RWS,
            COUNT
        }

        private readonly ulong playerId;
        private readonly EFilter filter;

        public ulong PlayerId => playerId;

        public LbPlayerRunsTable(LbMenuContext context, ulong playerId, EFilter filter) :
            base(context)
        {
            this.playerId = playerId;
            this.filter = filter;

            table.AddColumn("Map", LayoutW.FILL, (runs) => new AllMapEntry(context.Fonts.FontMedium.Font, runs.MapId));
            table.AddColumn("New Lap", 220, (runs) => new TimePlaceEntry(context.Fonts.FontMedium.Font, runs.NewLap));
            table.AddColumn("1 Lap", 220, (runs) => new TimePlaceEntry(context.Fonts.FontMedium.Font, runs.OneLap));
            table.AddColumn("New Lap (Skip)", 220, (runs) => new TimePlaceEntry(context.Fonts.FontMedium.Font, runs.NewLapSkip));
            table.AddColumn("1 Lap (Skip)", 220, (runs) => new TimePlaceEntry(context.Fonts.FontMedium.Font, runs.OneLapSkip));
            table.OnClickRow = (wevent, row, runs, i) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    context.ChangePage(new LbMapMenuPage(context, runs.MapId));
                }
            };
        }

        public override IEnumerable<MapRunInfos> GetElems()
        {
            switch (filter)
            {
                case EFilter.OFFICIAL:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId).Where((infos) => Map.IsOfficial(infos.MapId));
                case EFilter.RWS:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId).Where((infos) => Map.IsRWS(infos.MapId));
                case EFilter.OLD_RWS:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId).Where((infos) => Map.IsOldRWS(infos.MapId));
                default:
                    return null;
            }
        }

        public override float Height(MapRunInfos elem, int i)
        {
            return 40f;
        }

        public override void Refresh()
        {
            table.RowCount = GetElems().Count();
        }

        public override void Rerequest()
        {
            RunsDatabase.Instance.PushRequestRuns(new GetPlayerPBsRequest(PlayerId), null);
        }
    }

    public class LbPlayerMenuPage : LbMenuPage
    {
        private readonly ImageW avatar;
        private readonly LabelW statsTitles1;
        private readonly LabelW statsValues1;
        private readonly LabelW statsTitles2;
        private readonly LabelW statsValues2;
        private readonly LabelW backButton;
        private readonly SelectorButtonW filterSelect;
        private readonly TabbedW<LbPlayerRunsTable> tables;
        private readonly LabelW steamPageButton;
        private readonly LayoutW steamPageButtonLayout;

        private readonly ulong playerId;

        public ulong PlayerId => playerId;

        public LbPlayerMenuPage(LbMenuContext context, ulong playerId) :
            base(context, SteamCache.GetName(playerId), buttonRowLower: true)
        {
            this.playerId = playerId;

            titleBar.ClearChildren();
            avatar = new ImageW(SteamCache.GetAvatar(playerId));
            statsTitles1 = new LabelW("PBs:\nWRs:", context.Fonts.FontMedium.Font);
            Style.ApplyText(statsTitles1);
            statsTitles1.Align = new Vector2(0f, 0.5f);
            statsValues1 = new LabelW("", context.Fonts.FontMedium.Font);
            Style.ApplyText(statsValues1);
            statsValues1.Align = new Vector2(0f, 0.5f);
            statsTitles2 = new LabelW("Score:\n", context.Fonts.FontMedium.Font);
            Style.ApplyText(statsTitles2);
            statsTitles2.Align = new Vector2(0f, 0.5f);
            statsValues2 = new LabelW("", context.Fonts.FontMedium.Font);
            Style.ApplyText(statsValues2);
            statsValues2.Align = new Vector2(0f, 0.5f);
            titleBar.AddChild(avatar, 60f);
            titleBar.AddSpace(10f);
            titleBar.AddChild(title, LayoutW.FILL);

            backButton = new LabelW("Back", context.Fonts.FontMedium.Font);
            Style.ApplyButton(backButton);
            backButton.OnClick = click =>
            {
                RunsDatabase.Instance.CancelAll();
                if (click.Button == WEMouseClick.EButton.LEFT)
                {
                    context.PopPage();
                }
            };

            string[] filters = new string[] { "Official", "RWS", "Old RWS" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium.Font);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbPlayerRunsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                tables.SetTab(i, new LbPlayerRunsTable(context, playerId, (LbPlayerRunsTable.EFilter)i));
            }
            tables.OnSwitch = context.Rerequest;

            content.Child = tables;

            buttonRowLower.AddChild(backButton, 200f);
            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(filterSelect, 600f);

            steamPageButton = new LabelW("Steam page", context.Fonts.FontMedium.Font);
            Style.ApplyButton(steamPageButton);
            steamPageButton.OnClick = click =>
            {
                if (click.Button == WEMouseClick.EButton.LEFT)
                {
                    SteamFriends.ActivateGameOverlayToUser("steamid", new CSteamID(playerId));
                }
            };

            steamPageButtonLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            steamPageButtonLayout.AddChild(steamPageButton, 35f);
            steamPageButtonLayout.Offset = new Vector2(0f, 7f);

            titleBar.AddChild(steamPageButtonLayout, 180f);
            titleBar.AddSpace(10f);
            titleBar.AddChild(statsTitles1, 65f);
            titleBar.AddChild(statsValues1, 65f);
            titleBar.AddChild(statsTitles2, 80f);
            titleBar.AddChild(statsValues2, 80f);
        }

        public override void Refresh()
        {
            tables.Current.Refresh();

            statsValues1.Text = "";
            statsValues2.Text = "";

            int count = 0;
            foreach (MapRunInfos info in RunsDatabase.Instance.GetPlayerPBs(playerId))
            {
                if (info.NewLap.Id != -1) count++;
                if (info.OneLap.Id != -1) count++;
                if (info.NewLapSkip.Id != -1) count++;
                if (info.OneLapSkip.Id != -1) count++;
            }
            statsValues1.Text += "" + count;

            bool found = false;
            foreach (PlayerInfoWRs wrs in RunsDatabase.Instance.GetWRCounts())
            {
                if (wrs.PlayerId == PlayerId)
                {
                    statsValues1.Text += "\n" + wrs.WrCount;
                    found = true;
                    break;
                }
            }
            if (!found)
                statsValues1.Text += "\n0";

            found = false;
            foreach (PlayerInfoScore score in RunsDatabase.Instance.GetScores())
            {
                if (score.PlayerId == PlayerId)
                {
                    statsValues2.Text = "" + score.Score;
                    found = true;
                    break;
                }
            }
            if (!found)
                statsValues2.Text += "0";

            statsValues2.Text += "\n";
        }

        public override void Rerequest()
        {
            tables.Current.Rerequest();
            RunsDatabase.Instance.PushRequestScores(null);
            RunsDatabase.Instance.PushRequestRuns(new GetWRsRequest(), Refresh);
            RunsDatabase.Instance.RunRequestRuns((error) => context.Error = error.Message);
        }

        public override void ResetState()
        {
            tables.Tabs.ForEach(tab => tab.ResetState());
        }
    }

    public class LbTopRunsTable : LbTable<MapRunInfos>
    {
        public enum EFilter
        {
            OFFICIAL, RWS, OLD_RWS,
            COUNT
        }

        private readonly EFilter filter;

        public LbTopRunsTable(LbMenuContext context, EFilter filter) :
            base(context)
        {
            this.filter = filter;

            table.AddColumn("Map", LayoutW.FILL, (runs) => new AllMapEntry(context.Fonts.FontMedium.Font, runs.MapId));
            table.AddColumn("New Lap",        220, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium.Font, runs.NewLap));
            table.AddColumn("1 Lap",          220, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium.Font, runs.OneLap));
            table.AddColumn("New Lap (Skip)", 220, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium.Font, runs.NewLapSkip));
            table.AddColumn("1 Lap (Skip)",   220, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium.Font, runs.OneLapSkip));
            table.OnClickRow = (wevent, row, runs, i) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                    {
                        context.ChangePage(new LbMapMenuPage(context, runs.MapId));
                    }
                };
        }

        public override IEnumerable<MapRunInfos> GetElems()
        {
            switch (filter)
            {
                case EFilter.OFFICIAL:
                    return RunsDatabase.Instance.GetWRs().Where((infos) => Map.IsOfficial(infos.MapId));
                case EFilter.RWS:
                    return RunsDatabase.Instance.GetWRs().Where((infos) => Map.IsRWS(infos.MapId));
                case EFilter.OLD_RWS:
                    return RunsDatabase.Instance.GetWRs().Where((infos) => Map.IsOldRWS(infos.MapId));
                default:
                    return null;
            }
        }

        public override float Height(MapRunInfos elem, int i)
        {
            return 84f;
        }

        public override void Refresh()
        {
            table.RowCount = GetElems().Count();
        }

        public override void Rerequest()
        {
            RunsDatabase.Instance.PushRequestRuns(new GetWRsRequest(), Refresh);
            RunsDatabase.Instance.RunRequestRuns((error) => context.Error = error.Message);
        }
    }

    public class LbTopRunsMenuPage : LbMenuPage
    {
        private readonly SelectorButtonW filterSelect;
        private readonly TabbedW<LbTopRunsTable> tables;

        public LbTopRunsMenuPage(LbMenuContext context) :
            base(context, "Top runs", buttonRowLower: true, showStatus: false)
        {
            string[] filters = new string[] { "Official", "RWS", "Old RWS" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium.Font);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbTopRunsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                tables.SetTab(i, new LbTopRunsTable(context, (LbTopRunsTable.EFilter)i));
            }
            tables.OnSwitch = context.Rerequest;

            content.Child = tables;

            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(filterSelect, 600f);
        }

        public override void Refresh()
        {
            tables.Current.Refresh();
        }

        public override void Rerequest()
        {
            tables.Current.Rerequest();
        }

        public override void ResetState()
        {
            tables.Tabs.ForEach(tab => tab.ResetState());
        }
    }

    public class LbRecentRunsTable : LbRunsTable
    {
        public enum EFilter
        {
            ALL, WRS_ONLY,
            COUNT
        }

        private readonly EFilter filter;

        public LbRecentRunsTable(LbMenuContext context, EFilter filter) :
            base(context)
        {
            this.filter = filter;

            table.AddColumn("Map", LayoutW.FILL, (run) => new MapEntry(context.Fonts.FontMedium.Font, run));
            table.AddColumn("Category", 170f, (run) => new CategoryEntry(context.Fonts.FontMedium.Font, run));
            table.AddColumn("Player", 350f, (run) => new PlayerEntry(context.Fonts.FontMedium.Font, run));
            table.AddColumn("Time", 170f, (run) => new TimeEntry(context.Fonts.FontMedium.Font, run));
            table.AddColumn("Age", 150f, (run) => new AgeEntry(context.Fonts.FontMedium.Font, run));
            table.AddColumn("#", 40f, (run) => new PlaceEntry(context.Fonts.FontMedium.Font, run));
        }

        public override IEnumerable<RunInfo> GetElems()
        {
            switch (filter)
            {
                case EFilter.ALL:
                    return RunsDatabase.Instance.GetRecent();
                case EFilter.WRS_ONLY:
                    return RunsDatabase.Instance.GetRecentWRs();
                default:
                    return null;
            }
        }

        protected override void Request(int start, int count, Action onSuccess, Action<Exception> onFailure)
        {
            switch (filter)
            {
                case EFilter.ALL:
                    RunsDatabase.Instance.PushRequestRuns(new GetRecentRequest(start, count), onSuccess);
                    break;
                case EFilter.WRS_ONLY:
                    RunsDatabase.Instance.PushRequestRuns(new GetRecentWRsRequest(start, count), onSuccess);
                    break;
            }
            RunsDatabase.Instance.RunRequestRuns(onFailure);
        }
    }

    public class LbRecentMenuPage : LbMenuPage
    {
        private readonly SelectorButtonW filterSelect;
        private readonly TabbedW<LbRecentRunsTable> tables;

        public LbRecentMenuPage(LbMenuContext context) :
            base(context, "Recent runs", buttonRowLower: true, showStatus: false)
        {
            string[] filters = new string[] { "All", "WRs only" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium.Font);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbRecentRunsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                tables.SetTab(i, new LbRecentRunsTable(context, (LbRecentRunsTable.EFilter)i));
            }
            tables.OnSwitch = context.Rerequest;

            content.Child = tables;

            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(filterSelect, 400f);
        }

        public override void Refresh()
        {
            tables.Current.Refresh();
        }

        public override void Rerequest()
        {
            tables.Current.Rerequest();
        }

        public override void ResetState()
        {
            tables.Tabs.ForEach(tab => tab.ResetState());
        }
    }

    public class LbTopPlayersScoreTable : LbTable<PlayerInfoScore>
    {
        public class PlaceEntry : LabelW
        {
            public PlaceEntry(CFont font, PlayerInfoScore player) :
                base("", font)
            {
                Align = new Vector2(0f, 0.5f);
                Text = "" + (player.Place + 1);
                Style.ApplyText(this);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    Color = Leaderboard.Instance.HighlightTextColor.Value.Get;
            }
        }

        public class PlayerEntry : LayoutW
        {
            private readonly ImageW image;
            private readonly LabelW label;

            private readonly PlayerInfoScore player;

            public PlayerEntry(CFont font, PlayerInfoScore player) :
                base(EOrientation.HORIZONTAL)
            {
                this.player = player;

                image = new ImageW(null);
                label = new LabelW("", font)
                {
                    Align = new Vector2(0f, 0.5f)
                };

                AddChild(image, 33f);
                AddSpace(4f);
                AddChild(label, FILL);
                Style.ApplyText(label);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    label.Color = Leaderboard.Instance.HighlightTextColor.Value.Get;
            }

            public override void Draw(Widget hovered, float scale, float opacity)
            {
                image.Image = SteamCache.GetAvatar(player.PlayerId);
                label.Text = SteamCache.GetName(player.PlayerId);

                base.Draw(hovered, scale, opacity);
            }
        }

        public class ScoreEntry : LabelW
        {
            public ScoreEntry(CFont font, PlayerInfoScore player) :
                base("", font)
            {
                Align = new Vector2(0f, 0.5f);
                Style.ApplyText(this);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    Color = Leaderboard.Instance.HighlightTextColor.Value.Get;

                Text = "" + player.Score;
            }
        }

        public LbTopPlayersScoreTable(LbMenuContext context) :
            base(context)
        {
            table.AddColumn("#", 50f, (player) => new PlaceEntry(context.Fonts.FontMedium.Font, player));
            table.AddColumn("Player", LayoutW.FILL, (player) => new PlayerEntry(context.Fonts.FontMedium.Font, player));
            table.AddColumn("Score", 120f, (player) => new ScoreEntry(context.Fonts.FontMedium.Font, player));

            table.OnClickRow = (wevent, row, player, i) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    context.ChangePage(new LbPlayerMenuPage(context, player.PlayerId));
                }
            };
        }

        public override IEnumerable<PlayerInfoScore> GetElems()
        {
            return RunsDatabase.Instance.GetScores();
        }

        public override float Height(PlayerInfoScore elem, int i)
        {
            return 40f;
        }

        public override void Refresh()
        {
            table.RowCount = RunsDatabase.Instance.GetScores().Count();
        }

        public override void Rerequest()
        {
            RunsDatabase.Instance.PushRequestScores(Refresh);
            RunsDatabase.Instance.RunRequestRuns((error) => context.Error = error.Message);
        }
    }

    public class LbTopPlayersWRsTable : LbTable<PlayerInfoWRs>
    {
        public class PlaceEntry : LabelW
        {
            public PlaceEntry(CFont font, PlayerInfoWRs player) :
                base("", font)
            {
                Align = new Vector2(0f, 0.5f);
                Text = "" + (player.Place + 1);
                Style.ApplyText(this);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    Color = Leaderboard.Instance.HighlightTextColor.Value.Get;
            }
        }

        public class PlayerEntry : LayoutW
        {
            private readonly ImageW image;
            private readonly LabelW label;

            private readonly PlayerInfoWRs player;

            public PlayerEntry(CFont font, PlayerInfoWRs player) :
                base(EOrientation.HORIZONTAL)
            {
                this.player = player;

                image = new ImageW(null);
                label = new LabelW("", font)
                {
                    Align = new Vector2(0f, 0.5f)
                };

                AddChild(image, 33f);
                AddSpace(4f);
                AddChild(label, FILL);
                Style.ApplyText(label);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    label.Color = Leaderboard.Instance.HighlightTextColor.Value.Get;
            }

            public override void Draw(Widget hovered, float scale, float opacity)
            {
                image.Image = SteamCache.GetAvatar(player.PlayerId);
                label.Text = SteamCache.GetName(player.PlayerId);

                base.Draw(hovered, scale, opacity);
            }
        }

        public class WrCountEntry : LabelW
        {
            public WrCountEntry(CFont font, PlayerInfoWRs player) :
                base("", font)
            {
                Align = new Vector2(0f, 0.5f);
                Style.ApplyText(this);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    Color = Leaderboard.Instance.HighlightTextColor.Value.Get;

                Text = "" + player.WrCount;
            }
        }

        public LbTopPlayersWRsTable(LbMenuContext context) :
            base(context)
        {
            table.AddColumn("#", 50f, (player) => new PlaceEntry(context.Fonts.FontMedium.Font, player));
            table.AddColumn("Player", LayoutW.FILL, (player) => new PlayerEntry(context.Fonts.FontMedium.Font, player));
            table.AddColumn("WRs", 80f, (player) => new WrCountEntry(context.Fonts.FontMedium.Font, player));

            table.OnClickRow = (wevent, row, player, i) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    context.ChangePage(new LbPlayerMenuPage(context, player.PlayerId));
                }
            };
        }

        public override IEnumerable<PlayerInfoWRs> GetElems()
        {
            return RunsDatabase.Instance.GetWRCounts();
        }

        public override float Height(PlayerInfoWRs elem, int i)
        {
            return 40f;
        }

        public override void Refresh()
        {
            table.RowCount = RunsDatabase.Instance.GetWRCounts().Count();
        }

        public override void Rerequest()
        {
            RunsDatabase.Instance.PushRequestRuns(new GetWRsRequest(), Refresh);
            RunsDatabase.Instance.RunRequestRuns((error) => context.Error = error.Message);
        }
    }

    public class LbTopPlayersMenuPage : LbMenuPage
    {
        public enum EType
        {
            SCORE, WR_COUNT,
            COUNT
        }

        private readonly SelectorButtonW typeSelector;
        private readonly TabbedW<LbMenu> tables;

        public LbTopPlayersMenuPage(LbMenuContext context) :
            base(context, "Top players", buttonRowLower: true, showStatus: false)
        {
            string[] types = new[] { "Score", "WR count" };

            typeSelector = new SelectorButtonW(types, 0, context.Fonts.FontMedium.Font);
            Style.ApplySelectorButton(typeSelector);

            tables = new TabbedW<LbMenu>(typeSelector);
            tables.SetTab((int)EType.SCORE, new LbTopPlayersScoreTable(context));
            tables.SetTab((int)EType.WR_COUNT, new LbTopPlayersWRsTable(context));
            tables.OnSwitch = context.Rerequest;

            content.Child = tables;

            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(typeSelector, 400f);
        }

        public override void Refresh()
        {
            tables.Current.Refresh();
        }

        public override void Rerequest()
        {
            tables.Current.Rerequest();
        }

        public override void ResetState()
        {
            tables.Tabs.ForEach(tab => tab.ResetState());
        }
    }

    public class LbRulesMenuPage : LbMenuPage
    {
        protected LabelW rules;
        protected ScrollW scroll;

        public LbRulesMenuPage(LbMenuContext context) :
            base(context, "Rules")
        {
            rules = new LabelW("", context.Fonts.FontMedium.Font)
            {
                Align = Vector2.Zero,
                Padding = 10f * Vector2.One
            };

            scroll = new ScrollW(rules)
            {
                BackgroundVisible = true,
                BackgroundColor = () => Leaderboard.Instance.EntryColor2.Value.Get(),
                ScrollBarColor = () => Leaderboard.Instance.ButtonColor.Value.Get(),
                ScrollBarWidth = Leaderboard.Instance.ScrollBarWidth.Value
            };

            content.Child = scroll;

            layout.AddSpace(10f);
            
            rules.Text =
@"All runs are automatically verified and categorized by Velo itself before being automatically submitted
as you play. In case an invalid run still manages to get submitted either by a bug or in a cheated 
manner, contact a leaderboard moderator and they will be able to remove the run.

A run is categorized as ""1 lap"" if any of the following apply:
  -Lap was started by finishing a previous lap
  -Player had boost upon starting the lap
  -Player had boostacoke upon starting the lap (laboratory only)
  -A gate was not closed upon starting the lap (except Club V)
  -A fall tile (black obstacle) was broken upon starting the lap
  -Lap did not start from countdown and reset lasers setting is disabled (powerplant and libary only)
  -Player pressed lap reset while climbing and reset wall boost setting is disabled
  -Player pressed lap reset right after pressing jump and reset jump boost setting is disabled

A run is categorized as ""Skip"" if any of the following apply:
  -Player missed a secondary checkpoint

A run is categorized as ""New lap"" if none of the above apply.

A run is invalid if any of the following apply:
  -An obstacle was broken upon starting the lap
  -Player used an item (5 second cooldown, infinite cooldown for bombs)
  -Player was in a drill state (5 second cooldown)
  -An item actor was still alive upon starting the lap
  -A Ghost blocked a laser (use ""disable ghost laser interaction"")
  -Player had boostacoke upon starting the lap (any except laboratory)
  -Player modified their boostacoke by pressing + or -
  -Time of the run is longer than 2 minutes
  -An illegal Velo mod was used
   (1 second cooldown for savestates, infinite cooldown for any other)
  -Map was not Velo curated
  -Option SuperSpeedRunners, SpeedRapture or Destructible Environment was enabled
  -Player paused the game
  -Player missed a primary checkpoint

Cooldowns last until running out or pressing reset.

The checkpoint system:
In order to differentiate between Skip and non-Skip runs and to further improve validation, Velo makes
use of a checkpoint system comprised of primary and secondary checkpoints (not to be confused with
the game's own checkpoint system). These checkpoints are made up of rectangular sections scattered
around each map. In order for a run to be valid, the player needs to hit all primary checkpoints,
and to be categorized as non-Skip, the player needs to hit all secondary checkpoints.

The score system:
Each PB run grants you somewhere between 1 to 1000 points, depending on how close it is to the WR
run. If we denote WR time as 'wr' and your PB time as 'pb', you get 1000 * wr / (wr + 25 * (wr - pb))
points per run.";
        }

        public override void Refresh()
        {
            
        }

        public override void Rerequest()
        {

        }

        public override void ResetState()
        {

        }
    }

    public class LbMainMenuPage : LbMenuPage
    {
        public enum EPages
        {
            TOP_RUNS, TOP_PLAYERS, RECENT_RUNS, RULES
        }

        private readonly SelectorButtonW pageSelector;
        private readonly TabbedW<LbMenuPage> pages;

        public LbMainMenuPage(LbMenuContext context) :
            base(context, "", buttonRowLower: true, buttonRowSpace: false)
        {
            string[] pages = new[] { "Top runs", "Top players", "Recent runs", "Rules" };

            pageSelector = new SelectorButtonW(pages, 0, context.Fonts.FontMedium.Font);
            Style.ApplySelectorButton(pageSelector);

            this.pages = new TabbedW<LbMenuPage>(pageSelector);
            this.pages.SetTab((int)EPages.TOP_RUNS, new LbTopRunsMenuPage(context));
            this.pages.SetTab((int)EPages.RECENT_RUNS, new LbRecentMenuPage(context));
            this.pages.SetTab((int)EPages.TOP_PLAYERS, new LbTopPlayersMenuPage(context));
            this.pages.SetTab((int)EPages.RULES, new LbRulesMenuPage(context));
            this.pages.OnSwitch = context.Rerequest;

            content.Child = this.pages;

            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(pageSelector, 800f);
        }

        public override void Refresh()
        {
            pages.Current.Refresh();
        }

        public override void Rerequest()
        {
            pages.Current.Rerequest();
        }

        public override void ResetState()
        {
            pages.Tabs.ForEach(page => page.ResetState());
        }
    }
}
