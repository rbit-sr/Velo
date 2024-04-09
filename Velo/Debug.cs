using System;
using System.IO;

namespace Velo
{
    public class Debug
    {
#if !VELO_OLD
#pragma warning disable IDE1006
#endif
        public static void writeStackTrace(Exception ex)
        {
            StreamWriter writer = new StreamWriter("stacktrace.txt");
            writer.WriteLine(ex.Message);
            writer.WriteLine(ex.ToString());
            writer.Close();
        }
    }
}
