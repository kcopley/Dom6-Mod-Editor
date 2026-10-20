Dom6 Mod Editor @VERSION@ (test build, @DATE@, @COMMIT@)
============================================================

An editor for Dominions 6 mod files (.dm): browse the game's own data and a mod's, edit every
entity with real panels, build event chains, and save back without disturbing lines you
didn't change.

New in this build
- Renumber: "Renumber..." on the toolbar moves the units, weapons, armor and sites a mod numbers
  itself to blocks you choose. It lists how many of each the mod has and the numbers they use
  now; type a start number for each type to move, and the new numbers show as you type. They
  are packed from the start in their order, around numbers the game, a mod this one needs, or a
  line that refers to them already uses; units that turn into the next number stay next to it.
  Every line that refers to a moved number follows, names written with their number
  ("Militia (8100)") too, and only the number changes in a line. It's one edit of the open mod:
  Undo puts it back, and saving is the usual save (a backup first). A move that would change
  what a name in the mod finds in game isn't made. Mods that need the mod (its submods) aren't
  changed.
- #req_pretismnr and #req_targrealmnr are read as units (they follow a renumber or a merge).

In 0.13.1 (fixes)
- Mercenaries: the commander, unit and item a band offers are saved as names, as the game reads
  them (#com "Militia"; "Militia (30)" when another monster has the same name). Picking one
  wrote its number, which the game doesn't read: for a commander or unit it stops loading the
  mod ("bad #com"). Merging mods writes them as names too.
- Events: #1unit, #2com, #1d6units and the other commands named with a number add a line each,
  as in the game (an event with nine #1unit lines showed one).
- An event with more requirements (12) or effects (20) than the game keeps: the message says
  which lines the game drops, and that lines without a number (#notext, #nolog,
  #req_pop0ok, ...) take a slot too.
- The report: lines outside any block (after a misspelt header such as #newevents) are listed:
  the game skips them up to the next block. A number glued to its command (#school-1) is
  pointed out. Every finding has its line.
- Removing a weapon a unit got from a copy no longer changes the order of its other weapons.
  Redo of a new unit: references to it by name work again.
- Monsters: #3castbattlespell.
- Sprites without transparency (24-bit TGA, PNG without alpha) show as in the game: black is
  transparent, magenta a shadow.

In 0.13.0
- Merge mods: "Merge mods..." on the toolbar puts several mods into one new file, in the order
  the game would read them. Where two mods use the same number, the later one's entity moves
  to a free number and everything that refers to it follows (event codes, variables,
  enchantments and monster tags too). A submod is read over its parent. A mod's copy of a game
  unit another mod changes stays as the game has it. The mods themselves aren't changed; the
  merged mod comes with a report of everything that moved and what to look at.
- Several entities side by side: "New window" on a page opens the entity in a window of its own;
  so does a middle click on any link or row (as in a browser), its right click menu ("Open in a
  new window"), or Ctrl+click.
  Edits show in every window at once; undo and save are the mod's. Each window has its own
  back and forward, and opens where the last one was.
- Spells: what a spell's #damage is for each #effect is now read from the game itself (summons
  of a mod's own units by effects 10089/10114, enchantments of effects 133 and 10085, sites).
  The report says when a spell can't work: a combat effect from 1000 to 9999 (the game has no
  such effect: an older way of writing a lasting cloud, it seems), a ritual effect the game
  doesn't know, or a spell nothing can cast.
- Submods: Load ▾, "Load as submod..." opens a submod over its parent in one go. The editor no
  longer guesses a parent when a mod is opened alone.
- A quoted name followed by more text on its line (#copyspell "Stellar Strike" (note)) is read
  as the game reads it: up to the closing quote.

In 0.12.0
- A new look: the editor is now the same program on Windows, macOS and Linux (built with
  Avalonia instead of WPF). Everything works as before; pages, lists and windows look a little
  different (another font, slightly tighter rows). Your settings and backups are where they
  were.
- Event chain map: drag with the middle mouse button to move around, Ctrl+wheel or Fit to
  zoom; click a card to follow its links (double-click opens it); each code and variable has
  its own colour.

Also in 0.11.0
- Submods: a mod can be read over the mods it needs (Sombre Warhammer's submods over Sombre),
  as the game reads mods: in the order they were enabled. Mod Info, "Needs": add, remove and
  order them (remembered for the mod). The parent's units, items and events show and link like
  the game's; changing one adds a line to your mod, and the parent's file is never changed.
  (0.13.0: Load ▾, "Load as submod..." does it in one go.)
- "Used by" lists what really uses an entity (it listed spells that pick from the game's unit
  lists under the wrong monster, and every event that boosts a path under monster 1).
- The report also says when a submod's new unit takes a number its parent already uses.
- The editor also runs on macOS and Linux (separate test packages, ask for them).

Earlier (0.10.0)
- Opening a mod checks it: a bar says what goes wrong in game, which lines the game ignores
  (with "did you mean" for typos) and what's worth a look. "Open report" lists every finding
  with its line and a Go to; "Save for the author..." writes it as a file to send to the mod's
  author. What the game reads is taken from the game itself.
- Your files are safe: a copy of the mod is made when you open it and before every save, and a
  save must read back the same before it replaces the file (see "Backups" below).
- Mounted units: a Mount panel on unit pages (mount, co-rider, riders, skilled rider).

Running it
- Windows 10 or 11, 64-bit. Nothing to install: unzip the folder anywhere and run
  Dom5Editor.exe. Keep the other files next to it (vanilla.dm and the rest are the game's data
  the editor builds on).
- Windows may warn that the program is unrecognised (it isn't signed): "More info", then
  "Run anyway".

Dominions 6
- The game's own texts (unit, item, spell and nation descriptions, event messages) and its
  unit, item and site pictures and nation flags are read from your Dominions 6 install when
  the editor starts. It looks in your Steam libraries. If it doesn't find the game, the status
  bar says so: use the small arrow next to Load, "Dominions 6 folder...", and pick the
  folder Dominions6.exe is in (read at the next start).
- These are read from Dominions 6 version 6.37. With another version they aren't shown; the
  rest works.

Using it
- Load (Ctrl+O) opens a .dm file; Save (Ctrl+S) writes it back. Lines you didn't touch are
  saved exactly as they were. Undo/Redo: Ctrl+Z / Ctrl+Y.
- Ctrl+P: go to anything by name or ID. Ctrl+F: search the list (for events, also the message
  text). Hover anything for what it does.
- Validate checks the mod as it is now and opens the report.
- Merge mods... puts several mods into one new file; Renumber... moves the open mod's own
  numbers to blocks you choose (one edit: Undo puts it back).
- Mods from the Steam workshop: Steam replaces those files when the mod updates, so the editor
  asks before saving there. Save a copy in your Dominions 6 mods folder instead.

Backups
- A copy of the mod's file is made when you open it and before each save, in
  %APPDATA%\Dom5Editor\backups (the last 30 per mod). The small arrow next to Load has
  "Backups of this mod...". If a save doesn't read back the same, the file isn't replaced and
  the editor says why. If the editor fails with unsaved edits, a copy of the mod with them goes
  to %APPDATA%\Dom5Editor\recovery.

If something goes wrong
- The editor keeps running and writes the details to %APPDATA%\Dom5Editor\errors.log. Please
  send that file with what you were doing. Settings (window, recent mods, the game folder) are
  in %APPDATA%\Dom5Editor\settings.json.
