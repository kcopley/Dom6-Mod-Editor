# Renumbering a mod

Built 2026-10-18 for the author of the Legendary mods, whose numbers are "all over the place"
and partly in the game's own ranges: he asked for a way to move every ID of a kind to a block he
picks (monsters from 8500, weapons from 1700). The user wanted it generic: the numbers each mod
uses, a start number per type, everything remapped to the start plus the range used. It's a
small tool on a loaded mod, and the usual saving (backups, the read-back check) works alongside.

## Using it

**Renumber...** (next to Merge mods..., with a mod open):

| Column | |
|---|---|
| How many | the entities the mod numbers itself; "19 +2": and two of its own on the game's numbers |
| Numbers now | the lowest and highest |
| Start at | the first new number; empty: that type stays |
| New numbers | the block they'd take, worked out again at each number typed |

Below the table:

- **The game's numbers:** a tick box when the mod makes its own `#new...` on the game's numbers.
  In game such an entity replaces the game's; moved, the game has its own back.
- **Problems (⛔):** why nothing can move yet.
- **Notes (•):** what to know first: numbers skipped, units kept where they are, lines to change
  by hand.

**Renumber** makes the move: one edit of the open mod. Undo puts it back. Nothing is written
until the mod is saved, and saving is the usual save: the file as it was goes to the backups,
and the save is read back and must hold the same as the editor. A mod in Steam's workshop folder
gets a note to use Save As instead (Steam replaces that folder when the mod updates).

From the command line, the same plan, edit and save:

```
Dom5Tests renumber MOD.dm OUT.dm --start monster=8500 --start weapon=1700 [--game-numbers] [--with PARENT.dm] [--json FILE]
Dom5Tests renumber MOD.dm OUT.dm --survey     # only the numbers per type
```

It prints the plan, saves OUT.dm as the editor saves, writes OUT.map.json for the referee
(`python3 tools/dom6exe/gameread.py --merge OUT.dm --map OUT.map.json`), then checks that undo
saves the mod byte for byte as an unedited save and redo as OUT.dm.

## What moves

Monsters, weapons, armor and sites: the types a mod numbers itself. `#newspell`, `#newitem` and
`#newnation` take no number (the game gives one), so there's nothing to move. An entity moves
when the mod makes it on a number nothing below uses (not the game, not a mod it needs). A
`#select...` of the game's entity, or of a needed mod's, isn't the mod's number and stays. The
mod's own `#new...` on the game's numbers moves only when ticked.

## Where they go

Per type, the entities are packed from the start number in their order (lowest first). The
block skips every number that's taken:

- **The game's:** the start must be above the game's last (monster 4136, weapon 885, armor 296,
  site 1405), and the block must end by the type's last number (19999, 3999, 1999, 3999).
- **A needed mod's:** with `#needs` (or `--with`), its numbers.
- **The mod's own that stay:** see below.
- **Numbers a line refers to that nothing has:** the line still finds nothing after the move,
  instead of finding whatever landed there (a note per line).

Units that go by each other's numbers stay in a row in the same order (`ModMerger.Runs`, the
merger's rule):

- `#shrinkhp` and `#growhp` (the next or previous number);
- `#xpshape`/`#labxpshape` without `#xpshapemon`;
- a ritual summoning the next number too (effect 10141).

A unit whose neighbour isn't the mod's own (a lone `#shrinkhp` to a number the mod doesn't make)
keeps its number. That neighbour stays free, so the unit still turns into the same thing. A
note says so.

## What follows

Every line that refers to a moved number follows it:

- references by number (`#weapon 1212`, `#homemon`, event `#1unit`, `#req_monster`, a spell's
  `#damage` where the effect takes a monster, ...);
- a name written with its number, `"Name (8100)"` (a merc's `#com`, a template's `#form`);
- the block headers (`#newmonster 7864`, `#selectmonster 7864`). Spacing and comments are kept,
  and only the number changes.

A line is rewritten only where a number changed, in its original text. Everything else is saved
as written. A line with several commands keeps its shape, and the file keeps its line count.

Two pre-existing faults were fixed so that more lines follow: `#req_pretismnr` and
`#req_targrealmnr` were read as plain numbers, and are now monster references.

**Names.** The game finds a name at the lowest number that has it. A move can change which of
two entities with the same name is lowest, and so what a line naming it finds. The edit checks
every name written: if any would find something else, nothing is renumbered, and the message
says which name. An own entity on a game number that a line names by name is a problem: moved,
the name would find the game's.

## What it doesn't change

- **Lines the editor doesn't read** (kept as written) that the game reads, with a number that
  moves: they don't follow. A note gives the line and the number to change by hand.
  Misspelled commands the game skips aren't noted.
- **Submods that need this mod:** a submod's lines referring to the moved numbers aren't
  changed. Renumber the parent before the submods refer to it, or edit them after.
- **Other mods:** a separate mod that refers to these numbers by number.

## Checks

The referee reads the renumbered mod as a one-part merge (`gameread.py --merge`). It maps every
number through the moves, compares what the game reads from the mod and from the renumbered
file, and checks chained units kept their order. Sprites are compared by file, so the mod's
image folders must sit next to the renumbered file. The suite links them there.

Fidelity suite stage 6, `renumbers` in `tools/fidelity/suite.json` (docs/FIDELITY_SUITE.md):

| Case | Moved | Lines changed |
|---|---|---|
| fixture (`Dom5Tests/fixtures/renumber/ren_a.dm`) | 8 | 22 of 148 |
| fixture, overlapping the numbers now, game numbers ticked | 11 | 30 of 148 |
| Legendary Cataclysm (game numbers ticked) | 261 | 1,380 of 14,613 |
| Legendary Dwellings | 370 | 1,405 of 10,917 |
| Legendary Guildmasters | 62 | 211 of 3,514 |
| Legendary Sellswords | 128 | 189 of 4,832 |
| Legendary Sites | 165 | 388 of 6,213 |
| Legendary Vendors | 64 | 415 of 4,712 |
| Confluence 1.06 | 2,739 | 9,862 of 136,880 |
| Forgotten Realms 0.95 | 1,838 | 6,558 of 70,787 |
| Sombre Warhammer | 3,425 | 13,172 of 135,121 |
| DomEnhanced 2.14 | 4,515 | 16,350 of 194,075 |

All the cases above pass:

- undo and redo are byte for byte;
- the game reads each renumbered file as the mod;
- no chained unit is out of order.

The referee notes one known kind, on Confluence: a `#descr` without its closing quote runs over
a rewritten line.

The Renumber window was checked headless (`Dom5Editor --snapshot --mod MOD --renumber-window
FILE.png monster=4200 weapon=900 site=1500 game apply --save ...`): its save is the command
line's, byte for byte, and undo restores the file.

## Code

| Where | What |
|---|---|
| `Dom5Edit/Merge/ModRenumber.cs` | `Survey` (numbers per type), `Plan` (blocks, notes, problems), `RenumberContext` (what doesn't depend on the start numbers: ~200 ms on Confluence once, then ~2 ms per plan) |
| `Dom5Edit/Editing/RenumberEdit.cs` | the edit: retargets, moves, names written with numbers, raw text and headers, the name check; undo/redo |
| `Dom5Edit/Merge/Renumbering.cs` | `MoveAll`: every move through temporary numbers, then references follow |
| `Dom5Edit/Merge/ModMerger.cs` | `Runs` (shared with the merger), `WriteRenumberMap` (the referee's map) |
| `Dom5Editor.Core/UI/Views/RenumberViewModel.cs` | the window's rows, plan and apply |
| `Dom5Editor.Avalonia/Views/RenumberWindow.axaml` | the window |
| `Dom5Tests/RenumberCheck.cs` | `Dom5Tests renumber` |
