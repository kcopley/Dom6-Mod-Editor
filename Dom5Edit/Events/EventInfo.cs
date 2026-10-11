using System.Text.RegularExpressions;
using Dom5Edit.Commands;
using Dom5Edit.Props;
using Dom5Edit.Resolve;

namespace Dom5Edit.Events
{
    /// <summary>
    /// What the event commands mean for chains and for showing an event (the event manual,
    /// docs/pdf_extracted/eventman.txt): which commands set and check codes, variables and
    /// enchantments; rarities and orders by name; an event's title from its message.
    /// </summary>
    public static class EventInfo
    {
        public static readonly IReadOnlySet<Command> CodeSetters = new HashSet<Command> { Command.CODE, Command.CODE2, Command.CODEDELAY, Command.CODEDELAY2 };
        public static readonly IReadOnlySet<Command> CodeCheckers = new HashSet<Command> { Command.REQ_CODE, Command.REQ_ANYCODE, Command.REQ_NEARBYCODE, Command.REQ_NEAROWNCODE };
        public static readonly IReadOnlySet<Command> CodeExcluders = new HashSet<Command> { Command.REQ_NOTCODE, Command.REQ_NOTANYCODE };
        public static readonly IReadOnlySet<Command> CodeResetters = new HashSet<Command> { Command.RESETCODE, Command.RESETCODEDELAY, Command.RESETCODEDELAY2 };
        public static readonly IReadOnlySet<Command> VariableSetters = new HashSet<Command>
        {
            Command.CLEARVAR, Command.INCVAR, Command.DECVAR, Command.INC10VAR, Command.DEC10VAR, Command.INVVAR, Command.TOGGLEVAR,
        };
        public static readonly IReadOnlySet<Command> VariableCheckers = new HashSet<Command> { Command.REQ_VARPOS, Command.REQ_VARNEG, Command.REQ_VARZERO, Command.REQ_VARONE };
        public static readonly IReadOnlySet<Command> EnchantmentCheckers = new HashSet<Command>
        {
            Command.REQ_ENCH, Command.REQ_NOENCH, Command.REQ_MYENCH, Command.REQ_FRIENDLYENCH, Command.REQ_HOSTILEENCH, Command.REQ_ENCHDOM,
            Command.REQ_ENCHTARGET, Command.REQ_ENCHNEARBY, Command.NATIONENCH, Command.ASSOWNERENCH,
        };
        public static readonly IReadOnlySet<Command> Delays = new HashSet<Command> { Command.DELAY, Command.DELAY25, Command.DELAY50 };

        /// <summary>Site requirements and effects that use the name in brackets at the end of the message (any value: 0 inverts a requirement).</summary>
        private static readonly IReadOnlySet<Command> SiteByName = new HashSet<Command>
        {
            Command.REQ_FOUNDSITE, Command.REQ_HIDDENSITE, Command.REQ_SITE, Command.REQ_NEARBYSITE, Command.REQ_CLAIMEDTHRONE,
            Command.REQ_UNCLAIMEDTHRONE, Command.REVEALSITE,
        };

        /// <summary>Effects that use the bracketed name when their value is -1 (a site) or 9 (an item).</summary>
        private static readonly IReadOnlyDictionary<Command, long> ByNameWhen = new Dictionary<Command, long>
        {
            [Command.ADDSITE] = -1, [Command.REMOVESITE] = -1, [Command.HIDDENSITE] = -1, [Command.MAYBEADDSITE] = -1,
            [Command.MAYBEHIDDENSITE] = -1, [Command.MAGICITEM] = 9, [Command.ADDEQUIP] = 9,
        };

        /// <summary>The rarities (manual p. 2).</summary>
        public static readonly IReadOnlyList<(long Value, string Name, string Meaning)> Rarities = new[]
        {
            (1L, "Common bad", "rolled at random, a bad event"),
            (2L, "Uncommon bad", "rolled at random, a rarer bad event"),
            (-1L, "Common good", "rolled at random, a good event"),
            (-2L, "Uncommon good", "rolled at random, a rarer good event"),
            (0L, "Always", "every month in every province where it can happen; at most one of these per province a month"),
            (5L, "Always, unlimited", "like Always, but any number can happen in a province each month (for enchantment effects)"),
            (10L, "Always global", "a global event, planned 7 months ahead"),
            (11L, "Common global", "a random global event, planned 7 months ahead"),
            (12L, "Uncommon global", "a rarer random global event, planned 7 months ahead"),
            (13L, "Always global, immediate", "a global event that happens at once"),
        };

        public static string RarityName(long? rarity) =>
            rarity is long r ? Rarities.FirstOrDefault(x => x.Value == r).Name ?? $"rarity {r}" : "no rarity";

        public static bool IsAlways(long rarity) => rarity == 0 || rarity == 5 || rarity == 10 || rarity == 13;
        public static bool IsGlobal(long rarity) => rarity >= 10 && rarity <= 13;
        public static bool IsGood(long rarity) => rarity < 0;
        public static bool IsBad(long rarity) => rarity == 1 || rarity == 2;

        /// <summary>The event orders a province can offer (#order mask), and the matching #req_targorder number.</summary>
        public static readonly IReadOnlyList<(long Mask, long Order, string Name)> Orders = new[]
        {
            (1L, 100L, "Investigate"), (2L, 101L, "Continue"), (4L, 102L, "Accept"), (8L, 103L, "Decline"), (16L, 104L, "Withdraw"),
            (32L, 105L, "Attack"), (64L, 106L, "Diplomacy"), (128L, 107L, "Subterfuge"), (256L, 108L, "Magic"),
        };

        public static long OrderMaskOf(long order) => Orders.FirstOrDefault(o => o.Order == order).Mask;
        public static string OrderName(long order) => Orders.FirstOrDefault(o => o.Order == order).Name ?? $"order {order}";

        /// <summary>Spell effects that make an enchantment whose number is the spell's #damage (global, province).</summary>
        public static bool IsEnchantmentEffect(long effect) => effect is 10081 or 10082 or 10084 or 10085;

        /// <summary>Spell effects that cause the event whose #id is the spell's #damage.</summary>
        public static bool IsCauseEventEffect(long effect) => effect is 42 or 10042;

        /// <summary>The range the manual gives mods for codes (the rest is the game's, and 0 is "no chain").</summary>
        public static bool IsModCode(long code) => code <= -300 && code >= -5000;

        /// <summary>A line's first argument as a number, or null (a name, or no argument).</summary>
        public static long? Number(Property? p)
        {
            if (p == null)
                return null;
            var first = ResolvedValue.ArgumentsOf(p).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return long.TryParse(first, out var n) ? n : null;
        }

        /// <summary>The slots an event has: requirements and effects are two lists of (code, value) pairs (tools/dom6exe README, Events).</summary>
        public const int MaxRequirements = 12, MaxEffects = 20;

        /// <summary>Whether a line goes in the requirement list (the #req_ commands) rather than the effect list.</summary>
        public static bool IsRequirement(Command c) => Name(c).StartsWith("#req_");

        /// <summary>
        /// The lines that take requirement and effect slots in the game, in the order they take
        /// them (past the last slot, the game drops them): a repeatable command (the catalog: it
        /// adds) takes one per line; any other replaces its earlier line's pair, in that slot.
        /// #clear empties both lists; #rarity and #msg aren't pairs.
        /// </summary>
        public static (List<Property> Requirements, List<Property> Effects) Slots(IEnumerable<Property> lines)
        {
            var once = new HashSet<Command>();
            var req = new List<Property>();
            var eff = new List<Property>();
            foreach (var p in lines)
            {
                if (p.Command == Command.CLEAR)
                {
                    once.Clear();
                    req.Clear();
                    eff.Clear();
                    continue;
                }
                if (p.IsDisplayAsset || p.Command == Command.RARITY || p.Command == Command.MSG
                    || GameData.GameCommandCatalog.IsRead(Entities.EntityType.EVENT, p.Command) != true)
                    continue;
                var effect = GameData.GameCommandCatalog.EffectOf(Entities.EntityType.EVENT, p.Command);
                bool repeats = effect != null && (effect.Add.Count > 0 || effect.Or.Count > 0);
                if (!repeats && !once.Add(p.Command))
                    continue;
                (IsRequirement(p.Command) ? req : eff).Add(p);
            }
            return (req, eff);
        }

        public static long? Rarity(IEnumerable<Property> lines) => Number(lines.LastOrDefault(p => p.Command == Command.RARITY));

        /// <summary>The event's message (#msg), or null.</summary>
        public static string? Message(IEnumerable<Property> lines) =>
            lines.LastOrDefault(p => p.Command == Command.MSG) is StringProperty s ? s.Value : null;

        /// <summary>The site or item named in brackets at the end of a message ("[Hidden Gem Deposits]"), or null.</summary>
        public static string? BracketName(string? message)
        {
            if (string.IsNullOrEmpty(message))
                return null;
            var m = Regex.Match(message, @"\[([^\[\]]+)\]\s*$");
            return m.Success ? m.Groups[1].Value.Trim() : null;
        }

        /// <summary>Whether the line uses the bracketed name at the end of the message.</summary>
        public static bool NeedsBracketName(Property p) =>
            SiteByName.Contains(p.Command) || ByNameWhen.TryGetValue(p.Command, out var v) && Number(p) == v;

        private static readonly IReadOnlyDictionary<string, string> Tags = new Dictionary<string, string>
        {
            ["landname"] = "[province]", ["godname"] = "[god]", ["fullgodname"] = "[god]", ["disname"] = "[god]",
            ["goddisname"] = "[god]", ["targname"] = "[commander]", ["fulltargname"] = "[commander]",
            ["targhis"] = "[his]", ["natname"] = "[nation]", ["profname"] = "[prophet]",
        };

        /// <summary>
        /// A short title for an event, from its message: the header line (#header 2) or the first
        /// sentence, tags as words ("[province]"), without the bracketed name; else what it does.
        /// </summary>
        public static string Title(IReadOnlyList<Property> lines)
        {
            var msg = Message(lines);
            if (!string.IsNullOrWhiteSpace(msg))
            {
                var text = Regex.Replace(msg, @"\[([^\[\]]+)\]\s*$", "").Trim();
                text = Regex.Replace(text, "##(\\w+)##", m => Tags.TryGetValue(m.Groups[1].Value.ToLowerInvariant(), out var w) ? w : "...");
                var firstLine = text.Split('\n')[0].Trim();
                if (Number(lines.LastOrDefault(p => p.Command == Command.HEADER)) == 2 && firstLine.Length > 0)
                    text = firstLine;
                else
                {
                    var m = Regex.Match(text, @"^(.+?[.!?])(\s|$)");
                    if (m.Success)
                        text = m.Groups[1].Value;
                }
                text = Regex.Replace(text, @"\s+", " ").Trim();
                return text.Length > 72 ? text[..70].TrimEnd() + "…" : text;
            }
            var effect = lines.FirstOrDefault(p => !p.Command.ToString().StartsWith("REQ_") && p.Command != Command.RARITY
                                                   && p.Command != Command.NOTEXT && p.Command != Command.NOLOG && p.Command != Command.NATION);
            return effect != null ? $"({Name(effect.Command)} {ResolvedValue.ArgumentsOf(effect)})".Trim() : "(no message)";
        }

        public static string Name(Command c) => CommandsMap.TryGetString(c, out var s) ? s : "#" + c.ToString().ToLowerInvariant();
    }
}
