using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.Xna.Framework;

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
        SEND_MAP_NAME
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

        public void Run(Action<Exception> onFailure = null, bool wait = false)
        {
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

                        ulong time = client.Receive<ulong>();

                        if (cancelToken.IsCancellationRequested)
                            throw new OperationCanceledException();
                        Time = time;

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
            requests = new List<RequestEntry>();
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

        public ulong Time { get; set; }
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
        public GetWRsRequest()
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
            return (uint)ERequestType.GET_WRS;
        }
    }

    public class GetWRsNonCuratedRequest : IRequest<List<RunInfo>>
    {
        public GetWRsNonCuratedRequest()
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
        public bool NewWr;
    }

    public class SubmitRunRequest : IRequest<NewPbInfo>
    {
        private readonly Recording recording;

        public SubmitRunRequest(Recording recording)
        {
            this.recording = recording;
        }

        public void SendHeader(Client client)
        {
            
        }

        public NewPbInfo Run(Client client)
        {
            long deltaSum = 0;
            for (int i = recording.LapStart; i < recording.Count; i++)
            {
                deltaSum += recording[i].Delta;
            }
            float avgFramerate = 1f / (float)new TimeSpan(deltaSum / (recording.Count - 1 - recording.LapStart)).TotalSeconds;

            client.Send(recording.Info);
            client.Send(recording.Timepoints[recording.LapStart].Ticks);
            client.Send(recording.Timepoints[recording.Count - 1].Ticks);
            client.Send(avgFramerate);
            client.SendCrc();

            if (!Velo.CheckForVerifier(error: true))
                return new NewPbInfo { RunInfo = new RunInfo { Id = -1 }, TimeSave = 0 };

            int salt = client.Receive<int>();
            Task<int> verification = Task.Run(() =>
            {
                try
                {
                    Process process = Process.Start("VeloVerifier.exe", salt + "");
                    process.WaitForExit();
                    return process.ExitCode;
                }
                catch (Exception)
                {
                    return 0;
                }
            });

            int timeSave = client.Receive<int>();
            
            if (timeSave == -1)
            {
                client.VerifyCrc();
                return new NewPbInfo { RunInfo = new RunInfo { Id = -1 }, TimeSave = 0 };
            }
            
            int newWr = client.Receive<int>();
            client.VerifyCrc();

            MemoryStream dataStream = new MemoryStream();
            recording.Write(dataStream);

            byte[] bytes = dataStream.ToArray();
            client.Send(bytes.Length);
            client.Send(bytes);

            verification.Wait();
            if (verification.Result == 0)
            {
                Notifications.Instance.PushNotification(
                    "WARNING: Unable to check for client modifications!\n" +
                    "Your run will be manually verified by a leaderboard moderator.",
                    Color.Lime, TimeSpan.FromSeconds(6d));
            }
            client.Send(verification.Result);

            client.SendCrc();

            int verificationResult = client.Receive<int>();
            if (verificationResult == -1)
            {
                Notifications.Instance.PushNotification(
                    "WARNING: Time manipulation detected!\n" +
                    "Your run will be manually verified by a leaderboard moderator.",
                    Color.Lime, TimeSpan.FromSeconds(6d));
            }
            if (verificationResult == -2)
            {
                Notifications.Instance.PushNotification(
                    "WARNING: Client modifications detected!\n" +
                    "Your run will be manually verified by a leaderboard moderator.",
                    Color.Lime, TimeSpan.FromSeconds(6d));
            }

            RunInfo result = client.Receive<RunInfo>();
            client.VerifyCrc();

            return new NewPbInfo 
            { 
                RunInfo = result, 
                TimeSave = timeSave,
                NewWr = newWr != 0
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
            client.Receive(data);

            MemoryStream dataStream = new MemoryStream(data)
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
        private readonly ulong time;

        public GetAddedSinceRequest(ulong time)
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
        private readonly ulong time;

        public GetDeletedSinceRequest(ulong time)
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
}
