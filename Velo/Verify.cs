using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Velo
{
    public class Verify
    {
        [DllImport("VeloVerifier.dll", EntryPoint = "verify")]
        private static extern bool verify(IntPtr file, int file_len, IntPtr hash);
        
        public static bool VerifyFiles(out Dictionary<string, bool> result)
        {
            result = new Dictionary<string, bool>();
            if (!File.Exists("files.dat"))
                return true;
            string[] fileHashes = File.ReadAllLines("files.dat");
            foreach (string fileHash in fileHashes)
            {
                if (fileHash.Length == 0)
                    continue;
                string[] fileAndHash = fileHash.Split(':');
                unsafe
                {
                    fixed (byte* fileBytes = Encoding.ASCII.GetBytes(fileAndHash[0]))
                    {
                        fixed (byte* hashBytes = Encoding.ASCII.GetBytes(fileAndHash[1]))
                        {
                            try
                            {
                                result.Add(fileAndHash[0], File.Exists(fileAndHash[0]) && verify((IntPtr)fileBytes, 32, (IntPtr)hashBytes));
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }
            return result.Values.All(b => b);
        }
    }
}
