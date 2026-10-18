using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Dom5Editor.Ava.Controls;
using Dom5Editor.Ava.Views;
using Dom5Edit.Commands;
using Dom5Edit.Entities;
using Dom5Editor.UI;
using Dom5Editor.UI.Controls;
using Dom5Editor.UI.ViewModels;

namespace Dom5Editor.Ava
{
    /// <summary>
    /// Dom5Editor --snapshot STEPS: the editor driven with no one at the screen and rendered
    /// off-screen (Avalonia's headless platform with Skia: no display needed, so it runs in WSL
    /// and Linux CI), with the step names and log lines the WPF harness had. The steps that need only
    /// the view models (--mod, --select, --set, --add, --remove, --undo, --redo, --save, --sweep,
    /// --used-by, --dump, ... ) are shared: Dom5Editor.Core's UI/SnapshotSteps.cs lists them.
    /// This harness's own, which look at the controls:
    ///   --size W H               the window's size (default 1400 x 2000, as WPF's)
    ///   --png FILE.png           render the window
    ///   --view FILE.png          render the selected entity's page at its full height
    ///   --report FILE.png        the check made on opening a mod (its bar, timing) and the report window
    ///   --validation FILE.png    the full issue list ("Every issue..." in the report window)
    ///   --tooltips               every control shown with no tooltip on it or around it, and the buttons' tips
    ///   --key GESTURE            press keys (Ctrl+F, Ctrl+P, Alt+Left, Down, Enter): logs what has the cursor
    ///   --type TEXT              type into what has the cursor (with --key: the "Go to" box, a search box)
    ///   --load-menu              log the Load ▾ menu (recent mods, the game's folder, backups)
    ///   --game-folder PATH       log what "Dominions 6 folder..." would make of PATH (nothing saved)
    ///   --popout FILE.png        the selected page in a window of its own (its "New window"), rendered
    ///   --popout-go TYPE ID      in the newest page window, a link to TYPE ID clicked (opens there)
    ///   --popout-ctrl TYPE ID    the same with Ctrl held (another window); --link-ctrl TYPE ID from the main window's page
    ///   --popout-row TYPE ID     the list's row Ctrl+clicked (or middle-clicked): a window, the list's selection kept
    ///   --popout-back            back in the newest page window
    ///   --popout-png FILE.png    render the newest page window
    ///   --popout-close           close the newest page window: logs what still listens to the mod
    ///   --middle-click-link N    middle-click the Nth link on the main window's page (a mouse click on it):
    ///                            logs the links there (buttons with a LinkCommand or a Link.Open) and the windows
    ///   --merge-window FILE.png OUT.dm A.dm B.dm[=A.dm] ...   "Merge mods...": the mods listed (B over A), merged into
    ///                            OUT.dm; renders FILE-before.png and FILE.png (after), logs the summary
    ///   --renumber-window FILE.png TYPE=START ... [game] [apply]   "Renumber..." on the open mod: start
    ///                            numbers typed (monster=8500 weapon=1700), the game's numbers ticked with
    ///                            "game"; renders FILE.png, logs the rows, notes and problems; "apply" presses
    ///                            Renumber (then --undo, --redo, --save, --select show the result)
    /// Messages go to FILE.png.log / FILE.dm.log next to the first output, and to stdout.
    /// Settings, backups, errors.log and recovery copies go to temp folders, never the user's.
    /// Example: Dom5Editor.Avalonia --snapshot --mod my.dm --select monster 3 --set hp 30 --png hp.png --save out.dm
    /// </summary>
    internal static class Snapshot
    {
        private static readonly List<string> _log = new();
        private static string? _logPath;

        public static int Run(string[] args)
        {
            // test runs keep their backups out of the user's
            Dom5Edit.ModBackups.Folder = Path.Combine(Path.GetTempPath(), "Dom5Editor-snapshot-backups");
            SnapshotSteps.UseTestNeededMods(); // (and the mods each mod needs)
            AppBuilder.Configure<App>()
                .UseSkia()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                .WithInterFont()
                .SetupWithoutStarting();
            int width = 1400, height = 2000;
            for (int i = 1; i + 2 < args.Length; i++)
                if (args[i] == "--size") { width = int.Parse(args[i + 1]); height = int.Parse(args[i + 2]); }
            int code = 0;
            MainWindow? window = null;
            try
            {
                // the user's settings are read (their game folder), never written: from here on they go to temp
                if ((EditorStartup.Configure() ?? EditorStartup.LoadVanilla()) is string problem)
                    Log(problem);
                Session.Settings.Folder = Path.Combine(Path.GetTempPath(), "Dom5Editor-snapshot-settings");
                Hooks.Install();
                window = new MainWindow { Width = width, Height = height, SkipCloseConfirmation = true, KeepLayout = false };
                Hooks.Owner = window;
                EntityTypeTab.Confirm = null; // no dialogs off-screen: deletes go ahead (and say so in the log)
                EntityPageViewModel.SaveFirst = null; // (an image on a mod never saved: the error, not the save dialog)
                window.Show();
                var vm = window.ViewModel;
                if (!args.Contains("--mod"))
                    vm.CreateNewMod();
                Pump();
                Log("game texts: " + Dom5Edit.VanillaLoader.TextsStatus);
                var steps = new SnapshotSteps(vm, Log, Pump) { Output = file => _logPath ??= file + ".log" };

                for (int i = 1; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--size":
                            i += 2;
                            break;
                        case "--png":
                            _logPath ??= args[i + 1] + ".log";
                            Pump();
                            Render(window, args[++i]);
                            Log($"rendered {args[i]}");
                            break;
                        case "--view":
                        {
                            _logPath ??= args[i + 1] + ".log";
                            var page = vm.SelectedPage ?? throw new InvalidOperationException("nothing selected");
                            RenderView(page, width, args[++i]);
                            Log($"rendered view {args[i]}");
                            break;
                        }
                        case "--report":
                        {
                            // --report FILE.png: the check made on opening (its bar's text, how long it took)
                            // and the report window, rendered
                            _logPath ??= args[i + 1] + ".log";
                            Pump();
                            steps.LogReport();
                            var rw = new ReportWindow(vm, vm.Report!) { Width = 1000, Height = 900 };
                            rw.Show();
                            Pump();
                            Render(rw, args[++i]);
                            rw.Close();
                            Log($"rendered report {args[i]}");
                            break;
                        }
                        case "--validation":
                        {
                            // --validation FILE.png: Validate's full issue list, as the report window's "Every issue..." shows it
                            _logPath ??= args[i + 1] + ".log";
                            var result = vm.Validate() ?? throw new InvalidOperationException("no mod");
                            var vw = new ValidationWindow(result, vm) { Width = 900, Height = 900 };
                            vw.Show();
                            Pump();
                            Render(vw, args[++i]);
                            Log($"validation: {vm.StatusMessage}; {vw.ShownText}");
                            vw.Close();
                            Log($"rendered validation {args[i]}");
                            break;
                        }
                        case "--tooltips":
                            Tooltips(window);
                            break;
                        case "--key":
                        {
                            // --key GESTURE: press keys as the user would (Ctrl+F, Ctrl+P, Alt+Left, Down, Enter); logs where the cursor is
                            var gesture = KeyGesture.Parse(args[++i]);
                            var raw = RawInputModifiers.None;
                            if (gesture.KeyModifiers.HasFlag(KeyModifiers.Control)) raw |= RawInputModifiers.Control;
                            if (gesture.KeyModifiers.HasFlag(KeyModifiers.Shift)) raw |= RawInputModifiers.Shift;
                            if (gesture.KeyModifiers.HasFlag(KeyModifiers.Alt)) raw |= RawInputModifiers.Alt;
                            if (gesture.KeyModifiers.HasFlag(KeyModifiers.Meta)) raw |= RawInputModifiers.Meta;
                            window.KeyPress(gesture.Key, raw, PhysicalKey.None, null);
                            window.KeyRelease(gesture.Key, raw, PhysicalKey.None, null);
                            Pump();
                            Log($"key {args[i]}: {Focused(window)}; on {vm.SelectedPage?.DisplayName ?? "(nothing)"}");
                            break;
                        }
                        case "--type":
                            // --type TEXT: type into whatever has the cursor
                            window.KeyTextInput(args[++i]);
                            Pump();
                            Log($"typed {args[i]}: {Focused(window)}");
                            break;
                        case "--game-folder":
                        {
                            // --game-folder PATH: what "Dominions 6 folder..." makes of a folder picked (nothing is saved)
                            var (folder, note) = MainWindow.CheckGameFolder(args[++i]);
                            Log($"game folder {args[i]}: {folder ?? "(not the game's)"}; {note.Trim().Replace("\n", " ")}");
                            break;
                        }
                        case "--flags":
                        {
                            // --flags FILE.png: every nation's flag as the editor shows it (its #flag file,
                            // else the one the game makes from its colors) on one sheet, with the time it
                            // took and a checksum of the pixels in nation order: tools/dom6exe/flags.py
                            // check prints the one the game's rule gives
                            var session = vm.Session!;
                            var tab = vm.TabOf(EntityType.NATION)!;
                            List<(int ID, Dom5Editor.Imaging.Picture? Image)> All() => tab.Items.OrderBy(x => x.ID)
                                .Select(x => (x.ID, Sprites.SpriteLoader.Of(session.Resolve(x.Entity), EntityType.NATION, session.Mod.FullFilePath)))
                                .ToList();
                            var watch = System.Diagnostics.Stopwatch.StartNew();
                            var flags = All();
                            long built = watch.ElapsedMilliseconds;
                            watch.Restart();
                            All(); // cached now
                            long again = watch.ElapsedMilliseconds;
                            using var sha = System.Security.Cryptography.SHA256.Create();
                            var shown = flags.Where(x => x.Image != null).ToList();
                            foreach (var (_, image) in shown)
                                sha.TransformBlock(image!.Bgra, 0, image.Bgra.Length, null, 0);
                            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                            const int cell = 132, cols = 12;
                            int rows = Math.Max(1, (flags.Count + cols - 1) / cols);
                            var sheet = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(cols * cell, rows * cell));
                            using (var dc = sheet.CreateDrawingContext())
                            {
                                dc.FillRectangle(new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)), new Rect(0, 0, cols * cell, rows * cell));
                                for (int k = 0; k < flags.Count; k++)
                                {
                                    var (nation, image) = flags[k];
                                    double x = k % cols * cell, y = k / cols * cell;
                                    if (Hooks.ToBitmap(image) is { } bitmap)
                                        dc.DrawImage(bitmap, new Rect(x + 2, y + 2, 128, 128));
                                    dc.DrawText(new FormattedText(nation.ToString(), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                                        Typeface.Default, 11, Avalonia.Media.Brushes.Yellow), new Point(x + 3, y + 2));
                                }
                            }
                            sheet.Save(Path.GetFullPath(args[++i]));
                            Log($"flags: {shown.Count} of {flags.Count} nations ({built} ms, {again} ms again); pixels sha256 {Convert.ToHexString(sha.Hash!).ToLowerInvariant()[..16]}; sheet {args[i]}");
                            break;
                        }
                        case "--time-view":
                        {
                            // --time-view TYPE NAME: how long selecting a page takes until it's laid out; and how many visuals it has
                            var t = Enum.Parse<EntityType>(args[++i], ignoreCase: true);
                            var name = args[++i];
                            var tab = vm.TabOf(t) ?? throw new ArgumentException("no tab for " + t);
                            vm.SelectedTab = tab;
                            Pump();
                            var item = tab.Items.First(x => x.DisplayName.Contains(name, StringComparison.OrdinalIgnoreCase));
                            var watch = System.Diagnostics.Stopwatch.StartNew();
                            tab.SelectedItem = item;
                            long model = watch.ElapsedMilliseconds;
                            window.UpdateLayout();
                            long layout = watch.ElapsedMilliseconds;
                            Pump();
                            long shown = watch.ElapsedMilliseconds;
                            Log($"   model {model} ms, layout {layout - model} ms, render {shown - layout} ms");
                            var visuals = window.GetVisualDescendants().OfType<Control>().ToList();
                            var counts = visuals.GroupBy(v => v.GetType().Name).OrderByDescending(g => g.Count()).Take(6).Select(g => $"{g.Key} {g.Count()}");
                            Log($"time-view {item.DisplayName}: page {model} ms, shown {shown} ms; visuals {visuals.Count}: {string.Join(", ", counts)}");
                            var page = window.GetVisualDescendants().OfType<EntityPageView>().First();
                            var badges = page.GetVisualDescendants().OfType<CompactBadge>().ToList();
                            var pickers = page.GetVisualDescendants().OfType<RefPicker>().ToList();
                            Log($"   {badges.Count} badges, {badges.Sum(b => b.GetVisualDescendants().Count())} visuals in them; page {page.GetVisualDescendants().Count()}");
                            Log($"   {pickers.Count} pickers ({pickers.Count(p => p.IsEffectivelyVisible)} shown), {pickers.Sum(p => p.GetVisualDescendants().Count())} visuals in them");
                            break;
                        }
                        case "--scroll":
                        {
                            // --scroll Y: scroll the page to Y; then logs where it is (after the next steps, --scroll -1 just logs)
                            var page = window.GetVisualDescendants().OfType<EntityPageView>().First();
                            var scroller = page.GetVisualDescendants().OfType<ScrollViewer>().First();
                            double y = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                            if (y >= 0)
                            {
                                scroller.Offset = new Vector(scroller.Offset.X, y);
                                Pump();
                            }
                            Log($"page scrolled to {scroller.Offset.Y:0} of {scroller.Extent.Height:0}");
                            break;
                        }
                        case "--scroll-list":
                        {
                            // --scroll-list TYPE SCREENS: scroll the type's list a screen at a time (0: to the
                            // end); logs the slowest screen (rows are worked out, sprites decoded, as they show)
                            var t = Enum.Parse<EntityType>(args[++i], ignoreCase: true);
                            int screens = int.Parse(args[++i]);
                            var tab = vm.TabOf(t) ?? throw new ArgumentException("no tab for " + t);
                            vm.SelectedTab = tab;
                            Pump();
                            var list = window.GetVisualDescendants().OfType<EntityTypeTabView>().First(v => v.IsEffectivelyVisible && v.DataContext == tab);
                            var scroller = list.GetVisualDescendants().OfType<ScrollViewer>().First(s => s.Extent.Height > s.Viewport.Height);
                            scroller.ScrollToHome();
                            Pump();
                            var total = System.Diagnostics.Stopwatch.StartNew();
                            long slowest = 0;
                            int n = 0;
                            while ((screens == 0 || n < screens) && scroller.Offset.Y + scroller.Viewport.Height < scroller.Extent.Height - 1)
                            {
                                var watch = System.Diagnostics.Stopwatch.StartNew();
                                scroller.PageDown();
                                Pump();
                                slowest = Math.Max(slowest, watch.ElapsedMilliseconds);
                                n++;
                            }
                            Log($"scroll-list {t}: {n} screens in {total.ElapsedMilliseconds} ms, slowest {slowest} ms; " +
                                $"memory {GC.GetTotalMemory(true) / (1 << 20)} MB managed, {System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1 << 20)} MB working set");
                            var all = System.Diagnostics.Stopwatch.StartNew();
                            int withSprite = tab.Items.Count(x => x.Sprite != null);
                            Log($"   {withSprite} of {tab.Items.Count} rows have a sprite (the rest worked out in {all.ElapsedMilliseconds} ms)");
                            break;
                        }
                        case "--type-tab":
                        {
                            // --type-tab COMMAND VALUE NEXT: type VALUE in COMMAND's box on screen, then Tab to
                            // NEXT's box: logs where the cursor is after the edit rebuilt the page
                            var c = SnapshotSteps.CommandOf(args[++i]);
                            var value = args[++i];
                            var next = SnapshotSteps.CommandOf(args[++i]);
                            TextBox BoxOf(Command cmd)
                            {
                                var badges = window.GetVisualDescendants().OfType<CompactBadge>().Where(b => b.DataContext is PropertyItem p && p.Command == cmd).ToList();
                                var boxes = badges.SelectMany(b => b.GetVisualDescendants().OfType<TextBox>()).ToList();
                                return boxes.FirstOrDefault(b => b.IsEffectivelyVisible)
                                       ?? throw new InvalidOperationException($"no box for {cmd}: {badges.Count} badges, {boxes.Count} boxes");
                            }
                            var box = BoxOf(c);
                            box.Focus();
                            box.Text = value;
                            Pump();
                            BoxOf(next).Focus(); // what Tab does
                            Pump();
                            var badge = (window.FocusManager?.GetFocusedElement() as Visual)?.FindAncestorOfType<CompactBadge>();
                            Log($"typed {value} in {args[i - 2]}, tabbed: cursor in {(badge?.DataContext as PropertyItem)?.Command.ToString() ?? "(nothing)"}; page has {vm.SelectedPage?.Resolved.Get(c)?.Arguments}");
                            break;
                        }
                        case "--map":
                        {
                            // --map click TEXT | dblclick TEXT | hover TEXT | drag DX DY | fit | zoom Z | png FILE:
                            // the event page's chain map driven with the mouse (a card by part of its title)
                            var graph = window.GetVisualDescendants().OfType<ChainGraph>().FirstOrDefault(g => g.IsEffectivelyVisible)
                                        ?? throw new InvalidOperationException("no chain map on screen");
                            var scroller = graph.FindAncestorOfType<ScrollViewer>()!;
                            scroller.BringIntoView(); // (the page scrolled to the map, as a user would)
                            Pump();
                            Point CardAt(string text)
                            {
                                var node = graph.Nodes!.FirstOrDefault(n => n.Title.Contains(text, StringComparison.OrdinalIgnoreCase))
                                           ?? throw new ArgumentException("no card " + text);
                                var center = new Point((node.Bounds.Left + node.Bounds.Width / 2) * graph.Zoom, (node.Bounds.Top + node.Bounds.Height / 2) * graph.Zoom);
                                // (scrolled into view first, as a user would)
                                scroller.Offset = new Vector(Math.Max(0, center.X - scroller.Viewport.Width / 2), Math.Max(0, center.Y - scroller.Viewport.Height / 2));
                                Pump();
                                return graph.TranslatePoint(center, window) ?? throw new InvalidOperationException("the map isn't in the window");
                            }
                            var what = args[++i];
                            switch (what)
                            {
                                case "click":
                                case "dblclick":
                                {
                                    var at = CardAt(args[++i]);
                                    window.MouseMove(at);
                                    window.MouseDown(at, MouseButton.Left);
                                    window.MouseUp(at, MouseButton.Left);
                                    if (what == "dblclick")
                                    {
                                        window.MouseDown(at, MouseButton.Left);
                                        window.MouseUp(at, MouseButton.Left);
                                    }
                                    break;
                                }
                                case "hover":
                                    window.MouseMove(CardAt(args[++i]));
                                    break;
                                case "drag":
                                {
                                    double dx = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture), dy = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                                    var from = scroller.TranslatePoint(new Point(scroller.Viewport.Width / 2, scroller.Viewport.Height / 2), window)!.Value;
                                    window.MouseMove(from);
                                    window.MouseDown(from, MouseButton.Middle);
                                    for (int k = 1; k <= 5; k++)
                                        window.MouseMove(new Point(from.X + dx * k / 5, from.Y + dy * k / 5));
                                    window.MouseUp(new Point(from.X + dx, from.Y + dy), MouseButton.Middle);
                                    break;
                                }
                                case "fit":
                                    graph.FitCommand.Execute(null);
                                    break;
                                case "zoom":
                                    graph.Zoom = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                                    break;
                                case "png":
                                {
                                    // the map's part of the window, as it's shown (on the page's background)
                                    Pump();
                                    var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("nothing rendered");
                                    var origin = scroller.TranslatePoint(default, window)!.Value;
                                    var area = new Rect(origin, scroller.Bounds.Size);
                                    using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)Math.Ceiling(area.Width), (int)Math.Ceiling(area.Height)));
                                    using (var dc = bitmap.CreateDrawingContext())
                                        dc.DrawImage(frame, area, new Rect(area.Size));
                                    bitmap.Save(Path.GetFullPath(args[++i]));
                                    break;
                                }
                                default:
                                    throw new ArgumentException("--map " + what);
                            }
                            Pump();
                            Log($"map {what}: zoom {graph.Zoom:0.00}, scrolled to {scroller.Offset.X:0},{scroller.Offset.Y:0} of {scroller.Extent.Width:0}x{scroller.Extent.Height:0}; " +
                                $"card clicked: {graph.SelectedCard?.Title ?? "none"}; on {vm.SelectedPage?.DisplayName}");
                            break;
                        }
                        case "--popout":
                        {
                            // the page's "New window" button
                            _logPath ??= args[i + 1] + ".log";
                            var page = vm.SelectedPage ?? throw new InvalidOperationException("nothing selected");
                            Log($"popout: the page offers it: {page.CanPopOut}");
                            page.PopOutCommand.Execute(null);
                            Pump();
                            LogPopOuts(window);
                            Render(Newest(window), args[++i]);
                            Log($"rendered page window {args[i]}");
                            break;
                        }
                        case "--popout-go":
                        case "--popout-ctrl":
                        case "--link-ctrl":
                        {
                            var step = args[i];
                            var type = Enum.Parse<EntityType>(args[++i], ignoreCase: true);
                            int id = int.Parse(args[++i]);
                            var page = step == "--link-ctrl" ? vm.SelectedPage : Newest(window).ViewModel?.Page;
                            if (page == null)
                                throw new InvalidOperationException("no page to click a link on");
                            Hooks.ForceNewWindow = step != "--popout-go";
                            try { page.Go(type, id); }
                            finally { Hooks.ForceNewWindow = false; }
                            Pump();
                            Log($"{step} {type} {id}: main window on {vm.SelectedPage?.DisplayName ?? "(nothing)"}");
                            LogPopOuts(window);
                            break;
                        }
                        case "--middle-click-link":
                        {
                            int n = int.Parse(args[++i]);
                            Pump();
                            var view = window.GetVisualDescendants().OfType<EntityPageView>().FirstOrDefault(v => v.IsEffectivelyVisible)
                                       ?? throw new InvalidOperationException("no page on screen");
                            var links = view.GetVisualDescendants().OfType<Button>()
                                .Where(b => b.IsEffectivelyVisible && (b.Command is ILinkCommand || Link.GetOpen(b) != null)).ToList();
                            var nonLinks = view.GetVisualDescendants().OfType<Button>().Count(b => b.IsEffectivelyVisible) - links.Count;
                            Log($"links on the page: {links.Count} ({string.Join(", ", links.Take(12).Select(b => ToolTip.GetTip(b) as string ?? (b.Content as string) ?? "?").Select(t => t.Length > 40 ? t[..40] : t))}); other buttons {nonLinks}");
                            if (n >= links.Count)
                                throw new InvalidOperationException($"no link {n}");
                            var target = links[n];
                            target.BringIntoView();
                            Pump();
                            var at = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window) ?? throw new InvalidOperationException("link not on screen");
                            window.MouseMove(at);
                            window.MouseDown(at, MouseButton.Middle);
                            window.MouseUp(at, MouseButton.Middle);
                            Pump();
                            Log($"middle-clicked link {n} ({ToolTip.GetTip(target)}): main window on {vm.SelectedPage?.DisplayName}");
                            LogPopOuts(window);
                            break;
                        }
                        case "--popout-row":
                        {
                            var type = Enum.Parse<EntityType>(args[++i], ignoreCase: true);
                            int id = int.Parse(args[++i]);
                            var tab = vm.TabOf(type) ?? throw new ArgumentException("no tab for " + type);
                            var item = tab.Find(id) ?? throw new InvalidOperationException($"no {type} {id}");
                            tab.PopOut(item);
                            Pump();
                            Log($"--popout-row {type} {id}: the list's selection {tab.SelectedItem?.DisplayName ?? "(none)"}");
                            LogPopOuts(window);
                            break;
                        }
                        case "--popout-back":
                            Newest(window).ViewModel?.GoBack();
                            Pump();
                            LogPopOuts(window);
                            break;
                        case "--popout-png":
                            _logPath ??= args[i + 1] + ".log";
                            Pump();
                            Render(Newest(window), args[++i]);
                            Log($"rendered page window {args[i]}");
                            break;
                        case "--popout-close":
                        {
                            var session = vm.Session!;
                            int before = session.ChangedListeners;
                            Newest(window).Close();
                            Pump();
                            Log($"closed a page window: {window.PopOuts.Count} left; the mod's listeners {before} -> {session.ChangedListeners}");
                            break;
                        }
                        case "--merge-window":
                        {
                            var png = args[++i];
                            _logPath ??= png + ".log";
                            var output = Path.GetFullPath(args[++i]);
                            var merge = new MergeWindow { Width = 860, Height = 720 };
                            // (SUB.dm=PARENT.dm: a submod read over its parent, as its row's "Parent..." sets)
                            while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                            {
                                var a = args[++i];
                                int eq = a.IndexOf(".dm=", StringComparison.OrdinalIgnoreCase);
                                merge.ViewModel.Add(Path.GetFullPath(eq > 0 ? a[..(eq + 3)] : a));
                                if (eq > 0)
                                    merge.ViewModel.SetNeeds(merge.ViewModel.Rows[^1], a[(eq + 4)..]);
                            }
                            merge.ViewModel.OutputFile = output;
                            merge.Show();
                            Pump();
                            Render(merge, Path.ChangeExtension(png, null) + "-before.png");
                            Log($"merge window: {merge.ViewModel.Rows.Count} mods ({string.Join(", ", merge.ViewModel.Rows.Select(r => $"{r.Order}. {r.Name}{(r.HasNeeds ? " " + r.NeedsText : "")}"))}); problem: {merge.ViewModel.Problem ?? "none"}");
                            var watch = System.Diagnostics.Stopwatch.StartNew();
                            var task = merge.ViewModel.MergeAsync();
                            while (!task.IsCompleted)
                            {
                                Pump();
                                Thread.Sleep(50);
                            }
                            Pump();
                            Render(merge, png);
                            Log($"merged in {watch.ElapsedMilliseconds} ms: {merge.ViewModel.Status}\n{merge.ViewModel.Summary}");
                            merge.Close();
                            break;
                        }
                        case "--renumber-window":
                        {
                            var png = args[++i];
                            _logPath ??= png + ".log";
                            var rvm = new Dom5Editor.UI.Views.RenumberViewModel(vm.Session ?? throw new InvalidOperationException("no mod open"));
                            var renumber = new RenumberWindow(rvm) { Width = 780, Height = 660 };
                            bool apply = false;
                            while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                            {
                                var a = args[++i];
                                if (a == "game")
                                    rvm.GameNumbers = true;
                                else if (a == "apply")
                                    apply = true;
                                else
                                {
                                    var kv = a.Split('=', 2);
                                    var row = rvm.Rows.FirstOrDefault(r => r.Survey.Kind == kv[0].ToLowerInvariant()) ?? throw new ArgumentException($"the mod has no {kv[0]}s to renumber");
                                    row.Start = kv[1];
                                }
                            }
                            renumber.Show();
                            Pump();
                            Render(renumber, png);
                            foreach (var r in rvm.Rows)
                                Log($"   {r.Title}: {r.CountText}, {r.RangeText} -> {(r.Start.Length > 0 ? r.Start : "(stays)")}: {r.NewRange}{(r.Error != null ? " " + r.Error : "")}");
                            foreach (var p in rvm.Problems)
                                Log("   problem: " + p);
                            foreach (var n in rvm.Notes)
                                Log("   note: " + n);
                            Log($"renumber window: {rvm.Summary}");
                            if (apply)
                            {
                                var watch = System.Diagnostics.Stopwatch.StartNew();
                                var error = rvm.Apply();
                                Pump();
                                Log(error != null ? "renumber refused: " + error : $"renumbered in {watch.ElapsedMilliseconds} ms: {rvm.Summary}");
                            }
                            renumber.Close();
                            break;
                        }
                        case "--load-menu":
                            // the Load ▾ menu: its entries and their tooltips
                            foreach (var item in window.LoadMenu())
                                Log(item is MenuItem m ? $"   {m.Header}{(m.IsEnabled ? "" : " (disabled)")}: {ToolTip.GetTip(m)}" : "   ----");
                            Log("load menu");
                            break;
                        default:
                            if (!steps.TryRun(args, ref i))
                                throw new ArgumentException("unknown snapshot step " + args[i]);
                            break;
                    }
                    Pump();
                }
            }
            catch (Exception ex)
            {
                Log("ERROR " + ex);
                code = 1;
            }
            finally
            {
                window?.Close();
                if (_logPath != null)
                    File.WriteAllLines(_logPath, _log);
            }
            return code;
        }

        private static PageWindow Newest(MainWindow window) =>
            window.PopOuts.Count > 0 ? window.PopOuts[^1] : throw new InvalidOperationException("no page window open");

        /// <summary>The page windows open: their titles, what each shows, back/forward.</summary>
        private static void LogPopOuts(MainWindow window)
        {
            Log($"page windows: {window.PopOuts.Count}");
            foreach (var w in window.PopOuts)
            {
                var pw = w.ViewModel!;
                Log($"   '{w.Title}': {(pw.IsGone ? pw.GoneNote : pw.Page?.DisplayName)}; back {pw.CanGoBack}, forward {pw.CanGoForward}; " +
                    $"page host is its window: {ReferenceEquals(pw.Page?.Host, pw)}");
            }
        }

        private static void Log(string message)
        {
            _log.Add(message);
            Console.WriteLine(message);
        }

        /// <summary>Lets bindings, layout and rendering catch up.</summary>
        private static void Pump()
        {
            for (int k = 0; k < 4; k++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }
        }

        /// <summary>What has the keyboard: "TextBox 'Search' in EntityTypeTabView".</summary>
        private static string Focused(Window window)
        {
            if (window.FocusManager?.GetFocusedElement() is not Control c)
                return "nothing focused";
            var view = c.GetVisualAncestors().OfType<Control>().FirstOrDefault(a => a is UserControl || a is RefPicker || a is Window);
            return $"{c.GetType().Name}{(string.IsNullOrEmpty(c.Name) ? "" : $" '{c.Name}'")} in {view?.GetType().Name}{(string.IsNullOrEmpty(view?.Name) ? "" : $" '{view!.Name}'")}" +
                   (c is TextBox t && !string.IsNullOrEmpty(t.Text) ? $" (\"{t.Text}\")" : "");
        }

        private static void Render(TopLevel top, string path)
        {
            var frame = top.CaptureRenderedFrame() ?? throw new InvalidOperationException("nothing rendered");
            frame.Save(Path.GetFullPath(path));
        }

        /// <summary>
        /// The selected entity's page (the view the main window shows for it) at the list's
        /// neighbour's width and whatever height it needs, so nothing is cut off by scrolling.
        /// </summary>
        private static void RenderView(EntityPageViewModel page, int width, string path)
        {
            var view = new EntityPageView { DataContext = page };
            var host = new Window { Width = width - 320, Height = 600, Content = new Border { Padding = new Thickness(16), Child = view } };
            host.Show();
            Pump();
            // its scroll viewer, given unlimited height, takes the content's height
            var root = (Control)host.Content!;
            root.Measure(new Size(width - 320, double.PositiveInfinity));
            host.Height = Math.Max(200, Math.Ceiling(root.DesiredSize.Height));
            Pump();
            Render(host, path);
            host.Close();
        }

        /// <summary>
        /// --tooltips: every visible control (button, box, check box, picker) with no tooltip on it
        /// or on anything around it, and the tooltips the buttons show.
        /// </summary>
        private static void Tooltips(Window window)
        {
            Pump();
            int controls = 0, missing = 0;
            var seen = new HashSet<string>();
            foreach (var c in window.GetVisualDescendants().OfType<Control>())
            {
                if (!(c is Button || c is TextBox || c is ComboBox || c is RefPicker) || !c.IsEffectivelyVisible || !c.IsHitTestVisible || c.Opacity <= 0)
                    continue;
                // parts of a control's own template (a combo box's, a scroll bar's, a picker's box) show its tooltip
                if (c.TemplatedParent is ComboBox or ScrollBar or TextBox or AutoCompleteBox || c.FindAncestorOfType<RefPicker>() is { } picker && !ReferenceEquals(picker, c))
                    continue;
                controls++;
                object? tip = null;
                for (Visual? x = c; x != null && tip == null; x = x.GetVisualParent())
                    if (x is Control f && ToolTip.GetTip(f) is object t && !(t is string ts && ts.Length == 0))
                        tip = t;
                var what = $"{c.GetType().Name} '{(c as ContentControl)?.Content as string ?? (c as TextBox)?.Text ?? ""}' in {c.DataContext?.GetType().Name}";
                if (tip == null && seen.Add(what))
                {
                    missing++;
                    Log($"   no tooltip: {what}");
                }
                else if (tip is string text && c is Button && seen.Add("tip:" + text))
                    Log($"   tip: {c.GetType().Name} '{(c as ContentControl)?.Content as string}': {text.Replace('\n', ' ')}");
            }
            Log($"tooltips: {controls} controls shown, {missing} kinds without a tooltip");
        }
    }
}
