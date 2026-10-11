using Dom5Edit.Entities;
using Dom5Edit.Props;

namespace Dom5Edit.Editing
{
    /// <summary>
    /// One finished edit of a mod, undoable and redoable exactly: it restores every touched
    /// entity's lines (the Property objects themselves, so an undone edit saves as before) and
    /// the entities it created or deleted.
    /// </summary>
    public interface IModEdit
    {
        string Description { get; }

        /// <summary>The entities whose lines the edit changed (including created and deleted ones).</summary>
        IReadOnlyCollection<IDEntity> Entities { get; }

        void Undo();
        void Redo();
    }

    /// <summary>An edit recorded as before/after snapshots of what it touched.</summary>
    internal sealed class SnapshotEdit : IModEdit
    {
        internal sealed class EntityState
        {
            public List<Property> Before = null!;
            public List<Property> After = null!;
            public string NameBefore = null!;
            public string NameAfter = null!;
        }

        private readonly Mod _mod;
        private readonly Dictionary<IDEntity, EntityState> _states;
        private readonly List<IDEntity> _created;
        private readonly List<IDEntity> _deleted;
        private readonly Action<IModEdit> _changed;

        public string Description { get; }
        public IReadOnlyCollection<IDEntity> Entities { get; }

        internal SnapshotEdit(Mod mod, string description, Dictionary<IDEntity, EntityState> states,
                              List<IDEntity> created, List<IDEntity> deleted, Action<IModEdit> changed)
        {
            _mod = mod;
            Description = description;
            _states = states;
            _created = created;
            _deleted = deleted;
            _changed = changed;
            Entities = states.Keys.Concat(created).Concat(deleted).Distinct(ReferenceEqualityComparer.Instance).Cast<IDEntity>().ToList();
        }

        public void Undo()
        {
            foreach (var (entity, s) in _states)
            {
                entity.RestoreProperties(s.Before);
                entity._name = s.NameBefore;
            }
            foreach (var e in _created)
                _mod.Database[e.GetEntityType()].Remove(e);
            foreach (var e in _deleted)
                _mod.Database[e.GetEntityType()].Restore(e);
            _changed(this);
        }

        public void Redo()
        {
            foreach (var e in _deleted)
                _mod.Database[e.GetEntityType()].Remove(e);
            foreach (var (entity, s) in _states)
            {
                entity.RestoreProperties(s.After);
                entity._name = s.NameAfter;
            }
            // (after their lines are back: the set looks up an entity by the name they give it)
            foreach (var e in _created)
                _mod.Database[e.GetEntityType()].Restore(e);
            _changed(this);
        }
    }

    /// <summary>A change to the mod's header (#modname, #description, #icon, #version, #domversion).</summary>
    internal sealed class ModInfoEdit : IModEdit
    {
        private readonly Action<string?> _set;
        private readonly string? _before, _after;
        private readonly Action<IModEdit> _changed;

        public string Description { get; }
        public IReadOnlyCollection<IDEntity> Entities => Array.Empty<IDEntity>();

        internal ModInfoEdit(string description, Action<string?> set, string? before, string? after, Action<IModEdit> changed)
        {
            Description = description;
            _set = set;
            _before = before;
            _after = after;
            _changed = changed;
        }

        public void Undo()
        {
            _set(_before);
            _changed(this);
        }

        public void Redo()
        {
            _set(_after);
            _changed(this);
        }
    }
}
