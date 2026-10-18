using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Dom5Edit.Events;
using Dom5Editor.Ava.Controls;
using Dom5Editor.UI.Controls;
using Dom5Editor.UI.ViewModels;
using Dom5Editor.UI.Views;

namespace Dom5Editor.Ava.Views
{
    /// <summary>
    /// The editor's window: opening and
    /// saving mods (the Load menu: recent mods, the game's folder, backups), the "Go to" box,
    /// keyboard shortcuts, the report, and where the window was last time.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm = new();
        private readonly Session.Settings _settings = Session.Settings.Load();

        /// <summary>Set by the snapshot harness: no "save changes?" question when closing.</summary>
        public bool SkipCloseConfirmation { get; set; }

        /// <summary>Remembering the window's place and size when it closes (the snapshot harness sizes it itself).</summary>
        public bool KeepLayout { get; set; } = true;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            KeyDown += OnKeyDown;
            Closing += OnClosing;
            // the mouse's back and forward buttons (whatever the pointer is over)
            AddHandler(PointerReleasedEvent, (s, e) =>
            {
                if (e.InitialPressMouseButton == MouseButton.XButton1) _vm.GoBack();
                else if (e.InitialPressMouseButton == MouseButton.XButton2) _vm.GoForward();
            }, RoutingStrategies.Bubble, handledEventsToo: true);

            // the "Go to" box: every entity, listed when first used
            JumpBox.GotFocus += (s, e) => _vm.EnsureJumpTargets();
            JumpBox.SelectionChanged += (s, e) => _jumpPick = e.NewItem;
            if (JumpBox.GetLogicalDescendants().OfType<AutoCompleteBox>().FirstOrDefault() is { } jumpList)
            {
                // the pick changes as the arrow keys move through the list: go when the list closes on
                // it (Enter, a click), not on Escape
                jumpList.AddHandler(KeyDownEvent, (s, e) =>
                {
                    if (e.Key == Key.Escape)
                        _jumpPick = null;
                }, RoutingStrategies.Tunnel);
                jumpList.DropDownClosed += (s, e) => JumpToPick(jumpList);
            }

            // an entity's page in a window of its own (the list's menu, the page's ⧉, Ctrl+click on a
            // link); they close with this one
            _vm.PopOutRequested += ShowPopOut;
            Closed += (s, e) =>
            {
                foreach (var w in _popOuts.ToList())
                    w.Close();
            };

            // deleting something others use asks first
            EntityTypeTab.Confirm = message => WaitFor(Dialogs.Ask(this, "Delete", message, "Delete", "Cancel"), true);
            // an image set on a mod never saved: it's copied next to the .dm file, so save first
            EntityPageViewModel.SaveFirst = () =>
            {
                if (!WaitFor(Dialogs.Ask(this, "Save the mod first",
                        "The image is copied into the mod's folder, next to its .dm file, and this mod hasn't been saved yet.\n\nSave it now?", "Save...", "Cancel"), false))
                    return false;
                WaitFor(SaveAsAsync().ContinueWith(_ => true), false);
                return !string.IsNullOrEmpty(_vm.CurrentFilePath);
            };

            // the game: what's missing, if anything (the view model's note knows only Dominions6.exe)
            if (GameNote() is string note)
                _vm.StatusMessage = note;

            PositionChanged += (s, e) =>
            {
                if (WindowState == WindowState.Normal)
                    _normalPosition = e.Point;
            };
            RestoreLayout();
        }

        public MainWindowViewModel ViewModel => _vm;

        // ---- pages in windows of their own ----

        private readonly List<PageWindow> _popOuts = new();

        /// <summary>The pages open in windows of their own.</summary>
        public IReadOnlyList<PageWindow> PopOuts => _popOuts;

        /// <summary>
        /// A page in a window of its own: where the last one was and as big (remembered between
        /// runs, if that place is still on a screen), else over the main window's page side;
        /// each one more a little lower and to the right.
        /// </summary>
        private void ShowPopOut(PageWindowViewModel vm)
        {
            int k = _popOuts.Count % 8;
            var at = new PixelPoint(Position.X + 360, Position.Y + 60);
            if (_settings.PopOutLeft is double l && _settings.PopOutTop is double t)
            {
                var saved = new PixelPoint((int)Math.Round(l * DesktopScaling), (int)Math.Round(t * DesktopScaling));
                if (Screens.All.Any(s => s.WorkingArea.Contains(saved + new PixelVector(100, 10))))
                    at = saved;
            }
            var window = new PageWindow(vm, this)
            {
                Width = _settings.PopOutWidth is double w && w >= 480 ? w : 920,
                Height = _settings.PopOutHeight is double h && h >= 300 ? h : Math.Min(900, Math.Max(400, Bounds.Height)),
                WindowStartupLocation = WindowStartupLocation.Manual,
                Position = at + new PixelVector(32 * k, 32 * k),
            };
            _popOuts.Add(window);
            window.Closing += (s, e) =>
            {
                if (!KeepLayout || window.WindowState != WindowState.Normal)
                    return;
                _settings.PopOutLeft = window.Position.X / window.DesktopScaling;
                _settings.PopOutTop = window.Position.Y / window.DesktopScaling;
                _settings.PopOutWidth = window.ClientSize.Width;
                _settings.PopOutHeight = window.ClientSize.Height;
                _settings.Save();
            };
            window.Closed += (s, e) => _popOuts.Remove(window);
            window.Show();
        }

        /// <summary>Ctrl+S in a page's own window: the mod saved, as from here.</summary>
        public async Task SaveShortcut()
        {
            if (_vm.HasMod)
                await SaveAsync();
        }

        public void Status(string message) => _vm.StatusMessage = message;

        // ---- the game's folder ----

        /// <summary>
        /// The status bar's note when part of the game wasn't found (null: all of it was). On Mac and
        /// Linux the game's folder has dom6_mac / dom6_amd64 and data/*.trs (its pictures); the texts
        /// and event messages are read from Dominions6.exe (docs/CROSS_PLATFORM.md).
        /// </summary>
        private static string? GameNote()
        {
            if (GameInstall.Exe() != null)
                return null;
            if (GameInstall.GameFolder() is string folder)
                return $"Dominions 6's pictures are read from {folder}; its texts and event messages need Dominions6.exe, which isn't there (▾ next to Load: Dominions 6 folder...)";
            return "Dominions 6 wasn't found: the game's texts, sprites and event messages aren't shown (▾ next to Load: Dominions 6 folder...)";
        }

        /// <summary>
        /// The game's folder for a folder the user picked (it, or the one above if they picked its
        /// data folder), and what of the game isn't in it; null folder if it isn't the game's.
        /// </summary>
        public static (string? Folder, string Note) CheckGameFolder(string picked)
        {
            static bool HasArchives(string dir) => Directory.Exists(Path.Combine(dir, "data")) && Directory.EnumerateFiles(Path.Combine(dir, "data"), "*.trs").Any();
            var folder = new[] { picked, Path.GetDirectoryName(picked.TrimEnd('/', '\\')) }
                .FirstOrDefault(d => !string.IsNullOrEmpty(d) && (HasArchives(d) || File.Exists(Path.Combine(d, "Dominions6.exe"))));
            if (folder == null)
                return (null, $"{picked} isn't Dominions 6's folder: it has no Dominions6.exe, and no data folder with the game's .trs files. " +
                              "Pick the folder the game is installed in (in Steam: Manage, Browse local files).");
            bool exe = File.Exists(Path.Combine(folder, "Dominions6.exe")), art = HasArchives(folder);
            var note = exe && art ? ""
                : !exe ? "\n\nIts pictures (unit, item and site sprites, flags) will be shown, but not the game's texts and event messages: " +
                         "they're read from Dominions6.exe (the Windows game file), which isn't in this folder. " +
                         "With a copy of it from the same game version elsewhere, set DOM6_EXE to its path."
                : "\n\nIt has no data folder with the game's .trs files: the game's pictures won't be shown.";
            return (folder, note);
        }

        private async Task PickGameFolder()
        {
            var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Where is Dominions 6? Pick its folder (with Dominions6.exe, dom6_mac or dom6_amd64, and data)",
                AllowMultiple = false,
            });
            if (picked.Count == 0 || picked[0].TryGetLocalPath() is not string path)
                return;
            var (folder, note) = CheckGameFolder(path);
            if (folder == null)
            {
                await Dialogs.Tell(this, "Dominions 6 folder", note);
                return;
            }
            _settings.GameFolder = folder;
            _settings.Save();
            await Dialogs.Tell(this, "Dominions 6 folder", $"The editor will read the game's texts, sprites and event messages from {folder} the next time it starts.{note}");
        }

        // ---- the Load menu ----

        private void LoadMenu_Click(object? sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { ItemsSource = LoadMenu(), Placement = PlacementMode.BottomEdgeAlignedLeft };
            menu.Open(LoadMenuButton);
        }

        /// <summary>The Load menu: the recent mods, the game's folder, this mod's backups.</summary>
        public List<Control> LoadMenu()
        {
            var items = new List<Control>();
            foreach (var path in _settings.RecentFiles)
            {
                var item = new MenuItem { Header = Path.GetFileName(path) };
                ToolTip.SetTip(item, path);
                item.Click += async (s, a) =>
                {
                    if (await ConfirmDiscard())
                        Open(path);
                };
                items.Add(item);
            }
            if (items.Count == 0)
                items.Add(new MenuItem { Header = "(no recent mods)", IsEnabled = false });
            items.Add(new Separator());

            // a submod over its parent, both picked (Mod Info's "Needs" changes it later)
            var submod = new MenuItem { Header = "Load as submod..." };
            ToolTip.SetTip(submod, "Open a submod over the mod it needs (its parent): pick the submod, then the parent. The parent is read first, " +
                                   "as the game reads it when it's enabled first, and never changed; remembered for the submod (Mod Info, \"Needs\")");
            submod.Click += async (s, a) => await LoadAsSubmod();
            items.Add(submod);

            var exe = GameInstall.Exe();
            var game = exe != null ? Path.GetDirectoryName(exe) : GameInstall.GameFolder();
            var gameItem = new MenuItem { Header = "Dominions 6 folder..." };
            ToolTip.SetTip(gameItem, game == null
                ? "Dominions 6 wasn't found: pick its folder to show the game's texts, sprites and event messages"
                : exe == null ? $"The game's pictures are read from {game}; its texts and event messages need Dominions6.exe, which isn't there. Pick another folder."
                : $"The game's texts, sprites and event messages are read from {game}. Pick another folder.");
            gameItem.Click += async (s, a) => await PickGameFolder();
            items.Add(gameItem);

            var backups = new MenuItem { Header = "Backups of this mod..." };
            ToolTip.SetTip(backups, $"Copies of the mod's file, made when it's opened and before each save (the last {Dom5Edit.ModBackups.Keep} kept), in {Dom5Edit.ModBackups.Folder}");
            backups.Click += async (s, a) => await OpenBackups();
            items.Add(backups);
            return items;
        }

        /// <summary>"Load as submod...": the submod, then the mod it's read over.</summary>
        private async Task LoadAsSubmod()
        {
            if (!await ConfirmDiscard())
                return;
            var filter = Hooks.Filters("Dominions mods (*.dm)|*.dm|All files|*.*");
            var subs = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "The submod to open", AllowMultiple = false, FileTypeFilter = filter });
            if (subs.Count == 0 || subs[0].TryGetLocalPath() is not string sub)
                return;
            var start = Path.GetDirectoryName(sub) is string dir ? await StorageProvider.TryGetFolderFromPathAsync(dir) : null;
            var parents = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"The mod {Path.GetFileName(sub)} needs (its parent, enabled before it in game)",
                AllowMultiple = false,
                FileTypeFilter = filter,
                SuggestedStartLocation = start,
            });
            if (parents.Count == 0 || parents[0].TryGetLocalPath() is not string parent)
                return;
            try
            {
                _vm.LoadAsSubmod(sub, parent);
                _settings.AddRecent(sub);
                _settings.Save();
            }
            catch (Exception ex)
            {
                await Dialogs.Tell(this, "Load Error", $"The mod couldn't be opened:\n\n{ex.Message}");
            }
        }

        /// <summary>Opens the folder with the copies of the current mod's file (made on opening it and before each save).</summary>
        private async Task OpenBackups()
        {
            var file = _vm.CurrentFilePath;
            var folder = string.IsNullOrEmpty(file) ? Dom5Edit.ModBackups.Folder : Dom5Edit.ModBackups.FolderOf(file);
            if (!Directory.Exists(folder))
                await Dialogs.Tell(this, "Backups", "No backups of this mod yet: a copy is made when a mod is opened and before each save.");
            else if (!Desktop.OpenFolder(folder))
                await Dialogs.Tell(this, "Backups", $"The backups are in {folder}");
        }

        // ---- keys ----

        /// <summary>
        /// The window's shortcuts, before the control with the cursor sees the key (a text box would
        /// take Alt+arrows as caret moves): files, search, go to, back and forward.
        /// </summary>
        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl, or Cmd on a Mac
            bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
            bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            bool alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
            // (Option+arrows move by words in a Mac text box: left to it there)
            bool arrowsFree = !(OperatingSystem.IsMacOS() && FocusManager?.GetFocusedElement() is TextBox);
            if (ctrl && e.Key == Key.O) Load_Click(this, e);
            else if (ctrl && e.Key == Key.N) New_Click(this, e);
            else if (ctrl && e.Key == Key.S && shift) { if (_vm.HasMod) SaveAs_Click(this, e); }
            else if (ctrl && e.Key == Key.S) { if (_vm.HasMod) Save_Click(this, e); }
            else if (ctrl && e.Key == Key.F) VisibleTabView()?.FocusSearch();
            else if (ctrl && e.Key == Key.P) FocusJumpBox();
            else if (alt && !ctrl && e.Key == Key.Left && arrowsFree) _vm.GoBack();
            else if (alt && !ctrl && e.Key == Key.Right && arrowsFree) _vm.GoForward();
            else
                return;
            e.Handled = true;
        }

        /// <summary>Undo and redo: after the control with the cursor (a text box undoes its own typing first, as in WPF).</summary>
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
            bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
            if (ctrl && (e.Key == Key.Y || (e.Key == Key.Z && shift))) _vm.Redo();
            else if (ctrl && e.Key == Key.Z) _vm.Undo();
            else
                return;
            e.Handled = true;
        }

        /// <summary>The entity list on screen, if any.</summary>
        private EntityTypeTabView? VisibleTabView() =>
            Tabs.GetVisualDescendants().OfType<EntityTypeTabView>().FirstOrDefault(v => v.IsEffectivelyVisible);

        /// <summary>Ctrl+P: the cursor in the "Go to" box (the entities listed first).</summary>
        private void FocusJumpBox()
        {
            if (!_vm.HasMod)
                return;
            _vm.EnsureJumpTargets();
            if (JumpBox.GetVisualDescendants().OfType<TextBox>().FirstOrDefault() is { } box)
            {
                box.Focus();
                box.SelectAll();
            }
        }

        private ReferenceItem? _jumpPick;

        /// <summary>Goes to the "Go to" box's pick, and empties the box for the next one.</summary>
        private async void JumpToPick(AutoCompleteBox box)
        {
            var pick = _jumpPick;
            _jumpPick = null;
            if (pick == null)
                return;
            _vm.JumpTo(pick);
            // the box isn't done with the pick yet (it takes the cursor back and writes the typed
            // text back in as it closes): the rest once it is
            await Settle();
            if (VisibleTabView() is { } view)
                view.FocusList();
            else
                Focus();
            // the box emptied for the next one. An empty box lists every entity (a new search) when
            // its text box reports the change, a moment later: not this time
            int prefix = box.MinimumPrefixLength;
            box.MinimumPrefixLength = 1;
            JumpBox.SelectedId = null;
            box.Text = "";
            await Settle();
            box.Text = "";
            box.IsDropDownOpen = false;
            await Settle();
            box.MinimumPrefixLength = prefix;
        }

        /// <summary>Lets what's queued run first (input, layout, the controls' own events).</summary>
        private static async Task Settle() => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

        // ---- opening and saving ----

        private bool _closingConfirmed;

        private async void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (SkipCloseConfirmation || _closingConfirmed || !_vm.IsDirty)
            {
                RememberLayout();
                return;
            }
            e.Cancel = true;
            if (await ConfirmDiscard())
            {
                _closingConfirmed = true;
                Close();
            }
        }

        /// <summary>
        /// Before unsaved edits would be lost (a new mod, another mod opened, closing): save them,
        /// drop them, or stay. True to go on.
        /// </summary>
        private async Task<bool> ConfirmDiscard()
        {
            if (!_vm.IsDirty || SkipCloseConfirmation)
                return true;
            switch (await Dialogs.Choose(this, "Unsaved changes", "You have unsaved changes. Do you want to save before continuing?", "Save", "Don't save", "Cancel"))
            {
                case 0:
                    await SaveAsync();
                    return !_vm.IsDirty; // only if the save went through
                case 1:
                    return true;
                default:
                    return false;
            }
        }

        private async void New_Click(object? sender, RoutedEventArgs e)
        {
            if (await ConfirmDiscard())
                _vm.CreateNewMod();
        }

        private async void Load_Click(object? sender, RoutedEventArgs e)
        {
            if (!await ConfirmDiscard())
                return;
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Load Mod File",
                AllowMultiple = false,
                FileTypeFilter = Hooks.Filters("Dominions mod files (*.dm)|*.dm|All files|*.*"),
            });
            if (files.Count > 0 && files[0].TryGetLocalPath() is string path)
                Open(path);
        }

        /// <summary>Opens a mod file (from the dialog, the recent list or the command line) and remembers it.</summary>
        public void Open(string path)
        {
            try
            {
                _vm.LoadMod(path);
                _settings.AddRecent(path);
                _settings.Save();
                if (_settings.LastTab is string tab && _vm.Tabs.OfType<EntityTypeTab>().FirstOrDefault(t => t.Title == tab) is { } found)
                    _vm.SelectedTab = found;
            }
            catch (Exception ex)
            {
                _ = Dialogs.Tell(this, "Load Error", $"Failed to load mod:\n\n{ex.Message}");
            }
        }

        /// <summary>Opens a mod the system hands over (a file opened with the editor), asking about unsaved edits first.</summary>
        public async Task OpenAsking(string path)
        {
            if (await ConfirmDiscard())
                Open(path);
        }

        private async void Save_Click(object? sender, RoutedEventArgs e) => await SaveAsync();

        private async void SaveAs_Click(object? sender, RoutedEventArgs e) => await SaveAsAsync();

        private async Task SaveAsync()
        {
            var path = _vm.CurrentFilePath;
            if (string.IsNullOrEmpty(path) || !await ConfirmSaveLocation(path))
                await SaveAsAsync();
            else
                await SaveTo(path);
        }

        private async Task SaveAsAsync()
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Mod File",
                DefaultExtension = "dm",
                SuggestedFileName = Path.GetFileName(_vm.CurrentFilePath ?? (_vm.ModName ?? "newmod") + ".dm"),
                FileTypeChoices = Hooks.Filters("Dominions mod files (*.dm)|*.dm|All files|*.*"),
            });
            if (file?.TryGetLocalPath() is string path && await ConfirmSaveLocation(path))
                await SaveTo(path);
        }

        /// <summary>
        /// Steam replaces the files in its workshop folder when a mod updates (and checks them):
        /// saving there would lose the changes. Asks first; false: don't save there.
        /// </summary>
        private async Task<bool> ConfirmSaveLocation(string path)
        {
            var full = Path.GetFullPath(path).Replace('\\', '/');
            if (!full.Contains("/steamapps/workshop/content/", StringComparison.OrdinalIgnoreCase))
                return true;
            return await Dialogs.Ask(this, "Save into the workshop folder?",
                $"{Path.GetFileName(path)} is in Steam's workshop folder. Steam replaces these files when the mod updates, so changes saved here can be lost.\n\n" +
                "Save here anyway? (No: choose another place, e.g. your Dominions 6 mods folder.)", "Save here", "No");
        }

        private async Task SaveTo(string path)
        {
            try
            {
                _vm.SaveMod(path);
                _settings.AddRecent(path);
                _settings.Save();
            }
            catch (Exception ex)
            {
                await Dialogs.Tell(this, "Save Error", $"The mod wasn't saved:\n\n{ex.Message}");
            }
        }

        private void Undo_Click(object? sender, RoutedEventArgs e) => _vm.Undo();
        private void Redo_Click(object? sender, RoutedEventArgs e) => _vm.Redo();
        private void Back_Click(object? sender, RoutedEventArgs e) => _vm.GoBack();
        private void Forward_Click(object? sender, RoutedEventArgs e) => _vm.GoForward();

        // ---- the report on the mod (the bar after opening a mod, and Validate) ----

        private ReportWindow? _reportWindow;

        private void Validate_Click(object? sender, RoutedEventArgs e) => OpenReport(rebuild: true);

        /// <summary>"Merge mods...": the open mod first in the list if it's saved; then, if asked, the merged mod opened.</summary>
        private async void Merge_Click(object? sender, RoutedEventArgs e)
        {
            var merge = new MergeWindow();
            if (_vm.CurrentFilePath is string current && File.Exists(current))
                merge.ViewModel.Add(current);
            await merge.ShowDialog(this);
            if (merge.OpenAfter is string merged && await ConfirmDiscard())
                Open(merged);
        }
        /// <summary>"Renumber...": the open mod's numbers moved to the blocks picked (one edit, saved as usual).</summary>
        private async void Renumber_Click(object? sender, RoutedEventArgs e)
        {
            if (_vm.Session is not { } session)
                return;
            var window = new RenumberWindow(new RenumberViewModel(session));
            await window.ShowDialog(this);
            if (window.ViewModel?.IsDone == true)
                _vm.StatusMessage = window.ViewModel.Summary;
        }
        private void ReportOpen_Click(object? sender, RoutedEventArgs e) => OpenReport(rebuild: false);
        private void ReportClose_Click(object? sender, RoutedEventArgs e) => _vm.ShowReportBar = false;

        private async void ReportSave_Click(object? sender, RoutedEventArgs e)
        {
            if (_vm.Report is { } report)
                await ReportFiles.SaveForAuthor(this, report);
        }

        /// <summary>Shows the report window (one, not modal: Go to keeps it open), checking the mod again first if asked.</summary>
        private void OpenReport(bool rebuild)
        {
            var report = rebuild || _vm.Report == null ? _vm.BuildReport() : _vm.Report;
            if (report == null)
                return;
            _reportWindow?.Close();
            _reportWindow = new ReportWindow(_vm, report);
            _reportWindow.Closed += (s, a) => _reportWindow = null;
            _reportWindow.Show(this);
        }

        // ---- where the window was ----

        private PixelPoint _normalPosition;
        private Size _normalSize;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            // the size to come back to after maximizing
            if (change.Property == ClientSizeProperty && WindowState == WindowState.Normal)
                _normalSize = ClientSize;
        }

        /// <summary>The window's size and place from last time (if that's still on a screen), and the list's width.</summary>
        private void RestoreLayout()
        {
            if (_settings.Width is double w && _settings.Height is double h && w > 200 && h > 200)
            {
                Width = w;
                Height = h;
            }
            // (kept in device-independent units, as the WPF editor keeps them: the same settings file on Windows)
            if (_settings.Left is double l && _settings.Top is double t)
            {
                var at = new PixelPoint((int)Math.Round(l * DesktopScaling), (int)Math.Round(t * DesktopScaling));
                if (Screens.All.Any(s => s.WorkingArea.Contains(at + new PixelVector(100, 10))))
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    Position = at;
                }
            }
            if (_settings.Maximized)
                WindowState = WindowState.Maximized;
            if (_settings.ListWidth is double list && list >= 200)
                EntityTypeTabView.ListWidth = list;
        }

        private void RememberLayout()
        {
            if (!KeepLayout)
                return;
            bool normal = WindowState == WindowState.Normal;
            var at = normal ? Position : _normalPosition;
            var size = normal ? ClientSize : _normalSize;
            _settings.Left = at.X / DesktopScaling;
            _settings.Top = at.Y / DesktopScaling;
            if (size.Width > 200 && size.Height > 200)
            {
                _settings.Width = size.Width;
                _settings.Height = size.Height;
            }
            _settings.Maximized = WindowState == WindowState.Maximized;
            _settings.LastTab = (_vm.SelectedTab as EntityTypeTab)?.Title;
            _settings.ListWidth = EntityTypeTabView.ListWidth;
            _settings.Save();
        }

        // ---- questions the core asks while it works ----

        /// <summary>
        /// The answer to a dialog the core needs now (it asks in the middle of an edit, as a WPF
        /// message box would): the dispatcher runs until the dialog closes. <paramref name="fallback"/>
        /// if that can't be done.
        /// </summary>
        private static T WaitFor<T>(Task<T> task, T fallback)
        {
            if (!task.IsCompleted)
            {
                var frame = new DispatcherFrame();
                task.ContinueWith(_ => frame.Continue = false, TaskScheduler.FromCurrentSynchronizationContext());
                try
                {
                    Dispatcher.UIThread.PushFrame(frame);
                }
                catch (Exception)
                {
                    return fallback; // (a platform without nested loops)
                }
            }
            return task.Status == TaskStatus.RanToCompletion ? task.Result : fallback;
        }
    }
}
