using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Velo
{
    public class SaveFile : Module
    {
        private SaveFile() : base("Save File")
        {

        }

        public static SaveFile Instance = new SaveFile();

        public override void PreRender()
        {
            base.PreRender();

            if (Keyboard.Pressed[(ushort)Keys.F10])
                Save();
            if (Keyboard.Pressed[(ushort)Keys.F11])
                Load();
        }

        private void Save()
        {
            JsonElement settings = ModuleManager.Instance.ToJson(true);
            File.WriteAllText("settings.json", settings.ToString(true));
        }

        private void Load()
        {
            JsonElement settings = JsonElement.FromString(File.ReadAllText("settings.json"));
            ModuleManager.Instance.LoadSettings(settings);
        }
    }
}
