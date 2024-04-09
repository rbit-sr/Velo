using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Velo
{
    public class SaveFile : Module
    {
        private static readonly string VERSION = "1.8.0";

        private readonly List<Setting> modified = new List<Setting>();
        private TimeSpan lastSave = TimeSpan.Zero;

        private SaveFile() : base("Save File")
        {
            ModuleManager.Instance.AddModifiedListener(modified.Add);
        }

        public static SaveFile Instance = new SaveFile();

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (modified.Count > 0 && new TimeSpan(DateTime.Now.Ticks) - lastSave >= TimeSpan.FromSeconds(1))
                SaveModified();
        }

        public void SaveModified()
        {
            List<Module> modules = new List<Module>(1);

            foreach (Setting setting in modified)
            {
                if (!modules.Contains(setting.Module))
                    modules.Add(setting.Module);
            }

            Save(modules);

            modified.Clear();
            lastSave = new TimeSpan(DateTime.Now.Ticks);
        }

        public void Save(List<Module> modules)
        {
            if (!Directory.Exists("Velo"))
                Directory.CreateDirectory("Velo");

            foreach (Module module in modules)
            {
                JsonElement settings = module.ToJson(EToJsonType.FOR_SAVE_FILE);
                (settings as JsonObject).AddString("Version", VERSION);
                File.WriteAllText("Velo\\" + module.Name + ".json", settings.ToString(true));
            }
        }

        public void Load()
        {
            if (!Directory.Exists("Velo"))
                Directory.CreateDirectory("Velo");

            List<Module> unvisited = ModuleManager.Instance.Modules.ToList();

            string[] files = Directory.GetFiles("Velo");
            foreach (string file in files)
            {
                if (!file.EndsWith(".json"))
                    continue;
                string moduleName = file.Substring(file.LastIndexOf('\\') + 1).Replace(".json", "");
                Module module = ModuleManager.Instance.Get(moduleName);
                if (module == null)
                    continue;
                JsonElement settings = JsonElement.FromString(File.ReadAllText(file));
                module.LoadSettings(settings);

                unvisited.Remove(module);
            }

            Save(unvisited);
        }
    }
}
