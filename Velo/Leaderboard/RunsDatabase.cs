using System.Collections.Generic;
using System;
using System.Linq;

namespace Velo
{
    public enum ECategoryType : long
    {
        NEW_LAP, ONE_LAP, NEW_LAP_SKIPS, ONE_LAP_SKIPS, 
        ANY_PERC, HUNDRED_PERC,
        EVENT,
        COUNT
    }

    public struct Category
    {
        public ulong MapId;
        public ulong TypeId;
    }

    public struct RunInfo
    {
        public static readonly byte FLAG_NEW_GCD = 1 << 0;
        public static readonly byte FLAG_FIX_BOUNCE_GLITCH = 1 << 1;

        public ulong PlayerId;
        public long CreateTime;

        public int Id;
        public int RunTime;
        public Category Category;
        public byte WasWR;
        public byte HasComments;
        public byte SpeedrunCom;
        public byte PhysicsFlags;
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

            /*cmp = CreateTime.CompareTo(other.CreateTime);
            if (cmp != 0)
                return cmp;*/

            return Id.CompareTo(other.Id);
        }
    }

    public struct MapRunInfos
    {
        public ulong MapId;
        public RunInfo NewLap;
        public RunInfo OneLap;
        public RunInfo NewLapSkip;
        public RunInfo OneLapSkip;
        public RunInfo AnyPerc;
        public RunInfo HundredPerc;

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
                case ECategoryType.ANY_PERC:
                    AnyPerc = info;
                    break;
                case ECategoryType.HUNDRED_PERC:
                    HundredPerc = info;
                    break;
            }
        }

        public RunInfo Get(ECategoryType type)
        {
            switch (type)
            {
                case ECategoryType.NEW_LAP:
                    return NewLap;
                case ECategoryType.ONE_LAP:
                    return OneLap;
                case ECategoryType.NEW_LAP_SKIPS:
                    return NewLapSkip;
                case ECategoryType.ONE_LAP_SKIPS:
                    return OneLapSkip;
                case ECategoryType.ANY_PERC:
                    return AnyPerc;
                case ECategoryType.HUNDRED_PERC:
                    return HundredPerc;
            }
            return default;
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
                case ECategoryType.ANY_PERC:
                    return "Any%";
                case ECategoryType.HUNDRED_PERC:
                    return "100%";
                case ECategoryType.EVENT:
                    return "Event";
                default:
                    return "";
            }
        }
    }

    public struct MapEvent
    {
        public long From;
        public long To;
        public ECategoryType CategoryType;
        public ulong Winner;

        public long RemainingStart()
        {
            return From - DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public long RemainingEnd()
        {
            return To - DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public bool CurrentlyNotRunning()
        {
            return RunsDatabase.Instance.EventTime / TimeSpan.TicksPerSecond >= To;
        }
    }

    public class RunsDatabase : Module, IComparer<int>
    {
        private readonly RequestHandler getRunHandler = new RequestHandler();
        private readonly RequestHandler getRecordingHandler = new RequestHandler();
        private readonly RequestHandler getCommentHandler = new RequestHandler();
        private readonly RequestHandler initHandler = new RequestHandler();
        private bool addedDeletedSincePushed = false;
        private bool eventsPushed = false;
        private bool popularThisWeekPushed = false;

        private readonly Queue<KeyValuePair<int, Recording>> recordingCache = new Queue<KeyValuePair<int, Recording>>();

        private readonly SortedDictionary<int, RunInfo> allRuns = new SortedDictionary<int, RunInfo>(Comparer<int>.Create((x, y) => y.CompareTo(x)));
        private readonly Dictionary<Category, List<int>> runsPerCategory = new Dictionary<Category, List<int>>();
        private readonly List<int> pendingRuns = new List<int>();
        private readonly Dictionary<int, string> comments = new Dictionary<int, string>();
        private readonly List<PlayerInfoScore> scores = new List<PlayerInfoScore>();

        private readonly IEnumerable<ulong> curatedMapChronologicOrder = Enumerable.Range(0, (int)Map.CURATED_COUNT).Select(i => (ulong)i);
        private List<ulong> curatedMapPopularityOrder = new List<ulong>();
        private List<ulong> nonCuratedMapPopularityOrder = new List<ulong>();

        private readonly Dictionary<ulong, MapEvent> mapEvents = new Dictionary<ulong, MapEvent>();
        private long eventTime = 0;
        public long EventTime => eventTime;

        private readonly List<ulong> popularThisWeek = new List<ulong>();

        private readonly Dictionary<ulong, string> pseudoSteamIdToSpeedrunComName = new Dictionary<ulong, string>();

        private bool initRequest = false;

        public RunsDatabase() : base("Runs Database")
        {

        }

        public static RunsDatabase Instance = new RunsDatabase();

        public void Add(RunInfo info, bool updatePlace)
        {
            if (!allRuns.ContainsKey(info.Id))
            {
                allRuns.Add(info.Id, info);

                if (!runsPerCategory.ContainsKey(info.Category))
                    runsPerCategory.Add(info.Category, new List<int>());
                List<int> runs = runsPerCategory[info.Category];

                int index = runs.BinarySearch(info.Id, this);
                if (index < 0)
                    index = ~index;
                runs.Insert(index, info.Id);
                if (updatePlace && GetPB(info.PlayerId, info.Category).Id == info.Id)
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

            if (runsPerCategory[info.Category] == null)
                return;
            List<int> runs = runsPerCategory[info.Category];

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
            runsPerCategory.Clear();
            scores.Clear();
            comments.Clear();
            popularThisWeek.Clear();
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
            if (!runsPerCategory.TryGetValue(category, out List<int> runsForCategory) || runsForCategory == null)
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
            List<RunInfo> runs = new List<RunInfo>();
            if (!runsPerCategory.TryGetValue(category, out List<int> runsForCategory) || runsForCategory == null)
                return runs;

            return runsForCategory.Select(i => allRuns[i]).ToList();
        }

        public IEnumerable<RunInfo> GetWRHistoryForCategory(Category category)
        {
            List<RunInfo> runs = new List<RunInfo>();
            if (!runsPerCategory.TryGetValue(category, out List<int> runsForCategory) || runsForCategory == null)
                return runs;

            return runsForCategory.Select(i => allRuns[i]).Where(run => run.WasWR == 1);
        }

        public IEnumerable<MapRunInfos> GetWRs(int place, bool curated, bool popularity)
        {
            List<MapRunInfos> runs = new List<MapRunInfos>();

            IEnumerable<ulong> maps = curated ? (popularity ? curatedMapPopularityOrder : curatedMapChronologicOrder) : nonCuratedMapPopularityOrder;
            foreach (ulong m in maps)
            {
                MapRunInfos mapRuns = new MapRunInfos
                {
                    MapId = m
                };
                for (int t = 0; t < (int)ECategoryType.COUNT; t++)
                {
                    Category category = new Category { MapId = m, TypeId = (ulong)t };
                    if (!runsPerCategory.TryGetValue(category, out List<int> runsForCategory) || runsForCategory == null)
                        mapRuns.Set((ECategoryType)t, new RunInfo { Id = -1 });
                    else
                    {
                        int index = runsForCategory.FindIndex(i => allRuns[i].Place == place);
                        if (index == -1)
                            mapRuns.Set((ECategoryType)t, new RunInfo { Id = -1 });
                        else
                            mapRuns.Set((ECategoryType)t, allRuns[runsForCategory[index]]);
                    }
                }
                runs.Add(mapRuns);
            }
            return runs;
        }

        public IEnumerable<MapRunInfos> GetPlayerPBs(ulong playerId, bool curated, bool popularity)
        {
            List<MapRunInfos> runs = new List<MapRunInfos>();

            IEnumerable<ulong> maps = curated ? (popularity ? curatedMapPopularityOrder : curatedMapChronologicOrder) : nonCuratedMapPopularityOrder;
            foreach (ulong m in maps)
            {
                MapRunInfos mapRuns = new MapRunInfos
                {
                    MapId = m
                };
                for (int t = 0; t < (int)ECategoryType.COUNT; t++)
                {
                    Category category = new Category { MapId = m, TypeId = (ulong)t };
                    if (!runsPerCategory.TryGetValue(category, out List<int> runsForCategory) || runsForCategory == null || runsForCategory.Count == 0)
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
            return allRuns.Select(pair => pair.Value).Where(run => run.WasWR == 1 && !Map.IsOther(run.Category.MapId) && run.Category.TypeId != (ulong)ECategoryType.EVENT);
        }

        public RunInfo GetPB(ulong player, Category category)
        {
            if (!runsPerCategory.ContainsKey(category) || runsPerCategory[category] == null)
                return new RunInfo { Id = -1 };

            foreach (int id in runsPerCategory[category])
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

        public RunInfo GetWR(Category category)
        {
            if (!runsPerCategory.ContainsKey(category) || runsPerCategory[category] == null || runsPerCategory[category].Count == 0)
                return new RunInfo
                {
                    Id = -1
                };

            return allRuns[runsPerCategory[category][0]];
        }

        public IEnumerable<PlayerInfoWRs> GetWRCounts()
        {
            List<PlayerInfoWRs> players = new List<PlayerInfoWRs>();
            Dictionary<ulong, PlayerInfoWRs> playerDict = new Dictionary<ulong, PlayerInfoWRs>();

            IEnumerable<ulong> maps = curatedMapChronologicOrder;
            foreach (ulong m in maps)
            {
                for (int t = 0; t < (int)ECategoryType.COUNT; t++)
                {
                    if (t == (int)ECategoryType.EVENT)
                        continue;
                    Category category = new Category { MapId = m, TypeId = (ulong)t };
                    if (!runsPerCategory.TryGetValue(category, out List<int> runsForCategory) || runsForCategory == null || runsForCategory.Count == 0)
                        continue;

                    RunInfo run = allRuns[runsForCategory[0]];
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

        public string GetPlayerName(ulong steamId)
        {
            if (steamId > int.MaxValue)
                return SteamCache.GetPlayerName(steamId);

            if (pseudoSteamIdToSpeedrunComName.TryGetValue(steamId, out string name))
            {
                return name;
            }
            return "";
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

        public MapEvent GetEvent(ulong mapId)
        {
            if (mapEvents.TryGetValue(mapId, out MapEvent mapEvent))
            {
                return mapEvent;
            }
            return new MapEvent { From = 0, To = 0 };
        }

        public IEnumerable<ulong> GetPopularThisWeek()
        {
            return popularThisWeek;
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
            if (!addedDeletedSincePushed)
                PushAddedDeletedSince();
            addedDeletedSincePushed = true;
            if (!eventsPushed)
                PushRequestEvents();
            eventsPushed = true;
            if (!popularThisWeekPushed)
                PushRequestPopularThisWeek();
            popularThisWeekPushed = true;
            //if (pseudoSteamIdToSpeedrunComName.Count == 0)
            //PushRequestSpeedrunComPlayers(null);
        }

        public void PushRequestPopularityOrder(Action onSuccess, bool curated)
        {
            getRunHandler.Push(new GetPopularityOrderRequest(curated ? 0 : 1), newOrder =>
            {
                if (curated)
                    curatedMapPopularityOrder = newOrder;
                else
                    nonCuratedMapPopularityOrder = newOrder;
                onSuccess?.Invoke();
            });
        }

        public void PushRequestSpeedrunComPlayers(Action onSuccess)
        {
            getRunHandler.Push(new GetSpeedrunComPlayersRequest(), players =>
            {
                pseudoSteamIdToSpeedrunComName.Clear();
                foreach (var player in players)
                {
                    pseudoSteamIdToSpeedrunComName.Add(player.PseudoSteamId, player.Name);
                }
                onSuccess?.Invoke();
            });
        }

        public void PushRequestScores(Action onSuccess)
        {
            getRunHandler.Push(new GetScoresRequest(), newScores =>
            {
                scores.Clear();
                scores.AddRange(newScores);
                scores.Sort();
                scores.Reverse();
                onSuccess?.Invoke();
            });
        }

        public void RunRequestRuns(Action onSuccess, Action<Exception> onFailure)
        {
            getRunHandler.Run(onSuccess, onFailure);
            addedDeletedSincePushed = false;
            eventsPushed = false;
            popularThisWeekPushed = false;
        }

        public void RequestComment(int id, Action onSuccess, Action<Exception> onFailure)
        {
            getCommentHandler.Push(new GetCommentsRequest(id), text =>
            {
                SetComment(id, text);
            });
            getCommentHandler.Run(onSuccess, onFailure);
        }

        private void PushAddedDeletedSince()
        {
            if (getRunHandler.Time == 0)
                return;
            getRunHandler.Push(new GetAddedSinceRequest(getRunHandler.Time / TimeSpan.TicksPerSecond), added => { Add(added, true); });
            getRunHandler.Push(new GetDeletedSinceRequest(getRunHandler.Time / TimeSpan.TicksPerSecond), deleted => { Remove(deleted, true); });
        }

        private void PushRequestEvents()
        {
            getRunHandler.Push(new GetEventsRequest(), events => 
            { 
                mapEvents.Clear();
                foreach (var pair in events)
                    mapEvents.Add(pair.Key, pair.Value);
                eventTime = getRunHandler.Time;
            });
        }

        private void PushRequestPopularThisWeek()
        {
            getRunHandler.Push(new GetPopularThisWeekRequest(), runs =>
            {
                popularThisWeek.Clear();
                popularThisWeek.AddRange(runs.Select(r => r.Category.MapId));
                Add(runs, false);
            });
        }

        public void RequestRecording(int id, Action<Recording> onSuccess = null, Action<Exception> onFailure = null)
        {
            getRecordingHandler.Push(new GetRecordingRequest(id), recording =>
            {
                recording.Info.Id = id;
                if (recordingCache.Count >= 10)
                    recordingCache.Dequeue();
                recordingCache.Enqueue(new KeyValuePair<int, Recording>(id, recording));
                onSuccess?.Invoke(recording);
            });
            getRecordingHandler.Run(null, onFailure);
        }

        public void RequestRecordings(IEnumerable<int> ids, Action<Recording[]> onSuccess = null, Action<Exception> onFailure = null)
        {
            Recording[] recordings = new Recording[ids.Count()];

            int i = 0;
            while (ids.Any())
            {
                IEnumerable<int> next8 = ids.Take(8);
                ids = ids.Skip(8);

                foreach (int id in next8)
                {
                    int j = i;
                    getRecordingHandler.Push(new GetRecordingRequest(id), recording => recordings[j] = recording, false);
                    i++;
                }
                getRecordingHandler.Run(null, onFailure, wait: true);
            }

            onSuccess(recordings);
        }

        public void RequestRecordingCached(int id, Action<Recording> onSuccess = null, Action<Exception> onFailure = null)
        {
            foreach (var pair in recordingCache)
            {
                if (pair.Key == id)
                {
                    onSuccess(pair.Value);
                    return;
                }
            }
            RequestRecording(id, onSuccess, onFailure);
        }

        public override void Init()
        {
            base.Init();
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            TimeSpan delta = Velo.RealDelta;
            if (delta > TimeSpan.FromSeconds(5))
                delta = TimeSpan.FromSeconds(5);
            eventTime += delta.Ticks;

            if (Leaderboard.Instance.DisableLeaderboard.Value)
                return;

            if (Velo.Ingame && !Velo.IngamePrev)
            {
                ulong mapId = Map.GetCurrentMapId();
                Map.MapIdToName(mapId); // refresh cache
                if (!initRequest)
                {
                    initHandler.Push(new GetPlayerPBsRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), runs => Add(runs, true));
                    initHandler.Push(new GetPlayerPBsNonCuratedRequest(Steamworks.SteamUser.GetSteamID().m_SteamID), runs => Add(runs, true));
                    initHandler.Push(new SendPlayerNameRequest());
                    initHandler.Push(new GetEventsRequest());
                    initHandler.Run();
                    initRequest = true;
                }
            }
        }

        public int Compare(int i1, int i2)
        {
            return allRuns[i1].CompareTo(allRuns[i2]);
        }
    }
}
