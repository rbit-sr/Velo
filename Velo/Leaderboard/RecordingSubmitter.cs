using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;

namespace Velo
{
    public class RecordingSubmitter : Module
    {
        private class SubmitRequest
        {
            private readonly Recording recording;
            private string path;
            private RequestHandler handler;
            private Task task;
            private RunInfo tempInfo;

            public SubmitRequest(Recording recording, string path)
            {
                this.recording = recording;
                this.path = path;
            }

            public void Run()
            {
                task = Task.Run(() =>
                {
                    byte[] bytes = recording.ToBytes();
                    if (recording.Sign == null)
                    {
                        recording.GenerateSign(bytes);
                    }

                    handler = new RequestHandler(int.MaxValue, (i) =>
                    {
                        if (i < 3)
                            return 0;
                        else if (i < 6)
                            return 20 * 1000;
                        else
                            return 5 * 60 * 1000;
                    });
                    tempInfo = recording.Info;
                    RunsDatabase.Instance.AddPending(ref tempInfo);
                    if (Map.IsOther(recording.Info.Category.MapId))
                    {
                        handler.Push(new SendMapNameRequest(recording.Info.Category.MapId));
                    }
                    handler.Push(new SubmitRunRequest(recording, bytes), result =>
                    {
                        Result = result;
                    });
                    handler.Run();

                    if (path == "")
                    {
                        string id = Guid.NewGuid().ToString();
                        path = "Velo\\pending\\" + id + ".temp";

                        if (!Directory.Exists("Velo\\pending"))
                            Directory.CreateDirectory("Velo\\pending");

                        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                        {
                            stream.Write(bytes, 0, bytes.Length);
                            stream.Write(recording.Sign, 0, recording.Sign.Length);
                        }
                    }
                });
            }

            public NewPbInfo Result { get; set; }

            public RunInfo TempInfo
            {
                get { return tempInfo; }
            }

            public void DeleteFile()
            {
                Task.Run(() =>
                {
                    task?.Wait();
                    if (File.Exists(path))
                        File.Delete(path);
                });
            }
        }

        private readonly List<SubmitRequest> requests = new List<SubmitRequest>();

        public RecordingSubmitter() : base("Recording Submitter")
        {
            
        }

        public static RecordingSubmitter Instance = new RecordingSubmitter();

        public override void Init()
        {
            base.Init();

            if (Leaderboard.Instance.DisableLeaderboard.Value)
                return;

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
                    RunsDatabase.Instance.Remove(request.TempInfo.Id, false);
                    if (result.RunInfo.Id != -1)
                    {
                        RunsDatabase.Instance.Add(result.RunInfo, true);
                        string message;
                        if (result.NewWr)
                            message = "New WR!";
                        else
                            message = "New PB!";

                        if (result.TimeSave > 0)
                            message += " (-" + Util.FormatTime(result.TimeSave, Leaderboard.Instance.TimeFormat.Value) + ")";

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
                    {
                        recording.Read(stream);
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
            if (!recording.Rules.Valid || recording.Info.Id == -50)
                return;

            RunInfo pb = RunsDatabase.Instance.GetPB(Steamworks.SteamUser.GetSteamID().m_SteamID, recording.Info.Category);

            if (pb.Id != -1 && recording.Info.RunTime >= pb.RunTime)
                return;

            string message = "Submitting as ";
            message += ((ECategoryType)recording.Info.Category.TypeId).Label();
            message += "...";
            Notifications.Instance.PushNotification(message);

            SubmitRequest request = new SubmitRequest(recording, path);
            requests.Add(request);
            request.Run();
        }
    }
}
