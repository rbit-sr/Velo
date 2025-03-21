namespace Velo
{
    public class Version
    {
        public static readonly ushort VERSION = 70;
        public static readonly string VERSION_NAME = "2.2.27b";
        public static readonly string AUTHOR = "rbit, olsu";

        public static readonly ushort MIN_SAVESTATE_VERSION = 35;

        public static int[] ToIntArr(string version)
        {
            int[] numbers = new int[4];
            string[] parts = version.Split('.');
            for (int i = 0; i < parts.Length && i < 4; i++)
            {
                int number;
                if (parts[i] == "a")
                    number = 1;
                else if (parts[i] == "b")
                    number = 2;
                else if (parts[i] == "c")
                    number = 3;
                else if (parts[i] == "d")
                    number = 4;
                else
                    int.TryParse(parts[i], out number);
                numbers[i] = number;
            }
            return numbers;
        }

        public static int Compare(string version1, string version2)
        {
            int[] numbers1 = ToIntArr(version1);
            int[] numbers2 = ToIntArr(version2);

            for (int i = 0; i < 4; i++)
            {
                int c = numbers1[i].CompareTo(numbers2[i]);
                if (c != 0)
                    return c;
            }
            return 0;
        }
    }
}
