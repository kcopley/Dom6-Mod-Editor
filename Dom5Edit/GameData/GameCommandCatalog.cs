using System.Reflection;
using System.Text.Json;
using Dom5Edit.Commands;
using Dom5Edit.Entities;

namespace Dom5Edit.GameData
{
    /// <summary>
    /// The commands Dominions 6 reads for each entity type, taken from the game's own .dm parser
    /// (game-commands-*.json, written by tools/dom6exe catalog). A command the game doesn't read
    /// is kept in the mod but changes nothing in game, so it is shown read-only.
    /// </summary>
    public static class GameCommandCatalog
    {
        private const string ResourceName = "Dom5Edit.GameData.game-commands.json";

        private class Context
        {
            public bool Complete;
            public HashSet<string> Commands = new HashSet<string>();
            public Dictionary<string, CommandEffect> Effects = new Dictionary<string, CommandEffect>();
        }

        private static readonly Dictionary<string, Context> _contexts = new Dictionary<string, Context>();
        private static readonly HashSet<string> _top = new HashSet<string>();

        /// <summary>Game version the catalog was read from (e.g. "6.37"), or null if it didn't load.</summary>
        public static string? GameVersion { get; private set; }

        static GameCommandCatalog()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream == null)
                return;
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;
            GameVersion = root.GetProperty("game_version").GetString();
            foreach (var c in root.GetProperty("top").EnumerateArray())
                _top.Add(c.GetString()!);
            foreach (var ctx in root.GetProperty("contexts").EnumerateObject())
            {
                var context = new Context { Complete = ctx.Value.GetProperty("complete").GetBoolean() };
                foreach (var c in ctx.Value.GetProperty("commands").EnumerateArray())
                    context.Commands.Add(c.GetString()!);
                if (ctx.Value.TryGetProperty("effects", out var effects))
                    foreach (var e in effects.EnumerateObject())
                    {
                        var effect = CommandEffect.Read(e.Value);
                        // an event keeps requirements and effects in two lists with their own numbers
                        // (tools/dom6exe README, Events): #req_code's 59 isn't #decscale3's 59
                        if (ctx.Name == "event" && e.Name.StartsWith("req_"))
                            effect = effect.Renamed(k => k.StartsWith("a") ? "r" + k.Substring(1) : k);
                        context.Effects[e.Name] = effect;
                    }
                _contexts[ctx.Name] = context;
            }
        }

        /// <summary>The commands (without '#') the game reads in a block of this type, or none if the catalog has no such context.</summary>
        public static IReadOnlyCollection<string> CommandsOf(EntityType type) =>
            ContextOf(type) is string c && _contexts.TryGetValue(c, out var context) ? context.Commands : (IReadOnlyCollection<string>)Array.Empty<string>();

        /// <summary>
        /// Whether the game reads this command (without '#') outside a block: a #new.../#select...,
        /// a #clear... of the whole mod, or a general setting (#poppergold, ...), read anywhere.
        /// </summary>
        public static bool IsModLevel(string name) =>
            _top.Contains(name) || _contexts.TryGetValue("global", out var general) && general.Commands.Contains(name);

        /// <summary>The game parser contexts ("monster", "item", ...) that read this command (without '#').</summary>
        public static IEnumerable<string> ContextsReading(string name) =>
            _contexts.Where(kv => kv.Key != "top" && kv.Value.Commands.Contains(name)).Select(kv => kv.Key);

        /// <summary>The game parser's name for an entity type, or null if the catalog has none.</summary>
        public static string? ContextOf(EntityType type)
        {
            return type switch
            {
                EntityType.MONSTER => "monster",
                EntityType.WEAPON => "weapon",
                EntityType.ARMOR => "armor",
                EntityType.ITEM => "item",
                EntityType.SPELL => "spell",
                EntityType.SITE => "site",
                EntityType.NATION => "nation",
                EntityType.MERCENARY => "merc",
                EntityType.POPTYPE => "poptype",
                EntityType.NAMETYPE => "nametype",
                EntityType.EVENT => "event",
                EntityType.BLESS => "bless",
                EntityType.TEMPLATE => "template",
                _ => null,
            };
        }

        /// <summary>
        /// What the command writes into an entity of this type in the game (from the game's parser),
        /// or null when the catalog doesn't model it (#weapon, #copystats, nation lists, ...).
        /// </summary>
        public static CommandEffect? EffectOf(EntityType type, Command command)
        {
            return _effectCache.GetOrAdd(((int)type << 16) ^ (int)command, _ =>
            {
                CommandEffect? effect = null;
                if (GameVersion != null && CommandsMap.TryGetString(command, out var s))
                {
                    var ctx = ContextOf(type);
                    if (ctx != null && _contexts.TryGetValue(ctx, out var context))
                        context.Effects.TryGetValue(s.TrimStart('#'), out effect);
                }
                return effect;
            });
        }

        // (type, command) -> effect: the resolver asks for every line against every value
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, CommandEffect?> _effectCache = new();

        /// <summary>The commands of this type the catalog has effects for.</summary>
        public static IReadOnlyList<Command> CommandsWithEffects(EntityType type)
        {
            var ctx = ContextOf(type);
            if (ctx == null || !_contexts.TryGetValue(ctx, out var context))
                return Array.Empty<Command>();
            var list = new List<Command>();
            foreach (var name in context.Effects.Keys)
                if (CommandsMap.TryGetCommand("#" + name, out var c))
                    list.Add(c);
            return list;
        }

        /// <summary>Whether the game reads this command for this entity (see the overload by type).</summary>
        public static bool? IsRead(IDEntity entity, Command command)
        {
            return IsRead(entity.GetEntityType(), command);
        }

        /// <summary>
        /// Whether the game reads this command for this entity type: true, false, or null when
        /// the catalog can't tell (no catalog, an entity type it doesn't cover, or one whose parser
        /// also reads commands by pattern, like events' #2d6units).
        /// </summary>
        public static bool? IsRead(EntityType type, Command command)
        {
            if (GameVersion == null || !CommandsMap.TryGetString(command, out var s))
                return null;
            var name = s.TrimStart('#');
            if (_top.Contains(name))
                return true;
            var ctx = ContextOf(type);
            if (ctx == null || !_contexts.TryGetValue(ctx, out var context))
                return null;
            if (context.Commands.Contains(name))
                return true;
            return context.Complete ? false : null;
        }
    }

    /// <summary>
    /// What one command writes into its entity's game record (tools/dom6exe command_effects): keys
    /// "f&lt;offset&gt;" for record fields, "a&lt;n&gt;" for abilities, "&lt;word offset&gt;:&lt;bit&gt;" for flag bits.
    /// </summary>
    public sealed class CommandEffect
    {
        /// <summary>Fields and abilities the command replaces.</summary>
        public IReadOnlyCollection<string> Set { get; private set; } = Array.Empty<string>();
        /// <summary>Abilities the command appends to (repeatable commands).</summary>
        public IReadOnlyCollection<string> Add { get; private set; } = Array.Empty<string>();
        /// <summary>Abilities the command ORs a value into.</summary>
        public IReadOnlyCollection<string> Or { get; private set; } = Array.Empty<string>();
        /// <summary>Abilities the command removes.</summary>
        public IReadOnlyCollection<string> Del { get; private set; } = Array.Empty<string>();
        /// <summary>Flag bits set, one key per bit.</summary>
        public IReadOnlyCollection<string> Bits { get; private set; } = Array.Empty<string>();
        /// <summary>Flag bits cleared, one key per bit.</summary>
        public IReadOnlyCollection<string> Clears { get; private set; } = Array.Empty<string>();

        /// <summary>The argument's range, for commands the game reads with its generic handler (it clamps to it); null if unknown.</summary>
        public long? Min { get; private set; }
        public long? Max { get; private set; }
        /// <summary>The argument may be left out.</summary>
        public bool Optional { get; private set; }

        /// <summary>Constants the command stores, by key, where it doesn't store its argument (#quadruped: item slots a182 = 786432).</summary>
        public IReadOnlyDictionary<string, long> Values { get; private set; } = new Dictionary<string, long>();
        /// <summary>The command takes no argument (its range is one value).</summary>
        public bool NoArgument => Min.HasValue && Min == Max;

        /// <summary>A repeatable command: each line adds an entry rather than replacing the last.</summary>
        public bool Appends => Set.Count == 0 && Bits.Count == 0 && (Add.Count > 0 || Or.Count > 0);

        /// <summary>The same effect with its ability keys renamed (an event's requirement list: "a59" -> "r59").</summary>
        internal CommandEffect Renamed(Func<string, string> rename) => new CommandEffect
        {
            Set = Set.Select(rename).ToHashSet(), Add = Add.Select(rename).ToHashSet(), Or = Or.Select(rename).ToHashSet(),
            Del = Del.Select(rename).ToHashSet(), Bits = Bits, Clears = Clears, Min = Min, Max = Max, Optional = Optional, Values = Values,
        };

        internal static CommandEffect Read(JsonElement e)
        {
            HashSet<string> Keys(string name, bool splitBits = false)
            {
                var set = new HashSet<string>();
                if (!e.TryGetProperty(name, out var arr))
                    return set;
                foreach (var k in arr.EnumerateArray())
                {
                    var key = k.GetString()!;
                    if (!splitBits)
                    {
                        set.Add(key);
                        continue;
                    }
                    // "word:mask" -> one key per bit, so overlaps compare bit by bit
                    var parts = key.Split(':');
                    ulong mask = ulong.Parse(parts[1]);
                    for (int b = 0; b < 64; b++)
                        if ((mask >> b & 1) != 0)
                            set.Add(parts[0] + ":" + b);
                }
                return set;
            }
            return new CommandEffect
            {
                Set = Keys("set"), Add = Keys("add"), Or = Keys("or"), Del = Keys("del"),
                Bits = Keys("bits", true), Clears = Keys("clears", true),
                Min = e.TryGetProperty("min", out var min) ? min.GetInt64() : null,
                Max = e.TryGetProperty("max", out var max) ? max.GetInt64() : null,
                Optional = e.TryGetProperty("optional", out var opt) && opt.GetBoolean(),
                Values = e.TryGetProperty("values", out var vals)
                    ? vals.EnumerateObject().Where(v => v.Value.TryGetInt64(out _)).ToDictionary(v => v.Name, v => v.Value.GetInt64())
                    : new Dictionary<string, long>(),
            };
        }
    }
}
