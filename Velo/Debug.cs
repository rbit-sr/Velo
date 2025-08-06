using System;
using System.IO;

namespace Velo
{
    public class Debug
    {
#pragma warning disable IDE1006
        public static void writeStackTrace(Exception ex)
        {
            using (StreamWriter writer = new StreamWriter("stacktrace.txt"))
            {
                writer.WriteLine(ex.Message);
                writer.WriteLine(ex.ToString());
            }

            IRecorder recorder = OfflineGameMods.Instance.RecordingAndReplay.Recorder;
            if (recorder is TASRecorder tasRecorder && tasRecorder.NeedsSave)
                tasRecorder.Save(false, recover: true);

            RequestHandler handler = new RequestHandler();
            handler.Push(new SendStacktraceRequest(ex.Message + "\n" + ex.ToString()));
            handler.Run(null, null, true);
        }
#pragma warning restore IDE1006
    }
}
