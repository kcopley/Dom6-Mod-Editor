using Dom5Edit.Commands;
using Dom5Edit.Props;
using Dom5Edit.Validation;

namespace Dom5Edit.Entities
{
    public class EntitySet<T> where T : IDEntity, new()
    {
        public Mod Parent { get; set; }
        public int START_ID { get; set; }
        public int END_ID { get; set; } = -1;

        private int VANILLA_END_ID { get; set; }
        private int CURRENT_ID { get; set; } = -1;
        private SortedDictionary<int, T> Entities = new SortedDictionary<int, T>();
        private List<T> UnIDdEntities = new List<T>();
        private SortedDictionary<int, T> DisabledEntities = new SortedDictionary<int, T>();

        public void Init()
        {
            VANILLA_END_ID = START_ID;
        }

        public bool TryGetValue(int i, out T entity)
        {
            //dependency code?
            //foreach (var m in Dependencies)
            //{
            //    if (m.Monsters.TryGetValue(i, out entity)) return true;
            //}
            if (i <= 0)
            {
                entity = null;
                return false;
            }

            if (Entities.TryGetValue(i, out entity)) return true;
            return false;
        }
        private Dictionary<string, T> NamedEntities = new Dictionary<string, T>(StringComparer.InvariantCultureIgnoreCase);
        private List<T> UnnamedEntities = new List<T>();
        public bool TryGetValueNamed(string i, out T entity)
        {
            //foreach (var m in Dependencies)
            //{
            //    if (m.NamedMonsters.TryGetValue(i, out entity)) return true;
            //}
            if (string.IsNullOrEmpty(i))
            {
                entity = null;
                return false;
            }

            if (NamedEntities.TryGetValue(i, out entity)) return true;
            return false;
        }

        /// <summary>
        /// Retrieves an ID'd or named entity from the dictionary. Checks ID first and name second. Use -1 for the ID to skip, or null on the string to skip.
        /// </summary>
        /// <param name="i">The ID of the entity.</param>
        /// <param name="s">The name of the entity.</param>
        /// <param name="entity">The entity returned from the dictionary.</param>
        /// <returns>True if either the ID or name is found; otherwise false.</returns>
        public bool TryGet(int i, string s, out T entity)
        {
            if (TryGetValue(i, out entity)) return true;
            return TryGetValueNamed(s, out entity);
        }

        public void Add(int id, string name, T t)
        {
            if (id > 0)
            {
                if (!Entities.ContainsKey(id))
                {
                    Entities.Add(id, t);
                }
                else
                {
                    var typeName = t.GetType().Name;
                    var message = $"Duplicate {typeName} ID {id} - this ID was already defined earlier in the mod";
                    Parent.Log(message);
                    Parent.AddParseIssue(ParseIssueType.DuplicateId, message);
                }
            }
            else
            {
                UnIDdEntities.Add(t);
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (!NamedEntities.TryGetValue(name, out T existing))
                {
                    NamedEntities.Add(name, t);
                }
                else
                {
                    var typeName = t.GetType().Name;
                    var message = $"Duplicate {typeName} name \"{name}\" - this name was already defined earlier in the mod";
                    Parent.Log(message);
                    Parent.AddParseIssue(ParseIssueType.DuplicateName, message);
                    if (IsLowerID(id, existing)) NamedEntities[name] = t;
                }
            }
            else
            {
                UnnamedEntities.Add(t);
            }
        }

        public void GiveName(IDEntity ie, string name)
        {
            if (!NamedEntities.TryGetValue(name, out T existing))
            {
                NamedEntities.Add(name, (T)ie);
            }
            else if (IsLowerID(ie.ID, existing))
            {
                NamedEntities[name] = (T)ie;
            }
            UnnamedEntities.Remove((T)ie);
        }

        /// <summary>
        /// The game resolves a name that several entities share to the lowest ID. An entity without
        /// an ID (yet) never displaces one that has one.
        /// </summary>
        internal static bool IsLowerID(int id, IDEntity existing)
        {
            return id > 0 && (existing.ID <= 0 || id < existing.ID);
        }

        public void GiveID(string name, int id)
        {
            if (NamedEntities.TryGetValue(name, out T t))
            {
                if (id > 0)
                {
                    if (!Entities.ContainsKey(id))
                    {
                        Entities.Add(id, t);
                    }
                }
            }
        }

        internal void Resolve()
        {
            foreach (var kvp in Entities)
            {
                kvp.Value.Resolve();
            }
            foreach (var e in UnIDdEntities)
            {
                e.Resolve();
            }
        }

        internal void Map()
        {
            foreach (var kvp in Entities)
            {
                kvp.Value.Map();
            }
            foreach (var e in UnIDdEntities)
            {
                e.Map();
            }
        }

        /// <summary>
        /// Writes each entity as one block: mod-range IDs, then vanilla-range, then entities with no
        /// ID. With a filter, only the entities it accepts (the original-order writer uses this for
        /// entities that have no block in the parsed file).
        /// </summary>
        internal void Export(StreamWriter writer, Func<T, bool>? include = null)
        {
            if (include != null)
            {
                foreach (var m in Entities.OrderBy(x => x.Key).Where(x => x.Key >= START_ID).Concat(
                             Entities.OrderBy(x => x.Key).Where(x => x.Key < START_ID)).Select(x => x.Value)
                             .Concat(UnIDdEntities).Where(include))
                {
                    if (m.ID < VANILLA_END_ID && m.ID != -1) m.Selected = true;
                    m.Export(writer);
                    writer.WriteLine();
                }
                return;
            }
            foreach (var m in DisabledEntities.OrderBy(x => x.Key).Where(x => x.Key >= START_ID))
            {
                if (m.Key < VANILLA_END_ID && m.Value.ID != -1) m.Value.Selected = true;
                m.Value.Export(writer);
                writer.WriteLine();
            }
            foreach (var m in DisabledEntities.OrderBy(x => x.Key).Where(x => x.Key < START_ID))
            {
                if (m.Key < VANILLA_END_ID && m.Value.ID != -1) m.Value.Selected = true;
                m.Value.Export(writer);
                writer.WriteLine();
            }
            foreach (var m in Entities.OrderBy(x => x.Key).Where(x => x.Key >= START_ID))
            {
                if (m.Key < VANILLA_END_ID && m.Value.ID != -1) m.Value.Selected = true;
                m.Value.Export(writer);
                writer.WriteLine();
            }
            foreach (var m in Entities.OrderBy(x => x.Key).Where(x => x.Key < START_ID))
            {
                if (m.Key < VANILLA_END_ID && m.Value.ID != -1) m.Value.Selected = true;
                m.Value.Export(writer);
                writer.WriteLine();
            }
            foreach (var m in UnIDdEntities)
            {
                if (m.ID < VANILLA_END_ID && m.ID != -1) m.Selected = true;
                m.Export(writer);
                writer.WriteLine();
            }
        }

        /// <summary>Takes an entity out of the set (deleted in the editor); <see cref="Restore"/> puts it back.</summary>
        internal void Remove(T t)
        {
            foreach (var kv in Entities.Where(kv => ReferenceEquals(kv.Value, t)).ToList())
                Entities.Remove(kv.Key);
            foreach (var kv in NamedEntities.Where(kv => ReferenceEquals(kv.Value, t)).ToList())
                NamedEntities.Remove(kv.Key);
            UnIDdEntities.RemoveAll(x => ReferenceEquals(x, t));
            UnnamedEntities.RemoveAll(x => ReferenceEquals(x, t));
        }

        /// <summary>Puts back an entity <see cref="Remove"/> took out (an undone delete or create).</summary>
        internal void Restore(T t)
        {
            Add(t.ID, t.Named ? t._name : null, t);
            if (t.TryGetName(out var name) && !string.IsNullOrEmpty(name))
                GiveName(t, name);
        }

        /// <summary>The next ID free in the mod's range for this type.</summary>
        internal int NextFreeID() => GetNextID();

        /// <summary>Moves an entity to another number in the set (Renumbering); its names stay.</summary>
        internal void Rekey(T entity, int oldId, int newId)
        {
            if (Entities.TryGetValue(oldId, out var held) && ReferenceEquals(held, entity))
                Entities.Remove(oldId);
            Entities[newId] = entity;
        }

        /// <summary>
        /// After entities moved to other numbers (Merge.ModRenumber): each name given goes to the
        /// lowest number that has it again (the game's rule for a name several share), among the
        /// entity it finds now and the ones given with it (every entity with a #name of it).
        /// </summary>
        internal void Rename(IEnumerable<(string Name, T Entity)> named)
        {
            foreach (var g in named.GroupBy(n => n.Name, StringComparer.InvariantCultureIgnoreCase))
            {
                NamedEntities.TryGetValue(g.Key, out var best);
                foreach (var (_, e) in g)
                    if (best == null || !ReferenceEquals(e, best) && IsLowerID(e.ID, best))
                        best = e;
                if (best != null)
                    NamedEntities[g.Key] = best;
            }
        }

        /// <summary>Puts back what a name found (an undone renumbering): <paramref name="entity"/>, or nothing.</summary>
        internal void SetNamed(string name, T? entity)
        {
            if (entity == null)
                NamedEntities.Remove(name);
            else
                NamedEntities[name] = entity;
        }

        /// <summary>Whether a number is taken in the set.</summary>
        internal bool Has(int id) => Entities.ContainsKey(id);

        /// <summary>The entities in the order Export writes them.</summary>
        internal IEnumerable<T> ExportOrder()
        {
            return DisabledEntities.OrderBy(x => x.Key).Where(x => x.Key >= START_ID)
                .Concat(DisabledEntities.OrderBy(x => x.Key).Where(x => x.Key < START_ID))
                .Concat(Entities.OrderBy(x => x.Key).Where(x => x.Key >= START_ID))
                .Concat(Entities.OrderBy(x => x.Key).Where(x => x.Key < START_ID))
                .Select(x => x.Value).Concat(UnIDdEntities);
        }

        /// <summary>Whether entities were disabled (DisableMages; a merge feature).</summary>
        internal bool HasDisabled => DisabledEntities.Count > 0;

        internal void Disable(EntityType t, int id, Mod mod)
        {
            //do a switch on t
            if (DisabledEntities.ContainsKey(id)) return;
            IDEntity m;
            if (!Entities.ContainsKey(id))
            {
                m = IDEntity.SelectVanillaEntity<Monster>(id, mod);
                m.AddProperty(CommandProperty.Create(Command.CLEARMAGIC, m));
            }
            else
            {
                m = Entities[id];
                m.ClearProperties();
                m.AddProperty(CommandProperty.Create(Command.CLEARMAGIC, m));
            }
            Entities.Remove(id);

            DisabledEntities.Add(id, (T)m);
        }

        public void AssignFirstID()
        {
            CURRENT_ID = START_ID;
        }

        private int GetNextID()
        {
            if (CURRENT_ID < START_ID) CURRENT_ID = START_ID;
            //very crude search unfortunately, but should be fine for our purposes
            while (Entities.ContainsKey(CURRENT_ID))
            {
                CURRENT_ID++;
            }
            if (END_ID != -1 && CURRENT_ID > END_ID)
            {
                var typeName = typeof(T).Name;
                var message = $"{typeName} ID {CURRENT_ID} exceeds maximum allowed ID ({END_ID})";
                Parent.Log("Warning: " + message);
                Parent.AddParseIssue(ParseIssueType.IdRangeExceeded, message);
            }
            return CURRENT_ID;
        }

        public void Merge(EntitySet<T> set)
        {
            var entityList = set.GetEntityCollection();
            foreach (var entity in entityList)
            {
                if (entity.DependentEntity != null)
                {
                    entity.DependentEntity.AddProperties(entity.Properties.ToList());
                    continue;
                }
                if (entity.ID < START_ID && entity.Selected)
                {
                    //select monster command on a vanilla
                    if (!Entities.ContainsKey(entity.ID))
                    {
                        Entities.Add(entity.ID, entity);
                    }
                    else
                    {
                        Entities[entity.ID].AddProperties(entity.Properties.ToList());
                    }
                }
                else if (entity.ID < START_ID)
                {
                    //new monster on a vanilla ID?
                }
                else
                {
                    //assign a new ID upwards
                    int newID = GetNextID();
                    entity.ID = newID;
                    Entities.Add(newID, entity);
                }
            }
            foreach (var entity in set.GetUnIDdEntities())
            {
                if (entity.DependentEntity != null)
                {
                    entity.DependentEntity.AddProperties(entity.Properties.ToList());
                    continue;
                }
                //assign a new ID upwards
                int newID = GetNextID();
                entity.ID = newID;
                Entities.Add(newID, entity);
            }
        }

        private List<T> GetEntityCollection()
        {
            return Entities.Values.ToList();
        }

        private List<T> GetUnIDdEntities()
        {
            return UnIDdEntities;
        }

        /// <summary>The entities without an ID: #newevent, #newmerc, a #select by name, number 0.</summary>
        internal IReadOnlyList<T> Unnumbered => UnIDdEntities;

        public List<T> GetFullList()
        {
            List<T> ret = new List<T>();
            ret.AddRange(Entities.Values);
            ret.AddRange(UnIDdEntities);
            return ret;
        }
    }
}
