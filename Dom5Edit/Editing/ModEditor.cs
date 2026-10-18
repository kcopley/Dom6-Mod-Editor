using Dom5Edit.Commands;
using Dom5Edit.Entities;
using Dom5Edit.GameData;
using Dom5Edit.Props;
using Dom5Edit.Resolve;

namespace Dom5Edit.Editing
{
    /// <summary>An edit that can't be made as asked (the message says why, for the user).</summary>
    public class EditException : Exception
    {
        public EditException(string message) : base(message) { }
    }

    /// <summary>
    /// Every change the editor makes to a mod, as undoable edits (docs/EDIT_FLOW.md). What an edit
    /// does depends on where the value comes from (ResolvedValue.Source):
    /// - the entity's own line: changed or removed in place;
    /// - vanilla, a copy source, or nothing: a line is added to the mod's entity for it, made on
    ///   the first edit of a vanilla entity (copy-on-write: vanilla data is never changed);
    /// - removing an inherited value: "#x 0" for an ability the game removes on 0, else the
    ///   group's clear command plus the rest of the group added back (all the game offers).
    /// Where added lines are saved is SavePlan's job; the resolver then shows the result.
    /// </summary>
    public sealed class ModEditor
    {
        public ModEditor(Mod mod)
        {
            Mod = mod;
        }

        /// <summary>Why a vanilla mercenary can't be edited (tools/dom6exe: the merc parser has no #select).</summary>
        public const string VanillaMercenaryNote =
            "The game's own band: a mod can't change it (there is no #selectmerc). #clearmercs removes all the game's bands; #newmerc adds a band.";

        public Mod Mod { get; }

        public ModResolver Resolver => ModResolver.For(Mod);

        /// <summary>Raised after an edit is made, undone or redone (the resolver has been reset).</summary>
        public event Action<IModEdit>? Changed;

        public ResolvedEntity Resolve(IDEntity entity) => Resolver.Resolve(entity);

        /// <summary>The mod's own entity for this one (itself, or the mod's #select of a vanilla one), or null.</summary>
        public IDEntity? OwnEntity(IDEntity entity)
        {
            if (entity.ParentMod == Mod)
                return entity;
            var type = entity.GetEntityType();
            if (!Mod.Database.TryGetValue(type, out var set))
                return null;
            if (set.TryGet(entity.ID, null, out var own))
                return own;
            // number 0 (nation 0, bless 0, event 0) isn't kept by number: the mod's #select of it;
            // and a #select by name ("#selectbless \"Fear\"") is linked to the game's entity
            return set.Unnumbered.FirstOrDefault(e => e.Selected && (entity.ID == 0 && entity.Selected && e.ID == 0
                                                                     || ReferenceEquals(e.DependentEntity, entity)));
        }

        /// <summary>
        /// Why the mod can't change an entity it doesn't hold yet, or null if it can (by a #select
        /// of it): the game's mercenaries have no #select, and a needed mod's new events and bands
        /// have no number to select them by.
        /// </summary>
        public string? CannotChange(IDEntity entity)
        {
            if (entity.ParentMod == Mod || OwnEntity(entity) != null)
                return null;
            var owner = entity.ParentMod;
            bool needed = owner != null && owner.Dependencies.Count > 0;
            string name = needed ? owner!.DisplayName : "";
            if (entity.GetEntityType() == EntityType.MERCENARY)
                return needed ? $"A band of {name}, a mod this one needs: a mod over it can't change it (there is no #selectmerc)." : VanillaMercenaryNote;
            if (entity.ID < 0 && needed)
                return $"Made by {name}, a mod this one needs: a mod over it can't change it (it has no number to #select it by).";
            return null;
        }

        /// <summary>
        /// Runs several changes as one edit (one undo step). Returns null if nothing changed. If the
        /// body throws, everything it did is undone and the exception passes on.
        /// </summary>
        public IModEdit? Run(string description, Action<Transaction> body)
        {
            var tx = new Transaction(this);
            try
            {
                body(tx);
            }
            catch
            {
                tx.Rollback();
                Resolver.Invalidate();
                throw;
            }
            var edit = tx.Commit(description);
            Resolver.Invalidate();
            if (edit != null)
                Changed?.Invoke(edit);
            return edit;
        }

        internal void OnUndoRedo(IModEdit edit)
        {
            Resolver.Invalidate();
            Changed?.Invoke(edit);
        }

        public IModEdit? Set(IDEntity entity, Command c, string args) =>
            Run($"Set {Name(c)} {args}".TrimEnd(), tx => tx.Set(entity, c, args));

        public IModEdit? Add(IDEntity entity, Command c, string args) =>
            Run($"Add {Name(c)} {args}".TrimEnd(), tx => tx.Add(entity, c, args));

        public IModEdit? Change(IDEntity entity, ResolvedValue value, string args) =>
            Run($"Change {Name(value.Command)} to {args}", tx => tx.Change(entity, value, args));

        public IModEdit? Remove(IDEntity entity, ResolvedValue value) =>
            Run($"Remove {Name(value.Command)} {value.Arguments}".TrimEnd(), tx => tx.Remove(entity, value));

        public IModEdit? Reset(IDEntity entity, Command c) =>
            Run($"Reset {Name(c)}", tx => tx.Reset(entity, c));

        /// <summary>
        /// The entity's block as text, the way the editor shows it ("in the file"): its #new/#select
        /// line, its lines as saved, #end. The mod's single block, or for an entity the mod doesn't
        /// have yet, the #select line it would get. Null when the mod writes it in several blocks.
        /// </summary>
        public string? BlockText(IDEntity entity)
        {
            var own = OwnEntity(entity);
            if (own == null)
            {
                CommandsMap.TryGetString(entity.GetSelectCommand(), out var select);
                return entity.ID >= 0 ? $"{select} {entity.ID}\n#end" : null;
            }
            var plan = new SavePlan(Mod);
            if (plan.HasBlocks(own) && plan.BlocksOf(own).Count != 1)
                return null;
            var lines = ModExporter.EntityLines(Mod, own);
            return lines.Count > 0 ? string.Join("\n", lines) : null;
        }

        /// <summary>The entity's lines replaced by this text, as in the file (Transaction.ReplaceText).</summary>
        public IModEdit? ReplaceText(IDEntity entity, string text) =>
            Run("Edit the lines as text", tx => tx.ReplaceText(entity, text));

        public IModEdit? SetFlag(IDEntity entity, Command c, bool on) =>
            Run($"{(on ? "Set" : "Remove")} {Name(c)}", tx => tx.SetFlag(entity, c, on));

        public IModEdit Create(EntityType type, string? name, out IDEntity created)
        {
            IDEntity? made = null;
            var edit = Run($"New {type.ToString().ToLowerInvariant()}", tx => made = tx.Create(type, name))!;
            created = made!;
            return edit;
        }

        /// <summary>Moves one of an entity's own lines up (-1) or down (+1); see Transaction.MoveLine.</summary>
        public IModEdit? MoveLine(IDEntity entity, Property line, int delta) =>
            Run($"Move {Name(line.Command)} {(delta < 0 ? "up" : "down")}", tx => tx.MoveLine(entity, line, delta));

        /// <summary>Moves one of an entity's own lines to just before (or after) another.</summary>
        public IModEdit? MoveLine(IDEntity entity, Property line, Property target, bool after) =>
            Run($"Move {Name(line.Command)}", tx => tx.MoveLine(entity, line, target, after));

        public IModEdit? Delete(IDEntity entity) =>
            Run($"Delete {entity.GetEntityType().ToString().ToLowerInvariant()} {entity.ID}", tx => tx.Delete(entity));

        /// <summary>Changes a header field: #modname, #description, #icon, #version or #domversion.</summary>
        public IModEdit? SetModInfo(Command field, string? value)
        {
            Func<string?> get;
            Action<string?> set;
            switch (field)
            {
                case Command.MODNAME: get = () => Mod.ModName; set = v => Mod.ModName = v!; break;
                case Command.DESCRIPTION: get = () => Mod.Description; set = v => Mod.Description = v!; break;
                case Command.ICON: get = () => Mod.Icon; set = v => Mod.Icon = v!; break;
                case Command.VERSION: get = () => Mod.Version; set = v => Mod.Version = v!; break;
                case Command.DOMVERSION: get = () => Mod.DomVersion; set = v => Mod.DomVersion = v!; break;
                default: throw new EditException($"{Name(field)} isn't a mod header field");
            }
            var before = get();
            if (before == value)
                return null;
            set(value);
            var edit = new ModInfoEdit($"Set {Name(field)}", set, before, value, OnUndoRedo);
            Changed?.Invoke(edit);
            return edit;
        }

        /// <summary>
        /// The Renumber tool (Merge.ModRenumber): the plan's entities to their new numbers, every
        /// reference following; one edit. Throws an EditException, with nothing changed, if the
        /// plan has a problem or a name would find another entity afterwards.
        /// </summary>
        public IModEdit Renumber(Merge.RenumberPlan plan)
        {
            var edit = RenumberEdit.Apply(Mod, plan, OnUndoRedo);
            Resolver.Invalidate();
            Changed?.Invoke(edit);
            return edit;
        }

        internal static string Name(Command c) => CommandsMap.TryGetString(c, out var s) ? s : c.ToString();
    }

    /// <summary>
    /// The changes of one edit, made one by one on the model (each sees the previous ones), and
    /// recorded so the whole edit undoes as one.
    /// </summary>
    public sealed class Transaction
    {
        private readonly ModEditor _editor;
        private readonly Dictionary<IDEntity, (List<Property> Lines, string Name)> _before = new(ReferenceEqualityComparer.Instance);
        private readonly List<IDEntity> _created = new List<IDEntity>();
        private readonly List<IDEntity> _deleted = new List<IDEntity>();

        internal Transaction(ModEditor editor)
        {
            _editor = editor;
        }

        private Mod Mod => _editor.Mod;

        /// <summary>What the entity is now, with this transaction's changes so far.</summary>
        public ResolvedEntity Resolve(IDEntity entity) => _editor.Resolver.Resolve(entity);

        // ---- primitives ----

        /// <summary>
        /// The mod's entity for this one, made if only vanilla has it (a #select in the mod, which
        /// starts as vanilla and holds the changes). Vanilla data is never edited.
        /// </summary>
        public IDEntity Editable(IDEntity entity)
        {
            var own = _editor.OwnEntity(entity);
            if (own != null)
                return own;
            // the game's mercenaries have no #select: a mod can only remove them all (#clearmercs);
            // a needed mod's new events and bands have no number to select them by
            if (_editor.CannotChange(entity) is string why)
                throw new EditException(why);
            var made = (IDEntity)Activator.CreateInstance(entity.GetType())!;
            made.Assign(entity.ID.ToString(), "", Mod, selected: true);
            made.Resolve();
            _created.Add(made);
            Changed();
            return made;
        }

        /// <summary>A new line for the entity, its arguments read as the game reads a line ("12", "4 3", "\"Spear\"", or "" for a flag).</summary>
        public Property Line(IDEntity entity, Command c, string args)
        {
            if (!entity.GetPropertyMap().TryGetValue(c, out var create))
                throw new EditException($"{ModEditor.Name(c)} isn't a {entity.GetEntityType().ToString().ToLowerInvariant()} command");
            var p = create();
            p.Parent = entity;
            args = (args ?? "").Trim();
            bool quoted = args.StartsWith("\"");
            var value = quoted ? args.Trim('"') : args;
            var was = Mod.LineWasTrimmed;
            Mod.LineWasTrimmed = quoted;
            try
            {
                p.Parse(c, value, "");
            }
            finally
            {
                Mod.LineWasTrimmed = was;
            }
            if (p is Reference r)
                r.Resolve();
            return p;
        }

        public void AddLine(IDEntity entity, Property p, bool placeFirst = false)
        {
            Touch(entity);
            p.PlaceFirst = placeFirst || GameRules.IsCopy(p.Command) || GameRules.IsClear(p.Command);
            entity.InsertLive(p);
            Changed();
        }

        public void RemoveLine(IDEntity entity, Property p)
        {
            Touch(entity);
            if (!entity.RemoveLive(p))
                throw new EditException($"{ModEditor.Name(p.Command)} isn't one of the entity's lines");
            Changed();
        }

        public void ReplaceLine(IDEntity entity, Property old, Property replacement)
        {
            Touch(entity);
            replacement.PlaceFirst = old.PlaceFirst;
            // a line in an order chain keeps its place (SavePlan)
            replacement.PlaceKey = old.PlaceKey;
            replacement.PlaceAfterKey = old.PlaceAfterKey;
            replacement.PlaceAtStart = old.PlaceAtStart;
            if (!entity.ReplaceLive(old, replacement))
                throw new EditException($"{ModEditor.Name(old.Command)} isn't one of the entity's lines");
            Changed();
        }

        // ---- operations (what the editor's controls do) ----

        /// <summary>
        /// Sets a single-valued command (for a keyed one, #magicskill, the value for that first
        /// argument): changes the entity's own line in place, else adds one that overrides the
        /// inherited value. On a repeatable command (#weapon) it adds an entry.
        /// </summary>
        public void Set(IDEntity entity, Command c, string args)
        {
            var type = entity.GetEntityType();
            var target = Editable(entity);
            var line = Line(target, c, args);
            if (GameRules.IsRepeatable(type, c))
            {
                AddLine(target, line);
                return;
            }
            var r = Resolve(target);
            var current = Current(r, line);
            if (current != null && r.IsEditableInPlace(current))
                ReplaceLine(target, current.Property, line);
            else
                AddLine(target, line);
        }

        /// <summary>Adds an entry (a weapon, a recruit, a random magic path); on a single-valued command, Set.</summary>
        public void Add(IDEntity entity, Command c, string args)
        {
            if (!GameRules.IsRepeatable(entity.GetEntityType(), c))
            {
                Set(entity, c, args);
                return;
            }
            var target = Editable(entity);
            AddLine(target, Line(target, c, args));
        }

        /// <summary>Turns a flag on or off (no change, and no entity made, if it already is).</summary>
        public void SetFlag(IDEntity entity, Command c, bool on)
        {
            var r = Resolve(entity);
            var current = r.Get(c);
            if (on == (current != null))
                return;
            if (on)
                Set(entity, c, "");
            else
                Remove(entity, current!);
        }

        /// <summary>Changes one value (one weapon of several, one magic path) to new arguments.</summary>
        public void Change(IDEntity entity, ResolvedValue value, string args)
        {
            var type = entity.GetEntityType();
            var target = Editable(entity);
            var r = Resolve(target);
            var current = Find(r, value);
            var line = Line(target, value.Command, args);
            if (r.IsEditableInPlace(current))
            {
                ReplaceLine(target, current.Property, line);
                return;
            }
            if (!GameRules.IsRepeatable(type, value.Command))
            {
                // an inherited value: a line of our own overrides it (for a keyed command with a new
                // first argument, the old entry has to go first)
                var replaced = new ResolvedValue(line, ValueSource.Own);
                if (GameRules.IsKeyedByFirstArgument(type, value.Command) && replaced.Selector != current.Selector)
                    RemoveInherited(target, current);
                AddLine(target, line);
                return;
            }
            RewriteGroup(target, current, line);
        }

        /// <summary>
        /// Removes a value, so the entity no longer has it in game: the entity's own line goes; an
        /// inherited value is removed the way the game allows (RemoveInherited). A base stat can't
        /// be removed (only changed); removing the entity's own line puts it back to what it inherits.
        /// </summary>
        public void Remove(IDEntity entity, ResolvedValue value)
        {
            var type = entity.GetEntityType();
            var target = Editable(entity);
            var r = Resolve(target);
            var current = Find(r, value);
            if (!r.IsEditableInPlace(current))
            {
                RemoveInherited(target, current);
                return;
            }
            RemoveLine(target, current.Property);
            if (GameRules.IsRepeatable(type, current.Command))
                return;
            // an inherited value the line overrode shows again: remove it too, if the game can
            var again = Current(Resolve(target), current.Property);
            if (again != null && again.Source != ValueSource.Own && CanRemoveInherited(type, again.Command))
                RemoveInherited(target, again);
        }

        /// <summary>Drops the entity's own lines for a command, so it has what it inherits again.</summary>
        public void Reset(IDEntity entity, Command c)
        {
            var own = _editor.OwnEntity(entity);
            if (own == null)
                return;
            foreach (var p in own.Properties.Where(p => p.Command == c).ToList())
                RemoveLine(own, p);
        }

        /// <summary>Where a line ends up (after taking it out at <paramref name="from"/>) to be just before the target, or after it (after = 1).</summary>
        private static int TargetIndex(List<Property> lines, int from, Property target, int after)
        {
            int t = lines.FindIndex(p => ReferenceEquals(p, target));
            if (t < 0)
                throw new EditException($"{ModEditor.Name(target.Command)} isn't one of the entity's lines");
            if (t > from)
                t--; // the list is one shorter after taking the line out
            return t + after;
        }

        /// <summary>
        /// Makes a new entity in the mod's ID range (an event gets none: #newevent takes no number).
        /// With <paramref name="placeAfter"/>, it's saved right after that entity (an event's
        /// delayed follow-up: the game takes the next event in the file).
        /// </summary>
        public IDEntity Create(EntityType type, string? name, IDEntity? placeAfter = null)
        {
            var set = Mod.Database[type];
            var made = (IDEntity)Activator.CreateInstance(Mod.TypeOf(type))!;
            // events and mercenaries take no number (#newevent, #newmerc); poptypes and nametypes
            // have no #new: a new one is a #select of a number the game doesn't use (the manual:
            // poptypes 1-249, the game's go to 106; nametypes 170-399 are free for mods). A nation
            // too: #newnation takes no number (the game gives it the first free one from 120), so
            // the mod's references to it couldn't name it; the manual's way is #selectnation 150+.
            // Spells and items the same (#newspell and #newitem take no number): the save writes them
            // as #selectspell / #selectitem (Spell.Export, Item.Export), so the page's "in the file" box should too
            bool selectOnly = type == EntityType.POPTYPE || type == EntityType.NAMETYPE || type == EntityType.NATION
                              || type == EntityType.SPELL || type == EntityType.ITEM;
            if (type == EntityType.BLESS || type == EntityType.TEMPLATE)
                throw new EditException(type == EntityType.BLESS ? "Blesses can only be changed (#selectbless), not made"
                    : "A template is made for a nation (#newtemplate <nation>): add it in the file");
            string id = type == EntityType.EVENT || type == EntityType.MERCENARY ? ""
                : type == EntityType.POPTYPE ? Enumerable.Range(150, 100).First(n => !set.TryGetValue(n, out _)).ToString()
                : set.NextFreeID().ToString();
            made.Assign(id, "", Mod, selected: selectOnly);
            if (placeAfter != null)
                made.PlacedAfter = _editor.OwnEntity(placeAfter) ?? placeAfter;
            _created.Add(made);
            Changed();
            if (!string.IsNullOrEmpty(name) && made.GetPropertyMap().ContainsKey(Command.NAME))
                AddLine(made, Line(made, Command.NAME, "\"" + name + "\""));
            return made;
        }

        /// <summary>
        /// Moves one of the entity's own lines up (delta -1) or down (+1) among its lines as saved;
        /// order matters in events (#tempunits, #assowner, #cleartarg act on the lines after them).
        /// A parsed block's lines are rewritten as a chain in the new order (copies keep their
        /// text and comments); an entity made in the editor just reorders its list.
        /// </summary>
        public void MoveLine(IDEntity entity, Property line, int delta) => MoveLine(entity, line, null, delta);

        /// <summary>Moves one of the entity's own lines to just before (or after) another of its lines.</summary>
        public void MoveLine(IDEntity entity, Property line, Property target, bool after) => MoveLine(entity, line, target, after ? 1 : 0);

        private void MoveLine(IDEntity entity, Property line, Property? target, int delta)
        {
            var own = _editor.OwnEntity(entity) ?? throw new EditException("Not one of the mod's lines");
            var plan = new SavePlan(Mod);
            var blocks = plan.BlocksOf(own);
            if (!plan.HasBlocks(own))
            {
                var list = own.Properties.ToList();
                int at = list.FindIndex(p => ReferenceEquals(p, line));
                if (at < 0)
                    throw new EditException($"{ModEditor.Name(line.Command)} isn't one of the entity's lines");
                int to = Math.Clamp(target == null ? at + delta : TargetIndex(list, at, target, delta), 0, own.Properties.Count - 1);
                if (to == at)
                    return;
                Touch(own);
                own.MoveLive(line, to);
                Changed();
                return;
            }
            if (blocks.Count != 1)
                throw new EditException("This entity is in several blocks of the file; move its lines there");
            var lines = blocks[0].Block.Lines.Where(p => own.Properties.Any(q => ReferenceEquals(q, p))).ToList();
            int i = lines.FindIndex(p => ReferenceEquals(p, line));
            if (i < 0)
                throw new EditException($"{ModEditor.Name(line.Command)} isn't one of the entity's lines");
            int j = Math.Clamp(target == null ? i + delta : TargetIndex(lines, i, target, delta), 0, lines.Count - 1);
            if (j == i)
                return;
            lines.RemoveAt(i);
            lines.Insert(j, line);
            long previous = 0;
            foreach (var p in lines)
            {
                var copy = p.Clone();
                // the same line, moved: saved with its text as read while its value is unchanged
                copy.RawText = p.RawText;
                copy.BaselineExport = p.BaselineExport;
                copy.PlaceKey = Property.NewPlaceKey();
                copy.PlaceAtStart = previous == 0;
                copy.PlaceAfterKey = previous;
                previous = copy.PlaceKey;
                RemoveLine(own, p);
                AddLine(own, copy, p.PlaceFirst);
            }
        }

        /// <summary>The entity's block as text (ModEditor.BlockText).</summary>
        public string? BlockText(IDEntity entity) => _editor.BlockText(entity);

        /// <summary>
        /// Replaces the entity's lines with these, written as in the file (its #new/#select line,
        /// lines, #end; the first line can't change). Lines whose text is unchanged stay as they
        /// were (their original text and place); a changed line of the same command replaces the
        /// old one in place; new lines are added where they stand. Each line must be one the game
        /// reads for the type. Typed lines are saved as typed (comments too).
        /// </summary>
        public void ReplaceText(IDEntity entity, string text)
        {
            var current = BlockText(entity) ?? throw new EditException("The mod writes this entity in several blocks of the file: edit its lines on the page");
            var want = SplitLines(text);
            var have = SplitLines(current);
            if (want.Count < 2 || !want[^1].TrimStart().StartsWith("#end", StringComparison.OrdinalIgnoreCase))
                throw new EditException("The text must start with the entity's first line and end with #end");
            if (StripComment(want[0]) != StripComment(have[0]))
                throw new EditException($"The first line must stay \"{have[0]}\" (change the ID or name on the page)");
            var body = want.Skip(1).Take(want.Count - 2).Where(l => l.Trim().Length > 0).ToList();
            if (body.Count == 0 && _editor.OwnEntity(entity) == null)
                return; // nothing for a game entity the mod doesn't have: no empty #select block

            var target = Editable(entity);
            var plan = new SavePlan(Mod);
            var old = plan.HasBlocks(target) && plan.BlocksOf(target).Count == 1
                ? plan.BlocksOf(target)[0].Block.Lines.Where(p => target.Properties.Any(q => ReferenceEquals(q, p))).ToList()
                : target.Properties.ToList();
            // the typed text has "\n" line ends (SplitLines); a line's text as read keeps the file's
            // ("\r\n" inside a #msg over several lines): the same line either way, kept as read
            string Text(Property p) => (Mod.KeepOriginalText ? p.SaveText() : p.ToExportString()).Replace("\r\n", "\n").Trim();

            // the longest run of unchanged lines (by text), in order
            int n = old.Count, m = body.Count;
            var lcs = new int[n + 1, m + 1];
            for (int i = n - 1; i >= 0; i--)
                for (int j = m - 1; j >= 0; j--)
                    lcs[i, j] = Text(old[i]) == body[j].Trim() ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            var final = new List<Property>();
            var removed = new List<Property>();
            var added = new List<Property>();
            int a = 0, b = 0;
            while (a < n || b < m)
            {
                if (a < n && b < m && Text(old[a]) == body[b].Trim())
                {
                    final.Add(old[a++]);
                    b++;
                }
                else if (a < n && (b == m || lcs[a + 1, b] >= lcs[a, b + 1]))
                    removed.Add(old[a++]);
                else
                {
                    var line = TypedLine(target, body[b++]);
                    // a changed line: the old one of the same command just before it is replaced
                    int r = removed.FindLastIndex(p => p.Command == line.Command);
                    if (r >= 0 && r == removed.Count - 1)
                    {
                        ReplaceLine(target, removed[r], line);
                        removed.RemoveAt(r);
                    }
                    else
                        added.Add(line);
                    final.Add(line);
                }
            }
            foreach (var p in removed)
                RemoveLine(target, p);
            if (added.Count == 0)
                return;
            // new lines only at the end: added there; else the block's order is saved as a chain
            bool IsAdded(Property p) => added.Any(q => ReferenceEquals(q, p));
            int firstNew = final.FindIndex(IsAdded);
            if (final.Skip(firstNew).All(IsAdded))
            {
                foreach (var p in added)
                    AddLine(target, p);
                return;
            }
            foreach (var p in added)
                AddLine(target, p);
            if (!new SavePlan(Mod).HasBlocks(target))
            {
                for (int i = 0; i < final.Count; i++)
                    target.MoveLive(final[i], i);
                return;
            }
            long previous = 0;
            foreach (var p in final)
            {
                var copy = p.Clone();
                copy.RawText = p.RawText;
                copy.BaselineExport = p.BaselineExport;
                copy.PlaceKey = Property.NewPlaceKey();
                copy.PlaceAtStart = previous == 0;
                copy.PlaceAfterKey = previous;
                previous = copy.PlaceKey;
                RemoveLine(target, p);
                AddLine(target, copy, p.PlaceFirst);
            }
        }

        /// <summary>One typed line as a line of the entity: "#hp 20 -- note" (saved as typed while its value is).</summary>
        private Property TypedLine(IDEntity entity, string typed)
        {
            var text = typed.Trim();
            var code = StripComment(text);
            if (!code.StartsWith("#"))
                throw new EditException($"\"{text}\" isn't a command line: lines start with #, and a comment goes after a line (#hp 20 -- note)");
            int space = code.IndexOfAny(new[] { ' ', '\t' });
            var name = space < 0 ? code : code.Substring(0, space);
            var args = space < 0 ? "" : code.Substring(space + 1).Trim();
            if (!CommandsMap.TryGetCommand(name, out var c))
                throw new EditException($"{name} isn't a command the editor knows");
            var line = Line(entity, c, args);
            line.RawText = text;
            line.BaselineExport = line.ToExportString();
            return line;
        }

        /// <summary>A line without its comment ("--" outside quotes), trimmed.</summary>
        private static string StripComment(string line)
        {
            bool quoted = false;
            for (int i = 0; i + 1 < line.Length; i++)
            {
                if (line[i] == '"')
                    quoted = !quoted;
                else if (!quoted && line[i] == '-' && line[i + 1] == '-')
                    return line.Substring(0, i).Trim();
            }
            return line.Trim();
        }

        /// <summary>The text's lines; a quoted text running over several lines (#descr) is one line.</summary>
        private static List<string> SplitLines(string text)
        {
            var lines = new List<string>();
            string? open = null;
            foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                var line = open == null ? raw : open + "\n" + raw;
                int quotes = StripComment(line).Count(ch => ch == '"');
                if (quotes % 2 == 1)
                {
                    open = line;
                    continue;
                }
                open = null;
                lines.Add(line);
            }
            if (open != null)
                lines.Add(open);
            return lines.Where((l, i) => l.Trim().Length > 0 || (i > 0 && i < lines.Count - 1)).ToList();
        }

        /// <summary>Deletes one of the mod's entities (for a #select of a vanilla one: drops the mod's changes to it).</summary>
        public void Delete(IDEntity entity)
        {
            var own = _editor.OwnEntity(entity) ?? throw new EditException("Vanilla data can't be deleted");
            Mod.Database[own.GetEntityType()].Remove(own);
            if (_created.Remove(own))
                return;
            _deleted.Add(own);
            Changed();
        }

        // ---- removing inherited values ----

        /// <summary>Whether the game offers a way to remove this inherited value (else it can only be changed).</summary>
        public static bool CanRemoveInherited(EntityType type, Command c) =>
            GameRules.RemovesWithZero(type, c) || GroupClear(type, c) != null;

        /// <summary>The clear command that removes this value's group (#clearweapons, #clearspec; #clear for the simple types), or null.</summary>
        private static Command? GroupClear(EntityType type, Command c)
        {
            var group = GameRules.GroupOf(type, c);
            var clear = GameRules.ClearCommandFor(type, group);
            if (clear != null)
                return clear;
            // weapons, armor, items, spells, sites: no groups, only #clear (everything but the name)
            bool simple = type == EntityType.WEAPON || type == EntityType.ARMOR || type == EntityType.ITEM
                          || type == EntityType.SPELL || type == EntityType.SITE;
            return simple && GameRules.Clears(type, Command.CLEAR, c) ? Command.CLEAR : null;
        }

        /// <summary>Removes an inherited value: "#x 0", or the group's clear and the rest of the group added back.</summary>
        public void RemoveInherited(IDEntity target, ResolvedValue value)
        {
            var type = target.GetEntityType();
            if (GameRules.RemovesWithZero(type, value.Command))
            {
                AddLine(target, Line(target, value.Command, "0"));
                return;
            }
            RewriteGroup(target, value, null);
        }

        /// <summary>
        /// Clears the value's group and adds back every other value in it (with the one value
        /// replaced, or left out): the only way to drop or change one inherited entry of a list.
        /// The entity's own lines after the clear stay as they are.
        /// </summary>
        private void RewriteGroup(IDEntity target, ResolvedValue value, Property? replacement)
        {
            var type = target.GetEntityType();
            var clear = GroupClear(type, value.Command)
                        ?? throw new EditException($"{ModEditor.Name(value.Command)} can't be removed, only changed");
            var r = Resolve(target);
            var members = r.Values.Where(v => GameRules.Clears(type, clear, v.Command)).ToList();
            AddLine(target, Line(target, clear, ""), placeFirst: true);
            foreach (var v in members)
            {
                if (ReferenceEquals(v.Property, value.Property))
                {
                    if (replacement != null)
                        AddLine(target, replacement, placeFirst: !r.IsEditableInPlace(v));
                    if (r.IsEditableInPlace(v))
                        RemoveLine(target, v.Property);
                    continue;
                }
                if (!r.IsEditableInPlace(v))
                    AddLine(target, Line(target, v.Command, v.Arguments), placeFirst: true);
            }
            // a line added back can also set a flag bit outside the group that an inherited line
            // cleared (#undead sets the almost-undead bit, vanilla's #almostliving after it clears
            // it, and #clearspec doesn't touch it): that line goes back in after it
            var setAgain = new HashSet<string>(members.Where(v => !ReferenceEquals(v.Property, value.Property) && !r.IsEditableInPlace(v))
                .SelectMany(v => GameCommandCatalog.EffectOf(type, v.Command)?.Bits ?? Enumerable.Empty<string>()));
            if (replacement != null)
                setAgain.UnionWith(GameCommandCatalog.EffectOf(type, replacement.Command)?.Bits ?? Enumerable.Empty<string>());
            foreach (var v in r.Values)
                if (v.Source != ValueSource.Own && !GameRules.Clears(type, clear, v.Command)
                    && GameCommandCatalog.EffectOf(type, v.Command)?.Clears is { Count: > 0 } clears && clears.Any(setAgain.Contains))
                    AddLine(target, Line(target, v.Command, v.Arguments), placeFirst: true);
        }

        // ---- helpers ----

        /// <summary>The value a line of this command would replace: the last one (for a keyed command, with the same first argument).</summary>
        private static ResolvedValue? Current(ResolvedEntity r, Property line)
        {
            if (!GameRules.IsKeyedByFirstArgument(line.Parent?.Kind, line.Command))
                return r.Get(line.Command);
            var selector = new ResolvedValue(line, ValueSource.Own).Selector;
            return r.GetAll(line.Command).LastOrDefault(v => v.Selector == selector);
        }

        /// <summary>The same value in a fresh resolve (by its line).</summary>
        private static ResolvedValue Find(ResolvedEntity r, ResolvedValue value) =>
            r.Values.FirstOrDefault(v => ReferenceEquals(v.Property, value.Property))
            ?? throw new EditException($"{ModEditor.Name(value.Command)} {value.Arguments} isn't one of the entity's values any more");

        private void Touch(IDEntity entity)
        {
            if (!_before.ContainsKey(entity))
                _before[entity] = (entity.SnapshotProperties(), entity._name);
        }

        private void Changed() => _editor.Resolver.Invalidate();

        internal void Rollback()
        {
            foreach (var (entity, (lines, name)) in _before)
            {
                entity.RestoreProperties(lines);
                entity._name = name;
            }
            foreach (var e in _created)
                Mod.Database[e.GetEntityType()].Remove(e);
            foreach (var e in _deleted)
                Mod.Database[e.GetEntityType()].Restore(e);
        }

        internal IModEdit? Commit(string description)
        {
            var states = new Dictionary<IDEntity, SnapshotEdit.EntityState>(ReferenceEqualityComparer.Instance);
            foreach (var (entity, (lines, name)) in _before)
            {
                var now = entity.SnapshotProperties();
                if (now.Count == lines.Count && now.Zip(lines).All(x => ReferenceEquals(x.First, x.Second)) && entity._name == name)
                    continue;
                states[entity] = new SnapshotEdit.EntityState { Before = lines, After = now, NameBefore = name, NameAfter = entity._name };
            }
            if (states.Count == 0 && _created.Count == 0 && _deleted.Count == 0)
                return null;
            return new SnapshotEdit(Mod, description, states, _created.ToList(), _deleted.ToList(), _editor.OnUndoRedo);
        }
    }
}
