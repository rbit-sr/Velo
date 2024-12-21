using Microsoft.Xna.Framework;
using Steamworks;
using System;
using System.Diagnostics;
using System.IO;

namespace Velo
{
    public class AuContext : MenuContext
    {
        public HolderW<AutoUpdateWindow> Window;

        public AuContext(ToggleSetting enabled) 
            : base(enabled, enableDim: true)
        {
            Window = new HolderW<AutoUpdateWindow>();
            AddElem(Window, new Vector2(610f, 415f), new Vector2(700f, 250f));
        }

        public void Show(VeloUpdate update)
        {
            Window.Child = new AutoUpdateWindow(this, update);
            Enabled.Enable();
        }
    }

    public class AutoUpdate : ToggleModule
    {
        private readonly RequestHandler handler;

        private bool updateAvailable = false;

        private AuContext context;

        public AutoUpdate() :
            base(
                "Auto Update",
                addEnabledSetting: false
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
                updateAvailable = true;
                context.Show(update);
            }, onSuccessPreUpdate: true);
            handler.Run();
        }

        public override void Init()
        {
            base.Init();

            context = new AuContext(Enabled);

            Check();
        }

        public override void PostRender()
        {
            base.PostRender();

            context.Draw();
        }
    }

    public class AutoUpdateWindow : LayoutW
    {
        private readonly LabelW popupText;
        private readonly LabelW yesButton;
        private readonly LabelW noButton;
        private readonly LayoutW buttonRow;

        public AutoUpdateWindow(AuContext context, VeloUpdate update) :
            base(EOrientation.VERTICAL)
        {
            popupText = new LabelW("A new version of Velo is available! (" + update.VersionName + ")\nDo you want to install now?\n(This will automatically restart your game.)", context.Fonts.FontLarge);
            Style.ApplyText(popupText);

            yesButton = new LabelW("Yes", context.Fonts.FontLarge);
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

            noButton = new LabelW("No", context.Fonts.FontLarge);
            Style.ApplyButton(noButton);
            noButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    AutoUpdate.Instance.Refuse();
                }
            };

            buttonRow = new LayoutW(EOrientation.HORIZONTAL);
            buttonRow.AddSpace(FILL);
            buttonRow.AddChild(yesButton, 120f);
            buttonRow.AddSpace(10f);
            buttonRow.AddChild(noButton, 120f);
            buttonRow.AddSpace(10f);

            BackgroundVisible = true;
            BackgroundColor = () => new Color(20, 20, 20, 150);
            AddSpace(10f);
            AddChild(popupText, FILL);
            AddSpace(10f);
            AddChild(buttonRow, 45f);
            AddSpace(10f);
        }
    }
}
