using Dom5Edit.Commands;
using Dom5Edit.Entities;

namespace Dom5Edit.Props
{
    public abstract class Property
    {
        public IDEntity Parent { get; set; }
        public string Comment { get; set; }

        /// <summary>
        /// An argument a property couldn't take as its values (fewer numbers than it holds:
        /// "#path -1", "#custommagic 200"): written back as read, so the game reads what it read.
        /// </summary>
        internal string? Unparsed { get; set; }

        /// <summary>The command with its unparsed argument as read (and the comment).</summary>
        internal string UnparsedExport(string command) =>
            command + " " + Unparsed + (string.IsNullOrEmpty(Comment) ? "" : " -- " + Comment);
        public int LineNumber { get; set; }
        public abstract void Parse(Command c, string v, string comment);

        public abstract string ToExportString();

        public Command Command { get; set; }

        /// <summary>The line's text as read from the file (null for a property made in the session).</summary>
        internal string? RawText { get; set; }

        /// <summary>When it was read from a line with several commands ("#stealthy 999 #inanimate"): that line.</summary>
        internal LineGroup? Line { get; set; }

        /// <summary>Whether it's as read: text from the file and its value unchanged since.</summary>
        internal bool IsAsRead => RawText != null && BaselineExport != null && ToExportString() == BaselineExport;

        /// <summary>
        /// ToExportString() when the mod was first resolved. While it still matches, the property
        /// is unedited and saving writes RawText (docs/SAVE_FLOW.md, "Original text").
        /// </summary>
        internal string? BaselineExport { get; set; }

        /// <summary>The text to save: the original line if unedited, else the regenerated one.</summary>
        /// <summary>
        /// Not a line of the data: a vanilla sprite or description the editor loaded from its own
        /// asset files to show (VanillaAssetLoader). The resolver lists these apart from values,
        /// so they're never copied into a mod as lines.
        /// </summary>
        public bool IsDisplayAsset { get; internal set; }

        /// <summary>
        /// An added copy or clear line (or a line re-added after one) that must take effect before the
        /// entity's own lines: saved before them rather than at the end of a block (SavePlan).
        /// </summary>
        internal bool PlaceFirst { get; set; }

        /// <summary>
        /// Lines put in order in the editor (Transaction.MoveLine; order matters in events: #tempunits,
        /// #assowner, #cleartarg act on the lines after them) form a chain: each has a key, and is
        /// saved right after the live line with key <see cref="PlaceAfterKey"/>, or first in the
        /// entity's block (<see cref="PlaceAtStart"/>). A line that replaces one keeps its keys.
        /// </summary>
        internal long PlaceKey { get; set; }

        /// <inheritdoc cref="PlaceKey"/>
        internal long PlaceAfterKey { get; set; }

        /// <inheritdoc cref="PlaceKey"/>
        internal bool PlaceAtStart { get; set; }

        private static long _placeKeys;

        /// <summary>A new key for a line's place in a chain.</summary>
        internal static long NewPlaceKey() => System.Threading.Interlocked.Increment(ref _placeKeys);

        internal string SaveText()
        {
            var text = ToExportString();
            return RawText != null && BaselineExport != null && text == BaselineExport ? RawText : text;
        }

        internal abstract Property GetDefault();

        internal abstract bool EqualsProperty<T>(T copyFrom) where T : Property, new();

        /// <summary>
        /// Returns a copy of this property. Used by the copy/inheritance materialization
        /// (see docs/COPY_INHERITANCE_REDESIGN.md) to freeze a snapshot of a copy source's
        /// values at copy time, independent of later edits to the source.
        ///
        /// MemberwiseClone is a faithful deep copy for the value-typed / string-valued
        /// properties Phase 1 bakes (IntProperty, NameProperty, etc.). Reference types may
        /// override if they hold mutable collections; the snapshot stores them but Phase 1
        /// does not bake references.
        /// </summary>
        internal virtual Property Clone()
        {
            var clone = (Property)this.MemberwiseClone();
            clone.RawText = null; // a clone is a new line, not the one read from the file
            clone.BaselineExport = null;
            return clone;
        }
    }

    /// <summary>
    /// A line of the file with several commands: its text as read, how many commands it had, and
    /// the properties read from it, in order. A save writes the line as it was while they are all
    /// still there, in order and unchanged (ModExporter).
    /// </summary>
    internal sealed class LineGroup
    {
        public LineGroup(string text, int commands)
        {
            Text = text;
            Commands = commands;
        }

        public string Text { get; internal set; } // (a renumbering rewrites its numbers: Editing.RenumberEdit)
        public int Commands { get; }
        public List<Property> Members { get; } = new List<Property>();
    }
}
