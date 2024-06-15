using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Steamworks;
using System;
using System.Diagnostics;
using System.IO;

namespace Velo
{
    public class AutoUpdate : Module
    {
        private readonly RequestHandler handler;

        private VeloUpdate update;
        private bool updateAvailable = false;
        private bool updateRefused = false;

        private CachedFont font;
        private bool initialized = false;

        private WidgetContainer container;

        public AutoUpdate() : base("Auto Update")
        {
            handler = new RequestHandler();
        }

        public static AutoUpdate Instance = new AutoUpdate();

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
            }, onSuccessPreUpdate: false);
            handler.Run();
        }

        public void Initialize()
        {
            FontCache.Get(ref font, "UI\\Font\\NotoSans-Regular.ttf:24");

            LabelW popupText = new LabelW("A new version of Velo is available! (" + update.VersionName + ")\nDo you want to install now?\n(This will automatically restart your game.)", font.Font);
            Style.ApplyText(popupText);

            LabelW yesButton = new LabelW("Yes", font.Font);
            Style.ApplyButton(yesButton);
            yesButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    string[] args = Environment.GetCommandLineArgs();
                    Main.game.Dispose();
                    SteamAPI.Shutdown();
                    Process.Start("VeloUpdater.exe", "Velo\\update\\" + update.Filename + " " + string.Join(" ", args));
                    Environment.Exit(0);
                }
            };

            LabelW noButton = new LabelW("No", font.Font);
            Style.ApplyButton(noButton);
            noButton.OnClick = wevent =>
            {
                if (wevent.Button == WEMouseClick.EButton.LEFT)
                {
                    updateRefused = true;
                    Velo.DisableCursor(this);
                }
            };

            LayoutW buttonRow = new LayoutW(LayoutW.EOrientation.HORIZONTAL);
            buttonRow.AddSpace(LayoutW.FILL);
            buttonRow.AddChild(yesButton, 120f);
            buttonRow.AddSpace(10f);
            buttonRow.AddChild(noButton, 120f);
            buttonRow.AddSpace(10f);

            LayoutW popupLayout = new LayoutW(LayoutW.EOrientation.VERTICAL)
            {
                BackgroundVisible = true,
                BackgroundColor = () => new Color(20, 20, 20, 150)
            };
            popupLayout.AddSpace(10f);
            popupLayout.AddChild(popupText, LayoutW.FILL);
            popupLayout.AddSpace(10f);
            popupLayout.AddChild(buttonRow, 45f);
            popupLayout.AddSpace(10f);

            StackW stack = new StackW
            {
                BackgroundVisible = true,
                BackgroundColor = () => new Color(0, 0, 0, 127)
            };
            stack.AddChild(popupLayout, new Vector2(610, 415), new Vector2(700, 250));
            container = new WidgetContainer(stack, new Rectangle(0, 0, 1920, 1080));

            initialized = true;
        }

        public bool Enabled { get { return updateAvailable && !updateRefused; } }

        public override void Init()
        {
            base.Init();

            Check();
        }

        public override void PostRender()
        {
            base.PostRender();

            if (!Enabled)
                return;

            if (!initialized)
            {
                Initialize();
                initialized = true;
                Velo.EnableCursor(this);
            }

            container.Draw();
        }
    }
}
