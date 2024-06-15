using System;
using System.Collections.Generic;

namespace Velo
{
    public class Map
    {
        public static readonly int COUNT = 55;

        private static readonly Dictionary<string, int> AllowedMapsNames = new Dictionary<string, int>
        {
            { "Metro", 0 },
            { "SS Royale", 1 },
            { "Mansion", 2 },
            { "Plaza", 3 },
            { "Factory", 4 },
            { "Theme Park", 5 },
            { "powerplant", 6 },
            { "Silo", 7 },
            { "Library", 8 },
            { "Nightclub", 9 },
            { "Zoo", 10 },
            { "Swift Peaks", 11 },
            { "Casino", 12 },
            { "Festival", 13 },
            { "Resort", 14 },
            { "Airport", 15 },
            { "Lab v2", 16 },
            { "Citadel", 17 },
            { "City Run R2", 18 },
            { "Club House", 19 },
            { "Club V", 20 },
            { "Coastline", 21 },
            { "Dance Hall", 22 },
            { "Dash the night", 23 },
            { "Disco Lounge", 24 },
            { "Dragon City", 25 },
            { "Genetics", 26 },
            { "Gift Store", 27 },
            { "Granary", 28 },
            { "Lunar Colony", 29 },
            { "Minery", 30 },
            { "New Age City", 31 },
            { "Oasis", 32 },
            { "Oasis - Abyss", 33 },
            { "Oceanslide", 34 },
            { "Pitfall", 35 },
            { "Plantation", 36 },
            { "Shore Enuff", 37 },
            { "Snowed In", 38 },
            { "Sound Shiver", 39 },
            { "SpeedCity Nights", 40 },
            { "Surfing in the oasis", 41 },
            { "Terminal", 42 },
            { "Texas Run'em", 43 },
            { "Void", 44 }
        };

        private static readonly Dictionary<ulong, int> AllowedMapsFileIds = new Dictionary<ulong, int>
        {
            { 459395096UL, 45 }, // Arctic
            { 355112213UL, 46 }, // Boardwalk
            { 366868469UL, 47 }, // Mall
            { 684228943UL, 48 }, // Metalworks
            { 373772831UL, 49 }, // Park
            { 318216468UL, 50 }, // Rainx Laboratory
            { 726451361UL, 51 }, // Rush Ring
            { 947525541UL, 52 }, // Safari Park r45
            { 510517129UL, 53 }, // SubWave 1.4
            { 389154478UL, 54 } // Winds Peak
        };

        public static string[] MapIdToName = new Func<string[]>(() =>
        {
            string[] names = new string[AllowedMapsNames.Count + AllowedMapsFileIds.Count];
            foreach (var pair in AllowedMapsNames)
            {
                names[pair.Value] = pair.Key;
            }
            names[6] = "Powerplant";
            names[16] = "Laboratory";
            names[45] = "Arctic";
            names[46] = "Boardwalk";
            names[47] = "Mall";
            names[48] = "Metalworks";
            names[49] = "Park";
            names[50] = "Rainx Laboratory";
            names[51] = "Rush Ring";
            names[52] = "Safari Park r45";
            names[53] = "SubWave 1.4";
            names[54] = "Winds Peak";
            return names;
        })();

        public static Dictionary<int, ulong> MapIdToFileId = new Func<Dictionary<int, ulong>>(() =>
        {
            Dictionary<int, ulong> dict = new Dictionary<int, ulong>();
            foreach (var pair in AllowedMapsFileIds)
            {
                dict.Add(pair.Value, pair.Key);
            }
            return dict;
        })();

        public static int GetCurrentMapId()
        {
            if (Velo.ModuleSolo == null)
                return -1;
            if (Velo.ModuleSolo.LevelData == null)
                return -1;

            Console.WriteLine();
            
            string mapName = Velo.ModuleSolo.LevelData.name;
            string mapAuthor = Velo.ModuleSolo.LevelData.author;

            if (mapAuthor == "Casper van Est" || mapAuthor == "Gert-Jan Stolk" || mapAuthor == "dd_workshop")
            {
                if (AllowedMapsNames.ContainsKey(mapName))
                    return AllowedMapsNames[mapName];
                return -1;
            }

            if (Velo.ModuleSolo.gameInfo.unknown1 != null)
            {
                ulong fileId = ((PublishedFileId)Velo.ModuleSolo.gameInfo.unknown1.publishedFileId).published_file_id.m_PublishedFileId;
                if (AllowedMapsFileIds.ContainsKey(fileId))
                    return AllowedMapsFileIds[fileId];
                return -1;
            }

            return -1;
        }

        public static bool IsOfficial(int mapId)
        {
            return mapId <= 16;
        }

        public static bool IsRWS(int mapId)
        {
            return mapId >= 17 && mapId <= 44;
        }

        public static bool IsOldRWS(int mapId)
        {
            return mapId >= 45 && mapId <= 54;
        }

        public static bool HasBoostaCoke(int mapId)
        {
            return
                mapId == 16;
        }

        public static bool HasMovingLaser(int mapId)
        {
            return
                mapId == 6 || mapId == 8;
        }

        public static bool HasSkip(int mapId)
        {
            return
                mapId == 0 ||
                mapId == 1 ||
                mapId == 3 ||
                mapId == 4 ||
                mapId == 5 ||
                mapId == 8 ||
                mapId == 10 ||
                mapId == 13 ||
                mapId == 20 ||
                mapId == 23 ||
                mapId == 26 ||
                mapId == 29 ||
                mapId == 44 ||
                mapId == 50 ||
                mapId == 51;
        }
    }
}
