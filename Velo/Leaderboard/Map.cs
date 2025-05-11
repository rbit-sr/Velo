using Steamworks;
using System;
using System.Collections.Generic;
using static System.Net.WebRequestMethods;

namespace Velo
{
    public class Map
    {
        public static readonly ulong CURATED_COUNT = 88;

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
            { "Club V", 20 },
            { "Dance Hall", 22 },
            { "Dash the night", 23 },
            { "Genetics", 26 },
            { "Minery", 30 },
            { "New Age City", 31 },
            { "Oasis - Abyss", 33 },
            { "Oceanslide", 34 },
            { "Pitfall", 35 },
            { "Plantation", 36 },
            { "Shore Enuff", 37 },
            { "Snowed In", 38 },
            { "SpeedCity Nights", 40 },
            { "Texas Run'em", 43 },
            { "Aztec", 85 },
            { "Titan", 87 }
        };

        // contains curated maps (old RWS) by file IDs and maps it to their Velo map ID
        private static readonly Dictionary<ulong, ulong> CuratedMapFileIdToId = new Dictionary<ulong, ulong>
        {
            { 459395096UL, 45 }, // Arctic
            { 355112213UL, 46 }, // Boardwalk
            { 2376184180UL, 19 }, // Club House
            { 3408572013UL, 21 }, // Coastline
            { 3157484074UL, 24 }, // Disco Lounge
            { 3408573248UL, 25 }, // Dragon City
            { 355841009UL, 27 }, // Gift Store
            { 3408573618UL, 28 }, // Granary
            { 3366953228UL, 86 }, // Jungle Juice
            { 1739949510UL, 29 }, // Lunar Colony
            { 366868469UL, 47 }, // Mall
            { 684228943UL, 48 }, // Metalworks
            { 3408575662UL, 32 }, // Oasis
            { 373772831UL, 49 }, // Park
            { 318216468UL, 50 }, // Rainx Laboratory
            { 726451361UL, 51 }, // Rush Ring
            { 947525541UL, 52 }, // Safari Park r45
            { 3408574055UL, 39 }, // Sound Shiver
            { 510517129UL, 53 }, // SubWave 1.4
            { 3431632319UL, 41 }, // Surfing in the Oasis
            { 3408574219UL, 42 }, // Terminal
            { 3408574449UL, 44 }, // Void
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
            names[19] = "Club House";
            names[21] = "Coastline";
            names[24] = "Disco Lounge";
            names[25] = "Dragon City";
            names[27] = "Gift Store";
            names[28] = "Granary";
            names[29] = "Lunar Colony";
            names[32] = "Oasis";
            names[39] = "Sound Shiver";
            names[41] = "Surfing in the Oasis";
            names[42] = "Terminal";
            names[44] = "Void";
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
            names[86] = "Jungle Juice";
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
            LevelData levelData;
            GameInfo gameInfo;

            if (Velo.ModuleSolo != null)
            {
                levelData = Velo.ModuleSolo.LevelData;
                gameInfo = Velo.ModuleSolo.gameInfo;
            }
            else if (Velo.ModuleMP != null)
            {
                levelData = Velo.ModuleMP.LevelData;
                gameInfo = Velo.ModuleMP.gameInfo;
            }
            else
                return ulong.MaxValue;

            if (Origins.Instance.IsOrigins())
                return Origins.Instance.Current;
            if (levelData == null)
                return ulong.MaxValue;
            
            string mapName = levelData.name;
            string mapAuthor = levelData.author;

            // check for official or RWS
            if (mapAuthor == "Casper van Est" || mapAuthor == "Gert-Jan Stolk" || mapAuthor == "dd_workshop")
            {
                if (CuratedMapNameToId.ContainsKey(mapName))
                    return CuratedMapNameToId[mapName];
            }

            if (gameInfo.unknown1 != null)
            {
                ulong fileId = ((PublishedFileId)gameInfo.unknown1.publishedFileId).published_file_id.m_PublishedFileId;
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

        public static int GetPage(ulong mapId)
        {
            if (IsOfficial(mapId))
                return 0;
            if (IsRWS(mapId))
                return 1;
            if (IsOldRWS(mapId))
                return 2;
            if (IsOrigins(mapId))
                return 3;
            return 4;
        }

        public static int StandardOrder(ulong id1, ulong id2)
        {
            int page1 = GetPage(id1);
            int page2 = GetPage(id2);

            if (page1 != page2)
                return page1.CompareTo(page2);
            
            switch (page1)
            {
                case 0:
                    return id1.CompareTo(id2);
                case 1:
                    return MapIdToName(id1).CompareTo(MapIdToName(id2));
                case 2:
                    return MapIdToName(id1).CompareTo(MapIdToName(id2));
                case 3:
                    return id1.CompareTo(id2);
                case 4:
                    return MapIdToName(id1).CompareTo(MapIdToName(id2));
            }
            return 0;
        }

        public static bool IsOfficial(ulong mapId)
        {
            return mapId <= 16;
        }

        public static bool IsRWS(ulong mapId)
        {
            return 
                ((mapId >= 17 && mapId <= 44) ||
                (mapId >= 85 && mapId <= 87)) &&
                !IsOldRWS(mapId);
        }

        public static bool IsOldRWS(ulong mapId)
        {
            return 
                mapId == 19 ||
                mapId == 21 ||
                (mapId >= 24 && mapId <= 25) ||
                (mapId >= 27 && mapId <= 29) ||
                mapId == 32 ||
                mapId == 39 ||
                mapId == 41 ||
                mapId == 42 ||
                mapId == 44 ||
                mapId == 86 ||
                mapId >= 45 && mapId <= 54;
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
                mapId != 17 &&
                mapId != 20;
        }

        public static bool AllowDrill(ulong mapId)
        {
            return
                mapId != 3 &&
                mapId != 17 &&
                mapId != 20;
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
