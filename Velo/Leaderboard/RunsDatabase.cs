using System.Collections.Generic;
using System;
using System.Linq;

namespace Velo
{
    public enum ECategoryType : byte
    {
        NEW_LAP, ONE_LAP, NEW_LAP_SKIPS, ONE_LAP_SKIPS, COUNT
    }

    public struct Category
    {
        public byte MapId;
        public byte TypeId;
    }

    public struct RunInfo
    {
        public ulong PlayerId;
        public long CreateTime;

        public int Id;
        public int RunTime;
        public Category Category;
        public byte WasWR;
        public byte HasComments;
        public int Place;
        public int Dist;
        public int GroundDist;
        public int SwingDist;
        public int ClimbDist;
        public short AvgSpeed;
        public short Grapples;
        public short Jumps;
        public short BoostUsed;

        public int CompareTo(RunInfo other)
        {
            int cmp = RunTime.CompareTo(other.RunTime);
            if (cmp != 0)
                return cmp;

            return Id.CompareTo(other.Id);
        }
    }

    public struct MapRunInfos
    {
        public int MapId;
        public RunInfo NewLap;
        public RunInfo OneLap;
        public RunInfo NewLapSkip;
        public RunInfo OneLapSkip;

        public void Set(ECategoryType type, RunInfo info)
        {
            switch (type)
            {
                case ECategoryType.NEW_LAP:
                    NewLap = info;
                    break;
                case ECategoryType.ONE_LAP:
                    OneLap = info;
                    break;
                case ECategoryType.NEW_LAP_SKIPS:
                    NewLapSkip = info;
                    break;
                case ECategoryType.ONE_LAP_SKIPS:
                    OneLapSkip = info;
                    break;
            }
        }
    }

    public struct PlayerInfoWRs : IComparable<PlayerInfoWRs>
    {
        public ulong PlayerId;
        public int WrCount;
        public int Place;

        public int CompareTo(PlayerInfoWRs other)
        {
            return WrCount.CompareTo(other.WrCount);
        }
    }

    public struct PlayerInfoScore : IComparable<PlayerInfoScore>
    {
        public ulong PlayerId;
        public int Score;
        public int Place;

        public int CompareTo(PlayerInfoScore other)
        {
            return Score.CompareTo(other.Score);
        }
    }

    public static class CategoryTypeExt
    {
        public static string Label(this ECategoryType categoryType)
        {
            switch (categoryType)
            {
                case ECategoryType.NEW_LAP:
                    return "New lap";
                case ECategoryType.ONE_LAP:
                    return "1 lap";
                case ECategoryType.NEW_LAP_SKIPS:
                    return "New lap (Skip)";
                case ECategoryType.ONE_LAP_SKIPS:
                    return "1 lap (Skip)";
                default:
                    return "";
            }
        }
    }

    public class RunsDatabase : Module, IComparer<int>
    {
        private readonly RequestHandler getRunHandler = new RequestHandler();
        private readonly RequestHandler getRecordingHandler = new RequestHandler();
        private readonly RequestHandler getCommentHandler = new RequestHandler();
        private readonly RequestHandler initHandler = new RequestHandler();
        private bool addedDeletecSincePushed = false;

        private readonly Queue<KeyValuePair<int, Recording>> recordingCache = new Queue<KeyValuePair<int, Recording>>();

        private readonly SortedDictionary<int, RunInfo> allRuns = new SortedDictionary<int, RunInfo>(Comparer<int>.Create((x, y) => y.CompareTo(x)));
        private readonly List<int>[,] runsPerCategory = new List<int>[Map.COUNT, (int)ECategoryType.COUNT];
        private readonly List<int> pendingRuns = new List<int>();
        private readonly Dictionary<int, string> comments = new Dictionary<int, string>();
        private readonly List<PlayerInfoScore> scores = new List<PlayerInfoScore>();

        private bool initRequest = false;

        public RunsDatabase() : base("Runs Database")
        {

        }

        public static RunsDatabase Instance = new RunsDatabase();

        public void Add(RunInfo info, bool updatePlace)
        {
            if (info.Category.MapId >= Map.COUNT)
                return;

            if (!allRuns.ContainsKey(info.Id))
            {
                allRuns.Add(info.Id, info);

                if (runsPerCategory[info.Category.MapId, info.Category.TypeId] == null)
                    runsPerCategory[info.Category.MapId, info.Category.TypeId] = new List<int>();
                List<int> runs = runsPerCategory[info.Category.MapId, info.Category.TypeId];

                int index = runs.BinarySearch(info.Id, this);
                if (index < 0)
                    index = ~index;
                runs.Insert(index, info.Id);
                if (updatePlace)
                {
                    for (int i = index + 1; i < runs.Count; i++)
                    {
                        if (info.CompareTo(allRuns[runs[i]]) < 0 && allRuns[runs[i]].Place != -1)
                        {
                            RunInfo info2 = allRuns[runs[i]];
                            info2.Place++;
                            allRuns[runs[i]] = info2;
                        }
                    }
                }
            }
            else
            {
                allRuns[info.Id] = info;
            }
        }

        public void Add(IEnumerable<RunInfo> infos, bool updatePlace)
        {
            foreach (RunInfo info in infos)
            {
                Add(info, updatePlace);
            }
        }

        public void Remove(int id, bool updatePlace)
        {
            if (!allRuns.ContainsKey(id))
                return;

            RunInfo info = allRuns[id];

            if (runsPerCategory[info.Category.MapId, info.Category.TypeId] == null)
                return;
            List<int> runs = runsPerCategory[info.Category.MapId, info.Category.TypeId];

            int index = runs.BinarySearch(info.Id, this);
            if (index < 0)
                return;
            runs.RemoveAt(index);
            if (updatePlace)
            {
                for (int i = index; i < runs.Count; i++)
                {
                    if (info.CompareTo(allRuns[runs[i]]) < 0 && allRuns[runs[i]].Place != -1)
                    {
                        RunInfo info2 = allRuns[runs[i]];
                        info2.Place--;
                        allRuns[runs[i]] = info2;
                    }
                }
            }

            allRuns.Remove(id);
        }

        public void Remove(IEnumerable<int> ids, bool updatePlace)
        {
            foreach (int id in ids)
            {
                Remove(id, updatePlace);
            }
        }

        public void AddPending(ref RunInfo info)
        {
            info.Id = -100 - pendingRuns.Count;
            info.Place = -1;
            Add(new List<RunInfo> { info }, false);
            pendingRuns.Add(info.Id);
        }

        public void SetComment(int id, string comment)
        {
            comments[id] = comment;
        }

        public void Clear()
        {
            List<RunInfo> pending = pendingRuns.Where((i) => allRuns.ContainsKey(i)).Select((i) => allRuns[i]).ToList();
            allRuns.Clear();
            for (int m = 0; m < Map.COUNT; m++)
            {
                for (int c = 0; c < (int)ECategoryType.COUNT; c++)
                {
                    runsPerCategory[m, c]?.Clear();
                }
            }
            scores.Clear();
            comments.Clear();
            foreach (var info in pending)
            {
                Add(new List<RunInfo> { info }, false);
            }
        }

        public RunInfo Get(int id)
        {
            if (allRuns.ContainsKey(id))
                return allRuns[id];
            return new RunInfo { Id = -1 };
        }

        public int Count()
        {
            return allRuns.Count;
        }

        public IEnumerable<RunInfo> GetPBsForCategory(Category category)
        {
            List<RunInfo> runs = new List<RunInfo>();
            var runsForCategory = runsPerCategory[category.MapId, category.TypeId];
            if (runsForCategory == null)
                return runs;

            HashSet<ulong> visitedPlayers = new HashSet<ulong>();

            for (int i = 0; i < runsForCategory.Count; i++)
            {
                int id = runsForCategory[i];
                RunInfo info = allRuns[id];
                if (visitedPlayers.Contains(info.PlayerId))
                    continue;

                visitedPlayers.Add(info.PlayerId);
                runs.Add(info);
            }
            return runs;
        }

        public IEnumerable<RunInfo> GetAllForCategory(Category category)
        {
            var runsForCategory = runsPerCategory[category.MapId, category.TypeId];
            if (runsForCategory == null)
                return new List<RunInfo>();

            return runsForCategory.Select(i => allRuns[i]).ToList();
        }

        public IEnumerable<RunInfo> GetWRHistoryForCategory(Category category)
        {
            var runsForCategory = runsPerCategory[category.MapId, category.TypeId];
            if (runsForCategory == null)
                return new List<RunInfo>();

            return runsForCategory.Select(i => allRuns[i]).Where(run => run.WasWR == 1);
        }

        public IEnumerable<MapRunInfos> GetWRs()
        {
            List<MapRunInfos> runs = new List<MapRunInfos>();

            for (int m = 0; m < Map.COUNT; m++)
            {
                MapRunInfos mapRuns = new MapRunInfos
                {
                    MapId = m
                };
                for (int t = 0; t < (int)ECategoryType.COUNT; t++)
                {
                    var runsForCategory = runsPerCategory[m, t];
                    if (runsForCategory == null || runsForCategory.Count == 0)
                        mapRuns.Set((ECategoryType)t, new RunInfo { Id = -1 });
                    else
                        mapRuns.Set((ECategoryType)t, allRuns[runsForCategory[0]]);
                }
                runs.Add(mapRuns);
            }
            return runs;
        }

        public IEnumerable<MapRunInfos> GetPlayerPBs(ulong playerId)
        {
            List<MapRunInfos> runs = new List<MapRunInfos>();

            for (int m = 0; m < Map.COUNT; m++)
            {
                MapRunInfos mapRuns = new MapRunInfos
                {
                    MapId = m
                };
                for (int t = 0; t < (int)ECategoryType.COUNT; t++)
                {
                    var runsForCategory = runsPerCategory[m, t];
                    if (runsForCategory == null || runsForCategory.Count == 0)
                        mapRuns.Set((ECategoryType)t, new RunInfo { Id = -1 });
                    else
                    {
                        int j = runsForCategory.FindIndex((id) => allRuns[id].PlayerId == playerId);
                        if (j == -1)
                            mapRuns.Set((ECategoryType)t, new RunInfo { Id = -1 });
                        else
                            mapRuns.Set((ECategoryType)t, allRuns[runsForCategory[j]]);
                    }
                }
                runs.Add(mapRuns);
            }
            return runs;
        }

        public IEnumerable<RunInfo> GetRecent()
        {
            return allRuns.Select(pair => pair.Value);
        }

        public IEnumerable<RunInfo> GetRecentWRs()
        {
            return allRuns.Select(pair => pair.Value).Where(run => run.WasWR == 1);
        }

        public RunInfo GetPB(ulong player, Category category)
        {
            if (runsPerCategory[category.MapId, category.TypeId] == null)
                return new RunInfo { Id = -1 };

            foreach (int id in runsPerCategory[category.MapId, category.TypeId])
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

        public RunInfo GetWR(int mapId, ECategoryType category)
        {
            if (runsPerCategory[mapId, (int)category] == null || runsPerCategory[mapId, (int)category].Count == 0)
                return new RunInfo
                {
                    Id = -1
                };

            return allRuns[runsPerCategory[mapId, (int)category][0]];
        }

        public IEnumerable<PlayerInfoWRs> GetWRCounts()
        {
            List<PlayerInfoWRs> players = new List<PlayerInfoWRs>();
            Dictionary<ulong, PlayerInfoWRs> playerDict = new Dictionary<ulong, PlayerInfoWRs>();
            for (int m = 0; m < Map.COUNT; m++)
            {
                for (int c = 0; c < (int)ECategoryType.COUNT; c++)
                {
                    if (runsPerCategory[m, c] == null || runsPerCategory[m, c].Count == 0)
                        continue;

                    RunInfo run = allRuns[runsPerCategory[m, c][0]];
                    if (run.Id == -1)
                        continue;
                    if (!playerDict.ContainsKey(run.PlayerId))
                        playerDict.Add(run.PlayerId, new PlayerInfoWRs { PlayerId = run.PlayerId, WrCount = 1 });
                    else
                    {
                        PlayerInfoWRs player = playerDict[run.PlayerId];
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
            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfoWRs info = players[i];
                info.Place = i;
                players[i] = info;
            }
            return players;
        }

        public string GetComment(int id)
        {
            if (!comments.ContainsKey(id))
                return null;
            return comments[id];
        }

        public IEnumerable<PlayerInfoScore> GetScores()
        {
            return scores;
        }

        public bool Pending()
        {
            return
                getRunHandler.Status == ERequestStatus.PENDING ||
                getRecordingHandler.Status == ERequestStatus.PENDING ||
                getCommentHandler.Status == ERequestStatus.PENDING;
        }

        public void CancelAll()
        {
            getRunHandler.Cancel();
            getRecordingHandler.Cancel();
            getCommentHandler.Cancel();
        }

        public void CancelRequestRecording()
        {
            getRecordingHandler.Cancel();
        }

        public void CancelRequestComment()
        {
            getCommentHandler.Cancel();
        }

        public void PushRequestRuns(IRequest<List<RunInfo>> request, Action onSuccess)
        {
            getRunHandler.Push(request, (runs) =>
            {
                Add(runs, request is GetAddedSinceRequest);
                onSuccess?.Invoke();
            });
            if (!addedDeletecSincePushed)
                PushAddedDeletedSince();
            addedDeletecSincePushed = true;
        }

        public void PushRequestScores(Action onSuccess)
        {
            getRunHandler.Push(new GetScoresRequest(), (newScores) =>
            {
                scores.Clear();
                scores.AddRange(newScores);
                scores.Sort();
                scores.Reverse();
                onSuccess?.Invoke();
            });
        }

        public void RunRequestRuns(Action<Exception> onFailure)
        {
            getRunHandler.Run(onFailure);
            addedDeletecSincePushed = false;
        }

        public void RequestComment(int id, Action onSuccess, Action<Exception> onFailure)
        {
            getCommentHandler.Push(new GetCommentsRequest(id), (text) =>
            {
                SetComment(id, text);
                onSuccess?.Invoke();
            });
            getCommentHandler.Run(onFailure);
        }

        private void PushAddedDeletedSince()
        {
            getRunHandler.Push(new GetAddedSinceRequest(getRunHandler.Time), (added) => { Add(added, true); });
            getRunHandler.Push(new GetDeletedSinceRequest(getRunHandler.Time), (deleted) => { Remove(deleted, true); });
        }

        public Recording GetRecording(int id)
        {
            foreach (var pair in recordingCache)
            {
                if (pair.Key == id)
                    return pair.Value;
            }
            return null;
        }

        public void RequestRecording(int id, Action<Recording> onSuccess = null, Action<Exception> onFailure = null)
        {
            getRecordingHandler.Push(new GetRecordingRequest(id), (recording) =>
            {
                if (recordingCache.Count >= 10)
                    recordingCache.Dequeue();
                recordingCache.Enqueue(new KeyValuePair<int, Recording>(id, recording));
                onSuccess?.Invoke(recording);
            });
            getRecordingHandler.Run(onFailure);
        }

        public override void Init()
        {
            base.Init();
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            if (Leaderboard.Instance.DisableLeaderboard.Value)
                return;

            if (Velo.Ingame && !Velo.IngamePrev && !initRequest)
            {
                initHandler.Push(new GetPlayerPBsRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), runs => Add(runs, true));
                initHandler.Push(new SendPlayerNameRequest());
                initHandler.Run();
                initRequest = true;
            }
        }

        public int Compare(int i1, int i2)
        {
            return allRuns[i1].CompareTo(allRuns[i2]);
        }
    }
}
