using System.Text;
using System.Text.RegularExpressions;
using Dom5Edit.Commands;
using Dom5Edit.Entities;
using Dom5Edit.Merge;
using Dom5Edit.Props;
using Dom5Edit.Resolve;

namespace Dom5Edit.Editing
{
    /// <summary>
    /// The Renumber tool's edit (Merge.ModRenumber): the plan's entities moved to their new
    /// numbers, every reference following. A line written as read keeps its text with only the
    /// numbers that moved replaced (the author's spacing and comments stay), a block's header the
    /// same; a name written with its number ("Militia (30)", a template's #form) takes the new
    /// one; a reference by number to the game's entity that a moved #new... replaced (the mod's
    /// in game) points at the moved one. If a name would find another entity afterwards (the
    /// game takes the lowest number for a name several share), nothing moves. Undo and redo put
    /// back exactly what was there.
    /// </summary>
    public sealed class RenumberEdit : IModEdit
    {
        private readonly Mod _mod;
        private readonly Action<IModEdit> _changed;
        private readonly List<RenumberMove> _moves;
        private readonly List<(StringOrIDRef Ref, IDEntity Before, IDEntity After)> _retargets = new();
        private readonly List<(StringOrIDRef Ref, string Before, string After)> _names = new();
        private readonly List<(StringProperty Line, string Before, string After)> _forms = new();
        private readonly List<(Property Line, string Raw, string Baseline, string RawAfter, string BaselineAfter)> _texts = new();
        private readonly List<(SourceBlock Block, string Raw, int Id, string RawAfter, int IdAfter)> _headers = new();
        private readonly List<(LineGroup Line, string Text, string TextAfter)> _lines = new();
        // the names the moved entities have, with every entity of the mod that has one, and what each found before
        private readonly List<(EntityType Type, string Name, IDEntity Entity)> _named = new();
        private readonly List<(EntityType Type, string Name, IDEntity? Holder)> _holders = new();

        public string Description { get; }
        public IReadOnlyCollection<IDEntity> Entities { get; }
        public IReadOnlyList<RenumberMove> Moves => _moves;

        private RenumberEdit(Mod mod, List<RenumberMove> moves, Action<IModEdit> changed)
        {
            _mod = mod;
            _moves = moves;
            _changed = changed;
            Entities = moves.Select(m => m.Entity).ToList();
            Description = "Renumber " + string.Join(", ", moves.GroupBy(m => m.Type).Select(g => $"{g.Count()} {ModRenumber.Kind(g.Key)}{(g.Count() == 1 ? "" : "s")}"));
        }

        /// <summary>Makes the plan's moves; throws an EditException (with everything as it was) if a name would find another entity.</summary>
        internal static RenumberEdit Apply(Mod mod, RenumberPlan plan, Action<IModEdit> changed)
        {
            if (!plan.CanApply)
                throw new EditException(plan.Problems.FirstOrDefault() ?? "Nothing to renumber");
            var edit = new RenumberEdit(mod, plan.Moves.ToList(), changed);
            edit.First();
            return edit;
        }

        private void First()
        {
            ModRenumber.ResolveAll(_mod);
            var moveOf = _moves.ToDictionary<RenumberMove, IDEntity, RenumberMove>(m => m.Entity, m => m, ReferenceEqualityComparer.Instance);
            // the lines whose text may change, as they're written now
            var exports = new Dictionary<Property, string>(ReferenceEqualityComparer.Instance);
            foreach (var p in Renumbering.Lines(_mod))
                if (p.RawText != null && p.BaselineExport != null && (p is Reference || p.Command == Command.FORM))
                    exports[p] = p.ToExportString();
            // what each name finds now
            var refs = ModRenumber.StringRefs(_mod).ToList();
            var found = new Dictionary<StringOrIDRef, (int Line, EntityType Type, IDEntity? Found)>(ReferenceEqualityComparer.Instance);
            foreach (var (line, r) in refs)
                if (r.IsStringRef && !string.IsNullOrEmpty(r.WrittenName) && ModRenumber.SafeType(r) is EntityType t && ModRenumber.Types.Contains(t))
                    found.TryAdd(r, (line, t, Find(t, r.WrittenName!)));
            var byName = _mod.SourceBlocks.Where(b => b.Selected && b.Header.Length > 0 && !ModRenumber.Number.IsMatch(b.Header.Split(' ')[0])
                                                      && ModRenumber.Types.Contains(b.Entity.GetEntityType()))
                .ToDictionary<SourceBlock, SourceBlock, IDEntity?>(b => b, b => Find(b.Entity.GetEntityType(), b.Header), ReferenceEqualityComparer.Instance);

            // a reference by number to the game's entity a moved #new... replaced: in game it was the mod's
            var vanilla = VanillaLoader.Vanilla;
            var onGame = _moves.Where(m => !m.Entity.Selected && vanilla != null && vanilla.Database[m.Type].Has(m.Old)).ToDictionary(m => (m.Type, m.Old), m => m.Entity);
            if (onGame.Count > 0)
                foreach (var (_, r) in refs)
                    if (!r.IsStringRef && r.TryGetEntity(out var target) && !ReferenceEquals(target.ParentMod, _mod)
                        && onGame.TryGetValue((target.GetEntityType(), target.ID), out var own))
                        _retargets.Add((r, target, own));
            // the names the moved entities have, and every entity of the mod with one of them
            var names = new HashSet<(EntityType, string)>();
            foreach (var p in Renumbering.Lines(_mod).OfType<NameProperty>())
                if (p.Parent != null && moveOf.ContainsKey(p.Parent) && !string.IsNullOrEmpty(p.Value))
                    names.Add((p.Parent.GetEntityType(), p.Value.ToLowerInvariant()));
            foreach (var m in _moves)
                if (m.Entity.HeaderName is string h && h.Length > 0)
                    names.Add((m.Type, h.ToLowerInvariant()));
            foreach (var p in Renumbering.Lines(_mod).OfType<NameProperty>())
                if (p.Parent != null && !string.IsNullOrEmpty(p.Value) && names.Contains((p.Parent.GetEntityType(), p.Value.ToLowerInvariant())))
                    _named.Add((p.Parent.GetEntityType(), p.Value, p.Parent));
            foreach (var e in Renumbering.Entities(_mod))
                if (e.HeaderName is string h && h.Length > 0 && names.Contains((e.GetEntityType(), h.ToLowerInvariant())))
                    _named.Add((e.GetEntityType(), h, e));
            foreach (var (type, name, _) in _named.DistinctBy(n => (n.Type, n.Name.ToLowerInvariant())))
                _holders.Add((type, name, _mod.Database[type].TryGetValueNamed(name, out var holder) ? holder : null));

            foreach (var (r, _, after) in _retargets)
                r.Retarget(after);
            Renumbering.MoveAll(_mod, _moves.Select(m => (m.Entity, m.New)).ToList());
            // names written with a number, and a template's #form "Dragon (265)"
            foreach (var (_, r) in refs)
                if (r.IsStringRef && r.TryGetEntity(out var target) && moveOf.TryGetValue(target, out var m) && r.FollowNamedNumber(m.Old, m.New) is string before)
                    _names.Add((r, before, r.WrittenName!));
            var monsters = _moves.Where(m => m.Type == EntityType.MONSTER).ToDictionary(m => m.Old, m => m.New);
            foreach (var p in Renumbering.Lines(_mod).OfType<StringProperty>().Where(p => p.Command == Command.FORM && p.Parent?.GetEntityType() == EntityType.TEMPLATE))
            {
                var fm = FormNumber.Match(p.Value ?? "");
                if (fm.Success && int.TryParse(fm.Groups[1].Value, out int n) && monsters.TryGetValue(n, out int now))
                {
                    var was = p.Value!;
                    p.Value = was.Substring(0, fm.Groups[1].Index) + now + was.Substring(fm.Groups[1].Index + fm.Groups[1].Length);
                    _forms.Add((p, was, p.Value));
                }
            }
            Rename();

            // a name that would find another entity now: nothing moves
            var changed = new List<string>();
            foreach (var (r, (line, type, before)) in found)
            {
                var after = Find(type, r.WrittenName!);
                if (!ReferenceEquals(before, after) && changed.Count < 12)
                    changed.Add($"line {line}: \"{r.WrittenName}\" would find {Describe(after)} instead of {Describe(before)}");
            }
            foreach (var (block, before) in byName)
            {
                var after = Find(block.Entity.GetEntityType(), block.Header);
                if (!ReferenceEquals(before, after) && changed.Count < 12)
                    changed.Add($"line {block.HeaderLine}: #select \"{block.Header}\" would find {Describe(after)} instead of {Describe(before)}");
            }
            if (changed.Count > 0)
            {
                Backward();
                throw new EditException("Nothing was renumbered: the game takes the lowest number for a name several share, and after the move these names would find another entity: "
                                        + string.Join("; ", changed) + ". Write those lines with the number, or pick other start numbers");
            }

            // the author's text, with only the numbers that moved replaced
            foreach (var (p, before) in exports)
            {
                var after = p.ToExportString();
                if (p.Line != null || after == before || before != p.BaselineExport || Substitute(p.RawText!, before, after) is not string raw)
                    continue;
                _texts.Add((p, p.RawText!, p.BaselineExport!, raw, after));
                p.RawText = raw;
                p.BaselineExport = after;
            }
            // a line with several commands ("#weapon 1501 #armor 600"): its text the same way, when
            // every command on it is as read (else its commands are written a line each, as edited)
            foreach (var group in exports.Keys.Where(p => p.Line != null).Select(p => p.Line!).Distinct())
            {
                var members = group.Members;
                if (members.Any(m => m.RawText == null || m.BaselineExport == null))
                    continue;
                var befores = members.Select(m => exports.TryGetValue(m, out var b) ? b : m.ToExportString()).ToList();
                var afters = members.Select(m => m.ToExportString()).ToList();
                if (befores.SequenceEqual(afters) || members.Where((m, k) => befores[k] != m.BaselineExport).Any())
                    continue;
                var text = Substitute(group.Text, string.Join(" ", befores), string.Join(" ", afters));
                var raws = members.Select((m, k) => befores[k] == afters[k] ? m.RawText : Substitute(m.RawText!, befores[k], afters[k])).ToList();
                if (text == null || raws.Any(r => r == null))
                    continue;
                _lines.Add((group, group.Text, text));
                group.Text = text;
                for (int k = 0; k < members.Count; k++)
                    if (befores[k] != afters[k])
                    {
                        _texts.Add((members[k], members[k].RawText!, members[k].BaselineExport!, raws[k]!, afters[k]));
                        members[k].RawText = raws[k];
                        members[k].BaselineExport = afters[k];
                    }
            }
            foreach (var block in _mod.SourceBlocks)
                if (block.RawHeader != null && moveOf.TryGetValue(block.Entity, out var m) && block.IdAtParse == m.Old
                    && LeadingNumber.IsMatch(block.Header) && SubstituteFirst(block.RawHeader, m.Old, m.New) is string raw)
                {
                    _headers.Add((block, block.RawHeader, block.IdAtParse, raw, m.New));
                    block.RawHeader = raw;
                    block.IdAtParse = m.New;
                }
            ModResolver.For(_mod).Invalidate();
        }

        public void Undo()
        {
            Backward();
            _changed(this);
        }

        public void Redo()
        {
            foreach (var (r, _, after) in _retargets)
                r.Retarget(after);
            Renumbering.MoveAll(_mod, _moves.Select(m => (m.Entity, m.New)).ToList());
            foreach (var (r, _, after) in _names)
                r.RestoreName(after);
            foreach (var (p, _, after) in _forms)
                p.Value = after;
            Rename();
            foreach (var (p, _, _, raw, baseline) in _texts)
            {
                p.RawText = raw;
                p.BaselineExport = baseline;
            }
            foreach (var (b, _, _, raw, id) in _headers)
            {
                b.RawHeader = raw;
                b.IdAtParse = id;
            }
            foreach (var (line, _, after) in _lines)
                line.Text = after;
            ModResolver.For(_mod).Invalidate();
            _changed(this);
        }

        private void Backward()
        {
            foreach (var (p, raw, baseline, _, _) in _texts)
            {
                p.RawText = raw;
                p.BaselineExport = baseline;
            }
            foreach (var (b, raw, id, _, _) in _headers)
            {
                b.RawHeader = raw;
                b.IdAtParse = id;
            }
            foreach (var (line, text, _) in _lines)
                line.Text = text;
            foreach (var (p, before, _) in _forms)
                p.Value = before;
            foreach (var (r, before, _) in _names)
                r.RestoreName(before);
            Renumbering.MoveAll(_mod, _moves.Select(m => (m.Entity, m.Old)).ToList());
            foreach (var (r, before, _) in _retargets)
                r.Retarget(before);
            foreach (var (type, name, holder) in _holders)
                _mod.Database[type].SetNamed(name, holder);
            ModResolver.For(_mod).Invalidate();
        }

        // each name to the lowest number that has it (the game's rule), as at loading
        private void Rename()
        {
            foreach (var g in _named.GroupBy(n => n.Type))
                _mod.Database[g.Key].Rename(g.Select(n => (n.Name, n.Entity)));
        }

        private IDEntity? Find(EntityType type, string name) => _mod.TryGet(type, 0, name, out var e) ? e : null;

        private static string Describe(IDEntity? e) =>
            e == null ? "nothing" : $"{ModRenumber.Kind(e.GetEntityType())} {e.ID} {e.Name}" + (ReferenceEquals(e.ParentMod, VanillaLoader.Vanilla) ? " (the game's)" : "");

        private static readonly Regex FormNumber = new(@"\((\d+)\)\s*$");

        // a header by number ("#newmonster 7665 MAIN" is 7665), not by name
        private static readonly Regex LeadingNumber = new(@"^[+-]?\d+");

        /// <summary>
        /// The line as written with only the numbers that changed replaced: its export before and
        /// after differ in numbers only, and the line has the export's numbers, in order; else null
        /// (the line is written anew).
        /// </summary>
        internal static string? Substitute(string raw, string before, string after)
        {
            var b = ModRenumber.Number.Matches(before);
            var a = ModRenumber.Number.Matches(after);
            var r = ModRenumber.Number.Matches(raw);
            if (b.Count != a.Count || r.Count != b.Count
                || ModRenumber.Number.Replace(before, "0") != ModRenumber.Number.Replace(after, "0"))
                return null;
            var sb = new StringBuilder();
            int at = 0;
            for (int i = 0; i < r.Count; i++)
            {
                if (r[i].Value != b[i].Value)
                    return null;
                sb.Append(raw, at, r[i].Index - at).Append(a[i].Value);
                at = r[i].Index + r[i].Length;
            }
            return sb.Append(raw, at, raw.Length - at).ToString();
        }

        /// <summary>A header with its number replaced (the first number on it, if it's <paramref name="old"/>), else null.</summary>
        internal static string? SubstituteFirst(string raw, int old, int now)
        {
            var m = ModRenumber.Number.Match(raw);
            if (!m.Success || m.Value != old.ToString())
                return null;
            return raw.Substring(0, m.Index) + now + raw.Substring(m.Index + m.Length);
        }
    }
}
