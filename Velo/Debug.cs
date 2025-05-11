using System;
using System.IO;

namespace Velo
{
    public class Debug
    {
#pragma warning disable IDE1006
        public static void writeStackTrace(Exception ex)
        {
            StreamWriter writer = new StreamWriter("stacktrace.txt");
            writer.WriteLine(ex.Message);
            writer.WriteLine(ex.ToString());
            writer.Close();

            RequestHandler handler = new RequestHandler();
            handler.Push(new SendStacktraceRequest(ex.Message + "\n" + ex.ToString()));
            handler.Run(null, null, true);
        }
#pragma warning restore IDE1006
    }
}
