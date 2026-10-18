using Dom5Edit.Commands;
using Dom5Edit.Entities;

namespace Dom5Edit.Props
{
    public class StringOrIDRef : Reference
    {
        public bool IsStringRef { get; set; } = false;

        private int _id;
        public int ID
        {
            get
            {
                if (Entity != null)
                {
                    return Entity.ID;
                }
                else return _id;
            }
            set
            {
                _id = value;
                _nameWhenResolved = null;
                // Setting an ID means we have a value
                if (value != 0)
                {
                    HasValue = true;
                    IsStringRef = false;
                }
                if (Parent?.ParentMod?.IsLoaded == true)
                {
                    Resolve();
                }
            }
        }
        private string _name;
        public string Name
        {
            get
            {
                if (Entity != null)
                {
                    return Entity.Name;
                }
                else return _name;
            }
            set
            {
                _name = value;
                _nameWhenResolved = null;
                if (Parent?.ParentMod?.IsLoaded == true)
                {
                    Resolve();
                }
            }
        }

        public bool HasValue { get; set; }

        public IDEntity Entity { get; set; }
        public bool Resolved { get; set; }

        internal override EntityType GetEntityType()
        {
            throw new NotImplementedException();
        }

        public override void Resolve()
        {
            // Clear existing entity before resolving to ensure we look up the new ID
            // This is critical when re-resolving after an ID change via the UI
            Entity = null;
            Resolved = false;

            if (Parent.ParentMod.TryGet(GetEntityType(), _id, _name, out IDEntity e))
            {
                Entity = e;
                Resolved = true;
                _nameWhenResolved ??= e.Name;
            }
            //move start ID to be in an entitytype set
            //handle non-resolved separately... these are ones in a dependency?
            if (!Resolved && !IsStringRef && _id > Parent.ParentMod.GetStartID(GetEntityType()))
            {
                Parent.ParentMod.Log(GetEntityType() + " not resolved for: " + this._id);
            }
        }

        /// <summary>Points at another entity (a merge's snapshot of a game entity): written by its number.</summary>
        internal void Retarget(IDEntity target)
        {
            Entity = target;
            Resolved = true;
            _id = target.ID;
            IsStringRef = false;
            HasValue = true;
        }

        /// <summary>
        /// A number that finds nothing in its mod, moved to another that finds nothing (a merge:
        /// another part defines the first); the note says what it was.
        /// </summary>
        internal void Redirect(int id, string note)
        {
            _id = id;
            HasValue = true;
            IsStringRef = false;
            Entity = null;
            Resolved = false;
            Comment = string.IsNullOrEmpty(Comment) ? note : Comment + " " + note;
        }

        internal override void FollowTarget(IDEntity moved)
        {
            if (ReferenceEquals(Entity, moved) && !IsStringRef)
                _id = moved.ID;
        }

        /// <summary>The name as written in the file (null for a reference by number).</summary>
        internal string? WrittenName => IsStringRef ? _name : null;

        /// <summary>
        /// A name written with its number ("Militia (30)": the game's monster lookup takes that
        /// form) when that number moved (Merge.ModRenumber): the new one. Returns the name before,
        /// or null when it wasn't one.
        /// </summary>
        internal string? FollowNamedNumber(int old, int now)
        {
            if (!IsStringRef || string.IsNullOrEmpty(_name))
                return null;
            var m = System.Text.RegularExpressions.Regex.Match(_name, @"^(.*) \((\d+)\)$");
            if (!m.Success || m.Groups[2].Value != old.ToString())
                return null;
            var before = _name;
            _name = $"{m.Groups[1].Value} ({now})";
            return before;
        }

        /// <summary>Puts back a name <see cref="FollowNamedNumber"/> changed (an undone renumbering).</summary>
        internal void RestoreName(string name) => _name = name;

        public void SetEntity(string value)
        {
            Parse(this.Command, value, "");
            this.Resolve();
        }

        public override bool TryGetEntity(out IDEntity e)
        {
            e = null;
            if (!Resolved) return false;
            if (Entity != null)
            {
                e = Entity;
                return true;
            }
            return false;
        }

        public override void Parse(Command c, string s, string comment)
        {
            this.Command = c;
            this.Comment = comment;

            HasValue = s.TryRetrieveNumericFromString(out int val, out string remainder);

            if (HasValue && !(Parent?.ParentMod?.LineWasTrimmed ?? true))
            {
                ID = val;
                IsStringRef = false;
                Comment += remainder;
                // a number picked in the editor for a line the game reads only as a name: saved as the name
                _numberForName = NameOnly && Parent?.ParentMod?.IsLoaded == true;
            }
            else
            {
                HasValue = s.Length > 0;
                if (HasValue)
                {
                    Name = s;
                    IsStringRef = true;
                    _writtenAsName = true;
                }
            }
        }

        private static readonly List<Command> _StringExported = new List<Command>()
        {
            Command.STARTSITE,
            Command.SPELL,
            Command.AUTOSPELL,
        };

        /// <summary>
        /// A mercenary band's #com, #unit and #item: the game reads them only as a quoted name (its
        /// merc parser reads text there, Dominions6.exe 0x14023df70): "#com 5101" reads no name and
        /// the game stops loading the mod ("bad #com, no monster called ''").
        /// </summary>
        private bool NameOnly => Parent is Mercenary;

        // a number given in the editor where the game reads only a name (NameOnly): written as the name
        private bool _numberForName;

        // the author wrote a name (a merge writes the number, and the name as its comment)
        private bool _writtenAsName;

        // the target's name when this was first resolved: a different one at saving means it was
        // renamed in the editor since
        private string _nameWhenResolved;

        /// <summary>
        /// The name to write: as the author wrote it, so the game finds what it found before (a
        /// #copystats after #name gives the monster the copied name, and a reference by the first
        /// name then finds nothing in game, which a rewrite must not change); the target's new
        /// name when it was renamed in the editor; in a merge, the target's name.
        /// </summary>
        private string ExportName()
        {
            if (!Resolved || Entity == null)
                return _name;
            if (Parent?.ParentMod?.KeepReferenceForms == false)
                return Entity.TryGetName(out var merged) ? merged : _name;
            var now = Entity.Name;
            if (string.IsNullOrEmpty(_name) || (!string.IsNullOrEmpty(now) && _nameWhenResolved != null && now != _nameWhenResolved))
                return string.IsNullOrEmpty(now) ? _name : now;
            return _name;
        }

        /// <summary>
        /// The name to write. A mercenary's monster, "Name (id)" when the plain name would find
        /// another monster first: the merc parser's lookup (Dominions6.exe 0x1402b7720) tries
        /// "%s (%d)" against every monster, then the name ignoring case, lowest number first. A
        /// name the author wrote finds what it found before, so it stays as written.
        /// </summary>
        private string GameName()
        {
            var name = ExportName();
            if (!NameOnly || !Resolved || Entity == null || Entity.ID <= 0 || string.IsNullOrEmpty(name) || Entity.GetEntityType() != EntityType.MONSTER)
                return name;
            var mod = Parent?.ParentMod;
            if (mod == null || !mod.TryGet(EntityType.MONSTER, 0, name, out var found) || found.ID == Entity.ID)
                return name;
            return $"{name} ({Entity.ID})";
        }

        public override string ToExportString()
        {
            if (!CommandsMap.TryGetString(Command, out string s)) return "";

            // (a name the merge writes as a number stays as its comment, for whoever reads the file)
            string? nameNote = null;
            if (Parent?.ParentMod?.KeepReferenceForms == false)
            {
                if (_writtenAsName && Entity != null && Entity.ID != -1 && !_StringExported.Contains(Command) && !NameOnly && string.IsNullOrEmpty(Comment))
                    nameNote = ExportName();
                // a merge: by number where there is one
                if (Entity != null && Entity.ID != -1)
                    IsStringRef = false;

                if (_StringExported.Contains(Command) || NameOnly)
                {
                    IsStringRef = true;
                }
            }

            if (IsStringRef || _numberForName && Resolved && Entity != null)
            {
                string _exportName = GameName();

                if (!String.IsNullOrEmpty(Comment))
                {
                    if (HasValue)
                    {
                        return s + " \"" + _exportName + "\" -- " + Comment;
                    }
                    else
                    {
                        return s + " -- " + Comment;
                    }
                }
                else
                {
                    return s + " \"" + _exportName + "\"";
                }
            }
            else
            {
                int _exportID;
                if (Entity != null)
                    _exportID = Resolved ? Entity.ID : ID; //true is left, false is right
                else _exportID = ID;

                if (!String.IsNullOrEmpty(Comment))
                {
                    if (HasValue)
                    {
                        return s + " " + _exportID + " -- " + Comment;
                    }
                    else
                    {
                        return s + " -- " + Comment;
                    }
                }
                else if (!string.IsNullOrEmpty(nameNote) && HasValue)
                {
                    return s + " " + _exportID + " -- " + nameNote;
                }
                else
                {
                    return HasValue ? s + " " + _exportID : s; // no value given, none written
                }
            }
        }

        internal override bool EqualsProperty<T>(T copyFrom)
        {
            if (copyFrom is StringOrIDRef)
            {
                var compare = copyFrom as StringOrIDRef;
                if (this.Command == compare.Command && this.Entity == compare.Entity) return true;
            }
            return false;
        }
    }
}
