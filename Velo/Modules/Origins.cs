using CEngine.Graphics.Library;
using CEngine.World.Actor;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Velo
{
    public class OrMenuContext : MenuContext
    {
        public OrMenuContext()
        {

        }

        public override void OnExit()
        {
            Miscellaneous.Instance.OriginsMenu.Disable();
        }

        public override void OnCancelAllRequests()
        {

        }

        public override void OnClearCache()
        {

        }
    }

    public class Origins
    {
        private bool initialized = false;

        private OrMenuContext context;

        public ulong Selected = ulong.MaxValue;
        public ulong Current = ulong.MaxValue;

        private bool isOrigins = false;

        private bool finished = false;

        public readonly IEnumerable<OriginMap> Maps =
            Enumerable.Range((int)Map.ORIGINS_START, (int)(Map.ORIGINS_END - Map.ORIGINS_START)).
            Select(m => new OriginMap(Map.MapIdToFileName[(ulong)m]));

        private Origins()
        {

        }

        public static Origins Instance = new Origins();

        private void Initialize()
        {
            context = new OrMenuContext();

            initialized = true;
        }

        public void Init()
        {
            Velo.OnMainPlayerReset.Add(ResetActors);
        }

        public void PostUpdate()
        {
            if (!Velo.Ingame && Selected != ulong.MaxValue)
                isOrigins = true;
            if (Velo.Ingame && !Velo.IngamePrev)
            {
                Current = Selected;
                Selected = ulong.MaxValue;
            }
            if (!Velo.Ingame && Selected == ulong.MaxValue)
                isOrigins = false;
        }

        public void PostRender()
        {
            bool modified = Miscellaneous.Instance.OriginsMenu.Modified();

            if (modified)
            {
                if (Miscellaneous.Instance.OriginsMenu.Value.Enabled)
                    Cursor.EnableCursor(this);
                else
                    Cursor.DisableCursor(this);
            }

            if (Miscellaneous.Instance.OriginsMenu.Value.Enabled && !initialized)
                Initialize();

            if (!Miscellaneous.Instance.OriginsMenu.Value.Enabled && !initialized)
                return;

            if (modified)
            {
                if (Miscellaneous.Instance.OriginsMenu.Value.Enabled)
                {
                    context.EnterMenu(new OriginsMenu(context));
                }
                else
                {
                    context.ExitMenu(animation: true);
                }
            }

            context.Draw();
        }

        public void SelectOrigins(ulong mapId)
        {
            Selected = mapId;
            if (mapId != ulong.MaxValue)
                Notifications.Instance.PushNotification("Now please enter any official map!", Color.Red, TimeSpan.FromSeconds(3d));
        }

        public bool EnterOrigins()
        {
            return Selected != ulong.MaxValue;
        }

        public string OriginsPath()
        {
            if (Selected == ulong.MaxValue)
                return "";

            string path = "Levels\\" + Map.MapIdToFileName[Selected];
            string[] files = Directory.GetFiles("Content\\" + path);
            files[0] = files[0].Substring(files[0].LastIndexOf('\\') + 1);
            files[0] = files[0].Substring(0, files[0].Length - 4);
            path += "\\" + files[0];
            return path;
        }

        public bool IsOrigins()
        {
            return isOrigins && Velo.ModuleSolo != null;
        }

        public void TouchFinishBomb(Player player)
        {
            if (player != Velo.MainPlayer)
                return;
            if (!finished)
            {
                Velo.MainPlayer.PlayFinishLapSound();
                Velo.ModuleSolo.timer.Trigger();
                finished = true;
            }
        }

        public void ResetActors()
        {
            finished = false;

            List<CActor> actors = CEngine.CEngine.Instance.World.CollisionEngine.actors;
            foreach (CActor actor in actors)
            {
                if (actor.controller is Boss1)
                {
                    actor.controller.Reset();
                }
                if (actor.controller is Boss2)
                {
                    actor.controller.Reset();
                }
                if (actor.controller is Boss4)
                {
                    actor.controller.Reset();
                }
                if (actor.controller is BossSaw)
                {
                    actor.controller.Reset();
                }
                if (actor.controller is FallBlock)
                {
                    actor.controller.Reset();
                }
                if (actor.controller is MovingPlatform)
                {
                    actor.controller.Reset();
                }
                if (actor.controller is Pickup)
                {
                    actor.controller.Reset();
                }
            }
        }
    }

    public struct OriginMap
    {
        public string Name;

        public OriginMap(string name)
        {
            Name = name;
        }
    }

    public class OriginMapEntry : LabelW
    {
        private readonly ulong mapId;

        public OriginMapEntry(CFont font, ulong mapId, Action exitMenu) :
            base(Map.MapIdToName(mapId), font)
        {
            this.mapId = mapId;

            Align = new Vector2(0f, 0.5f);
            Padding = new Vector2(10f, 0f);
            Style.ApplyText(this);

            OnClick = (wevent) =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    if (mapId == Origins.Instance.Selected)
                        Origins.Instance.SelectOrigins(ulong.MaxValue);
                    else
                    {
                        Origins.Instance.SelectOrigins(mapId);
                        exitMenu();
                    }
                }
            };
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            if (mapId == Origins.Instance.Selected)
                Color = Leaderboard.Instance.HighlightTextColor.Value.Get;
            else
                Color = Leaderboard.Instance.TextColor.Value.Get;

            base.Draw(hovered, scale, opacity);
        }
    }

    public class OriginsMenu : Menu, IListEntryFactory<OriginMap>
    {
        private readonly LabelW title;
        private readonly LabelW info;
        private readonly ScrollW scroll;
        private readonly ListW<OriginMap> list;
        private readonly LayoutW layout;

        public OriginsMenu(OrMenuContext context) :
            base(context)
        {
            layout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            title = new LabelW("Origins", context.Fonts.FontLarge.Font)
            {
                Align = new Vector2(0f, 0.5f),
                Color = Leaderboard.Instance.HeaderTextColor.Value.Get
            };

            list = new ListW<OriginMap>((int)(Map.ORIGINS_END - Map.ORIGINS_START), this)
            {
                EntryBackgroundVisible = true,
                EntryBackgroundColor1 = Leaderboard.Instance.EntryColor1.Value.Get,
                EntryBackgroundColor2 = Leaderboard.Instance.EntryColor2.Value.Get,
                EntryHoverable = true,
                EntryBackgroundColorHovered = Leaderboard.Instance.EntryHoveredColor.Value.Get
            };
            scroll = new ScrollW(list)
            {
                ScrollBarColor = Leaderboard.Instance.ButtonColor.Value.Get,
                ScrollBarWidth = Leaderboard.Instance.ScrollBarWidth.Value
            };
            info = new LabelW("", context.Fonts.FontMedium.Font)
            {
                Align = new Vector2(0f, 0.5f),
                Color = () => Color.Red
            };

            layout.AddChild(title, 80f);
            layout.AddSpace(10f);
            layout.AddChild(info, 40f);
            layout.AddSpace(10f);
            layout.AddChild(scroll, LayoutW.FILL);
            
            Child = layout;
        }

        public override void Draw(Widget hovered, float scale, float opacity)
        {
            info.Text = "";
            if (Origins.Instance.Selected != ulong.MaxValue)
                info.Text = "Now please enter any official map!";
            
            base.Draw(hovered, scale, opacity);
        }

        public Widget Create(OriginMap elem, int i)
        {
            return new OriginMapEntry(context.Fonts.FontMedium.Font, Map.ORIGINS_START + (ulong)i, () => context.ExitMenu(animation: false));
        }

        public IEnumerable<OriginMap> GetElems()
        {
            return Origins.Instance.Maps;
        }

        public float Height(OriginMap elem, int i)
        {
            return 40f;
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
}
