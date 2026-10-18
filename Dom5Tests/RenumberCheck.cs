using Dom5Edit;
using Dom5Edit.Entities;
using Dom5Edit.Merge;
using Dom5Edit.Resolve;

namespace Dom5Tests
{
    /// <summary>
    /// Dom5Tests renumber MOD.dm OUT.dm TYPE OLD NEW [TYPE OLD NEW]...: moves entities to other
    /// numbers (Merge.Renumbering), saves, reads the save back and compares every entity's values
    /// with the moved model's; prints the lines that changed. docs/MERGING.md, step 1.
    /// </summary>
    internal static class RenumberCheck
    {
        public static void Run(string basePath, string[] args)
        {
            if (args.Contains("--start") || args.Contains("--survey"))
            {
                Tool(basePath, args);
                return;
            }
            Program.LoadVanillaBase(basePath);
            var mod = Mod.Import(args[1]);
            var outPath = args[2];
            for (int i = 3; i + 2 < args.Length; i += 3)
            {
                var type = Enum.Parse<EntityType>(args[i], true);
                int old = int.Parse(args[i + 1]), now = int.Parse(args[i + 2]);
                if (!mod.Database[type].TryGetValue(old, out var e) || e.ParentMod != mod)
                    throw new ArgumentException($"{mod.DisplayName} has no {type} {old} of its own");
                Renumbering.Move(e, now, new[] { mod });
                Console.WriteLine($"moved {type} {old} -> {now} ({e.Name})");
            }
            mod.Export(outPath);
            // the lines the save changed
            var before = File.ReadAllLines(args[1]);
            var after = File.ReadAllLines(outPath);
            int changed = 0;
            for (int k = 0; k < Math.Min(before.Length, after.Length); k++)
                if (before[k] != after[k] && changed++ < 30)
                    Console.WriteLine($"   line {k + 1}: {before[k].Trim()}  ->  {after[k].Trim()}");
            Console.WriteLine($"{changed} lines changed ({before.Length} -> {after.Length} lines)");
            // read back: every entity the same as the moved model
            var back = Mod.Import(outPath);
            int entities = 0, differ = 0;
            foreach (var (type, set) in mod.Database)
                foreach (var e in set.GetFullList().Where(x => x.ID > 0 && set.TryGetValue(x.ID, out var held) && ReferenceEquals(held, x)))
                {
                    entities++;
                    if (!back.Database[type].TryGetValue(e.ID, out var b))
                    {
                        differ++;
                        Console.WriteLine($"   missing after reading back: {type} {e.ID}");
                        continue;
                    }
                    var x = Values(ModResolver.For(mod).Resolve(e));
                    var y = Values(ModResolver.For(back).Resolve(b));
                    if (!x.SequenceEqual(y) && differ++ < 10)
                        Console.WriteLine($"   {type} {e.ID}: {string.Join(" | ", x.Except(y).Take(3))}  vs  {string.Join(" | ", y.Except(x).Take(3))}");
                }
            Console.WriteLine($"read back: {entities} entities, {differ} differ");
        }

        /// <summary>
        /// Dom5Tests renumber MOD.dm OUT.dm --start TYPE=N [--start TYPE=N]... [--game-numbers]
        /// [--with PARENT.dm]... [--json FILE] (or --survey): the Renumber tool on the mod, as the editor's window
        /// runs it (Merge.ModRenumber, ModEditor.Renumber): its numbers per type and the plan; the
        /// edit; OUT.dm saved as the editor saves (it must read back the same); OUT.map.json for
        /// the referee (python3 tools/dom6exe/gameread.py --merge OUT.dm --map OUT.map.json); then
        /// undo must save the mod as an unedited save does, byte for byte, and redo as OUT.dm.
        /// Exit code 1 when the plan can't be made or a check fails; --json writes the counts the
        /// fidelity suite keeps (tools/fidelity/run.mjs, stage 6). docs/RENUMBER.md.
        /// </summary>
        private static void Tool(string basePath, string[] args)
        {
            Program.LoadVanillaBase(basePath);
            Dom5Edit.ModBackups.Folder = Path.Combine(Path.GetTempPath(), "Dom5Tests-backups");
            string modPath = args[1], outPath = args[2];
            var starts = new Dictionary<EntityType, int>();
            bool gameNumbers = false, survey = false;
            string? json = null;
            Mod? below = null;
            for (int i = 3; i < args.Length; i++)
                switch (args[i])
                {
                    case "--start":
                        var kv = args[++i].Split('=', 2);
                        starts[Enum.Parse<EntityType>(kv[0], true)] = int.Parse(kv[1]);
                        break;
                    case "--game-numbers": gameNumbers = true; break;
                    case "--survey": survey = true; break;
                    case "--with": below = Mod.Import(args[++i], below); break;
                    case "--json": json = args[++i]; break;
                    default: throw new ArgumentException("unknown option " + args[i]);
                }
            var mod = Mod.Import(modPath, below);
            Console.WriteLine($"{mod.DisplayName}: its numbers");
            foreach (var t in ModRenumber.Survey(mod).Where(t => t.Count > 0 || t.OnGameNumbers.Count > 0))
                Console.WriteLine($"   {t.Kind,-8} {t.Count,5}  {(t.Count > 0 ? $"{t.Lowest}-{t.Highest}" : "-"),-12} (the game's end at {t.GameLast}, the last is {t.Last})"
                                  + (t.OnGameNumbers.Count > 0 ? $"; on the game's numbers: {string.Join(", ", t.OnGameNumbers.Select(e => e.ID))}" : ""));
            if (survey)
                return;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var context = new RenumberContext(mod);
            long prepared = watch.ElapsedMilliseconds;
            ModRenumber.Plan(context, starts, gameNumbers);
            watch.Restart();
            var plan = ModRenumber.Plan(context, starts, gameNumbers);
            Console.WriteLine($"   (worked out in {prepared} ms; planned again in {watch.ElapsedMilliseconds} ms, as at each start number typed)");
            foreach (var b in plan.Blocks)
                Console.WriteLine($"   {ModRenumber.Kind(b.Type),-8} {b.Count,5}  {b.OldLowest}-{b.OldHighest}  ->  {b.NewLowest}-{b.NewHighest}");
            foreach (var n in plan.Notes)
                Console.WriteLine("   note: " + n);
            foreach (var p in plan.Problems)
                Console.WriteLine("   PROBLEM: " + p);
            if (!plan.CanApply)
            {
                Console.WriteLine(plan.Problems.Count > 0 ? "nothing renumbered" : "nothing to renumber");
                Environment.ExitCode = 1;
                return;
            }
            var unedited = Path.ChangeExtension(outPath, ".unedited.dm");
            mod.ExportCopy(unedited);
            var editor = new Dom5Edit.Editing.ModEditor(mod);
            Dom5Edit.Editing.IModEdit edit;
            try
            {
                edit = editor.Renumber(plan);
            }
            catch (Dom5Edit.Editing.EditException ex)
            {
                Console.WriteLine("REFUSED: " + ex.Message);
                Environment.ExitCode = 1;
                return;
            }
            Console.WriteLine($"{edit.Description}: {plan.Moves.Count} moved");
            ModMerger.WriteRenumberMap(Path.ChangeExtension(outPath, ".map.json"), mod, plan.Moves);
            mod.SafeSave(outPath);
            // the lines the save changed
            var before = File.ReadAllLines(unedited);
            var after = File.ReadAllLines(outPath);
            int changed = 0;
            for (int k = 0; k < Math.Min(before.Length, after.Length); k++)
                if (before[k] != after[k] && changed++ < 12)
                    Console.WriteLine($"   line {k + 1}: {before[k].Trim()}  ->  {after[k].Trim()}");
            Console.WriteLine($"saved {Path.GetFileName(outPath)}: it reads back the same; {changed} lines changed ({before.Length} -> {after.Length} lines)");
            // undo: as it was; redo: as saved
            edit.Undo();
            var undone = Path.ChangeExtension(outPath, ".undone.dm");
            mod.ExportCopy(undone);
            bool undoOk = File.ReadAllBytes(undone).SequenceEqual(File.ReadAllBytes(unedited));
            edit.Redo();
            var redone = Path.ChangeExtension(outPath, ".redone.dm");
            mod.ExportCopy(redone);
            bool redoOk = File.ReadAllBytes(redone).SequenceEqual(File.ReadAllBytes(outPath));
            Console.WriteLine($"undo: {(undoOk ? "as it was, byte for byte" : "FAIL: differs from the unedited save")}; redo: {(redoOk ? "as saved, byte for byte" : "FAIL: differs from the save")}");
            if (!undoOk || !redoOk)
                Environment.ExitCode = 1;
            if (json != null)
                File.WriteAllText(json, System.Text.Json.JsonSerializer.Serialize(new
                {
                    moved = plan.Moves.Count,
                    movedByKind = plan.Moves.GroupBy(m => ModRenumber.Kind(m.Type)).ToDictionary(g => g.Key, g => g.Count()),
                    onGameNumbers = plan.Moves.Count(m => m.Old <= context.Survey.First(t => t.Type == m.Type).GameLast),
                    notes = plan.Notes.Count,
                    linesChanged = changed,
                    linesBefore = before.Length,
                    linesAfter = after.Length,
                    undo = undoOk,
                    redo = redoOk,
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }

        private static List<string> Values(ResolvedEntity r) =>
            r.Values.Select(v => ResolvedValue.ArgumentsOf(v.Property) is var a ? $"{v.Command} {a}" : "").ToList();
    }
}
