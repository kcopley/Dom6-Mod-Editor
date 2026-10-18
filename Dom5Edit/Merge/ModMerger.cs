using System.Text;
using Dom5Edit.Commands;
using Dom5Edit.Entities;
using Dom5Edit.Props;
using Dom5Edit.Resolve;

namespace Dom5Edit.Merge
{
    /// <summary>One mod to merge, and the mod it's read over if it's a submod (another input, or a mod that stays separate).</summary>
    public sealed class MergeInput
    {
        public MergeInput(string file, string? needs = null)
        {
            File = Path.GetFullPath(file);
            Needs = needs == null ? null : Path.GetFullPath(needs);
        }

        public string File { get; }
        public string? Needs { get; }
    }

    /// <summary>A number a merge moved: in which part, what (monster, event code, ...), from, to.</summary>
    public sealed record MergeMove(string Part, string Kind, int Old, int New, string Name);

    /// <summary>What a merge did and what to look at (docs/MERGING.md, "A report").</summary>
    public sealed class MergeReport
    {
        public List<MergeMove> Moves { get; } = new();
        public List<string> Renames { get; } = new();
        public List<string> Conflicts { get; } = new();
        public List<string> Notes { get; } = new();
        public List<string> Kept { get; } = new();
        public List<string> Separate { get; } = new();
        public List<string> Parts { get; } = new();

        public string Markdown(string modName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {modName}: merge report").AppendLine();
            sb.AppendLine("Parts, in the order they're merged (a later part's numbers move when they collide with an earlier one's):");
            foreach (var p in Parts)
                sb.AppendLine($"- {p}");
            if (Separate.Count > 0)
            {
                sb.AppendLine().AppendLine("Needed but not merged: enable these before the merged mod, in this order:");
                foreach (var s in Separate)
                    sb.AppendLine($"- {s}");
            }
            sb.AppendLine().AppendLine($"## Numbers moved ({Moves.Count})").AppendLine();
            foreach (var g in Moves.GroupBy(m => (m.Part, m.Kind)))
            {
                sb.AppendLine($"### {g.Key.Part}: {g.Key.Kind} ({g.Count()})").AppendLine();
                foreach (var m in g)
                    sb.AppendLine($"- {m.Old} -> {m.New}{(string.IsNullOrEmpty(m.Name) ? "" : "  " + m.Name)}");
                sb.AppendLine();
            }
            void Section(string title, List<string> lines)
            {
                sb.AppendLine($"## {title} ({lines.Count})").AppendLine();
                foreach (var l in lines)
                    sb.AppendLine($"- {l}");
                sb.AppendLine();
            }
            Section("Renamed (a name-only reference would have found another part's)", Renames);
            Section("Copies kept apart (a part copies a game entity an earlier part changes: it copies the game's own, as when it's alone)", Kept);
            Section("Across parts (works as in game: the later part wins)", Conflicts);
            Section("Notes", Notes);
            return sb.ToString();
        }
    }

    public sealed class MergeResult
    {
        public required string OutputFile { get; init; }
        public required MergeReport Report { get; init; }
        /// <summary>The parts as read, with their moved numbers (for checks).</summary>
        public required IReadOnlyList<Mod> Parts { get; init; }
        /// <summary>The needed mods that stay separate, in reading order (the merged mod is read over the last).</summary>
        public required IReadOnlyList<Mod> Separate { get; init; }
        /// <summary>Every number moved, by part: (what, old) -> new ("monster", "event code", ...).</summary>
        public required IReadOnlyDictionary<Mod, Dictionary<(string Kind, int Old), int>> Moved { get; init; }
        /// <summary>The game entities copied as they are before any part changes them: (kind, the game's number, the copy's number).</summary>
        public required IReadOnlyList<(string Kind, int Of, int Number)> Snapshots { get; init; }
    }

    /// <summary>
    /// Merges mods into one (docs/MERGING.md): each part read over what it needs, a later part's
    /// colliding numbers (entities, codes, variables, enchantments, monster tags, item groups,
    /// spell-event ids) moved to free ones, references written by number wherever the game takes
    /// one, a name-only reference's target renamed only when its name would find another part's,
    /// the files parts name copied next to the merged file, and the parts written in order, each
    /// with its own text.
    /// </summary>
    public static class ModMerger
    {
        // the types whose entities a mod numbers itself
        private static readonly EntityType[] Numbered =
        {
            EntityType.MONSTER, EntityType.WEAPON, EntityType.ARMOR, EntityType.SPELL, EntityType.ITEM,
            EntityType.SITE, EntityType.NATION, EntityType.POPTYPE, EntityType.NAMETYPE,
        };

        // the numbers events, spells and items share between mods
        private static readonly EntityType[] Shared =
        {
            EntityType.EVENT_CODE, EntityType.EVENT_VAR, EntityType.ENCHANTMENT, EntityType.MONTAG,
            EntityType.RESTRICTED_ITEM, EntityType.EVENT_CODE_EFFECT,
        };

        // the game reads these in number order (the manuals): #shrinkhp turns the unit into the next
        // monster number, #growhp into the previous, #xpshape and #labxpshape into the next unless
        // #xpshapemon names one; so such a chain moves as one block, in order
        private static readonly Command[] NextShape = { Command.SHRINKHP, Command.XPSHAPE, Command.LABXPSHAPE };

        private static readonly Command[] PathCommands =
        {
            Command.SPR1, Command.SPR2, Command.XSPR1, Command.XSPR2, Command.SPR, Command.FLAG, Command.INDEPFLAG,
            Command.MOUNTEDSPR1, Command.MOUNTEDSPR2, Command.UNMOUNTEDSPR1, Command.UNMOUNTEDSPR2, Command.SAMPLE,
        };

        public static MergeResult Merge(IReadOnlyList<MergeInput> inputs, string outputFile, string modName)
        {
            if (inputs.Count < 2)
                throw new ArgumentException("a merge needs two mods or more");
            var report = new MergeReport();
            var moved = new Dictionary<Mod, Dictionary<(string Kind, int Old), int>>();
            var byFile = new Dictionary<string, Mod>(StringComparer.OrdinalIgnoreCase);
            var parts = new List<Mod>();
            var separate = new List<Mod>();
            // 1. each part over what it needs: an earlier part, or a mod that stays separate
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                Mod? below = null;
                if (input.Needs != null)
                {
                    int at = inputs.ToList().FindIndex(x => string.Equals(x.File, input.Needs, StringComparison.OrdinalIgnoreCase));
                    if (at > i)
                        throw new ArgumentException($"{Path.GetFileName(input.File)} needs {Path.GetFileName(input.Needs)}: list that one first (the game reads a submod after its parent)");
                    if (!byFile.TryGetValue(input.Needs, out below))
                    {
                        below = Mod.Import(input.Needs);
                        byFile[input.Needs] = below;
                        separate.Add(below);
                        report.Separate.Add($"{below.DisplayName} ({Path.GetFileName(input.Needs)})");
                    }
                }
                var mod = Mod.Import(input.File, below);
                byFile[input.File] = mod;
                parts.Add(mod);
                report.Parts.Add($"{mod.DisplayName} ({Path.GetFileName(input.File)}{(string.IsNullOrEmpty(mod.Version) ? "" : ", version " + mod.Version)})"
                                 + (below != null ? $", read over {below.DisplayName}" : ""));
            }
            // 2. merge mode: references by number where the game takes one (StringOrIDRef, the headers);
            // a line a later copy or clear in the file took out is still read and written: its
            // reference finds its target too (loading resolves the live lines only)
            foreach (var m in parts)
            {
                m.KeepReferenceForms = false;
                foreach (var r in Renumbering.Lines(m).OfType<Reference>())
                    if (!r.TryGetEntity(out _))
                        r.Resolve();
            }
            // 3. numbers
            foreach (var part in parts)
                moved[part] = new Dictionary<(string Kind, int Old), int>();
            // the numbers the game gives #new blocks that take none (FirstFree), by whose they are
            var slots = new Dictionary<EntityType, Dictionary<int, (Mod Owner, IDEntity Entity)>>();
            foreach (var type in Numbered)
                MoveEntities(type, parts, separate, report, moved, slots);
            foreach (var type in Shared)
                MoveShared(type, parts, separate, report, moved);
            TemplateForms(parts, moved, report);
            var spellKinds = SpellKinds(parts, moved);
            var snapshots = new List<Snapshot>();
            KeepCopiesApart(parts, separate, report, snapshots, slots);
            var strays = new Dictionary<Mod, Dictionary<(string Kind, int Old), int>>();
            Strays(parts, separate, snapshots, report, strays, slots);
            Overlaps(parts, report);
            // 4. name-only references whose names would find another part's
            RenameClashes(parts, separate, report);
            // 5. the files parts name, next to the merged file
            var outDir = Path.GetDirectoryName(Path.GetFullPath(outputFile))!;
            Directory.CreateDirectory(outDir);
            CopyFiles(parts, outDir, report);
            // 6. one file: the merged header, then each part in its own order
            Write(parts, outputFile, modName, report, snapshots);
            File.WriteAllText(Path.ChangeExtension(outputFile, ".report.md"), report.Markdown(modName));
            WriteMap(Path.ChangeExtension(outputFile, ".map.json"), parts, separate, moved, snapshots, strays, spellKinds);
            return new MergeResult
            {
                OutputFile = outputFile, Report = report, Parts = parts, Separate = separate, Moved = moved,
                Snapshots = snapshots.Select(s => (Kind(s.Type), s.Of, s.Number)).ToList(),
            };
        }

        private static bool DependsOn(Mod x, Mod y) => x.Below().Contains(y);

        /// <summary>A game entity copied, at the top of the merged file, as it is before any part changes it.</summary>
        private sealed class Snapshot
        {
            public required EntityType Type { get; init; }
            public required int Of { get; init; }
            public required int Number { get; init; }
            public required IDEntity Entity { get; init; }
            public Dictionary<Mod, int> Copies { get; } = new();
            /// <summary>The game's value of the line that hides the snapshot, put back after each copy of it (none: the same).</summary>
            public int? Restore { get; init; }
        }

        // what a snapshot is written with: its header, the copy commands that make it the game's
        // entity, and a line that keeps it out of the game where a copy alone would show (a spell
        // to research, a site on the map)
        private static readonly Dictionary<EntityType, (Command Header, Command[] Copy, (Command Line, int Value)? Hide)> SnapshotForm = new()
        {
            [EntityType.MONSTER] = (Command.NEWMONSTER, new[] { Command.COPYSTATS, Command.COPYSPR }, null),
            [EntityType.WEAPON] = (Command.NEWWEAPON, new[] { Command.COPYWEAPON }, null),
            [EntityType.ARMOR] = (Command.NEWARMOR, new[] { Command.COPYARMOR }, null),
            // #selectspell: the game's #newspell takes no number (the first free one from 1500,
            // 0x1401ad830), #selectspell takes any below 8000. School -1: nobody can research it
            // (the game's own monster-only spells; a new spell's default). #copyspell copies the
            // whole spell, name and school too (0x140246f29)
            [EntityType.SPELL] = (Command.SELECTSPELL, new[] { Command.COPYSPELL }, (Command.SCHOOL, -1)),
            // rarity 5: never a random site (the manual)
            [EntityType.SITE] = (Command.NEWSITE, new[] { Command.COPYSITE }, (Command.RARITY, 5)),
            // #selectitem: #newitem takes no number (the first free from 700, 0x140229150).
            // Construction level 11 (stored 5): the forge needs research of 11 (0x1401ed910, level
            // * 2 + 1 against the nation's research: -6 "too low"), and the random picks
            // (0x1402bd340, 0x1402bd420) ask for levels 0-4, or for any level by tag or arena
            // prize, which game items with those get no snapshot for (NoSnapshot). #copyitem
            // copies the whole record (0x14023730e)
            [EntityType.ITEM] = (Command.SELECTITEM, new[] { Command.COPYITEM }, (Command.CONSTLEVEL, 11)),
        };

        /// <summary>
        /// Why a game entity gets no snapshot though its type has one: a game item the game hands
        /// out at any construction level (the random pick 0x1402bd420 asked for level -1): an
        /// arena prize (#champprize, ability 259: "won the death match", 0x140116667) or a unit's
        /// starting gear by tag (ability 567, which no command sets: equipnewcom 0x1400c0bc9; the
        /// Dragon Pearls, the Crown of Ohya, ...). A copy of one, hidden by its level, could still
        /// be given out; so a copy of one copies the changed item, noted.
        /// </summary>
        private static string? NoSnapshot(IDEntity target)
        {
            if (target.GetEntityType() != EntityType.ITEM)
                return null;
            if (target.Properties.Any(p => p.Command == Command.CHAMPPRIZE))
                return "an arena prize (#champprize)";
            if (target.GameValues.Any(g => g.Label == "ability 567"))
                return "a unit's starting gear by tag";
            return null;
        }

        /// <summary>
        /// The user: a part's copy of a game entity (DomEnhanced's #copystats of the game's Archer)
        /// mustn't take on what an earlier part changes in it (Forgotten Realms' changes to the
        /// Archer), as it doesn't when the part is alone. Read one after another, it would; so the
        /// merged file starts with a copy of each such game entity as it is before any part
        /// changes it, and the part's copy commands copy that. Not for a part's own changes to it,
        /// nor a submod's copy of what its parent changed (both meant). A copy of a unit, weapon or
        /// armor changes nothing in game until something uses it; one of a spell or site would
        /// show (a spell to research twice, a site on the map), so the snapshot has a line that
        /// hides it (<see cref="SnapshotForm"/>) and each copy of it gets the game's value back on
        /// the line after the copy. An item's is hidden by its construction level, unless the game
        /// hands it out at any level (<see cref="NoSnapshot"/>): noted.
        /// </summary>
        private static void KeepCopiesApart(List<Mod> parts, List<Mod> separate, MergeReport report, List<Snapshot> snapshots,
                                            Dictionary<EntityType, Dictionary<int, (Mod Owner, IDEntity Entity)>> slots)
        {
            var vanilla = VanillaLoader.Vanilla;
            if (vanilla == null)
                return;
            // the game entities each part changes (a #select of the game's), by type
            var changed = parts.ToDictionary(p => p, p => new HashSet<(EntityType, int)>());
            foreach (var part in parts)
                foreach (var type in Numbered)
                    foreach (var e in part.Database[type].GetFullList())
                        if (e.Selected && e.ID > 0 && vanilla.Database[type].Has(e.ID))
                            changed[part].Add((type, e.ID));
            var taken = new Dictionary<EntityType, HashSet<int>>();
            HashSet<int> Taken(EntityType type) =>
                taken.TryGetValue(type, out var t) ? t
                : taken[type] = new HashSet<int>(parts.Concat(separate).Append(vanilla).SelectMany(m => Numbers(m, type))
                    .Concat(slots.TryGetValue(type, out var given) ? given.Keys : Enumerable.Empty<int>()));
            var unhandled = new Dictionary<(Mod, EntityType), int>();
            var exposed = new Dictionary<(Mod Part, string What, string Why), int>();
            for (int b = 1; b < parts.Count; b++)
            {
                var part = parts[b];
                foreach (var r in Renumbering.Lines(part).OfType<StringOrIDRef>().Where(r => GameRules.IsCopy(r.Command)).ToList())
                {
                    if (!r.TryGetEntity(out var target) || target == null || !ReferenceEquals(target.ParentMod, vanilla))
                        continue;
                    var type = target.GetEntityType();
                    int of = target.ID;
                    var earlier = parts.Take(b).Where(a => !DependsOn(part, a) && changed[a].Contains((type, of))).ToList();
                    if (earlier.Count == 0)
                        continue;
                    if (changed[part].Contains((type, of)))
                    {
                        report.Conflicts.Add($"{part.DisplayName} copies game {type.ToString().ToLowerInvariant()} {of} {target.Name} (line {r.LineNumber}), which it changes too, as does {earlier[0].DisplayName}: it copies what both make of it");
                        continue;
                    }
                    if (!SnapshotForm.TryGetValue(type, out var form))
                    {
                        unhandled[(part, type)] = unhandled.TryGetValue((part, type), out var n) ? n + 1 : 1;
                        continue;
                    }
                    if (NoSnapshot(target) is string why)
                    {
                        var key = (part, $"game {type.ToString().ToLowerInvariant()} {of} {target.Name}", why);
                        exposed[key] = exposed.TryGetValue(key, out var x) ? x + 1 : 1;
                        continue;
                    }
                    var snap =snapshots.FirstOrDefault(s => s.Type == type && s.Of == of);
                    if (snap == null)
                    {
                        var range = part.Database[type];
                        var t = Taken(type);
                        // spells and items from the top: below, the game gives the parts' unnumbered
                        // #newspell and #newitem theirs
                        bool down = (type == EntityType.SPELL || type == EntityType.ITEM) && range.END_ID > 0;
                        int number = down ? range.END_ID : range.START_ID;
                        while (t.Contains(number))
                            number += down ? -1 : 1;
                        t.Add(number);
                        var entity = (IDEntity)Activator.CreateInstance(vanilla.TypeOf(type))!;
                        entity.ID = number;
                        int? restore = null;
                        if (form.Hide is { } hide && target.TryGet<IntProperty>(hide.Line, out var own) == ReturnType.TRUE && own.Value != hide.Value)
                            restore = own.Value;
                        snap = new Snapshot { Type = type, Of = of, Number = number, Entity = entity, Restore = restore };
                        snapshots.Add(snap);
                    }
                    r.Retarget(snap.Entity);
                    if (form.Hide is { } h && snap.Restore is int value)
                        RestoreAfter(r, h.Line, value);
                    snap.Copies[part] = snap.Copies.TryGetValue(part, out var c) ? c + 1 : 1;
                }
            }
            foreach (var s in snapshots)
                foreach (var (part, count) in s.Copies)
                    report.Kept.Add($"{part.DisplayName}: {count} copy line{(count == 1 ? "" : "s")} of game {s.Type.ToString().ToLowerInvariant()} {s.Of} {(vanilla.Database[s.Type].TryGetValue(s.Of, out var v) ? v.Name : "")} now copy {s.Number}, the game's as it is before {string.Join(", ", parts.Where(a => changed[a].Contains((s.Type, s.Of)) && !ReferenceEquals(a, part)).Select(a => a.DisplayName))} change{(parts.Count(a => changed[a].Contains((s.Type, s.Of)) && !ReferenceEquals(a, part)) == 1 ? "s" : "")} it");
            foreach (var ((part, type), count) in unhandled)
                report.Notes.Add($"{part.DisplayName}: {count} copy line{(count == 1 ? "" : "s")} of game {type.ToString().ToLowerInvariant()}s an earlier part changes: they copy the changed ones (a copy of a {type.ToString().ToLowerInvariant()} to keep them apart would show in game)");
            foreach (var ((part, what, why), count) in exposed)
                report.Notes.Add($"{part.DisplayName}: {count} copy line{(count == 1 ? "" : "s")} of {what}, which an earlier part changes: {(count == 1 ? "it copies" : "they copy")} the changed one ({why}: the game hands such items out at any level, so a copy kept apart could turn up in game)");
        }

        /// <summary>
        /// The game's value of the line that hides a snapshot, on the line right after a copy of
        /// it (the part's own lines after the copy still override it, as before). A copy line a
        /// later copy or clear takes out gets one too: the game reads both.
        /// </summary>
        private static void RestoreAfter(StringOrIDRef copy, Command line, int value)
        {
            var owner = copy.Parent;
            if (!owner.GetPropertyMap().TryGetValue(line, out var create))
                return;
            var p = create();
            p.Parent = owner;
            p.Parse(line, value.ToString(System.Globalization.CultureInfo.InvariantCulture), "the game's, as the copy above had it before the merge");
            if (copy.PlaceKey == 0)
                copy.PlaceKey = Property.NewPlaceKey();
            p.PlaceAfterKey = copy.PlaceKey;
            owner.InsertLive(p);
            // (in the list too, where the file has it)
            int at = owner.Properties.ToList().FindIndex(q => ReferenceEquals(q, copy));
            if (at >= 0)
                owner.MoveLive(p, at + 1);
        }

        /// <summary>
        /// The entities of a type the mod numbers itself: a #new N, or a #select N of a number
        /// nothing under it has (a new nation, poptype, nametype). Never a game entity's number: a
        /// #new on it replaces the game's entity, on purpose. Not a #new that takes no number
        /// (<see cref="FirstFree"/>: the game gives it one, <see cref="GameSlots"/>).
        /// </summary>
        private static List<IDEntity> Owned(Mod mod, EntityType type)
        {
            var set = mod.Database[type];
            var vanilla = VanillaLoader.Vanilla?.Database[type];
            bool vanillaHasType = vanilla != null && vanilla.GetFullList().Count > 0;
            bool firstFree = FirstFree.ContainsKey(type);
            return set.GetFullList()
                .Where(e => e.ID > 0 && set.TryGetValue(e.ID, out var held) && ReferenceEquals(held, e) && vanilla?.Has(e.ID) != true
                            && (!e.Selected ? !firstFree : vanillaHasType && mod.FindBelow(type, e.ID, null) == null))
                .OrderBy(e => e.ID).ToList();
        }

        /// <summary>
        /// The #new commands that take no number: the game gives each the first free one from
        /// here, whatever the line says (the exe: #newspell 0x1401ad830 from 1500, #newitem
        /// 0x140229150 from 700, #newnation from 120). A mod makes numbered ones with #select N.
        /// </summary>
        private static readonly Dictionary<EntityType, int> FirstFree = new()
        {
            [EntityType.SPELL] = 1500,
            [EntityType.ITEM] = 700,
            [EntityType.NATION] = 120,
        };

        /// <summary>
        /// The numbers the game gives a mod's #new blocks that take none (<see cref="FirstFree"/>),
        /// read in file order: each the first free one, skipping every number used so far (the
        /// game's, the mods read before, the mod's own numbered blocks before it). Adds what it
        /// reads to <paramref name="used"/>.
        /// </summary>
        private static List<(IDEntity Entity, int Slot)> GameSlots(Mod mod, EntityType type, HashSet<int> used)
        {
            var given = new List<(IDEntity, int)>();
            var seen = new HashSet<IDEntity>(ReferenceEqualityComparer.Instance);
            int from = FirstFree[type];
            foreach (var b in mod.SourceBlocks)
            {
                if (b.Entity == null || b.Entity.GetEntityType() != type)
                    continue;
                if (!b.Selected)
                {
                    int n = from;
                    while (used.Contains(n))
                        n++;
                    used.Add(n);
                    seen.Add(b.Entity);
                    given.Add((b.Entity, n));
                }
                else if (!seen.Contains(b.Entity) && b.Entity.ID > 0)
                    used.Add(b.Entity.ID);
            }
            return given;
        }

        private static IEnumerable<int> Numbers(Mod mod, EntityType type) =>
            mod.Database[type].GetFullList().Select(e => e.ID).Where(id => id > 0);

        private static void MoveEntities(EntityType type, List<Mod> parts, List<Mod> separate, MergeReport report,
                                         Dictionary<Mod, Dictionary<(string Kind, int Old), int>> moved,
                                         Dictionary<EntityType, Dictionary<int, (Mod Owner, IDEntity Entity)>> slots)
        {
            var range = parts[0].Database[type];
            // every number anyone uses is taken: a moved entity mustn't land on another
            var taken = new HashSet<int>(parts.Concat(separate).SelectMany(m => Numbers(m, type)));
            if (VanillaLoader.Vanilla != null)
                taken.UnionWith(Numbers(VanillaLoader.Vanilla, type));
            var claimed = new List<(Mod Owner, int Id)>();
            foreach (var s in separate)
                claimed.AddRange(Owned(s, type).Select(e => (s, e.ID)));
            // the numbers the game gives #new blocks that take none, as it reads the separate mods,
            // then the merged file: an earlier part's are its own (a later part's numbered entity
            // on one would be the same entity in game: it moves), and nothing moves onto one
            bool firstFree = FirstFree.ContainsKey(type);
            var given = slots[type] = new Dictionary<int, (Mod Owner, IDEntity Entity)>();
            var used = new HashSet<int>(VanillaLoader.Vanilla != null ? Numbers(VanillaLoader.Vanilla, type) : Enumerable.Empty<int>());
            if (firstFree)
                foreach (var s in separate)
                    foreach (var (e, n) in GameSlots(s, type, used))
                    {
                        given[n] = (s, e);
                        claimed.Add((s, n));
                        taken.Add(n);
                    }
            // (poptypes have no range in the tables: mods make theirs from 150 to 249, as the editor does)
            int next = type == EntityType.POPTYPE ? 150 : range.START_ID, end = type == EntityType.POPTYPE ? 249 : range.END_ID;
            foreach (var part in parts)
            {
                var mine = Owned(part, type);
                foreach (var run in Runs(part, type, mine, report))
                {
                    if (!run.Any(e => claimed.Any(c => c.Id == e.ID && !DependsOn(part, c.Owner))))
                        continue;
                    int first = FreeRun(run.Count);
                    if (run.Count > 1)
                        report.Notes.Add($"{part.DisplayName}: monsters {run[0].ID}-{run[^1].ID} go by each other's numbers (#shrinkhp, #growhp, #xpshape, a ritual summoning the next): moved together to {first}-{first + run.Count - 1}");
                    for (int k = 0; k < run.Count; k++)
                        Move(part, run[k], first + k);
                }
                if (firstFree)
                    PlaceUnnumbered(part, mine);
                claimed.AddRange(mine.Select(e => (part, e.ID)));
            }

            // a block of free numbers in a row (one, for most)
            int FreeRun(int count)
            {
                while (Enumerable.Range(next, count).Any(taken.Contains))
                    next++;
                if (end > 0 && next + count - 1 > end)
                    throw new InvalidOperationException($"no free {type.ToString().ToLowerInvariant()} number left up to {end}");
                for (int k = 0; k < count; k++)
                    taken.Add(next + k);
                return next;
            }

            // the part's #new blocks that take no number get theirs after everything read before
            // them; one of the part's numbered entities read later on such a number would be the
            // same entity in game (the parts before pushed its #new blocks up): it moves, and the
            // numbers are worked out again
            void PlaceUnnumbered(Mod part, List<IDEntity> mine)
            {
                for (int round = 0; ; round++)
                {
                    var trial = new HashSet<int>(used);
                    var got = GameSlots(part, type, trial);
                    var clash = mine.Where(e => got.Any(g => g.Slot == e.ID)).ToList();
                    if (clash.Count == 0)
                    {
                        used = trial;
                        foreach (var (e, n) in got)
                        {
                            given[n] = (part, e);
                            claimed.Add((part, n));
                            taken.Add(n);
                        }
                        if (got.Count > 0 && !ReferenceEquals(part, parts[0]))
                        {
                            var kind = type.ToString().ToLowerInvariant();
                            int lo = got.Min(g => g.Slot), hi = got.Max(g => g.Slot);
                            report.Notes.Add(got.Count == 1
                                ? $"{part.DisplayName}: its #new{kind} without a number gets {kind} {lo} in game (the first free one, after the parts before it)"
                                : $"{part.DisplayName}: its {got.Count} #new{kind} without a number get {kind}s {lo}-{hi} in game (the first free ones, after the parts before it)");
                        }
                        return;
                    }
                    if (round == 20)
                        throw new InvalidOperationException($"{part.DisplayName}: its {type.ToString().ToLowerInvariant()}s without a number keep landing on its numbered ones");
                    taken.UnionWith(got.Select(g => g.Slot));
                    foreach (var e in clash)
                    {
                        report.Notes.Add($"{part.DisplayName}: {type.ToString().ToLowerInvariant()} {e.ID} {e.Name} is a number the game gives one of the part's #new{type.ToString().ToLowerInvariant()} blocks in the merged file (the parts before it take the first free ones): moved");
                        Move(part, e, FreeRun(1));
                    }
                }
            }

            void Move(Mod part, IDEntity e, int now)
            {
                int old = e.ID;
                var over = parts.Where(p => ReferenceEquals(p, part) || DependsOn(p, part)).ToList();
                Renumbering.Move(e, now, over);
                moved[part][(type.ToString().ToLowerInvariant(), old)] = now;
                report.Moves.Add(new MergeMove(part.DisplayName, type.ToString().ToLowerInvariant(), old, now, e.Name ?? ""));
                // a nation's AI pretender designs are numbered by it (#newtemplate 176): they move with it
                if (type == EntityType.NATION)
                    foreach (var x in over)
                        foreach (var t in x.Database[EntityType.TEMPLATE].GetFullList().Where(t => t.ID == old).ToList())
                        {
                            Renumbering.Move(t, now, parts.Where(p => ReferenceEquals(p, x) || DependsOn(p, x)));
                            report.Notes.Add($"{x.DisplayName}: #newtemplate {old} (an AI pretender for nation {old}) follows it to {now}");
                        }
                // a submod's #new on this number replaced this entity on purpose: it moves with it
                foreach (var sub in over.Where(p => !ReferenceEquals(p, part)))
                    if (sub.Database[type].TryGetValue(old, out var again) && ReferenceEquals(again.ParentMod, sub) && !again.Selected)
                    {
                        Renumbering.Move(again, now, parts.Where(p => ReferenceEquals(p, sub) || DependsOn(p, sub)));
                        moved[sub][(type.ToString().ToLowerInvariant(), old)] = now;
                        report.Notes.Add($"{sub.DisplayName}'s #new{type.ToString().ToLowerInvariant()} {old} replaces {part.DisplayName}'s: moved with it to {now}");
                    }
            }
        }

        /// <summary>
        /// The part's entities in runs that must keep their numbers in a row (monsters that turn
        /// into the next or previous number, a unit a ritual summons with the next one); one
        /// entity per run otherwise. A unit whose next
        /// number isn't in the part is noted: moving it changes which unit that is.
        /// </summary>
        private static List<List<IDEntity>> Runs(Mod part, EntityType type, List<IDEntity> mine, MergeReport report) =>
            Runs(part, type, mine, (e, neighbour, why) => report.Notes.Add($"{part.DisplayName}: {why}, which the part doesn't make: if it moves, that's another unit"));

        /// <summary>
        /// <see cref="Runs(Mod, EntityType, List{IDEntity}, MergeReport)"/>, telling <paramref name="loose"/> of each
        /// entity that goes by a number <paramref name="mine"/> doesn't hold (the entity, that number, what it does).
        /// </summary>
        internal static List<List<IDEntity>> Runs(Mod part, EntityType type, List<IDEntity> mine, Action<IDEntity, int, string> loose)
        {
            if (type != EntityType.MONSTER)
                return mine.Select(e => new List<IDEntity> { e }).ToList();
            var byId = mine.ToDictionary(e => e.ID);
            var resolver = Resolve.ModResolver.For(part);
            var linkNext = new HashSet<int>();
            foreach (var e in mine)
            {
                var r = resolver.Resolve(e);
                bool next = r.Has(Command.SHRINKHP) || (r.Has(Command.XPSHAPE) || r.Has(Command.LABXPSHAPE)) && !r.Has(Command.XPSHAPEMON);
                bool previous = r.Has(Command.GROWHP);
                if (next && byId.ContainsKey(e.ID + 1)) linkNext.Add(e.ID);
                if (previous && byId.ContainsKey(e.ID - 1)) linkNext.Add(e.ID - 1);
                if (next && !byId.ContainsKey(e.ID + 1))
                    loose(e, e.ID + 1, $"monster {e.ID} {e.Name} turns into the next number ({e.ID + 1})");
                if (previous && !byId.ContainsKey(e.ID - 1))
                    loose(e, e.ID - 1, $"monster {e.ID} {e.Name} grows into the previous number ({e.ID - 1})");
            }
            // a ritual that summons its unit and the next number (effect 10141, the exe: Call the
            // Birds of Splendor's two Yllerion): the pair keeps its order
            foreach (var spell in part.Database[EntityType.SPELL].GetFullList().OfType<Spell>())
                if (spell.TryGetEffectNumber(out int effect) && effect == 10141)
                    foreach (var d in spell.Properties.OfType<SpellDamage>())
                        if (d.TryGetEntity(out var m) && m != null && byId.ContainsKey(m.ID))
                        {
                            if (byId.ContainsKey(m.ID + 1))
                                linkNext.Add(m.ID);
                            else
                                loose(m, m.ID + 1, $"spell {spell.Name} summons monster {m.ID} and the next number ({m.ID + 1})");
                        }
            var runs = new List<List<IDEntity>>();
            foreach (var e in mine)
            {
                if (runs.Count > 0 && linkNext.Contains(e.ID - 1) && runs[^1][^1].ID == e.ID - 1)
                    runs[^1].Add(e);
                else
                    runs.Add(new List<IDEntity> { e });
            }
            return runs;
        }

        /// <summary>
        /// The user: a dangling reference mustn't catch something in a merge. A number a part
        /// refers to that nothing it's read over has (it finds nothing when the part is alone)
        /// would find another part's entity (or a snapshot) in the merged file, as it would with
        /// both enabled in game; so it's moved to a number nobody uses, which finds nothing too,
        /// the old number in its comment. Run after every other number is placed.
        /// </summary>
        private static void Strays(List<Mod> parts, List<Mod> separate, List<Snapshot> snapshots, MergeReport report,
                                   Dictionary<Mod, Dictionary<(string Kind, int Old), int>> strays,
                                   Dictionary<EntityType, Dictionary<int, (Mod Owner, IDEntity Entity)>> slots)
        {
            var vanilla = VanillaLoader.Vanilla;
            var taken = new Dictionary<EntityType, HashSet<int>>();
            HashSet<int> Taken(EntityType type)
            {
                if (taken.TryGetValue(type, out var t))
                    return t;
                t = new HashSet<int>(parts.Concat(separate).SelectMany(m => Numbers(m, type)));
                if (vanilla != null)
                    t.UnionWith(Numbers(vanilla, type));
                t.UnionWith(snapshots.Where(sn => sn.Type == type).Select(sn => sn.Number));
                if (slots.TryGetValue(type, out var given))
                    t.UnionWith(given.Keys);
                return taken[type] = t;
            }
            foreach (var part in parts)
            {
                strays[part] = new Dictionary<(string Kind, int Old), int>();
                foreach (var (line, r) in Renumbering.Lines(part).OfType<Reference>()
                             .SelectMany(x => x.Parts().OfType<StringOrIDRef>().Select(p => (x.LineNumber, p))).ToList())
                {
                    if (r.IsStringRef || r.ID <= 0 || r.TryGetEntity(out _) || r.Parent?.ParentMod == null)
                        continue;
                    EntityType type;
                    try { type = RefType(r); }
                    catch (Exception) { continue; }
                    if (!Numbered.Contains(type))
                        continue;
                    int old = r.ID;
                    var kind = type.ToString().ToLowerInvariant();
                    if (!strays[part].TryGetValue((kind, old), out var now))
                    {
                        // what it would find in the merged file: another part's entity (numbered, or
                        // a #new the game gives that number), or a snapshot
                        var owner = parts.FirstOrDefault(p => !ReferenceEquals(p, part) && p.Database[type].TryGetValue(old, out var e) && ReferenceEquals(e.ParentMod, p) && !FirstFreeNew(e));
                        IDEntity? slotted = null;
                        if (owner == null && slots.TryGetValue(type, out var given) && given.TryGetValue(old, out var g) && !ReferenceEquals(g.Owner, part) && parts.Contains(g.Owner))
                            (owner, slotted) = (g.Owner, g.Entity);
                        var snap = snapshots.FirstOrDefault(sn => sn.Type == type && sn.Number == old);
                        if (owner == null && snap == null)
                            continue;
                        var t = Taken(type);
                        now = part.Database[type].START_ID;
                        while (t.Contains(now) || part.Database[type].Has(now))
                            now++;
                        t.Add(now);
                        strays[part][(kind, old)] = now;
                        var what = slotted != null ? $"{owner!.DisplayName}'s {slotted.Name} (a #new{kind} the game numbers {old})"
                            : owner != null && owner.Database[type].TryGetValue(old, out var hit) ? $"{owner.DisplayName}'s {hit.Name} #{old}" : $"the merge's copy of a game {kind} #{old}";
                        report.Conflicts.Add($"{part.DisplayName} refers to {kind} {old} (first at line {line}), which it doesn't define: it would have found {what}; now {now}, which finds nothing, as when it's alone");
                    }
                    r.Redirect(now, $"(was {old}: not in this part)");
                }
            }
        }

        /// <summary>A #new the game numbers itself (whatever number the line has).</summary>
        private static bool FirstFreeNew(IDEntity e) => !e.Selected && FirstFree.ContainsKey(e.GetEntityType());

        private static EntityType RefType(Reference r)
        {
            var m = typeof(Reference).GetMethod("GetEntityType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            return (EntityType)m.Invoke(r, null)!;
        }

        /// <summary>
        /// A template's #form names its monster, optionally with its number ("Dragon (265)", the
        /// manual): the number follows a move of that monster.
        /// </summary>
        private static void TemplateForms(List<Mod> parts, Dictionary<Mod, Dictionary<(string Kind, int Old), int>> moved, MergeReport report)
        {
            var number = new System.Text.RegularExpressions.Regex(@"\((\d+)\)\s*$");
            foreach (var part in parts)
                    foreach (var p in Renumbering.Lines(part).OfType<StringProperty>().Where(p => p.Command == Command.FORM && p.Parent?.GetEntityType() == EntityType.TEMPLATE))
                    {
                        var m = number.Match(p.Value ?? "");
                        if (!m.Success)
                            continue;
                        int old = int.Parse(m.Groups[1].Value);
                        // its own part's monster, or a part it's read over
                        var mover = new[] { part }.Concat(part.Below()).FirstOrDefault(x => moved.TryGetValue(x, out var mv) && mv.ContainsKey(("monster", old)));
                        if (mover == null)
                            continue;
                        int now = moved[mover][("monster", old)];
                        p.Value = p.Value!.Substring(0, m.Groups[1].Index) + now + p.Value.Substring(m.Groups[1].Index + m.Groups[1].Length);
                        report.Notes.Add($"{part.DisplayName}: template #form \"{p.Value}\" follows monster {old} -> {now}");
                    }
        }

        /// <summary>A shared number the mod brings in itself (not a game one, not its parent's): in the mods' range.</summary>
        private static List<DependentEntity> OwnedShared(Mod mod, EntityType type)
        {
            var set = mod.Dependents[type];
            return set.Values.Where(d => d.Dependent == null && !d.IsVanilla
                                         && (set.ID_DOWN ? d.ID <= set.START_ID : d.ID >= set.START_ID))
                .OrderBy(d => Math.Abs(d.ID)).ToList();
        }

        private static void MoveShared(EntityType type, List<Mod> parts, List<Mod> separate, MergeReport report,
                                       Dictionary<Mod, Dictionary<(string Kind, int Old), int>> moved)
        {
            var range = parts[0].Dependents[type];
            var taken = new HashSet<int>(parts.Concat(separate).SelectMany(m => m.Dependents[type].Keys));
            if (VanillaLoader.Vanilla != null)
                taken.UnionWith(VanillaLoader.Vanilla.Dependents[type].Keys);
            var claimed = new List<(Mod Owner, int Id)>();
            foreach (var s in separate)
                claimed.AddRange(OwnedShared(s, type).Select(d => (s, d.ID)));
            int next = range.START_ID;
            foreach (var part in parts)
            {
                var mine = OwnedShared(part, type);
                foreach (var d in mine)
                {
                    int old = d.ID;
                    if (!claimed.Any(c => c.Id == old && !DependsOn(part, c.Owner)))
                        continue;
                    while (taken.Contains(next))
                        next += range.ID_DOWN ? -1 : 1;
                    int now = next;
                    taken.Add(now);
                    Renumbering.Move(part, type, d, now);
                    moved[part][(Kind(type), old)] = now;
                    report.Moves.Add(new MergeMove(part.DisplayName, Kind(type), old, now, ""));
                }
                claimed.AddRange(mine.Select(d => (part, d.ID)));
            }
        }

        private static string Kind(EntityType type) => type switch
        {
            EntityType.EVENT_CODE => "event code",
            EntityType.EVENT_VAR => "event variable",
            EntityType.ENCHANTMENT => "enchantment",
            EntityType.MONTAG => "monster tag",
            EntityType.RESTRICTED_ITEM => "item group (#restricteditem)",
            EntityType.EVENT_CODE_EFFECT => "spell event id (#id)",
            _ => type.ToString().ToLowerInvariant(),
        };

        /// <summary>
        /// What acts across parts and can't be untangled (the game does the same with the mods
        /// enabled one after another): game entities several parts change, game numbers several
        /// parts replace, clearing commands in a later part.
        /// </summary>
        private static void Overlaps(List<Mod> parts, MergeReport report)
        {
            foreach (var type in Numbered.Append(EntityType.EVENT))
            {
                var byId = new Dictionary<int, List<Mod>>();
                foreach (var part in parts)
                    foreach (var e in part.Database[type].GetFullList())
                        if (e.ID > 0 && VanillaLoader.Vanilla?.Database[type].Has(e.ID) == true)
                        {
                            if (!byId.TryGetValue(e.ID, out var list))
                                byId[e.ID] = list = new List<Mod>();
                            if (!list.Contains(part))
                                list.Add(part);
                        }
                foreach (var (id, list) in byId.Where(kv => kv.Value.Count > 1).OrderBy(kv => kv.Key))
                {
                    var name = VanillaLoader.Vanilla!.Database[type].TryGetValue(id, out var v) ? v.Name : "";
                    report.Conflicts.Add($"game {type.ToString().ToLowerInvariant()} {id} {name}: changed by {string.Join(", then ", list.Select(p => p.DisplayName))}");
                }
            }
            var clearing = new[] { Command.CLEARALLEVENTS, Command.CLEARMERCS };
            foreach (var part in parts.Skip(1))
                foreach (var p in Renumbering.Lines(part).Where(p => clearing.Contains(p.Command)))
                        report.Conflicts.Add($"{part.DisplayName} has {(CommandsMap.TryGetString(p.Command, out var c) ? c : p.Command.ToString())} (line {p.LineNumber}): it clears what the parts before it added");
        }

        /// <summary>
        /// The references the game finds only by name (#startsite, an item's #spell and
        /// #autospell: StringOrIDRef writes them as names): if the name would find another
        /// entity in the merged file (the game takes the lowest number), the target is renamed
        /// with its part's name. The last resort (the user): everything else goes by number.
        /// </summary>
        private static void RenameClashes(List<Mod> parts, List<Mod> separate, MergeReport report)
        {
            var nameOnly = new[] { Command.STARTSITE, Command.SPELL, Command.AUTOSPELL };
            var everyone = new List<Mod>();
            if (VanillaLoader.Vanilla != null)
                everyone.Add(VanillaLoader.Vanilla);
            everyone.AddRange(separate);
            everyone.AddRange(parts);
            foreach (var part in parts)
                foreach (var r in Renumbering.Lines(part).OfType<StringOrIDRef>().Where(r => nameOnly.Contains(r.Command)).ToList())
                    {
                        if (!r.TryGetEntity(out var target) || target == null || !target.TryGetName(out var name) || string.IsNullOrEmpty(name))
                            continue;
                        var type = target.GetEntityType();
                        var first = everyone.SelectMany(m => m.Database[type].GetFullList())
                            .Where(x => x.TryGetName(out var n) && string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                            .OrderBy(x => x.ID > 0 ? x.ID : int.MaxValue).FirstOrDefault();
                        if (first == null || ReferenceEquals(first, target) || first.ID == target.ID)
                            continue;
                        var owner = parts.FirstOrDefault(p => ReferenceEquals(p, target.ParentMod));
                        var nameLine = target.Properties.OfType<NameProperty>().LastOrDefault();
                        if (owner == null || nameLine == null)
                        {
                            report.Notes.Add($"{part.DisplayName}: {Line(r)} means {Describe(target)}, but the name finds {Describe(first)} in the merged file, and that name can't be changed here: fix it by hand");
                            continue;
                        }
                        var renamed = $"{name} ({Short(owner)})";
                        nameLine.Value = renamed;
                        report.Renames.Add($"{owner.DisplayName}'s {type.ToString().ToLowerInvariant()} \"{name}\" #{target.ID} -> \"{renamed}\": {Line(r)} in {part.DisplayName} would have found {Describe(first)}");
                    }
        }

        private static string Line(Property p) => (CommandsMap.TryGetString(p.Command, out var c) ? c : p.Command.ToString()) + (p.LineNumber > 0 ? $" (line {p.LineNumber})" : "");

        private static string Describe(IDEntity e) => $"{(e.TryGetName(out var n) ? n : "")} #{e.ID} ({e.ParentMod?.DisplayName ?? "the game"})";

        private static string Short(Mod m)
        {
            var name = m.DisplayName;
            return name.Length <= 24 ? name : name.Substring(0, 24).TrimEnd();
        }

        /// <summary>
        /// The images and sounds a part names (paths relative to its .dm): copied into a folder
        /// for the part next to the merged file, the paths written to match.
        /// </summary>
        private static void CopyFiles(List<Mod> parts, string outDir, MergeReport report)
        {
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in parts)
            {
                var folder = string.Concat(Path.GetFileNameWithoutExtension(part.FullFilePath).Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_'));
                while (!folders.Add(folder))
                    folder += "_";
                var partDir = Path.GetDirectoryName(part.FullFilePath)!;
                int copied = 0, missing = 0;
                foreach (var p in Renumbering.Lines(part).OfType<FilePathProperty>().Where(p => PathCommands.Contains(p.Command)))
                    {
                        // the path the game reads: up to the closing quote (Confluence has text after it)
                        var named = (p.Value ?? "").Split('"')[0].Trim();
                        if (named.Length == 0)
                            continue;
                        var source = Imaging.ModFiles.Resolve(named, part.FullFilePath);
                        if (source == null || !File.Exists(source))
                        {
                            missing++;
                            continue;
                        }
                        var relative = Path.GetRelativePath(partDir, source);
                        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
                            relative = Path.Combine("_outside", Path.GetFileName(source));
                        var target = Path.Combine(outDir, folder, relative);
                        if (!File.Exists(target))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                            File.Copy(source, target);
                            copied++;
                        }
                        p.Value = (folder + "/" + relative).Replace('\\', '/');
                    }
                if (copied > 0 || missing > 0)
                    report.Notes.Add($"{part.DisplayName}: {copied} image/sound files copied into {folder}/" + (missing > 0 ? $"; {missing} named files weren't found (their lines are kept as written)" : ""));
            }
        }

        /// <summary>
        /// OUT.map.json, for the game-reading referee (tools/dom6exe/gameread.py --merge): the parts
        /// and what moved in each, which kind of number each command's argument is (per pass, from
        /// the core's references) and how a spell's #damage reads by its effect.
        /// </summary>
        /// <summary>
        /// Per spell of a part, what its #damage is when its effect comes from elsewhere (a
        /// #copyspell, an earlier block): by its number (as the part writes it, before moves) or
        /// its name. Worked out before copies are turned to snapshots (whose effect only the game
        /// data has).
        /// </summary>
        private static Dictionary<Mod, Dictionary<string, string>> SpellKinds(List<Mod> parts, Dictionary<Mod, Dictionary<(string Kind, int Old), int>> moved)
        {
            string? DamageKind(Spell spell) => spell.IsEnchant() ? Kind(EntityType.ENCHANTMENT) : spell.IsEventEffect() ? Kind(EntityType.EVENT_CODE_EFFECT)
                : spell.IsSummon() ? "monster or tag" : null;
            var all = new Dictionary<Mod, Dictionary<string, string>>();
            foreach (var p in parts)
            {
                var back = moved[p].Where(m => m.Key.Kind == "spell").ToDictionary(m => m.Value, m => m.Key.Old);
                var kinds = all[p] = new Dictionary<string, string>();
                foreach (var spell in p.Database[EntityType.SPELL].GetFullList().OfType<Spell>())
                    if (DamageKind(spell) is string k)
                        kinds[spell.ID > 0 ? (back.TryGetValue(spell.ID, out var old) ? old : spell.ID).ToString() : (spell.Name ?? "").ToLowerInvariant()] = k;
            }
            return all;
        }

        /// <summary>
        /// The map of a renumbered mod (the Renumber tool) for the referee, as a merge of one part:
        /// <c>gameread.py --merge NEW.dm --map MAP.json</c> reads the mod's file as it was, numbers
        /// moved as listed, against the renumbered one.
        /// </summary>
        public static void WriteRenumberMap(string path, Mod mod, IReadOnlyList<RenumberMove> moves)
        {
            var parts = new List<Mod> { mod };
            var separate = mod.Below().Where(m => m != VanillaLoader.Vanilla && !string.IsNullOrEmpty(m.FullFilePath)).Reverse().ToList();
            var moved = new Dictionary<Mod, Dictionary<(string Kind, int Old), int>>
            {
                [mod] = moves.ToDictionary(m => (Kind(m.Type), m.Old), m => m.New),
            };
            var strays = new Dictionary<Mod, Dictionary<(string Kind, int Old), int>> { [mod] = new() };
            WriteMap(path, parts, separate, moved, new List<Snapshot>(), strays, SpellKinds(parts, moved));
        }

        private static void WriteMap(string path, List<Mod> parts, List<Mod> separate, Dictionary<Mod, Dictionary<(string Kind, int Old), int>> moved, List<Snapshot> snapshots,
                                     Dictionary<Mod, Dictionary<(string Kind, int Old), int>> strays, Dictionary<Mod, Dictionary<string, string>> spellKinds)
        {
            var passes = new Dictionary<string, Dictionary<string, string>>();
            var probe = new Mod();
            foreach (EntityType type in Enum.GetValues<EntityType>())
            {
                Type? cls;
                try { cls = probe.TypeOf(type); }
                catch (Exception) { continue; }
                if (cls == null || !typeof(IDEntity).IsAssignableFrom(cls) || cls.IsAbstract)
                    continue;
                Dictionary<Command, Func<Property>> map;
                try { map = ((IDEntity)Activator.CreateInstance(cls)!).GetPropertyMap(); }
                catch (Exception) { continue; }
                var kinds = new Dictionary<string, string>();
                foreach (var (command, create) in map)
                {
                    if (!CommandsMap.TryGetString(command, out var text))
                        continue;
                    Property made;
                    try { made = create(); }
                    catch (Exception) { continue; }
                    var kind = made switch
                    {
                        SpellDamage => "spell damage",
                        MonsterOrMontagRef => "monster or tag",
                        MontagIDRef => Kind(EntityType.MONTAG),
                        EventCodeRef => Kind(EntityType.EVENT_CODE),
                        EventVarRef => Kind(EntityType.EVENT_VAR),
                        EnchIDRef => Kind(EntityType.ENCHANTMENT),
                        RestrictedItemIDRef => Kind(EntityType.RESTRICTED_ITEM),
                        EventEffectCodeRef => Kind(EntityType.EVENT_CODE_EFFECT),
                        Reference r => SafeKind(r),
                        _ => null,
                    };
                    if (kind != null)
                        kinds[text.TrimStart('#')] = kind;
                }
                passes[type == EntityType.MERCENARY ? "merc" : type.ToString().ToLowerInvariant()] = kinds;
            }
            // a spell's #damage: what it is depends on the spell's effect
            var effects = new SortedDictionary<int, string>();
            foreach (var spell in parts.SelectMany(p => p.Database[EntityType.SPELL].GetFullList()).OfType<Spell>())
                if (spell.TryGetSpellEffect(out int effect) && !effects.ContainsKey(effect))
                {
                    var kind = spell.IsEnchant() ? Kind(EntityType.ENCHANTMENT) : spell.IsEventEffect() ? Kind(EntityType.EVENT_CODE_EFFECT)
                        : spell.IsSummon() ? "monster or tag" : null;
                    if (kind != null)
                        effects[effect] = kind;
                }
            var json = new
            {
                parts = parts.Select(p => new
                {
                    file = p.FullFilePath,
                    needs = p.Dependencies.FirstOrDefault(d => d != VanillaLoader.Vanilla)?.FullFilePath,
                    moves = moved[p].GroupBy(m => m.Key.Kind).ToDictionary(g => g.Key, g => g.ToDictionary(m => m.Key.Old.ToString(), m => m.Value)),
                    spells = spellKinds[p],
                    strays = strays[p].GroupBy(m => m.Key.Kind).ToDictionary(g => g.Key, g => g.ToDictionary(m => m.Key.Old.ToString(), m => m.Value)),
                    copies = snapshots.Where(s => s.Copies.ContainsKey(p)).GroupBy(s => Kind(s.Type))
                        .ToDictionary(g => g.Key, g => g.ToDictionary(s => s.Of.ToString(), s => s.Number)),
                }),
                snapshots = snapshots.Select(s => new
                {
                    kind = Kind(s.Type), of = s.Of, number = s.Number,
                    header = CommandsMap.TryGetString(SnapshotForm[s.Type].Header, out var h) ? h.TrimStart('#') : "",
                    copies = SnapshotForm[s.Type].Copy.Select(c => CommandsMap.TryGetString(c, out var cs) ? cs.TrimStart('#') : ""),
                    // the line that hides it, and the game's value each copy of it gets back after the copy
                    hide = SnapshotForm[s.Type].Hide is { } hide && CommandsMap.TryGetString(hide.Line, out var hs) ? new object[] { hs.TrimStart('#'), hide.Value } : null,
                    restore = s.Restore,
                }),
                separate = separate.Select(s => s.FullFilePath),
                commands = passes,
                spell_effects = effects.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            };
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(json, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        private static string? SafeKind(Reference r)
        {
            try { return Kind(r.GetEntityType()); }
            catch (Exception) { return null; }
        }

        private static void Write(List<Mod> parts, string outputFile, string modName, MergeReport report, List<Snapshot> snapshots)
        {
            var newLine = parts[0].SourceNewLine ?? "\n";
            using var writer = new StreamWriter(outputFile, false, new UTF8Encoding(false)) { NewLine = newLine };
            var description = $"Merged from {string.Join(", ", parts.Select(p => p.DisplayName + (string.IsNullOrEmpty(p.Version) ? "" : " " + p.Version)))}, in this order. " +
                              $"Made with Dom6 Mod Editor; what moved is in {Path.GetFileNameWithoutExtension(outputFile)}.report.md.";
            writer.WriteLine(CommandsMap.Format(Command.MODNAME, modName, true));
            writer.WriteLine(CommandsMap.Format(Command.DESCRIPTION, description, true));
            writer.WriteLine(CommandsMap.Format(Command.VERSION, "1.00"));
            var domVersion = parts.Select(p => p.DomVersion).Where(v => !string.IsNullOrEmpty(v))
                .OrderByDescending(v => decimal.TryParse(v, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0).FirstOrDefault();
            if (domVersion != null)
                writer.WriteLine(CommandsMap.Format(Command.DOMVERSION, domVersion));
            if (snapshots.Count > 0)
            {
                writer.WriteLine();
                writer.WriteLine("-- ======== Game entities as they are before the parts change them (later parts copy these: see the report) ========");
                foreach (var s in snapshots)
                {
                    var (header, copies, hide) = SnapshotForm[s.Type];
                    var name = VanillaLoader.Vanilla?.Database[s.Type].TryGetValue(s.Of, out var v) == true ? v.Name : "";
                    writer.WriteLine($"{(CommandsMap.TryGetString(header, out var h) ? h : "")} {s.Number} -- the game's {name} #{s.Of}");
                    foreach (var c in copies)
                        writer.WriteLine($"{(CommandsMap.TryGetString(c, out var cs) ? cs : "")} {s.Of}");
                    if (hide is { } hd && CommandsMap.TryGetString(hd.Line, out var hs))
                        writer.WriteLine($"{hs} {hd.Value} -- kept out of the game: only the copies of it are used (they get the game's value back)");
                    writer.WriteLine("#end");
                }
            }
            foreach (var part in parts)
            {
                writer.WriteLine();
                writer.WriteLine($"-- ======== {part.DisplayName} ({Path.GetFileName(part.FullFilePath)}) ========");
                part.ExportBody(writer);
            }
        }
    }
}
