using CEngine.Graphics.Library;
using CEngine.World.Actor;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Velo
{
    public class Origins : MenuModule
    {
        public ulong Selected = ulong.MaxValue;
        public ulong Current = ulong.MaxValue;

        private bool isOrigins = false;

        private bool finished = false;

        public readonly IEnumerable<OriginMap> Maps =
            Enumerable.Range((int)Map.ORIGINS_START, (int)(Map.ORIGINS_END - Map.ORIGINS_START)).
            Select(m => new OriginMap(Map.MapIdToFileName[(ulong)m]));

        private Origins() : base("Origins", addEnabledSetting: false)
        {

        }

        public static Origins Instance = new Origins();

        public override void Init()
        {
            base.Init();

            Velo.OnMainPlayerReset.Add(ResetActors);
        }

        public override void PostUpdate()
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
            if (player != Velo.MainPlayer || Velo.ModuleSolo == null)
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
            if (!IsOrigins())
                return;

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

        public override Menu GetStartMenu()
        {
            return new OriginsMenu(this);
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

        public OriginMapEntry(CachedFont font, ulong mapId, Action exitMenu) :
            base(Map.MapIdToName(mapId), font)
        {
            this.mapId = mapId;

            Align = new Vector2(0f, 0.5f);
            Padding = new Vector2(10f, 0f);
            Style.ApplyText(this);

            OnLeftClick = () =>
            {
                if (mapId == Origins.Instance.Selected)
                    Origins.Instance.SelectOrigins(ulong.MaxValue);
                else
                {
                    Origins.Instance.SelectOrigins(mapId);
                    exitMenu();
                }
            };
        }

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            if (mapId == Origins.Instance.Selected)
                Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            else
                Color = SettingsUI.Instance.TextColor.Value.Get;

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

        public OriginsMenu(MenuModule context) :
            base(context)
        {
            layout = new LayoutW(LayoutW.EOrientation.VERTICAL);
            title = new LabelW("Origins", context.Fonts.FontTitle)
            {
                Align = new Vector2(0f, 0.5f),
                Color = SettingsUI.Instance.HeaderTextColor.Value.Get
            };

            list = new ListW<OriginMap>(this);
            Style.ApplyList(list);

            scroll = new ScrollW(list);
            Style.ApplyScroll(scroll);

            info = new LabelW("", context.Fonts.FontMedium)
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

        public override void Draw(IWidget hovered, float scale, float opacity)
        {
            info.Text = "";
            if (Origins.Instance.Selected != ulong.MaxValue)
                info.Text = "Now please enter any official map!";
            
            base.Draw(hovered, scale, opacity);
        }

        public Widget Create(OriginMap elem, int i)
        {
            return new OriginMapEntry(module.Fonts.FontMedium, Map.ORIGINS_START + (ulong)i, () => module.ExitMenu(animation: false));
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
    }
}
