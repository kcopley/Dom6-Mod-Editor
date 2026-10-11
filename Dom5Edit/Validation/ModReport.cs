using System.Text;
using System.Text.RegularExpressions;
using Dom5Edit.Entities;
using Dom5Edit.GameData;

namespace Dom5Edit.Validation
{
    /// <summary>
    /// A report for a mod's author, or for anyone who opens the mod: what the editor found, worst
    /// first, each with its line in the file and what the game does with it. Four parts: what goes
    /// wrong in game (events that can't happen or lose lines, lines the game reads differently than
    /// they look, numbers taken from the game), lines the game ignores (with "did you mean" for a
    /// misspelt command, or the type whose blocks read it), numbers that neither this mod nor the
    /// game has (fine when they come from a mod this one needs), and things worth a look. What it
    /// says about the game comes from Dominions6.exe (tools/dom6exe) and the modding manuals.
    /// <see cref="Build"/> gives it as data (the editor's report window), <see cref="Write"/> as
    /// Markdown (to send to the author).
    /// </summary>
    public static class ModReport
    {
        /// <summary>One line of the file the report points at: its number, its text, and the entity whose block holds it.</summary>
        public sealed record Line(int? Number, string Text, IDEntity? Entity);

        /// <summary>Alike findings (the same message), with their lines.</summary>
        public sealed record Group(string Text, IReadOnlyList<Line> Lines);

        public sealed record Section(string Key, string Title, string Intro, int Count, IReadOnlyList<Group> Groups);

        public sealed record Report(string Name, string About, IReadOnlyList<Section> Sections)
        {
            public int Count(string key) => Sections.FirstOrDefault(s => s.Key == key)?.Count ?? 0;
            public int Total => Sections.Sum(s => s.Count);
        }

        public const string Wrong = "wrong", Ignored = "ignored", Missing = "missing", Look = "look";

        public static string Write(Mod mod, ValidationResult validation, IEnumerable<Events.EventProblem>? eventProblems = null) =>
            Markdown(Build(mod, validation, eventProblems));

        public static Report Build(Mod mod, ValidationResult validation, IEnumerable<Events.EventProblem>? eventProblems = null)
        {
            var file = mod.FullFilePath;
            string[] lines = Array.Empty<string>();
            try
            {
                if (!string.IsNullOrEmpty(file) && File.Exists(file))
                    lines = File.ReadAllLines(file);
            }
            catch (IOException)
            {
                // quoted lines are left out
            }
            Line At(int? line, IDEntity? entity = null)
            {
                // a finding about a whole entity (its number, ...): its block's first line
                if ((line is not int given || given <= 0) && entity != null)
                    line = mod.SourceBlocks.FirstOrDefault(b => ReferenceEquals(b.Entity, entity))?.HeaderLine;
                return new Line(line is int n && n > 0 ? n : null,
                    line is int k && k >= 1 && k <= lines.Length ? lines[k - 1].Trim() : "",
                    entity ?? (line is int m && m > 0 ? mod.EntityAt(m) : null));
            }

            var wrong = new List<Item>();
            var ignored = new List<Item>();
            var missing = new List<Item>();
            var look = new List<Item>();

            // lines the game doesn't read, from the parser
            foreach (var p in mod.ParseIssues)
            {
                // a #copystats after lines that set what it copies (name, stats, abilities: the exe)
                var copy = Regex.Match(p.Message ?? "", @"^#copystats at line \d+ overwrites \d+ previously defined property\(s\): (.*)$");
                if (p.IssueType == ParseIssueType.PropertiesClearedBySubsequentClear && copy.Success)
                {
                    var replaced = string.Join(", ", copy.Groups[1].Value.Split(", ").Distinct());
                    look.Add(new Item(At(p.LineNumber), $"#copystats replaces what the lines before it in the block set ({replaced}): lines meant to change the copy go after it"));
                    continue;
                }
                if (p.IssueType != ParseIssueType.InvalidCommand && p.IssueType != ParseIssueType.NotReadByGame)
                    continue;
                // (the whole word as written: #1com, #school-1)
                var command = Regex.Match(p.Message ?? "", @"#[^\s'""]+").Value;
                var typeName = Regex.Match(p.Message ?? "", @"for:? ([A-Z][a-z]+)").Groups[1].Value;
                Enum.TryParse<EntityType>(typeName, true, out var type);
                // a number glued to a command of this type ("#school-1"): which commands the game reads
                // that way isn't known to the editor (tools/dom6exe/README.md "Commands"), so it isn't called ignored
                var glued = Regex.Match(command, @"^#([A-Za-z0-9_]+)([-+]\d.*)$"); // (the game's matcher wants no letter or digit after the name)
                if (glued.Success && typeName.Length > 0 && GameCommandCatalog.CommandsOf(type).Contains(glued.Groups[1].Value))
                {
                    look.Add(new Item(At(p.LineNumber), $"`{command}` has no space before its number: the editor reads it as an unknown line " +
                        $"(the game reads some commands this way, others not); written `#{glued.Groups[1].Value} {glued.Groups[2].Value}`, both read it"));
                    continue;
                }
                ignored.Add(new Item(At(p.LineNumber), IgnoredWhy(command, type, typeName)));
            }

            // references, numbers, names, lines read differently, from Validate
            foreach (var v in validation.Issues)
            {
                if (v.Category == "Invalid Command" || v.Severity == ValidationSeverity.Info)
                    continue; // (the parser's notes above say it better)
                var item = new Item(At(v.LineNumber ?? v.Property?.LineNumber, v.Entity as IDEntity), Explain(v));
                (v.Category == "Reference" && (v.Message ?? "").StartsWith("Unresolved reference") ? missing
                    : v.Severity == ValidationSeverity.Error || v.Category == "Reference" ? wrong : look).Add(item);
            }

            // events
            foreach (var e in eventProblems ?? Array.Empty<Events.EventProblem>())
            {
                var line = e.Line?.LineNumber is int n && n > 0 ? n
                    : e.Event.Properties.Select(p => p.LineNumber).Where(n2 => n2 > 0).DefaultIfEmpty(0).Min();
                (e.IsError ? wrong : look).Add(new Item(At(line, e.Event), e.Message));
            }

            // spells: effects the game has no case for, spells nothing can cast
            Spells(mod, At, wrong, look);

            // a number on #newspell/#newitem/#newnation: the game reads none (the exe: each takes the
            // first free one); a #select of that number would be another entity
            foreach (var block in mod.SourceBlocks.Where(b => !b.Selected))
            {
                var m = Regex.Match(block.RawHeader ?? block.Header ?? "", @"^\s*#new(spell|item|nation)\s+(\d+)");
                if (!m.Success)
                    continue;
                string kind = m.Groups[1].Value, from = kind == "spell" ? "1500" : kind == "item" ? "700" : "120";
                look.Add(new Item(At(block.HeaderLine, block.Entity),
                    $"#new{kind} takes no number: the game gives the new {kind} the first free one (from {from}), so {m.Groups[2].Value} isn't its number " +
                    $"(a #select{kind} {m.Groups[2].Value} elsewhere is another {kind}); #select{kind} {m.Groups[2].Value} makes one with that number"));
            }

            var with = mod.Below().Where(d => d != VanillaLoader.Vanilla && !string.IsNullOrEmpty(d.FullFilePath))
                .Reverse().Select(d => $"`{Path.GetFileName(d.FullFilePath)}`").ToList();
            var about = $"File: `{Path.GetFileName(file)}`" + (string.IsNullOrEmpty(mod.Version) ? "" : $", version {mod.Version}") +
                        (with.Count > 0 ? $", together with {string.Join(", ", with)}" : "") +
                        $". Checked {DateTime.Now:yyyy-MM-dd} against Dominions {GameCommandCatalog.GameVersion ?? "6"} (what the game reads is taken from the game itself).";
            return new Report(mod.ModName ?? Path.GetFileNameWithoutExtension(file) ?? "mod", about, new[]
            {
                new Section(Wrong, "Goes wrong in game", "These change what happens in game, or keep something from happening.", wrong.Count, Grouped(wrong)),
                new Section(Ignored, "Lines the game ignores", "The game skips these lines: they change nothing.", ignored.Count, Grouped(ignored)),
                new Section(Missing, "Numbers not in this mod or the game", "Fine if they come from another mod this one needs, loaded with it; if not, these lines fail in game.", missing.Count, ByKind(missing)),
                new Section(Look, "Worth a look", "Not wrong as such, but probably not what was meant.", look.Count, Grouped(look)),
            });
        }

        private sealed record Item(Line Line, string Why);

        /// <summary>
        /// The mod's spells: an #effect the game has no case for (combat effects 1000-9999, a
        /// ritual effect the ritual code doesn't know: SpellEffectData.NotHandledWhy, read from
        /// Dominions6.exe), and a spell of its own that nothing can cast: no #school line at all
        /// (nobody can research it) and nothing names it (DomEnhanced's Gjallarhorn spell; Bloodwar's
        /// "Contact Lamyros", paths but no school or effect). A #school -1 is left alone: the
        /// usual way to switch a spell off (DomEnhanced One Age's 83).
        /// </summary>
        private static void Spells(Mod mod, Func<int?, IDEntity?, Line> at, List<Item> wrong, List<Item> look)
        {
            if (!mod.Database.TryGetValue(EntityType.SPELL, out var set))
                return;
            var own = set.GetFullList().OfType<Spell>().Where(s => ReferenceEquals(s.ParentMod, mod)).ToList();
            if (own.Count == 0)
                return;
            int? HeaderLine(IDEntity e) => mod.SourceBlocks.FirstOrDefault(b => ReferenceEquals(b.Entity, e))?.HeaderLine;
            foreach (var spell in own)
                foreach (var p in spell.Properties.OfType<Props.IntProperty>().Where(p => p.Command == Commands.Command.EFFECT))
                    if (SpellEffectData.Instance.NotHandledWhy(p.Value) is string why)
                        wrong.Add(new Item(at(p.LineNumber > 0 ? p.LineNumber : HeaderLine(spell), spell), why));

            // what names a spell: every reference to one in the mod and the mods it's read over
            // (#spell, #autospell, #nextspell, #onebattlespell, ...), by the spell, its number or its name
            var named = new HashSet<IDEntity>(ReferenceEqualityComparer.Instance);
            var numbers = new HashSet<int>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var layer in new[] { mod }.Concat(mod.Below()))
                foreach (var s in layer.Database.Values)
                    foreach (var e in s.GetFullList())
                        foreach (var r in e.Properties.OfType<Props.Reference>())
                        {
                            if (r is Props.SpellDamage)
                                continue; // (its number is a unit, an enchantment, ...: never a spell)
                            if (r.TryGetEntity(out var target) && target is Spell)
                            {
                                named.Add(target);
                                if (target.ID > 0) numbers.Add(target.ID);
                                if (!string.IsNullOrEmpty(target.Name)) names.Add(target.Name);
                                continue;
                            }
                            bool spellRef;
                            try { spellRef = r.GetEntityType() == EntityType.SPELL; }
                            catch (NotImplementedException) { spellRef = false; }
                            if (spellRef && r is Props.StringOrIDRef sr)
                            {
                                if (sr.ID > 0) numbers.Add(sr.ID);
                                if (!string.IsNullOrEmpty(sr.Name)) names.Add(sr.Name);
                            }
                        }
            var vanilla = VanillaLoader.Vanilla?.Database[EntityType.SPELL];
            var resolver = Resolve.ModResolver.For(mod);
            foreach (var spell in own)
            {
                // the mod's own spells (not its changes to the game's or a needed mod's, by number or name)
                if (spell.ID > 0 && (vanilla?.Has(spell.ID) == true || mod.FindBelow(EntityType.SPELL, spell.ID, null) != null)
                    || spell.Selected && spell.ID <= 0)
                    continue;
                if (named.Contains(spell) || spell.ID > 0 && numbers.Contains(spell.ID) || !string.IsNullOrEmpty(spell.Name) && names.Contains(spell.Name))
                    continue;
                // (any #school, -1 too, is the author's choice; a #copyspell brings the copied one's)
                if (spell.Properties.Any(p => p.Command == Commands.Command.SCHOOL || p.Command == Commands.Command.COPYSPELL)
                    || resolver.Resolve(spell).Get(Commands.Command.SCHOOL) != null)
                    continue;
                look.Add(new Item(at(HeaderLine(spell), spell),
                    "nothing casts this spell: it has no #school, so nobody can research it, and no unit, item, event or other spell " +
                    "names it (#spell, #autospell, #nextspell, ...): left over, or unfinished?"));
            }
        }

        /// <summary>Alike ones together (the same message but for its numbers), most first, each group's lines in file order.</summary>
        private static List<Group> Grouped(List<Item> items) =>
            items.GroupBy(i => Regex.Replace(i.Why, @"-?\d+", "N")).OrderByDescending(g => g.Count())
                .Select(g => new Group(g.First().Why + (g.Count() > 1 ? $" ({g.Count()} lines)" : ""), g.Select(i => i.Line).OrderBy(l => l.Number ?? 0).ToList()))
                .ToList();

        /// <summary>References to numbers nothing has, by kind ("monster 15992"): the numbers, then the lines.</summary>
        private static List<Group> ByKind(List<Item> items) =>
            items.GroupBy(i => i.Why.Split(' ')[0]).OrderByDescending(g => g.Count())
                .Select(g =>
                {
                    var numbers = g.Select(i => i.Why.Split(' ').Last()).Distinct().ToList();
                    return new Group($"{g.Key} {string.Join(", ", numbers.Take(30))}{(numbers.Count > 30 ? $" and {numbers.Count - 30} more" : "")} ({g.Count()} lines)",
                        g.Select(i => i.Line).OrderBy(l => l.Number ?? 0).ToList());
                })
                .ToList();

        /// <summary>The report as Markdown, for the author.</summary>
        public static string Markdown(Report report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {report.Name}: what the mod editor found");
            sb.AppendLine();
            sb.AppendLine(report.About);
            sb.AppendLine();
            foreach (var s in report.Sections)
                if (s.Count > 0 || s.Key != Missing)
                    sb.AppendLine($"- **{s.Title}:** {s.Count}");
            foreach (var s in report.Sections.Where(s => s.Count > 0))
            {
                int shown = s.Key == Missing ? 5 : 12;
                sb.AppendLine();
                sb.AppendLine($"## {s.Title} ({s.Count})");
                sb.AppendLine();
                sb.AppendLine(s.Intro);
                sb.AppendLine();
                foreach (var g in s.Groups)
                {
                    sb.AppendLine($"- {g.Text}");
                    foreach (var l in g.Lines.Take(shown))
                        sb.AppendLine(l.Number is int n ? $"  - line {n}: `{Trim(l.Text)}`"
                            : $"  - {(l.Text.Length > 0 ? Trim(l.Text) : l.Entity != null ? $"{l.Entity.GetEntityType().ToString().ToLowerInvariant()} {l.Entity.Name ?? "#" + l.Entity.ID}" : "(no line)")}");
                    if (g.Lines.Count > shown)
                        sb.AppendLine($"  - and {g.Lines.Count - shown} more");
                }
            }
            if (report.Total == 0)
                sb.AppendLine("\nNothing found.");
            return sb.ToString();
        }

        // "an item", "an armor", "a monster"
        private static string A(string word) => word.Length > 0 && "aeiou".Contains(char.ToLowerInvariant(word[0])) ? "an" : "a";

        private static string Trim(string s) => (s.Length > 110 ? s.Substring(0, 110) + "…" : s).Replace("`", "'");

        /// <summary>Why the game skips a command: misspelt (with the likeliest meant one), another type's command, or not in Dominions 6.</summary>
        private static string IgnoredWhy(string command, EntityType type, string typeName)
        {
            var name = command.TrimStart('#').ToLowerInvariant();
            if (typeName.Length == 0)
            {
                // outside a block: only a block's header or a mod-level command is read there
                var outside = Suggest(name, BlockHeaders);
                return outside.Length > 0 ? $"`{command}` isn't a Dominions 6 command" + outside + " (as written, the block after it has no header)"
                    : $"`{command}` isn't a Dominions 6 command (outside a block, maybe one from an older version)";
            }
            var kind = typeName.ToLowerInvariant();
            var read = GameCommandCatalog.CommandsOf(type);
            var others = GameCommandCatalog.ContextsReading(name).ToList();
            if (others.Count > 0)
            {
                var where = string.Join("/", others.Take(3));
                return $"`{command}` isn't read in {A(kind)} {kind} block (it's {A(where)} {where} command)";
            }
            var meant = Suggest(name, read);
            return meant.Length > 0 ? $"`{command}` isn't a Dominions 6 command" + meant
                : $"`{command}` isn't a Dominions 6 command for {A(kind)} {kind} (maybe one from an older version)";
        }

        // the commands that start a block (#newevent, #newmerc, ... aren't in the catalog's top level)
        private static readonly string[] BlockHeaders =
        {
            "newmonster", "selectmonster", "newweapon", "selectweapon", "newarmor", "selectarmor", "newitem", "selectitem",
            "newspell", "selectspell", "newsite", "selectsite", "newnation", "selectnation", "newevent", "selectevent", "newmerc",
            "selectnametype", "selectpoptype", "selectbless", "newtemplate", "selectsound",
        };

        /// <summary>"; did you mean #x?" for the closest command the game reads here (one or two typos away), or "".</summary>
        private static string Suggest(string name, IReadOnlyCollection<string> candidates)
        {
            if (name.Length < 4 || candidates.Count == 0)
                return "";
            int best = int.MaxValue;
            string? meant = null;
            foreach (var c in candidates)
            {
                if (Math.Abs(c.Length - name.Length) > 2)
                    continue;
                int d = Distance(name, c);
                // (on a tie, the one inside what was typed: #1com meant #com, not #2com)
                if (d < best || d == best && meant != null && name.Contains(c) && !name.Contains(meant))
                {
                    best = d;
                    meant = c;
                }
            }
            int allowed = name.Length <= 6 ? 1 : 2;
            return meant != null && best <= allowed ? $"; did you mean `#{meant}`?" : "";
        }

        /// <summary>Edit distance with transpositions (magciboost -> magicboost is 1).</summary>
        private static int Distance(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            return d[a.Length, b.Length];
        }

        /// <summary>The numbers the mod refers to that neither it nor the mods under it define (the "missing" section), each once.</summary>
        public static IReadOnlyList<(EntityType Type, int Id)> MissingNumbers(ValidationResult validation)
        {
            var found = new List<(EntityType, int)>();
            foreach (var v in validation.Issues)
            {
                var m = Regex.Match(v.Message ?? "", @"Unresolved reference to (\w+) ID (\d+)");
                if (m.Success && Enum.TryParse<EntityType>(m.Groups[1].Value, true, out var type) && !found.Contains((type, int.Parse(m.Groups[2].Value))))
                    found.Add((type, int.Parse(m.Groups[2].Value)));
            }
            return found;
        }

        /// <summary>A validation issue said for an author.</summary>
        private static string Explain(ValidationIssue v)
        {
            var m = Regex.Match(v.Message ?? "", @"Unresolved reference to (\w+) ID (-?\d+)");
            if (m.Success)
            {
                var kind = m.Groups[1].Value.ToLowerInvariant();
                return $"{kind} {m.Groups[2].Value}";
            }
            return v.Message ?? "";
        }
    }
}
