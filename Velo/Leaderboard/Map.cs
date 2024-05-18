using System;
using System.Collections.Generic;

namespace Velo
{
    public class Map
    {
        public static readonly int COUNT = 45;

        public static Dictionary<string, int> AllowedMaps = new Dictionary<string, int>
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

        public static string[] MapIdToName = new Func<string[]>(() =>
            {
                string[] names = new string[AllowedMaps.Count];
                foreach (var pair in AllowedMaps)
                {
                    int delim = pair.Key.IndexOf('|');
                    if (delim == -1)
                        names[pair.Value] = pair.Key;
                    else
                        names[pair.Value] = pair.Key.Substring(0, delim);
                }
                names[6] = "Powerplant";
                names[16] = "Laboratory";
                return names;
            })();

        public static int GetCurrentMapId()
        {
            if (Velo.ModuleSolo == null)
                return -1;
            if (Velo.ModuleSolo.LevelData == null)
                return -1;

            string mapName = Velo.ModuleSolo.LevelData.name;
            string mapAuthor = Velo.ModuleSolo.LevelData.author;
            if (mapAuthor != "Casper van Est" && mapAuthor != "Gert-Jan Stolk" && mapAuthor != "dd_workshop")
                return -1;
            int mapId = -1;
            if (AllowedMaps.ContainsKey(mapName))
                mapId = AllowedMaps[mapName];

            return mapId;
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
                mapId == 44;
        }
    }
}
