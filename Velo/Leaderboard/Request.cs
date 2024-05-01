using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Velo
{
    public enum ERequestType : uint
    {
        GET_PBS_FOR_MAP_CAT, GET_PLAYER_PBS, GET_WRS, SUBMIT_RUN, GET_RECORDING, SEND_PLAYER_NAME
    }

    public enum ERequestStatus
    {
        NONE, PENDING, SUCCESS, FAILURE
    }

    public class RequestHandler<T> where T : class
    {
        private Task<T> task;
        private CancellationTokenSource cancel;
        private bool statusChanged = false;

        private int maxAttempts;
        private Func<int, int> attemptWaitTimes;

        public RequestHandler(int maxAttempts = 5, Func<int, int> attemptWaitTimes = null)
        {
            this.maxAttempts = maxAttempts;
            this.attemptWaitTimes = attemptWaitTimes;
        }

        public void Run(IRequest<T> request)
        {
            Cancel();

            cancel = new CancellationTokenSource();
            statusChanged = true;

            task = Task.Run(() =>
            {
                for (int i = 0; i < maxAttempts; i++)
                {
                    T result = null;
                    Client client = new Client(cancel.Token);
                    try
                    {
                        client.Connect();
                        result = request.Run(client);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        client.Close();
                    }
                    if (result != null)
                    {
                        return result;
                    }

                    if (attemptWaitTimes != null && i + 1 < maxAttempts)
                    {
                        int millis = attemptWaitTimes(i);
                        Task.Delay(millis);
                    }
                }
                return null;
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
                if (task.Result != null)
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
            get { return task.Result; }
        }
    }

    public interface IRequest<T> where T : class
    {
        T Run(Client client);
    }

    public class GetPBsForMapCatRequest : IRequest<List<RunInfo>>
    {
        private int mapId;
        private ECategory category;

        public GetPBsForMapCatRequest(int mapId, ECategory category)
        {
            this.mapId = mapId;
            this.category = category;
        }

        public List<RunInfo> Run(Client client)
        {
            client.Send((uint)ERequestType.GET_PBS_FOR_MAP_CAT);
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

            if (!client.ReceiveSuccess())
                return null;

            return result;
        }
    }

    public class GetPlayerPBs : IRequest<List<RunInfo>>
    {
        private ulong playerId;

        public GetPlayerPBs(ulong playerId)
        {
            this.playerId = playerId;
        }

        public List<RunInfo> Run(Client client)
        {
            client.Send((uint)ERequestType.GET_PLAYER_PBS);
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

            if (!client.ReceiveSuccess())
                return null;

            return result;
        }
    }

    public class GetWRsRequest : IRequest<List<RunInfo>>
    {
        public GetWRsRequest()
        {

        }

        public List<RunInfo> Run(Client client)
        {
            client.Send((uint)ERequestType.GET_WRS);
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

            if (!client.ReceiveSuccess())
                return null;

            return result;
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
        private Recording recording;

        public SubmitRunRequest(Recording recording)
        {
            this.recording = recording;
        }

        public NewPbInfo Run(Client client)
        {
            client.Send((uint)ERequestType.SUBMIT_RUN);

            byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
            RunInfo.GetBytes(recording.Info, buffer);
            client.Send(buffer, buffer.Length);

            int timeSave = 0;
            int newWr = 0;
            client.Receive(ref timeSave);
            client.Receive(ref newWr);

            if (timeSave == -1)
            {
                return new NewPbInfo { RunInfo = new RunInfo { Id = -1 }, TimeSave = 0 };
            }

            client.Send(recording.Count * Marshal.SizeOf<Frame>());
            buffer = new byte[Marshal.SizeOf<Frame>()];
            for (int i = 0; i < recording.Count; i++)
            {
                Frame.GetBytes(recording[i], buffer);
                client.Send(buffer, buffer.Length);
            }

            client.Send(recording.Savestates[0].Chunk.Data.Length);
            client.Send(recording.Savestates[0].Chunk.Data, recording.Savestates[0].Chunk.Data.Length);

            client.SendCrc();

            buffer = new byte[Marshal.SizeOf<RunInfo>()];
            client.Receive(buffer, buffer.Length);
            client.VerifyCrc();
            RunInfo result = RunInfo.FromBytes(buffer);

            if (!client.ReceiveSuccess())
                return null;

            return new NewPbInfo 
            { 
                RunInfo = result, 
                TimeSave = timeSave,
                NewWr = newWr != 0
            };
        }
    }

    public class GetRecordingRequest : IRequest<Recording>
    {
        private int id;

        public GetRecordingRequest(int id)
        {
            this.id = id;
        }

        public Recording Run(Client client)
        {
            client.Send((uint)ERequestType.GET_RECORDING);
            client.Send(id);
            client.SendCrc();

            Recording result = new Recording();
            int size = 0;
            client.Receive(ref size);
            result.Frames.Clear();
            byte[] buffer = new byte[Marshal.SizeOf<Frame>()];
            int frameCount = size / buffer.Length;
            for (int i = 0; i < frameCount; i++)
            {
                client.Receive(buffer, buffer.Length);
                result.PushBack(Frame.FromBytes(buffer), null);
            }

            client.Receive(ref size);
            result.Savestates[0] = new Savestate();
            result.Savestates[0].Chunk.Data = new byte[size];
            client.Receive(result.Savestates[0].Chunk.Data, size);

            client.VerifyCrc();

            if (!client.ReceiveSuccess())
                return null;

            return result;
        }
    }
}
