using System;
using System.Collections.Generic;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Steamworks;
using static Velo.Playback;
using System.Threading.Tasks;

namespace Velo
{
    public abstract class LbMenuPage : RequestingMenu
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
                this.title = new LabelW(title, context.Fonts.FontTitle)
                {
                    Align = new Vector2(0f, 0.5f),
                    Color = SettingsUI.Instance.HeaderTextColor.Value.Get
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

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            base.Draw(hovered, scale, opacity);

            if (!showStatus)
                return;

            IWidget lowest = layout.Children.Last();
            if (lowest == null)
                return;

            if (RunsDatabase.Instance.Pending())
            {
                context.Error = null;

                float dt = (float)Velo.RealDelta.TotalSeconds;
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

    public abstract class LbTable<T> : RequestingMenu, ITableEntryFactory<T> where T : struct
    {
        protected readonly TableW<T> table;

        public LbTable(LbMenuContext context) :
            base(context)
        {
            table = new TableW<T>(context.Fonts.FontMedium, 0, 40, this)
            {
                HeaderAlign = new Vector2(0f, 0.5f),
                HeaderColor = SettingsUI.Instance.HeaderTextColor.Value.Get,
                EntryBackgroundVisible = true,
                EntryBackgroundColor1 = SettingsUI.Instance.EntryColor1.Value.Get,
                EntryBackgroundColor2 = SettingsUI.Instance.EntryColor2.Value.Get,
                EntryHoverable = true,
                EntryBackgroundColorHovered = SettingsUI.Instance.EntryHoveredColor.Value.Get,
                ScrollBarColor = SettingsUI.Instance.ButtonColor.Value.Get,
                ScrollBarWidth = SettingsUI.Instance.ScrollBarWidth.Value
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
        public PlaceEntry(CachedFont font, RunInfo run) :
            base("", font)
        {
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
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

        public PlayerEntry(CachedFont font, RunInfo run) :
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
                label.Color = SettingsUI.Instance.HighlightTextColor.Value.Get;

            image.Image = SteamCache.GetAvatar(run.PlayerId);
            label.Text = RunsDatabase.Instance.GetPlayerName(run.PlayerId);
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            if (run.Id == -1)
                return;

            image.Image = SteamCache.GetAvatar(run.PlayerId);
            label.Text = RunsDatabase.Instance.GetPlayerName(run.PlayerId);

            base.Draw(hovered, scale, opacity);
        }
    }

    public class TimeEntry : LabelW
    {
        public TimeEntry(CachedFont font, RunInfo run) :
            base("", font)
        {
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = SettingsUI.Instance.HighlightTextColor.Value.Get;

            if (run.Id == -1)
            {
                Text = "";
            }
            else
            {
                long time = run.RunTime;
                Text = Util.FormatTime(time, Leaderboard.Instance.TimeFormat.Value);
                if (run.SpeedrunCom == 1)
                {
                    int lastDigit = Text.LastIndexOf('9');
                    if (lastDigit != -1)
                        Text = Text.Remove(lastDigit, 1).Insert(lastDigit, "*");
                }
            }
        }
    }

    public class AgeEntry : LabelW
    {
        private RunInfo run;

        public AgeEntry(CachedFont font, RunInfo run) :
            base("", font)
        {
            this.run = run;

            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
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
        private RunInfo run;
        
        public MapEntry(CachedFont font, RunInfo run) :
            base("", font)
        {
            this.run = run;
            
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            if (run.Id == -1)
            {
                Text = "";
            }
            else
            {
                Text = Map.MapIdToName(run.Category.MapId);
            }

            base.Draw(hovered, scale, opacity);
        }
    }

    public class CategoryEntry : LabelW
    {
        public CategoryEntry(CachedFont font, RunInfo run) :
            base("", font)
        {
            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Color = SettingsUI.Instance.HighlightTextColor.Value.Get;

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
        private readonly ulong mapId;

        public AllMapEntry(CachedFont font, ulong mapId) :
            base("", font)
        {
            this.mapId = mapId;

            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            Text = Map.MapIdToName(mapId);

            base.Draw(hovered, scale, opacity);
        }
    }

    public class PlayerTimeEntry : LayoutW
    {
        private readonly PlayerEntry player;

        private readonly LabelW time;

        public PlayerTimeEntry(CachedFont font, RunInfo run, bool compact = false) :
            base(!compact ? EOrientation.VERTICAL : EOrientation.HORIZONTAL)
        {
            player = new PlayerEntry(font, run);

            time = new LabelW("", font)
            {
                Align = new Vector2(!compact ? 0f : 1f, 0.5f)
            };

            if (!compact)
            {
                AddChild(time, 40);
                AddSpace(4);
                AddChild(player, 40);
            }
            else
            {
                AddChild(player, FILL);
                AddSpace(10);
                AddChild(time, 170);
            }
            Style.ApplyText(time);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                time.Color = SettingsUI.Instance.HighlightTextColor.Value.Get;

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
        public TimePlaceEntry(CachedFont font, RunInfo run) :
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
                    Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            }
        }
    }

    public abstract class LbRunsTable : LbTable<RunInfo>
    {
        private class ExpandedRow : TransitionW<Widget>
        {
            private readonly RunInfo run;

            private readonly Widget original;
            private readonly LabelW viewProfile;
            private readonly LabelW mapPage;
            private readonly LabelW setGhost;
            private readonly LabelW viewReplay;
            private readonly LabelW verify;
            private readonly LabelW comments;
            private readonly LayoutW layout;

            public Widget Original => original;
            public Widget Expanded => layout;

            public ExpandedRow(Widget original, LbMenuContext context, RunInfo run, bool mapPageButton, Action<int, Playback.EPlaybackType> requestRecording)
            {
                this.run = run;
                this.original = original;

                LabelW generalLabels = new LabelW("", context.Fonts.FontMedium)
                {
                    Align = Vector2.Zero,
                    Text =
                        "Player:\nMap:\nCategory:\nTime:\nPlace:\nWas WR:\nGrapple CD:\nDate:\nID:"
                };
                Style.ApplyTextHeader(generalLabels);

                LabelW generalValues = new LabelW("", context.Fonts.FontMedium)
                {
                    Align = Vector2.Zero,
                    Text =
                        RunsDatabase.Instance.GetPlayerName(run.PlayerId) + "\n" +
                        Map.MapIdToName(run.Category.MapId) + "\n" +
                        ((ECategoryType)run.Category.TypeId).Label() + "\n" +
                        Util.FormatTime(run.RunTime, Leaderboard.Instance.TimeFormat.Value) + "\n" +
                        (run.Place != -1 ? "" + (run.Place + 1) : "-") + "\n" +
                        (run.WasWR == 1 ? "Yes" : "No") + "\n" +
                        (run.NewGCD == 1 ? "0.20s" : "0.25s") + "\n" +
                        DateTimeOffset.FromUnixTimeSeconds(run.CreateTime).LocalDateTime.ToString("dd MMM yyyy - HH:mm") + "\n" +
                        run.Id
                };
                Style.ApplyText(generalValues);

                LabelW statsLabels = new LabelW("", context.Fonts.FontMedium)
                {
                    Align = Vector2.Zero,
                    Text =
                        "Jumps:\nGrapples:\nDistance:\nGround distance:\nSwing distance:\nClimb distance:\nAvg. speed:\nBoost used:"
                };
                Style.ApplyTextHeader(statsLabels);

                LabelW statsValues = new LabelW("", context.Fonts.FontMedium)
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

                viewProfile = new LabelW("View profile", context.Fonts.FontMedium);
                Style.ApplyButton(viewProfile);
                viewProfile.Hoverable = false;
                viewProfile.OnClick = (wevent) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                        context.ChangePage(new LbPlayerMenuPage(context, run.PlayerId));
                };
                mapPage = new LabelW("Map page", context.Fonts.FontMedium);
                Style.ApplyButton(mapPage);
                mapPage.Hoverable = false;
                mapPage.OnClick = (wevent) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                        context.ChangePage(new LbMapMenuPage(context, run.Category.MapId));
                };

                LayoutW avatarLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
                avatarLayout.AddSpace(30f);
                avatarLayout.AddChild(avatar, 200f);
                avatarLayout.AddSpace(10f);
                avatarLayout.AddChild(viewProfile, 40f);
                avatarLayout.AddSpace(LayoutW.FILL);

                LayoutW infos = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                infos.AddSpace(40f);
                infos.AddChild(generalLabels, 145f);
                infos.AddChild(generalValues, 280f);
                infos.AddSpace(20f);
                infos.AddChild(statsLabels, 195f);
                infos.AddChild(statsValues, 150f);
                infos.AddSpace(LayoutW.FILL);
                infos.AddChild(avatarLayout, 200f);
                infos.AddSpace(40f);

                setGhost = new LabelW("Set ghost", context.Fonts.FontMedium);
                Style.ApplyButton(setGhost);
                setGhost.Hoverable = false;
                setGhost.OnClick = (wevent) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                        requestRecording(run.Id, Playback.EPlaybackType.SET_GHOST);
                };
                viewReplay = new LabelW("Watch replay", context.Fonts.FontMedium);
                Style.ApplyButton(viewReplay);
                viewReplay.Hoverable = false;
                viewReplay.OnClick = (wevent) =>
                {
                    if (wevent.Button == WEMouseClick.EButton.LEFT)
                        requestRecording(run.Id, Playback.EPlaybackType.VIEW_REPLAY);
                };
                verify = new LabelW("Verify", context.Fonts.FontMedium);
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
                if (mapPageButton)
                {
                    buttons.AddChild(mapPage, 200f);
                    buttons.AddSpace(40f);
                }

                layout = new LayoutW(LayoutW.EOrientation.VERTICAL);
                layout.AddSpace(10f);
                layout.AddChild(infos, 280f);
                layout.AddSpace(10f);
                if (run.HasComments != 0)
                {
                    comments = new LabelW(RunsDatabase.Instance.GetComment(run.Id), context.Fonts.FontMedium);
                    Style.ApplyText(comments);
                    comments.Align = Vector2.Zero;
                    comments.Padding = 10f * Vector2.One;
                    ScrollW scroll = new ScrollW(comments)
                    {
                        BackgroundColor = () => new Color(20, 20, 20, 150),
                        BackgroundVisible = true,
                        ScrollBarColor = () => SettingsUI.Instance.ButtonColor.Value.Get()
                    };

                    LayoutW commentsLayout = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
                    commentsLayout.AddSpace(20f);
                    commentsLayout.AddChild(scroll, LayoutW.FILL);
                    commentsLayout.AddSpace(20f);

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
                    mapPage.Hoverable = false;
                    setGhost.Hoverable = false;
                    viewReplay.Hoverable = false;
                    verify.Hoverable = false;
                }
                else
                {
                    Crop = true;
                }
                TransitionTo(widget, 8f, Vector2.Zero, onFinish: () =>
                {
                    if (widget == Expanded)
                    {
                        viewProfile.Hoverable = true;
                        mapPage.Hoverable = true;
                        setGhost.Hoverable = true;
                        viewReplay.Hoverable = true;
                        verify.Hoverable = true;
                    }
                    else
                    {
                        Crop = false;
                    }
                    onFinish?.Invoke();
                });
            }

            public void RefreshComments()
            {
                comments.Text = RunsDatabase.Instance.GetComment(run.Id);
            }
        }

        private int count;
        private int requestCount;

        private int expandedId = -1;
        private readonly Dictionary<int, ExpandedRow> expanded = new Dictionary<int, ExpandedRow>();

        private int requestedId = -1;
        private EPlaybackType requestedPlaybackType = default;

        public LbRunsTable(LbMenuContext context, bool mapPageButton) :
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
                            expandedRow = new ExpandedRow(row, context, elem, mapPageButton, InitiatePlayback)
                            {
                                OnClick = row.OnClick
                            };
                            expanded.Add(expandedId, expandedRow);
                        }

                        expandedRow.TransitionTo(expandedRow.Expanded);

                        if (expandedId != -1 && elem.HasComments != 0)
                            RunsDatabase.Instance.RequestComment(elem.Id, expandedRow.RefreshComments, (error) => context.Error = error.Message);
                        else
                            RunsDatabase.Instance.CancelRequestComment();
                    }

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

        private void InitiatePlayback(int id, Playback.EPlaybackType type)
        {
            if (id == requestedId && type == requestedPlaybackType)
                return;

            requestedId = id;
            requestedPlaybackType = type;

            RunsDatabase.Instance.CancelRequestRecording();
            ulong currentMapId = Map.GetCurrentMapId();
            if (currentMapId == ulong.MaxValue || RunsDatabase.Instance.Get(id).Category.MapId != currentMapId)
            {
                context.Error = "Please enter the respective map first!";
                return;
            }

            int ghostIndex = !OfflineGameMods.Instance.EnableMultiGhost.Value ? 0 : OfflineGameMods.Instance.GhostPlaybackCount();
            if (type == EPlaybackType.SET_GHOST)
                Ghosts.Instance.GetOrSpawn(!OfflineGameMods.Instance.EnableMultiGhost.Value ? 0 : OfflineGameMods.Instance.GhostPlaybackCount(), OfflineGameMods.Instance.GhostDifferentColors.Value);
           
            RunsDatabase.Instance.RequestRecordingCached(id, (recording) =>
                {
                    Task.Run(() =>
                    {
                        if (type == EPlaybackType.SET_GHOST)
                            Ghosts.Instance.WaitForGhost(ghostIndex);
                        Velo.AddOnPreUpdate(() =>
                        {
                            context.ExitMenu(false);
                            OfflineGameMods.Instance.StartPlayback(recording, type, notification: !OfflineGameMods.Instance.DisableReplayNotifications.Value);
                        });
                    });
                },
                (error) => context.Error = error.Message
            );
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
                return (1f - R) * 40f + R * (350f + (elem.PlayerId == 0 || elem.HasComments != 0 ? 210f : 0f));
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
            base(context, mapPageButton: false)
        {
            this.category = category;
            this.filter = filter;

            table.AddColumn("#", 50, (run) => new PlaceEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Player", LayoutW.FILL, (run) => new PlayerEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Time", 200, (run) => new TimeEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Age", 200, (run) => new AgeEntry(context.Fonts.FontMedium, run));
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
        private readonly LabelW enterMapButton;
        private readonly LayoutW enterMapButtonLayout;

        public LbMapMenuPage(LbMenuContext context, ulong mapId) :
            base(context, Map.MapIdToName(mapId), buttonRowUpper: true, buttonRowLower: true)
        {
            string[] categories = !Map.IsOrigins(mapId) ? Map.HasSkip(mapId) ?
                new string[] { "New Lap", "1 Lap", "New Lap (Skip)", "1 Lap (Skip)" } :
                new string[] { "New Lap", "1 Lap" } :
                Map.Has100Perc(mapId) ? 
                new string[] { "Any%", "100%" } :
                new string[] { "Any%" };

            categorySelect = new SelectorButtonW(categories, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(categorySelect);
           
            string[] filters = new string[] { "PBs only", "WR history", "All" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(filterSelect);

            tables = new DualTabbedW<LbMapRunsTable>(categorySelect, filterSelect);
            for (int i1 = 0; i1 < categories.Length; i1++)
            {
                for (int i2 = 0; i2 < filters.Length; i2++)
                {
                    tables.SetTab(i1, i2, new LbMapRunsTable(context, new Category { MapId = mapId, TypeId = (ulong)(!Map.IsOrigins(mapId) ? i1 : i1 + 4) }, (LbMapRunsTable.EFilter)i2));
                }
            }
            tables.OnSwitch = _ => context.OnChangePage();

            content.Child = tables;
            
            backButton = new LabelW("Back", context.Fonts.FontMedium);
            Style.ApplyButton(backButton);
            backButton.OnClick = click =>
            {
                if (click.Button == WEMouseClick.EButton.LEFT)
                {
                    context.PopPage();
                }
            };

            buttonRowUpper.AddSpace(LayoutW.FILL);
            buttonRowUpper.AddChild(filterSelect, 570);

            buttonRowLower.AddChild(backButton, 190);
            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(categorySelect, categories.Length * 190);

            ulong fileId = mapId;
            if (Map.IsOther(mapId) || Map.MapIdToFileId.TryGetValue(mapId, out fileId))
            {
                workshopPageButton = new LabelW("Workshop page", context.Fonts.FontMedium);
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

                titleBar.AddChild(workshopPageButtonLayout, 220f);
            }
            if (Map.IsOrigins(mapId))
            {
                enterMapButton = new LabelW("Enter map", context.Fonts.FontMedium);
                Style.ApplyButton(enterMapButton);
                enterMapButton.OnClick = click =>
                {
                    if (click.Button == WEMouseClick.EButton.LEFT)
                    {
                        Origins.Instance.SelectOrigins(mapId);
                        context.ExitMenu(animation: false);
                    }
                };
                enterMapButtonLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
                enterMapButtonLayout.AddSpace(LayoutW.FILL);
                enterMapButtonLayout.AddChild(enterMapButton, 35);

                titleBar.AddChild(enterMapButtonLayout, 190f);
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
            OFFICIAL, RWS, OLD_RWS, ORIGINS, OTHER,
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

            int colWidth = filter != EFilter.OTHER && filter != EFilter.ORIGINS ? 230 : 280;
            table.AddColumn("Map", LayoutW.FILL, (runs) => new AllMapEntry(context.Fonts.FontMedium, runs.MapId));
            if (filter != EFilter.ORIGINS)
            {
                table.AddColumn("New Lap", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, runs.NewLap));
                table.AddColumn("1 Lap", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, runs.OneLap));
                if (filter != EFilter.OTHER)
                {
                    table.AddColumn("New Lap (Skip)", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, runs.NewLapSkip));
                    table.AddColumn("1 Lap (Skip)", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, runs.OneLapSkip));
                }
            }
            else
            {
                table.AddColumn("Any%", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, runs.AnyPerc));
                table.AddColumn("100%", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, runs.HundredPerc));
            }
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
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, true).Where((infos) => Map.IsOfficial(infos.MapId));
                case EFilter.RWS:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, true).Where((infos) => Map.IsRWS(infos.MapId));
                case EFilter.OLD_RWS:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, true).Where((infos) => Map.IsOldRWS(infos.MapId));
                case EFilter.ORIGINS:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, true).Where((infos) => Map.IsOrigins(infos.MapId));
                case EFilter.OTHER:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, false).Where((infos) => infos.NewLap.Id != -1 || infos.OneLap.Id != -1);
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
            RunsDatabase.Instance.PushRequestNonCuratedOrder(null);
            RunsDatabase.Instance.PushRequestRuns(new GetPlayerPBsRequest(PlayerId), null);
            RunsDatabase.Instance.PushRequestRuns(new GetPlayerPBsNonCuratedRequest(PlayerId), null);
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
            base(context, RunsDatabase.Instance.GetPlayerName(playerId), buttonRowLower: true)
        {
            this.playerId = playerId;

            titleBar.ClearChildren();
            avatar = new ImageW(SteamCache.GetAvatar(playerId));
            statsTitles1 = new LabelW("PBs:\nWRs:", context.Fonts.FontMedium);
            Style.ApplyText(statsTitles1);
            statsTitles1.Align = new Vector2(0f, 0.5f);
            statsValues1 = new LabelW("", context.Fonts.FontMedium);
            Style.ApplyText(statsValues1);
            statsValues1.Align = new Vector2(0f, 0.5f);
            statsTitles2 = new LabelW("Score:\nPerfection:", context.Fonts.FontMedium);
            Style.ApplyText(statsTitles2);
            statsTitles2.Align = new Vector2(0f, 0.5f);
            statsValues2 = new LabelW("", context.Fonts.FontMedium);
            Style.ApplyText(statsValues2);
            statsValues2.Align = new Vector2(0f, 0.5f);
            titleBar.AddChild(avatar, 60f);
            titleBar.AddSpace(10f);
            titleBar.AddChild(title, LayoutW.FILL);

            backButton = new LabelW("Back", context.Fonts.FontMedium);
            Style.ApplyButton(backButton);
            backButton.OnClick = click =>
            {
                RunsDatabase.Instance.CancelAll();
                if (click.Button == WEMouseClick.EButton.LEFT)
                {
                    context.PopPage();
                }
            };

            string[] filters = new string[] { "Official", "RWS", "Old RWS", "Origins", "Other" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbPlayerRunsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                tables.SetTab(i, new LbPlayerRunsTable(context, playerId, (LbPlayerRunsTable.EFilter)i));
            }
            tables.OnSwitch = _ => context.OnChangePage();

            content.Child = tables;

            buttonRowLower.AddChild(backButton, 190f);
            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(filterSelect, 950f);

            steamPageButton = new LabelW("Steam page", context.Fonts.FontMedium);
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

            titleBar.AddChild(steamPageButtonLayout, 190f);
            titleBar.AddSpace(10f);
            titleBar.AddChild(statsTitles1, 65f);
            titleBar.AddChild(statsValues1, 95f);
            titleBar.AddChild(statsTitles2, 130f);
            titleBar.AddChild(statsValues2, 80f);
        }

        public override void Refresh()
        {
            tables.Current.Refresh();

            statsValues1.Text = "";
            statsValues2.Text = "";

            int pbs = 0;
            int pbsNonCurated = 0;
            long perfectTimeSum = 0;
            long timeSum = 0;
            foreach (MapRunInfos infos in RunsDatabase.Instance.GetPlayerPBs(playerId, true))
            {
                for (int t = 0; t < 6; t++)
                {
                    RunInfo info = infos.Get((ECategoryType)t);
                    if (info.Id != -1)
                    {
                        pbs++;
                        perfectTimeSum += RunsDatabase.Instance.GetWR(info.Category).RunTime;
                        timeSum += info.RunTime;
                    }
                }
            }
            foreach (MapRunInfos infos in RunsDatabase.Instance.GetPlayerPBs(playerId, false))
            {
                for (int t = 0; t < 6; t++)
                {
                    RunInfo info = infos.Get((ECategoryType)t);
                    if (info.Id != -1)
                        pbsNonCurated++;
                }
            }
            statsValues1.Text += "" + pbs + (pbsNonCurated > 0 ? "+" + pbsNonCurated : "");

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

            if (perfectTimeSum == 0)
                statsValues2.Text += "-";
            else
                statsValues2.Text += new RoundingMultiplier("0.1").ToStringRounded((float)((double)perfectTimeSum / timeSum * 100.0)) + "%";
        }

        public override void Rerequest()
        {
            tables.Current.Rerequest();
            RunsDatabase.Instance.PushRequestScores(null);
            RunsDatabase.Instance.PushRequestRuns(new GetWRsRequest(0), Refresh);
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
            OFFICIAL, RWS, OLD_RWS, ORIGINS, OTHER,
            COUNT
        }

        public enum ESorting
        {
            POPULARITY, ALPHABET, RECENT_WR, RECENTLY_BUILT,
            COUNT
        }

        private readonly EFilter filter;
        private readonly Func<int> place;

        public ESorting Sorting;

        public LbTopRunsTable(LbMenuContext context, EFilter filter, Func<int> place) :
            base(context)
        {
            this.filter = filter;
            this.place = place;

            int colWidth = (filter != EFilter.OTHER && filter != EFilter.ORIGINS) ? 230 : 380;
            table.AddColumn("Map", LayoutW.FILL, (runs) => new AllMapEntry(context.Fonts.FontMedium, runs.MapId));
            if (filter != EFilter.ORIGINS)
            {
                table.AddColumn("New Lap", colWidth, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium, runs.NewLap, compact: filter == EFilter.OTHER));
                if (filter == EFilter.OTHER)
                    table.AddSpace(40f);
                table.AddColumn("1 Lap", colWidth, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium, runs.OneLap, compact: filter == EFilter.OTHER));
                if (filter == EFilter.OTHER)
                    table.AddSpace(15f);
                if (filter != EFilter.OTHER)
                {
                    table.AddColumn("New Lap (Skip)", colWidth, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium, runs.NewLapSkip));
                    table.AddColumn("1 Lap (Skip)", colWidth, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium, runs.OneLapSkip));
                }
            }
            else
            {
                table.AddColumn("Any%", colWidth, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium, runs.AnyPerc, compact: true));
                table.AddSpace(40f);
                table.AddColumn("100%", colWidth, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium, runs.HundredPerc, compact: true));
                table.AddSpace(15f);
            }
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
                    return RunsDatabase.Instance.GetWRs(place(), true).Where((infos) => Map.IsOfficial(infos.MapId));
                case EFilter.RWS:
                    return RunsDatabase.Instance.GetWRs(place(), true).Where((infos) => Map.IsRWS(infos.MapId));
                case EFilter.OLD_RWS:
                    return RunsDatabase.Instance.GetWRs(place(), true).Where((infos) => Map.IsOldRWS(infos.MapId));
                case EFilter.ORIGINS:
                    return RunsDatabase.Instance.GetWRs(place(), true).Where((infos) => Map.IsOrigins(infos.MapId));
                case EFilter.OTHER:
                    {
                        IEnumerable<MapRunInfos> elems = RunsDatabase.Instance.GetWRs(place(), false);
                        switch (Sorting)
                        {
                            case ESorting.POPULARITY:
                                return elems;
                            case ESorting.ALPHABET:
                                return elems.OrderBy(infos => Map.MapIdToName(infos.MapId));
                            case ESorting.RECENT_WR:
                                return elems.OrderByDescending(infos => Math.Max(infos.NewLap.Id, infos.OneLap.Id));
                            case ESorting.RECENTLY_BUILT:
                                return elems.OrderByDescending(infos => infos.MapId);
                            default:
                                return elems;
                        }
                    }
                default:
                    return null;
            }
        }

        public override float Height(MapRunInfos elem, int i)
        {
            return filter != EFilter.ORIGINS && filter != EFilter.OTHER ? 84f : 40f;
        }

        public override void Refresh()
        {
            table.RowCount = GetElems().Count();
        }

        public override void Rerequest()
        {
            if (filter == EFilter.OTHER)
                RunsDatabase.Instance.PushRequestNonCuratedOrder(null);
            if (filter != EFilter.OTHER)
                RunsDatabase.Instance.PushRequestRuns(new GetWRsRequest(place()), Refresh);
            else
                RunsDatabase.Instance.PushRequestRuns(new GetWRsNonCuratedRequest(place()), Refresh);
            RunsDatabase.Instance.RunRequestRuns((error) => context.Error = error.Message);
        }
    }

    public static class SortingExt
    {
        public static string Label(this LbTopRunsTable.ESorting sorting)
        {
            switch (sorting)
            {
                case LbTopRunsTable.ESorting.POPULARITY:
                    return "Popularity";
                case LbTopRunsTable.ESorting.ALPHABET:
                    return "Alphabet";
                case LbTopRunsTable.ESorting.RECENT_WR:
                    return "Recent WR";
                case LbTopRunsTable.ESorting.RECENTLY_BUILT:
                    return "Recently built";
                default:
                    return "";
            }
        }
    }

    public class LbTopRunsMenuPage : LbMenuPage
    {
        private readonly SelectorButtonW filterSelect;
        private readonly TabbedW<LbTopRunsTable> tables;
        private readonly LbTopRunsTable otherTable;
        private readonly LabelW placeLabel;
        private readonly LabelW placeSelectLabel;
        private readonly LabelW placeDecrButton;
        private readonly LabelW placeIncrButton;
        private readonly LayoutW placeSelectLayoutV;
        private readonly LayoutW placeSelectLayoutH;
        private readonly LabelW sortingButton;
        private readonly LabelW orderByLabel;
        private readonly LayoutW sortingLayout;
        private readonly TransitionW<LayoutW> sortingTransition;

        private int place = 0;

        public LbTopRunsMenuPage(LbMenuContext context) :
            base(context, "Top runs", buttonRowLower: true, showStatus: false)
        {
            string[] filters = new string[] { "Official", "RWS", "Old RWS", "Origins", "Other" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbTopRunsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                LbTopRunsTable next = new LbTopRunsTable(context, (LbTopRunsTable.EFilter)i, () => place);
                tables.SetTab(i, next);
                if ((LbTopRunsTable.EFilter)i == LbTopRunsTable.EFilter.OTHER)
                    otherTable = next;
            }
            tables.OnSwitch = newTab =>
            {
                place = 0;
                UpdatePlaceLabel();
                context.OnChangePage();
                if (newTab == otherTable)
                    sortingTransition.TransitionTo(sortingLayout, 8f, Vector2.Zero);
                else
                    sortingTransition.TransitionTo(null, 8f, Vector2.Zero);
            };
            content.Child = tables;

            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(filterSelect, 950f);

            sortingButton = new LabelW(LbTopRunsTable.ESorting.POPULARITY.Label(), context.Fonts.FontMedium);
            Style.ApplyButton(sortingButton);
            sortingButton.OnClick = (wevent) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    otherTable.Sorting = (LbTopRunsTable.ESorting)((int)otherTable.Sorting + 1);
                    if (otherTable.Sorting == LbTopRunsTable.ESorting.COUNT)
                        otherTable.Sorting = 0;
                    sortingButton.Text = otherTable.Sorting.Label();
                }
            };
            orderByLabel = new LabelW("Order by:", context.Fonts.FontMedium);
            Style.ApplyText(orderByLabel);
            sortingLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            sortingLayout.AddChild(orderByLabel, 25f);
            sortingLayout.AddSpace(10f);
            sortingLayout.AddChild(sortingButton, 35f);
            sortingTransition = new TransitionW<LayoutW>();
            titleBar.AddChild(sortingTransition, 190f);

            placeLabel = new LabelW("Place:", context.Fonts.FontMedium);
            Style.ApplyText(placeLabel);
            placeSelectLabel = new LabelW("1st", context.Fonts.FontMedium);
            Style.ApplyText(placeSelectLabel);
            placeDecrButton = new LabelW("-", context.Fonts.FontMedium);
            Style.ApplyButton(placeDecrButton);
            placeDecrButton.OnClick = (wevent) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    place--;
                    if (place < 0)
                        place = 0;
                    else
                    {
                        UpdatePlaceLabel();
                        context.OnChangePage();
                    }
                }
            };
            placeIncrButton = new LabelW("+", context.Fonts.FontMedium);
            Style.ApplyButton(placeIncrButton);
            placeIncrButton.OnClick = (wevent) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    place++;
                    UpdatePlaceLabel();
                    context.OnChangePage();
                }
            };
            placeSelectLayoutH = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
            placeSelectLayoutH.AddChild(placeDecrButton, 50f);
            placeSelectLayoutH.AddChild(placeSelectLabel, 90f);
            placeSelectLayoutH.AddChild(placeIncrButton, 50f);
            placeSelectLayoutV = new LayoutW(LayoutW.EOrientation.VERTICAL);
            placeSelectLayoutV.AddChild(placeLabel, 25f);
            placeSelectLayoutV.AddSpace(10f);
            placeSelectLayoutV.AddChild(placeSelectLayoutH, 35f);
            titleBar.AddSpace(10f);
            titleBar.AddChild(placeSelectLayoutV, 190f);
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

        private void UpdatePlaceLabel()
        {
            int units = (place + 1) % 10;
            int tens = ((place + 1) / 10) % 10;

            placeSelectLabel.Text = "" + (place + 1);
            if (units == 1 && tens != 1)
                placeSelectLabel.Text += "st";
            else if (units == 2 && tens != 1)
                placeSelectLabel.Text += "nd";
            else if (units == 3 && tens != 1)
                placeSelectLabel.Text += "rd";
            else
                placeSelectLabel.Text += "th";
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
            base(context, mapPageButton: true)
        {
            this.filter = filter;

            table.AddColumn("Map", LayoutW.FILL, (run) => new MapEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Category", 170f, (run) => new CategoryEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Player", 350f, (run) => new PlayerEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Time", 170f, (run) => new TimeEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Age", 150f, (run) => new AgeEntry(context.Fonts.FontMedium, run));
            table.AddColumn("#", 40f, (run) => new PlaceEntry(context.Fonts.FontMedium, run));
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

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbRecentRunsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                tables.SetTab(i, new LbRecentRunsTable(context, (LbRecentRunsTable.EFilter)i));
            }
            tables.OnSwitch = _ => context.OnChangePage();

            content.Child = tables;

            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(filterSelect, 380f);
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
            public PlaceEntry(CachedFont font, PlayerInfoScore player) :
                base("", font)
            {
                Align = new Vector2(0f, 0.5f);
                Text = "" + (player.Place + 1);
                Style.ApplyText(this);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            }
        }

        public class PlayerEntry : LayoutW
        {
            private readonly ImageW image;
            private readonly LabelW label;

            private readonly PlayerInfoScore player;

            public PlayerEntry(CachedFont font, PlayerInfoScore player) :
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
                    label.Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            }

            public override void Draw(IWidget hovered, float scale, float opacity)
            {
                image.Image = SteamCache.GetAvatar(player.PlayerId);
                label.Text = RunsDatabase.Instance.GetPlayerName(player.PlayerId);

                base.Draw(hovered, scale, opacity);
            }
        }

        public class ScoreEntry : LabelW
        {
            public ScoreEntry(CachedFont font, PlayerInfoScore player) :
                base("", font)
            {
                Align = new Vector2(0f, 0.5f);
                Style.ApplyText(this);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    Color = SettingsUI.Instance.HighlightTextColor.Value.Get;

                Text = "" + player.Score;
            }
        }

        public LbTopPlayersScoreTable(LbMenuContext context) :
            base(context)
        {
            table.AddColumn("#", 50f, (player) => new PlaceEntry(context.Fonts.FontMedium, player));
            table.AddColumn("Player", LayoutW.FILL, (player) => new PlayerEntry(context.Fonts.FontMedium, player));
            table.AddColumn("Score", 120f, (player) => new ScoreEntry(context.Fonts.FontMedium, player));

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
            public PlaceEntry(CachedFont font, PlayerInfoWRs player) :
                base("", font)
            {
                Align = new Vector2(0f, 0.5f);
                Text = "" + (player.Place + 1);
                Style.ApplyText(this);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            }
        }

        public class PlayerEntry : LayoutW
        {
            private readonly ImageW image;
            private readonly LabelW label;

            private readonly PlayerInfoWRs player;

            public PlayerEntry(CachedFont font, PlayerInfoWRs player) :
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
                    label.Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            }

            public override void Draw(IWidget hovered, float scale, float opacity)
            {
                image.Image = SteamCache.GetAvatar(player.PlayerId);
                label.Text = RunsDatabase.Instance.GetPlayerName(player.PlayerId);

                base.Draw(hovered, scale, opacity);
            }
        }

        public class WrCountEntry : LabelW
        {
            public WrCountEntry(CachedFont font, PlayerInfoWRs player) :
                base("", font)
            {
                Align = new Vector2(0f, 0.5f);
                Style.ApplyText(this);
                if (player.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    Color = SettingsUI.Instance.HighlightTextColor.Value.Get;

                Text = "" + player.WrCount;
            }
        }

        public LbTopPlayersWRsTable(LbMenuContext context) :
            base(context)
        {
            table.AddColumn("#", 50f, (player) => new PlaceEntry(context.Fonts.FontMedium, player));
            table.AddColumn("Player", LayoutW.FILL, (player) => new PlayerEntry(context.Fonts.FontMedium, player));
            table.AddColumn("WRs", 80f, (player) => new WrCountEntry(context.Fonts.FontMedium, player));

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
            RunsDatabase.Instance.PushRequestRuns(new GetWRsRequest(0), Refresh);
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
        private readonly TabbedW<RequestingMenu> tables;

        public LbTopPlayersMenuPage(LbMenuContext context) :
            base(context, "Top players", buttonRowLower: true, showStatus: false)
        {
            string[] types = new[] { "Score", "WR count" };

            typeSelector = new SelectorButtonW(types, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(typeSelector);

            tables = new TabbedW<RequestingMenu>(typeSelector);
            tables.SetTab((int)EType.SCORE, new LbTopPlayersScoreTable(context));
            tables.SetTab((int)EType.WR_COUNT, new LbTopPlayersWRsTable(context));
            tables.OnSwitch = _ => context.OnChangePage();

            content.Child = tables;

            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(typeSelector, 380f);
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
            rules = new LabelW("", context.Fonts.FontMedium)
            {
                Align = Vector2.Zero,
                Padding = 10f * Vector2.One
            };
            Style.ApplyText(rules);

            scroll = new ScrollW(rules)
            {
                BackgroundVisible = true,
                BackgroundColor = () => SettingsUI.Instance.EntryColor2.Value.Get(),
                ScrollBarColor = () => SettingsUI.Instance.ButtonColor.Value.Get(),
                ScrollBarWidth = SettingsUI.Instance.ScrollBarWidth.Value
            };

            content.Child = scroll;

            layout.AddSpace(10f);
            
            rules.Text =
@"All runs are automatically verified and categorized by Velo itself before being automatically submitted as
you play. In case an invalid run still manages to get submitted either by a bug or in a cheated manner, 
contact a leaderboard moderator and they will be able to remove the run.
Maps in the ""other"" category do not count towards score or WRs.

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

""Any%"" and ""100%"" categories only apply to Origins maps. ""Any%"" just requires you to complete the level, 
whereas ""100%"" requires you to also collect all winged sandals. The ""100%"" category does not exist for 
levels without winged sandals and it will be counted as ""Any%"" instead.

A run is invalid if any of the following apply:
  -An obstacle was broken upon starting the lap
  -Player used an item (5 second cooldown, infinite cooldown for drills, bombs and smileys on certain maps)
  -Player was in a drill state (5 second cooldown, infinite cooldown on certain maps)
  -An item actor was still alive upon starting the lap
  -A ghost blocked a laser (use ""disable ghost laser interaction"")
  -A ghost hit a fall tile (use ""disable ghost fall tile interaction"")
  -Player had boostacoke upon starting the lap (any map except laboratory)
  -Player modified their boostacoke by pressing + or -
  -Time of the run is longer than 30 minutes
  -Physics, camera or TAS settings were changed in Velo (changing camera zoom on Origins is allowed)
  -Framelimit was set below 30 or above 300 in Velo
  -Blindrun simulator was enabled in Velo
  -A savestate was used (1 second cooldown, infinite cooldown when using savestate load halt)
  -A savestate from a version prior 2.2.11 was used
  -A replay was used (1 second cooldown for replays of own runs, 5 second cooldown for other replays)
  -An external program like Cheat Engine was used
  -Map was neither official, nor Origins, nor published Workshop
  -Option SuperSpeedRunners, SpeedRapture or Destructible Environment was enabled
  -Player paused the game (5 second cooldown)
  -Player missed a primary checkpoint
  -Player missed a secondary checkpoint and a ternary checkpoint

Cooldowns last until running out. Violations and cooldowns are reset upon exiting a map or pressing 
""reset lap"". They are carried over by savestates.

The checkpoint system:
In order to differentiate between Skip and non-Skip runs and to further improve validation, Velo makes use 
of a checkpoint system comprised of primary, secondary and ternary checkpoints (not to be confused
with the game's own checkpoint system). These checkpoints are made up of rectangular sections scattered
around each map. In order for a run to be valid, the player needs to hit all primary checkpoints
and either all secondary or all ternary checkpoints. To be categorized as non-Skip, the player needs
to hit all secondary checkpoints.

The score system:
Each PB run grants you somewhere between 1 to 1000 (or 500) points, depending on how close it is to the
WR run. If we denote WR time as 'wr' and your PB time as 'pb', you get M * wr / (wr + 25 * (pb - wr)) points
per run where M = 1000 on all maps except for old RWS and Origins with M = 500.

The new grapple cooldown:
On 09/OCT/2024 the game received an update, lowering the grapple cooldown from 0.25s to 0.20s. Playing
with either of these is allowed and you can see the value used under ""Grapple CD"".";
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

            pageSelector = new SelectorButtonW(pages, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(pageSelector);

            this.pages = new TabbedW<LbMenuPage>(pageSelector);
            this.pages.SetTab((int)EPages.TOP_RUNS, new LbTopRunsMenuPage(context));
            this.pages.SetTab((int)EPages.RECENT_RUNS, new LbRecentMenuPage(context));
            this.pages.SetTab((int)EPages.TOP_PLAYERS, new LbTopPlayersMenuPage(context));
            this.pages.SetTab((int)EPages.RULES, new LbRulesMenuPage(context));
            this.pages.OnSwitch = _ => context.OnChangePage();

            content.Child = this.pages;

            buttonRowLower.AddSpace(LayoutW.FILL);
            buttonRowLower.AddChild(pageSelector, 760f);
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
