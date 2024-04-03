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
            ReloadKey = AddHotkey("reload key", 0x97);
            Contents = AddStringList("contents", new string[] { "Speedrunner", "Moonraker", "Cosmonaut" });
        }

        public static HotReload Instance = new HotReload();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Keyboard.Pressed[ReloadKey.Value])
                reloadContents();
        }

        public override void PostRender()
        {
            base.PostRender();

            contentsReloaded = false;
        }

        private void reloadContents()
        {
            contentsReloaded = true;
            
            foreach (string content in Contents.Value)
            {
                reloadContent(content);
            }
        }

        private void reloadContent(string id)
        {
            if (id.ToLower().EndsWith(".xnb"))
                id = id.Replace(".xnb", "");
            if (id.StartsWith("Content\\"))
                id = id.Replace("Content\\", "");

            if (!Directory.Exists("Content\\" + id) && !File.Exists("Content\\" + id + ".xnb"))
            {
                reloadContent("Content\\Characters\\" + id);
                return;
            }

            if (Directory.Exists("Content\\" + id))
            {
                string[] contents = Directory.GetFiles("Content\\" + id);
                foreach (string content in contents)
                    reloadContent(content);
                contents = Directory.GetDirectories("Content\\" + id);
                foreach (string content in contents)
                    reloadContent(content);
                return;
            }

            if (Velo.ContentManager != null)
            {
                foreach (var entry in Velo.ContentManager.dict)
                {
                    if (entry.Value == null) continue;
                    for (int i = 0; i < entry.Value.ContentCount; i++)
                    {
                        ICContent content = entry.Value.GetContent(i);
                        if (content == null || content.Name == null) continue;
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
}
