using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Dom5Edit;
using Dom5Edit.Commands;
using Dom5Edit.Editing;
using Dom5Edit.Entities;
using Dom5Edit.Props;
using Dom5Editor.Session;

namespace Dom5Editor.UI.ViewModels
{
    /// <summary>
    /// One entity type's tab: the list of entities (vanilla and the mod's) and the page of the
    /// selected one. The list is built when the tab is first shown and kept up to date as edits
    /// create, delete, rename or first change entities.
    /// </summary>
    public sealed class EntityTypeTab : INotifyPropertyChanged
    {
        private readonly EditorSession _session;
        private ObservableCollection<EntityListItem>? _items;
        private readonly Dictionary<object, EntityListItem> _byKey = new Dictionary<object, EntityListItem>();
        private EntityListItem? _selected;
        private EntityPageViewModel? _page;

        public EntityTypeTab(EditorSession session, EntityType type, string title)
        {
            _session = session;
            Type = type;
            Title = title;
            // (blesses can only be selected; a template is made for a nation)
            NewCommand = new RelayCommand(CreateNew, () => type != EntityType.BLESS && type != EntityType.TEMPLATE);
            DeleteCommand = new RelayCommand<object>(o => Delete(o as EntityListItem), o => o is EntityListItem i && (i.IsNew || i.IsModified));
        }

        // the list's filter, kept per tab (the list view is rebuilt when the tab is shown again)
        private string _searchText = "";
        private bool _showVanilla = true, _showModified = true, _showNew = true;
        public string SearchText { get => _searchText; set { _searchText = value ?? ""; OnPropertyChanged(); } }
        public bool ShowVanilla { get => _showVanilla; set { _showVanilla = value; OnPropertyChanged(); } }
        public bool ShowModified { get => _showModified; set { _showModified = value; OnPropertyChanged(); } }
        public bool ShowNew { get => _showNew; set { _showNew = value; OnPropertyChanged(); } }
        private string _sortBy = "ID";
        public string SortBy { get => _sortBy; set { _sortBy = value ?? "ID"; OnPropertyChanged(); } }

        private string _facet = "All";

        /// <summary>A type's own filter ("Rituals", "In a chain"), "All" for none; see <see cref="Facets"/>.</summary>
        public string Facet { get => _facet; set { _facet = value ?? "All"; OnPropertyChanged(); } }

        /// <summary>The type's own filters, or null: events by kind and chain, spells by kind.</summary>
        public IReadOnlyList<string>? Facets => Type switch
        {
            EntityType.EVENT => new[] { "All", "Good", "Bad", "Always", "Global", "In a chain", "Started by a spell", "With problems" },
            EntityType.SPELL => new[] { "All", "Combat spells", "Rituals", "Global enchantments", "Summons" },
            EntityType.MONSTER => new[] { "All", "Mages", "Priests", "Pretenders", "Recruitable" },
            EntityType.ITEM => new[] { "All", "Weapons", "Missile weapons", "Shields", "Armor", "Helmets and crowns", "Boots", "Misc items" },
            EntityType.WEAPON => new[] { "All", "Melee", "Missile" },
            EntityType.ARMOR => new[] { "All", "Shields", "Body armor", "Helmets", "Barding" },
            EntityType.SITE => new[] { "All", "Fire", "Air", "Water", "Earth", "Astral", "Death", "Nature", "Glamour", "Blood", "Holy" },
            EntityType.NATION => new[] { "All", "Early era", "Middle era", "Late era" },
            _ => null,
        };

        private static readonly Command[] RecruitCommands = { Command.ADDRECUNIT, Command.ADDRECCOM, Command.ADDFOREIGNUNIT, Command.ADDFOREIGNCOM };

        /// <summary>Whether a row is in one of the type's own filters.</summary>
        private bool InFacet(EntityListItem item, string facet)
        {
            if (Type == EntityType.EVENT)
            {
                var graph = _session.Events;
                var e = _session.Editor.OwnEntity(item.Entity) ?? item.Entity;
                var rarity = Dom5Edit.Events.EventInfo.Rarity(graph.LinesOf(e)) ?? 0;
                return facet switch
                {
                    "Good" => Dom5Edit.Events.EventInfo.IsGood(rarity),
                    "Bad" => Dom5Edit.Events.EventInfo.IsBad(rarity),
                    "Always" => rarity == 0 || rarity == 5,
                    "Global" => Dom5Edit.Events.EventInfo.IsGlobal(rarity),
                    "In a chain" => graph.ChainOf(e) != null,
                    "Started by a spell" => graph.To(e).Any(l => !l.IsEventToEvent),
                    "With problems" => graph.ProblemsOf(e).Any(),
                    _ => true,
                };
            }
            if (Type != EntityType.SPELL)
            {
                var r = _session.Resolve(item.Entity);
                long N(Command c) => Dom5Edit.Events.EventInfo.Number(r.Get(c)?.Property) ?? -1;
                IEnumerable<long> Paths() => r.GetAll(Command.MAGICSKILL).Select(v => Dom5Edit.Events.EventInfo.Number(v.Property) ?? -1);
                return (Type, facet) switch
                {
                    (EntityType.MONSTER, "Mages") => Paths().Any(p => p >= 0 && p <= 8) || r.Has(Command.CUSTOMMAGIC),
                    (EntityType.MONSTER, "Priests") => Paths().Any(p => p == 9),
                    (EntityType.MONSTER, "Pretenders") => r.Has(Command.PATHCOST) || r.Has(Command.STARTDOM),
                    (EntityType.MONSTER, "Recruitable") => item.ID > 0 && _session.Usage.UsedBy(EntityType.MONSTER, item.ID).Any(u => RecruitCommands.Contains(u.Via)),
                    (EntityType.ITEM, "Weapons") => N(Command.TYPE) is 1 or 2,
                    (EntityType.ITEM, "Missile weapons") => N(Command.TYPE) == 3,
                    (EntityType.ITEM, "Shields") => N(Command.TYPE) == 4,
                    (EntityType.ITEM, "Armor") => N(Command.TYPE) is 5 or 10,
                    (EntityType.ITEM, "Helmets and crowns") => N(Command.TYPE) is 6 or 9,
                    (EntityType.ITEM, "Boots") => N(Command.TYPE) == 7,
                    (EntityType.ITEM, "Misc items") => N(Command.TYPE) == 8,
                    (EntityType.WEAPON, "Melee") => N(Command.RANGE) <= 0,
                    (EntityType.WEAPON, "Missile") => N(Command.RANGE) > 0,
                    (EntityType.ARMOR, "Shields") => N(Command.TYPE) == 4,
                    (EntityType.ARMOR, "Body armor") => N(Command.TYPE) == 5,
                    (EntityType.ARMOR, "Helmets") => N(Command.TYPE) == 6,
                    (EntityType.ARMOR, "Barding") => N(Command.TYPE) == 9,
                    (EntityType.SITE, _) => Array.IndexOf(new[] { "Fire", "Air", "Water", "Earth", "Astral", "Death", "Nature", "Glamour", "Blood", "Holy" }, facet) is int p && p >= 0 && N(Command.PATH) == p,
                    (EntityType.NATION, "Early era") => N(Command.ERA) == 1,
                    (EntityType.NATION, "Middle era") => N(Command.ERA) == 2,
                    (EntityType.NATION, "Late era") => N(Command.ERA) == 3,
                    _ => true,
                };
            }
            if (Type == EntityType.SPELL)
            {
                var r = _session.Resolve(item.Entity);
                long effect = Dom5Edit.Events.EventInfo.Number(r.Get(Command.EFFECT)?.Property) ?? 0;
                var kind = Data.GameTables.EffectOf((int)effect);
                return facet switch
                {
                    "Combat spells" => effect < 10000,
                    "Rituals" => effect >= 10000,
                    "Global enchantments" => Dom5Edit.Events.EventInfo.IsEnchantmentEffect(effect),
                    "Summons" => kind?.ArgumentType == "unit_id",
                    _ => true,
                };
            }
            return true;
        }

        public System.Windows.Input.ICommand NewCommand { get; }
        public System.Windows.Input.ICommand DeleteCommand { get; }

        /// <summary>Why the last new/delete couldn't be done, for the status bar.</summary>
        public string? LastError { get; private set; }

        private void CreateNew()
        {
            IDEntity? made = null;
            LastError = _session.Edit(ed =>
            {
                var edit = ed.Create(Type, $"New {Singular.ToLowerInvariant()}", out var m);
                made = m;
                return edit;
            });
            if (made != null)
                SelectedItem = Items.FirstOrDefault(i => ReferenceEquals(i.Entity, made));
        }

        /// <summary>The list's "+ New" tooltip: what it makes.</summary>
        public string NewTip => Type switch
        {
            EntityType.EVENT => "Make a new event (#newevent, at the end of the file) and open it",
            EntityType.MERCENARY => "Make a new mercenary band (#newmerc) and open it",
            EntityType.BLESS or EntityType.TEMPLATE => $"New {Nouns.Plural(Type)} aren't made here: a {Nouns.Of(Type)} belongs to {(Type == EntityType.BLESS ? "the game (select one to change it)" : "a nation")}",
            _ => $"Make a new {Nouns.Of(Type)} (next free ID) and open it",
        };

        /// <summary>The list's "Delete" tooltip: what it does to the selected row.</summary>
        public string DeleteTip => _selected == null ? $"Select a {Nouns.Of(Type)} to delete it"
            : _selected.IsNew ? $"Delete {Nouns.Named(_selected.DisplayName, _selected.ID)} from the mod"
            : _selected.IsModified ? $"Drop the mod's changes to {Nouns.Named(_selected.DisplayName, _selected.ID)}"
            : $"{Nouns.Named(_selected.DisplayName, _selected.ID)} is unchanged: nothing to delete";

        /// <summary>Asks the user to confirm something (set by the window; with none, the answer is yes).</summary>
        public static Func<string, bool>? Confirm { get; set; }

        /// <summary>Raised with a message for the status bar.</summary>
        public event Action<string>? Status;

        private void Delete(EntityListItem? item)
        {
            if (item == null)
                return;
            var users = item.ID > 0 ? _session.Usage.UsedBy(Type, item.ID).Select(u => (u.Type, u.Id)).Distinct().Count() : 0;
            string what = item.IsNew ? $"Delete {item.DisplayName} #{item.ID}" : $"Drop this mod's changes to {item.DisplayName} #{item.ID}";
            if (users > 0 && item.IsNew && Confirm?.Invoke($"{what}?\n\n{users} entities refer to it; their references would point at nothing. (Undo brings it back.)") == false)
                return;
            LastError = _session.Edit(ed => ed.Delete(item.Entity));
            Status?.Invoke(LastError ?? (item.IsNew ? $"Deleted {item.DisplayName} #{item.ID}" + (users > 0 ? $"; {users} entities still refer to it" : "")
                                                     : $"Dropped the mod's changes to {item.DisplayName} #{item.ID}"));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raised when the user (or navigation) selects an entity, for the back/forward history.</summary>
        public event Action<EntityTypeTab, EntityListItem>? Selected;

        public EntityType Type { get; }
        public string Title { get; }

        /// <summary>One of the type ("Monster" for "Monsters", "Mercenary" for "Mercenaries").</summary>
        public string Singular => Title.EndsWith("ies") ? Title[..^3] + "y" : Title.EndsWith("sses") || Title.EndsWith("ses") ? Title[..^2] : Title.TrimEnd('s');

        public ObservableCollection<EntityListItem> Items => _items ??= Build();

        /// <summary>
        /// Whether a row shows with these filters: its kind (the game's unchanged, changed by the
        /// mod, new), the type's own filter (a facet: "Commanders", ...), and the search text in its
        /// name, number or anything the type searches (an event's message). Both editors' lists.
        /// </summary>
        public static bool Matches(EntityListItem item, bool showVanilla, bool showModified, bool showNew, string facet, string search)
        {
            if (item.IsVanilla && !item.IsModified && !showVanilla) return false;
            if (item.IsModified && !showModified) return false;
            if (item.IsNew && !showNew) return false;
            if (!item.InFacet(facet)) return false;
            if (!string.IsNullOrEmpty(search)
                && (item.DisplayName ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0
                && !item.ID.ToString().Contains(search)
                && !item.MatchesText(search))
                return false;
            return true;
        }

        /// <summary>The rows shown with the tab's own filters, in its order (by name, else by number).</summary>
        public IEnumerable<EntityListItem> Shown()
        {
            var rows = Items.Where(r => Matches(r, ShowVanilla, ShowModified, ShowNew, Facet, SearchText));
            return SortBy == "DisplayName" ? rows.OrderBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase) : rows.OrderBy(r => r.SortKey);
        }

        public EntityListItem? SelectedItem
        {
            get => _selected;
            set
            {
                if (ReferenceEquals(_selected, value))
                    return;
                _selected = value;
                Page = value == null ? null : EntityPages.Create(_session, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(DeleteTip));
                if (value != null)
                    Selected?.Invoke(this, value);
            }
        }

        /// <summary>The selected entity's page.</summary>
        public EntityPageViewModel? Page
        {
            get => _page;
            private set
            {
                _page?.Detach();
                _page = value;
                OnPropertyChanged();
            }
        }

        /// <summary>An entity's row, by the entity itself (or its ID), or null.</summary>
        public EntityListItem? Find(IDEntity entity) =>
            Items.FirstOrDefault(i => ReferenceEquals(i.Entity, entity)) ?? (HasNumber(entity) ? Items.FirstOrDefault(i => i.ID == entity.ID) : null);

        /// <summary>The row of the entity with this ID, or null.</summary>
        public EntityListItem? Find(int id) => Items.FirstOrDefault(i => i.ID == id);

        /// <summary>Whether the row is still in the list (not deleted since).</summary>
        public bool Lists(EntityListItem item) => _byKey.TryGetValue(Key(item.Entity), out var held) && ReferenceEquals(held, item);

        /// <summary>Selects an entity's row by the entity itself (or its ID).</summary>
        public bool Select(IDEntity entity)
        {
            var item = Find(entity);
            if (item == null)
                return false;
            SelectedItem = item;
            return true;
        }

        /// <summary>Selects the entity with this ID (false if the list has none).</summary>
        public bool Select(int id)
        {
            var item = Find(id);
            if (item == null)
                return false;
            SelectedItem = item;
            return true;
        }

        /// <summary>Raised to open a row in a window of its own (the list's menu, Ctrl+click, a middle click).</summary>
        public event Action<EntityListItem>? PopOutRequested;

        /// <summary>Opens a row in a window of its own; the list's selection stays.</summary>
        public void PopOut(EntityListItem item) => PopOutRequested?.Invoke(item);

        /// <summary>
        /// The list again from the mod, the same entity selected (after a renumbering: rows are
        /// kept by number). The views pick up the new rows from the Items change.
        /// </summary>
        public void Reload()
        {
            if (_items == null)
                return;
            var selected = _selected?.Entity;
            _byKey.Clear();
            _items = Build();
            OnPropertyChanged(nameof(Items));
            if (selected != null)
            {
                _selected = null;
                SelectedItem = Find(selected);
            }
        }

        /// <summary>Keeps the list in step with an edit (and an undo or redo of one).</summary>
        public void OnChanged(IModEdit edit)
        {
            if (_items == null)
                return;
            foreach (var entity in edit.Entities.Where(e => e.Kind == Type))
                Update(entity);
        }

        private void Update(IDEntity entity)
        {
            var key = Key(entity);
            var set = _session.Mod.Database[Type];
            bool held = HasNumber(entity) && set.TryGetValue(entity.ID, out var byId) ? ReferenceEquals(byId, entity)
                : set.GetFullList().Any(e => ReferenceEquals(e, entity));
            var vanilla = VanillaOf(entity.ID);
            if (_byKey.TryGetValue(key, out var item))
            {
                if (!held && vanilla == null)
                {
                    // a mod entity deleted (or an undone create)
                    _items!.Remove(item);
                    _byKey.Remove(key);
                    if (ReferenceEquals(_selected, item))
                        SelectedItem = null;
                    return;
                }
                item.Entity = held ? entity : vanilla!;
                item.IsModified = item.IsVanilla && held;
                item.DisplayName = NameOf(item.Entity);
                item.Refresh();
                return;
            }
            if (held)
            {
                bool game = vanilla != null || entity.Selected && !HasVanillaData(Type);
                var added = new EntityListItem(Type, entity, NameOf(entity), isVanilla: game, isModified: game)
                    { FromMod = EditorSession.NameOf(vanilla?.ParentMod), FromModChangesGame = vanilla != null && ChangesGame(vanilla) };
                Equip(added);
                // (an event made next to another is sorted there: its place in the file)
                added.Order = Type == EntityType.EVENT && _session.Events.IndexOf(entity) is int at && at >= 0 ? at : _items!.Count;
                _byKey[key] = added;
                _items!.Add(added);
            }
        }

        private ObservableCollection<EntityListItem> Build()
        {
            var list = new List<EntityListItem>();
            var mod = _session.Mod;
            var own = mod.Database.TryGetValue(Type, out var set) ? set.GetFullList() : new List<IDEntity>();
            var ownById = own.Where(HasNumber).GroupBy(e => e.ID).ToDictionary(g => g.Key, g => g.First());
            // the entities of the mods read before this one (vanilla, then the mods it needs): by
            // number the nearest one's, in vanilla's order; a needed mod's new ones after them
            var below = new Dictionary<object, IDEntity>();
            foreach (var layer in _session.Below.Reverse())
                if (layer.Database.TryGetValue(Type, out var layerSet))
                    foreach (var v in layerSet.GetFullList())
                        below[Key(v)] = v;
            foreach (var v in below.Values)
            {
                var from = EditorSession.NameOf(v.ParentMod);
                bool changesGame = from != null && ChangesGame(v);
                if (ownById.TryGetValue(v.ID, out var changed))
                    list.Add(new EntityListItem(Type, changed, NameOf(changed), isVanilla: true, isModified: true) { FromMod = from, FromModChangesGame = changesGame });
                else
                    list.Add(new EntityListItem(Type, v, Type == EntityType.EVENT || string.IsNullOrEmpty(v.Name) && from != null ? NameOf(v)
                        : string.IsNullOrEmpty(v.Name) ? $"#{v.ID}" : v.Name, isVanilla: true, isModified: false) { FromMod = from, FromModChangesGame = changesGame });
            }
            var listed = new HashSet<int>(list.Select(i => i.ID));
            // the mod's own; a #select of a type the loaded vanilla data lacks is still the game's:
            // listed as vanilla, changed. (The game's mercenaries have no number: a mod's #newmerc
            // bands are listed after them.)
            foreach (var e in own)
                if (!HasNumber(e) || !listed.Contains(e.ID))
                {
                    bool game = e.Selected && !HasVanillaData(Type);
                    list.Add(new EntityListItem(Type, e, NameOf(e), isVanilla: game, isModified: game));
                }
            int order = 0;
            foreach (var item in list)
            {
                Equip(item);
                item.Order = order++;
                _byKey[Key(item.Entity)] = item;
            }
            return new ObservableCollection<EntityListItem>(list);
        }

        /// <summary>
        /// Whether the vanilla data has this type (vanilla.dm has every type but AI templates, which
        /// the game only reads from mods; the events come from their own file). A mod's #select of an
        /// ID it doesn't have makes a new entity; for a type it lacks the #select changes a game
        /// entity we can't show.
        /// </summary>
        public static bool HasVanillaData(EntityType type) =>
            VanillaLoader.Vanilla?.Database.TryGetValue(type, out var set) == true && set.GetFullList().Count > 0;

        /// <summary>Gives a row its detail line (key stats) and sprite, worked out when the row is shown.</summary>
        private void Equip(EntityListItem item)
        {
            item.DetailProvider = Detail;
            item.FacetMatcher = InFacet;
            if (Type == EntityType.EVENT)
                item.TextMatcher = (i, text) => Dom5Edit.Events.EventInfo.Message(_session.Events.LinesOf(_session.Editor.OwnEntity(i.Entity) ?? i.Entity)) is string msg
                    && msg.Contains(text, StringComparison.OrdinalIgnoreCase);
            if (Type == EntityType.MONSTER || Type == EntityType.ITEM || Type == EntityType.SITE || Type == EntityType.NATION)
                item.SpriteProvider = i => Sprites.SpriteLoader.Thumbnail(Sprites.SpriteLoader.Of(_session.Resolve(i.Entity), Type, _session.Mod.FullFilePath));
        }

        /// <summary>A row's key stats, by type.</summary>
        private string Detail(EntityListItem item)
        {
            (Command, string)[] fields = Type switch
            {
                EntityType.MONSTER => new[] { (Command.HP, "hp"), (Command.ATT, "att"), (Command.DEF, "def"), (Command.PROT, "prot"), (Command.SIZE, "size") },
                EntityType.WEAPON => new[] { (Command.DMG, "dmg"), (Command.ATT, "att"), (Command.DEF, "def"), (Command.LEN, "len") },
                EntityType.ARMOR => new[] { (Command.PROT, "prot"), (Command.DEF, "def"), (Command.ENC, "enc") },
                EntityType.SPELL => new[] { (Command.RESEARCHLEVEL, "research") },
                EntityType.ITEM => new[] { (Command.CONSTLEVEL, "const") },
                EntityType.SITE => new[] { (Command.LEVEL, "level") },
                _ => Array.Empty<(Command, string)>(),
            };
            if (Type == EntityType.EVENT)
                return EventDetail(item);
            if (Type is EntityType.MERCENARY or EntityType.POPTYPE or EntityType.NAMETYPE or EntityType.BLESS)
                return ListDetail(item);
            if (fields.Length == 0)
                return "";
            var r = _session.Resolve(item.Entity);
            var detail = string.Join("  ", fields.Select(f => r.Get(f.Item1) is { } v ? $"{f.Item2} {v.Arguments}" : null).Where(x => x != null));
            if (Type == EntityType.MONSTER)
            {
                // a mage's paths, as the game abbreviates them: F3 S2, +2 random
                var paths = string.Join(" ", r.GetAll(Command.MAGICSKILL).Select(v => v.Property is IntIntProperty p && p.Value1 >= 0 && p.Value1 <= 9
                    ? $"{"FAWESDNGBH"[p.Value1]}{p.Value2}" : null).Where(x => x != null));
                int random = r.GetAll(Command.CUSTOMMAGIC).Count();
                if (paths.Length > 0 || random > 0)
                    detail += "   " + paths + (random > 0 ? $" +{random}" : "");
            }
            return detail;
        }

        /// <summary>
        /// A row of a type without stats to show: a band's commander and size, a poptype's
        /// recruits, a nametype's first names, a bless's paths and cost (poptypes and nametypes
        /// have no name in game, so this is what tells them apart).
        /// </summary>
        private string ListDetail(EntityListItem item)
        {
            var r = _session.Resolve(item.Entity);
            long N(Command c) => Dom5Edit.Events.EventInfo.Number(r.Get(c)?.Property) ?? 0;
            string Monster(Dom5Edit.Resolve.ResolvedValue v)
            {
                int id = v.Property is MonsterOrMontagRef m && m.MonsterRef != null && m.MonsterRef.HasValue ? m.MonsterRef.ID
                    : int.TryParse(v.Arguments.Split(' ')[0], out var n) ? n : 0;
                return id > 0 && _session.Mod.TryGet(EntityType.MONSTER, id, null, out var e)
                    && _session.Resolve(e).Get(Command.NAME)?.Property is StringProperty s ? s.Value ?? $"#{id}" : $"#{id}";
            }
            string Names(IEnumerable<string> names, int shown)
            {
                var list = names.ToList();
                return string.Join(", ", list.Take(shown)) + (list.Count > shown ? $" +{list.Count - shown}" : "");
            }
            switch (Type)
            {
                case EntityType.MERCENARY:
                    var com = r.Get(Command.COM);
                    return (com != null ? Monster(com) : "") + $"   level {N(Command.LEVEL)}, {N(Command.NRUNITS)} men, {N(Command.MINPAY)} gold";
                case EntityType.POPTYPE:
                    return Names(r.GetAll(Command.ADDRECUNIT).Select(Monster), 3);
                case EntityType.NAMETYPE:
                    var names = r.GetAll(Command.ADDNAME).Select(v => v.Property is StringProperty s ? s.Value ?? "" : v.Arguments).ToList();
                    return names.Count == 0 ? "" : $"{names.Count} names: " + Names(names, 4);
                case EntityType.BLESS:
                    string Path(Command path, Command cost) => r.Get(path) is { } && N(path) is >= 0 and <= 9 ? $"{"FAWESDNGBH"[(int)N(path)]}{N(cost)}" : "";
                    return (Path(Command.PATH0, Command.COST0) + " " + Path(Command.PATH1, Command.COST1)).Trim();
            }
            return "";
        }

        /// <summary>An event's row: how it's rolled, and what starts it (an enchantment, a code, a spell).</summary>
        private string EventDetail(EntityListItem item)
        {
            var lines = _session.Resolve(item.Entity).Values.Select(v => v.Property).ToList();
            var parts = new List<string> { Dom5Edit.Events.EventInfo.RarityName(Dom5Edit.Events.EventInfo.Rarity(lines)) };
            foreach (var p in lines)
            {
                var n = Dom5Edit.Events.EventInfo.Number(p);
                if (Dom5Edit.Events.EventInfo.EnchantmentCheckers.Contains(p.Command) && p.Command != Command.REQ_NOENCH)
                    parts.Add($"ench {n}");
                else if (Dom5Edit.Events.EventInfo.CodeCheckers.Contains(p.Command) && n != 0)
                    parts.Add($"needs code {n}");
                else if (Dom5Edit.Events.EventInfo.CodeSetters.Contains(p.Command) && n != 0)
                    parts.Add($"sets code {n}");
                else if (Dom5Edit.Events.EventInfo.Delays.Contains(p.Command))
                    parts.Add($"delay {n}");
                else if (p.Command == Command.ID)
                    parts.Add($"spell event {n}");
            }
            return string.Join(" · ", parts.Distinct().Take(4));
        }

        /// <summary>Whether a needed mod's entity is its #select of one the mods under it (the game) have.</summary>
        private static bool ChangesGame(IDEntity e) => e.Selected && e.ParentMod.FindBelow(e.Kind, e.ID, null) != null;

        /// <summary>The entity with this number in the mods read before this one (the nearest that has it: a needed mod, vanilla), or null.</summary>
        private IDEntity? VanillaOf(int id)
        {
            if (id < 0 || id == 0 && !NumberedFromZero(Type))
                return null;
            foreach (var layer in _session.Below)
            {
                if (!layer.Database.TryGetValue(Type, out var set))
                    continue;
                // (an entity set doesn't keep number 0 by its number)
                var v = id > 0 ? set.TryGetValue(id, out var byId) ? byId : null : set.GetFullList().FirstOrDefault(e => e.ID == 0 && e.Selected);
                if (v != null)
                    return v;
            }
            return null;
        }

        private object Key(IDEntity e) => HasNumber(e) ? e.ID : e;

        /// <summary>Types whose game numbers start at 0: event 0, nation 0 (the Independents), bless 0.</summary>
        public static bool NumberedFromZero(EntityType type) => type is EntityType.EVENT or EntityType.NATION or EntityType.BLESS;

        /// <summary>Whether the entity has a number in game: an ID, or a game event's number (from 0; a #newevent has none).</summary>
        private bool HasNumber(IDEntity e) => Type == EntityType.EVENT ? e.ID >= 0 && e.Selected : e.ID > 0 || e.ID == 0 && e.Selected && NumberedFromZero(Type);

        /// <summary>The entity's name in game (a copy's may come from its source).</summary>
        private string NameOf(IDEntity entity)
        {
            // an event has no name: its title comes from its message
            if (Type == EntityType.EVENT)
                return EventPageViewModel.TitleOf(entity, EventPageViewModel.LinesOf(_session.Resolve(entity)));
            var name = _session.Resolve(entity).Get(Command.NAME)?.Property is StringProperty s ? s.Value : null;
            if (string.IsNullOrEmpty(name))
                name = entity.HeaderName;
            if (string.IsNullOrEmpty(name))
                return entity.ID > 0 ? $"{Singular} {entity.ID}" : $"(unnamed {Singular.ToLowerInvariant()})";
            return name!;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
