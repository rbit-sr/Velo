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
        }
#pragma warning restore IDE1006
    }
}
