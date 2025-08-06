using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Velo.Frame;

namespace Velo
{
    public class Timeline : IReplayable
    {
        private readonly static Savestate buffer = new Savestate();

        public List<Frame> Frames = new List<Frame>();
        public List<StatsTracker> Stats = new List<StatsTracker>();
        public SavestateStack Savestates = new SavestateStack();
        public int Position = -1;
        public int LapStart { get; set; }
        public int PlayerCount = 1;
        private RunInfo info;
        public RunInfo Info
        {
            get => info;
            set { info = value; }
        }

        public Timeline(RunInfo info)
        {
            Info = info;
        }

        public void Write(Frame frame, StatsTracker stats, Savestate savestate)
        {
            if (Position < LapStart)
                LapStart = 0;
            Position++;
            Frames.RemoveRange(Position, Frames.Count - Position);
            Frames.Add(frame);
            Stats.RemoveRange(Position, Stats.Count - Position);
            Stats.Add(stats);
            Savestates.Set(Position, savestate);
        }

        public Frame this[int frame]
        {
            get => Frames[frame];
            set
            {
                Frames[frame] = value;
            }
        }
            
        public bool LoadSavestate(int frame, bool setGlobalTime, int ghostIndex = -1, Savestate.ResultFlags resultFlags = null)
        {
            Savestates.Get(frame, buffer);
            return buffer.Load(setGlobalTime, ghostIndex, resultFlags);
        }

        public int Count => Frames.Count;
     
        public Frame GetFrame()
        {
            return Frames[Position];
        }

        public void SetFrame(Frame frame)
        {
            Frames[Position] = frame;
        }

        public StatsTracker GetStats()
        {
            return Stats[Position];
        }

        public void SetStats(StatsTracker stats)
        {
            Stats[Position] = stats;
        }

        public Timeline ShallowClone()
        {
            return new Timeline(info)
            {
                Frames = Frames.ToList(),
                Stats = Stats.ToList(),
                Savestates = Savestates.ShallowClone(),
                Position = Position,
                LapStart = LapStart,
                PlayerCount = PlayerCount
            };
        }

        public void Write(Stream stream, Dictionary<Savestate, int> savestateLookup, ref int nextId)
        {
            stream.Write(Count);
            stream.Write(Position);
            stream.Write(LapStart);
            stream.Write(PlayerCount);
            stream.Write(info);
            foreach (Frame frame in Frames)
                stream.Write(frame);
            foreach (StatsTracker stats in Stats)
                stream.Write(stats);

            for (int i = 0; i < Count; i++)
            {
                SavestateStack.Slot slot = Savestates.GetCompressed(i);
                stream.Write(slot.UncompressedSize);
                stream.Write(slot.KeySavestate);
                stream.Write(slot.LossSum);

                if (savestateLookup.TryGetValue(slot.Savestate, out int index))
                {
                    stream.Write(index);
                }
                else
                {
                    stream.Write(~nextId);
                    savestateLookup.Add(slot.Savestate, nextId);
                    nextId++;

                    stream.Write((int)slot.Savestate.Stream.Length);
                    slot.Savestate.Stream.Position = 0;
                    slot.Savestate.Stream.CopyTo(stream);
                }
            }
        }

        public void Read(Stream stream, Dictionary<int, Savestate> savestateLookup)
        {
            int count = stream.Read<int>();
            Position = stream.Read<int>();
            LapStart = stream.Read<int>();
            PlayerCount = stream.Read<int>();
            info = stream.Read<RunInfo>();

            Frames.Clear();
            for (int i = 0; i < count; i++)
                Frames.Add(stream.Read<Frame>());

            Stats.Clear();
            for (int i = 0; i < count; i++)
                Stats.Add(stream.Read<StatsTracker>());

            Savestates.Clear();
            for (int i = 0; i < count; i++)
            {
                SavestateStack.Slot slot = new SavestateStack.Slot
                {
                    UncompressedSize = stream.Read<int>(),
                    KeySavestate = stream.Read<int>(),
                    LossSum = stream.Read<int>()
                };

                int id = stream.Read<int>();
                if (id >= 0)
                {
                    slot.Savestate = savestateLookup[id];
                }
                else
                {
                    slot.Savestate = new Savestate();
                    int size = stream.Read<int>();
                    byte[] buffer = StreamUtil.GetBuffer(size);
                    stream.Read(buffer, 0, size);
                    slot.Savestate.Stream.Position = 0;
                    slot.Savestate.Stream.Write(buffer, 0, size);

                    savestateLookup.Add(~id, slot.Savestate);
                }

                Savestates.SetCompressed(i, slot);
            }
        }
    }

    public class TASProject
    {
        public static readonly int VERSION = 0;

        public string Name;
        public Timeline Main => Timelines["main"];
        public Dictionary<string, Timeline> Timelines = new Dictionary<string, Timeline>();
        private RunInfo infoTemplate;
        public int Rerecords = 0;

        public TASProject(string name)
        {
            Name = name;
            Clear();
        }

        public void Clear()
        {
            Timelines.Clear();
            infoTemplate.Id = -1;
            infoTemplate.PlayerId = Steamworks.SteamUser.GetSteamID().m_SteamID;
            infoTemplate.Category.MapId = Map.GetCurrentMapId();
            infoTemplate.Category.TypeId = (long)ECategoryType.NEW_LAP;
            infoTemplate.RunTime = 0;
            infoTemplate.CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (OfflineGameMods.Instance.GetGrappleCooldown() == 0.2f)
                infoTemplate.PhysicsFlags |= RunInfo.FLAG_NEW_GCD;
            if (OfflineGameMods.Instance.GetFixBounceGlitch())
                infoTemplate.PhysicsFlags |= RunInfo.FLAG_FIX_BOUNCE_GLITCH;
            Timelines.Add("main", new Timeline(infoTemplate));
        }

        public void Write(Stream stream, bool compress)
        {
            long initPosition = stream.Position;
            stream.Write(VERSION | (compress ? 0 : 1 << 31));

            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            Stream raw = compress ? new MemoryStream() : stream;

            raw.WriteStr(Name);
            raw.Write(infoTemplate);
            raw.Write(Rerecords);

            Dictionary<Savestate, int> savestateLookup = new Dictionary<Savestate, int>();
            int nextId = 0;

            raw.Write(Timelines.Count);

            foreach (KeyValuePair<string, Timeline> pair in Timelines)
            {
                raw.WriteStr(pair.Key);
                pair.Value.Write(raw, savestateLookup, ref nextId);
            }

            if (compress)
            {
                encoder.WriteCoderProperties(stream);
                stream.Write(BitConverter.GetBytes((int)raw.Length), 0, sizeof(int));
                try
                {
                    raw.Position = 0;
                    encoder.Code(raw, stream, raw.Length, -1, null);
                }
                catch (Exception)
                {
                    raw.Close();
                    stream.Position = initPosition;
                    Write(stream, compress: false);
                    return;
                }
                raw.Close();
            }
        }

        public void Read(Stream stream)
        {
            int versionInt = stream.Read<int>();
            bool compressed = (versionInt & (1 << 31)) == 0;
            versionInt &= ~(1 << 31);

            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
            if (compressed)
            {
                byte[] properties = new byte[5];
                stream.ReadExactly(properties, 0, properties.Length);
                decoder.SetDecoderProperties(properties);
            }

            Stream raw = compressed ? new MemoryStream() : stream;

            if (compressed)
            {
                int dataSize = stream.Read<int>();

                decoder.Code(stream, raw, stream.Length, dataSize, null);
                raw.Position = 0;
            }

            Name = raw.ReadStr();
            infoTemplate = raw.Read<RunInfo>();
            Rerecords = raw.Read<int>();

            Dictionary<int, Savestate> savestateLookup = new Dictionary<int, Savestate>();

            int timelinesCount = raw.Read<int>();
            Timelines.Clear();
            for (int i = 0; i < timelinesCount; i++)
            {
                string name = raw.ReadStr();
                Timeline timeline = new Timeline(default);
                timeline.Read(raw, savestateLookup);
                Timelines.Add(name, timeline);
            }

            if (compressed)
                raw.Close();
        }

        public static TASProject Load(string name)
        {
            string filename = $"Velo\\TASprojects\\{name}.srtas";
            if (!File.Exists(filename))
                return null;
            TASProject tasProject = new TASProject(name);

            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                tasProject.Read(stream);
            }
            return tasProject;
        }

    }

    public class TASRecorder : IRecorder, ISavestateManager
    {
        private TASProject project;
        private Timeline undo;
        private Savestate savestatePreStart;
        private bool rewindedOrLoadedSavestate = false;
        private TimeSpan lastSaveRemind = TimeSpan.Zero;
        public bool NeedsSave = false;

        private readonly List<Action<string, Timeline>> onLoad = new List<Action<string, Timeline>>();

        public TimeSpan Time => project.Main.GetFrame().DeltaSum - project.Main[project.Main.LapStart].DeltaSum;
        public int Frame => project.Main.Position - project.Main.LapStart;

        private bool running = false;
        public bool Running => running;
        public bool DtFixed => true;

        public TASRecorder()
        {

        }

        public void AddOnLoad(Action<string, Timeline> onLoad)
        {
            this.onLoad.Add(onLoad);
        }

        public void Set(TASProject project)
        {
            this.project = project;
            lastSaveRemind = Velo.RealTime;
        }

        public void Resume()
        {
            if (savestatePreStart == null)
            {
                savestatePreStart = new Savestate();
                savestatePreStart.Save(new List<Savestate.ActorType>(), Savestate.EListMode.EXCLUDE);
            }
            running = true;
            Timeline main = project.Main;
            if (main.Count == 0)
                Capture();
            else
                main.LoadSavestate(main.Position, true);
            OfflineGameMods.Instance.Pause();
            Velo.AddOnPreUpdate(() => ConsoleM.Instance.AppendLine("You are now in recording mode!"));
        }

        public void Stop()
        {
            running = false;
        }

        public void Close()
        {
            if (NeedsSave)
            {
                Save(false, recover: true);
            }

            Stop();
            project = null;
            OfflineGameMods.Instance.Unpause();
            savestatePreStart.Load(true);
        }

        private void Capture()
        {
            Savestate savestate = new Savestate();
            savestate.Save(new List<Savestate.ActorType> { Savestate.ATAIVolume }, Savestate.EListMode.EXCLUDE);

            Timeline main = project.Main;

            Frame frame = new Frame(
                   player: Velo.MainPlayer,
                   realTime: Velo.RealTime,
                   gameDelta: TimeSpan.Zero, // to be filled in next PostUpdate
                   gameTime: TimeSpan.Zero, // to be filled in next PostUpdate
                   deltaSum: main.Position >= 0 ? main[main.Position].DeltaSum + main[main.Position].Delta : TimeSpan.Zero
               );
            frame.SetInputs(Velo.MainPlayer);

            StatsTracker stats = default;
            if (main.Position < 0)
                stats.Init(Velo.MainPlayer);
            else
            {
                stats = main.GetStats();
                stats.Update(Velo.MainPlayer, main[main.Position]);
            }

            main.Write(
                frame,
                stats,
                savestate
            );

            if (rewindedOrLoadedSavestate)
            {
                project.Rerecords++;
                rewindedOrLoadedSavestate = false;
            }

            RunInfo info = main.Info;
            stats.Apply(ref info, main.Count - main.LapStart);
            main.Info = info;

            NeedsSave = true;
        }

        public void PreUpdate()
        {
            if (Velo.RealTime - lastSaveRemind >= TimeSpan.FromMinutes(OfflineGameMods.Instance.SaveReminderInterval.Value))
            {
                lastSaveRemind = Velo.RealTime;
                ConsoleM.Instance.AppendLine("Remember to save your TAS-project!");
            }
        }

        public void PostUpdate()
        {
            if (Velo.MainPlayer == null || Velo.MainPlayer.destroyed)
                return;
            if (project == null)
                return;

            Timeline main = project.Main;

            Frame frame = main.GetFrame();
            frame.Delta = Velo.GameDelta;
            frame.Time = Velo.GameTime;
            main.SetFrame(frame);

            Capture();
        }

        public void JumpToTime(TimeSpan newTime)
        {
            Timeline main = project.Main;
            ref int i = ref main.Position;

            newTime += main[main.LapStart].DeltaSum;

            if (newTime < TimeSpan.Zero)
            {
                i = 0;
            }
            else if (newTime < Time)
            {
                while (main.Frames[i].DeltaSum >= newTime)
                {
                    i--;
                    if (i == 0)
                        break;
                }
            }
            else if (newTime > Time)
            {
                while (main.Frames[i].DeltaSum + main.Frames[i].Delta < newTime)
                {
                    i++;
                    if (i >= main.Count - 1)
                        break;
                }
            }
            main.LoadSavestate(i, true);
            rewindedOrLoadedSavestate = true;
        }

        public void JumpToFrame(int frame)
        {
            Timeline main = project.Main;
            frame += main.LapStart;
            main.Position = frame;
            if (main.Position >= main.Count)
                main.Position = main.Count - 1;
            if (main.Position < 0)
                main.Position = 0;
            project.Main.LoadSavestate(main.Position, true);
            rewindedOrLoadedSavestate = true;
        }

        public void MainPlayerReset()
        {
            Timeline main = project.Main;
            Frame frame = main.GetFrame();
            frame.SetFlag(EFlag.RESET_LAP, true);
            main.SetFrame(frame);
            if (main.LapStart == 0)
                main.LapStart = main.Position;
        }

        public void LapFinish(float time)
        {
            Timeline main = project.Main;
            if (main.LapStart == 0)
            {
                main.LapStart = main.Position;
                RunInfo info = main.Info;
                info.RunTime = (int)(time * 1000f);
                main.Info = info;
            }
        }

        public void Save(string name)
        {
            if (name == "main")
                return;
            project.Timelines[name] = project.Main.ShallowClone();
        }

        public bool Load(string name)
        {
            if (!project.Timelines.TryGetValue(name, out Timeline timeline) || timeline == null)
                return false;

            undo = project.Timelines["main"];
            project.Timelines["main"] = timeline.ShallowClone();
            project.Main.LoadSavestate(project.Main.Position, true);
            rewindedOrLoadedSavestate = true;
            onLoad.ForEach(c => c(name, timeline));
            return true;
        }

        public bool Undo()
        {
            project.Timelines["main"] = undo.ShallowClone();
            project.Main.LoadSavestate(project.Main.Position, true);
            rewindedOrLoadedSavestate = true;
            onLoad.ForEach(c => c("undo", undo));
            return true;
        }

        public bool Rename(string oldName, string newName)
        {
            if (oldName == "main")
                return false;
            if (!project.Timelines.TryGetValue(oldName, out Timeline track) || track == null)
                return false;

            project.Timelines.Remove(oldName);
            project.Timelines[newName] = track;
            return true;
        }

        public bool Copy(string sourceName, string targetName)
        {
            if (!project.Timelines.TryGetValue(sourceName, out Timeline track) || track == null)
                return false;

            project.Timelines.Add(targetName, track.ShallowClone());
            return true;
        }

        public bool Delete(string name)
        {
            if (name == "main")
                return false;
            return project.Timelines.Remove(name);
        }

        public IEnumerable<string> List()
        {
            return project.Timelines.Keys;
        }

        public void Clear()
        {
            List<string> toRemove = project.Timelines.Keys.Where(n => n != "main").ToList();
            foreach (string name in toRemove)
                project.Timelines.Remove(name);
        }

        public void Save(bool compress, bool recover = false)
        {
            string recoverStr = recover ? "\\backup" : "";

            if (!Directory.Exists($"Velo\\TASprojects{recoverStr}"))
                Directory.CreateDirectory($"Velo\\TASprojects{recoverStr}");

            using (FileStream stream = File.Create($"Velo\\TASprojects{recoverStr}\\{project.Name}.srtas"))
            {
                project.Write(stream, compress);
            }
            lastSaveRemind = Velo.RealTime;
            NeedsSave = false;
        }
    }
}