using Steamworks;
using System;
using System.Collections.Generic;

namespace Velo
{
    public class Map
    {
        public static readonly ulong CURATED_COUNT = 85;

        // contains curated maps (official and RWS and origins) by level title and maps it to their Velo map ID
        private static readonly Dictionary<string, ulong> CuratedMapNameToId = new Dictionary<string, ulong>
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

        // contains curated maps (old RWS) by file IDs and maps it to their Velo map ID
        private static readonly Dictionary<ulong, ulong> CuratedMapFileIdToId = new Dictionary<ulong, ulong>
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

        public static readonly ulong ORIGINS_START = 55;
        public static readonly ulong ORIGINS_END = 85;

        // contains curated maps (Origins) by file names and maps it to their Velo map ID
        private static readonly Dictionary<string, ulong> CuratedMapFileNameToId = new Dictionary<string, ulong>
        {
            { "Prologue", 55 },
            { "Level1", 56 },
            { "Level2", 57 },
            { "Level2b", 58 },
            { "B1. Boss1", 59 },
            { "Level3", 60 },
            { "Level4", 61 },
            { "Level4b", 62 },
            { "Level5", 63 },
            { "Level5b", 64 },
            { "B2. Boss2", 65 },
            { "Level6", 66 },
            { "Level7", 67 },
            { "Special2", 68 },
            { "Level8", 69 },
            { "Level8b", 70 },
            { "Level8c", 71 },
            { "B4. Boss4", 72 },
            { "S3. Sponsor3", 73 },
            { "S2. Sponsor2", 74 },
            { "S1. Sponsor1", 75 },
            { "S4. Sponsor4", 76 },
            { "S5. Sponsor5", 77 },
            { "N1. New1", 78 },
            { "N2. New2", 79 },
            { "N3. New3", 80 },
            { "N4. New4", 81 },
            { "Special1", 82 },
            { "X. Final", 83 },
            { "N5. New5", 84 }
        };

        // maps Velo map IDs to their corresponding map name
        private static readonly string[] curatedMapIdToName = new Func<string[]>(() =>
        {
            string[] names = new string[CURATED_COUNT];
            foreach (var pair in CuratedMapNameToId)
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
            names[55] = "Prologue";
            names[56] = "Another Bomb";
            names[57] = "More bombs";
            names[58] = "For Science!";
            names[59] = "The Chase";
            names[60] = "The City";
            names[61] = "Rooftops";
            names[62] = "Boobytraps";
            names[63] = "To the Tower";
            names[64] = "Going up..";
            names[65] = "The Escape";
            names[66] = "Hide & Seek";
            names[67] = "Finding Stalnik";
            names[68] = "Treacherous Traps";
            names[69] = "Almost There";
            names[70] = "Hide & Seek II";
            names[71] = "Final Defences";
            names[72] = "The Finale";
            names[73] = "VR Mission I";
            names[74] = "VR Mission II";
            names[75] = "VR Mission III";
            names[76] = "VR Mission IV";
            names[77] = "VR Mission V";
            names[78] = "VR Mission VI";
            names[79] = "VR Mission VII";
            names[80] = "VR Mission VIII";
            names[81] = "VR Mission IX";
            names[82] = "VR Mission X";
            names[83] = "Final Challenge";
            names[84] = "Unused";
            return names;
        })();

        public static string MapIdToName(ulong id)
        {
            if (id == ulong.MaxValue)
                return "[unknown]";

            if (id < CURATED_COUNT)
                return curatedMapIdToName[id];

            return SteamCache.FileIdToName(id);
        }

        // maps old RWS Velo map IDs to their corresponding file ID
        public static Dictionary<ulong, ulong> MapIdToFileId = new Func<Dictionary<ulong, ulong>>(() =>
        {
            Dictionary<ulong, ulong> dict = new Dictionary<ulong, ulong>();
            foreach (var pair in CuratedMapFileIdToId)
            {
                dict.Add(pair.Value, pair.Key);
            }
            return dict;
        })();

        // maps Origins Velo map IDS to their corresponding file names
        public static Dictionary<ulong, string> MapIdToFileName = new Func<Dictionary<ulong, string>>(() =>
        {
            Dictionary<ulong, string> dict = new Dictionary<ulong, string>();
            foreach (var pair in CuratedMapFileNameToId)
            {
                dict.Add(pair.Value, pair.Key);
            }
            return dict;
        })();

        public static ulong GetCurrentMapId()
        {
            if (Velo.ModuleSolo == null)
                return ulong.MaxValue;
            if (Origins.Instance.IsOrigins())
                return Origins.Instance.Current;
            if (Velo.ModuleSolo.LevelData == null)
                return ulong.MaxValue;
            
            string mapName = Velo.ModuleSolo.LevelData.name;
            string mapAuthor = Velo.ModuleSolo.LevelData.author;

            // check for official or RWS
            if (mapAuthor == "Casper van Est" || mapAuthor == "Gert-Jan Stolk" || mapAuthor == "dd_workshop")
            {
                if (CuratedMapNameToId.ContainsKey(mapName))
                    return CuratedMapNameToId[mapName];
            }

            if (Velo.ModuleSolo.gameInfo.unknown1 != null)
            {
                ulong fileId = ((PublishedFileId)Velo.ModuleSolo.gameInfo.unknown1.publishedFileId).published_file_id.m_PublishedFileId;
                // check for old RWS
                if (CuratedMapFileIdToId.ContainsKey(fileId))
                    return CuratedMapFileIdToId[fileId];
                if (fileId == 0)
                    return ulong.MaxValue;
                
                // non Velo curated
                return fileId;
            }

            return ulong.MaxValue;
        }

        public static bool IsOfficial(ulong mapId)
        {
            return mapId <= 16;
        }

        public static bool IsRWS(ulong mapId)
        {
            return mapId >= 17 && mapId <= 44;
        }

        public static bool IsOldRWS(ulong mapId)
        {
            return mapId >= 45 && mapId <= 54;
        }

        public static bool IsOrigins(ulong mapId)
        {
            return mapId >= 55 && mapId <= 84;
        }

        public static bool IsOther(ulong mapId)
        {
            return mapId >= 10000;
        }

        public static bool HasBoostaCoke(ulong mapId)
        {
            return
                mapId == 16;
        }

        public static bool HasMovingLaser(ulong mapId)
        {
            return
                mapId == 6 || mapId == 8;
        }

        public static bool HasSkip(ulong mapId)
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
                mapId == 24 ||
                mapId == 26 ||
                mapId == 29 ||
                mapId == 39 ||
                mapId == 44 ||
                mapId == 50 ||
                mapId == 51;
        }

        public static bool AllowBombSmiley(ulong mapId)
        {
            return
                mapId != 3 &&
                mapId != 6 &&
                mapId != 17;
        }

        public static bool AllowDrill(ulong mapId)
        {
            return
                mapId != 3 &&
                mapId != 17;
        }

        public static bool Has100Perc(ulong mapId)
        {
            return
                mapId == 55 ||
                mapId == 56 ||
                mapId == 57 ||
                mapId == 58 ||
                mapId == 60 ||
                mapId == 61 ||
                mapId == 62 ||
                mapId == 63 ||
                mapId == 64 ||
                mapId == 66 ||
                mapId == 67 ||
                mapId == 68 ||
                mapId == 69 ||
                mapId == 70 ||
                mapId == 71;
        }

        public static int SandalCount(ulong mapId)
        {
            switch (mapId)
            {
                case 55:
                case 56:
                case 57:
                case 58:
                    return 2;
                case 60:
                case 61:
                case 62:
                case 63:
                case 64:
                case 66:
                case 67:
                case 68:
                case 69:
                case 70:
                case 71:
                    return 3;
                default:
                    return 0;
            }
        }
    }
}
