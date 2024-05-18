using System.Collections.Generic;
using System;

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
        public int Place;
        public int TravDist;
        public short AvgSpeed;
        public short Grapples;
        public short Jumps;
        public short BoostUsed;

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
            Bytes.Write(info.Place, bytes, ref off);
            Bytes.Write(info.TravDist, bytes, ref off);
            Bytes.Write(info.AvgSpeed, bytes, ref off);
            Bytes.Write(info.Grapples, bytes, ref off);
            Bytes.Write(info.Jumps, bytes, ref off);
            Bytes.Write(info.BoostUsed, bytes, ref off);
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
            Bytes.Read(ref info.Place, bytes, ref off);
            Bytes.Read(ref info.TravDist, bytes, ref off);
            Bytes.Read(ref info.AvgSpeed, bytes, ref off);
            Bytes.Read(ref info.Grapples, bytes, ref off);
            Bytes.Read(ref info.Jumps, bytes, ref off);
            Bytes.Read(ref info.BoostUsed, bytes, ref off);
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

    public struct PlayerInfo : IComparable<PlayerInfo>
    {
        public ulong PlayerId;
        public int WrCount;

        public int CompareTo(PlayerInfo other)
        {
            return WrCount.CompareTo(other.WrCount);
        }
    }

    public enum ECategory : int
    {
        NEW_LAP, ONE_LAP, NEW_LAP_SKIPS, ONE_LAP_SKIPS, COUNT
    }

    public static class CategoryExt
    {
        public static string Label(this ECategory category)
        {
            switch (category)
            {
                case ECategory.NEW_LAP:
                    return "New lap";
                case ECategory.ONE_LAP:
                    return "1 lap";
                case ECategory.NEW_LAP_SKIPS:
                    return "New lap (Skip)";
                case ECategory.ONE_LAP_SKIPS:
                    return "1 lap (Skip)";
                default:
                    return "";
            }
        }
    }

    public class RunsDatabase : Module, IComparer<int>
    {
        public RequestHandler<List<RunInfo>>[] GetRunHandlers = new RequestHandler<List<RunInfo>>[]
            {
                new RequestHandler<List<RunInfo>>(),
                new RequestHandler<List<RunInfo>>()
            };
        public RequestHandler<Recording> GetRecordingHandler = new RequestHandler<Recording>();

        private readonly SortedDictionary<int, RunInfo> allRuns = new SortedDictionary<int, RunInfo>(Comparer<int>.Create((x, y) => y.CompareTo(x)));
        private readonly List<int>[,] runsPerMapCat = new List<int>[Map.COUNT, (int)ECategory.COUNT];
        private readonly List<int> pendingRuns = new List<int>();

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

        public void Add(IEnumerable<RunInfo> infos)
        {
            lock (allRuns)
            {
                foreach (RunInfo info in infos)
                {
                    Add(info);
                }
            }
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
        }

        public void AddPending(ref RunInfo info)
        {
            info.Id = -100 - pendingRuns.Count;
            Add(new List<RunInfo> { info });
            pendingRuns.Add(info.Id);
        }

        public RunInfo Get(int id)
        {
            if (allRuns.ContainsKey(id))
                return allRuns[id];
            return new RunInfo { Id = -1, PlayerId = 0, MapId = -1, Category = -1 };
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
                    if (runsPerMapCat[m, c] == null || runsPerMapCat[m, c].Count == 0)
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

        public void GetPlayerPBs(ulong playerId, RunInfo[,] runs)
        {
            for (int m = 0; m < Map.COUNT; m++)
            {
                for (int c = 0; c < (int)ECategory.COUNT; c++)
                {
                    if (runsPerMapCat[m, c] == null || runsPerMapCat[m, c].Count == 0)
                    {
                        runs[m, c] = new RunInfo
                        {
                            Id = -1,
                            PlayerId = 0
                        };
                        continue;
                    }

                    int i = runsPerMapCat[m, c].FindIndex((id) => allRuns[id].PlayerId == playerId);
                    if (i == -1)
                    {
                        runs[m, c] = new RunInfo
                        {
                            Id = -1,
                            PlayerId = 0
                        };
                        continue;
                    }

                    runs[m, c] = allRuns[runsPerMapCat[m, c][i]];
                }
            }
        }

        public void GetRecent(List<RunInfo> runs)
        {
            int count = 0;
            foreach (var pair in allRuns)
            {
                runs.Add(pair.Value);

                count++;
                if (count == 50)
                    break;
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

        public void GetTopPlayers(List<PlayerInfo> players)
        {
            Dictionary<ulong, PlayerInfo> playerDict = new Dictionary<ulong, PlayerInfo>();
            for (int m = 0; m < Map.COUNT; m++)
            {
                for (int c = 0; c < (int)ECategory.COUNT; c++)
                {
                    if (runsPerMapCat[m, c] == null || runsPerMapCat[m, c].Count == 0)
                        continue;

                    RunInfo run = allRuns[runsPerMapCat[m, c][0]];
                    if (run.Id == -1)
                        continue;
                    if (!playerDict.ContainsKey(run.PlayerId))
                        playerDict.Add(run.PlayerId, new PlayerInfo { PlayerId = run.PlayerId, WrCount = 1 });
                    else
                    {
                        PlayerInfo player = playerDict[run.PlayerId];
                        player.WrCount++;
                        playerDict[run.PlayerId] = player;
                    }
                }
            }

            foreach (var pair in playerDict)
            {
                players.Add(pair.Value);
            }
            players.Sort();
            players.Reverse();
        }

        public void RequestPBsForMapCat(int mapId, ECategory category, Action onSuccess = null, Action<Exception> onFailure = null, int handler = 0)
        {
            Request(new GetPBsForMapCatRequest(mapId, category), onSuccess, onFailure, handler);
        }

        public void RequestPlayerPBs(ulong playerId, Action onSuccess = null, Action<Exception> onFailure = null, int handler = 0)
        {
            Request(new GetPlayerPBs(playerId), onSuccess, onFailure, handler);
        }

        public void RequestWRs(Action onSuccess = null, Action<Exception> onFailure = null, int channel = 0)
        {
            Request(new GetWRsRequest(), onSuccess, onFailure, channel);
        }

        public void RequestRecent(Action onSuccess = null, Action<Exception> onFailure = null, int channel = 0)
        {
            Request(new GetRecentRequest(), onSuccess, onFailure, channel);
        }

        private void Request(IRequest<List<RunInfo>> request, Action onSuccess, Action<Exception> onFailure, int handler = 0)
        {
            GetRunHandlers[handler].Run(request, (runs) =>
            {
                Add(runs);
                onSuccess.NullCond((o) => o());
            }, onFailure);
        }

        public void RequestRecording(int id, Action<Recording> onSuccess = null, Action<Exception> onFailure = null)
        {
            GetRecordingHandler.Run(new GetRecordingRequest(id), onSuccess, onFailure);
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
                RequestPlayerPBs(Steamworks.SteamUser.GetSteamID().m_SteamID, null, null, 1);
                new RequestHandler<string>().Run(new SendPlayerNameRequest());
                initRequest = true;
            }
        }

        public int Compare(int i1, int i2)
        {
            return allRuns[i1].CompareTo(allRuns[i2]);
        }
    }
}
