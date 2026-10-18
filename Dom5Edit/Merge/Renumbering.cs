using Dom5Edit.Entities;
using Dom5Edit.Props;
using Dom5Edit.Resolve;

namespace Dom5Edit.Merge
{
    /// <summary>
    /// Moves an entity, or a code, variable, enchantment or monster tag number, to another number
    /// (docs/MERGING.md). Connections are pointers: a reference holds its target and writes the
    /// target's number, a code is one shared object written by its number, a block's header is
    /// rewritten when its entity's number changed; so a move only re-keys the tables, and the
    /// numbers references keep for looking their targets up again follow.
    /// </summary>
    public static class Renumbering
    {
        /// <summary>
        /// Moves an entity of its mod to a free number. <paramref name="mods"/>: the mods whose
        /// references may point at it (its own, and mods read over it: a submod's #select of it
        /// follows it).
        /// </summary>
        public static void Move(IDEntity entity, int newId, IEnumerable<Mod> mods)
        {
            var type = entity.GetEntityType();
            int old = entity.ID;
            if (old == newId)
                return;
            if (entity.ParentMod.Database[type].Has(newId))
                throw new InvalidOperationException($"{type} {newId} is taken in {entity.ParentMod.DisplayName}");
            entity.ParentMod.Database[type].Rekey(entity, old, newId);
            entity.ID = newId;
            foreach (var mod in mods.Distinct())
            {
                // a mod over it that selects it: its entity for it moves too (its number is this one's)
                if (!ReferenceEquals(mod, entity.ParentMod) && mod.Database[type].TryGetValue(old, out var over) && ReferenceEquals(over.DependentEntity, entity))
                    mod.Database[type].Rekey(over, old, newId);
                foreach (var p in Lines(mod))
                    if (p is Reference r)
                        foreach (var part in r.Parts())
                            part.FollowTarget(entity);
                ModResolver.For(mod).Invalidate();
            }
        }

        /// <summary>
        /// Moves many of a mod's entities at once (the Renumber tool): through numbers nothing uses
        /// first, so none lands on one yet to move; then one pass over the mod's lines, each
        /// reference to a moved entity taking its new number.
        /// </summary>
        internal static void MoveAll(Mod mod, IReadOnlyList<(IDEntity Entity, int To)> moves)
        {
            int temp = int.MinValue / 2;
            foreach (var (e, _) in moves)
            {
                mod.Database[e.GetEntityType()].Rekey(e, e.ID, temp);
                e.ID = temp++;
            }
            foreach (var (e, to) in moves)
            {
                mod.Database[e.GetEntityType()].Rekey(e, e.ID, to);
                e.ID = to;
            }
            var moved = new HashSet<IDEntity>(moves.Select(m => m.Entity), ReferenceEqualityComparer.Instance);
            foreach (var p in Lines(mod))
                if (p is Reference r)
                    foreach (var part in r.Parts())
                        if (part.TryGetEntity(out var target) && moved.Contains(target))
                            part.FollowTarget(target);
            ModResolver.For(mod).Invalidate();
        }

        /// <summary>
        /// Moves a code, variable, enchantment, monster tag or the like (a DependentEntity of the
        /// mod) to another number: everything that names it is written with the new one; a submod's
        /// own entry for it (linked to this one) follows.
        /// </summary>
        public static void Move(Mod mod, EntityType type, DependentEntity dependent, int newId)
        {
            var set = mod.Dependents[type];
            int old = dependent.ID;
            if (old == newId)
                return;
            if (set.ContainsKey(newId))
                throw new InvalidOperationException($"{type} {newId} is taken in {mod.DisplayName}");
            set.Remove(old);
            dependent.ID = newId;
            set[newId] = dependent;
            ModResolver.For(mod).Invalidate();
        }

        /// <summary>
        /// Every line the mod's save writes: its blocks' lines as read, live or not (a line a later
        /// #copy... or #clear... in the file took out is still read by the game, and written),
        /// and lines added since.
        /// </summary>
        internal static IEnumerable<Property> Lines(Mod mod) =>
            mod.SourceBlocks.SelectMany(b => b.Properties).Concat(Entities(mod).SelectMany(e => e.Properties)).Distinct();

        /// <summary>Every entity the mod holds (numbered, unnumbered, events).</summary>
        internal static IEnumerable<IDEntity> Entities(Mod mod) =>
            mod.Database.Values.SelectMany(s => s.GetFullList()).Concat(mod.Events).Distinct();
    }
}
