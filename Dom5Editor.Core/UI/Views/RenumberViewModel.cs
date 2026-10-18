using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Dom5Edit.Entities;
using Dom5Edit.Merge;
using Dom5Editor.Session;

namespace Dom5Editor.UI.Views
{
    /// <summary>
    /// "Renumber..." (Dom5Edit.Merge.ModRenumber, docs/RENUMBER.md): the open mod's numbers per
    /// type, a start number for each, and the block each would move to; then the move, one edit
    /// of the open mod. Undo puts it back; saving is the usual save, with its backups and its
    /// read-back check.
    /// </summary>
    public sealed class RenumberViewModel : INotifyPropertyChanged
    {
        private readonly EditorSession _session;
        // what doesn't depend on the start numbers, worked out once (the window is modal: the mod doesn't change meanwhile)
        private readonly RenumberContext _context;
        private RenumberPlan? _plan;
        private bool _gameNumbers;
        private string? _result;

        public RenumberViewModel(EditorSession session)
        {
            _session = session;
            _context = new RenumberContext(session.Mod);
            foreach (var t in _context.Survey.Where(t => t.Count > 0 || t.OnGameNumbers.Count > 0))
                Rows.Add(new RenumberRow(this, t));
            Update();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>A row per type the mod numbers itself (monsters, weapons, armor, sites).</summary>
        public ObservableCollection<RenumberRow> Rows { get; } = new();

        public bool HasRows => Rows.Count > 0;

        public string ModText => $"{_session.Mod.DisplayName}" + (_session.FilePath != null ? $" ({Path.GetFileName(_session.FilePath)})" : "");

        /// <summary>The mod's #new... on the game's own numbers (they replace the game's entities in game), or null.</summary>
        public string? GameNumbersText
        {
            get
            {
                var on = Rows.Where(r => r.Survey.OnGameNumbers.Count > 0).ToList();
                if (on.Count == 0)
                    return null;
                return "Also move the mod's own " + string.Join("; ", on.Select(r =>
                    $"{r.Survey.Kind}{(r.Survey.OnGameNumbers.Count == 1 ? "" : "s")} on the game's numbers ({string.Join(", ", r.Survey.OnGameNumbers.Select(e => $"{e.ID} {e.Name}"))})"))
                    + ". In game each replaces the game's own entity with that number; moved, the game has its own back";
            }
        }

        public bool HasGameNumbers => GameNumbersText != null;

        public bool GameNumbers
        {
            get => _gameNumbers;
            set
            {
                _gameNumbers = value;
                Changed();
                Update();
            }
        }

        /// <summary>Saving over a workshop file is undone by Steam's next update: a note to Save As elsewhere.</summary>
        public string? WorkshopNote => _session.FilePath != null && MergeViewModel.InWorkshop(_session.FilePath)
            ? "This mod's file is in Steam's workshop folder, which Steam replaces when the mod updates: after renumbering, use Save As to keep it in your Dominions 6 mods folder"
            : null;

        /// <summary>Why the plan can't be made (nothing moves while there's one).</summary>
        public IReadOnlyList<string> Problems => _plan?.Problems ?? (IReadOnlyList<string>)Array.Empty<string>();

        /// <summary>What to know before moving: numbers skipped, units kept where they are, lines the editor doesn't read.</summary>
        public IReadOnlyList<string> Notes => _plan?.Notes ?? (IReadOnlyList<string>)Array.Empty<string>();

        public bool HasProblems => Problems.Count > 0;
        public bool HasNotes => Notes.Count > 0;

        /// <summary>What the move comes to, or what's missing.</summary>
        public string Summary
        {
            get
            {
                if (_result != null)
                    return _result;
                if (Rows.All(r => r.StartNumber == null))
                    return Rows.Count == 0 ? "This mod numbers nothing itself: nothing to renumber" : "Give a start number for each type to move (empty: it stays)";
                if (Rows.Any(r => r.Error != null))
                    return "A start number isn't a number";
                if (_plan == null || _plan.Problems.Count > 0)
                    return "Nothing can move yet (see below)";
                if (_plan.Moves.Count == 0)
                    return "Everything is where these start numbers put it already";
                return $"{_plan.Moves.Count} number{(_plan.Moves.Count == 1 ? "" : "s")} change; every line that refers to one follows. One edit: Undo puts it back, and the mod is saved as usual (with a backup first)";
            }
        }

        public bool CanApply => _result == null && _plan?.CanApply == true && Rows.All(r => r.Error == null);

        /// <summary>Whether the move was made (the window then only closes).</summary>
        public bool IsDone => _result != null;

        /// <summary>The plan again from the start numbers (after one changed).</summary>
        internal void Update()
        {
            var starts = Rows.Where(r => r.StartNumber != null).ToDictionary(r => r.Survey.Type, r => r.StartNumber!.Value);
            _plan = starts.Count > 0 && Rows.All(r => r.Error == null) ? ModRenumber.Plan(_context, starts, _gameNumbers) : null;
            foreach (var row in Rows)
                row.Show(_plan?.Blocks.FirstOrDefault(b => b.Type == row.Survey.Type));
            Changed(nameof(Problems));
            Changed(nameof(Notes));
            Changed(nameof(HasProblems));
            Changed(nameof(HasNotes));
            Changed(nameof(Summary));
            Changed(nameof(CanApply));
        }

        /// <summary>Makes the move (one edit); null when done, else why not (nothing changed then).</summary>
        public string? Apply()
        {
            if (!CanApply || _plan == null)
                return Summary;
            var plan = _plan;
            var error = _session.Edit(ed => ed.Renumber(plan));
            if (error != null)
                return error;
            _result = $"Renumbered: {plan.Moves.Count} number{(plan.Moves.Count == 1 ? "" : "s")} changed. Save to keep it (Ctrl+S; the file as it was goes to the backups first); Undo puts it back";
            Changed(nameof(Summary));
            Changed(nameof(CanApply));
            Changed(nameof(IsDone));
            return null;
        }

        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>One type in "Renumber...": its numbers, the start number typed, and where they'd go.</summary>
    public sealed class RenumberRow : INotifyPropertyChanged
    {
        private readonly RenumberViewModel _owner;
        private string _start = "";
        private string _newRange = "";

        internal RenumberRow(RenumberViewModel owner, RenumberType survey)
        {
            _owner = owner;
            Survey = survey;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public RenumberType Survey { get; }

        public string Title => Survey.Type switch
        {
            EntityType.MONSTER => "Monsters",
            EntityType.WEAPON => "Weapons",
            EntityType.ARMOR => "Armor",
            EntityType.SITE => "Sites",
            _ => Survey.Kind,
        };

        /// <summary>How many the mod numbers itself ("19 +2": and its own on the game's numbers, moved when ticked).</summary>
        public string CountText => Survey.Count + (Survey.OnGameNumbers.Count > 0 ? $" +{Survey.OnGameNumbers.Count}" : "");

        public string RangeText => Survey.Count == 0 ? "-" : Survey.Lowest == Survey.Highest ? $"{Survey.Lowest}" : $"{Survey.Lowest}-{Survey.Highest}";

        public string Tip => $"The game's own {Survey.Kind}s are numbered up to {Survey.GameLast}; the last {Survey.Kind} number is {Survey.Last}. " +
                             $"The {Survey.Count} the mod numbers itself move to a block from the start number, in their order" +
                             (Survey.OnGameNumbers.Count > 0 ? $"; +{Survey.OnGameNumbers.Count}: the mod's own on the game's numbers, moved too when ticked below" : "");

        /// <summary>The start number as typed (empty: the type doesn't move).</summary>
        public string Start
        {
            get => _start;
            set
            {
                _start = value ?? "";
                Changed();
                Changed(nameof(Error));
                _owner.Update();
            }
        }

        public int? StartNumber => int.TryParse(_start.Trim(), out var n) ? n : null;

        public string? Error => _start.Trim().Length > 0 && StartNumber == null ? "not a number" : null;

        /// <summary>Where they'd go ("15000-15202"), or empty.</summary>
        public string NewRange
        {
            get => _newRange;
            private set { _newRange = value; Changed(); }
        }

        internal void Show(RenumberBlock? block) =>
            NewRange = block == null ? "" : block.Count == 1 ? $"{block.NewLowest}" : $"{block.NewLowest}-{block.NewHighest}";

        private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
