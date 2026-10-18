using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Dom5Edit.Commands;
using Dom5Edit.Editing;
using Dom5Edit.Entities;
using Dom5Edit.Validation;
using Dom5Editor.Session;
using Dom5Editor.UI.ViewModels;

namespace Dom5Editor.UI.Views
{
    /// <summary>
    /// The main window: the open mod (EditorSession), a tab per entity type plus the mod's header,
    /// undo/redo, saving, and back/forward through the entities visited; pages opened in windows
    /// of their own (PageWindowViewModel).
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged, IPageHost
    {
        private static readonly (EntityType Type, string Title)[] TabTypes =
        {
            (EntityType.MONSTER, "Monsters"), (EntityType.WEAPON, "Weapons"), (EntityType.ARMOR, "Armor"),
            (EntityType.SPELL, "Spells"), (EntityType.ITEM, "Items"), (EntityType.SITE, "Sites"),
            (EntityType.NATION, "Nations"), (EntityType.EVENT, "Events"), (EntityType.MERCENARY, "Mercenaries"),
            (EntityType.POPTYPE, "Poptypes"), (EntityType.NAMETYPE, "Nametypes"), (EntityType.BLESS, "Blesses"),
            (EntityType.TEMPLATE, "Templates"),
        };

        private EditorSession? _session;
        private object? _selectedTab;
        // (without the game the editor still works: only the game's texts, sprites and messages are missing)
        private string _statusMessage = Dom5Edit.Events.GameInstall.Exe() == null
            ? "Dominions 6 wasn't found: the game's texts, sprites and event messages aren't shown (▾ next to Load: Dominions 6 folder...)"
            : "Ready";
        private readonly List<(EntityTypeTab Tab, EntityListItem Item)> _back = new();
        private readonly List<(EntityTypeTab Tab, EntityListItem Item)> _forward = new();
        private (EntityTypeTab Tab, EntityListItem Item)? _current;
        private bool _travelling;

        public event PropertyChangedEventHandler? PropertyChanged;

        public EditorSession? Session => _session;
        public bool HasMod => _session != null;
        public string? CurrentFilePath => _session?.FilePath;
        public string ModName => _session?.Mod.ModName ?? "No mod loaded";

        /// <summary>The mod header tab, then one tab per entity type.</summary>
        public ObservableCollection<object> Tabs { get; } = new ObservableCollection<object>();

        public object? SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                // only the page on screen refreshes after each edit; the others when shown again
                foreach (var tab in Tabs.OfType<EntityTypeTab>())
                    if (tab.Page != null)
                        tab.Page.IsActive = ReferenceEquals(tab, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedPage));
            }
        }

        /// <summary>The page shown on the selected tab, if any.</summary>
        public EntityPageViewModel? SelectedPage => (SelectedTab as EntityTypeTab)?.Page;

        public EntityTypeTab? TabOf(EntityType type) => Tabs.OfType<EntityTypeTab>().FirstOrDefault(t => t.Type == type);

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool CanUndo => _session?.History.CanUndo == true;
        public bool CanRedo => _session?.History.CanRedo == true;
        public bool IsDirty => _session?.History.IsDirty == true;
        public string? UndoDescription => _session?.History.UndoDescription is string d ? "Undo: " + d : null;
        public string? RedoDescription => _session?.History.RedoDescription is string d ? "Redo: " + d : null;
        public string UndoTip => (UndoDescription ?? "Nothing to undo") + " (Ctrl+Z)";
        public string RedoTip => (RedoDescription ?? "Nothing to redo") + " (Ctrl+Y)";
        public bool CanGoBack => _back.Count > 0;
        public bool CanGoForward => _forward.Count > 0;

        public int EntityCount => _session?.Mod.Database.Values.Sum(s => s.GetFullList().Count) ?? 0;

        // ---- mod ----

        public void CreateNewMod() => Open(EditorSession.New(), "Created a new mod");

        public void LoadMod(string filePath)
        {
            // over the mods it needs, as remembered for it (Mod Info: "Mods this one needs")
            var session = EditorSession.Load(filePath, NeededMods.For(filePath));
            var notes = new[] { session.BackupNote, session.NeededNote }.Where(n => n != null).ToList();
            Open(session, $"Loaded {System.IO.Path.GetFileName(filePath)}" +
                (session.Needed.Count > 0 ? " over " + string.Join(", ", session.Needed.Select(EditorSession.NameOf)) : "") +
                (notes.Count > 0 ? $" ({string.Join("; ", notes)})" : ""));
            // the report on what was opened, once the window has drawn the mod
            Ui.Later(() =>
            {
                if (ReferenceEquals(_session, session))
                    CheckOnOpen();
            });
        }

        /// <summary>
        /// Reads the mod again over these mods (in this order: the order to enable them in game),
        /// remembered for it. Only for a saved mod without unsaved edits: they would be lost.
        /// </summary>
        public void SetNeeded(IReadOnlyList<string> files)
        {
            var path = _session?.FilePath;
            if (_session == null || path == null)
            {
                StatusMessage = "Save the mod first: the mods it needs are remembered for its file";
                return;
            }
            if (_session.History.IsDirty)
            {
                StatusMessage = "Save first: the mod is read again over the mods it needs, and unsaved edits would be lost";
                return;
            }
            if (files.Any(f => string.Equals(System.IO.Path.GetFullPath(f), System.IO.Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = "A mod can't need itself";
                return;
            }
            NeededMods.Set(path, files.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            var tab = (SelectedTab as EntityTypeTab)?.Type;
            try
            {
                LoadMod(path);
            }
            catch (Exception ex)
            {
                StatusMessage = $"The mod couldn't be read again: {ex.Message}";
                return;
            }
            SelectedTab = tab is EntityType t ? TabOf(t) : Tabs[0]; // (the tab it was changed on)
        }

        /// <summary>
        /// Opens a submod over the mod it's read over (the Load menu's "Load as submod..."): the
        /// parent remembered for it, as Mod Info's "Needs" does. The user picks it: the editor
        /// doesn't guess (stray numbers are found in any big mod by chance).
        /// </summary>
        public void LoadAsSubmod(string submod, string parent)
        {
            if (string.Equals(System.IO.Path.GetFullPath(submod), System.IO.Path.GetFullPath(parent), StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "A mod can't be read over itself";
                return;
            }
            NeededMods.Set(submod, new[] { parent });
            LoadMod(submod);
        }

        public void SaveMod(string filePath)
        {
            if (_session == null)
                return;
            _session.Save(filePath);
            StatusMessage = $"Saved {System.IO.Path.GetFileName(filePath)} (checked: it reads back the same; the previous version is in the backups)";
            OnPropertyChanged(nameof(CurrentFilePath));
        }

        private void Open(EditorSession session, string status)
        {
            // the pages in windows of their own were the other mod's
            foreach (var w in _popOuts.ToList())
                w.Close();
            _session = session;
            _report = null;
            ShowReportBar = false;
            _back.Clear();
            _forward.Clear();
            _current = null;
            _jumpTargets = null;
            session.History.Changed += OnHistoryChanged;
            session.Changed += OnSessionChanged;
            session.NavigationRequested += NavigateToEntity;
            session.EntityNavigationRequested += NavigateToEntity;
            Tabs.Clear();
            Tabs.Add(new ModInfoViewModel(session, this));
            foreach (var (type, title) in TabTypes)
            {
                var tab = new EntityTypeTab(session, type, title);
                tab.Selected += OnSelected;
                tab.PopOutRequested += item => PopOut(item);
                tab.Status += message => StatusMessage = message;
                tab.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName != nameof(EntityTypeTab.Page))
                        return;
                    if (tab.Page != null)
                    {
                        tab.Page.IsActive = ReferenceEquals(tab, SelectedTab);
                        tab.Page.Host = this;
                    }
                    OnPropertyChanged(nameof(SelectedPage));
                };
                Tabs.Add(tab);
            }
            SelectedTab = Tabs[1];
            StatusMessage = status;
            OnPropertyChanged(string.Empty);
        }

        private void OnSessionChanged(IModEdit edit)
        {
            if (edit is RenumberEdit)
            {
                // every list again (rows are kept by number); the way back and forward went by the old rows
                _jumpTargets = null;
                foreach (var tab in Tabs.OfType<EntityTypeTab>())
                    tab.Reload();
                _back.Clear();
                _forward.Clear();
                _current = SelectedTab is EntityTypeTab t && t.SelectedItem is { } item ? (t, item) : null;
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoForward));
                OnPropertyChanged(nameof(EntityCount));
                return;
            }
            if (edit.Entities.Count > 0)
                _jumpTargets = null; // names or entities may have changed; rebuilt on next use
            foreach (var tab in Tabs.OfType<EntityTypeTab>())
                tab.OnChanged(edit);
            OnPropertyChanged(nameof(EntityCount));
            OnPropertyChanged(nameof(ModName));
        }

        private void OnHistoryChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(UndoDescription));
            OnPropertyChanged(nameof(RedoDescription));
            OnPropertyChanged(nameof(UndoTip));
            OnPropertyChanged(nameof(RedoTip));
        }

        public void Undo()
        {
            var edit = _session?.Undo();
            if (edit != null)
                StatusMessage = "Undone: " + edit.Description;
        }

        public void Redo()
        {
            var edit = _session?.Redo();
            if (edit != null)
                StatusMessage = "Redone: " + edit.Description;
        }

        // ---- the report on the mod (Dom5Edit.Validation.ModReport) ----

        private ModReport.Report? _report;
        private bool _showReportBar;

        /// <summary>The report made when the mod was opened (or last checked).</summary>
        public ModReport.Report? Report => _report;

        /// <summary>The bar under the toolbar after opening a mod: what the check found.</summary>
        public bool ShowReportBar
        {
            get => _showReportBar;
            set { _showReportBar = value; OnPropertyChanged(); }
        }

        /// <summary>"Checked on opening: 4 go wrong in game · 281 lines the game ignores · ..."</summary>
        public string ReportSummary { get; private set; } = "";

        /// <summary>Whether the report found something that goes wrong in game (the bar is marked).</summary>
        public bool ReportHasWrong => (_report?.Count(ModReport.Wrong) ?? 0) > 0;

        /// <summary>Whether the report found nothing at all.</summary>
        public bool ReportIsClean => _report != null && _report.Total == 0;

        /// <summary>Checks the mod as it is now: the parser's notes, Validate, the event chains.</summary>
        public ModReport.Report? BuildReport()
        {
            if (_session == null)
                return null;
            var validation = new ModValidator().ValidateWithSummary(_session.Mod);
            _report = ModReport.Build(_session.Mod, validation, _session.Events.Problems);
            _validation = validation;
            OnPropertyChanged(nameof(Report));
            OnPropertyChanged(nameof(ReportHasWrong));
            OnPropertyChanged(nameof(ReportIsClean));
            return _report;
        }

        private ValidationResult? _validation;

        /// <summary>The check run when a mod is opened: its counts in the bar.</summary>
        public void CheckOnOpen()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var report = BuildReport();
            if (report == null)
                return;
            static string N(int n, string one, string many) => n == 1 ? $"1 {one}" : $"{n} {many}";
            var parts = new List<string>();
            if (report.Count(ModReport.Wrong) is int w and > 0) parts.Add(N(w, "thing goes wrong in game", "things go wrong in game"));
            if (report.Count(ModReport.Ignored) is int g and > 0) parts.Add(N(g, "line the game ignores", "lines the game ignores"));
            if (report.Count(ModReport.Missing) is int m and > 0) parts.Add(N(m, "number not in this mod or the game", "numbers not in this mod or the game"));
            if (report.Count(ModReport.Look) is int l and > 0) parts.Add(N(l, "thing worth a look", "things worth a look"));
            ReportSummary = parts.Count == 0 ? "Checked on opening: nothing found, the game reads the mod as written"
                : "Checked on opening: " + string.Join(" · ", parts);
            OnPropertyChanged(nameof(ReportSummary));
            ShowReportBar = true;
            ReportMilliseconds = watch.ElapsedMilliseconds;
        }

        /// <summary>How long the check on opening took (the snapshot harness logs it).</summary>
        public long ReportMilliseconds { get; private set; }

        public ValidationResult? Validate()
        {
            if (_session == null)
                return null;
            var results = new ModValidator().ValidateWithSummary(_session.Mod);
            StatusMessage = $"Validation: {results.ErrorCount} errors, {results.WarningCount} warnings";
            return results;
        }

        // ---- navigation ----

        private List<Controls.ReferenceItem>? _jumpTargets;

        /// <summary>Every entity of every type, for the "Go to" box: name and type, ID; built when first used.</summary>
        public IReadOnlyList<Controls.ReferenceItem>? JumpTargets => _jumpTargets;

        public void EnsureJumpTargets()
        {
            if (_session == null || _jumpTargets != null)
                return;
            _jumpTargets = new List<Controls.ReferenceItem>();
            foreach (var (type, title) in TabTypes)
                foreach (var r in _session.References(type))
                    _jumpTargets.Add(new Controls.ReferenceItem { ID = r.ID, DisplayName = $"{r.DisplayName}  ({title.TrimEnd('s').ToLowerInvariant()})", Tag = (type, r.ID) });
            OnPropertyChanged(nameof(JumpTargets));
        }

        /// <summary>Goes to a "Go to" box pick.</summary>
        public void JumpTo(Controls.ReferenceItem? item)
        {
            if (item?.Tag is ValueTuple<EntityType, int> t)
                NavigateToEntity(t.Item1, t.Item2);
        }

        /// <summary>Shows an entity: its type's tab, selected in the list.</summary>
        public void NavigateToEntity(EntityType type, int id)
        {
            var tab = TabOf(type);
            if (tab == null)
            {
                StatusMessage = $"{type} #{id} has no tab";
                return;
            }
            SelectedTab = tab;
            if (!tab.Select(id))
                StatusMessage = $"{type} #{id} not found";
        }

        /// <summary>Shows an entity that may have no number (an event).</summary>
        public void NavigateToEntity(IDEntity entity)
        {
            var tab = TabOf(entity.Kind);
            if (tab == null)
                return;
            SelectedTab = tab;
            if (!tab.Select(entity))
                StatusMessage = $"{entity.Kind} not found";
        }

        // ---- the page's window (IPageHost): links, and pages in windows of their own ----

        /// <summary>A link on a page in the main window: shown here, or with Ctrl in a window of its own.</summary>
        public void Open(EntityType type, int id)
        {
            if (Ui.WantsNewWindow?.Invoke() == true)
                PopOut(type, id);
            else
                NavigateToEntity(type, id);
        }

        public void Open(IDEntity entity)
        {
            if (Ui.WantsNewWindow?.Invoke() == true)
                PopOut(entity);
            else
                NavigateToEntity(entity);
        }

        public void OpenInNewWindow(EntityListItem item) => PopOut(item);

        public bool IsOwnWindow => false;

        private readonly List<PageWindowViewModel> _popOuts = new();

        /// <summary>The pages open in windows of their own.</summary>
        public IReadOnlyList<PageWindowViewModel> PopOuts => _popOuts;

        /// <summary>Raised for a page to show in a window of its own (the view makes the window).</summary>
        public event Action<PageWindowViewModel>? PopOutRequested;

        /// <summary>Opens an entity's page in a window of its own (several can be open, side by side; edits show in all).</summary>
        public PageWindowViewModel? PopOut(EntityListItem item)
        {
            if (_session == null)
                return null;
            var window = new PageWindowViewModel(this, _session, item);
            _popOuts.Add(window);
            window.Closed += () => _popOuts.Remove(window);
            PopOutRequested?.Invoke(window);
            StatusMessage = $"Opened {item.DisplayName} in a window of its own";
            return window;
        }

        public PageWindowViewModel? PopOut(EntityType type, int id)
        {
            if (FindItem(type, id) is { } item)
                return PopOut(item);
            StatusMessage = $"{type} #{id} not found";
            return null;
        }

        public PageWindowViewModel? PopOut(IDEntity entity)
        {
            if (FindItem(entity) is { } item)
                return PopOut(item);
            StatusMessage = $"{entity.Kind} not found";
            return null;
        }

        /// <summary>An entity's row in its type's list (the lists are the main window's), or null.</summary>
        public EntityListItem? FindItem(EntityType type, int id) => TabOf(type)?.Find(id);

        public EntityListItem? FindItem(IDEntity entity) => TabOf(entity.Kind)?.Find(entity);

        private void OnSelected(EntityTypeTab tab, EntityListItem item)
        {
            if (!_travelling)
            {
                if (_current is { } c && !ReferenceEquals(c.Item, item))
                    _back.Add(c);
                _forward.Clear();
            }
            _current = (tab, item);
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
            OnPropertyChanged(nameof(SelectedPage));
        }

        public void GoBack() => Travel(_back, _forward);

        public void GoForward() => Travel(_forward, _back);

        private void Travel(List<(EntityTypeTab Tab, EntityListItem Item)> from, List<(EntityTypeTab Tab, EntityListItem Item)> to)
        {
            if (from.Count == 0)
                return;
            var target = from[^1];
            from.RemoveAt(from.Count - 1);
            if (_current is { } c)
                to.Add(c);
            _travelling = true;
            try
            {
                SelectedTab = target.Tab;
                target.Tab.SelectedItem = target.Item;
                _current = target;
            }
            finally
            {
                _travelling = false;
            }
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>The mod's header (#modname, #description, #version, #domversion, #icon), edited with undo.</summary>
    public sealed class ModInfoViewModel : INotifyPropertyChanged
    {
        private readonly EditorSession _session;
        private readonly MainWindowViewModel _main;

        public ModInfoViewModel(EditorSession session, MainWindowViewModel main)
        {
            _session = session;
            _main = main;
            session.Changed += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            // (saving or undoing to the saved state lets the needed mods change)
            session.History.Changed += () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title => "Mod Info";

        public string? ModName { get => _session.Mod.ModName; set => Set(Command.MODNAME, value); }
        public string? ModDescription { get => _session.Mod.Description; set => Set(Command.DESCRIPTION, value); }
        public string? ModVersion { get => _session.Mod.Version; set => Set(Command.VERSION, value); }
        public string? ModDomVersion { get => _session.Mod.DomVersion; set => Set(Command.DOMVERSION, value); }
        public string? ModIcon { get => _session.Mod.Icon; set => Set(Command.ICON, value); }
        public string? CurrentFilePath => _session.FilePath;

        /// <summary>The icon box's "Pick...": a banner image, copied into the mod's folder like a sprite.</summary>
        public System.Windows.Input.ICommand PickIconCommand => new RelayCommand(() =>
        {
            Ui.PickFile?.Invoke("Mod banner (#icon): 128x32 or 256x64",
                "Images the game reads (*.tga;*.png)|*.tga;*.png|Other images, converted to .png (*.bmp;*.jpg;*.jpeg;*.gif)|*.bmp;*.jpg;*.jpeg;*.gif|All files|*.*", SetIcon);
        });

        /// <summary>Sets #icon from an image file, copied into the mod's sprites folder (a mod with no file is saved first).</summary>
        public void SetIcon(string file)
        {
            if (string.IsNullOrEmpty(_session.Mod.FullFilePath))
                EntityPageViewModel.SaveFirst?.Invoke();
            var modFile = _session.Mod.FullFilePath;
            if (string.IsNullOrEmpty(modFile))
            {
                _main.StatusMessage = "Save the mod first: the image is copied into the mod's folder, next to the .dm file";
                return;
            }
            try
            {
                var result = Sprites.SpriteImport.Import(file, modFile);
                ModIcon = result.RelativePath;
                _main.StatusMessage = (result.CopiedTo != null
                        ? $"Copied {System.IO.Path.GetFileName(file)} into the mod's folder and set #icon \"{result.RelativePath}\""
                        : $"Set #icon \"{result.RelativePath}\"")
                    + (result.Note != null ? $". Note: {result.Note}" : "");
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is ArgumentException)
            {
                _main.StatusMessage = $"The image couldn't be brought into the mod: {ex.Message}";
            }
        }

        // ---- the mods this one needs (a submod's parent) ----

        /// <summary>The mods this one is read over, in the order the game must read them (enabled before it, in this order).</summary>
        public IReadOnlyList<NeededModRow> Needed => _session.Needed.Select((m, i) => new NeededModRow(this, i, m)).ToList();

        public bool HasNeeded => _session.Needed.Count > 0;

        /// <summary>Whether the list can change now: the mod is read again, so it must be saved, without unsaved edits.</summary>
        public bool CanChangeNeeded => _session.FilePath != null && !_session.History.IsDirty;

        public string NeededTip => _session.FilePath == null ? "Save the mod first: the mods it needs are remembered for its file"
            : _session.History.IsDirty ? "Save first: the mod is read again over the mods it needs, and unsaved edits would be lost"
            : "Add a mod this one needs (a submod's parent): its entities are read first, as the game reads them, and are never changed";

        public System.Windows.Input.ICommand AddNeededCommand => new RelayCommand(() =>
            Ui.PickFile?.Invoke("A mod this one needs", "Dominions mods (*.dm)|*.dm|All files|*.*",
                file => _main.SetNeeded(Files().Append(file).ToList())));

        private List<string> Files() => _session.Needed.Select(m => m.FullFilePath).ToList();

        internal void RemoveNeeded(int index)
        {
            var files = Files();
            files.RemoveAt(index);
            _main.SetNeeded(files);
        }

        internal void MoveNeededUp(int index)
        {
            var files = Files();
            (files[index - 1], files[index]) = (files[index], files[index - 1]);
            _main.SetNeeded(files);
        }

        private void Set(Command field, string? value)
        {
            var error = _session.Edit(ed => ed.SetModInfo(field, value));
            if (error != null)
                _main.StatusMessage = error;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    /// <summary>One of the mods the open one needs, on Mod Info: its name and file, removable, movable up.</summary>
    public sealed class NeededModRow
    {
        private readonly ModInfoViewModel _info;
        private readonly int _index;

        internal NeededModRow(ModInfoViewModel info, int index, Dom5Edit.Mod mod)
        {
            _info = info;
            _index = index;
            Name = EditorSession.NameOf(mod) ?? "";
            File = mod.FullFilePath;
        }

        public string Name { get; }
        public string File { get; }
        public string FileName => System.IO.Path.GetFileName(File);
        public string Order => $"{_index + 1}.";
        public bool CanMoveUp => _index > 0 && _info.CanChangeNeeded;

        public System.Windows.Input.ICommand RemoveCommand => new RelayCommand(() => _info.RemoveNeeded(_index), () => _info.CanChangeNeeded);
        public System.Windows.Input.ICommand MoveUpCommand => new RelayCommand(() => _info.MoveNeededUp(_index), () => CanMoveUp);
    }
}
