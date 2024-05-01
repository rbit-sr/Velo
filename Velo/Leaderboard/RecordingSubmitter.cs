using System.Threading.Tasks;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Velo
{
    public class RecordingSubmitter : Module
    {
        private class SubmitRequest
        {
            private RequestHandler<NewPbInfo> handler;
            private Task writeFileTask;
            private string path;
            private RunInfo tempInfo;

            public SubmitRequest(Recording recording, string path)
            {
                this.path = path;
                if (this.path == "")
                {
                    string id = System.Guid.NewGuid().ToString();
                    this.path = "Velo\\pending\\" + id + ".temp";

                    writeFileTask = Task.Run(() =>
                    {
                        try
                        {
                            if (!Directory.Exists("Velo\\pending"))
                                Directory.CreateDirectory("Velo\\pending");

                            using (FileStream stream = new FileStream(this.path, FileMode.Create, FileAccess.Write))
                            using (BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.ASCII))
                            {
                                byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
                                RunInfo.GetBytes(recording.Info, buffer);
                                writer.Write(buffer);

                                writer.Write(recording.Count * Marshal.SizeOf<Frame>());
                                buffer = new byte[Marshal.SizeOf<Frame>()];
                                for (int i = 0; i < recording.Count; i++)
                                {
                                    Frame.GetBytes(recording[i], buffer);
                                    writer.Write(buffer);
                                }

                                writer.Write(recording.Savestates[0].Chunk.Data.Length);
                                writer.Write(recording.Savestates[0].Chunk.Data);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    });
                }

                handler = new RequestHandler<NewPbInfo>(int.MaxValue, (i) =>
                {
                    if (i < 5)
                        return 0;
                    else if (i < 10)
                        return 5000;
                    else
                        return 60000;
                });
                tempInfo = recording.Info;
                RunsDatabase.Instance.AddPending(ref tempInfo);
                handler.Run(new SubmitRunRequest(recording));
            }

            public NewPbInfo Result
            {
                get
                {
                    if (handler.StatusChanged && handler.Status == ERequestStatus.SUCCESS)
                    {
                        return handler.Result;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public RunInfo TempInfo
            {
                get { return tempInfo; }
            }

            public void DeleteFile()
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private List<SubmitRequest> requests = new List<SubmitRequest>();

        public RecordingSubmitter() : base("Recording Submitter")
        {
            
        }

        public static RecordingSubmitter Instance = new RecordingSubmitter();

        public override void Init()
        {
            base.Init();

            LoadFromFilesAndSubmit();
        }

        public override void PreUpdate()
        {
            base.PreUpdate();

            for (int i = 0; i < requests.Count;)
            {
                SubmitRequest request = requests[i];
                NewPbInfo result = request.Result;
                if (result != null)
                {
                    requests.RemoveAt(i);
                    RunsDatabase.Instance.Remove(request.TempInfo.Id);
                    if (result.TimeSave != -1)
                    {
                        RunsDatabase.Instance.Add(new List<RunInfo> { result.RunInfo });
                        string message = "";
                        if (result.NewWr)
                            message = "New WR!";
                        else
                            message = "New PB!";

                        if (result.TimeSave > 0)
                            message += " (-" + Util.FormatTime(result.TimeSave) + ")";

                        Notifications.Instance.PushNotification(message);
                    }
                    request.DeleteFile();
                }
                else
                    i++;
            }
        }

        public void LoadFromFilesAndSubmit()
        {
            if (!Directory.Exists("Velo\\pending"))
                return;

            string[] pendingPaths = Directory.GetFiles("Velo\\pending");
            foreach (string pendingPath in pendingPaths)
            {
                try
                {
                    Recording recording = new Recording();

                    using (FileStream stream = new FileStream(pendingPath, FileMode.Open, FileAccess.Read))
                    using (BinaryReader reader = new BinaryReader(stream, System.Text.Encoding.ASCII))
                    {
                        byte[] buffer = new byte[Marshal.SizeOf<RunInfo>()];
                        stream.ReadExactly(buffer, 0, buffer.Length);
                        recording.Info = RunInfo.FromBytes(buffer);

                        buffer = new byte[Marshal.SizeOf<Frame>()];
                        int count = reader.ReadInt32() / Marshal.SizeOf<Frame>();
                        if (count > 100000)
                            throw new Exception();
                        for (int i = 0; i < count; i++)
                        {
                            stream.ReadExactly(buffer, 0, buffer.Length);
                            recording.PushBack(Frame.FromBytes(buffer), null);
                        }

                        int size = reader.ReadInt32();
                        if (size > 1000000)
                            throw new Exception();
                        recording.Savestates[0] = new Savestate();
                        recording.Savestates[0].Chunk.Data = new byte[size];
                        stream.ReadExactly(recording.Savestates[0].Chunk.Data, 0, size);
                    }

                    Submit(recording, pendingPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public void Submit(Recording recording, string path = "")
        {
            if (!recording.Rules.Valid())
                return;

            RunInfo pb = RunsDatabase.Instance.GetPB(Steamworks.SteamUser.GetSteamID().m_SteamID, recording.Info.MapId, (ECategory)recording.Info.Category);

            if (pb.Id != -1 && recording.Info.RunTime >= pb.RunTime)
                return;

            string message = "Submitting as ";
            switch ((ECategory)recording.Info.Category)
            {
                case ECategory.NEW_LAP:
                    message += "New lap";
                    break;
                case ECategory.ONE_LAP:
                    message += "1 lap";
                    break;
                case ECategory.NEW_LAP_SKIPS:
                    message += "New lap (Skip)";
                    break;
                case ECategory.ONE_LAP_SKIPS:
                    message += "1 lap (Skip)";
                    break;
            }
            message += "...";
            Notifications.Instance.PushNotification(message);

            requests.Add(new SubmitRequest(recording, path));
        }
    }
}
