using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using Steamworks;
using System.Threading.Tasks;

namespace Velo
{
    public interface ILbWidget : IWidget
    {
        void PushRequests();
    }

    public abstract class LbMenuPage : VLayoutW, ILbWidget
    {
        protected readonly LbContext context;

        protected LabelW title;
        protected HLayoutW titleBar;
        protected HolderW content;
        protected readonly HLayoutW buttonRowUpper;
        protected readonly HLayoutW buttonRowLower;

        public LbMenuPage(LbContext context, string title, bool buttonRowUpper = false, bool buttonRowLower = false, bool buttonRowSpace = true)
        {
            this.context = context;

            content = new HolderW(null);

            if (title != "")
            {
                this.title = new LabelW(title, context.Fonts.FontTitle)
                {
                    Align = new Vector2(0f, 0.5f),
                    Color = SettingsUI.Instance.HeaderTextColor.Value.Get
                };
                titleBar = new HLayoutW();
                titleBar.AddChild(this.title, FILL);
                AddChild(titleBar, 80f);
                AddSpace(10f);
            }
            AddChild(content, FILL);
            if (buttonRowSpace && (buttonRowLower || buttonRowUpper))
                AddSpace(10f);
            if (buttonRowUpper)
            {
                this.buttonRowUpper = new HLayoutW();
                AddChild(this.buttonRowUpper, 35f);
            }
            if (buttonRowLower)
            {
                this.buttonRowLower = new HLayoutW();
                AddChild(this.buttonRowLower, 35f);
            }
        }

        public abstract void PushRequests();
    }

    public abstract class LbTable<T> : VLayoutW, ILbWidget, ITableEntryFactory<T> where T : struct
    {
        protected readonly LbContext context;

        protected TableW<T> table;
        
        public LbTable(LbContext context)
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

        public IEnumerable<T> GetElems(int start)
        {
            return GetElems().Skip(start);
        }

        public int Length => GetElems().Count();
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
            if (SteamFriends.HasFriend(new CSteamID(run.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;
            if (run.Id == -1)
                Text = "";
            else if (run.Place != -1)
                Text = "" + (run.Place + 1);
            else
                Text = "-";
        }
    }

    public class PlayerEntry : HLayoutW
    {
        private readonly ImageW image;
        private readonly HScrollW<LabelW> label;

        private readonly RunInfo run;

        public PlayerEntry(CachedFont font, RunInfo run)
        {
            this.run = run;
            
            if (run.Id == -1)
                return;

            image = new ImageW(null);
            label = new HScrollW<LabelW>(new LabelW("", font)
            {
                Align = new Vector2(0f, 0.5f),
                CropText = false
            })
            {
                AutoscrollDelay = 2f,
                AutoscrollSpeed = 50f
            };

            AddChild(image, 33f);
            AddSpace(4f);
            AddChild(label, FILL);
            Style.ApplyText(label.Child);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                label.Child.Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            if (SteamFriends.HasFriend(new CSteamID(run.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                label.Child.Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;

            image.Image = SteamCache.GetAvatar(run.PlayerId);
            label.Child.Text = RunsDatabase.Instance.GetPlayerName(run.PlayerId);
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            if (run.Id == -1)
                return;

            image.Image = SteamCache.GetAvatar(run.PlayerId);
            label.Child.Text = RunsDatabase.Instance.GetPlayerName(run.PlayerId);

            base.Draw(hovered, parentCropRec, scale, opacity);
        }
    }

    public class TimeEntry : HScrollW<LabelW>
    {
        public TimeEntry(CachedFont font, RunInfo run) :
            base(new LabelW("", font)
            {
                CropText = false
            })
        {
            Child.Align = new Vector2(0f, 0.5f);
            Style.ApplyText(Child);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Child.Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            if (SteamFriends.HasFriend(new CSteamID(run.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                Child.Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;

            AutoscrollDelay = 2f;
            AutoscrollSpeed = 50f;

            if (run.Id == -1)
            {
                Child.Text = "";
            }
            else
            {
                long time = run.RunTime;
                Child.Text = Util.FormatTime(time, Leaderboard.Instance.TimeFormat.Value);
                if ((run.InfoFlags & RunInfo.FLAG_OUTDATED) != 0)
                    Child.Text += "*";
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
            if (SteamFriends.HasFriend(new CSteamID(run.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;
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

    public class RemainingTimeEntry : LabelW
    {
        private MapEvent mapEvent;

        public RemainingTimeEntry(CachedFont font, MapEvent mapEvent, ulong wrHolder) :
            base("", font)
        {
            this.mapEvent = mapEvent;

            Align = new Vector2(0f, 0.5f);
            Style.ApplyText(this);
            if (wrHolder == SteamUser.GetSteamID().m_SteamID)
                Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            if (SteamFriends.HasFriend(new CSteamID(wrHolder), EFriendFlags.k_EFriendFlagImmediate))
                Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            long now = RunsDatabase.Instance.EventTime / TimeSpan.TicksPerSecond;
            long diff = mapEvent.To - now;
            Text = diff >= 0 ? Util.ApproxTime(diff) : "over";

            base.Draw(hovered, parentCropRec, scale, opacity);
        }
    }

    public class MapEntry : HScrollW<LabelW>
    {
        private RunInfo run;
        
        public MapEntry(CachedFont font, RunInfo run) :
            base(
                new LabelW("", font)
                {
                    CropText = false
                })
        {
            this.run = run;

            AutoscrollDelay = 2f;
            AutoscrollSpeed = 50f;

            Child.Align = new Vector2(0f, 0.5f);
            Style.ApplyText(Child);
            if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                Child.Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            if (SteamFriends.HasFriend(new CSteamID(run.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                Child.Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            Child.Text = Map.MapIdToName(run.Category.MapId);

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
            if (SteamFriends.HasFriend(new CSteamID(run.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;

            Text = ((ECategoryType)run.Category.TypeId).Label();
        }
    }

    public class AllMapEntry : HScrollW<LabelW>
    {
        private readonly ulong mapId;

        public AllMapEntry(CachedFont font, ulong mapId) :
            base(
                new LabelW("", font)
                {
                    CropText = false
                })
        {
            this.mapId = mapId;

            AutoscrollDelay = 2f;
            AutoscrollSpeed = 50f;

            Child.Align = new Vector2(0f, 0.5f);
            Style.ApplyText(Child);
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            Child.Text = Map.MapIdToName(mapId);

            base.Draw(hovered, parentCropRec, scale, opacity);
        }
    }

    public class PlayerTimeEntry : VLayoutW
    {
        public PlayerTimeEntry(CachedFont font, IEnumerable<RunInfo> runs, bool compact = false)
        {
            foreach (RunInfo run in runs)
            {
                LayoutW layout = new LayoutW(!compact ? EOrientation.VERTICAL : EOrientation.HORIZONTAL);

                PlayerEntry player = new PlayerEntry(font, run);

                LabelW time = new LabelW("", font)
                {
                    Align = new Vector2(!compact ? 0f : 1f, 0.5f),
                    CropText = false
                };

                if (!compact)
                {
                    layout.AddSpace(2);
                    layout.AddChild(time, 28);
                    layout.AddChild(player, 40);
                    layout.AddSpace(4);
                }
                else
                {
                    layout.AddChild(player, FILL);
                    layout.AddSpace(10);
                    layout.AddChild(time, REQUESTED_SIZE);
                }
                Style.ApplyText(time);
                if (run.PlayerId == SteamUser.GetSteamID().m_SteamID)
                    time.Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
                if (SteamFriends.HasFriend(new CSteamID(run.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                    time.Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;

                if (run.Id == -1)
                {
                    time.Text = "";
                }
                else
                {
                    time.Text = Util.FormatTime(run.RunTime, Leaderboard.Instance.TimeFormat.Value);
                    if ((run.InfoFlags & RunInfo.FLAG_OUTDATED) != 0)
                        time.Text += "*";
                }
                AddChild(layout, !compact ? 74.0f : 40.0f);
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
                if ((run.InfoFlags & RunInfo.FLAG_OUTDATED) != 0)
                    Text += "*";
                if (run.Place == 0)
                    Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            }
        }
    }

    public abstract class LbRunsTable : LbTable<RunInfo>
    {
        private class ExpandedRow : DecoratedW<TransitionW>
        {
            private readonly RunInfo run;

            private readonly IWidget original;
            private readonly ButtonW viewProfile;
            private readonly ButtonW mapPage;
            private readonly ButtonW setGhost;
            private readonly ButtonW viewReplay;
            private readonly ButtonW verify;
            private readonly LabelW comments;
            private readonly VLayoutW layout;

            public IWidget Original => original;
            public IWidget Expanded => layout;

            public ExpandedRow(IWidget original, LbContext context, RunInfo run, bool mapPageButton, Action<int, Playback.EPlaybackType> requestRecording) :
                base(new TransitionW())
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

                viewProfile = new ButtonW("Profile page", context.Fonts.FontMedium);
                Style.ApplyButton(viewProfile);
                viewProfile.Hoverable = false;
                viewProfile.OnLeftClick = () =>
                {
                    context.ChangePage(new LbPlayerMenuPage(context, run.PlayerId));
                };
                mapPage = new ButtonW("Map page", context.Fonts.FontMedium);
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

                setGhost = new ButtonW("Set ghost", context.Fonts.FontMedium);
                Style.ApplyButton(setGhost);
                setGhost.Hoverable = false;
                setGhost.OnLeftClick = () =>
                {
                    requestRecording(run.Id, Playback.EPlaybackType.SET_GHOST);
                };
                viewReplay = new ButtonW("Watch replay", context.Fonts.FontMedium);
                Style.ApplyButton(viewReplay);
                viewReplay.Hoverable = false;
                viewReplay.OnLeftClick = () =>
                {
                    requestRecording(run.Id, Playback.EPlaybackType.VIEW_REPLAY);
                };
                verify = new ButtonW("Verify", context.Fonts.FontMedium);
                Style.ApplyButton(verify);
                verify.Hoverable = false;
                verify.OnLeftClick = () =>
                {
                    requestRecording(run.Id, Playback.EPlaybackType.VERIFY);
                };

                if ((run.InfoFlags & RunInfo.FLAG_DELETED_RECORDING) != 0)
                {
                    setGhost.Color = SettingsUI.Instance.TextGreyedOutColor.Value.Get;
                    viewReplay.Color = SettingsUI.Instance.TextGreyedOutColor.Value.Get;
                    verify.Color = SettingsUI.Instance.TextGreyedOutColor.Value.Get;
                    setGhost.BackgroundColor = SettingsUI.Instance.ButtonGreyedOutColor.Value.Get;
                    viewReplay.BackgroundColor = SettingsUI.Instance.ButtonGreyedOutColor.Value.Get;
                    verify.BackgroundColor = SettingsUI.Instance.ButtonGreyedOutColor.Value.Get;
                }

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

                layout = new VLayoutW();
                layout.AddSpace(10f);
                layout.AddChild(infos, 280f);
                layout.AddSpace(10f);
                if ((run.InfoFlags & RunInfo.FLAG_OUTDATED) != 0)
                {
                    LabelW outdatedMessage = new LabelW("This run was played on an older map version!", context.Fonts.FontMedium);
                    Style.ApplyText(outdatedMessage);
                    outdatedMessage.Align = new Vector2(0.5f, 0.0f);
                    outdatedMessage.Color = () => Color.Red;
                    layout.AddChild(outdatedMessage, 40f);
                }
                if (run.HasComments == 1)
                {
                    comments = new LabelW(RunsDatabase.Instance.GetComment(run.Id), context.Fonts.FontMedium);
                    Style.ApplyText(comments);
                    comments.Align = Vector2.Zero;
                    comments.Padding = 10f * Vector2.One;
                    VScrollW scroll = new VScrollW(comments)
                    {
                        BackgroundColor = () => new Color(20, 20, 20, 150),
                        BackgroundVisible = true,
                        ScrollBarColor = () => SettingsUI.Instance.ButtonColor.Value.Get()
                    };

                    HLayoutW commentsLayout = new HLayoutW();
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

                ChildWidget.GoTo(original);

                Hoverable = true;
            }

            public void TransitionTo(IWidget widget, Action onFinish = null)
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
                ChildWidget.TransitionTo(widget, 8f, Vector2.Zero, onFinish: () =>
                {
                    if (widget == Expanded)
                    {
                        viewProfile.Hoverable = true;
                        mapPage.Hoverable = true;
                        if ((run.InfoFlags & RunInfo.FLAG_DELETED_RECORDING) == 0)
                        {
                            if (setGhost.OnClick != null)
                                setGhost.Hoverable = true;
                            if (viewReplay.OnClick != null)
                                viewReplay.Hoverable = true;
                            if (verify.OnClick != null)
                                verify.Hoverable = true;
                        }
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
                        expandedRow = new ExpandedRow(row, context, elem, mapPageButton, InitiatePlayback);
                        expandedRow.AddDecorator(new ClickableW { OnClick = row.GetDecorator<ClickableW>().OnClick });
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

            int ghostIndex = !RecordingAndReplay.Instance.EnableMultiGhost.Value ? 0 : RecordingAndReplay.Instance.GhostPlaybackCount;
            if (type == Playback.EPlaybackType.SET_GHOST)
                Ghosts.Instance.GetOrSpawn(ghostIndex, RecordingAndReplay.Instance.GhostDifferentColors.Value);

            RunsDatabase.Instance.RequestRecordingCached(id, (recording) =>
            {
                Task.Run(() =>
                {
                    if (type == Playback.EPlaybackType.SET_GHOST)
                        Ghosts.Instance.WaitForGhost(ghostIndex);
                    Velo.AddOnPreUpdate(() =>
                    {
                        context.ExitMenu(false);
                        RecordingAndReplay.Instance.StartPlayback(recording, type, ghostIndex, notification: true);
                    });
                });
            },
                (error) => context.Error = error.Message
            );
        }

        private bool NeedRequestMore()
        {
            return !initialRequest && table.ReachedEnd && Count == requestCount && Count <= Length;
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
                float ease = expandedRow.ChildWidget.Ease.Get();
                if (elem.Id != expandedId)
                    ease = 1f - ease;
                float expandedHeight = 350f;
                if (elem.HasComments == 1)
                    expandedHeight += 210f;
                if ((elem.InfoFlags & RunInfo.FLAG_OUTDATED) != 0)
                    expandedHeight += 40f;

                return (1f - ease) * 40f + ease * expandedHeight;
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
            table.AddSpace(10f);
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
        private readonly ButtonW backButton;
        private readonly LabelW eventLabel;
        private readonly VLayoutW eventLabelLayout;
        private readonly ButtonW showEventButton;
        private readonly VLayoutW showEventButtonLayout;
        private readonly ButtonW workshopPageButton;
        private readonly VLayoutW workshopPageButtonLayout;
        private readonly ButtonW enterMapButton;
        private readonly VLayoutW enterMapButtonLayout;

        private bool eventsTabShown = true;
        private readonly LayoutChild categorySelectChild;
        private readonly LayoutChild eventLabelLayoutChild;
        private readonly LayoutChild showEventButtonChild;
        private readonly LayoutChild showEventButtonSpaceChild;

        public LbMapMenuPage(LbContext context, ulong mapId, ECategoryType categoryType = ECategoryType.NEW_LAP) :
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
            int selected = categories.IndexOf(categoryType);
            if (selected == -1)
                selected = 0;
            categorySelect = new SelectorButtonW(categoryLabels, selected, context.Fonts.FontMedium);
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

            backButton = new ButtonW("Back", context.Fonts.FontMedium);
            Style.ApplyButton(backButton);
            backButton.OnLeftClick = context.ChangeBack;

            buttonRowUpper.AddSpace(FILL);
            buttonRowUpper.AddChild(filterSelect, 570f);

            buttonRowLower.AddChild(backButton, 190f);
            buttonRowLower.AddSpace(FILL);
            categorySelectChild = buttonRowLower.AddChild(categorySelect, categorySelect.ShownCount * 190);

            eventLabel = new LabelW("", context.Fonts.FontMedium);
            Style.ApplyText(eventLabel);
            eventLabel.Align = new Vector2(1f, 0.5f);

            showEventButton = new ButtonW("View event", context.Fonts.FontMedium);
            Style.ApplyButton(showEventButton);
            showEventButton.OnLeftClick = () =>
            {
                ShowEventsTab();
                categorySelect.Selected = categorySelect.Count - 1;
                filterSelect.Selected = 0;
            };

            eventLabelLayout = new VLayoutW();
            eventLabelLayout.AddSpace(FILL);
            eventLabelLayout.AddChild(eventLabel, 50f);

            showEventButtonLayout = new VLayoutW();
            showEventButtonLayout.AddSpace(FILL);
            showEventButtonLayout.AddChild(showEventButton, 35f);

            eventLabelLayoutChild = titleBar.AddChild(eventLabelLayout, 0f);
            titleBar.AddSpace(10f);

            showEventButtonChild = titleBar.AddChild(showEventButtonLayout, 170f);
            showEventButtonSpaceChild = titleBar.AddSpace(10f);

            if (mapEvent.From == 0)
            {
                showEventButtonChild.Size = 0f;
                showEventButtonSpaceChild.Size = 0f;
                showEventButton.Visible = false;
            }

            ulong fileId = mapId;
            if (Map.IsOther(mapId) || Map.MapIdToFileId.TryGetValue(mapId, out fileId))
            {
                workshopPageButton = new ButtonW("Workshop page", context.Fonts.FontMedium);
                Style.ApplyButton(workshopPageButton);
                workshopPageButton.OnLeftClick = () =>
                {
                    SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/sharedfiles/filedetails/?id=" + fileId);
                };
                workshopPageButtonLayout = new VLayoutW();
                workshopPageButtonLayout.AddSpace(FILL);
                workshopPageButtonLayout.AddChild(workshopPageButton, 35f);

                titleBar.AddChild(workshopPageButtonLayout, 220f);
            }
            if (Map.IsOrigins(mapId))
            {
                enterMapButton = new ButtonW("Enter map", context.Fonts.FontMedium);
                Style.ApplyButton(enterMapButton);
                enterMapButton.OnLeftClick = () =>
                {
                    Origins.Instance.SelectOrigins(mapId);
                    context.ExitMenu(animation: false);
                };
                enterMapButtonLayout = new VLayoutW();
                enterMapButtonLayout.AddSpace(FILL);
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
            categorySelectChild.Size = categorySelect.ShownCount * 190f;
            showEventButtonChild.Size = 170f;
            showEventButtonSpaceChild.Size = 10f;
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
                eventLabelLayoutChild.Size = 0f;
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
                eventLabelLayoutChild.Size = eventLabel.MeasureTextSize.X + 10f;
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
            table.AddSpace(10f);
            var getOrDefault = (Func<IEnumerable<RunInfo>, RunInfo>)(infos => infos.Count() > 0 ? infos.First() : new RunInfo() { Id = -1 });
            if (filter != EFilter.ORIGINS)
            {
                table.AddColumn("New Lap", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, getOrDefault(runs.NewLap)));
                table.AddColumn("1 Lap", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, getOrDefault(runs.OneLap)));
                if (filter != EFilter.OTHER)
                {
                    table.AddColumn("New Lap (Skip)", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, getOrDefault(runs.NewLapSkip)));
                    table.AddColumn("1 Lap (Skip)", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, getOrDefault(runs.OneLapSkip)));
                }
            }
            else
            {
                table.AddColumn("Any%", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, getOrDefault(runs.AnyPerc)));
                table.AddColumn("100%", colWidth, (runs) => new TimePlaceEntry(context.Fonts.FontMedium, getOrDefault(runs.HundredPerc)));
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
                    return RunsDatabase.Instance.GetPlayerPBs(PlayerId, curated: false, popularity: true).Where((infos) => infos.NewLap.Count > 0 || infos.OneLap.Count > 0);
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
        private readonly ButtonW backButton;
        private readonly SelectorButtonW filterSelect;
        private readonly TabbedW<LbPlayerRunsTable> tables;
        private readonly ButtonW steamPageButton;
        private readonly VLayoutW steamPageButtonLayout;

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
            titleBar.AddChild(title, FILL);

            backButton = new ButtonW("Back", context.Fonts.FontMedium);
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

            steamPageButton = new ButtonW("Steam page", context.Fonts.FontMedium);
            Style.ApplyButton(steamPageButton);
            steamPageButton.OnLeftClick = () =>
            {
                SteamFriends.ActivateGameOverlayToUser("steamid", new CSteamID(playerId));
            };

            steamPageButtonLayout = new VLayoutW();
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
            int pbsNonScored = 0;
            long perfectTimeSum = 0;
            long timeSum = 0;
            foreach (MapRunInfos infos in RunsDatabase.Instance.GetPlayerPBs(playerId, curated: true, popularity: false).Concat(RunsDatabase.Instance.GetPlayerPBs(playerId, curated: false, popularity: true)))
            {
                for (int t = 0; t < 6; t++)
                {
                    if (infos.Get((ECategoryType)t).Count() == 0)
                        continue;
                    RunInfo info = infos.Get((ECategoryType)t).First();
                    if (info.Id != -1)
                    {
                        if (Map.IsScored(info.Category.MapId))
                        {
                            pbs++;
                            perfectTimeSum += RunsDatabase.Instance.GetWR(info.Category).RunTime;
                            timeSum += info.RunTime;
                        }
                        else
                            pbsNonScored++;
                    }
                }
            }
            statsValues1.Text += "" + pbs + (pbsNonScored > 0 ? "+" + pbsNonScored : "");

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
            table.AddSpace(10f);
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
                    return elems.OrderByDescending(infos => Enumerable.Range(0, 6).Select(i => infos.Get((ECategoryType)i).Count() > 0 ? infos.Get((ECategoryType)i).First().Id : -1).Max());
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
            return 
                (Filter != EFilter.ORIGINS && Filter != EFilter.OTHER ? 74f : 40f) *
                Enumerable.Range(0, 6).Select(t => elem.Get((ECategoryType)t).Count()).Max();
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
        private readonly ButtonW placeDecrButton;
        private readonly ButtonW placeIncrButton;
        private readonly VLayoutW placeSelectLayoutV;
        private readonly HLayoutW placeSelectLayoutH;
        private readonly LabelW orderByLabel;
        private readonly ButtonW sortingButton;
        private readonly VLayoutW sortingLayout;

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
            sortingButton = new ButtonW(tables.Current.Sorting.Label(), context.Fonts.FontMedium);
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
            sortingLayout = new VLayoutW();
            sortingLayout.AddChild(orderByLabel, 25f);
            sortingLayout.AddSpace(10f);
            sortingLayout.AddChild(sortingButton, 35f);
            titleBar.AddChild(sortingLayout, 190f);

            placeLabel = new LabelW("Place:", context.Fonts.FontMedium);
            Style.ApplyText(placeLabel);
            placeSelectLabel = new LabelW("1st", context.Fonts.FontMedium);
            Style.ApplyText(placeSelectLabel);
            placeDecrButton = new ButtonW("-", context.Fonts.FontMedium);
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
            placeIncrButton = new ButtonW("+", context.Fonts.FontMedium);
            Style.ApplyButton(placeIncrButton);
            placeIncrButton.OnLeftClick = () =>
            {
                place++;
                UpdatePlaceLabel();
                context.Request();
            };
            placeSelectLayoutH = new HLayoutW();
            placeSelectLayoutH.AddChild(placeDecrButton, 50f);
            placeSelectLayoutH.AddChild(placeSelectLabel, 90f);
            placeSelectLayoutH.AddChild(placeIncrButton, 50f);
            placeSelectLayoutV = new VLayoutW();
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
            table.AddSpace(10f);
            table.AddColumn("Category", 170f, (run) => new CategoryEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Player", 340f, (run) => new PlayerEntry(context.Fonts.FontMedium, run));
            table.AddSpace(10f);
            table.AddColumn("Time", 170f, (run) => new TimeEntry(context.Fonts.FontMedium, run));
            table.AddColumn("Age", 150f, (run) => new AgeEntry(context.Fonts.FontMedium, run));
            table.AddColumn("#", 50f, (run) => new PlaceEntry(context.Fonts.FontMedium, run));
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

    public class LbEventsTable : LbTable<MapEventWithRun>
    {
        public enum EFilter
        {
            WRS, YOUR_PB,
            COUNT
        }

        private readonly EFilter filter;

        public LbEventsTable(LbContext context, EFilter filter) :
            base(context)
        {
            this.filter = filter;

            table.AddColumn("Map", FILL, pair => new MapEntry(context.Fonts.FontMedium, pair.Run));
            table.AddSpace(10f);
            table.AddColumn("Category", 170f, pair => new CategoryEntry(context.Fonts.FontMedium, pair.Run));
            table.AddColumn("Player", 340f, pair => new PlayerEntry(context.Fonts.FontMedium, pair.Run));
            table.AddSpace(10f);
            table.AddColumn("Time", 170f, pair => new TimeEntry(context.Fonts.FontMedium, pair.Run));
            table.AddColumn("Ends in", 190f, pair => new RemainingTimeEntry(context.Fonts.FontMedium, pair.Event, pair.Run.PlayerId));

            table.OnLeftClickRow = (row, e, i) =>
            {
                context.ChangePage(new LbMapMenuPage(context, e.Run.Category.MapId, ECategoryType.EVENT));
            };
        }

        public override IEnumerable<MapEventWithRun> GetElems()
        {
            if (filter == EFilter.WRS)
                return RunsDatabase.Instance.GetEventsWithWRs();
            else
                return RunsDatabase.Instance.GetEventsWithYourPBs();
        }

        public override float Height(MapEventWithRun elem, int i)
        {
            return 40f;
        }

        public override void PushRequests()
        {
            if (filter == EFilter.WRS)
                RunsDatabase.Instance.PushRequestRuns(new GetEventWRsRequest(), Refresh);
            else
                RunsDatabase.Instance.PushRequestRuns(new GetPlayerEventPBsRequest(SteamUser.GetSteamID().m_SteamID), Refresh);
        }
    }

    public class LbEventsMenuPage : LbMenuPage
    {
        private readonly SelectorButtonW filterSelect;
        private readonly TabbedW<LbEventsTable> tables;
        private readonly ButtonW backButton;

        public LbEventsMenuPage(LbContext context) :
            base(context, "Events", buttonRowLower: true)
        {
            string[] filters = new string[] { "WRs", "Your PBs" };

            filterSelect = new SelectorButtonW(filters, 0, context.Fonts.FontMedium);
            Style.ApplySelectorButton(filterSelect);

            tables = new TabbedW<LbEventsTable>(filterSelect);
            for (int i = 0; i < filters.Length; i++)
            {
                tables.SetTab(i, new LbEventsTable(context, (LbEventsTable.EFilter)i));
            }
            tables.OnSwitch = _ => context.Request();

            backButton = new ButtonW("Back", context.Fonts.FontMedium);
            Style.ApplyButton(backButton);
            backButton.OnLeftClick = context.ChangeBack;

            content.Child = tables;

            buttonRowLower.AddChild(backButton, 190f);
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
                if (SteamFriends.HasFriend(new CSteamID(player.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                    Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;
            }
        }

        public class PlayerEntry : HLayoutW
        {
            private readonly ImageW image;
            private readonly LabelW label;

            private readonly PlayerInfoScore player;

            public PlayerEntry(CachedFont font, PlayerInfoScore player) :
                base()
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
                if (SteamFriends.HasFriend(new CSteamID(player.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                    label.Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;
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
                if (SteamFriends.HasFriend(new CSteamID(player.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                    Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;

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
                if (SteamFriends.HasFriend(new CSteamID(player.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                    Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;
            }
        }

        public class PlayerEntry : HLayoutW
        {
            private readonly ImageW image;
            private readonly LabelW label;

            private readonly PlayerInfoWRs player;

            public PlayerEntry(CachedFont font, PlayerInfoWRs player)
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
                if (SteamFriends.HasFriend(new CSteamID(player.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                    label.Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;
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
                if (SteamFriends.HasFriend(new CSteamID(player.PlayerId), EFriendFlags.k_EFriendFlagImmediate))
                    Color = SettingsUI.Instance.Highlight2TextColor.Value.Get;

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
        protected VScrollW scroll;

        public LbRulesMenuPage(LbContext context) :
            base(context, "Rules")
        {
            rules = new LabelW("", context.Fonts.FontMedium)
            {
                Align = Vector2.Zero,
                Padding = 10f * Vector2.One
            };
            Style.ApplyText(rules);

            scroll = new VScrollW(rules)
            {
                BackgroundVisible = true,
                BackgroundColor = () => SettingsUI.Instance.EntryColor2.Value.Get(),
                ScrollBarColor = () => SettingsUI.Instance.ButtonColor.Value.Get(),
                ScrollBarWidth = SettingsUI.Instance.ScrollBarWidth.Value
            };

            content.Child = scroll;

            AddSpace(10f);
            
            rules.Text =
@"Velo records, verifies, categorizes and submits your runs as you play. This process is fully automatic 
and requires no interaction. In case an invalid run still manages to get submitted either by a bug or 
in a cheated manner, contact a leaderboard moderator and they will be able to remove the run.
Only maps in the ""official"" or ""RWS"" category count towards score and WRs.

A run is categorized as ""1 lap"" if any of the following apply:
  -Lap was started by finishing a previous lap
  -Player had boost upon starting the lap
  -Player had boostacoke upon starting the lap (Laboratory only)
  -A gate was not closed upon starting the lap (except Club V)
  -A fall tile (black obstacle) was broken upon starting the lap
  -Lap did not start from countdown and ""reset lasers"" setting is disabled (Powerplant and Library only)
  -Player pressed lap reset while in wall climbing state and ""reset wall boost"" setting is disabled
  -Player pressed lap reset while in a jump state and ""reset jump boost"" setting is disabled

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
  -Player had boostacoke upon starting the lap (any map except Laboratory)
  -Player modified their boostacoke by pressing + or -
  -Time of the run is longer than 30 minutes
  -Physics, camera or TAS settings were changed in Velo (changing camera zoom on Origins is allowed)
  -Framelimit was set below 30 or above 300 in Velo
  -Blindrun simulator was enabled in Velo
  -A ""set"" command was used (infinite cooldown that does not reset when pressing ""reset lap"")
  -A savestate was used (1 second cooldown, infinite cooldown when using ""savestate load halt"")
  -A savestate from a version prior 2.2.11 was used
  -A replay was used (1 second cooldown for replays of own runs, 5 second cooldown for other replays)
  -A ghost used an item
  -Camera was focused to a ghost
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
Each PB run grants you somewhere between 1 to 1000 points, depending on how close it is to the WR run. 
If we denote WR time as 'wr' and your PB time as 'pb', you get 1000 * wr / (wr + 25 * (pb - wr)) points per 
run.

The new grapple cooldown:
On 09/OCT/2024 the game received an update, lowering the grapple cooldown from 0.25s to 0.20s. Playing
with either of these is allowed and you can see the value used for a run under ""GCD"".

The flat slope bounce glitch fix:
On 12/DEC/2024 the game received an update, fixing the flat slope bounce glitch. Flat slope in particular
is a glitch where 25 units behind the peak of a slope there is a 1 unit wide spot on the ground that has
slope properties. This fix removes this spot when going up the slope, making it impossible to get bounced
up by it, while still allowing you to land on it from above. Playing with or without this fix is allowed and
you can see whether it was applied to a run under ""Fix BG"".";
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

    public class PopularWindow : VLayoutW, ILbWidget, IListEntryFactory<ulong>
    {
        public class PopularMapEntry : HLayoutW
        {
            private readonly MapEntry mapName;
            private readonly ImageW image;
            private readonly TimeEntry time;

            private readonly RunInfo wr;

            public PopularMapEntry(CachedFont font, RunInfo wr)
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

        private readonly HLayoutW headerLayout;
        private readonly LabelW title;
        private readonly ButtonW compactButton;
        private readonly ListW<ulong> maps;
        private readonly VScrollW scroll;
        private readonly VLayoutW layout;
        private readonly FadeW move;

        public PopularWindow(LbContext context)
        {
            this.context = context;
            title = new LabelW("Popular this week", context.Fonts.FontMedium);
            Style.ApplyText(title);
            title.Color = SettingsUI.Instance.HeaderTextColor.Value.Get;
            title.Align = new Vector2(0f, 0f);
            title.Offset = new Vector2(0f, -3f);
            compactButton = new ButtonW("", context.Fonts.FontSmall);
            Style.ApplyButton(compactButton);
            compactButton.OnLeftClick = () =>
            {
                SettingsUI.Instance.PopularThisWeekCompacted.Value = !SettingsUI.Instance.PopularThisWeekCompacted.Value;
                Compact(SettingsUI.Instance.PopularThisWeekCompacted.Value, 2f);
            };
            headerLayout = new HLayoutW();
            headerLayout.AddChild(title, FILL);
            headerLayout.AddChild(compactButton, 40f);

            maps = new ListW<ulong>(this);
            Style.ApplyList(maps);
            
            scroll = new VScrollW(maps);

            layout = new VLayoutW();
            layout.AddChild(headerLayout, 25f);
            layout.AddSpace(8f);
            layout.AddChild(scroll, FILL);

            move = new FadeW(layout);
            AddChild(move, FILL);

            Compact(SettingsUI.Instance.PopularThisWeekCompacted.Value, 10000f);
        }

        public void Compact(bool compact, float speed)
        {
            if (!compact)
            {
                move.FadeTo(speed, 1f, new Vector2(0f, 383f));
                compactButton.Text = "∧";
            }
            else
            {
                move.FadeTo(speed, 1f, Vector2.Zero);
                compactButton.Text = "∨";
            }
        }

        public IWidget Create(ulong elem, int i)
        {
            RunInfo info = RunsDatabase.Instance.GetWR(new Category { MapId = elem, TypeId = (ulong)ECategoryType.NEW_LAP });
            if (info.Id == -1)
                info = RunsDatabase.Instance.GetWR(new Category { MapId = elem, TypeId = (ulong)ECategoryType.ONE_LAP });
            PopularMapEntry popularMapEntry = new PopularMapEntry(context.Fonts.FontSmall, info);
            DecoratedW decorated = new DecoratedW(popularMapEntry);
            decorated.AddDecorator(new ClickableW(null)
            {
                OnLeftClick = () =>
                {
                    bool wasMapMenuPage = context.Page.Child is LbMapMenuPage;
                    context.ChangePage(new LbMapMenuPage(context, elem));
                    if (wasMapMenuPage)
                        context.Page.PopLast();
                }
            });
            return decorated;
        }
        
        public IEnumerable<ulong> GetElems(int start)
        {
            return RunsDatabase.Instance.GetPopularThisWeek().Reverse().Skip(start);
        }

        public int Length => RunsDatabase.Instance.GetPopularThisWeek().Count();

        public float Height(ulong elem, int i)
        {
            return 25f;
        }

        public void PushRequests()
        {

        }
    }
}
