using System.IO;
using CEngine.Content;

namespace Velo
{
    public class HotReload : Module
    {
        public HotkeySetting ReloadKey;
        public StringListSetting Contents;

        public bool contentsReloaded = false;
        
        private HotReload() : base("Hot Reload")
        {
            NewCategory("general");
            ReloadKey = AddHotkey("reload key", 0x97);
            Contents = AddStringList("contents", new string[] { "Speedrunner", "Moonraker", "Cosmonaut" });

            CurrentCategory.Tooltip =
                "Allows you to reload sprite and sound files while ingame.";

            Contents.Tooltip =
                "Provide a list of content files or directories you want to reload. " +
                "For characters, you can just use the folder name in the \"Characters\" folder." +
                "Otherwise, use the file's path, starting in the \"Content\" folder, " +
                "like \"UI\\MultiplayerHUD\\PowerUp\".\n" +
                "Refreshing some specific contents may crash the game.";
        }

        public static HotReload Instance = new HotReload();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Keyboard.Pressed[ReloadKey.Value])
                ReloadContents();
        }

        public override void PostRender()
        {
            base.PostRender();

            contentsReloaded = false;
        }

        private void ReloadContents()
        {
            contentsReloaded = true;
            
            foreach (string content in Contents.Value)
            {
                ReloadContent(content);
            }
        }

        private void ReloadContent(string id)
        {
            if (id.EndsWith(".xnb"))
                id = id.Replace(".xnb", "");
            if (id.StartsWith("Content\\"))
                id = id.Replace("Content\\", "");

            if (!Directory.Exists("Content\\" + id) && !File.Exists("Content\\" + id + ".xnb"))
            {
                ReloadContent("Content\\Characters\\" + id);
                return;
            }

            if (Directory.Exists("Content\\" + id))
            {
                string[] contents = Directory.GetFiles("Content\\" + id);
                foreach (string content in contents)
                    ReloadContent(content);
                contents = Directory.GetDirectories("Content\\" + id);
                foreach (string content in contents)
                    ReloadContent(content);
                return;
            }

            if (Velo.ContentManager == null)
                return;

            foreach (var entry in Velo.ContentManager.dict)
            {
                if (entry.Value == null) 
                    continue;
                for (int i = 0; i < entry.Value.ContentCount; i++)
                {
                    ICContent content = entry.Value.GetContent(i);
                    if (content == null || content.Name == null) 
                        continue;
                    if (content.Name.ToLower() == id.ToLower())
                    {
                        Velo.ContentManager.Release(content);
                        Velo.ContentManager.Load(content, false);
                        return;
                    }
                }

            }
        }
    }
}
