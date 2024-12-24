using CEngine.World.Actor;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Velo
{
    public class OrContext : MenuContext
    {
        public OriginsMenu Menu;

        public OrContext(ToggleSetting enabled) : 
            base(enabled, enableDim: true)
        {
            Menu = new OriginsMenu(this);
            AddElem(Menu, StackW.TOP_LEFT, new Vector2(375f, 100f), new Vector2(1170f, 880f));
        }
    }

    public class Origins : ToggleModule
    {
        public ulong Selected = ulong.MaxValue;
        public ulong Current = ulong.MaxValue;

        private bool isOrigins = false;

        private bool finished = false;

        public readonly IEnumerable<OriginMap> Maps =
            Enumerable.Range((int)Map.ORIGINS_START, (int)(Map.ORIGINS_END - Map.ORIGINS_START)).
            Select(m => new OriginMap(Map.MapIdToFileName[(ulong)m]));

        public OrContext context;

        private Origins() : base("Origins", addEnabledSetting: false)
        {

        }

        public static Origins Instance = new Origins();

        public override void Init()
        {
            base.Init();

            context = new OrContext(Enabled);

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

        public override void PostRender()
        {
            base.PostRender();

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
    }

    public struct OriginMap
    {
        public string Name;

        public OriginMap(string name)
        {
            Name = name;
        }
    }

    public class OriginMapEntry : ButtonW
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

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            if (mapId == Origins.Instance.Selected)
                Color = SettingsUI.Instance.HighlightTextColor.Value.Get;
            else
                Color = SettingsUI.Instance.TextColor.Value.Get;

            base.Draw(hovered, parentCropRec, scale, opacity);
        }
    }

    public class OriginsMenu : LayoutW, IListEntryFactory<OriginMap>
    {
        private readonly OrContext context;

        private readonly LabelW title;
        private readonly LabelW info;
        private readonly ScrollW scroll;
        private readonly ListW<OriginMap> list;

        public OriginsMenu(OrContext context) :
            base(EOrientation.VERTICAL)
        {
            this.context = context;
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

            AddChild(title, 80f);
            AddSpace(10f);
            AddChild(info, 40f);
            AddSpace(10f);
            AddChild(scroll, FILL);
        }

        public override void Draw(IWidget hovered, Rectangle parentCropRec, float scale, float opacity)
        {
            info.Text = "";
            if (Origins.Instance.Selected != ulong.MaxValue)
                info.Text = "Now please enter any official map!";
            
            base.Draw(hovered, parentCropRec, scale, opacity);
        }

        public IWidget Create(OriginMap elem, int i)
        {
            return new OriginMapEntry(context.Fonts.FontMedium, Map.ORIGINS_START + (ulong)i, () => context.ExitMenu(animation: false));
        }

        public IEnumerable<OriginMap> GetElems()
        {
            return Origins.Instance.Maps;
        }

        public float Height(OriginMap elem, int i)
        {
            return 40f;
        }
    }
}
