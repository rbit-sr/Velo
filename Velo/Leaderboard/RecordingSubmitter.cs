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
            private readonly RequestHandler<NewPbInfo> handler;
            private readonly Task writeFileTask;
            private readonly string path;
            private RunInfo tempInfo;

            public SubmitRequest(Recording recording, string path)
            {
                this.path = path;
                if (this.path == "")
                {
                    string id = Guid.NewGuid().ToString();
                    this.path = "Velo\\pending\\" + id + ".temp";

                    writeFileTask = Task.Run(() =>
                    {
                        try
                        {
                            if (!Directory.Exists("Velo\\pending"))
                                Directory.CreateDirectory("Velo\\pending");

                            using (FileStream stream = new FileStream(this.path, FileMode.Create, FileAccess.Write))
                            {
                                recording.Write(stream);
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
                    if (i < 3)
                        return 0;
                    else if (i < 6)
                        return 20 * 1000;
                    else
                        return 5 * 60 * 1000;
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
                Task.Run(() =>
                {
                    if (writeFileTask != null)
                        writeFileTask.Wait();
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
                        RunsDatabase.Instance.Add(new[] { result.RunInfo });
                        string message;
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
            if (!recording.Rules.Valid())
                return;

            RunInfo pb = RunsDatabase.Instance.GetPB(Steamworks.SteamUser.GetSteamID().m_SteamID, recording.Info.MapId, (ECategory)recording.Info.Category);

            if (pb.Id != -1 && recording.Info.RunTime >= pb.RunTime)
                return;

            string message = "Submitting as ";
            message += ((ECategory)recording.Info.Category).Label();
            message += "...";
            Notifications.Instance.PushNotification(message);

            requests.Add(new SubmitRequest(recording, path));
        }
    }
}
