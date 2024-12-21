using System;
using System.Collections.Generic;
using CEngine.Graphics.Library;
using CEngine.Graphics.Component;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Steamworks;
using System.Threading.Tasks;

namespace Velo
{
    public interface ILbWidget : IWidget
    {
        void PushRequests();
    }

    public abstract class LbMenuPage : LayoutW, ILbWidget
    {
        protected readonly LbContext context;

        protected LabelW title;
        protected LayoutW titleBar;
        protected HolderW<Widget> content;
        protected readonly LayoutW buttonRowUpper;
        protected readonly LayoutW buttonRowLower;

        public LbMenuPage(LbContext context, string title, bool buttonRowUpper = false, bool buttonRowLower = false, bool buttonRowSpace = true)
            : base(EOrientation.VERTICAL)
        {
            this.context = context;

            content = new HolderW<Widget>();

            if (title != "")
            {
                this.title = new LabelW(title, context.Fonts.FontTitle)
                {
                    Align = new Vector2(0f, 0.5f),
                    Color = SettingsUI.Instance.HeaderTextColor.Value.Get
                };
                titleBar = new LayoutW(EOrientation.HORIZONTAL);
                titleBar.AddChild(this.title, FILL);
                AddChild(titleBar, 80f);
                AddSpace(10f);
            }
            AddChild(content, FILL);
            if (buttonRowSpace && (buttonRowLower || buttonRowUpper))
                AddSpace(10f);
            if (buttonRowUpper)
            {
                this.buttonRowUpper = new LayoutW(EOrientation.HORIZONTAL);
                AddChild(this.buttonRowUpper, 35f);
            }
            if (buttonRowLower)
            {
                this.buttonRowLower = new LayoutW(EOrientation.HORIZONTAL);
                AddChild(this.buttonRowLower, 35f);
            }
        }

        public abstract void PushRequests();
    }

    public abstract class LbTable<T> : LayoutW, ILbWidget, ITableEntryFactory<T> where T : struct
    {
        protected readonly LbContext context;

        protected TableW<T> table;
        
        public LbTable(LbContext context) :
            base(EOrientation.VERTICAL)
        {
            this.context = context;
            table = new TableW<T>(context.Fonts.FontMedium, 40, this);
            this.context = context;
            Style.ApplyTable(table);
            table.AddSpace(10f);

            AddChild(table, FILL);
        }

        public abstract void PushRequests();
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

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            if (run.Id == -1)
                return;

            image.Image = SteamCache.GetAvatar(run.PlayerId);
            label.Text = RunsDatabase.Instance.GetPlayerName(run.PlayerId);

            base.Draw(hovered, parentCropRec, scale, opacity);
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

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
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

            base.Draw(hovered, parentCropRec, scale, opacity);
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

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            if (run.Id == -1)
            {
                Text = "";
            }
            else
            {
                Text = Map.MapIdToName(run.Category.MapId);
            }

            base.Draw(hovered, parentCropRec, scale, opacity);
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

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            Text = Map.MapIdToName(mapId);

            base.Draw(hovered, parentCropRec, scale, opacity);
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
                AddSpace(2);
                AddChild(time, 28);
                AddChild(player, 40);
                AddSpace(4);
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

            public ExpandedRow(Widget original, LbContext context, RunInfo run, bool mapPageButton, Action<int, Playback.EPlaybackType> requestRecording)
            {
                this.run = run;
                this.original = original;

                LabelW generalLabels = new LabelW("", context.Fonts.FontMedium)
                {
                    Align = Vector2.Zero,
                    Text =
                        "Player:\nMap:\nCategory:\nTime:\nPlace:\nWas WR:\nGCD, Fix BG:\nDate:\nID:"
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
                        ((run.PhysicsFlags & RunInfo.FLAG_NEW_GCD) != 0 ? "0.20s" : "0.25s") + ", " +
                        ((run.PhysicsFlags & RunInfo.FLAG_FIX_BOUNCE_GLITCH) != 0 ? "Yes" : "No") + "\n" +
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
                viewProfile.OnLeftClick = () =>
                {
                    context.ChangePage(new LbPlayerMenuPage(context, run.PlayerId));
                };
                mapPage = new LabelW("Map page", context.Fonts.FontMedium);
                Style.ApplyButton(mapPage);
                mapPage.Hoverable = false;
                mapPage.OnLeftClick = () =>
                {
                    context.ChangePage(new LbMapMenuPage(context, run.Category.MapId));
                };

                LayoutW avatarLayout = new LayoutW(EOrientation.VERTICAL);
                avatarLayout.AddSpace(30f);
                avatarLayout.AddChild(avatar, 200f);
                avatarLayout.AddSpace(10f);
                avatarLayout.AddChild(viewProfile, 40f);
                avatarLayout.AddSpace(FILL);

                LayoutW infos = new LayoutW(EOrientation.HORIZONTAL);
                infos.AddSpace(40f);
                infos.AddChild(generalLabels, 145f);
                infos.AddChild(generalValues, 280f);
                infos.AddSpace(20f);
                infos.AddChild(statsLabels, 195f);
                infos.AddChild(statsValues, 150f);
                infos.AddSpace(FILL);
                infos.AddChild(avatarLayout, 200f);
                infos.AddSpace(40f);

                MapEvent mapEvent = RunsDatabase.Instance.GetEvent(run.Category.MapId);
                
                setGhost = new LabelW("Set ghost", context.Fonts.FontMedium);
                Style.ApplyButton(setGhost);
                setGhost.Hoverable = false;
                setGhost.OnLeftClick = () =>
                {
                    requestRecording(run.Id, Playback.EPlaybackType.SET_GHOST);
                };
                viewReplay = new LabelW("Watch replay", context.Fonts.FontMedium);
                Style.ApplyButton(viewReplay);
                viewReplay.Hoverable = false;
                viewReplay.OnLeftClick = () =>
                {
                    requestRecording(run.Id, Playback.EPlaybackType.VIEW_REPLAY);
                };
                verify = new LabelW("Verify", context.Fonts.FontMedium);
                Style.ApplyButton(verify);
                verify.Hoverable = false;
                verify.OnLeftClick = () =>
                {
                    requestRecording(run.Id, Playback.EPlaybackType.VERIFY);
                };
                
                LayoutW buttons = new LayoutW(EOrientation.HORIZONTAL);
                //if (mapEvent.CurrentlyNotRunning() || run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                {
                    buttons.AddSpace(40f);
                    buttons.AddChild(setGhost, 240f);
                    buttons.AddSpace(10f);
                    buttons.AddChild(viewReplay, 240f);
                    buttons.AddSpace(10f);
                    buttons.AddChild(verify, 240f);
                }
                buttons.AddSpace(FILL);
                if (mapPageButton)
                {
                    buttons.AddChild(mapPage, 200f);
                    buttons.AddSpace(40f);
                }

                layout = new LayoutW(EOrientation.VERTICAL);
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

                    LayoutW commentsLayout = new LayoutW(EOrientation.HORIZONTAL);
                    commentsLayout.AddSpace(20f);
                    commentsLayout.AddChild(scroll, FILL);
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
                        if (setGhost.OnClick != null)
                            setGhost.Hoverable = true;
                        if (viewReplay.OnClick != null)
                            viewReplay.Hoverable = true;
                        if (verify.OnClick != null)
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

        protected int Count = 100;
        private int requestCount = 0;
        private bool initialRequest = true;

        private int expandedId = -1;
        private readonly Dictionary<int, ExpandedRow> expanded = new Dictionary<int, ExpandedRow>();

        private int requestedId = -1;
        private Playback.EPlaybackType requestedPlaybackType = default;

        public LbRunsTable(LbContext context, bool mapPageButton) :
            base(context)
        {
            table.OnLeftClickRow = (row, elem, i) =>
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

                table.RefreshEntry(i);
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
            if (type == Playback.EPlaybackType.SET_GHOST)
                Ghosts.Instance.GetOrSpawn(!OfflineGameMods.Instance.EnableMultiGhost.Value ? 0 : OfflineGameMods.Instance.GhostPlaybackCount(), OfflineGameMods.Instance.GhostDifferentColors.Value);
           
            RunsDatabase.Instance.RequestRecordingCached(id, (recording) =>
                {
                    Task.Run(() =>
                    {
                        if (type == Playback.EPlaybackType.SET_GHOST)
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

        private bool NeedRequestMore()
        {
            return !initialRequest && table.ReachedEnd && Count == requestCount && Count <= GetElems().Count();
        }

        public override void PushRequests()
        {
            if (NeedRequestMore())
            {
                requestCount += 100;
                PushRequests(Count, 100, () =>
                {
                    Count += 100;
                });
            }
            else
            {
                if (initialRequest)
                    requestCount = 100;
                initialRequest = false;
                PushRequests(Math.Max(0, Count - 100), 100, null);
            }
        }

        public override void Reset()
        {
            base.Reset();
            Count = 100;
            requestCount = 0;
            initialRequest = true;
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

        public override void UpdateBounds(Bounds parentBounds)
        {
            base.UpdateBounds(parentBounds);
            
            if (NeedRequestMore())
            {
                context.Request();
            }
        }

        protected abstract void PushRequests(int start, int count, Action onSuccess);
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

        public LbMapRunsTable(LbContext context, Category category, EFilter filter) :
            base(context, mapPageButton: false)
        {
            this.category = category;
            this.filter = filter;

            table.AddColumn("#", 50, (run) => new PlaceEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Player", FILL, (run) => new PlayerEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Time", 200, (run) => new TimeEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Age", 200, (run) => new AgeEntry(context.Fonts.FontMedium, run));
        }

        public override IEnumerable<RunInfo> GetElems()
        {
            switch (filter)
            {
                case EFilter.PBS_ONLY:
                    return RunsDatabase.Instance.GetPBsForCategory(category).Take(Count);
                case EFilter.WR_HISTORY:
                    return RunsDatabase.Instance.GetWRHistoryForCategory(category).Take(Count);
                case EFilter.ALL:
                    return RunsDatabase.Instance.GetAllForCategory(category).Take(Count);
                default:
                    return null;
            }
        }

        protected override void PushRequests(int start, int count, Action onSuccess)
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
        }
    }

    public class LbMapMenuPage : LbMenuPage
    {
        private readonly ulong mapId;
        private readonly SelectorButtonW categorySelect;
        private readonly SelectorButtonW filterSelect;
        private readonly DualTabbedW<LbMapRunsTable> tables;
        private readonly LabelW backButton;
        private readonly LabelW eventLabel;
        private readonly LayoutW eventLabelLayout;
        private readonly LabelW showEventButton;
        private readonly LayoutW showEventButtonLayout;
        private readonly LabelW workshopPageButton;
        private readonly LayoutW workshopPageButtonLayout;
        private readonly LabelW enterMapButton;
        private readonly LayoutW enterMapButtonLayout;

        private bool eventsTabShown = true;
        private int showEventButtonId = 0;

        public LbMapMenuPage(LbContext context, ulong mapId) :
            base(context, Map.MapIdToName(mapId), buttonRowUpper: true, buttonRowLower: true)
        {
            this.mapId = mapId;

            List<ECategoryType> categories = new List<ECategoryType>();

            if (!Map.IsOrigins(mapId))
            {
                categories.Add(ECategoryType.NEW_LAP);
                categories.Add(ECategoryType.ONE_LAP);
                if (Map.HasSkip(mapId))
                {
                    categories.Add(ECategoryType.NEW_LAP_SKIPS);
                    categories.Add(ECategoryType.ONE_LAP_SKIPS);
                }
            }
            else
            {
                categories.Add(ECategoryType.ANY_PERC);
                if (Map.Has100Perc(mapId))
                    categories.Add(ECategoryType.HUNDRED_PERC);
            }
            MapEvent mapEvent = RunsDatabase.Instance.GetEvent(mapId);
            categories.Add(ECategoryType.EVENT);

            string[] categoryLabels = categories.Select(c => c.Label()).ToArray();
            categorySelect = new SelectorButtonW(categoryLabels, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(categorySelect);
           
            string[] filters = new string[] { "PBs only", "WR history", "All" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(filterSelect);

            tables = new DualTabbedW<LbMapRunsTable>(categorySelect, filterSelect);
            for (int i1 = 0; i1 < categories.Count; i1++)
            {
                for (int i2 = 0; i2 < filters.Length; i2++)
                {
                    tables.SetTab(i1, i2, new LbMapRunsTable(context, new Category { MapId = mapId, TypeId = (ulong)categories[i1] }, (LbMapRunsTable.EFilter)i2));
                }
            }
            tables.OnSwitch = _ => context.Request();

            if (mapEvent.CurrentlyNotRunning())
            {
                categorySelect.ShownCount--;
                eventsTabShown = false;
            }

            content.Child = tables;

            backButton = new LabelW("Back", context.Fonts.FontMedium);
            Style.ApplyButton(backButton);
            backButton.OnLeftClick = context.ChangeBack;

            buttonRowUpper.AddSpace(FILL);
            buttonRowUpper.AddChild(filterSelect, 570f);

            buttonRowLower.AddChild(backButton, 190f);
            buttonRowLower.AddSpace(FILL);
            buttonRowLower.AddChild(categorySelect, categorySelect.ShownCount * 190);

            eventLabel = new LabelW("", context.Fonts.FontMedium);
            Style.ApplyText(eventLabel);
            eventLabel.Align = new Vector2(1f, 0.5f);

            showEventButton = new LabelW("View event", context.Fonts.FontMedium);
            Style.ApplyButton(showEventButton);
            showEventButton.OnLeftClick = () =>
            {
                ShowEventsTab();
                categorySelect.Selected = categorySelect.Count - 1;
                filterSelect.Selected = 0;
            };

            eventLabelLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            eventLabelLayout.AddSpace(LayoutW.FILL);
            eventLabelLayout.AddChild(eventLabel, 50f);

            showEventButtonLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            showEventButtonLayout.AddSpace(LayoutW.FILL);
            showEventButtonLayout.AddChild(showEventButton, 35f);

            titleBar.AddChild(eventLabelLayout, 0f);
            titleBar.AddSpace(10f);

            showEventButtonId = titleBar.AddChild(showEventButtonLayout, 170f);
            titleBar.AddSpace(10f);

            if (mapEvent.From == 0)
            {
                titleBar.SetSize(showEventButtonId, 0);
                titleBar.SetSize(showEventButtonId + 1, 0);
                showEventButton.Visible = false;
            }

            ulong fileId = mapId;
            if (Map.IsOther(mapId) || Map.MapIdToFileId.TryGetValue(mapId, out fileId))
            {
                workshopPageButton = new LabelW("Workshop page", context.Fonts.FontMedium);
                Style.ApplyButton(workshopPageButton);
                workshopPageButton.OnLeftClick = () =>
                {
                    SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/sharedfiles/filedetails/?id=" + fileId);
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
                enterMapButton.OnLeftClick = () =>
                {
                    Origins.Instance.SelectOrigins(mapId);
                    context.ExitMenu(animation: false);
                };
                enterMapButtonLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
                enterMapButtonLayout.AddSpace(LayoutW.FILL);
                enterMapButtonLayout.AddChild(enterMapButton, 35);

                titleBar.AddChild(enterMapButtonLayout, 190f);
            }
        }

        public void ShowEventsTab()
        {
            if (eventsTabShown)
                return;

            categorySelect.ShownCount++;
            eventsTabShown = true;
            buttonRowLower.SetSize(2, categorySelect.ShownCount * 190f);
            titleBar.SetSize(showEventButtonId, 170f);
            titleBar.SetSize(showEventButtonId + 1, 10f);
            showEventButton.Visible = true;
        }

        public override void Refresh()
        {
            base.Refresh();

            MapEvent mapEvent = RunsDatabase.Instance.GetEvent(mapId);
            if (!mapEvent.CurrentlyNotRunning())
                ShowEventsTab();
        }

        public override void PushRequests()
        {
            tables.Current.PushRequests();
        }

        public override void UpdateBounds(Bounds parentBounds)
        {
            MapEvent mapEvent = RunsDatabase.Instance.GetEvent(mapId);
            if (mapEvent.From == 0)
            {
                eventLabel.Text = "";
                titleBar.SetSize(eventLabelLayout, 0f);
            }
            else
            {
                long remainingStart = mapEvent.RemainingStart();
                long remainingEnd = mapEvent.RemainingEnd();
                if (remainingStart > 0)
                    eventLabel.Text = "Event begins in " + Util.FormatLongTime(remainingStart) + ".";
                else if (remainingEnd > 0)
                    eventLabel.Text = "Event ends in " + Util.FormatLongTime(remainingEnd) + ".";
                else
                {
                    eventLabel.Text = "Event ended " + Util.FormatLongTime(-remainingEnd) + " ago.\n";
                    if (mapEvent.Winner == 0)
                        eventLabel.Text += "No winner has been declared yet.";
                    else
                        eventLabel.Text += "The winner is " + SteamCache.GetPlayerName(mapEvent.Winner) + "!";
                }
                titleBar.SetSize(eventLabelLayout, eventLabel.MeasureTextSize.X + 10f);
            }

            base.UpdateBounds(parentBounds);
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

        public LbPlayerRunsTable(LbContext context, ulong playerId, EFilter filter) :
            base(context)
        {
            this.playerId = playerId;
            this.filter = filter;

            int colWidth = filter != EFilter.OTHER && filter != EFilter.ORIGINS ? 230 : 280;
            table.AddColumn("Map", FILL, (runs) => new AllMapEntry(context.Fonts.FontMedium, runs.MapId));
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
            table.OnLeftClickRow = (row, runs, i) =>
            {
                context.ChangePage(new LbMapMenuPage(context, runs.MapId));
            };
        }

        public override IEnumerable<MapRunInfos> GetElems()
        {
            switch (filter)
            {
                case EFilter.OFFICIAL:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, curated: true, popularity: false).Where((infos) => Map.IsOfficial(infos.MapId));
                case EFilter.RWS:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, curated: true, popularity: false).Where((infos) => Map.IsRWS(infos.MapId));
                case EFilter.OLD_RWS:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, curated: true, popularity: false).Where((infos) => Map.IsOldRWS(infos.MapId));
                case EFilter.ORIGINS:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, curated: true, popularity: false).Where((infos) => Map.IsOrigins(infos.MapId));
                case EFilter.OTHER:
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, curated: false, popularity: true).Where((infos) => infos.NewLap.Id != -1 || infos.OneLap.Id != -1);
                default:
                    return null;
            }
        }

        public override float Height(MapRunInfos elem, int i)
        {
            return 40f;
        }

        public override void PushRequests()
        {
            RunsDatabase.Instance.PushRequestPopularityOrder(null, curated: false);
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

        public LbPlayerMenuPage(LbContext context, ulong playerId) :
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
            backButton.OnLeftClick = context.ChangeBack;

            string[] filters = new string[] { "Official", "RWS", "Old RWS", "Origins", "Other" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbPlayerRunsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                tables.SetTab(i, new LbPlayerRunsTable(context, playerId, (LbPlayerRunsTable.EFilter)i));
            }
            tables.OnSwitch = _ => context.Request();

            content.Child = tables;

            buttonRowLower.AddChild(backButton, 190f);
            buttonRowLower.AddSpace(FILL);
            buttonRowLower.AddChild(filterSelect, 950f);

            steamPageButton = new LabelW("Steam page", context.Fonts.FontMedium);
            Style.ApplyButton(steamPageButton);
            steamPageButton.OnLeftClick = () =>
            {
                SteamFriends.ActivateGameOverlayToUser("steamid", new CSteamID(playerId));
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

        public override void PushRequests()
        {
            tables.Current.PushRequests();

            RunsDatabase.Instance.PushRequestScores(null);
            RunsDatabase.Instance.PushRequestRuns(new GetWRsRequest(0), Refresh);
        }

        public override void Refresh()
        {
            base.Refresh();

            statsValues1.Text = "";
            statsValues2.Text = "";

            int pbs = 0;
            int pbsNonCurated = 0;
            long perfectTimeSum = 0;
            long timeSum = 0;
            foreach (MapRunInfos infos in RunsDatabase.Instance.GetPlayerPBs(playerId, curated: true, popularity: false))
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
            foreach (MapRunInfos infos in RunsDatabase.Instance.GetPlayerPBs(playerId, curated: false, popularity: true))
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
                statsValues2.Text += new RoundingMultiplier("0.01").ToStringRounded((float)((double)perfectTimeSum / timeSum * 100.0)) + "%";

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
            POPULARITY, ALPHABET, RECENT_WR, RECENTLY_BUILT, CHRONOLOGIC,
            COUNT
        }

        public readonly EFilter Filter;
        private readonly Func<int> place;

        public ESorting Sorting;

        public LbTopRunsTable(LbContext context, EFilter filter, Func<int> place) :
            base(context)
        {
            Filter = filter;
            this.place = place;

            int colWidth = (filter != EFilter.OTHER && filter != EFilter.ORIGINS) ? 230 : 380;
            table.AddColumn("Map", FILL, (runs) => new AllMapEntry(context.Fonts.FontMedium, runs.MapId));
            if (filter != EFilter.ORIGINS)
            {
                table.AddColumn("New Lap", colWidth, runs => new PlayerTimeEntry(context.Fonts.FontMedium, runs.NewLap, compact: filter == EFilter.OTHER));
                if (filter == EFilter.OTHER)
                    table.AddSpace(40f);
                table.AddColumn("1 Lap", colWidth, runs => new PlayerTimeEntry(context.Fonts.FontMedium, runs.OneLap, compact: filter == EFilter.OTHER));
                if (filter == EFilter.OTHER)
                    table.AddSpace(15f);
                if (filter != EFilter.OTHER)
                {
                    table.AddColumn("New Lap (Skip)", colWidth, runs => new PlayerTimeEntry(context.Fonts.FontMedium, runs.NewLapSkip));
                    table.AddColumn("1 Lap (Skip)", colWidth, runs => new PlayerTimeEntry(context.Fonts.FontMedium, runs.OneLapSkip));
                }
            }
            else
            {
                table.AddColumn("Any%", colWidth, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium, runs.AnyPerc, compact: true));
                table.AddSpace(40f);
                table.AddColumn("100%", colWidth, (runs) => new PlayerTimeEntry(context.Fonts.FontMedium, runs.HundredPerc, compact: true));
                table.AddSpace(15f);
            }
            table.OnLeftClickRow = (row, runs, i) =>
                {
                    context.ChangePage(new LbMapMenuPage(context, runs.MapId));
                };
        }

        public override IEnumerable<MapRunInfos> GetElems()
        {
            IEnumerable<MapRunInfos> elems;
            switch (Filter)
            {
                case EFilter.OFFICIAL:
                    elems = RunsDatabase.Instance.GetWRs(place(), curated: true, popularity: Sorting == ESorting.POPULARITY).Where((infos) => Map.IsOfficial(infos.MapId));
                    break;
                case EFilter.RWS:
                    elems = RunsDatabase.Instance.GetWRs(place(), curated: true, popularity: Sorting == ESorting.POPULARITY).Where((infos) => Map.IsRWS(infos.MapId));
                    break;
                case EFilter.OLD_RWS:
                    elems = RunsDatabase.Instance.GetWRs(place(), curated: true, popularity: Sorting == ESorting.POPULARITY).Where((infos) => Map.IsOldRWS(infos.MapId));
                    break;
                case EFilter.ORIGINS:
                    elems = RunsDatabase.Instance.GetWRs(place(), curated: true, popularity: Sorting == ESorting.POPULARITY).Where((infos) => Map.IsOrigins(infos.MapId));
                    break;
                case EFilter.OTHER:
                    elems = RunsDatabase.Instance.GetWRs(place(), curated: false, popularity: true);
                    break;
                default:
                    return null;
            }
            switch (Sorting)
            {
                case ESorting.POPULARITY:
                    return elems;
                case ESorting.ALPHABET:
                    return elems.OrderBy(infos => Map.MapIdToName(infos.MapId));
                case ESorting.RECENT_WR:
                    return elems.OrderByDescending(infos => Enumerable.Range(0, 6).Select(i => infos.Get((ECategoryType)i).Id).Max());
                case ESorting.RECENTLY_BUILT:
                    return elems.OrderByDescending(infos => infos.MapId);
                case ESorting.CHRONOLOGIC:
                    return elems;
                default:
                    return elems;
            }
        }

        public override float Height(MapRunInfos elem, int i)
        {
            return Filter != EFilter.ORIGINS && Filter != EFilter.OTHER ? 74f : 40f;
        }

        public override void PushRequests()
        {
            RunsDatabase.Instance.PushRequestPopularityOrder(null, curated: Filter != EFilter.OTHER);
            if (Filter != EFilter.OTHER)
                RunsDatabase.Instance.PushRequestRuns(new GetWRsRequest(place()), Refresh);
            else
                RunsDatabase.Instance.PushRequestRuns(new GetWRsNonCuratedRequest(place()), Refresh);
        }
    }

    public static class FilterExt
    {
        public static LbTopRunsTable.ESorting DefaultSorting(this LbTopRunsTable.EFilter filter)
        {
            switch (filter)
            {
                case LbTopRunsTable.EFilter.OFFICIAL:
                case LbTopRunsTable.EFilter.ORIGINS:
                    return LbTopRunsTable.ESorting.CHRONOLOGIC;
                case LbTopRunsTable.EFilter.RWS:
                case LbTopRunsTable.EFilter.OLD_RWS:
                    return LbTopRunsTable.ESorting.ALPHABET;
                case LbTopRunsTable.EFilter.OTHER:
                    return LbTopRunsTable.ESorting.POPULARITY;
                default:
                    return default;
            }
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
                case LbTopRunsTable.ESorting.CHRONOLOGIC:
                    return "Chronologic";
                default:
                    return "";
            }
        }

        public static bool ValidFor(this LbTopRunsTable.ESorting sorting, LbTopRunsTable.EFilter filter)
        {
            switch (filter)
            {
                case LbTopRunsTable.EFilter.OFFICIAL:
                case LbTopRunsTable.EFilter.ORIGINS:
                    return sorting != LbTopRunsTable.ESorting.RECENTLY_BUILT;
                case LbTopRunsTable.EFilter.RWS:
                case LbTopRunsTable.EFilter.OLD_RWS:
                    return sorting != LbTopRunsTable.ESorting.RECENTLY_BUILT && sorting != LbTopRunsTable.ESorting.CHRONOLOGIC;
                case LbTopRunsTable.EFilter.OTHER:
                    return sorting != LbTopRunsTable.ESorting.CHRONOLOGIC;
                default:
                    return true;
            }
        }
    }

    public class LbTopRunsMenuPage : LbMenuPage
    {
        private readonly SelectorButtonW filterSelect;
        private readonly TabbedW<LbTopRunsTable> tables;
        private readonly LabelW placeLabel;
        private readonly LabelW placeSelectLabel;
        private readonly LabelW placeDecrButton;
        private readonly LabelW placeIncrButton;
        private readonly LayoutW placeSelectLayoutV;
        private readonly LayoutW placeSelectLayoutH;
        private readonly LabelW orderByLabel;
        private readonly LabelW sortingButton;
        private readonly LayoutW sortingLayout;

        private int place = 0;

        public LbTopRunsMenuPage(LbContext context) :
            base(context, "Top runs", buttonRowLower: true)
        {

            string[] filters = new string[] { "Official", "RWS", "Old RWS", "Origins", "Other" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbTopRunsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                LbTopRunsTable next = new LbTopRunsTable(context, (LbTopRunsTable.EFilter)i, () => place);
                next.Sorting = next.Filter.DefaultSorting();
                tables.SetTab(i, next);
            }
            tables.OnSwitch = newTab =>
            {
                place = 0;
                UpdatePlaceLabel();
                context.Request();
                newTab.Sorting = newTab.Filter.DefaultSorting();
                sortingButton.Text = newTab.Sorting.Label();
            };
            content.Child = tables;

            buttonRowLower.AddSpace(FILL);
            buttonRowLower.AddChild(filterSelect, 950f);

            orderByLabel = new LabelW("Order by:", context.Fonts.FontMedium);
            Style.ApplyText(orderByLabel);
            sortingButton = new LabelW(tables.Current.Sorting.Label(), context.Fonts.FontMedium);
            Style.ApplyButton(sortingButton);
            sortingButton.OnLeftClick = () =>
            {
                LbTopRunsTable.ESorting newSorting = tables.Current.Sorting;

                do
                {
                    newSorting = (LbTopRunsTable.ESorting)((int)newSorting + 1);
                    if (newSorting == LbTopRunsTable.ESorting.COUNT)
                        newSorting = 0;
                }
                while (!newSorting.ValidFor(tables.Current.Filter));

                tables.Current.Sorting = newSorting;
                sortingButton.Text = newSorting.Label();
            };
            sortingLayout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            sortingLayout.AddChild(orderByLabel, 25f);
            sortingLayout.AddSpace(10f);
            sortingLayout.AddChild(sortingButton, 35f);
            titleBar.AddChild(sortingLayout, 190f);

            placeLabel = new LabelW("Place:", context.Fonts.FontMedium);
            Style.ApplyText(placeLabel);
            placeSelectLabel = new LabelW("1st", context.Fonts.FontMedium);
            Style.ApplyText(placeSelectLabel);
            placeDecrButton = new LabelW("-", context.Fonts.FontMedium);
            Style.ApplyButton(placeDecrButton);
            placeDecrButton.OnLeftClick = () =>
            {
                place--;
                if (place < 0)
                    place = 0;
                else
                {
                    UpdatePlaceLabel();
                    context.Request();
                }
            };
            placeIncrButton = new LabelW("+", context.Fonts.FontMedium);
            Style.ApplyButton(placeIncrButton);
            placeIncrButton.OnLeftClick = () =>
            {
                place++;
                UpdatePlaceLabel();
                context.Request();
            };
            placeSelectLayoutH = new LayoutW(EOrientation.HORIZONTAL);
            placeSelectLayoutH.AddChild(placeDecrButton, 50f);
            placeSelectLayoutH.AddChild(placeSelectLabel, 90f);
            placeSelectLayoutH.AddChild(placeIncrButton, 50f);
            placeSelectLayoutV = new LayoutW(EOrientation.VERTICAL);
            placeSelectLayoutV.AddChild(placeLabel, 25f);
            placeSelectLayoutV.AddSpace(10f);
            placeSelectLayoutV.AddChild(placeSelectLayoutH, 35f);
            titleBar.AddSpace(10f);
            titleBar.AddChild(placeSelectLayoutV, 190f);
        }

        public override void PushRequests()
        {
            tables.Current.PushRequests();
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

        public LbRecentRunsTable(LbContext context, EFilter filter) :
            base(context, mapPageButton: true)
        {
            this.filter = filter;

            table.AddColumn("Map", FILL, (run) => new MapEntry(context.Fonts.FontMedium, run));
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
                    return RunsDatabase.Instance.GetRecent().Take(Count);
                case EFilter.WRS_ONLY:
                    return RunsDatabase.Instance.GetRecentWRs().Take(Count);
                default:
                    return null;
            }
        }

        protected override void PushRequests(int start, int count, Action onSuccess)
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
        }
    }

    public class LbRecentMenuPage : LbMenuPage
    {
        private readonly SelectorButtonW filterSelect;
        private readonly TabbedW<LbRecentRunsTable> tables;

        public LbRecentMenuPage(LbContext context) :
            base(context, "Recent runs", buttonRowLower: true)
        {
            string[] filters = new string[] { "All", "WRs only" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbRecentRunsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                tables.SetTab(i, new LbRecentRunsTable(context, (LbRecentRunsTable.EFilter)i));
            }
            tables.OnSwitch = _ => context.Request();

            content.Child = tables;

            buttonRowLower.AddSpace(FILL);
            buttonRowLower.AddChild(filterSelect, 380f);
        }

        public override void PushRequests()
        {
            tables.Current.PushRequests();
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

            public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
            {
                image.Image = SteamCache.GetAvatar(player.PlayerId);
                label.Text = RunsDatabase.Instance.GetPlayerName(player.PlayerId);

                base.Draw(hovered, parentCropRec, scale, opacity);
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

        public LbTopPlayersScoreTable(LbContext context) :
            base(context)
        {
            table.AddColumn("#", 50f, (player) => new PlaceEntry(context.Fonts.FontMedium, player));
            table.AddColumn("Player", FILL, (player) => new PlayerEntry(context.Fonts.FontMedium, player));
            table.AddColumn("Score", 120f, (player) => new ScoreEntry(context.Fonts.FontMedium, player));

            table.OnLeftClickRow = (row, player, i) =>
            {
                context.ChangePage(new LbPlayerMenuPage(context, player.PlayerId));
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

        public override void PushRequests()
        {
            RunsDatabase.Instance.PushRequestScores(Refresh);
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

            public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
            {
                image.Image = SteamCache.GetAvatar(player.PlayerId);
                label.Text = RunsDatabase.Instance.GetPlayerName(player.PlayerId);

                base.Draw(hovered, parentCropRec, scale, opacity);
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

        public LbTopPlayersWRsTable(LbContext context) :
            base(context)
        {
            table.AddColumn("#", 50f, (player) => new PlaceEntry(context.Fonts.FontMedium, player));
            table.AddColumn("Player", FILL, (player) => new PlayerEntry(context.Fonts.FontMedium, player));
            table.AddColumn("WRs", 80f, (player) => new WrCountEntry(context.Fonts.FontMedium, player));

            table.OnLeftClickRow = (row, player, i) =>
            {
                context.ChangePage(new LbPlayerMenuPage(context, player.PlayerId));
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

        public override void PushRequests()
        {
            RunsDatabase.Instance.PushRequestRuns(new GetWRsRequest(0), Refresh);
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
        private readonly TabbedW<ILbWidget> tables;

        public LbTopPlayersMenuPage(LbContext context) :
            base(context, "Top players", buttonRowLower: true)
        {
            string[] types = new[] { "Score", "WR count" };

            typeSelector = new SelectorButtonW(types, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(typeSelector);

            tables = new TabbedW<ILbWidget>(typeSelector);
            tables.SetTab((int)EType.SCORE, new LbTopPlayersScoreTable(context));
            tables.SetTab((int)EType.WR_COUNT, new LbTopPlayersWRsTable(context));
            tables.OnSwitch = _ => context.Request();

            content.Child = tables;

            buttonRowLower.AddSpace(FILL);
            buttonRowLower.AddChild(typeSelector, 380f);
        }

        public override void PushRequests()
        {
            tables.Current.PushRequests();
        }
    }

    public class LbRulesMenuPage : LbMenuPage
    {
        protected LabelW rules;
        protected ScrollW scroll;

        public LbRulesMenuPage(LbContext context) :
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

            AddSpace(10f);
            
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
  -The game's window was out of focus and there was a lag frame of more than 150ms
  -The game's window has been dragged or resized
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
with either of these is allowed and you can see the value used for a run under ""GCD"".

The flat slope bounce glitch fix:
On 12/DEC/2024 the game received an update, fixing the flat slope bounce glitch. Flat slope in particular
is a glitch where 25 units behind the peak of a slope there is a 1 unit wide spot on the ground that has
slope properties. This fix removes this spot when going up the slope, making it impossible to get bounced
up by it, while still allowing you to land on it from above. Playing with or without this fix is allowed and
you can see whether it was applied for a run under ""Fix BG"".";
        }

        public override void PushRequests()
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

        public LbMainMenuPage(LbContext context) :
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
            this.pages.OnSwitch = _ => context.Request();

            content.Child = this.pages;

            buttonRowLower.AddSpace(FILL);
            buttonRowLower.AddChild(pageSelector, 760f);
        }

        public override void PushRequests()
        {
            pages.Current.PushRequests();
        }
    }

    public class PopularWindow : LayoutW, ILbWidget, IListEntryFactory<ulong>
    {
        public class PopularMapEntry : LayoutW
        {
            private readonly MapEntry mapName;
            private readonly ImageW image;
            private readonly TimeEntry time;

            private readonly RunInfo wr;

            public PopularMapEntry(CachedFont font, RunInfo wr) :
                base(EOrientation.HORIZONTAL)
            {
                this.wr = wr;

                mapName = new MapEntry(font, wr);
                image = new ImageW(null);
                time = new TimeEntry(font, wr);

                AddSpace(3f);
                AddChild(mapName, FILL);
                AddSpace(3f);
                AddChild(image, 25f);
                AddSpace(3f);
                AddChild(time, 100f);
            }

            public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
            {
                image.Image = SteamCache.GetAvatar(wr.PlayerId);

                base.Draw(hovered, parentCropRec, scale, opacity);
            }
        }

        protected readonly LbContext context;

        private readonly LayoutW headerLayout;
        private readonly LabelW title;
        private readonly LabelW compactButton;
        private readonly ListW<ulong> maps;
        private readonly ScrollW scroll;
        private readonly LayoutW layout;
        private readonly MoveW move;

        public PopularWindow(LbContext context) :
            base(EOrientation.VERTICAL)
        {
            this.context = context;
            title = new LabelW("Popular this week", context.Fonts.FontMedium);
            Style.ApplyText(title);
            title.Color = SettingsUI.Instance.HeaderTextColor.Value.Get;
            title.Align = new Vector2(0f, 0f);
            title.Offset = new Vector2(0f, -3f);
            compactButton = new LabelW("", context.Fonts.FontSmall);
            Style.ApplyButton(compactButton);
            compactButton.OnLeftClick = () =>
            {
                SettingsUI.Instance.PopularThisWeekCompacted.Value = !SettingsUI.Instance.PopularThisWeekCompacted.Value;
                Compact(SettingsUI.Instance.PopularThisWeekCompacted.Value, 2f);
            };
            headerLayout = new LayoutW(EOrientation.HORIZONTAL);
            headerLayout.AddChild(title, FILL);
            headerLayout.AddChild(compactButton, 40f);

            maps = new ListW<ulong>(this);
            Style.ApplyList(maps);
            
            scroll = new ScrollW(maps);

            layout = new LayoutW(EOrientation.VERTICAL);
            layout.AddChild(headerLayout, 25f);
            layout.AddSpace(8f);
            layout.AddChild(scroll, FILL);

            move = new MoveW(layout);
            AddChild(move, FILL);

            Compact(SettingsUI.Instance.PopularThisWeekCompacted.Value, 10000f);
        }

        public void Compact(bool compact, float speed)
        {
            if (!compact)
            {
                move.MoveTo(speed, new Vector2(0f, 383f));
                compactButton.Text = "∧";
            }
            else
            {
                move.MoveTo(speed, Vector2.Zero);
                compactButton.Text = "∨";
            }
        }

        public Widget Create(ulong elem, int i)
        {
            RunInfo info = RunsDatabase.Instance.GetWR(new Category { MapId = elem, TypeId = (ulong)ECategoryType.NEW_LAP });
            if (info.Id == -1)
                info = RunsDatabase.Instance.GetWR(new Category { MapId = elem, TypeId = (ulong)ECategoryType.ONE_LAP });
            return new PopularMapEntry(context.Fonts.FontSmall, info)
            {
                OnLeftClick = () =>
                {
                    bool wasMapMenuPage = context.Page.Child is LbMapMenuPage;
                    context.ChangePage(new LbMapMenuPage(context, elem));
                    if (wasMapMenuPage)
                        context.Page.PopLast();
                }
            };
        }
        
        public IEnumerable<ulong> GetElems()
        {
            return RunsDatabase.Instance.GetPopularThisWeek().Reverse();
        }

        public float Height(ulong elem, int i)
        {
            return 25f;
        }

        public void PushRequests()
        {

        }
    }
}
