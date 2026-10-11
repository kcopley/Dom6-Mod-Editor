using Dom5Edit.Commands;
using Dom5Edit.Entities;
using Dom5Edit.Props;
using Dom5Edit.Resolve;

namespace Dom5Edit.Events
{
    /// <summary>How one event leads to another, or a spell to an event (docs/EVENT_EDITOR.md, "How events chain").</summary>
    public enum EventLinkKind
    {
        /// <summary>L1: the earlier event sets a province code (#code), the later one requires it (#req_code ...).</summary>
        Code,
        /// <summary>L1: the later event can't happen while the code is set (#req_notcode, #req_notanycode).</summary>
        CodeExcludes,
        /// <summary>L2: #delay N: the next event in the file happens N turns later.</summary>
        Delay,
        /// <summary>L2: #delayskip p: p% chance the event after the next is used instead.</summary>
        DelaySkip,
        /// <summary>L3: the earlier event changes an event variable (#incvar), the later one checks it (#req_varpos).</summary>
        Variable,
        /// <summary>L6: the earlier event offers province orders (#order), the later one answers one (#req_targorder 100-108).</summary>
        Choice,
        /// <summary>L4: a spell's enchantment (its #damage), required by the event (#req_ench ...).</summary>
        Enchantment,
        /// <summary>L5: a spell that causes the event (effect 42, its #damage is the event's #id).</summary>
        SpellEvent,
    }

    /// <summary>One link: from an event (or a spell) to an event, by what, through which lines.</summary>
    public sealed class EventLink
    {
        public EventLink(EventLinkKind kind, IDEntity from, IDEntity to, long key, Property? setter, Property? checker)
        {
            Kind = kind;
            From = from;
            To = to;
            Key = key;
            Setter = setter;
            Checker = checker;
        }

        public EventLinkKind Kind { get; }
        public IDEntity From { get; }
        public IDEntity To { get; }
        /// <summary>The code, variable, enchantment, event id, order, or the delay in turns.</summary>
        public long Key { get; }
        /// <summary>The line in From that makes the link (#code -509, #delay 2, #incvar 12, #order 12).</summary>
        public Property? Setter { get; }
        /// <summary>The line in To that checks it (#req_code -509, #req_varpos 12, #req_targorder 102).</summary>
        public Property? Checker { get; }

        /// <summary>The link in a few words, as the chain view labels it.</summary>
        public string Label => Kind switch
        {
            EventLinkKind.Code => $"code {Key}",
            EventLinkKind.CodeExcludes => $"not while code {Key}",
            EventLinkKind.Delay => Key == 1 ? "1 turn later" : $"{Key} turns later",
            EventLinkKind.DelaySkip => $"{Key}%: skips to this",
            EventLinkKind.Variable => $"variable {Key}",
            EventLinkKind.Choice => EventInfo.OrderName(Key),
            EventLinkKind.Enchantment => $"enchantment {Key}",
            EventLinkKind.SpellEvent => $"event id {Key}",
            _ => Kind.ToString(),
        };

        /// <summary>Links that chain events (not spells starting them).</summary>
        public bool IsEventToEvent => Kind != EventLinkKind.Enchantment && Kind != EventLinkKind.SpellEvent;
    }

    /// <summary>Something about an event that won't work as meant, or likely isn't.</summary>
    public sealed class EventProblem
    {
        public EventProblem(IDEntity e, Property? line, string message, bool isError = false)
        {
            Event = e;
            Line = line;
            Message = message;
            IsError = isError;
        }

        public IDEntity Event { get; }
        public Property? Line { get; }
        public string Message { get; }
        public bool IsError { get; }
        public override string ToString() => (IsError ? "Error: " : "Warning: ") + Message;
    }

    /// <summary>Events joined by codes, delays, variables or choices, in file order; and the spells that start them.</summary>
    public sealed class EventChain
    {
        internal EventChain(int number, List<IDEntity> events, List<EventLink> links, List<IDEntity> triggers)
        {
            Number = number;
            Events = events;
            Links = links;
            Triggers = triggers;
        }

        public int Number { get; }
        public IReadOnlyList<IDEntity> Events { get; }
        public IReadOnlyList<EventLink> Links { get; }
        /// <summary>Spells whose enchantment or event id starts events of the chain.</summary>
        public IReadOnlyList<IDEntity> Triggers { get; }
    }

    /// <summary>
    /// A mod's events as the game links them (docs/EVENT_EDITOR.md): every event's lines in file
    /// order, the links between events and from spells to events, the chains they form, and
    /// problems. Built from the save plan, so it matches what a save writes.
    /// </summary>
    public sealed class EventGraph
    {
        private readonly List<IDEntity> _events = new List<IDEntity>();
        private readonly List<IDEntity> _modEvents = new List<IDEntity>();
        private readonly List<IDEntity> _gameEvents = new List<IDEntity>();
        private readonly Dictionary<int, IDEntity> _byNumber = new Dictionary<int, IDEntity>();
        /// <summary>The lines the mod writes (a changed game event's other lines are the game's).</summary>
        private readonly HashSet<Property> _ownLines = new HashSet<Property>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<IDEntity, int> _modIndex = new(ReferenceEqualityComparer.Instance);
        private HashSet<IDEntity> _own = new HashSet<IDEntity>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<IDEntity, List<Property>> _lines = new(ReferenceEqualityComparer.Instance);
        private readonly List<EventLink> _links = new List<EventLink>();
        private readonly List<EventProblem> _problems = new List<EventProblem>();
        private readonly List<EventChain> _chains = new List<EventChain>();
        private readonly Dictionary<IDEntity, EventChain> _chainOf = new(ReferenceEqualityComparer.Instance);
        /// <summary>The spells that make enchantments or cause events (reusable while no spell changes).</summary>
        public SpellIndex Spells { get; private set; } = null!;
        private Dictionary<long, List<IDEntity>> _enchantmentSpells => Spells.Enchantments;
        private Dictionary<long, List<IDEntity>> _eventSpells => Spells.EventCauses;

        /// <summary>Every event: the mod's in file order, then the game's own that the mod doesn't change.</summary>
        public IReadOnlyList<IDEntity> Events => _events;

        /// <summary>The mod's events, in the order the game reads them.</summary>
        public IReadOnlyList<IDEntity> ModEvents => _modEvents;

        /// <summary>Whether an event is the mod's (a #newevent or a #selectevent), not one of the game's it leaves alone.</summary>
        public bool IsModEvent(IDEntity e) => _own.Contains(e);
        public IReadOnlyList<EventLink> Links => _links;
        public IReadOnlyList<EventProblem> Problems => _problems;
        public IReadOnlyList<EventChain> Chains => _chains;

        /// <summary>An event's lines as saved, in order.</summary>
        public IReadOnlyList<Property> LinesOf(IDEntity e) => _lines.TryGetValue(e, out var l) ? l : (IReadOnlyList<Property>)Array.Empty<Property>();

        public IEnumerable<EventLink> From(IDEntity e) => LinksBy(ref _from, l => l.From, e);
        public IEnumerable<EventLink> To(IDEntity e) => LinksBy(ref _to, l => l.To, e);
        private ILookup<IDEntity, EventLink>? _from, _to;
        private IEnumerable<EventLink> LinksBy(ref ILookup<IDEntity, EventLink>? index, Func<EventLink, IDEntity> key, IDEntity e) =>
            (index ??= _links.ToLookup<EventLink, IDEntity>(key, ReferenceEqualityComparer.Instance))[e];
        public IEnumerable<EventProblem> ProblemsOf(IDEntity e) => _problems.Where(p => ReferenceEquals(p.Event, e));
        public EventChain? ChainOf(IDEntity e) => _chainOf.TryGetValue(e, out var c) ? c : null;

        /// <summary>The spells that make this enchantment (their #damage), mod's and vanilla's.</summary>
        public IReadOnlyList<IDEntity> SpellsOfEnchantment(long ench) => _enchantmentSpells.TryGetValue(ench, out var l) ? l : (IReadOnlyList<IDEntity>)Array.Empty<IDEntity>();

        /// <summary>The position of one of the mod's events in its file (0 first), or -1.</summary>
        public int IndexOf(IDEntity e) => _modIndex.TryGetValue(e, out var i) ? i : -1;

        /// <summary>
        /// The event #delay plans after this one: the next record number (tools/dom6exe README,
        /// Events). After a #newevent that's the mod's next #newevent (a #selectevent block in
        /// between doesn't count); after #selectevent N (or the game's event N), event N + 1.
        /// <paramref name="steps"/> 2 is the one after it (#delayskip).
        /// </summary>
        public IDEntity? NextRecord(IDEntity e, int steps = 1)
        {
            if (e.Selected || !_own.Contains(e))
                return _byNumber.TryGetValue(e.ID + steps, out var next) ? next : null;
            for (int j = IndexOf(e) + 1; j < _modEvents.Count; j++)
                if (!_modEvents[j].Selected && --steps == 0)
                    return _modEvents[j];
            return null;
        }

        /// <summary>Every province code the mod's events set or check.</summary>
        public IEnumerable<long> CodesUsed => _events.SelectMany(LinesOf)
            .Where(p => EventInfo.CodeSetters.Contains(p.Command) || EventInfo.CodeCheckers.Contains(p.Command)
                        || EventInfo.CodeExcluders.Contains(p.Command) || EventInfo.CodeResetters.Contains(p.Command))
            .Select(EventInfo.Number).Where(n => n.HasValue).Select(n => n!.Value).Distinct();

        /// <summary>A code no event uses yet, from the manual's range for mods (-300 down to -5000).</summary>
        public long NextFreeCode()
        {
            var used = new HashSet<long>(CodesUsed);
            for (long c = -300; c >= -5000; c--)
                if (!used.Contains(c))
                    return c;
            return -5001;
        }

        /// <summary>
        /// Builds the graph for a mod's events. Spells (the mod's, resolved; vanilla's, as read) are
        /// looked at for enchantments and event-causing effects; pass <paramref name="spells"/> to
        /// reuse that from an earlier build when no spell changed. <paramref name="below"/>: the
        /// mods read before this one (Mod.Below: a mod it needs, vanilla), whose events and spells
        /// it links to; none for the mod's alone.
        /// </summary>
        public static EventGraph Build(Mod mod, Func<IDEntity, ResolvedEntity> resolve, IReadOnlyList<Mod>? below = null, SpellIndex? spells = null)
        {
            var g = new EventGraph();
            below ??= Array.Empty<Mod>();
            g.Spells = spells ?? SpellIndex.Build(mod, resolve, below);
            var plan = new SavePlan(mod);
            // the mod's events in the order the game reads them (its #newevents get records 3500,
            // 3501, ... in this order)
            foreach (var block in plan.Blocks())
            {
                if (block.Entity.GetEntityType() != EntityType.EVENT)
                    continue;
                if (!g._lines.TryGetValue(block.Entity, out var lines))
                {
                    g._lines[block.Entity] = lines = new List<Property>();
                    g._modEvents.Add(block.Entity);
                }
                lines.AddRange(block.Lines);
                g._ownLines.UnionWith(block.Lines);
            }
            // a #selectevent of a game event: what it is in game (the game's lines and the mod's)
            List<Property> Resolved(IDEntity e)
            {
                var r = resolve(e);
                var lines = r.Values.Select(v => v.Property).ToList();
                if (!lines.Any(p => p.Command == Command.MSG) && r.Assets.TryGetValue(Command.MSG, out var msg))
                    lines.Add(msg);
                return lines;
            }
            foreach (var e in g._modEvents.Where(e => e.Selected))
            {
                g._lines[e] = Resolved(e);
                if (e.ID >= 0)
                    g._byNumber[e.ID] = e;
            }
            // the events of the mods read before this one, which it doesn't change: a mod it needs
            // (its new events and its changes to the game's, as resolved), then the game's own
            // (their messages are display assets)
            foreach (var layer in below)
                if (layer.Database.TryGetValue(EntityType.EVENT, out var gameEvents))
                {
                    bool vanilla = layer.Dependencies.Count == 0;
                    foreach (var v in gameEvents.GetFullList().OrderBy(v => v.ID))
                    {
                        if (v.ID < 0 && vanilla || v.ID >= 0 && g._byNumber.ContainsKey(v.ID))
                            continue;
                        if (v.ID >= 0)
                            g._byNumber[v.ID] = v;
                        g._lines[v] = vanilla ? v.Properties.ToList() : Resolved(v);
                        g._gameEvents.Add(v);
                    }
                }
            g._events.AddRange(g._modEvents);
            g._events.AddRange(g._gameEvents);
            for (int i = 0; i < g._modEvents.Count; i++)
                g._modIndex[g._modEvents[i]] = i;
            g._own = new HashSet<IDEntity>(g._modEvents, ReferenceEqualityComparer.Instance);
            g.Link();
            g.Group();
            g.Check();
            return g;
        }

        private void Link()
        {
            // what each event sets, by key
            var codeSetters = new Dictionary<long, List<(IDEntity, Property)>>();
            var varSetters = new Dictionary<long, List<(IDEntity, Property)>>();
            var orders = new List<(IDEntity Event, Property Line, long Mask)>();
            foreach (var e in _events)
                foreach (var p in LinesOf(e))
                {
                    if (EventInfo.Number(p) is not long n)
                        continue;
                    if (EventInfo.CodeSetters.Contains(p.Command) && n != 0)
                        AddTo(codeSetters, n, (e, p));
                    else if (EventInfo.VariableSetters.Contains(p.Command))
                        AddTo(varSetters, n, (e, p));
                    else if (p.Command == Command.ORDER && n > 0)
                        orders.Add((e, p, n));
                }

            for (int i = 0; i < _events.Count; i++)
            {
                var e = _events[i];
                var lines = LinesOf(e);
                foreach (var p in lines)
                {
                    if (EventInfo.Number(p) is not long n)
                        continue;
                    if (EventInfo.CodeCheckers.Contains(p.Command) && n != 0 && codeSetters.TryGetValue(n, out var setters))
                        foreach (var (from, line) in setters.Where(s => !ReferenceEquals(s.Item1, e)))
                            _links.Add(new EventLink(EventLinkKind.Code, from, e, n, line, p));
                    else if (EventInfo.CodeExcluders.Contains(p.Command) && n != 0 && codeSetters.TryGetValue(n, out var blockers))
                        foreach (var (from, line) in blockers.Where(s => !ReferenceEquals(s.Item1, e)))
                            _links.Add(new EventLink(EventLinkKind.CodeExcludes, from, e, n, line, p));
                    else if (EventInfo.VariableCheckers.Contains(p.Command) && varSetters.TryGetValue(n, out var vs))
                        foreach (var (from, line) in vs.Where(s => !ReferenceEquals(s.Item1, e)))
                            _links.Add(new EventLink(EventLinkKind.Variable, from, e, n, line, p));
                    else if (EventInfo.EnchantmentCheckers.Contains(p.Command) && _enchantmentSpells.TryGetValue(n, out var spells))
                        foreach (var s in spells)
                            _links.Add(new EventLink(EventLinkKind.Enchantment, s, e, n, null, p));
                    else if (p.Command == Command.ID && _eventSpells.TryGetValue(n, out var causes))
                        foreach (var s in causes)
                            _links.Add(new EventLink(EventLinkKind.SpellEvent, s, e, n, null, p));
                    else if (p.Command == Command.REQ_TARGORDER && n >= 100 && n <= 108)
                    {
                        // the event offering the order: one that sets a code this one requires
                        var codes = lines.Where(q => EventInfo.CodeCheckers.Contains(q.Command)).Select(EventInfo.Number).ToHashSet();
                        foreach (var (from, line, mask) in orders)
                        {
                            if (ReferenceEquals(from, e) || (mask & EventInfo.OrderMaskOf(n)) == 0)
                                continue;
                            if (LinesOf(from).Any(q => EventInfo.CodeSetters.Contains(q.Command) && codes.Contains(EventInfo.Number(q))))
                                _links.Add(new EventLink(EventLinkKind.Choice, from, e, n, line, p));
                        }
                    }
                }

                // #delay: the next record; #delayskip: the one after it
                var delay = lines.LastOrDefault(p => EventInfo.Delays.Contains(p.Command));
                if (delay != null && NextRecord(e) is IDEntity next)
                    _links.Add(new EventLink(EventLinkKind.Delay, e, next, EventInfo.Number(delay) ?? 0, delay, null));
                var skip = lines.LastOrDefault(p => p.Command == Command.DELAYSKIP);
                if (skip != null && NextRecord(e, 2) is IDEntity after)
                    _links.Add(new EventLink(EventLinkKind.DelaySkip, e, after, EventInfo.Number(skip) ?? 0, skip, null));
            }
        }

        private void Group()
        {
            // connected events (codes, delays, variables, choices), each group in file order
            var parent = new Dictionary<IDEntity, IDEntity>(ReferenceEqualityComparer.Instance);
            IDEntity Find(IDEntity x)
            {
                while (parent.TryGetValue(x, out var p) && !ReferenceEquals(p, x))
                    x = p;
                return x;
            }
            foreach (var e in _events)
                parent[e] = e;
            foreach (var l in _links.Where(l => l.IsEventToEvent))
                parent[Find(l.From)] = Find(l.To);
            var groups = _events.GroupBy(Find, ReferenceEqualityComparer.Instance).Where(g => g.Count() > 1).ToList();
            int number = 1;
            foreach (var group in groups)
            {
                var members = group.ToList();
                var set = new HashSet<IDEntity>(members, ReferenceEqualityComparer.Instance);
                var links = _links.Where(l => set.Contains(l.To) && (set.Contains(l.From) || !l.IsEventToEvent)).ToList();
                var triggers = links.Where(l => !l.IsEventToEvent).Select(l => l.From).Distinct(ReferenceEqualityComparer.Instance).Cast<IDEntity>().ToList();
                var chain = new EventChain(number++, members, links.Where(l => l.IsEventToEvent).ToList(), triggers);
                _chains.Add(chain);
                foreach (var e in members)
                    _chainOf[e] = chain;
            }
        }

        private void Check()
        {
            var codesSet = new HashSet<long>(_events.SelectMany(LinesOf).Where(p => EventInfo.CodeSetters.Contains(p.Command)).Select(EventInfo.Number).Where(n => n.HasValue).Select(n => n!.Value));
            var codesChecked = new HashSet<long>(_events.SelectMany(LinesOf)
                .Where(p => EventInfo.CodeCheckers.Contains(p.Command) || EventInfo.CodeExcluders.Contains(p.Command) || EventInfo.CodeResetters.Contains(p.Command))
                .Select(EventInfo.Number).Where(n => n.HasValue).Select(n => n!.Value));
            // the mod's events only, and of a changed game event the mod's lines: the game's own are as they are
            foreach (var e in _modEvents)
            {
                var lines = LinesOf(e);
                var rarity = EventInfo.Rarity(lines);
                foreach (var p in lines.Where(_ownLines.Contains))
                {
                    long? n = EventInfo.Number(p);
                    if (EventInfo.CodeSetters.Contains(p.Command) && n is long set && set != 0)
                    {
                        if (!EventInfo.IsModCode(set))
                            _problems.Add(new EventProblem(e, p, $"Code {set} is outside -300 to -5000: the manual keeps the rest for the game's own events and asks mods to use that range"));
                        else if (!codesChecked.Contains(set))
                            _problems.Add(new EventProblem(e, p, $"Sets code {set}, but no event checks it (#req_code {set})"));
                    }
                    if (EventInfo.CodeCheckers.Contains(p.Command) && n is long need && need != 0 && EventInfo.IsModCode(need) && !codesSet.Contains(need))
                        _problems.Add(new EventProblem(e, p, $"Requires code {need}, but no event sets it (#code {need}): this event can't happen", isError: true));
                    if (p.Command == Command.WORLDRITREBATE && rarity != 11 && rarity != 12)
                        _problems.Add(new EventProblem(e, p, "#worldritrebate only works in common or uncommon global events (rarity 11 or 12)"));
                    if (p.Command == Command.REQ_PREGAME && rarity is long r && !EventInfo.IsAlways(r))
                        _problems.Add(new EventProblem(e, p, "#req_pregame needs an always rarity (0, 5, 10 or 13): no other events happen before the first turn"));
                }
                if (lines.Any(p => EventInfo.CodeSetters.Contains(p.Command) && EventInfo.Number(p) is long c && c != 0 && _ownLines.Contains(p))
                    && !lines.Any(p => EventInfo.CodeCheckers.Contains(p.Command)))
                    _problems.Add(new EventProblem(e, null, "Sets a code without requiring one (#req_code 0): it can break another chain going on in the province (manual)"));
                if (lines.Any(p => EventInfo.Delays.Contains(p.Command) && _ownLines.Contains(p)) && NextRecord(e) == null)
                    _problems.Add(new EventProblem(e, lines.First(p => EventInfo.Delays.Contains(p.Command)), e.Selected
                        ? $"#delay plans event {e.ID + 1}, and there's no such event"
                        : "#delay plans the next #newevent, and this mod has none after this one (with other mods loaded, the game runs the next mod's first new event)", isError: e.Selected));
                var (reqs, effs) = EventInfo.Slots(lines);
                Overflow(e, reqs, EventInfo.MaxRequirements, "requirements");
                Overflow(e, effs, EventInfo.MaxEffects, "effects");
                var msg = EventInfo.Message(lines);
                if (msg != null && msg.Length > 2399 && lines.Any(p => p.Command == Command.MSG && _ownLines.Contains(p)))
                    _problems.Add(new EventProblem(e, null, $"The message is {msg.Length} characters: the game takes at most 2399", isError: true));
                // the target tags name the commander a group of #req_targ* picks (DomEnhanced: 430 events use them, all with one)
                if (msg != null && lines.Any(p => p.Command == Command.MSG && _ownLines.Contains(p))
                    && System.Text.RegularExpressions.Regex.IsMatch(msg, "##(full)?targname##|##targhis##", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                    && !lines.Any(p => CommandsMap.TryGetString(p.Command, out var s) && s.StartsWith("#req_targ", StringComparison.OrdinalIgnoreCase)))
                    _problems.Add(new EventProblem(e, null, "The message names the target commander (##targname##, ##targhis##), but no requirement picks one (#req_targ...): there's no commander to name"));
                var needsName = lines.Where(_ownLines.Contains).FirstOrDefault(EventInfo.NeedsBracketName);
                if (needsName != null && EventInfo.BracketName(msg) == null)
                    _problems.Add(new EventProblem(e, needsName, $"{EventInfo.Name(needsName.Command)} uses the site or item named in brackets at the end of the message ([Name]), and the message has none", isError: true));
                if (rarity == null && e.Selected == false)
                    _problems.Add(new EventProblem(e, null, "No #rarity: the game counts the event as a free slot, so the next #newevent takes it again and this one is lost", isError: true));
            }
        }

        /// <summary>
        /// More lines than the list has slots: which the game drops (the problem's line is the
        /// first), and the lines without an argument that count too (#notext, #nolog: easy to miss;
        /// Confluence's 20 #killcom with both kill 18).
        /// </summary>
        private void Overflow(IDEntity e, List<Property> taken, int max, string what)
        {
            if (taken.Count <= max)
                return;
            var flags = taken.Where(p => GameData.GameCommandCatalog.EffectOf(EntityType.EVENT, p.Command)?.NoArgument == true)
                .Select(p => EventInfo.Name(p.Command)).Distinct().ToList();
            var dropped = taken.Skip(max).ToList();
            // the dropped commands, a run of one as "#killcom ×2" (no values: the report groups alike messages)
            var runs = new List<(string Name, int Count)>();
            foreach (var name in dropped.Select(p => EventInfo.Name(p.Command)))
                if (runs.Count > 0 && runs[^1].Name == name)
                    runs[^1] = (name, runs[^1].Count + 1);
                else
                    runs.Add((name, 1));
            var included = flags.Count == 0 ? ""
                : ", " + (flags.Count == 1 ? flags[0] : string.Join(", ", flags.Take(flags.Count - 1)) + " and " + flags[^1]) + " included";
            _problems.Add(new EventProblem(e, dropped[0],
                $"{taken.Count} {what}{included}: the game keeps the first {max} in file order and drops the last {(dropped.Count == 1 ? "one" : dropped.Count)} " +
                $"({string.Join(", ", runs.Select(r => r.Count > 1 ? $"{r.Name} ×{r.Count}" : r.Name))})", isError: true));
        }

        private static void AddTo<T>(Dictionary<long, List<T>> map, long key, T value)
        {
            if (!map.TryGetValue(key, out var list))
                map[key] = list = new List<T>();
            list.Add(value);
        }
    }

    /// <summary>The spells, mod's and vanilla's, that make an enchantment or cause an event, by that number (their #damage).</summary>
    public sealed class SpellIndex
    {
        internal Dictionary<long, List<IDEntity>> Enchantments { get; } = new Dictionary<long, List<IDEntity>>();
        internal Dictionary<long, List<IDEntity>> EventCauses { get; } = new Dictionary<long, List<IDEntity>>();

        public static SpellIndex Build(Mod mod, Func<IDEntity, ResolvedEntity> resolve, IReadOnlyList<Mod> below)
        {
            var index = new SpellIndex();
            void Note(IDEntity spell, long? effect, long? damage)
            {
                if (effect is not long e || damage is not long d)
                    return;
                var map = EventInfo.IsEnchantmentEffect(e) ? index.Enchantments : EventInfo.IsCauseEventEffect(e) ? index.EventCauses : null;
                if (map == null)
                    return;
                if (!map.TryGetValue(d, out var list))
                    map[d] = list = new List<IDEntity>();
                list.Add(spell);
            }
            var own = new HashSet<int>();
            if (mod.Database.TryGetValue(EntityType.SPELL, out var spells))
                foreach (var s in spells.GetFullList())
                {
                    var r = resolve(s);
                    Note(s, EventInfo.Number(r.Get(Command.EFFECT)?.Property), EventInfo.Number(r.Get(Command.DAMAGE)?.Property));
                    if (s.ID > 0)
                        own.Add(s.ID);
                }
            // the spells of the mods read before this one (nearest first): a mod it needs, resolved;
            // the game's, as read
            foreach (var layer in below)
                if (layer.Database.TryGetValue(EntityType.SPELL, out var vspells))
                    foreach (var s in vspells.GetFullList())
                    {
                        if (s.ID > 0 && !own.Add(s.ID))
                            continue;
                        if (layer.Dependencies.Count > 0)
                        {
                            var r = resolve(s);
                            Note(s, EventInfo.Number(r.Get(Command.EFFECT)?.Property), EventInfo.Number(r.Get(Command.DAMAGE)?.Property));
                            continue;
                        }
                        var props = s.Properties;
                        Note(s, EventInfo.Number(props.LastOrDefault(p => p.Command == Command.EFFECT)),
                            EventInfo.Number(props.LastOrDefault(p => p.Command == Command.DAMAGE)));
                    }
            return index;
        }
    }
}
