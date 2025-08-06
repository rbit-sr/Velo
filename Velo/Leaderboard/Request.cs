using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace Velo
{
    public class OutdatedVersionException : Exception
    {
        public OutdatedVersionException() :
            base("Outdated Client! Please update Velo to the latest version!") { }
    }

    public enum ERequestType : uint
    {
        NOP,
        GET_PLAYER_PBS, 
        GET_WRS, 
        GET_RECENT,
        GET_RECENT_WRS,
        GET_ALL_FOR_CATEGORY,
        GET_PBS_FOR_CATEGORY,
        GET_WR_HISTORY_FOR_CATEGORY,
        SUBMIT_RUN, 
        GET_RECORDING, 
        SEND_PLAYER_NAME, 
        GET_COMMENTS, 
        GET_SCORES,
        GET_ADDED_SINCE,
        GET_DELETED_SINCE,
        CHECK_UPDATE,
        GET_PLAYER_PBS_NON_CURATED,
        GET_WRS_NON_CURATED,
        GET_NON_CURATED_ORDER,
        SEND_MAP_NAME,
        GET_EVENTS,
        GET_POPULARITY_ORDER,
        GET_POPULAR_THIS_WEEK,
        GET_EVENT_WRS,
        GET_PLAYER_EVENT_PBS,
        SEND_STACKTRACE,
        SEND_SPEEDRUN_COM_DATA, // not implemented here
        GET_SPEEDRUN_COM_PLAYERS
    }

    public enum ERequestStatus
    {
        NONE, PENDING, SUCCESS, FAILURE
    }

    public class RequestHandler
    {
        private struct RequestEntry
        {
            public Action<Client> SendHeader;
            public Func<Client, Action> Run;
            public bool BypassVersionCheck;
        }

        private List<RequestEntry> requests = new List<RequestEntry>();
        private Task<bool> task;
        private CancellationTokenSource cancel;
        private bool statusChanged = false;

        private readonly int maxAttempts;
        private readonly Func<int, int> attemptWaitTimes;

        public RequestHandler(int maxAttempts = 3, Func<int, int> attemptWaitTimes = null)
        {
            this.maxAttempts = maxAttempts;
            this.attemptWaitTimes = attemptWaitTimes;
        }

        public void Push<T>(IRequest<T> request, Action<T> onSuccess = null, bool onSuccessPreUpdate = true)
        {
            requests.Add(new RequestEntry
            {
                SendHeader = (client) => 
                { 
                    client.Send(request.RequestType()); 
                    request.SendHeader(client); 
                },
                Run = (client) => 
                { 
                    T result = request.Run(client);
                    if (onSuccess != null && onSuccessPreUpdate)
                        return () => Velo.AddOnPreUpdateTS(() => onSuccess.Invoke(result));
                    else
                        return () => onSuccess?.Invoke(result);
                },
                BypassVersionCheck = request is IVersionBypassingRequest
            });
        }

        public void Run(Action onSuccess = null, Action<Exception> onFailure = null, bool wait = false)
        {
            if (requests.Count == 0) return;

            List<RequestEntry> requests2 = requests;
            requests = new List<RequestEntry>();

            Cancel();
            task?.Wait();

            cancel = new CancellationTokenSource();
            CancellationToken cancelToken = cancel.Token;
            statusChanged = true;

            task = Task.Run(() =>
            {
                List<Action> onSuccessActions = new List<Action>();
                Exception error = null;
                bool bypassVersionCheck = requests2.Any(entry => entry.BypassVersionCheck);
                for (int i = 0; i < maxAttempts; i++)
                {
                    onSuccessActions.Clear();
                    Client client = new Client(cancelToken);
                    try
                    {
                        client.Connect();
                        byte enableCrc = 1;
                        client.Send(enableCrc);
                        client.Send((byte)requests2.Count);
                        if (!bypassVersionCheck)
                            client.Send(Version.VERSION);
                        else
                            client.Send(ushort.MaxValue);
                        client.SendCrc(); 
                        try
                        {
                            foreach (var request in requests2)
                            {
                                request.SendHeader(client);
                            }

                            client.SendCrc();

                            int success = client.Receive<int>();
                            if (success != int.MaxValue)
                                throw new OutdatedVersionException();
                        }
                        catch (OutdatedVersionException e)
                        {
                            throw e;
                        }
                        catch (Exception e)
                        {
                            int success = client.Receive<int>();
                            if (success != int.MaxValue)
                                throw new OutdatedVersionException();
                            throw e;
                        }

                        long time = client.Receive<long>();

                        if (cancelToken.IsCancellationRequested)
                            throw new OperationCanceledException();
                        Time = time * TimeSpan.TicksPerSecond;

                        foreach (var request in requests2)
                        {
                            onSuccessActions.Add(request.Run(client));
                        }

                        client.ReceiveSuccess();
                    }
                    catch (OutdatedVersionException e)
                    {
                        Velo.AddOnPreUpdateTS(AutoUpdate.Instance.Check);
                        error = e;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        client.Close();
                    }

                    if (cancelToken.IsCancellationRequested || error is OutdatedVersionException)
                        break;

                    if (error == null)
                    {
                        foreach (var onSuccessAction in onSuccessActions)
                        {
                            onSuccessAction();
                        }
                        if (onSuccess != null)
                        {
                            Velo.AddOnPreUpdateTS(() =>
                            {
                                onSuccess();
                            });
                        }

                        return true;
                    }

                    if (attemptWaitTimes != null && i + 1 < maxAttempts)
                    {
                        int millis = attemptWaitTimes(i);
                        try
                        {
                            Task.Delay(millis).Wait(cancelToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
                if (onFailure != null && error != null && !(error is OperationCanceledException || error is TaskCanceledException))
                {
                    Velo.AddOnPreUpdateTS(() =>
                    {
                        onFailure(error);
                    });
                }
                return false;
            });
            if (wait)
                task.Wait();
        }

        public void Cancel()
        {
            if (cancel != null && task != null && !task.IsCompleted)
            {
                cancel.Cancel();
                cancel.Dispose();
                cancel = null;
            }
        }

        public ERequestStatus Status
        {
            get
            {
                if (task == null)
                    return ERequestStatus.NONE;
                if (!task.IsCompleted)
                    return ERequestStatus.PENDING;
                if (task.Result)
                    return ERequestStatus.SUCCESS;
                else
                    return ERequestStatus.FAILURE;
            }
        }

        public bool StatusChanged
        {
            get
            {
                if (!statusChanged || task == null || !task.IsCompleted)
                    return false;
                statusChanged = false;
                return true;
            }
        }

        public long Time { get; set; }
    }

    public interface IRequest<T>
    {
        void SendHeader(Client client);
        T Run(Client client);
        uint RequestType();
    }

    public interface IVersionBypassingRequest
    {

    }

    public class GetPlayerPBsRequest : IRequest<List<RunInfo>>
    {
        private readonly ulong playerId;

        public GetPlayerPBsRequest(ulong playerId)
        {
            this.playerId = playerId;
        }

        public void SendHeader(Client client)
        {
            client.Send(playerId);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_PLAYER_PBS;
        }
    }

    public class GetPlayerPBsNonCuratedRequest : IRequest<List<RunInfo>>
    {
        private readonly ulong playerId;

        public GetPlayerPBsNonCuratedRequest(ulong playerId)
        {
            this.playerId = playerId;
        }

        public void SendHeader(Client client)
        {
            client.Send(playerId);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_PLAYER_PBS_NON_CURATED;
        }
    }

    public class GetWRsRequest : IRequest<List<RunInfo>>
    {
        private readonly int place;

        public GetWRsRequest(int place)
        {
            this.place = place;
        }

        public void SendHeader(Client client)
        {
            client.Send(place);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_WRS;
        }
    }

    public class GetWRsNonCuratedRequest : IRequest<List<RunInfo>>
    {
        private readonly int place;

        public GetWRsNonCuratedRequest(int place)
        {
            this.place = place;
        }

        public void SendHeader(Client client)
        {
            client.Send(place);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_WRS_NON_CURATED;
        }
    }

    public class GetNonCuratedOrder : IRequest<List<ulong>>
    {
        public GetNonCuratedOrder()
        {

        }

        public void SendHeader(Client client)
        {

        }

        public List<ulong> Run(Client client)
        {
            List<ulong> result = new List<ulong>();
            while (true)
            {
                ulong current = client.Receive<ulong>();
                if (current == ulong.MaxValue)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_NON_CURATED_ORDER;
        }
    }

    public class GetRecentRequest : IRequest<List<RunInfo>>
    {
        private readonly int start;
        private readonly int count;

        public GetRecentRequest(int start, int count)
        {
            this.start = start;
            this.count = count;
        }

        public void SendHeader(Client client)
        {
            client.Send(start);
            client.Send(count);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.Receive<int>();

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_RECENT;
        }
    }

    public class GetRecentWRsRequest : IRequest<List<RunInfo>>
    {
        private readonly int start;
        private readonly int count;

        public GetRecentWRsRequest(int start, int count)
        {
            this.start = start;
            this.count = count;
        }

        public void SendHeader(Client client)
        {
            client.Send(start);
            client.Send(count);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.Receive<int>();

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_RECENT_WRS;
        }
    }

    public class GetAllForCategoryRequest : IRequest<List<RunInfo>>
    {
        private readonly Category category;
        private readonly int start;
        private readonly int count;

        public GetAllForCategoryRequest(Category category, int start, int count)
        {
            this.category = category;
            this.start = start;
            this.count = count;
        }

        public void SendHeader(Client client)
        {
            client.Send(category.MapId);
            client.Send(category.TypeId);
            client.Send(start);
            client.Send(count);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.Receive<int>();

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_ALL_FOR_CATEGORY;
        }
    }

    public class GetPBsForCategoryRequest : IRequest<List<RunInfo>>
    {
        private readonly Category category;
        private readonly int start;
        private readonly int count;

        public GetPBsForCategoryRequest(Category category, int start, int count)
        {
            this.category = category;
            this.start = start;
            this.count = count;
        }

        public void SendHeader(Client client)
        {
            client.Send(category.MapId);
            client.Send(category.TypeId);
            client.Send(start);
            client.Send(count);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.Receive<int>();

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_PBS_FOR_CATEGORY;
        }
    }

    public class GetWRHistoryForCategoryRequest : IRequest<List<RunInfo>>
    {
        private readonly Category category;
        private readonly int start;
        private readonly int count;

        public GetWRHistoryForCategoryRequest(Category category, int start, int count)
        {
            this.category = category;
            this.start = start;
            this.count = count;
        }

        public void SendHeader(Client client)
        {
            client.Send(category.MapId);
            client.Send(category.TypeId);
            client.Send(start);
            client.Send(count);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.Receive<int>();

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_WR_HISTORY_FOR_CATEGORY;
        }
    }

    public class NewPbInfo
    {
        public RunInfo RunInfo;
        public int TimeSave;
        public int Tied;
        public int RecordType;
    }

    public class SubmitRunRequest : IRequest<NewPbInfo>
    {
        private enum EVerificationResult : int
        {
            SUCCESS, NO_CLIENT_SIGN, NO_RUN_SIGN, TIME_MANIPULATION, CLIENT_MODIFICATION, MACRO
        }

        private readonly Recording recording;
        private readonly byte[] bytes;

        public SubmitRunRequest(Recording recording, byte[] bytes)
        {
            this.recording = recording;
            this.bytes = bytes;
        }

        public void SendHeader(Client client)
        {
            
        }

        [DllImport("VeloVerifier.dll", EntryPoint = "generate_sign")]
        private static extern void generate_sign(IntPtr salt, int salt_len, IntPtr sign);
        
        public NewPbInfo Run(Client client)
        {
            long deltaSum = 0;
            for (int i = recording.LapStart; i < recording.Count; i++)
            {
                deltaSum += recording[i].Delta.Ticks;
            }
            float avgFramerate = 1f / (float)new TimeSpan(deltaSum / (recording.Count - recording.LapStart)).TotalSeconds;

            client.Send(recording.Info);
            client.Send(recording[recording.LapStart].RealTime);
            client.Send(recording[recording.Count - 1].RealTime);
            client.Send(avgFramerate);
            client.Send(recording.Timings.Count);
            foreach (MacroDetection.Timing timing in recording.Timings)
            {
                client.Send(timing);
            }
            client.SendCrc();

            int timeSave = client.Receive<int>();

            if (timeSave < 0)
            {
                client.VerifyCrc();
                return new NewPbInfo { RunInfo = new RunInfo { Id = -1 }, TimeSave = timeSave };
            }

            int tied = client.Receive<int>();
            int recordType = client.Receive<int>();

            byte[] salt = new byte[32];
            byte[] sign = new byte[32];
            client.Receive(salt);
            client.VerifyCrc();

            if (!Debugger.IsAttached)
            {
                try
                {
                    unsafe
                    {
                        fixed (byte* saltBytes = salt)
                        {
                            fixed (byte* signBytes = sign)
                            {
                                generate_sign((IntPtr)saltBytes, 32, (IntPtr)signBytes);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    sign.Fill((byte)0);
                }
            }

            client.Send(sign);
            
            client.Send(bytes.Length);
            client.Send(bytes);
            client.Send(recording.Sign);
            client.SendCrc();

            EVerificationResult verificationResult = (EVerificationResult)client.Receive<int>();

            bool sendMismatchingFiles = false;
            if (verificationResult == EVerificationResult.NO_CLIENT_SIGN)
            {
                Notifications.Instance.PushNotification(
                    "WARNING: Could not verify client!\n" +
                    "Your run will be manually verified by a leaderboard moderator.",
                    Color.LightGreen, TimeSpan.FromSeconds(6d));
                sendMismatchingFiles = true;
            }
            else if (verificationResult == EVerificationResult.NO_RUN_SIGN)
            {
                Notifications.Instance.PushNotification(
                    "WARNING: Could not verify run!\n" +
                    "Your run will be manually verified by a leaderboard moderator.",
                    Color.LightGreen, TimeSpan.FromSeconds(6d));
                sendMismatchingFiles = true;
            }
            else if (verificationResult == EVerificationResult.TIME_MANIPULATION)
            {
                Notifications.Instance.PushNotification(
                    "WARNING: Time manipulation detected!\n" +
                    "Your run will be manually verified by a leaderboard moderator.",
                    Color.LightGreen, TimeSpan.FromSeconds(6d));
            }
            else if (verificationResult == EVerificationResult.CLIENT_MODIFICATION)
            {
                Notifications.Instance.PushNotification(
                    "WARNING: Client modifications detected!\n" +
                    "Your run will be manually verified by a leaderboard moderator.",
                    Color.LightGreen, TimeSpan.FromSeconds(6d));
                sendMismatchingFiles = true;
            }
            else if (verificationResult == EVerificationResult.MACRO)
            {
                Notifications.Instance.PushNotification(
                    "WARNING: Macro detected!\n" +
                    "Your run will be manually verified by a leaderboard moderator.",
                    Color.LightGreen, TimeSpan.FromSeconds(6d));
            }

            if (sendMismatchingFiles)
            {
                Verify.VerifyFiles(out Dictionary<string, bool> verifyResult);
                client.Send(verifyResult.Values.Count(b => !b));
                foreach (var pair in verifyResult)
                {
                    if (!pair.Value)
                        client.Send(pair.Key);
                }
                client.SendCrc();
            }

            RunInfo result = client.Receive<RunInfo>();
            client.VerifyCrc();

            return new NewPbInfo 
            { 
                RunInfo = result, 
                TimeSave = timeSave,
                Tied = tied,
                RecordType = recordType
            };
        }

        public uint RequestType()
        {
            return (uint)ERequestType.SUBMIT_RUN;
        }
    }

    public class GetRecordingRequest : IRequest<Recording>
    {
        private readonly int id;

        public GetRecordingRequest(int id)
        {
            this.id = id;
        }

        public void SendHeader(Client client)
        {
            client.Send(id);
        }

        public Recording Run(Client client)
        {
            Recording result = new Recording();

            int size = client.Receive<int>();
            byte[] data = new byte[size];
            client.Receive(data, size);

            MemoryStream dataStream = new MemoryStream(data, 0, size)
            {
                Position = 0
            };
            result.Read(dataStream);

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_RECORDING;
        }
    }

    public class SendPlayerNameRequest : IRequest<string>
    {
        public SendPlayerNameRequest()
        {

        }

        public void SendHeader(Client client)
        {

        }

        public string Run(Client client)
        {
            ulong steamId = Steamworks.SteamUser.GetSteamID().m_SteamID;
            client.Send(steamId);
            client.Send(SteamCache.GetPlayerName(steamId));
            client.SendCrc();

            return "";
        }

        public uint RequestType()
        {
            return (uint)ERequestType.SEND_PLAYER_NAME;
        }
    }

    public class SendMapNameRequest : IRequest<string>
    {
        private readonly ulong mapId;

        public SendMapNameRequest(ulong mapId)
        {
            this.mapId = mapId; 
        }

        public void SendHeader(Client client)
        {

        }

        public string Run(Client client)
        {
            client.Send(mapId);
            client.Send(Map.MapIdToName(mapId));
            client.SendCrc();

            return "";
        }

        public uint RequestType()
        {
            return (uint)ERequestType.SEND_MAP_NAME;
        }
    }

    public class GetCommentsRequest : IRequest<string>
    {
        private readonly int id;

        public GetCommentsRequest(int id)
        {
            this.id = id;
        }

        public void SendHeader(Client client)
        {
            client.Send(id);
        }

        public string Run(Client client)
        {
            string text = client.ReceiveStr();
            client.VerifyCrc();

            return text;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_COMMENTS;
        }
    }

    public class GetScoresRequest : IRequest<List<PlayerInfoScore>>
    {
        public GetScoresRequest()
        {

        }

        public void SendHeader(Client client)
        {
            
        }

        public List<PlayerInfoScore> Run(Client client)
        {
            List<PlayerInfoScore> result = new List<PlayerInfoScore>();
            while (true)
            {
                ulong playerId = client.Receive<ulong>();
                int score = client.Receive<int>();
               
                if (score == -1)
                    break;
                result.Add(new PlayerInfoScore { PlayerId = playerId, Score = score });
            }

            client.VerifyCrc();

            result.Sort();
            result.Reverse();

            for (int i = 0; i < result.Count; i++)
            {
                PlayerInfoScore info = result[i];
                info.Place = i;
                result[i] = info;
            }

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_SCORES;
        }
    }

    public class GetAddedSinceRequest : IRequest<List<RunInfo>>
    {
        private readonly long time;

        public GetAddedSinceRequest(long time)
        {
            this.time = time;
        }

        public void SendHeader(Client client)
        {
            client.Send(time);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_ADDED_SINCE;
        }
    }

    public class GetDeletedSinceRequest : IRequest<List<int>>
    {
        private readonly long time;

        public GetDeletedSinceRequest(long time)
        {
            this.time = time;
        }

        public void SendHeader(Client client)
        {
            client.Send(time);
        }

        public List<int> Run(Client client)
        {
            List<int> result = new List<int>();
            while (true)
            {
                int id = client.Receive<int>();
                if (id == -1)
                    break;
                result.Add(id);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_DELETED_SINCE;
        }
    }

    public class GetEventsRequest : IRequest<Dictionary<ulong, MapEvent>>
    {
        public GetEventsRequest()
        {

        }

        public void SendHeader(Client client)
        {

        }

        public Dictionary<ulong, MapEvent> Run(Client client)
        {
            Dictionary<ulong, MapEvent> result = new Dictionary<ulong, MapEvent>();
            while (true)
            {
                ulong mapId = client.Receive<ulong>();
                long from = client.Receive<long>();
                long to = client.Receive<long>();
                ulong categoryType = client.Receive<ulong>();
                ulong winner = client.Receive<ulong>();
                if (mapId == ulong.MaxValue)
                    break;
                result.Add(mapId, new MapEvent { From = from, To = to, CategoryType = (ECategoryType)categoryType, Winner = winner });
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_EVENTS;
        }
    }

    public class GetPopularityOrderRequest : IRequest<List<ulong>>
    {
        private readonly int type;

        public GetPopularityOrderRequest(int type)
        {
            this.type = type;
        }

        public void SendHeader(Client client)
        {
            client.Send(type);
        }

        public List<ulong> Run(Client client)
        {
            List<ulong> result = new List<ulong>();
            while (true)
            {
                ulong current = client.Receive<ulong>();
                if (current == ulong.MaxValue)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_POPULARITY_ORDER;
        }
    }

    public class GetPopularThisWeekRequest : IRequest<List<RunInfo>>
    {
        public GetPopularThisWeekRequest()
        {

        }

        public void SendHeader(Client client)
        {

        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_POPULAR_THIS_WEEK;
        }
    }

    public class GetEventWRsRequest : IRequest<List<RunInfo>>
    {
        public GetEventWRsRequest()
        {

        }

        public void SendHeader(Client client)
        {

        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_EVENT_WRS;
        }
    }

    public class GetPlayerEventPBsRequest : IRequest<List<RunInfo>>
    {
        private readonly ulong playerId;

        public GetPlayerEventPBsRequest(ulong playerId)
        {
            this.playerId = playerId;
        }

        public void SendHeader(Client client)
        {
            client.Send(playerId);
        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                RunInfo current = client.Receive<RunInfo>();
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_PLAYER_EVENT_PBS;
        }
    }

    public class SendStacktraceRequest : IRequest<string>
    {
        private readonly string stacktrace;

        public SendStacktraceRequest(string stacktrace)
        {
            this.stacktrace = stacktrace;
        }

        public void SendHeader(Client client)
        {

        }

        public string Run(Client client)
        {
            client.Send(stacktrace);
            client.SendCrc();

            return "";
        }

        public uint RequestType()
        {
            return (uint)ERequestType.SEND_STACKTRACE;
        }
    }

    public class VeloUpdate
    {
        public string VersionName;
        public string Filename;
        public byte[] Bytes;
    }

    public class CheckUpdateRequest : IRequest<VeloUpdate>, IVersionBypassingRequest
    {
        public CheckUpdateRequest()
        {

        }

        public void SendHeader(Client client)
        {
            client.Send(Version.VERSION);
        }

        public VeloUpdate Run(Client client)
        {
            int newVersion = client.Receive<int>();
            if (newVersion == 0)
            {
                client.VerifyCrc();
                return null;
            }

            VeloUpdate update = new VeloUpdate
            {
                VersionName = client.ReceiveStr(),
                Filename = client.ReceiveStr()
            };

            int fileSize = client.Receive<int>();

            update.Bytes = new byte[fileSize];
            client.Receive(update.Bytes);

            client.VerifyCrc();

            return update;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.CHECK_UPDATE;
        }
    }

    public struct SpeedrunComPlayer
    {
        public ulong PseudoSteamId;
        public string Name;
    }

    public class GetSpeedrunComPlayersRequest : IRequest<List<SpeedrunComPlayer>>
    {
        public GetSpeedrunComPlayersRequest()
        {

        }

        public void SendHeader(Client client)
        {

        }

        public List<SpeedrunComPlayer> Run(Client client)
        {
            List<SpeedrunComPlayer> players = new List<SpeedrunComPlayer>();
            SpeedrunComPlayer player;
            while (true)
            {
                player.PseudoSteamId = client.Receive<ulong>();
                player.Name = client.ReceiveStr();
                if (player.PseudoSteamId == 0)
                    break;
                players.Add(player);
            }
            client.VerifyCrc();
            return players;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_SPEEDRUN_COM_PLAYERS;
        }
    }
}
