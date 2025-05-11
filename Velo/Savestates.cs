using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.IO;

namespace Velo
{
    public class Savestates
    {
        private readonly OfflineGameMods instance;

        private readonly Dictionary<string, Savestate> savestates = new Dictionary<string, Savestate>();

        private readonly Action<Savestate> onSave;
        private readonly Action<Savestate> onLoad;

        public Savestates(OfflineGameMods instance, Action<Savestate> onSave, Action<Savestate> onLoad)
        {
            this.instance = instance;
            this.onSave = onSave;
            this.onLoad = onLoad;
        }

        public void Init()
        {
            if (Directory.Exists("Velo\\savestate"))
            {
                string[] files = Directory.GetFiles("Velo\\savestate");
                foreach (string file in files)
                {
                    string key = Path.GetFileNameWithoutExtension(file);

                    Task.Run(() =>
                    {
                        if (!savestates.ContainsKey(key))
                            savestates.Add(key, new Savestate());
                        using (FileStream stream = new FileStream("Velo\\savestate\\" + key + ".srss", FileMode.Open, FileAccess.Read))
                        {
                            int size = stream.Read<int>();
                            byte[] buffer = new byte[size];
                            stream.ReadExactly(buffer, 0, size);
                            savestates[key].Stream.Position = 0;
                            savestates[key].Stream.Write(buffer, 0, size);
                        }
                    });
                }
            }
        }

        public void PreUpdate()
        {
            bool savedAny = false;
            for (int i = 0; i < 10; i++)
            {
                string key = "ss" + (i + 1);

                if (instance.SaveKeys[i].Pressed())
                {
                    if (!savestates.ContainsKey(key))
                        savestates.Add(key, new Savestate());
                    if (instance.StoreAIVolumes.Value)
                        savestates[key].Save(new List<Savestate.ActorType> { Savestate.ATAIVolume }, Savestate.EListMode.EXCLUDE);
                    else
                        savestates[key].Save(new List<Savestate.ActorType> { }, Savestate.EListMode.EXCLUDE);
                    onSave?.Invoke(savestates[key]);
                    Task.Run(() =>
                    {
                        if (!Directory.Exists("Velo\\savestate"))
                            Directory.CreateDirectory("Velo\\savestate");
                        using (FileStream stream = new FileStream("Velo\\savestate\\" + key + ".srss", FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            stream.Write((int)savestates[key].Stream.Length);
                            byte[] buffer = new byte[savestates[key].Stream.Length];
                            savestates[key].Stream.Position = 0;
                            savestates[key].Stream.Read(buffer, 0, buffer.Length);
                            stream.Write(buffer, 0, buffer.Length);
                        }
                    });
                    savedAny = true;
                }
            }

            if (savedAny)
                return;

            for (int i = 0; i < 10; i++)
            {
                string key = "ss" + (i + 1);

                if (instance.LoadKeys[i].Pressed() && savestates.ContainsKey(key))
                {
                    Savestate savestate = savestates[key];

                    if (savestate.Load(setGlobalTime: false))
                        onLoad?.Invoke(savestate);
                }
            }
        }
    }
}
