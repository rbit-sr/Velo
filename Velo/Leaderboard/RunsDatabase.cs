using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Velo
{
    public struct RunInfo
    {
        public ulong PlayerId;
        public long CreateTime;

        public int Id;
        public int MapId;
        public int RunTime;
        public int Category;
        public int WasWR;

        public int Padding;

        public static void GetBytes(RunInfo info, byte[] bytes)
        {
            int off = 0;
            Bytes.Write(info.PlayerId, bytes, ref off);
            Bytes.Write(info.CreateTime, bytes, ref off);
            Bytes.Write(info.Id, bytes, ref off);
            Bytes.Write(info.MapId, bytes, ref off);
            Bytes.Write(info.RunTime, bytes, ref off);
            Bytes.Write(info.Category, bytes, ref off);
            Bytes.Write(info.WasWR, bytes, ref off);
        }

        public static RunInfo FromBytes(byte[] bytes)
        {
            RunInfo info = new RunInfo();
            int off = 0;
            Bytes.Read(ref info.PlayerId, bytes, ref off);
            Bytes.Read(ref info.CreateTime, bytes, ref off);
            Bytes.Read(ref info.Id, bytes, ref off);
            Bytes.Read(ref info.MapId, bytes, ref off);
            Bytes.Read(ref info.RunTime, bytes, ref off);
            Bytes.Read(ref info.Category, bytes, ref off);
            Bytes.Read(ref info.WasWR, bytes, ref off);
            return info;
        }

        public int CompareTo(RunInfo other)
        {
            int cmp = RunTime.CompareTo(other.RunTime);
            if (cmp != 0)
                return cmp;

            return Id.CompareTo(other.Id);
        }
    }

    public enum ECategory : int
    {
        NEW_LAP, ONE_LAP, NEW_LAP_SKIPS, ONE_LAP_SKIPS, COUNT
    }

    public class RunsDatabase : Module, IComparer<int>
    {
        public RequestHandler<List<RunInfo>>[] GetRunHandlers = new RequestHandler<List<RunInfo>>[]
            {
                new RequestHandler<List<RunInfo>>(),
                new RequestHandler<List<RunInfo>>(),
                new RequestHandler<List<RunInfo>>()
            };
        private Action[] onGetRunSuccess = new Action[3];

        public RequestHandler<Recording> GetRecordingHandler = new RequestHandler<Recording>();
        private Action<Recording> onGetRecordingSuccess;

        private Dictionary<int, RunInfo> allRuns = new Dictionary<int,RunInfo>();
        private List<int>[,] runsPerMapCat = new List<int>[Map.COUNT, (int)ECategory.COUNT];
        private List<int> pendingRuns = new List<int>();

        private bool initRequest = false;

        public RunsDatabase() : base("Runs Database")
        {

        }

        public static RunsDatabase Instance = new RunsDatabase();

        private void Add(RunInfo info)
        {
            if (!allRuns.ContainsKey(info.Id))
            {
                allRuns.Add(info.Id, info);

                if (runsPerMapCat[info.MapId, info.Category] == null)
                    runsPerMapCat[info.MapId, info.Category] = new List<int>();
                List<int> runs = runsPerMapCat[info.MapId, info.Category];

                int index = runs.BinarySearch(info.Id, this);
                if (index < 0)
                    index = ~index;
                runs.Insert(index, info.Id);
            }
            else
            {
                allRuns[info.Id] = info;
            }
        }

        public void Add(List<RunInfo> infos)
        {
            lock (allRuns)
            {
                foreach (RunInfo info in infos)
                {
                    Add(info);
                }
            }

            //SavePendingToFile();
        }

        public void Remove(int id)
        {
            lock (allRuns)
            {
                if (!allRuns.ContainsKey(id))
                    return;

                RunInfo info = allRuns[id];

                if (runsPerMapCat[info.MapId, info.Category] == null)
                    return;
                List<int> runs = runsPerMapCat[info.MapId, info.Category];

                int index = runs.BinarySearch(info.Id, this);
                if (index < 0)
                    return;
                runs.RemoveAt(index);

                allRuns.Remove(id);
            }

            //SavePendingToFile();
        }

        public void AddPending(ref RunInfo info)
        {
            info.Id = -100 - pendingRuns.Count;
            Add(new List<RunInfo> { info });
            pendingRuns.Add(info.Id);
        }

        public void GetPBsForMapCat(int mapId, ECategory category, List<RunInfo> runs)
        {
            if (runsPerMapCat[mapId, (int)category] == null)
                return;

            HashSet<ulong> visitedPlayers = new HashSet<ulong>();
            foreach (int id in runsPerMapCat[mapId, (int)category])
            {

                RunInfo info = allRuns[id];
                if (visitedPlayers.Contains(info.PlayerId))
                    continue;

                visitedPlayers.Add(info.PlayerId);
                runs.Add(info);
            }
        }

        public void GetWRs(RunInfo[,] runs)
        {
            for (int m = 0; m < Map.COUNT; m++)
            {
                for (int c = 0; c < (int)ECategory.COUNT; c++)
                {
                    if (runsPerMapCat[m, c] == null)
                    {
                        runs[m, c] = new RunInfo
                        {
                            Id = -1,
                            PlayerId = 0
                        };
                        continue;
                    }

                    runs[m, c] = allRuns[runsPerMapCat[m, c][0]];
                }
            }
        }

        public RunInfo GetPB(ulong player, int mapId, ECategory category)
        {
            if (runsPerMapCat[mapId, (int)category] == null)
                return new RunInfo
                    {
                        Id = -1
                    };

            foreach (int id in runsPerMapCat[mapId, (int)category])
            {

                RunInfo info = allRuns[id];
                if (info.PlayerId != player)
                    continue;
                return info;
            }

            return new RunInfo
            {
                Id = -1
            };
        }

        public RunInfo GetWR(int mapId, ECategory category)
        {
            if (runsPerMapCat[mapId, (int)category] == null || runsPerMapCat[mapId, (int)category].Count == 0)
                return new RunInfo
                {
                    Id = -1
                };

            return allRuns[runsPerMapCat[mapId, (int)category][0]];
        }

        public void RequestPBsForMapCat(int mapId, ECategory category, Action onSuccess, int channel = 0)
        {
            Request(new GetPBsForMapCatRequest(mapId, category), onSuccess, channel);
        }

        public void RequestPlayerPBs(ulong playerId, Action onSuccess, int channel = 0)
        {
            Request(new GetPlayerPBs(playerId), onSuccess, channel);
        }

        public void RequestWRs(Action onSuccess, int channel = 0)
        {
            Request(new GetWRsRequest(), onSuccess, channel);
        }

        private void Request(IRequest<List<RunInfo>> request, Action onSuccess, int channel = 0)
        {
            GetRunHandlers[channel].Run(request);
            onGetRunSuccess[channel] = () =>
                {
                    Add(GetRunHandlers[channel].Result);
                    if (onSuccess != null)
                        onSuccess();
                };
        }

        public void RequestRecording(int id, Action<Recording> onSuccess)
        {
            GetRecordingHandler.Run(new GetRecordingRequest(id));
            onGetRecordingSuccess = onSuccess;
        }

        public override void Init()
        {
            base.Init();
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Velo.Ingame && !Velo.IngamePrev && !initRequest)
            {
                RequestWRs(null, 1);
                RequestPlayerPBs(Steamworks.SteamUser.GetSteamID().m_SteamID, null, 2);
                initRequest = true;
            }

            for (int i = 0; i < 3; i++)
            {
                if (GetRunHandlers[i].StatusChanged && GetRunHandlers[i].Status == ERequestStatus.SUCCESS)
                {
                    onGetRunSuccess[i]();
                }
            }
            if (GetRecordingHandler.StatusChanged && GetRecordingHandler.Status == ERequestStatus.SUCCESS)
            {
                onGetRecordingSuccess(GetRecordingHandler.Result);
            }
        }

        public int Compare(int i1, int i2)
        {
            return allRuns[i1].CompareTo(allRuns[i2]);
        }
    }
}
