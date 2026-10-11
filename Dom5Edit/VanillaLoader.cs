using Dom5Edit.Entities;

namespace Dom5Edit
{
    /// <summary>
    /// Specifies which game version's vanilla data to load.
    /// </summary>
    public enum GameVersion
    {
        /// <summary>
        /// Dominions 5 - loads from TSV spreadsheets (legacy).
        /// </summary>
        Dom5,

        /// <summary>
        /// Dominions 6 - loads from vanilla.dm file.
        /// </summary>
        Dom6
    }

    public class VanillaLoader
    {
        private static VanillaLoader _loader;
        private static Mod _vanilla;
        private static GameVersion _gameVersion = GameVersion.Dom6; // Default to Dom6
        private static string _vanillaDmPath = null;
        private static string _spellEffectMappingPath = null;
        private static string _spellEffectTypesPath = null;

        /// <summary>
        /// Gets or sets the game version for vanilla data loading.
        /// Must be set before accessing Vanilla property.
        /// </summary>
        public static GameVersion GameVersion
        {
            get => _gameVersion;
            set
            {
                if (_vanilla != null && _gameVersion != value)
                {
                    // Reset cached vanilla if version changes
                    _vanilla = null;
                }
                _gameVersion = value;
            }
        }

        /// <summary>
        /// Gets or sets the path to vanilla.dm file for Dom6.
        /// If null, will search common locations.
        /// </summary>
        public static string VanillaDmPath
        {
            get => _vanillaDmPath;
            set => _vanillaDmPath = value;
        }

        /// <summary>
        /// Gets or sets the path to spell_effects_mapping.json.
        /// If null, will search common locations.
        /// </summary>
        public static string SpellEffectMappingPath
        {
            get => _spellEffectMappingPath;
            set => _spellEffectMappingPath = value;
        }

        /// <summary>
        /// Gets or sets the path to spell_effect_types.json (optional).
        /// </summary>
        public static string SpellEffectTypesPath
        {
            get => _spellEffectTypesPath;
            set => _spellEffectTypesPath = value;
        }

        /// <summary>The vanilla mod if it is loaded (or being resolved), without loading it.</summary>
        internal static Mod LoadedVanilla => _vanilla;

        public static Mod Vanilla
        {
            get
            {
                if (_vanilla == null)
                {
                    _loader = new VanillaLoader();

                    // Load spell effect data first (if paths are set)
                    LoadSpellEffectData();

                    _vanilla = _gameVersion == GameVersion.Dom6
                        ? _loader.LoadVanillaFromDm()
                        : _loader.LoadVanillaData();
                    _vanilla.Resolve();
                }
                return _vanilla;
            }
        }

        /// <summary>
        /// Forces a reload of vanilla data. Call after changing GameVersion or VanillaDmPath.
        /// </summary>
        public static void Reload()
        {
            _vanilla = null;
            // Also reload spell effect data
            LoadSpellEffectData();
        }

        /// <summary>
        /// Load spell effect data from JSON files.
        /// </summary>
        private static void LoadSpellEffectData()
        {
            // Try to find spell effect mapping file
            string mappingPath = _spellEffectMappingPath;
            string typesPath = _spellEffectTypesPath;

            if (string.IsNullOrEmpty(mappingPath))
            {
                // Search for the file in common locations
                var searchPaths = new[]
                {
                    "spell_effects_mapping.json",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spell_effects_mapping.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "spell_effects_mapping.json"),
                };

                foreach (var path in searchPaths)
                {
                    if (File.Exists(path))
                    {
                        mappingPath = path;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(typesPath))
            {
                // Search for effect types file
                var searchPaths = new[]
                {
                    "spell_effect_types.json",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spell_effect_types.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "spell_effect_types.json"),
                };

                foreach (var path in searchPaths)
                {
                    if (File.Exists(path))
                    {
                        typesPath = path;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(mappingPath) && File.Exists(mappingPath))
            {
                SpellEffectData.Instance.Load(mappingPath, typesPath);
            }
        }

        VanillaLoader()
        {
        }

        /// <summary>
        /// Loads vanilla data from a .dm file (Dom6 format).
        /// </summary>
        Mod LoadVanillaFromDm()
        {
            Mod m = new Mod { IsGameData = true };

            string dmPath = FindVanillaDmPath();
            if (string.IsNullOrEmpty(dmPath) || !File.Exists(dmPath))
            {
                // TSV fallback disabled - vanilla.dm is required for full data
                // Log warning and return empty mod instead of falling back to TSV
                System.Diagnostics.Debug.WriteLine("[VanillaLoader] WARNING: vanilla.dm not found! Magic skills and other data will be missing.");
                System.Diagnostics.Debug.WriteLine($"[VanillaLoader] Searched paths: BaseDirectory={AppDomain.CurrentDomain.BaseDirectory}");
                System.Diagnostics.Debug.WriteLine($"[VanillaLoader] VanillaDmPath setting: {_vanillaDmPath ?? "(not set)"}");
                // Uncomment below to restore TSV fallback:
                // return LoadVanillaData();
                return m; // Return empty mod
            }

            m.Parse(dmPath);

            // the game's own events (tools/dom6exe events), their messages from the player's exe
            var eventsPath = FindVanillaEventsPath(dmPath);
            if (eventsPath != null)
            {
                m.Parse(eventsPath);
                EventMessagesStatus = Events.VanillaEventMessages.Load(m, eventsPath);
            }

            // the game's descriptions, nation summaries, ... from the player's exe
            int texts = GameData.VanillaTexts.Load(m, out var textsStatus);
            TextsStatus = textsStatus;

            MarkAllEntitiesAsVanilla(m);

            // Load sprite paths from asset files, and descriptions too when the exe gave none
            VanillaAssetLoader.LoadAssets(m, descriptions: texts == 0);
            // the game's own sprites (read from the player's install) where no file gives one
            VanillaSprites.Load(m, dmPath);

            return m;
        }

        /// <summary>How the vanilla events' messages were read (or why not), for the status bar.</summary>
        public static string? EventMessagesStatus { get; private set; }

        /// <summary>How the game's texts (descriptions, summaries, ...) were read (or why not), for the status bar.</summary>
        public static string? TextsStatus { get; private set; }

        /// <summary>The vanilla events file: next to vanilla.dm (vanilla-events.dm, as the editor ships it), or the repo's tools/dom6exe/data.</summary>
        private static string? FindVanillaEventsPath(string dmPath)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(dmPath)) ?? "";
            foreach (var path in new[]
            {
                Path.Combine(dir, "vanilla-events.dm"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vanilla-events.dm"),
                Path.Combine(dir, "tools", "dom6exe", "data", "events-6.37.dm"),
            })
                if (File.Exists(path))
                    return path;
            return null;
        }

        /// <summary>
        /// Finds the vanilla.dm file path.
        /// </summary>
        private string FindVanillaDmPath()
        {
            // If explicitly set, use that
            if (!string.IsNullOrEmpty(_vanillaDmPath))
                return _vanillaDmPath;

            // Check common locations
            var searchPaths = new[]
            {
                "vanilla.dm", // Current directory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vanilla.dm"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VanillaData", "vanilla.dm"),
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        /// <summary>
        /// Marks all entities in the mod as vanilla (read-only).
        /// </summary>
        private void MarkAllEntitiesAsVanilla(Mod m)
        {
            foreach (var entitySet in m.Database.Values)
            {
                foreach (var entity in entitySet.GetFullList())
                {
                    entity.IsVanilla = true;
                }
            }
            foreach (var dependentSet in m.Dependents.Values)
            {
                foreach (var entity in dependentSet.Values)
                {
                    entity.IsVanilla = true;
                }
            }
        }

        private enum CustomMagic
        {
            FIRE = 128,
            AIR = 256,
            WATER = 512,
            EARTH = 1024,
            ASTRAL = 2048,
            DEATH = 4096,
            NATURE = 8192,
            BLOOD = 16384,
            PRIEST = 32768
        }

        Mod LoadVanillaData()
        {
            Mod m = new Mod();

            LoadMonsterData(m);
            LoadMonsterDescriptions(m);
            LoadWeaponData(m);
            LoadArmorData(m);
            LoadArmorProtData(m);
            LoadSiteData(m);
            return m;
        }

        void LoadWeaponData(Mod m)
        {
            bool first = true;
            List<string> header = new List<string>();
            using (StreamReader s = new StreamReader(new MemoryStream(FileResources.VanillaWeaponData)))
            {
                string line;
                while ((line = s.ReadLine()) != null)
                {
                    if (first)
                    {
                        first = !first;

                        header = line.Split('\t').ToList();
                    }
                    else
                    {
                        var split = line.Split('\t').ToList();
                        m.Parse(Commands.Command.NEWWEAPON, split[0], "");
                        for (int i = 1; i < split.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(split[i]))
                                if (header[i].StartsWith("@")) continue;
                                else switch (header[i])
                                    {
                                        default:
                                            m.ProcessStringToLine(header[i] + " " + split[i]);
                                            break;
                                    }
                        }
                    }
                }
            }
        }

        void LoadArmorData(Mod m)
        {
            bool first = true;
            List<string> header = new List<string>();
            using (StreamReader s = new StreamReader(new MemoryStream(FileResources.VanillaArmorData)))
            {
                string line;
                while ((line = s.ReadLine()) != null)
                {
                    if (first)
                    {
                        first = !first;

                        header = line.Split('\t').ToList();
                    }
                    else
                    {
                        var split = line.Split('\t').ToList();
                        m.Parse(Commands.Command.NEWARMOR, split[0], "");
                        for (int i = 1; i < split.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(split[i]))
                                if (header[i].StartsWith("@")) continue;
                                else switch (header[i])
                                    {
                                        default:
                                            m.ProcessStringToLine(header[i] + " " + split[i]);
                                            break;
                                    }
                        }
                    }
                }
            }
            int a = 0;
        }

        void LoadArmorProtData(Mod m)
        {
            bool first = true;
            List<string> header = new List<string>();
            using (StreamReader s = new StreamReader(new MemoryStream(FileResources.VanillaArmorProtData)))
            {
                string line;
                while ((line = s.ReadLine()) != null)
                {
                    if (first)
                    {
                        first = !first;

                        header = line.Split('\t').ToList();
                    }
                    else
                    {
                        var split = line.Split('\t').ToList();
                        m.Parse(Commands.Command.SELECTARMOR, split[0], "");
                        for (int i = 1; i < split.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(split[i]))
                                if (header[i].StartsWith("@")) continue;
                                else switch (header[i])
                                    {
                                        default:
                                            m.ProcessStringToLine(header[i] + " " + split[i]);
                                            break;
                                    }
                        }
                    }
                }
            }
        }

        void LoadMonsterData(Mod m)
        {
            bool first = true;
            List<string> header = new List<string>();
            using (StreamReader s = new StreamReader(new MemoryStream(FileResources.VanillaMonsterData)))
            {
                string line;
                while ((line = s.ReadLine()) != null)
                {
                    if (first)
                    {
                        first = !first;

                        header = line.Split('\t').ToList();
                    }
                    else
                    {
                        var split = line.Split('\t').ToList();
                        m.Parse(Commands.Command.NEWMONSTER, split[0], "");
                        for (int i = 1; i < split.Count; i++)
                        {
                            string returnVal;
                            if (!string.IsNullOrEmpty(split[i]))
                                if (header[i].StartsWith("@")) continue;
                                else switch (header[i])
                                    {
                                        case "%leader":
                                            if (TryConvertLeader(split, i, out returnVal))
                                            {
                                                m.ProcessStringToLine(returnVal);
                                            }
                                            break;
                                        case "%magicleader":
                                            if (TryConvertMagicLeader(split, i, out returnVal))
                                            {
                                                m.ProcessStringToLine(returnVal);
                                            }
                                            break;
                                        case "%undeadleader":
                                            if (TryConvertUndeadLeader(split, i, out returnVal))
                                            {
                                                m.ProcessStringToLine(returnVal);
                                            }
                                            break;
                                        case "%itemslots":
                                            if (TryConvertItemSlots(split, i, out returnVal))
                                            {
                                                m.ProcessStringToLine(returnVal);
                                                i += 5; //skip the next 5 indexes
                                            }
                                            break;
                                        case "%slowrec":
                                            if (int.TryParse(split[i], out int slowRec))
                                            {
                                                if (slowRec == 1) continue;
                                                if (slowRec == 2) m.ProcessStringToLine("#slowrec");
                                            }
                                            break;
                                        case "%custommagic":
                                            if (TryConvertCustomMagic(split, i, out returnVal))
                                            {
                                                m.ProcessStringToLine(returnVal);
                                                i += 3; //skip the next 3 indexes
                                            }
                                            break;
                                        case "%sailingshipsize":
                                            if (int.TryParse(split[i], out int sailUnitCount))
                                            {
                                                int sailUnitSize;
                                                if (!int.TryParse(split[i + 1], out sailUnitSize)) sailUnitSize = 6;
                                                m.ProcessStringToLine("#sailing " + sailUnitCount + " " + sailUnitSize);
                                                i += 1; // skip the next
                                            }
                                            break;
                                        case "%gemprod":
                                            switch (split[i])
                                            { //1E 1F 1N 1N1W 1S 1W 2F 2W 3B
                                                case "1E":
                                                    m.ProcessStringToLine("#gemprod 3 1");
                                                    break;
                                                case "1F":
                                                    m.ProcessStringToLine("#gemprod 0 1");
                                                    break;
                                                case "1N":
                                                    m.ProcessStringToLine("#gemprod 6 1");
                                                    break;
                                                case "1N1W":
                                                    m.ProcessStringToLine("#gemprod 6 1 #gemprod 2 1");
                                                    break;
                                                case "1S":
                                                    m.ProcessStringToLine("#gemprod 4 1");
                                                    break;
                                                case "1W":
                                                    m.ProcessStringToLine("#gemprod 2 1");
                                                    break;
                                                case "2F":
                                                    m.ProcessStringToLine("#gemprod 0 2");
                                                    break;
                                                case "2W":
                                                    m.ProcessStringToLine("#gemprod 2 2");
                                                    break;
                                                case "3B":
                                                    m.ProcessStringToLine("#gemprod 7 3");
                                                    break;
                                                default: break;
                                            }
                                            break;
                                        case "%makemonsters":
                                            if (TryConvertMakeMonsters(split, i, out returnVal))
                                            {
                                                m.ProcessStringToLine(returnVal);
                                                i += 1; //skip the next index
                                            }
                                            break;
                                        case "%summon":
                                            if (TryConvertSummon(split, i, out returnVal))
                                            {
                                                m.ProcessStringToLine(returnVal);
                                                i += 1; //skip the next index
                                            }
                                            break;
                                        default:
                                            m.ProcessStringToLine(header[i] + " " + split[i]);
                                            break;
                                    }
                        }
                    }
                }
            }
        }

        void LoadSiteData(Mod m)
        {
            bool first = true;
            List<string> header = new List<string>();
            using (StreamReader s = new StreamReader(new MemoryStream(FileResources.VanillaSiteData)))
            {
                string line;
                while ((line = s.ReadLine()) != null)
                {
                    if (first)
                    {
                        first = !first;

                        header = line.Split('\t').ToList();
                    }
                    else
                    {
                        var split = line.Split('\t').ToList();
                        m.Parse(Commands.Command.NEWSITE, split[0], "");
                        for (int i = 1; i < split.Count; i++)
                        {
                            string returnVal;
                            if (!string.IsNullOrEmpty(split[i])) {
                                int gems;
                                if (header[i].StartsWith("@")) continue;
                                else switch (header[i])
                                    {
                                        case "%F":
                                            if (int.TryParse(split[i], out gems))
                                            {
                                                m.ProcessStringToLine("#gems 0 " + gems);
                                            }
                                            break;
                                        case "%A":
                                            if (int.TryParse(split[i], out gems))
                                            {
                                                m.ProcessStringToLine("#gems 1 " + gems);
                                            }
                                            break;
                                        case "%W":
                                            if (int.TryParse(split[i], out gems))
                                            {
                                                m.ProcessStringToLine("#gems 2 " + gems);
                                            }
                                            break;
                                        case "%E":
                                            if (int.TryParse(split[i], out gems))
                                            {
                                                m.ProcessStringToLine("#gems 3 " + gems);
                                            }
                                            break;
                                        case "%S":
                                            if (int.TryParse(split[i], out gems))
                                            {
                                                m.ProcessStringToLine("#gems 4 " + gems);
                                            }
                                            break;
                                        case "%D":
                                            if (int.TryParse(split[i], out gems))
                                            {
                                                m.ProcessStringToLine("#gems 5 " + gems);
                                            }
                                            break;
                                        case "%N":
                                            if (int.TryParse(split[i], out gems))
                                            {
                                                m.ProcessStringToLine("#gems 6 " + gems);
                                            }
                                            break;
                                        case "%B":
                                            if (int.TryParse(split[i], out gems))
                                            {
                                                m.ProcessStringToLine("#gems 7 " + gems);
                                            }
                                            break;
                                        default:
                                            m.ProcessStringToLine(header[i] + " " + split[i]);
                                            break;
                                    }
                            }
                        }
                    }
                }
            }
        }

        void LoadMonsterDescriptions(Mod m)
        {
            using (StreamReader s = new StreamReader(new MemoryStream(FileResources.descriptions)))
            {
                m.read_stream(s);
            }
        }

        private readonly Dictionary<int, string> spell_effect_interpretations = new Dictionary<int, string>()
        {
            { 2, "#dt_normal"},
            {3, "#dt_stun"},
            {4, "@dt_feartype1"},
            {7, "#dt_poison"},
            {11, "#dt_aff"},
            {24, "#dt_holy"},
            {28, "@dt_enslave"},
            {32, "#dt_large"},
            {33, "#dt_small"},
            {46, "@dt_poisonfatigue"},
            {66, "#dt_paralyze"},
            {67, "#dt_weakness"},
            {73, "#dt_magic"},
            {74, "#dt_raise"},
            {96, "#dt_constructonly"},
            {97, "@dt_feartype2"},
            {99, "@dt_petrify"},
            {103, "#dt_drain"},
            {104, "#dt_weapondrain"},
            {107, "#dt_demon"},
            {108, "@dt_planeshift"},
            {109, "#dt_cap"},
            {116, "@dt_swallow"},
            {121, "@dt_swallowsize2"},
            {122, "@dt_swallowsmaller"},
            {123, "@dt_breakarmor"},
            {128, "#dt_realstun"},
            {134, "#dt_bouncekill"},
            {139, "@dt_cappedpoison"},
            {142, "@dt_salt"},
            {504, "@dt_cursedluck"},
            {600, "@dt_horrormark1"},
            {601, "@dt_horrormark2"},
            {1002, "@dt_lingering1"},
            {2002, "@dt_lingering2"},
            {2003, "@dt_lingeringstun2"},
            {2007, "@dt_lingeringpoison2"},
            {4007, "@dt_lingeringpoison4"},
        };

        private bool TryConvertLeader(List<string> list, int index, out string s)
        {
            //values 10, 20, 30, 35, 40, 60, 80, 90, 100, 120, 160
            if (int.TryParse(list[index], out int i))
            {
                if (i == 10)
                {
                    s = "#poorleader";
                    return true;
                }
                if (i > 10 && i < 40)
                {
                    s = "#poorleader #command " + (i - 10);
                    return true;
                }
                if (i == 40)
                {
                    s = "#okleader";
                    return true;
                }
                if (i > 40 && i < 80)
                {
                    s = "#okleader #command " + (i - 40);
                    return true;
                }
                if (i == 80)
                {
                    s = "#goodleader";
                    return true;
                }
                if (i > 80 && i < 120)
                {
                    s = "#goodleader #command " + (i - 80);
                    return true;
                }
                if (i == 120)
                {
                    s = "#expertleader";
                    return true;
                }
                if (i > 120 && i < 160)
                {
                    s = "#expertleader #command " + (i - 120);
                    return true;
                }
                if (i == 160)
                {
                    s = "#superiorleader";
                    return true;
                }
                if (i > 160)
                {
                    s = "#superiorleader #command " + (i - 160);
                    return true;
                }
            }
            s = "";
            return false;
        }

        private bool TryConvertMagicLeader(List<string> list, int index, out string s)
        {
            //values 10, 20, 30, 35, 40, 60, 80, 90, 100, 120, 160
            if (int.TryParse(list[index], out int i))
            {
                if (i == 0)
                {
                    s = "#nomagicleader";
                    return true;
                }
                if (i > 0 && i < 10)
                {
                    s = "#nomagicleader #magiccommand " + (i - 10);
                    return true;
                }
                if (i == 10)
                {
                    s = "#poormagicleader";
                    return true;
                }
                if (i > 10 && i < 40)
                {
                    s = "#poormagicleader #magiccommand " + (i - 10);
                    return true;
                }
                if (i == 40)
                {
                    s = "#okmagicleader";
                    return true;
                }
                if (i > 40 && i < 80)
                {
                    s = "#okmagicleader #magiccommand " + (i - 40);
                    return true;
                }
                if (i == 80)
                {
                    s = "#goodmagicleader";
                    return true;
                }
                if (i > 80 && i < 120)
                {
                    s = "#goodmagicleader #magiccommand " + (i - 80);
                    return true;
                }
                if (i == 120)
                {
                    s = "#expertmagicleader";
                    return true;
                }
                if (i > 120 && i < 160)
                {
                    s = "#expertmagicleader #magiccommand " + (i - 120);
                    return true;
                }
                if (i == 160)
                {
                    s = "#superiormagicleader";
                    return true;
                }
                if (i > 160)
                {
                    s = "#superiormagicleader #magiccommand " + (i - 160);
                    return true;
                }
            }
            s = "";
            return false;
        }

        private bool TryConvertUndeadLeader(List<string> list, int index, out string s)
        {
            //values 10, 20, 30, 35, 40, 60, 80, 90, 100, 120, 160
            if (int.TryParse(list[index], out int i))
            {
                if (i == 0)
                {
                    s = "#noundeadleader";
                    return true;
                }
                if (i > 0 && i < 10)
                {
                    s = "#noundeadleader #undcommand " + (i - 10);
                    return true;
                }
                if (i == 10)
                {
                    s = "#poorundeadleader";
                    return true;
                }
                if (i > 10 && i < 40)
                {
                    s = "#poorundeadleader #undcommand " + (i - 10);
                    return true;
                }
                if (i == 40)
                {
                    s = "#okundeadleader";
                    return true;
                }
                if (i > 40 && i < 80)
                {
                    s = "#okundeadleader #undcommand " + (i - 40);
                    return true;
                }
                if (i == 80)
                {
                    s = "#goodundeadleader";
                    return true;
                }
                if (i > 80 && i < 120)
                {
                    s = "#goodundeadleader #undcommand " + (i - 80);
                    return true;
                }
                if (i == 120)
                {
                    s = "#expertundeadleader";
                    return true;
                }
                if (i > 120 && i < 160)
                {
                    s = "#expertundeadleader #undcommand " + (i - 120);
                    return true;
                }
                if (i == 160)
                {
                    s = "#superiorundeadleader";
                    return true;
                }
                if (i > 160)
                {
                    s = "#superiorundeadleader #undcommand " + (i - 160);
                    return true;
                }
            }
            s = "";
            return false;
        }

        private bool TryConvertItemSlots(List<string> list, int index, out string s)
        {
            SlotType slots = 0;
            int arms, heads, body, feet, misc, crown;
            if (int.TryParse(list[index], out arms))
            {
                if (arms > 0) slots |= SlotType.HAND1;
                if (arms > 1) slots |= SlotType.HAND2;
                if (arms > 2) slots |= SlotType.HAND3;
                if (arms > 3) slots |= SlotType.HAND4;
                if (arms > 4) slots |= SlotType.HAND5;
                if (arms > 5) slots |= SlotType.HAND6;
            }
            // head offset is always + 1 - maybe access via header value later
            if (int.TryParse(list[index + 1], out heads))
            {
                if (heads > 0) slots |= SlotType.HEAD1;
                if (heads > 1) slots |= SlotType.HEAD2;
                if (heads > 2) slots |= SlotType.HEAD3;
            }
            if (int.TryParse(list[index + 2], out body))
            {
                if (body > 0) slots |= SlotType.BODY;
            }
            if (int.TryParse(list[index + 3], out feet))
            {
                if (feet > 0) slots |= SlotType.FEET;
            }
            if (int.TryParse(list[index + 4], out misc))
            {
                if (misc > 0) slots |= SlotType.MISC1;
                if (misc > 1) slots |= SlotType.MISC2;
                if (misc > 2) slots |= SlotType.MISC3;
                if (misc > 3) slots |= SlotType.MISC4;
                if (misc > 4) slots |= SlotType.MISC5;
                if (misc > 5) slots |= SlotType.MISC6;
            }
            if (int.TryParse(list[index + 5], out crown))
            {
                if (crown > 0) slots |= SlotType.CROWN;
            }
            if (slots != 0)
            {
                s = "#itemslots " + (int)slots;
                return true;
            }
            else
            {
                s = "#itemslots 1";
                return true;
            }
        }
        private bool TryConvertCustomMagic(List<string> list, int index, out string s)
        {
            CustomMagic magic = 0;
            //indexes - 0 = chance, 1 = number of entries, 2 = number of paths, 3 = mask
            int chance, numEntries, paths, mask;
            if (int.TryParse(list[index], out chance) && int.TryParse(list[index + 1], out numEntries) && int.TryParse(list[index + 2], out paths) && int.TryParse(list[index + 3], out mask))
            {
                s = "";
                for (int i = 0; i < numEntries; i++)
                {
                    s += "#custommagic " + mask + " " + chance * paths + " "; //space at end for repeating
                }
                return true;
            }
            s = "";
            return false;
        }

        private bool TryConvertMakeMonsters(List<string> list, int index, out string s)
        {
            int ids, num;
            if (int.TryParse(list[index], out ids) && int.TryParse(list[index + 1], out num))
            {
                s = "#makemonsters" + num + " " + ids;
                return true;
            }
            s = "";
            return false;
        }

        private bool TryConvertSummon(List<string> list, int index, out string s)
        {
            int ids, num;
            if (int.TryParse(list[index], out ids) && int.TryParse(list[index + 1], out num))
            {
                s = "#summon" + num + " " + ids;
                return true;
            }
            s = "";
            return false;
        }

        /* magic path for nations, skipping this for now
        public void ExportMagicPaths(string folder)
        {
            string separator = "\t";
            using (StreamWriter writer = new StreamWriter(folder + "\\mod_magicpaths.txt"))
            {
                Dictionary<string, double[]> nationStuff = new Dictionary<string, double[]>();
                Dictionary<string, double[]> offCapStuff = new Dictionary<string, double[]>();
                foreach (var mod in Mods)
                {
                    foreach (IDEntity e in mod.Nations.Values)
                    {
                        Nation n = e as Nation;
                        if (n.ID < ModManager.NATION_START_ID) continue;
                        bool hasName = n.TryGetName(out string name);
                        var era = n.Era;
                        if (era != null && era.HasValue)
                        {
                            int nationEra = era.Value;
                            switch (nationEra)
                            {
                                case 1:
                                    name = "EA " + name;
                                    break;
                                case 2:
                                    name = "MA " + name;
                                    break;
                                case 3:
                                    name = "LA " + name;
                                    break;
                            }
                        }
                        writer.Write("Nation: " + (hasName ? name : n.ID.ToString()));

                        writer.WriteLine();
                        double[] totalArr = new double[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                        //get recruitables
                        foreach (var m in n.Commanders)
                        {
                            writer.Write("OFF" + separator + m.ID);
                            var arr = GetMagicPaths(m);

                            for (int i = 0; i < arr.Length; i++)
                            {
                                double d = arr[i];
                                totalArr[i] = Math.Max(arr[i], totalArr[i]);
                                writer.Write(separator + d);
                            }
                            writer.WriteLine();
                        }
                        double[] offCap = new double[9];
                        for (int i = 0; i < offCap.Length; i++)
                        {
                            offCap[i] = totalArr[i];
                        }
                        //get cap sites
                        foreach (var s in n.Sites)
                        {
                            foreach (var m in s.HomeCommanders)
                            {
                                writer.Write("CAP" + separator + m.ID);

                                var arr = GetMagicPaths(m);

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    double d = arr[i];
                                    totalArr[i] = Math.Max(arr[i], totalArr[i]);
                                    writer.Write(separator + d);
                                }
                                writer.WriteLine();
                            }
                        }



                        writer.Write(hasName ? name : n.ID.ToString());
                        writer.WriteLine(" -- only off-cap included.");
                        writer.Write(hasName ? name : n.ID.ToString());
                        for (int i = 0; i < offCap.Length; i++)
                        {
                            double d = offCap[i];
                            writer.Write(separator + d);
                        }
                        writer.WriteLine();
                        offCapStuff.Add(hasName ? name : n.ID.ToString(), offCap);

                        writer.Write(hasName ? name : n.ID.ToString());
                        writer.WriteLine(" -- cap mages included.");
                        writer.Write(hasName ? name : n.ID.ToString());
                        for (int i = 0; i < totalArr.Length; i++)
                        {
                            double d = totalArr[i];
                            writer.Write(separator + d);
                        }
                        writer.WriteLine();
                        writer.WriteLine();
                        nationStuff.Add(hasName ? name : n.ID.ToString(), totalArr);
                    }
                }
                writer.WriteLine();
                writer.WriteLine("OFF CAP");
                foreach (var kvp in offCapStuff)
                {
                    writer.Write(kvp.Key);
                    for (int i = 0; i < kvp.Value.Length; i++)
                    {
                        double d = kvp.Value[i];
                        writer.Write(separator + d);
                    }
                    writer.WriteLine();
                }
                writer.WriteLine();
                writer.WriteLine("ON CAP");
                foreach (var kvp in nationStuff)
                {
                    writer.Write(kvp.Key);
                    for (int i = 0; i < kvp.Value.Length; i++)
                    {
                        double d = kvp.Value[i];
                        writer.Write(separator + d);
                    }
                    writer.WriteLine();
                }
                writer.WriteLine();
            }
        }

        private double[] GetMagicPaths(Monster m)
        {
            double[] arr = new double[9] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            foreach (var magic in m.MagicSkills)
            {
                switch (magic.Path)
                {
                    case Commands.MagicPaths.FIRE:
                        arr[0] = Math.Max(magic.Level, arr[0]);
                        break;
                    case Commands.MagicPaths.AIR:
                        arr[1] = Math.Max(magic.Level, arr[1]);
                        break;
                    case Commands.MagicPaths.WATER:
                        arr[2] = Math.Max(magic.Level, arr[2]);
                        break;
                    case Commands.MagicPaths.EARTH:
                        arr[3] = Math.Max(magic.Level, arr[3]);
                        break;
                    case Commands.MagicPaths.ASTRAL:
                        arr[4] = Math.Max(magic.Level, arr[4]);
                        break;
                    case Commands.MagicPaths.DEATH:
                        arr[5] = Math.Max(magic.Level, arr[5]);
                        break;
                    case Commands.MagicPaths.NATURE:
                        arr[6] = Math.Max(magic.Level, arr[6]);
                        break;
                    case Commands.MagicPaths.BLOOD:
                        arr[7] = Math.Max(magic.Level, arr[7]);
                        break;
                    case Commands.MagicPaths.PRIEST:
                        arr[8] = Math.Max(magic.Level, arr[8]);
                        break;
                    case Commands.MagicPaths.ELEMENTAL:
                        arr[0] = Math.Max(magic.Level, arr[0]);
                        arr[1] = Math.Max(magic.Level, arr[1]);
                        arr[2] = Math.Max(magic.Level, arr[2]);
                        arr[3] = Math.Max(magic.Level, arr[3]);
                        break;
                    case Commands.MagicPaths.SORCERY:
                        arr[4] = Math.Max(magic.Level, arr[4]);
                        arr[5] = Math.Max(magic.Level, arr[5]);
                        arr[6] = Math.Max(magic.Level, arr[6]);
                        arr[7] = Math.Max(magic.Level, arr[7]);
                        break;
                    case Commands.MagicPaths.ALL:
                        arr[0] = Math.Max(magic.Level, arr[0]);
                        arr[1] = Math.Max(magic.Level, arr[1]);
                        arr[2] = Math.Max(magic.Level, arr[2]);
                        arr[3] = Math.Max(magic.Level, arr[3]);
                        arr[4] = Math.Max(magic.Level, arr[4]);
                        arr[5] = Math.Max(magic.Level, arr[5]);
                        arr[6] = Math.Max(magic.Level, arr[6]);
                        arr[7] = Math.Max(magic.Level, arr[7]);
                        break;
                }
            }
            foreach (var magic in m.CustomMagic)
            {
                foreach (var mpath in magic.Path)
                {
                    switch (mpath)
                    {
                        case Commands.MagicPaths.FIRE:
                            arr[0] += (magic.Chance > .1) ? Math.Ceiling(magic.Chance) : 0;
                            break;
                        case Commands.MagicPaths.AIR:
                            arr[1] += (magic.Chance > .1) ? Math.Ceiling(magic.Chance) : 0;
                            break;
                        case Commands.MagicPaths.WATER:
                            arr[2] += (magic.Chance > .1) ? Math.Ceiling(magic.Chance) : 0;
                            break;
                        case Commands.MagicPaths.EARTH:
                            arr[3] += (magic.Chance > .1) ? Math.Ceiling(magic.Chance) : 0;
                            break;
                        case Commands.MagicPaths.ASTRAL:
                            arr[4] += (magic.Chance > .1) ? Math.Ceiling(magic.Chance) : 0;
                            break;
                        case Commands.MagicPaths.DEATH:
                            arr[5] += (magic.Chance > .1) ? Math.Ceiling(magic.Chance) : 0;
                            break;
                        case Commands.MagicPaths.NATURE:
                            arr[6] += (magic.Chance > .1) ? Math.Ceiling(magic.Chance) : 0;
                            break;
                        case Commands.MagicPaths.BLOOD:
                            arr[7] += (magic.Chance > .1) ? Math.Ceiling(magic.Chance) : 0;
                            break;
                        case Commands.MagicPaths.PRIEST:
                            arr[8] += (magic.Chance > .1) ? Math.Ceiling(magic.Chance) : 0;
                            break;
                    }
                }
            }
            return arr;
        }
        */
    }
}
