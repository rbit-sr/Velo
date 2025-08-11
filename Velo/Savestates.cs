using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.IO;

namespace Velo
{
    public interface ISavestateManager
    {
        void Save(string name);
        bool Load(string name);
        bool Undo();
        bool Rename(string oldName, string newName);
        bool Copy(string sourceName, string targetName);
        bool Delete(string name);
        IEnumerable<string> List();
        void Clear();
    }

    public class Savestates : ISavestateManager
    {
        private readonly Dictionary<string, Savestate> savestates = new Dictionary<string, Savestate>();
        private readonly Savestate undo = new Savestate();

        private readonly List<Action<string, Savestate>> onLoad = new List<Action<string, Savestate>>();

        public static Savestates Instance = new Savestates();

        public void AddOnLoad(Action<string, Savestate> onLoad)
        {
            this.onLoad.Add(onLoad);
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
                        using (FileStream stream = new FileStream($"Velo\\savestate\\{key}.srss", FileMode.Open, FileAccess.Read))
                        {
                            int size = stream.Read<int>();
                            byte[] buffer = StreamUtil.GetBuffer(size);
                            stream.ReadExactly(buffer, 0, size);
                            savestates[key].Stream.Position = 0;
                            savestates[key].Stream.Write(buffer, 0, size);
                        }
                    });
                }
            }
        }

        public void Save(string name)
        {
            if (!savestates.TryGetValue(name, out Savestate savestate) || savestate == null)
                savestates.Add(name, savestate = new Savestate());
            savestate.Save(OfflineGameMods.Instance.StoreActorTypes, Savestate.EListMode.INCLUDE);
            Task.Run(() =>
            {
                if (!Directory.Exists("Velo\\savestate"))
                    Directory.CreateDirectory("Velo\\savestate");
                using (FileStream stream = File.Create($"Velo\\savestate\\{name}.srss"))
                {
                    stream.Write((int)savestate.Stream.Length);
                    byte[] buffer = StreamUtil.GetBuffer((int)savestate.Stream.Length);
                    savestate.Stream.Position = 0;
                    savestate.Stream.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, buffer.Length);
                }
            });
        }

        public bool Load(string name)
        {
            if (!savestates.TryGetValue(name, out Savestate savestate) || savestate == null)
                return false;

            undo.Save(OfflineGameMods.Instance.StoreActorTypes, Savestate.EListMode.INCLUDE);
            if (savestate.Load(setGlobalTime: false))
                onLoad.ForEach(c => c(name, savestate));
            return true;
        }

        public bool Undo()
        {
            if (undo.Load(setGlobalTime: false))
                onLoad.ForEach(c => c("undo", undo));
            return true;
        }

        public bool Rename(string oldName, string newName)
        {
            if (!savestates.TryGetValue(oldName, out Savestate savestate) || savestate == null)
                return false;

            savestates.Remove(oldName);
            savestates.Add(newName, savestate);
            if (Directory.Exists("Velo\\savestate"))
            {
                File.Delete("Velo\\savestate\\" + newName + ".srss");
                File.Move("Velo\\savestate\\" + oldName + ".srss", "Velo\\savestate\\" + newName + ".srss");
            }
            return true;
        }

        public bool Copy(string sourceName, string targetName)
        {
            if (!savestates.TryGetValue(sourceName, out Savestate savestate) || savestate == null)
                return false;

            savestates.Add(targetName, savestate.Clone());

            if (Directory.Exists("Velo\\savestate"))
            {
                File.Delete("Velo\\savestate\\" + targetName + ".srss");
                File.Copy("Velo\\savestate\\" + sourceName + ".srss", "Velo\\savestate\\" + targetName + ".srss");
            }
            return true;
        }

        public bool Delete(string name)
        {
            if (Directory.Exists("Velo\\savestate"))
            {
                File.Delete("Velo\\savestate\\" + name + ".srss");
            }
            return savestates.Remove(name);
        }

        public IEnumerable<string> List()
        {
            return savestates.Keys;
        }

        public void Clear()
        {
            if (Directory.Exists("Velo\\savestate"))
            {
                foreach (string file in Directory.EnumerateFiles("Velo\\savestate"))
                {
                    if (file.EndsWith(".srss"))
                        File.Delete(file);
                }
            }
            savestates.Clear();
        }
    }
}
