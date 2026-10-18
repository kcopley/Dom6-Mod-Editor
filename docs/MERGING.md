# Merging mods

Assessment of 2026-10-07 (the user: "possibly coming back to how mods can be merged together").
Nothing here is built yet; "measured" means it was tried.

The Renumber tool (docs/RENUMBER.md, 2026-10-18) moves one mod's own numbers to blocks the author
picks, with the merger's moves (`Renumbering.MoveAll`) and its runs of units that go by each
other's numbers (`ModMerger.Runs`); the referee checks it as a one-part merge.

## What the game does with several mods

The game reads enabled mods one after another, in the order they were enabled, all 15 passes
of one mod before the next (`tools/dom6exe/README.md`, "Several mods"). Each mod sees what the
ones before it made. A later mod's `#newmonster 5000` where an earlier one already made monster
5000 replaces it (the validator says so for vanilla's numbers and, since 8d13631, for a needed
mod's). So two mods that use the same numbers can't both be enabled: that's the problem a
merge solves.

## How often numbers collide (measured)

New or modded numbers (`#new*`, and `#select*` in the modding ranges) in the installed mods,
and how many each pair shares:

| Pair | Shared numbers |
|---|---|
| Forgotten Realms 0.95 x DomEnhanced 2.14 | 1,087 (1,001 monsters) |
| Confluence 1.06 x DomEnhanced 2.14 | 997 (820 monsters, 160 items) |
| Forgotten Realms x Confluence | 728 (727 monsters) |
| Sombre Warhammer x Confluence | 520 (288 weapons, 192 monsters) |
| Sombre x Forgotten Realms | 270 (183 sites) |
| Sombre x DomEnhanced | 210 |
| Sombre x PS Bloodwar | 74 |

Every big mod starts at the bottom of the modding ranges, so none of them can be enabled
alongside another without a merge.

## Two kinds of merge

1. **Stacking** (a submod over its parent, or mods meant to be enabled together): the result
   must be what the game gets from enabling them in order. One file with the mods' text one
   after the other is almost that. The difference is that one file is read type by type over
   the whole text: the second mod's weapons are read before the first mod's monsters. Name
   lookups and `#copy*` sources can then see something else. The editor already reads a
   submod over its parent ("Needs" on Mod Info), so a stacked file is only useful to ship one
   file instead of several.
2. **Side by side** (independent nation mods, the project's original purpose): one mod's new
   entities move to free numbers, and every reference to them is rewritten: by number, by
   name when the name is unambiguous, and in events, spells (`#damage` where the game reads it as
   a monster, monster tag, enchantment, event `#id` or site: below), sites, nations and mercenaries. The old `ModSet.MergeAll` did a first version of this. It
   is dead code now: nothing calls it, and it predates the resolver, the save plan
   (original-order saving) and the reference rules ("used by").

## Design (decided with the user, 2026-10-07)

The user's original idea, kept: connections are object pointers, so numbers can change freely and
the export writes whatever numbers are current. The core already works so: a resolved reference
holds its target (`StringOrIDRef.Entity`, `IDRef.Entity`) and writes `Entity.ID`; codes, variables,
enchantments and monster tags are shared `DependentEntity` objects written by `GetID()`; a block's
header is rewritten when its entity's number changed (`ModExporter.KeepsHeader`).

- **Inputs, in order.** Each is read over what it needs (the Needs chain): a submod over its
  parent, whether the parent is in the merge or not; an independent mod over vanilla, as its
  author tested it. A needed mod that isn't in the merge stays a separate mod, enabled before the
  merged one; its numbers are taken.
- **Collisions: the later mod moves**, only what collides, into the first free numbers of the
  type's range (no bands). Collisions count only between mods that don't depend on each other: a
  submod's `#select` of its parent's entity is the parent's entity, and its `#new` on its parent's
  number is a deliberate replacement that keeps it. Owned numbers: a `#new N`, or a `#select N` of
  a number nothing under the mod has (new nations, poptypes, nametypes).
- **Not only entities:** event codes, event variables, enchantments, monster tags and the other
  `DependentEntity` numbers move the same way, so independent mods' events don't trigger each
  other; a submod's codes are its parent's objects (`DependentEntity.Dependent`) and follow them.
  Game numbers (vanilla enchantments, codes above -300) never move.
- **References by number wherever the game takes a number** (`Mod.KeepReferenceForms = false`), a
  name kept as a comment. Which commands take only names comes from the exe's reading rules
  (`dmread-6.37.json`: no numeric format). Selects by name become selects by number where there is
  one. Only a name-only reference whose name would find another entity in the merged file gets
  its target renamed with a suffix, as a last resort, and the report says so. A mercenary band's
  `#com`, `#unit` and `#item` are text to the game (its merc parser reads a quoted string: a
  number reads nothing and the game stops, "bad #com"): written as names, a monster as
  `"Name (id)"` where the plain name would find another one first (2026-10-11; before, a merge
  wrote them as numbers).
- **One file**: a merged header (`#modname`, a description listing the parts and their versions),
  then each part in its own order with its comments, behind a banner line; unchanged lines as
  written, lines rewritten only where numbers or names changed. Sprites and other files a part
  names are copied next to the merged file (in a folder per part) and their paths rewritten.
- **A report**: every number moved (old -> new, per mod), every rename, references a part leaves
  unresolved that now find another part's entity, and things that act across mods (clearing
  commands, both mods changing the same game entity: the later wins, as in game).
- **The referee**: the merged file read back must give every entity the same values as its part
  did (references compared by target); then `gameread.py` replays the game's reading of the parts
  one after another and of the merged file, numbers mapped, and they must agree.
- Not carried over: the Dom5 merge's mage disabling (Dom5 had a fixed number of magic-path slots;
  the user: leave it out for Dom6).

## What else a merge keeps as it was (built 2026-10-07)

- **Units chained by number** (`#shrinkhp`, `#xpshape`, `#labxpshape` turn into the next number,
  `#growhp` the previous; not with `#xpshapemon`) move as one block into free numbers in a row.
  AI pretender templates move with their nation; a template's `#form "Name (N)"` follows unit N.
- **Copies of game entities stay apart** (the user: DomEnhanced's copy of the game's Archer
  mustn't take Forgotten Realms' changes to the Archer). Read one after another, a later part's
  `#copystats 20` would copy what an earlier part made of unit 20; so the merged file starts with
  a snapshot of each such game entity, as the game has it before any part changes it, and the
  later part's copy lines copy that. Not for a part's copy of what it changes itself, nor a
  submod's copy of what its parent changed (both meant). A copy of a unit, weapon or armor shows
  nowhere until something uses it. A spell's or site's would (one more spell to research, one
  more site on the map), so the snapshot has a line that hides it, and each copy of it gets the
  game's value back on the line right after the copy (the part's own lines after it still win):
  - spells: `#selectspell N` from the top of the table (7999 down), `#copyspell`, `#school -1`
    (nobody can research it); each copy gets `#school <the game's>` after it. Not `#newspell`:
    the game's `#newspell` takes no number, it gets the first free one from 1500 (the exe,
    0x1401ad830); `#copyspell` copies the whole spell record, name and school too.
  - sites: `#newsite N` (the game keeps a number from 750 to 3999), `#copysite`, `#rarity 5`
    (never a random site); each copy gets `#rarity <the game's>`.
  - items: `#selectitem N` from the top of the range (1999 down; `#newitem` takes no number),
    `#copyitem`, `#constlevel 11`; each copy gets `#constlevel <the game's>` after it. From the
    exe (tools/dom6exe/README.md "Items"): the forge needs construction research of the level
    (11 is out of reach), and every random pick asks for a level from 0 to 4, except two that
    take any level: an arena prize (items with `#champprize`) and a new commander's gear by tag
    (ability 567, no command sets it). Copies of those 16 game items (the 7 Champion's items,
    the Dragon Pearls, the Crown of Ohya, ...) aren't kept apart, as a hidden copy could still
    be handed out: the report notes them.
  Forgotten Realms + DomEnhanced: 61 snapshots (5 units/weapons, 56 spells for 219 copy lines).
- **Numbers the game gives.** `#newspell`, `#newitem` and `#newnation` take no number (the exe):
  each gets the first free one, spells from 1500, items from 700, nations from 120, in the order
  the game reads them, whatever the line says. In the merged file a part's unnumbered ones come
  after every earlier part's, so the merge works out their numbers as the game will
  (`GameSlots`): a later part's numbered entity on one of them moves (it would be the same entity
  in game), as does one of a part's own numbered entities that its unnumbered ones, pushed up by
  the earlier parts, would land on; nothing moves onto them, nor do dangling references or
  snapshots. Forgotten Realms' 32 `#newitem` get items 700-731, where DomEnhanced has 28 of its
  own: those move. (Before this the merge put them, and one moved item, on Forgotten Realms'
  items; the referee now numbers blocks as the game does and catches it.)
- **Dangling references don't catch anything** (the user): a number a part refers to that nothing
  in it defines would find another part's entity in the merged file; it moves to a free number
  that finds nothing, as alone, and the report says so.
- **Both parts change the same game entity**: merged as the game reads them (the later wins per
  line), and reported (the user: "we merge the best we can on it").
- **Lines a later copy or clear takes out** are still written and still follow moves: the game
  reads them there.

Checked by stage 6 of the fidelity suite (docs/FIDELITY_SUITE.md): fixtures for events, sequences,
copies, the numbers the game gives and spells' #damage, and the installed workshop mods
(Confluence + Bloodwar, the Sombre pack with its submod, Forgotten Realms + DomEnhanced), with
baselines of what moved.

### A spell's #damage (checked against the exe, 2026-10-07)

What `#damage` is depends on `#effect`, and it is read from the game's own effect code
(`tools/dom6exe/dom6exe.py spelleffects` -> `data/spell-effects-6.37.json`, README "Spell effects
and #damage"; embedded in Dom5Edit, SpellEffectData), not from the inspector's tables. It moves
with its target only for: monsters and monster tags (1, 21, 31, 43, 54, 126, 165; 10001, 10021,
10026, 10037, 10038, 10050, 10062, 10093, 10119, 10130, 10137, 10141), 10089/10114 from 100 up
(1-99 is a key into the game's lists of uniques and stays), enchantments (81, 133, 10081-10085),
event `#id`s (10042) and sites (10154). Everything else stays as written: affliction and buff
bitmasks (web, false fetters: effect 11), monster ability numbers (500-699, 10500-10599), counts,
damage, codes. A spell with no `#effect` of its own uses its `#copyspell`'s or the game spell's
(from vanilla.dm). Combat effects 1000-9999 have no case in the game (only its AI reads effect %
1000): their #damage is a plain number, and the editor's report says the spell does nothing. The referee (`gameread.py --merge`) reads the same exe
table, so it checks the merger's spells instead of trusting its word: on Forgotten Realms 0.95 +
DomEnhanced 2.13 the old classification left 62 of DomEnhanced's 10089 spells summoning
Forgotten Realms' monsters. Open: 10141 summons its monster and the next number; a merge doesn't
keep such a pair in a row yet (only vanilla uses 10141). `Dom5Tests/fixtures/merge/sp_a.dm` +
`sp_b.dm` is the check (stage 6, merge-spells).

In the editor: "Merge mods..." (toolbar; `MergeViewModel`, `MergeWindow`): the mods in order, a
submod's parent per row (remembered from Mod Info's "Mods this one needs"), the merged mod's name
and file (never over one of the mods, nor in Steam's workshop folder; an existing file backed up
first); the merge runs off the window's thread, then the merged file is read back (no number
defined twice that the mods didn't) and the summary links the report and opens the merged mod.

Steps: (1) renumbering in the core (an entity or a dependent number moves, its references follow,
the save shows it); (2) `Dom5Tests merge OUTDIR NAME A.dm B.dm ... [--needs B.dm=A.dm]` with the
report and the read-back check; (3) the game-reading referee on mod pairs (Forgotten Realms +
DomEnhanced: 1,087 collisions) and on Sombre with its submods; (4) "Merge mods..." in the editor
(done 2026-10-07).
