using Microsoft.Xna.Framework;
using Steamworks;
using System;
using System.Diagnostics;
using System.IO;

namespace Velo
{
    public class AutoUpdate : MenuModule
    {
        private readonly RequestHandler handler;

        private VeloUpdate update;
        private bool updateAvailable = false;

        public AutoUpdate() :
            base(
                "Auto Update",
                addEnabledSetting: false,
                menuPos: new Vector2(610f, 415f),
                menuSize: new Vector2(700f, 250f)
                )
        {
            handler = new RequestHandler();
        }

        public static AutoUpdate Instance = new AutoUpdate();
        
        public void Refuse()
        {
            Enabled.Disable();
        }

        public void Check()
        {
            if (updateAvailable || handler.Status == ERequestStatus.PENDING)
                return;

            handler.Push(new CheckUpdateRequest(), update =>
            {
                try
                {
                    if (!Directory.Exists("Velo\\update"))
                        Directory.CreateDirectory("Velo\\update");

                    using (FileStream file = new FileStream("Velo\\update\\" + update.Filename, FileMode.OpenOrCreate))
                    {
                        file.Write(update.Bytes, 0, update.Bytes.Length);
                    }
                }
                catch (Exception)
                {
                    return;
                }
                this.update = update;
                updateAvailable = true;
                Enabled.Enable();
            }, onSuccessPreUpdate: true);
            handler.Run();
        }

        public override void Init()
        {
            base.Init();

            Check();
        }

        public override Menu GetStartMenu()
        {
            return new AutoUpdateWindow(this, update);
        }
    }

    public class AutoUpdateWindow : Menu
    {
        private readonly LabelW popupText;
        private readonly LabelW yesButton;
        private readonly LabelW noButton;
        private readonly LayoutW buttonRow;
        private readonly LayoutW popupLayout;

        public AutoUpdateWindow(MenuModule module, VeloUpdate update) :
            base(module)
        {
            popupText = new LabelW("A new version of Velo is available! (" + update.VersionName + ")\nDo you want to install now?\n(This will automatically restart your game.)", module.Fonts.FontLarge);
            popupText = new LabelW("A new version of Velo is available! (" + update.VersionName + ")\nDo you want to install now?\n(This will automatically restart your game.)", module.Fonts.FontLarge);
            Style.ApplyText(popupText);

            yesButton = new LabelW("Yes", module.Fonts.FontLarge);
            Style.ApplyButton(yesButton);
            yesButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    string[] args = Environment.GetCommandLineArgs();
                    Main.game.Dispose();
                    SteamAPI.Shutdown();
                    Process.Start("VeloUpdater2.exe", "Velo\\update\\" + update.Filename + " " + string.Join(" ", args));
                    Environment.Exit(0);
                }
            };

            noButton = new LabelW("No", module.Fonts.FontLarge);
            Style.ApplyButton(noButton);
            noButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    AutoUpdate.Instance.Refuse();
                }
            };

            buttonRow = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
            buttonRow.AddSpace(LayoutW.FILL);
            buttonRow.AddChild(yesButton, 120f);
            buttonRow.AddSpace(10f);
            buttonRow.AddChild(noButton, 120f);
            buttonRow.AddSpace(10f);

            popupLayout = new LayoutW(LayoutW.EOrientation.VERTICAL)
            {
                BackgroundVisible = true,
                BackgroundColor = () => new Color(20, 20, 20, 150)
            };
            popupLayout.AddSpace(10f);
            popupLayout.AddChild(popupText, LayoutW.FILL);
            popupLayout.AddSpace(10f);
            popupLayout.AddChild(buttonRow, 45f);
            popupLayout.AddSpace(10f);

            Child = popupLayout;
        }

        public override void Refresh()
        {

        }
    }
}
