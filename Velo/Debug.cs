using System;
using System.IO;

namespace Velo
{
    public class Debug
    {
        public static void writeStackTrace(Exception ex)
        {
            StreamWriter writer = new StreamWriter("stacktrace.txt");
            writer.WriteLine(ex.Message);
            writer.WriteLine(ex.ToString());
            writer.Close();
        }
    }
}
