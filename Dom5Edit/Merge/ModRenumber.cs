using System.Text.RegularExpressions;
using Dom5Edit.Commands;
using Dom5Edit.Entities;
using Dom5Edit.Props;

namespace Dom5Edit.Merge
{
    /// <summary>One type's numbers in a mod, for the Renumber tool (<see cref="ModRenumber"/>).</summary>
    public sealed class RenumberType
    {
        internal RenumberType(EntityType type, List<IDEntity> own, List<IDEntity> onGameNumbers, int gameLast, int last)
        {
            Type = type;
            Own = own;
            OnGameNumbers = onGameNumbers;
            GameLast = gameLast;
            Last = last;
        }

        public EntityType Type { get; }

        /// <summary>"monster", "weapon", ...</summary>
        public string Kind => ModRenumber.Kind(Type);

        /// <summary>The entities the mod numbers itself (not the game's, not a needed mod's), in number order.</summary>
        public IReadOnlyList<IDEntity> Own { get; }

        /// <summary>
        /// The mod's #new... on a number the game uses: in game it replaces the game's entity
        /// (moved only when asked: the game then has its own back).
        /// </summary>
        public IReadOnlyList<IDEntity> OnGameNumbers { get; }

        public int Count => Own.Count;
        public int Lowest => Own.Count > 0 ? Own[0].ID : 0;
        public int Highest => Own.Count > 0 ? Own[^1].ID : 0;

        /// <summary>The game's last number of the type: a new block starts above it.</summary>
        public int GameLast { get; }

        /// <summary>The type's last number in game (monsters 19999, weapons and sites 3999, armor 1999).</summary>
        public int Last { get; }
    }

    /// <summary>An entity's move: from its number to the new one.</summary>
    public sealed record RenumberMove(IDEntity Entity, EntityType Type, int Old, int New);

    /// <summary>What a type's block comes to: how many move, the numbers they had and get.</summary>
    public sealed record RenumberBlock(EntityType Type, int Count, int OldLowest, int OldHighest, int NewLowest, int NewHighest);

    /// <summary>The moves the Renumber tool would make, or why it can't (<see cref="ModRenumber.Plan"/>).</summary>
    public sealed class RenumberPlan
    {
        public List<RenumberMove> Moves { get; } = new();

        /// <summary>Why the plan can't be made: nothing moves while there is one.</summary>
        public List<string> Problems { get; } = new();

        /// <summary>What the author should know (numbers skipped, units kept where they are, ...).</summary>
        public List<string> Notes { get; } = new();

        public List<RenumberBlock> Blocks { get; } = new();

        public bool CanApply => Problems.Count == 0 && Moves.Count > 0;
    }

    /// <summary>
    /// The Renumber tool (the user, for an author whose decade-old mods' numbers are "all over the
    /// place"): per type, the entities the mod numbers itself move to a block from a start number,
    /// packed in their order (start, start+1, ...), and every line that refers to one follows.
    /// The plan never lands one on a number the game, a needed mod, another of the mod's own
    /// entities or a line that finds nothing uses, and keeps units that go by each other's
    /// numbers (#shrinkhp, #growhp, #xpshape, a ritual summoning the next) in a row. The move is
    /// one undoable edit of the open mod (Editing.RenumberEdit); saving it is the usual save, with
    /// its backups. docs/RENUMBER.md.
    /// </summary>
    public static class ModRenumber
    {
        /// <summary>The types a mod numbers itself (#newspell, #newitem and #newnation take no number: the game gives them one).</summary>
        public static readonly EntityType[] Types = { EntityType.MONSTER, EntityType.WEAPON, EntityType.ARMOR, EntityType.SITE };

        public static string Kind(EntityType type) => type.ToString().ToLowerInvariant();

        /// <summary>The mod's numbers, per type (<see cref="Types"/>).</summary>
        public static List<RenumberType> Survey(Mod mod)
        {
            var vanilla = VanillaLoader.Vanilla;
            var list = new List<RenumberType>();
            foreach (var type in Types)
            {
                var set = mod.Database[type];
                var own = new List<IDEntity>();
                var onGame = new List<IDEntity>();
                foreach (var e in set.GetFullList())
                {
                    if (e.ID <= 0 || !ReferenceEquals(e.ParentMod, mod) || !set.TryGetValue(e.ID, out var held) || !ReferenceEquals(held, e))
                        continue;
                    var below = mod.FindBelow(type, e.ID, null);
                    if (below == null)
                        own.Add(e);
                    else if (!e.Selected && vanilla != null && ReferenceEquals(below.ParentMod, vanilla))
                        onGame.Add(e);
                    // (else: a #select of the game's or a needed mod's entity, or a #new over a needed
                    // mod's on purpose: not the mod's number to move)
                }
                int gameLast = vanilla?.Database[type].GetFullList().Select(e => e.ID).DefaultIfEmpty(0).Max() ?? 0;
                list.Add(new RenumberType(type, own.OrderBy(e => e.ID).ToList(), onGame.OrderBy(e => e.ID).ToList(), gameLast, set.END_ID));
            }
            return list;
        }

        /// <summary>
        /// The moves for these start numbers (a type without one doesn't move). With
        /// <paramref name="gameNumbers"/>, the mod's #new... on the game's numbers move too.
        /// </summary>
        public static RenumberPlan Plan(Mod mod, IReadOnlyDictionary<EntityType, int> starts, bool gameNumbers = false) =>
            Plan(new RenumberContext(mod), starts, gameNumbers);

        /// <summary>
        /// <see cref="Plan(Mod, IReadOnlyDictionary{EntityType, int}, bool)"/> from what a context
        /// worked out once (the Renumber window plans again at every start number typed).
        /// </summary>
        public static RenumberPlan Plan(RenumberContext context, IReadOnlyDictionary<EntityType, int> starts, bool gameNumbers = false)
        {
            var plan = new RenumberPlan();
            foreach (var type in Types)
            {
                if (!starts.TryGetValue(type, out int start))
                    continue;
                var t = context.Survey.First(x => x.Type == type);
                var kind = t.Kind;
                var p = context.Prepare(type, gameNumbers);
                if (p.Moving.Count == 0)
                    continue;
                if (start <= t.GameLast)
                {
                    plan.Problems.Add($"{Capital(kind)}s: start above the game's last {kind}, {t.GameLast} (the numbers up to it are the game's)");
                    continue;
                }
                if (start > t.Last)
                {
                    plan.Problems.Add($"{Capital(kind)}s: the game's {kind} numbers end at {t.Last}");
                    continue;
                }
                plan.Notes.AddRange(p.Notes);
                // packed from the start, in their order, around the numbers taken
                int next = start, count = 0, newLow = 0, newHigh = 0, skipped = 0;
                bool full = false;
                foreach (var run in p.Runs)
                {
                    while (Enumerable.Range(next, run.Count).Any(p.Blocked.Contains))
                    {
                        next++;
                        skipped++;
                    }
                    if (next + run.Count - 1 > t.Last)
                    {
                        full = true;
                        break;
                    }
                    for (int k = 0; k < run.Count; k++)
                    {
                        if (count++ == 0)
                            newLow = next + k;
                        newHigh = next + k;
                        if (run[k].ID != next + k)
                            plan.Moves.Add(new RenumberMove(run[k], type, run[k].ID, next + k));
                    }
                    next += run.Count;
                }
                if (full)
                {
                    plan.Problems.Add($"{Capital(kind)}s: {p.Runs.Sum(r => r.Count)} don't fit from {start} up to {t.Last}, the game's last {kind} number (with the numbers taken in between)");
                    plan.Moves.RemoveAll(m => m.Type == type);
                    continue;
                }
                if (count == 0)
                    continue;
                plan.Blocks.Add(new RenumberBlock(type, count, p.Runs.SelectMany(r => r).Min(e => e.ID), p.Runs.SelectMany(r => r).Max(e => e.ID), newLow, newHigh));
                if (skipped > 0)
                    plan.Notes.Add($"{Capital(kind)}s: {skipped} number{(skipped == 1 ? "" : "s")} skipped in {newLow}-{newHigh}, taken (the game's, a needed mod's, the mod's own that stay, or one a line refers to)");
                if (context.Dangling.TryGetValue(type, out var lost))
                    foreach (var (n, line) in lost.Where(l => l.Key >= start && l.Key <= Math.Max(newHigh, start)).OrderBy(l => l.Key))
                        plan.Notes.Add($"Line {line} refers to {kind} {n}, which nothing has: {n} stays free, so the line still finds nothing");
                if (gameNumbers)
                    GameNumberChecks(context, type, t, plan);
            }
            UnreadLines(context, plan);
            return plan;
        }

        /// <summary>
        /// The mod's #new... on the game's numbers: moved, the game has its own entity back there
        /// (said in a note); a line naming either by name would then find another in game.
        /// </summary>
        private static void GameNumberChecks(RenumberContext context, EntityType type, RenumberType t, RenumberPlan plan)
        {
            var kind = t.Kind;
            var vanilla = VanillaLoader.Vanilla;
            foreach (var e in t.OnGameNumbers)
            {
                var game = vanilla?.Database[type].TryGetValue(e.ID, out var g) == true ? g : null;
                string mine = e.Name ?? "", theirs = game?.Name ?? "";
                plan.Notes.Add($"{Capital(kind)} {e.ID} {mine} takes the number of the game's {theirs}: moved, the game has its own {theirs} back at {e.ID} (and its units that use {e.ID} use it again)");
                foreach (var (line, refType, name) in context.Names)
                    if (refType == type && (string.Equals(name, mine, StringComparison.OrdinalIgnoreCase) || string.Equals(name, theirs, StringComparison.OrdinalIgnoreCase)))
                        plan.Problems.Add($"Line {line} names {kind} \"{name}\": moving {kind} {e.ID} off the game's number changes what that name finds in game. Write the line with the number first, or leave the game's numbers out");
            }
        }

        /// <summary>
        /// Lines the editor doesn't read (kept as written) that the game reads, with a number that
        /// moves: it doesn't follow. (A misspelt command the game skips too doesn't matter.)
        /// </summary>
        private static void UnreadLines(RenumberContext context, RenumberPlan plan)
        {
            if (plan.Moves.Count == 0)
                return;
            var old = plan.Moves.GroupBy(m => m.Old).ToDictionary(g => g.Key, g => g.ToList());
            var said = new HashSet<string>();
            foreach (var (text, headerLine, numbers) in context.Unread)
                foreach (var n in numbers)
                    if (old.TryGetValue(n, out var moves) && said.Count < 20 && said.Add(text))
                        plan.Notes.Add($"`{text}` (in the block of line {headerLine}) isn't a line the editor reads, so its {n} stays: if it means {string.Join(" or ", moves.Select(x => $"{Kind(x.Type)} {n}"))}, change it to {string.Join("/", moves.Select(x => x.New))} by hand");
        }

        // a number standing by itself (not part of a command's name: #1unit, #3d6units)
        internal static readonly Regex Number = new(@"(?<![\w#.])-?\d+(?![\w.])");

        /// <summary>Resolves the references of lines loading left alone (a line a later copy or clear took out): they follow a move too.</summary>
        internal static void ResolveAll(Mod mod)
        {
            foreach (var r in Renumbering.Lines(mod).OfType<Reference>())
                if (!r.TryGetEntity(out _))
                    r.Resolve();
        }

        /// <summary>Per type, the numbers lines refer to that nothing has (first line of each).</summary>
        internal static Dictionary<EntityType, Dictionary<int, int>> Dangling(Mod mod)
        {
            var map = new Dictionary<EntityType, Dictionary<int, int>>();
            foreach (var (line, r) in StringRefs(mod))
            {
                if (r.IsStringRef || r.ID <= 0 || r.TryGetEntity(out _) || SafeType(r) is not EntityType type || !Types.Contains(type))
                    continue;
                if (!map.TryGetValue(type, out var numbers))
                    map[type] = numbers = new Dictionary<int, int>();
                numbers.TryAdd(r.ID, line);
            }
            return map;
        }

        /// <summary>Every number-or-name reference in the mod's lines, with its line number.</summary>
        internal static IEnumerable<(int Line, StringOrIDRef Ref)> StringRefs(Mod mod) =>
            Renumbering.Lines(mod).OfType<Reference>().SelectMany(r => r.Parts().OfType<StringOrIDRef>().Select(p => (r.LineNumber, p)));

        internal static EntityType? SafeType(Reference r)
        {
            try { return r.GetEntityType(); }
            catch (Exception) { return null; }
        }

        internal static string Capital(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    /// <summary>
    /// What a renumbering plan needs that doesn't depend on the start numbers, worked out once
    /// (the Renumber window plans again at every start number typed): the mod's numbers, the
    /// numbers lines refer to that nothing has, the names written, the lines the editor doesn't
    /// read, and per type the runs that move together and the numbers taken. For the mod as it is
    /// when made: one made before an edit is stale after it.
    /// </summary>
    public sealed class RenumberContext
    {
        public RenumberContext(Mod mod)
        {
            Mod = mod;
            ModRenumber.ResolveAll(mod);
            Survey = ModRenumber.Survey(mod);
            Dangling = ModRenumber.Dangling(mod);
            Names = ModRenumber.StringRefs(mod)
                .Where(x => x.Ref.IsStringRef && !string.IsNullOrEmpty(x.Ref.WrittenName) && ModRenumber.SafeType(x.Ref) is EntityType t && ModRenumber.Types.Contains(t))
                .Select(x => (x.Line, ModRenumber.SafeType(x.Ref)!.Value, x.Ref.WrittenName!)).ToList();
            foreach (var block in mod.SourceBlocks)
                foreach (var (_, text) in block.Trivia)
                {
                    var t = text.Trim();
                    var command = Regex.Match(t, @"^#(\w+)");
                    if (!command.Success || !GameData.GameCommandCatalog.CommandsOf(block.Entity.GetEntityType()).Contains(command.Groups[1].Value.ToLowerInvariant()))
                        continue;
                    var numbers = ModRenumber.Number.Matches(t.Split("--")[0]).Select(m => int.TryParse(m.Value, out var n) ? n : int.MinValue).Where(n => n != int.MinValue).ToList();
                    if (numbers.Count > 0)
                        Unread.Add((t, block.HeaderLine, numbers));
                }
        }

        public Mod Mod { get; }

        /// <summary>The mod's numbers per type (<see cref="ModRenumber.Survey"/>).</summary>
        public IReadOnlyList<RenumberType> Survey { get; }

        internal Dictionary<EntityType, Dictionary<int, int>> Dangling { get; }

        // the names references are written with (line, type, name)
        internal List<(int Line, EntityType Type, string Name)> Names { get; }

        // lines the game reads that the editor doesn't, with their numbers (text, the block's line, numbers)
        internal List<(string Text, int HeaderLine, List<int> Numbers)> Unread { get; } = new();

        internal sealed class Prepared
        {
            public List<IDEntity> Moving = new();
            /// <summary>The runs that move, in order (a unit pinned by a number that doesn't move isn't in one).</summary>
            public List<List<IDEntity>> Runs = new();
            /// <summary>The numbers a moved entity mustn't land on.</summary>
            public HashSet<int> Blocked = new();
            public List<string> Notes = new();
        }

        private readonly Dictionary<(EntityType, bool), Prepared> _prepared = new();

        /// <summary>A type's moving entities, runs and numbers taken (with or without the mod's own on the game's numbers).</summary>
        internal Prepared Prepare(EntityType type, bool gameNumbers)
        {
            if (_prepared.TryGetValue((type, gameNumbers), out var done))
                return done;
            var p = new Prepared();
            var t = Survey.First(x => x.Type == type);
            p.Moving = t.Own.Concat(gameNumbers ? t.OnGameNumbers : Enumerable.Empty<IDEntity>()).OrderBy(e => e.ID).ToList();
            // the numbers a moved entity mustn't land on: the game's and the needed mods', the mod's
            // own that stay, and the ones a line refers to that nothing has (it would find the moved
            // entity instead of nothing)
            foreach (var below in Mod.Below())
                if (below.Database.TryGetValue(type, out var bs))
                    p.Blocked.UnionWith(bs.GetFullList().Select(e => e.ID).Where(id => id > 0));
            var movingSet = new HashSet<IDEntity>(p.Moving, ReferenceEqualityComparer.Instance);
            p.Blocked.UnionWith(Mod.Database[type].GetFullList().Where(e => e.ID > 0 && !movingSet.Contains(e)).Select(e => e.ID));
            if (Dangling.TryGetValue(type, out var lost))
                p.Blocked.UnionWith(lost.Keys);
            // units that go by a number that doesn't move with them stay where they are (with their
            // whole run), and that number stays free: moved, they'd turn into another unit
            var pinned = new HashSet<IDEntity>(ReferenceEqualityComparer.Instance);
            var runs = p.Moving.Count == 0 ? new List<List<IDEntity>>() : ModMerger.Runs(Mod, type, p.Moving, (e, neighbour, why) =>
            {
                pinned.Add(e);
                p.Blocked.Add(neighbour);
                p.Notes.Add($"{ModRenumber.Capital(why)}, which doesn't move with it: it keeps its number, and that one stays free");
            });
            foreach (var run in runs)
                if (run.Any(pinned.Contains))
                    p.Blocked.UnionWith(run.Select(e => e.ID));
                else
                    p.Runs.Add(run);
            return _prepared[(type, gameNumbers)] = p;
        }
    }
}
