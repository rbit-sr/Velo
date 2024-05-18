using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Velo
{
    public class OutdatedVersionException : Exception
    {
        public OutdatedVersionException() :
            base("Outdated Client! Please update Velo to the latest version!") { }
    }

    public enum ERequestType : uint
    {
        GET_PBS_FOR_MAP_CAT, GET_PLAYER_PBS, GET_WRS, GET_RECENT, SUBMIT_RUN, GET_RECORDING, SEND_PLAYER_NAME
    }

    public enum ERequestStatus
    {
        NONE, PENDING, SUCCESS, FAILURE
    }

    public class Result<T>
    {
        public T Value;
        public Exception Error;

        public Result(T value)
        {
            Value = value;
        }

        public Result(Exception error)
        {
            Error = error;
        }
    }

    public class RequestHandler<T>
    {
        public static readonly uint VERSION = 0;

        private Task<Result<T>> task;
        private CancellationTokenSource cancel;
        private bool statusChanged = false;

        private readonly int maxAttempts;
        private readonly Func<int, int> attemptWaitTimes;

        public RequestHandler(int maxAttempts = 3, Func<int, int> attemptWaitTimes = null)
        {
            this.maxAttempts = maxAttempts;
            this.attemptWaitTimes = attemptWaitTimes;
        }

        public void Run(IRequest<T> request, Action<T> onSuccess = null, Action<Exception> onFailure = null)
        {
            Cancel();

            cancel = new CancellationTokenSource();
            statusChanged = true;

            task = Task.Run(() =>
            {
                Result<T> result = null;
                for (int i = 0; i < maxAttempts; i++)
                {
                    Client client = new Client(cancel.Token);
                    try
                    {
                        client.Connect();
                        client.Send(request.RequestType() | (VERSION << 16));
                        client.SendCrc();
                        int success = 0;
                        client.Receive(ref success);
                        client.VerifyCrc();
                        if (success != int.MaxValue)
                            throw new OutdatedVersionException();

                        result = new Result<T>(request.Run(client));
                        client.ReceiveSuccess();
                    }
                    catch (Exception e)
                    {
                        result = new Result<T>(e);
                    }
                    finally
                    {
                        client.Close();
                    }
                    if (result.Value != null)
                    {
                        if (onSuccess != null)
                        {
                            Velo.AddOnPreUpdate(() =>
                            {
                                onSuccess(result.Value);
                            });
                        }
                        return result;
                    }

                    if (attemptWaitTimes != null && i + 1 < maxAttempts)
                    {
                        int millis = attemptWaitTimes(i);
                        Task.Delay(millis).Wait();
                    }
                }
                if (onFailure != null)
                {
                    Velo.AddOnPreUpdate(() =>
                    {
                        onFailure(result.Error);
                    });
                }
                return result;
            });
        }

        public void Cancel()
        {
            if (task != null && !task.IsCompleted)
            {
                cancel.Cancel();
                cancel.Dispose();
                task = null;
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
                if (task.Result.Error == null)
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

        public T Result
        {
            get { return task.Result.Value; }
        }

        public Exception Error
        {
            get { return task.Result.Error; }
        }
    }

    public interface IRequest<T>
    {
        T Run(Client client);
        uint RequestType();
    }

    public class GetPBsForMapCatRequest : IRequest<List<RunInfo>>
    {
        private readonly int mapId;
        private readonly ECategory category;

        public GetPBsForMapCatRequest(int mapId, ECategory category)
        {
            this.mapId = mapId;
            this.category = category;
        }

        public List<RunInfo> Run(Client client)
        {
            client.Send(mapId);
            client.Send((int)category);
            client.SendCrc();

            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
                client.Receive(buffer, buffer.Length);
                RunInfo current = RunInfo.FromBytes(buffer);
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_PBS_FOR_MAP_CAT;
        }
    }

    public class GetPlayerPBs : IRequest<List<RunInfo>>
    {
        private readonly ulong playerId;

        public GetPlayerPBs(ulong playerId)
        {
            this.playerId = playerId;
        }

        public List<RunInfo> Run(Client client)
        {
            client.Send(playerId);
            client.SendCrc();

            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
                client.Receive(buffer, buffer.Length);
                RunInfo current = RunInfo.FromBytes(buffer);
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

    public class GetWRsRequest : IRequest<List<RunInfo>>
    {
        public GetWRsRequest()
        {

        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
                client.Receive(buffer, buffer.Length);
                RunInfo current = RunInfo.FromBytes(buffer);
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

    public class GetRecentRequest : IRequest<List<RunInfo>>
    {
        public GetRecentRequest()
        {

        }

        public List<RunInfo> Run(Client client)
        {
            List<RunInfo> result = new List<RunInfo>();
            while (true)
            {
                byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
                client.Receive(buffer, buffer.Length);
                RunInfo current = RunInfo.FromBytes(buffer);
                if (current.Id == -1)
                    break;
                result.Add(current);
            }

            client.VerifyCrc();

            return result;
        }

        public uint RequestType()
        {
            return (uint)ERequestType.GET_RECENT;
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

        public NewPbInfo Run(Client client)
        {
            byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
            RunInfo.GetBytes(recording.Info, buffer);
            client.Send(buffer, buffer.Length);
            client.SendCrc();

            int timeSave = 0;
            int newWr = 0;
            client.Receive(ref timeSave);
            
            if (timeSave == -1)
            {
                client.VerifyCrc();
                return new NewPbInfo { RunInfo = new RunInfo { Id = -1 }, TimeSave = 0 };
            }

            client.Receive(ref newWr);
            client.VerifyCrc();

            MemoryStream dataStream = new MemoryStream();
            recording.Write(dataStream);

            byte[] bytes = dataStream.ToArray();
            client.Send(bytes.Length);
            client.Send(bytes, bytes.Length);

            client.SendCrc();

            buffer = new byte[Marshal.SizeOf<RunInfo>()];
            client.Receive(buffer, buffer.Length);
            client.VerifyCrc();
            RunInfo result = RunInfo.FromBytes(buffer);

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

        public Recording Run(Client client)
        {
            client.Send(id);
            client.SendCrc();

            Recording result = new Recording();

            int size = 0;
            client.Receive(ref size);
            byte[] data = new byte[size];
            client.Receive(data, size);

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

        public string Run(Client client)
        {
            ulong steamId = Steamworks.SteamUser.GetSteamID().m_SteamID;
            client.Send(steamId);
            client.Send(SteamCache.GetName(steamId));
            client.SendCrc();

            return "";
        }

        public uint RequestType()
        {
            return (uint)ERequestType.SEND_PLAYER_NAME;
        }
    }
}
