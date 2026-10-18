using Dom5Edit;
using Dom5Edit.Commands;
using Dom5Edit.Editing;
using Dom5Edit.Entities;
using Dom5Edit.Props;

namespace Dom5Editor.Session
{
    /// <summary>
    /// Which entities refer to which ("used by"): every reference value an entity has in game
    /// (a monster's weapons, a nation's recruits, a spell's summoned unit, ...), indexed by the
    /// entity it points at. Built on first use from vanilla and the mod; after an edit, the
    /// touched entities' entries are worked out again.
    /// </summary>
    public sealed class UsageIndex
    {
        public sealed class Use
        {
            public Use(EntityType type, int id, IDEntity from, Command via)
            {
                Type = type;
                Id = id;
                From = from;
                Via = via;
            }

            public EntityType Type { get; }
            public int Id { get; }
            public IDEntity From { get; }
            public Command Via { get; }
        }

        private readonly EditorSession _session;
        private Dictionary<(EntityType, int), List<Use>>? _byTarget;
        private readonly Dictionary<(EntityType, int), List<(EntityType, int)>> _targetsOf = new();
        // the same for entities without a number (events, mercenary bands), by the entity itself
        private readonly Dictionary<IDEntity, List<(EntityType, int)>> _targetsOfUnnumbered = new(ReferenceEqualityComparer.Instance);

        public UsageIndex(EditorSession session)
        {
            _session = session;
        }

        /// <summary>The entities that refer to this one, with the command they do it with.</summary>
        public IReadOnlyList<Use> UsedBy(EntityType type, int id)
        {
            Build();
            return _byTarget!.TryGetValue((type, id), out var list) ? list : (IReadOnlyList<Use>)Array.Empty<Use>();
        }

        /// <summary>Works the touched entities' entries out again.</summary>
        /// <summary>Forgets every entry (a renumbering moved what they're kept by); built again when next asked.</summary>
        public void Reset() => _byTarget = null;

        public void OnChanged(IModEdit edit)
        {
            if (_byTarget == null)
                return;
            foreach (var e in edit.Entities)
            {
                if (e.ID <= 0)
                {
                    // an event or a band: its own entries, again unless it was deleted
                    Remove(e);
                    if (_session.Mod.Database.TryGetValue(e.Kind, out var set) && set.GetFullList().Contains(e))
                        Add(e);
                    continue;
                }
                Remove((e.Kind, e.ID));
                var current = Current(e.Kind, e.ID);
                if (current != null)
                    Add(current);
            }
        }

        private void Build()
        {
            if (_byTarget != null)
                return;
            _byTarget = new Dictionary<(EntityType, int), List<Use>>();
            var mod = _session.Mod;
            foreach (var (type, set) in mod.Database)
                foreach (var e in set.GetFullList())
                    Add(e);
            // the mods read before it, nearest first (a needed mod, then vanilla): an entity the mod
            // (or a nearer one) has by that number replaces theirs
            var seen = new HashSet<(EntityType, int)>();
            foreach (var (type, set) in mod.Database)
                foreach (var e in set.GetFullList())
                    if (e.ID > 0)
                        seen.Add((type, e.ID));
            foreach (var layer in _session.Below)
                foreach (var (type, set) in layer.Database)
                    foreach (var e in set.GetFullList())
                        if (e.ID <= 0 || seen.Add((type, e.ID)))
                            Add(e);
        }

        /// <summary>The entity for a key as the game has it: the mod's, else the nearest mod's under it (vanilla last).</summary>
        private IDEntity? Current(EntityType type, int id)
        {
            if (_session.Mod.Database.TryGetValue(type, out var set) && set.TryGetValue(id, out var own))
                return own;
            return _session.BaseEntity(type, id);
        }

        private void Add(IDEntity entity)
        {
            EntityType kind;
            try { kind = entity.Kind; }
            catch (NotImplementedException) { return; }
            var from = (kind, entity.ID);
            var targets = new List<(EntityType, int)>();
            var resolved = _session.Resolve(entity);
            foreach (var v in resolved.Values)
            {
                if (v.Property is not Reference r)
                    continue;
                // only what the game reads: a line it skips for this type links to nothing; nor a
                // number the game ignores (an event's path boost aimed by target requirements)
                if (Dom5Edit.GameData.GameCommandCatalog.IsRead(kind, v.Command) == false
                    || !Dom5Edit.Resolve.ReferenceRules.NamesEntity(entity, resolved, v.Command))
                    continue;
                var pointed = new List<IDEntity>();
                if (r.TryGetEntity(out var one) && one != null)
                    pointed.Add(one);
                if (r is IMultiReference multi)
                    pointed.AddRange(multi.Targets()); // (a key into one of the game's lists: each unit in it)
                foreach (var target in pointed.Where(t => t.ID > 0).Distinct())
                {
                    EntityType targetKind;
                    try { targetKind = target.Kind; }
                    catch (NotImplementedException) { continue; }
                    var key = (targetKind, target.ID);
                    if (!_byTarget!.TryGetValue(key, out var list))
                        _byTarget[key] = list = new List<Use>();
                    list.Add(new Use(kind, entity.ID, entity, v.Command));
                    targets.Add(key);
                }
            }
            if (entity.ID > 0)
                _targetsOf[from] = targets;
            else
                _targetsOfUnnumbered[entity] = targets;
        }

        private void Remove(IDEntity unnumbered)
        {
            if (!_targetsOfUnnumbered.TryGetValue(unnumbered, out var targets))
                return;
            foreach (var t in targets.Distinct())
                if (_byTarget!.TryGetValue(t, out var list))
                    list.RemoveAll(u => ReferenceEquals(u.From, unnumbered));
            _targetsOfUnnumbered.Remove(unnumbered);
        }

        private void Remove((EntityType, int) from)
        {
            if (!_targetsOf.TryGetValue(from, out var targets))
                return;
            foreach (var t in targets.Distinct())
                if (_byTarget!.TryGetValue(t, out var list))
                    list.RemoveAll(u => u.Type == from.Item1 && u.Id == from.Item2);
            _targetsOf.Remove(from);
        }
    }
}
