using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CEngine.World.Actor;

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
            { "Powerplant", 6 },
            { "Silo", 7 },
            { "Library", 8 },
            { "Nightclub", 9 },
            { "Zoo", 10 },
            { "Swift Peaks", 11 },
            { "Casino", 12 },
            { "Festival", 13 },
            { "Resort", 14 },
            { "Airport", 15 },
            { "Laboratory", 16 },
            { "Citadel|Bensen", 17 },
            { "City Run R2|JM-Anime", 18 },
            { "Club House|SixthHeaven", 19 },
            { "Club V|Derp", 20 },
            { "Coastline|DistinctMadness", 21 },
            { "Dance Hall|Derp and Robin", 22 },
            { "Dash the night|Touki", 23 },
            { "Disco Lounge|Redz", 24 },
            { "Dragon City|Decard Cain", 25 },
            { "Genetics|Bunii", 26 },
            { "Gift Store|DistinctMadness", 27 },
            { "Granary|DistinctMadness", 28 },
            { "Lunar Colony|Derpiculous", 29 },
            { "Minery|Incursio", 30 },
            { "New Age City|Decard Cain", 31 },
            { "Oasis|Retro36", 32 },
            { "Oasis - Abyss|Retro36 and AmazingPineapple", 33 },
            { "Oceanslide|Plastic Bleach & Faith", 34 },
            { "Pitfall|KChadowsky, ZombieWizzard and Plastic Shiplord", 35 },
            { "Plantation|Plastic Bleach", 36 },
            { "Shore Enuff|Flaccid Cucumber", 37 },
            { "Snowed In|Plastic Shiplord, Glyme & DistinctMadness", 38 },
            { "Sound Shiver|GraphiqueNez2", 39 },
            { "SpeedCity Nights|JM-Anime", 40 },
            { "Surfing in the oasis|Retro36", 41 },
            { "Terminal|Redz", 42 },
            { "Texas Run'em|TaTa", 43 },
            { "Void|Qwerty", 44 }
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
                return names;
            })();

        public static int GetCurrentMapId()
        {
            if (Velo.ModuleSolo == null)
                return -1;

            string mapName = Velo.ModuleSolo.LevelData.name;
            string mapAuthor = Velo.ModuleSolo.LevelData.author;
            if (mapAuthor != "")
                mapName += "|" + mapAuthor.Substring("by ".Length);
            int mapId = -1;
            if (Map.AllowedMaps.ContainsKey(mapName))
                mapId = Map.AllowedMaps[mapName];

            return mapId;
        }
    }
}
