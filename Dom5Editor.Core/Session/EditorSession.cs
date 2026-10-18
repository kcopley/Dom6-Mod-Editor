using Dom5Edit;
using Dom5Edit.Commands;
using Dom5Edit.Editing;
using Dom5Edit.Entities;
using Dom5Edit.Props;
using Dom5Edit.Resolve;
using Dom5Editor.UI.Controls;

namespace Dom5Editor.Session
{
    /// <summary>
    /// One open mod in the editor: the model, the core editor every change goes through, undo and
    /// redo, and saving. Views read what an entity is in game from <see cref="Resolve"/> and change
    /// it only through <see cref="Edit"/> (docs/EDIT_FLOW.md, "GUI structure").
    /// </summary>
    public sealed class EditorSession
    {
        public Mod Mod { get; }
        public ModEditor Editor { get; }
        public EditHistory History { get; } = new EditHistory();

        /// <summary>The file the mod was loaded from or last saved to (null for a new, unsaved mod).</summary>
        public string? FilePath { get; private set; }

        /// <summary>Raised after every edit, undo and redo, with the edit.</summary>
        public event Action<IModEdit>? Changed;

        /// <summary>How many listen to <see cref="Changed"/> (the snapshot harness checks closed pages let go).</summary>
        internal int ChangedListeners => Changed?.GetInvocationList().Length ?? 0;

        /// <summary>Raised when a page asks to show another entity (a reference was clicked).</summary>
        public event Action<EntityType, int>? NavigationRequested;

        private readonly Dictionary<EntityType, List<ReferenceItem>> _references = new Dictionary<EntityType, List<ReferenceItem>>();

        private EditorSession(Mod mod, string? path)
        {
            Mod = mod;
            FilePath = path;
            Editor = new ModEditor(mod);
            Usage = new UsageIndex(this);
            Editor.Changed += e =>
            {
                // a renumbering (Renumber..., its undo and redo): what's kept by number is worked out again
                if (e is RenumberEdit)
                {
                    _references.Clear();
                    _events = null;
                    _spells = null;
                    Usage.Reset();
                    Changed?.Invoke(e);
                    return;
                }
                // the touched entities' names (or existence) may have changed: update their rows
                foreach (var entity in e.Entities)
                    UpdateReference(entity);
                if (e.Entities.Any(x => x.Kind == EntityType.EVENT || x.Kind == EntityType.SPELL))
                    _events = null;
                if (e.Entities.Any(x => x.Kind == EntityType.SPELL))
                    _spells = null;
                Usage.OnChanged(e);
                Changed?.Invoke(e);
            };
        }

        /// <summary>Which entities refer to which ("used by").</summary>
        public UsageIndex Usage { get; }

        /// <summary>The mods this one is read over (a submod's parent), in the order the game reads them; never changed or saved.</summary>
        public IReadOnlyList<Mod> Needed { get; private init; } = Array.Empty<Mod>();

        /// <summary>Why a needed mod was left out on opening (gone, unreadable), for the status bar; null if none was.</summary>
        public string? NeededNote { get; private init; }

        private IReadOnlyList<Mod>? _below;

        /// <summary>The mods read before this one, nearest first: the needed mods, then vanilla (Mod.Below).</summary>
        public IReadOnlyList<Mod> Below => _below ??= Mod.Below().ToList();

        /// <summary>The entity as the mods read before this one have it (the nearest that has the number), or null.</summary>
        public IDEntity? BaseEntity(EntityType type, int id) => Mod.FindBelow(type, id, null);

        /// <summary>A needed mod's name, for labels ("From Sombre Warhammer"); null for vanilla.</summary>
        public static string? NameOf(Mod? mod) => mod == null || mod.Dependencies.Count == 0 ? null : mod.DisplayName;

        public void Navigate(EntityType type, int id) => NavigationRequested?.Invoke(type, id);

        /// <summary>Raised to show an entity by itself (events have no number).</summary>
        public event Action<IDEntity>? EntityNavigationRequested;

        /// <summary>Shows an entity: by ID when it has one, else by itself.</summary>
        public void Navigate(IDEntity entity)
        {
            if (entity.ID > 0)
                NavigationRequested?.Invoke(entity.Kind, entity.ID);
            else
                EntityNavigationRequested?.Invoke(entity);
        }

        private Dom5Edit.Events.EventGraph? _events;
        private Dom5Edit.Events.SpellIndex? _spells;

        /// <summary>
        /// The mod's events as the game links them (codes, delays, variables, choices, spells),
        /// worked out when first asked for after an edit (the spells only after a spell edit).
        /// </summary>
        public Dom5Edit.Events.EventGraph Events =>
            _events ??= Dom5Edit.Events.EventGraph.Build(Mod, Resolve, Below,
                _spells ??= Dom5Edit.Events.SpellIndex.Build(Mod, Resolve, Below));

        /// <summary>Every entity of a type (vanilla and the mod's), for reference pickers: ID and name in game.</summary>
        public IReadOnlyList<ReferenceItem> References(EntityType type)
        {
            if (_references.TryGetValue(type, out var list))
                return list;
            var byId = new SortedDictionary<int, ReferenceItem>();
            var unnumbered = new List<ReferenceItem>();
            void Add(IDEntity e)
            {
                var item = ReferenceOf(e);
                if (e.ID > 0) byId[e.ID] = item; else unnumbered.Add(item);
            }
            // vanilla first, then the mods over it: a later one's entity for a number replaces it
            foreach (var layer in Below.Reverse())
                if (layer.Database.TryGetValue(type, out var set))
                    foreach (var e in set.GetFullList())
                        Add(e);
            if (Mod.Database.TryGetValue(type, out var own))
                foreach (var e in own.GetFullList())
                    Add(e);
            return _references[type] = byId.Values.Concat(unnumbered).ToList();
        }

        private ReferenceItem ReferenceOf(IDEntity e)
        {
            var name = Resolve(e).Get(Command.NAME)?.Property is StringProperty s ? s.Value : null;
            return new ReferenceItem { ID = e.ID, DisplayName = string.IsNullOrEmpty(name) ? $"#{e.ID}" : name!, Tag = e };
        }

        /// <summary>Keeps a cached reference list in step with one entity (renamed, created, deleted).</summary>
        private void UpdateReference(IDEntity entity)
        {
            if (!_references.TryGetValue(entity.Kind, out var list))
                return;
            int i = list.FindIndex(r => entity.ID > 0 ? r.ID == entity.ID : ReferenceEquals(r.Tag, entity));
            bool held = Mod.Database.TryGetValue(entity.Kind, out var set) && set.GetFullList().Contains(entity)
                        || entity.ID > 0 && BaseEntity(entity.Kind, entity.ID) != null;
            if (!held)
            {
                if (i >= 0)
                    list.RemoveAt(i);
                return;
            }
            var item = ReferenceOf(entity);
            if (i >= 0)
                list[i] = item;
            else
            {
                int at = list.FindIndex(r => r.ID > entity.ID || r.ID <= 0);
                list.Insert(at < 0 ? list.Count : at, item);
            }
        }

        /// <summary>References by the JSON configs' type names ("monster", "weapon", ...).</summary>
        public IReadOnlyList<ReferenceItem> References(string? refType) =>
            Data.BadgeConfigLoader.GetEntityTypeFromRefType(refType) is EntityType t ? References(t) : Array.Empty<ReferenceItem>();

        /// <summary>An empty mod on top of the vanilla data.</summary>
        public static EditorSession New()
        {
            var mod = new Mod { ModName = "New mod" };
            mod.ResolveDependencies();
            mod.Resolve();
            return new EditorSession(mod, null);
        }

        /// <summary>
        /// Opens a mod over the mods it needs (<paramref name="needed"/>: read first, each over the
        /// one before, as the game reads enabled mods); a copy of its file as it is now goes to the
        /// backups first (ModBackups). A needed mod that can't be read is left out (NeededNote).
        /// </summary>
        public static EditorSession Load(string path, IReadOnlyList<string>? needed = null)
        {
            string? backupNote;
            try
            {
                backupNote = ModBackups.Backup(path, "opened") != null ? "a backup copy was made" : null;
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
            {
                // opening still works; the status says the copy couldn't be made
                backupNote = $"no backup copy could be made ({ex.Message})";
            }
            Mod? below = null;
            var loaded = new List<Mod>();
            var problems = new List<string>();
            foreach (var file in needed ?? Array.Empty<string>())
            {
                var name = System.IO.Path.GetFileName(file);
                if (!System.IO.File.Exists(file))
                {
                    problems.Add($"{name} isn't there any more");
                    continue;
                }
                try
                {
                    below = Mod.Import(file, below);
                    loaded.Add(below);
                }
                catch (Exception ex)
                {
                    problems.Add($"{name} couldn't be read ({ex.Message})");
                }
            }
            return new EditorSession(Mod.Import(path, below), path)
            {
                BackupNote = backupNote,
                Needed = loaded,
                NeededNote = problems.Count > 0 ? "left out: " + string.Join("; ", problems) : null,
            };
        }

        /// <summary>What happened to the backup on opening, for the status bar (null: the same copy was there already).</summary>
        public string? BackupNote { get; private init; }

        public ResolvedEntity Resolve(IDEntity entity) => Editor.Resolve(entity);

        /// <summary>
        /// Makes an edit and records it for undo. Returns null on success, else why it couldn't be
        /// made (an edit the game offers no way to make, e.g. removing a base stat).
        /// </summary>
        public string? Edit(Func<ModEditor, IModEdit?> edit)
        {
            try
            {
                var done = edit(Editor);
                if (done != null)
                    History.Record(done);
                return null;
            }
            catch (EditException ex)
            {
                return ex.Message;
            }
        }

        /// <summary>Several changes as one undo step.</summary>
        public string? Edit(string description, Action<Transaction> body) => Edit(ed => ed.Run(description, body));

        public IModEdit? Undo() => History.Undo();

        public IModEdit? Redo() => History.Redo();

        /// <summary>
        /// Saves the mod (in its file's order, unedited lines as read): the file it replaces is
        /// backed up first, and the new file must read back with the same entities before it takes
        /// the old one's place (Mod.SafeSave); else nothing changes and an IOException says why.
        /// </summary>
        public void Save(string path)
        {
            Mod.SafeSave(path);
            FilePath = path;
            History.MarkSaved();
        }
    }
}
