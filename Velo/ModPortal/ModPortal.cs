﻿using Microsoft.Xna.Framework;
using System;
using System.Windows.Forms;

namespace Velo
{
    public class MpContext : MenuContext
    {
        public BackstackW<IMpWidget> Page;
        public LabelW VersionText;
        public ImageW LoadingSymbol;
        public LabelW ErrorMessage;

        public string Error;

        public MpContext(ToggleSetting enabled)
            : base(enabled, enableDim: true)
        {
            Page = new BackstackW<IMpWidget>();
            VersionText = new LabelW(Version.VERSION_NAME + " - " + Version.AUTHOR, Fonts.FontSmall);
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

            AddElem(Page, PAGE_POS, PAGE_SIZE);
            AddElem(VersionText, new Vector2(20f, 1035f), new Vector2(180f, 25f));
            AddElem(LoadingSymbol, PAGE_POS + new Vector2(PAGE_SIZE.X + 8f, PAGE_SIZE.Y - (LoadSymbol.SIZE + BOTTOM_ROW_HEIGHT) / 2), new Vector2(LoadSymbol.SIZE, LoadSymbol.SIZE));
            AddElem(ErrorMessage, PAGE_POS + new Vector2(PAGE_SIZE.X + 8f, PAGE_SIZE.Y - 35f / 2 - ERROR_MESSAGE_SIZE.Y / 2), ERROR_MESSAGE_SIZE);
        }

        public override void EnterMenu()
        {
            Page.TransitionTo(new MpBrowsePage(this), 8f, Vector2.Zero);

            base.EnterMenu();

            Request();
        }

        public override void ExitMenu(bool animation = true)
        {
            base.ExitMenu(animation);

            Page.Clear(); // clear the backstack
        }

        public void Request()
        {
            // ModsDatabase.Instance.CancelAll();
            Page.Child.PushRequests();
            // ModsDatabase.Instance.RunRequestMods(Refresh, error => Error = error.message);
        }

        public override void Draw()
        {
            base.Draw();

            if (/*ModsDatabase.Instance.Pending()*/ false)
            {
                LoadingSymbol.Visible = true;
                ErrorMessage.Text = "";
            }
            else
            {
                LoadingSymbol.Visible = false;
                LoadingSymbol.Rotation = (float)Math.PI / 2f;
            }
            if (Error != "" && Error != null)
                ErrorMessage.Text = Util.LineBreaks("Error: " + Error, 30);
        }
    }

    public class ModPortal : ToggleModule
    {
        public BoolSetting ExampleSetting1;
        public FloatSetting ExampleSetting2;

        private MpContext context;

        private ModPortal() : base("Mod Portal")
        {
            Enabled.SetValueAndDefault(new Toggle((ushort)Keys.F3));

            NewCategory("Category 1");
            ExampleSetting1 = AddBool("example 1", true);

            ExampleSetting1.Tooltip = "Tooltip";

            NewCategory("Category 2");
            ExampleSetting2 = AddFloat("example 2", 3f, 1f, 5f);
        }

        public static ModPortal Instance = new ModPortal();

        public override void Init()
        {
            base.Init();

            context = new MpContext(Enabled);
        }

        public override void PreUpdate()
        {
            base.PreUpdate();
        }

        public override void PostRender()
        {
            base.PostRender();

            context.Draw();
        }
    }
}
