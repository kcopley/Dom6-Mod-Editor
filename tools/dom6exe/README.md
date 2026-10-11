# dom6exe: game facts straight from Dominions6.exe

The dom6inspector's game data (and through it our `vanilla.dm`) comes from a CSV extraction
tool that has no current maintainer. The exe itself holds both the vanilla tables and the
`.dm` parser, so this reads them directly. Only the exe is needed: no other game files.

```bash
python3 tools/dom6exe/dom6exe.py commands  --out tools/dom6exe/data/commands-6.37.json
python3 tools/dom6exe/dom6exe.py readonly  --out tools/dom6exe/data/readonly-monster-abilities-6.37.json
python3 tools/dom6exe/dom6exe.py monsters  --out monsters.json      # ~3 MB, not committed
python3 tools/dom6exe/dom6exe.py vanilla   --out vanilla.dm    # the editor's vanilla base
python3 tools/dom6exe/dom6exe.py catalog   --out Dom5Edit/GameData/game-commands-6.37.json
python3 tools/dom6exe/dom6exe.py events    --out tools/dom6exe/data/events-6.37.dm
python3 tools/dom6exe/dom6exe.py sprites   --out tools/dom6exe/data/sprites-6.37.json
python3 tools/dom6exe/dom6exe.py texts     --out tools/dom6exe/data/texts-6.37.json
python3 tools/dom6exe/dom6exe.py spelleffects --out tools/dom6exe/data/spell-effects-6.37.json
python3 tools/dom6exe/dom6exe.py dmread    [--mod FILE.dm [--lines A-B] [--context monster,event]]
```
Options: `--exe PATH` (or env `DOM6_EXE`; default
`/mnt/c/Games/Steam/steamapps/common/Dominions6/Dominions6.exe`), `--inspector DIR` (a
dom6inspector checkout; its unit CSV names ability numbers, as hints only). Needs Python 3.8+
and GNU `objdump`. The exe is never copied into the repo.

## What it reads

| Output | What |
|---|---|
| `contexts` | Every command the game's `.dm` parser compares against, per entity type (monster, item, weapon, armor, spell, site, nation, merc, poptype, nametype, event, global, bless, sound, and the top level `#new*`/`#select*`). A command that isn't listed is ignored by the game. |
| `ability_keys` | For monster and item commands: the numbered ability each one sets (`#xpshape` = 1145). Monsters and items share the numbering. |
| `monsters` | The vanilla monster table: name, 12 base stats, up to 48 `(ability, value)` pairs. |
| `not_settable` | Abilities vanilla monsters have that no monster command sets. An editor shows them, read-only. `possibly_set_by` lists commands whose own handler uses that number (e.g. `#blind`, `#assassin`, `#unmountedspr1`); for small numbers that is often noise. |
| `catalog` | For Dom5Parser (embedded in Dom5Edit): per entity type, the commands the game reads, and whether that list is complete (the event parser also reads `#2d6units`-style commands by pattern); and per command, what it writes (`effects`: fields and abilities it sets, appends to, ORs into or removes, flag bits it sets or clears, and the argument range of generic-handler commands). Dom5Parser's resolver (Dom5Edit/Resolve) uses the effects to tell whether a line replaces an earlier one. |
| `vanilla` | Every vanilla type the editor lists, written as the commands that store each value: weapons, armor, monsters, spells, items, sites and nations as `#select*` blocks (`vanilla_dm.py`), then blesses, poptypes and nametypes as `#select*` blocks and the mercenaries as `#newmerc` blocks (`vanilla_other.py`). Values no command can store are `-- ro:` lines (shown read-only). Each table's end marker (a record named "end") is left out. AI templates have no vanilla data: the game reads them only from mods. |
| `sprites` | Which picture the game draws for each vanilla monster (and its attack frame and unmounted sprite) and item: the sprite numbers it stores, and the archive (`sprites.py`). Numbers only; the editor reads the pictures from the player's install (`Dom5Edit/VanillaSprites.cs`, `Dom5Editor/Sprites/GameArt.cs`). Also the rule for a site's picture, and (`"flag"`, `flags.py`) how the game builds a nation's flag from parts of `flag.trs` and the nation's colors. |
| `texts` | Where the game keeps its texts (`texts.py`): monster, item and spell descriptions, a spell's details, portent and cure, a nation's description, summary and brief. Only locations: the two lists of string pointers, how each kind's key is made, and a checksum of the bytes read. The editor reads the texts from the player's own exe (Dom5Edit/GameData/VanillaTexts.cs). See Texts below. |
| `snapshot` | Everything `changelog` compares, from one exe, into a folder (`--out DIR`; `--texts` keeps the game's texts too, local use only). |
| `changelog` | What changed between two versions: `changelog OLD NEW [--out F.md]`, each a snapshot folder, an exe or a vanilla .dm ("Game updates" below). |
| `dmread` | How the game reads a `.dm` file (`dmread.py`, "Reading .dm files" below). Alone: the rules it reads from the exe (the string commands per type with their length limit and whether reading skips their text, the `#new`/`#select` check, the types that refuse a missing `#end`). With `--mod`: the game's reading replayed on that file, and where it differs from a line-by-line reading (strings running over command lines, `#` read inside quotes, texts cut at their limit, fatal errors, ...); `--lines A-B` lists what each pass reads there. |
| `spelleffects` | What a spell's `#damage` is for each `#effect` (`spelleffects.py`): a monster, a monster or tag, an enchantment, an event `#id`, a site, an ability number, a bitmask or a plain value, with the code address it was read from. Embedded in Dom5Edit (SpellEffectData), used by the merger and its referee (`gameread.py --merge`). See "Spell effects and #damage" below. |
| `events` | The 3,302 vanilla events as `#selectevent N` blocks (`events.py`): rarity, requirements and effects in stored order. The messages (the game's text) are left out unless `--messages`; the header line `-- messages: exe <checksum> offset <file offset> record <size> size <message size> count <n>` says where an editor reads them from the player's own exe. Each stored (code, value) pair is written as the command that stores that code; codes no command writes are `-- ro: requirement N = v` / `-- ro: effect N = v` lines, with the game's own name for the code when it has one. A JSON summary goes to stdout. |

## How it finds things (no hard-coded addresses)

- **Parser functions:** each entity parser compares the line against its own command names, so
  it's located by an anchor command (`#copystats` for monsters, `#copyweapon` for weapons, ...).
  The function bounds come from the exe's exception table (`.pdata`).
- **Generic handlers:** most simple commands go through one of two functions called with
  (line, name, ability number, ...). They're the two callees most often called with a command
  name. The ability number is a constant, often written as `r9 + N`.
- **Ability setter:** the function `#magicboost 51 <n>` calls after loading ability 20.
- **Monster table:** located from known vanilla names (1 Logrian Slinger, 2 Standard, 3
  Serpent Cataphract), so the record size falls out (888 bytes in 6.37). The parser caps
  monster numbers at 19999. Every run checks the layout against Heavy Cavalry (20).
- **Event table:** from the event parser's own branches: `#msg` (the table's address, the
  record size it multiplies by, the message size), `#rarity` (a byte store), `#selectevent`
  (the end marker it refuses, the highest number), `#newevent` (the free marker it scans for,
  the first mod slot). The two pair lists come from the generic handler: it appends to one
  list or the other depending on which "current event" global is set, and the event parser
  sets one before the `#req_` commands and the other before the effects. Every run checks
  the record size against three consecutive vanilla events that share a message, and that
  the record after the last vanilla event reads "end".

## Reading .dm files (6.37)

How the game turns a mod file into commands, read from the code. `dmread.py` replays it on a
file (`dom6exe.py dmread --mod FILE`); on Confluence 1.06, ForgottenRealms-095 and Sombre
Warhammer (the examples below) and the other workshop mods installed here it reports no fatal
error, as it should for mods that load.

**One pass per entity type, over the whole text.** "Executing all mods" (0x140229c20) reads
every mod 15 times, one pass per type in this order: sounds (0x1402462d0), weapons
(0x140253300), armor (0x14022bdc0), monsters (0x140249700), name types (0x14023f570), blesses
(0x14022cfa0), sites (0x1402330f0), nations (0x14023fb20), spells (0x1402469a0), items
(0x140236620), general (0x1402350d0), poptypes (0x140245a70), mercenaries (0x14023df70), events
(0x14022e5f0), AI templates (0x14022afb0). (`#modname`, `#description`, `#icon`, `#version` have
their own reader, 0x14023f060.) Each pass loads the file (0x140068160), prepares it
(0x140227150) and walks the text one byte at a time, `cmp BYTE PTR [text+pos],0x23`: at every
`#`, wherever it is, it tries its commands; every other byte is skipped. There are no lines or
tokens. So types are read in that order whatever their order in the file (a monster can name a
weapon defined further down), and within a type in file order.

**Several mods: one after another, in the order they were enabled.** The loop runs all 15
passes on one mod before the next, so a mod sees only the mods before it. The list it walks
(names at 0x183d07fc0, 100 bytes each; the last index at 0x1432c957c, -1 for none; at most 500)
isn't sorted anywhere: enabling a mod on the Mod Preferences screen appends it (0x140225e60),
disabling one removes it and moves the rest up (0x140225ae1), `--enablemod X/Y` appends in
command-line order, and the list is saved in that order in `dom6config` (0x1401e2fc0) and
copied in that order into a new game's settings (0x140226754). So a submod has to be enabled
after the mod it needs; to change the order, disable and re-enable.

**Preparing the text** ("preparemodtext", 0x140227150), before every pass:
1. The file's last byte is overwritten by the terminator (`mov BYTE PTR [rax+rcx-1],0`; the
   loader allocates exactly the file size). A file that doesn't end with a line break loses its
   last character: PS Bloodwar Tanar'ri.dm ends with `#end`, which the game reads as `#en`, so
   its last nation is never closed (harmless for a nation; for a spell, bless or sound block the
   game stops: "no #end for modded spell").
2. A CR before an LF is dropped. Tabs become spaces.
3. `--` and everything after it to the line break is dropped, anywhere on the line: inside
   quotes too.
4. Every `$` is dropped (0x1400707b0; it's the marker steps 2-3 write over what they remove), in
   texts too: `"costs $5"` reads `costs 5`.

**Commands.** At a `#` a pass compares the text after it with its command names byte for byte
(strncmp, 0x140311130): case matters, `#Name` and `#HP` aren't commands. The matcher
(0x140258bb0, and the same code inlined in most parsers) also wants the next character not to
be a letter or digit (0x140070f80, 0x140070fa0). The two generic handlers, which read most simple
commands (0x1402595f0 for monsters, items and spells, 0x140259050 for sites, nations, poptypes
and events), compare `"name "` for a command with an argument, so it must be followed by a space
or tab, and `"name"` for one without (0x1402595f0 then refuses a following lowercase letter,
0x140259050 checks nothing; no two of the game's own names collide this way). A name that
matches nothing is ignored, as is everything that isn't after a `#`.

After a command the scan goes on right after its name (the matcher leaves the name's length in
0x14625e0c0, a branch that reads an argument adds it to the position, the generic handlers
return it; then the loop adds 1): the argument isn't skipped, so every command on a line is
read (`#stealthy 999 #inanimate #magicbeing`: all three). One exception, through that `+ 1`: a
`#` glued to the end of a command name the branch stepped over is skipped (`#slave#amphibian`
reads only `#slave`; none of the three mods below does this). At an unmatched `#` the scan also
skips the next byte, so in `##landname##` the tag's name is never compared: message tags are
never commands.

**Arguments.**
- Numbers and lookups: the rest of the line (0x1400f42a0: skip spaces, copy up to a CR, LF or
  the end of the text, at most 2,499 characters, trim trailing spaces), then sscanf `%d`,
  `%I64d` (generic handlers) or `%f`. So a sign and the digits up to the first non-digit: `+5`
  is 5, `5.0` is 5, `12 #hp 5` is 12, `0x10` is 0. With no digits sscanf sets nothing: the
  generic handlers then use 0 (clamped to the command's range), the others whatever their
  variable held. A name argument (`#weapon "Net"`, `#selectmonster "Heavy Cavalry"`,
  `#copystats`) is the first quoted string in that copy of the line, so it has to close on its
  line; the lookup ignores ASCII case (0x140074100) and also takes `"Name (id)"` (0x1402b7720).
- Texts: the quoted-string reader 0x1400f43e0 reads from the file text itself. The opening
  quote must come before the line break (else it reads nothing); then it copies everything up
  to the next `"`, across line breaks and `#`s, or to the end of the text, at most limit - 1
  characters (table below). There's no escape: the first `"` after the opening one ends the
  text, and what follows on that line isn't part of it (it's scanned for `#` like anything
  else). Names, file names and lookups get `%` replaced by `_` (0x14006de70, called when the
  reader's 4th argument is 1); descriptions and messages keep it.
- `#descr`, `#name` (mercenaries' excepted), `#details`, `#portent`, `#cure`, `#summary`,
  `#brief`, `#addname` that read nothing (no opening quote on their line, or `""`) stop the game
  ("bad descr for new monster", "#name, empty name", "bad #summary for nation", ...). An empty
  `#msg` or `#epithet` is accepted.

**What the scan does after a text** (`add pos,eax` after the reader's call, or not):
- Commands that skip their text: `#name` (all types but items), `#msg`, `#addname`, `#epithet`,
  `#spr1` `#spr2` `#xspr1` `#xspr2` `#unmountedspr1` `#unmountedspr2`, item `#spr`, `#flag`,
  `#indepflag`, `#sample`, weapon `#sound` (it also takes a number), the mercenary strings, the
  AI template strings. The scan resumes the text's length after the command name, which is just
  before the closing quote (one space between name and quote: at the text's last character), so
  nothing inside the text is read: a text over several lines swallows the `#` lines in it. A
  text cut at its limit resumes inside the text, at the cut.
- Texts that don't: `#descr` (monsters, items, spells, nations), `#details`, `#portent`,
  `#cure`, `#summary`, `#brief`, and item `#name`. The scan goes on right after the command name
  through the text, so a `#` followed by a command name of that type inside it is a command, an
  `#end` too.
- Either way, commands after the closing quote on its line are read (Confluence 1.06 line 2547:
  `...each month."	#spr1 "./..."`: the `#spr1` is read).

| Text | Characters kept |
|---|---|
| `#name`: monster, weapon, armor, spell, nation; `#addname`; item and mercenary names (read up to 2,499, then cut) | 35 |
| site `#name` / bless `#name` / `#epithet` | 39 / 31 / 37 |
| `#descr` (monster, item, spell, nation), `#details`, `#portent`, `#cure` | 2,499 read, 1,999 stored (texts are stored in 2,000 bytes, see Texts) |
| `#summary`, `#brief` | 499 |
| `#msg` | 2,399 (the event record's 2,400 bytes) |
| sprite and sound file names, `#flag`, AI template strings | 2,499 |
| `#modname` / `#icon` / mod `#description` | 49 / 99 / 1,999 (the mod `#description` finds its opening quote anywhere after the command, also lines later, 0x1400f4350, and drops trailing whitespace) |

**Blocks.** Outside a block a pass only knows its own `#new...`/`#select...` (and its
`#clearall...`; the general pass has no blocks: its commands are read anywhere). Inside one it
tests `#end` first, then the `#new`/`#select` check, then its own commands. `#end` is the only
thing that closes a block.
- The check (0x140258c90) compares the start of the text (no word end) with 20 names: `#select`
  and `#new` for armor, weapons, monsters, items, nations, spells and sites, `#selectnametype`,
  `#selectpoptype`, `#newtemplate`, `#clearallitems`, `#clearallspells`, `#clearallevents`.
  Inside a monster, weapon, armor, item, spell, site, nation, event, bless, sound or AI template
  block, any of them stops the game: "You must end modding the monster before using a #new... or
  #select... command" (0x140067cc0 formats it, 0x1401e21d0 prints it, shows it and exits).
- `#newevent`, `#selectevent`, `#newmerc`, `#selectbless`, `#selectsound` aren't on that list:
  inside another type's open block, that pass ignores them and the block goes on to the next
  `#end`, taking any of its own commands on the way (a bless block's `#name` renames the open
  monster). Inside an open event block, `#newevent` is ignored by the event pass as well: the
  next event's commands go to the open event.
- Name type, poptype and mercenary blocks have no check: an open one takes that type's commands
  from the following blocks until an `#end`.
- A block still open at the end of the file stops the game for spells, blesses and sounds ("no
  #end for modded spell"); the others keep it.

**The questions, in short.** (1) Every command on a line is read. (2) A text reads to the next
`"` across lines; a `#` line inside is text for the skipping commands (`#msg`, `#name`, ...) and
still a command for the descriptions; with no closing quote the text runs to its limit or the
end of the file. (3) Yes, text after a closing quote is scanned. (4) `#descr "#descr "Chaos
Spawn ..."` (Sombre Warhammer line 105688, a spell): the first `#descr` reads `#descr ` (up to the
second quote), the scan goes on into it, finds the second `#descr`, which reads `Chaos Spawn ...`
and replaces the first: the description is "Chaos Spawn are horrifying creatures ...". (5)
`--` is a comment anywhere, inside quotes too; indentation doesn't matter; no line length limit
except 2,499 characters for a command's rest of line; text limits in the table. (6) No implicit
close: a `#new`/`#select` in an open block is fatal for most types, ignored for the rest. (7)
Case-sensitive commands, case-insensitive name lookups, sscanf numbers, no escaped quotes, `$`
dropped, the last byte of the file dropped.

**Compared with Dom5Parser's ModParser** (2026-10-07), on the three mods it was checked with:
- A quote not closed on its line, followed by command lines: ModParser assumes a forgotten quote
  and reads the commands. The game does too for the descriptions (Confluence 1.06 lines 32853,
  113092, ..., ForgottenRealms-095 65057, Sombre Warhammer 132791: the commands are read; the
  description's text runs on to the next quote), but not for the skipping commands:
  ForgottenRealms-095 line 70735, `#msg "- The Darkstalker Wars Conclude - ` with `#removesite
  2995`, `#decscale1 0`, `#gold 125` on the next lines and the closing quote 6 lines down: one
  message of 7 lines (comment dropped), and the event has no #removesite, #decscale1 or #gold.
  (ModParser reads the three as effects, and its single-dash comment rule turns the `#msg`'s
  first line into a comment, leaving the message empty.)
  Confluence 1.06 line 89817, `#addname "Sigbert Markus von Schwertfeld` (no closing quote): the
  name is cut at 35 characters, `Sigbert Markus von Schwertfeld\n#add`, and the scan resumes
  inside the next line, so `#addname "Luitpold Volmar von Middendorf"` isn't read.
- A `#` inside a closed pair of quotes: text for the skipping commands, but read as a command in
  a description (Sombre Warhammer 105688 and Confluence 1.06 109059, `#descr "#descr "...`:
  the second `#descr` wins). ModParser reads the line as one `#descr` with the value `#descr
  "Chaos Spawn ... Daemons.`.
- Text after a closing quote: ModParser keeps it in the value (it trims the quotes at both
  ends). The game ends the value at the first closing quote: ForgottenRealms-095 line 60146,
  `#name "Shrine of the Undying Heart" inner sanctum of the cult of karsus and lair of
  Wulgreth`, is "Shrine of the Undying Heart"; line 47070 `#descr "Some dolphins ... fine
  scouts." These highly intelligent ...` ends at "scouts."; line 48278 `... very few became
  "adept in the arts. ..."` ends at "became ".
- `--` inside quotes: ModParser cuts the value there too, but the game then also loses the
  closing quote, so the text runs on to the next `"` (ForgottenRealms.dm, an older file in the
  same workshop item, line 33749: `#brief "'... Night Below.' -- except from Duerran dogma."` reads the brief up to the
  opening quote of the next line's `#descr`).
- Limits: Confluence 1.06 line 13958 `#name "Inscribe Greater Sigil of Enervation"` (36
  characters) is "Inscribe Greater Sigil of Enervatio" in the game; ForgottenRealms-095 has 8
  `#summary`/`#brief` texts over 499 characters (line 52010, ...).
- Agrees: several commands on a line, commands after a multi-line text's closing quote
  (Confluence 1.06 line 2547), message tags as text, case-sensitive command names. ModParser's
  single `-` comment isn't one for the game, but in these mods it only cuts text the game
  ignores too (after a number, after a closing quote) or can't read either (`#school - 1`,
  `#mon SHADAR-KAI`), apart from Confluence 1.06 line 109059 (the `#descr "#descr "` line,
  where it cuts the description at its first `-`).

## Game rules read from the code (6.37)

- `#clearmagic` empties only the magic-skill table. Magic boosts are abilities 10-22 on the
  monster, so they survive it.
- `#clearspec` clears the whole ability list (magic boosts and `#xpshape` included) and the
  flags word.
- `#clear` is `#clearmagic` + `#clearspec`, plus a reset of the base stats to defaults and of
  another block of slots. The name is kept. In detail (helper 0x14022a660(monster, stats,
  slots, name), which `#clear` calls with (1, 1, 0) and `#clearspec` with (0, 0, 0)): always the
  flags word and the ability list; stats: the 12 base stats, gold/resource costs, recruitment
  points, the second flags word (leadership classes, ...) and the body shape (humanoid); slots:
  the weapon and armor lists; name: the name (never, from a command). Sprites aren't touched.
  `#clearweapons` just ends the weapon list at slot 0.
- `#copystats` copies the name, base stats, every ability (so magic boosts, `#xpshape`,
  `#growhp`/`#shrinkhp`), the flags and slot blocks, and replaces the target's magic skills with
  the source's. A `#name` above `#copystats` is overwritten.
- `#djinn`, `#humanoid`, `#quadruped` and the other body types are read (they set item slots).
- Documented commands the parser never compares against: `#limitedregen`, `#statbreak`,
  `#domversion`. `#stunimmunity` (which the inspector exports) isn't a monster command.
- Generic handler arguments are read by following register constants from the previous call,
  including callee-saved registers a parser only ever sets to one value. Where the kind is
  kept in a register the parser also uses otherwise (sites, some spell and nation commands),
  it stays unknown and the command is treated as taking a plain number.
- Generic handler: a command with min = max takes no argument; kind 5 takes an optional one
  (default = the handler's last parameter); otherwise the argument is clamped and an offset
  added (`#incscale 1` stores 101, `#coldrec 2` stores 12). Tens digit 1: the command appends
  (repeatable, `#batstartsum1`), 2: it ORs into the current value.
- Abilities: with overwrite a value replaces the existing one and 0 removes it; repeatable
  commands append. The game reads the first of repeated non-repeatable ones (some vanilla
  monsters have two `#maxage`).
- `#eyes N` stores N - 2. `#horrormark` stores 5. `#sailing a b` stores a (999 if below 1) and
  b (10 unless 1-10) as two abilities. `#gemprod g n` is ability 30 + g. `#teleport` sets the
  teleport flag and map move 100; `#blink` only the flag. `#magicskill` takes paths 0-9 and
  50-52.
- Intrinsic resistances: `#fireres/#coldres/#shockres/#poisonres 100` set a flag instead of a
  value; any other value clears that flag. The flag counts 15, +10 for fire with the heat-aura
  flag or a heat aura, cold with the cold-aura flag or a cold aura, poison for undead or
  inanimate or with a poison cloud (the ability getter, 0x1401c5ae0). The heat/cold aura flags
  add 3 to `#heat`/`#cold`, which don't clear them. Spirit sight is 1 for horrors.
- Item slots: ability 182 (`#itemslots`, and set by `#mountedhumanoid`, `#quadruped`, ...),
  else 0xc0000 when `#noitem`'s flag is set, else 0xf2206; `#humanoid` and `#noitem` remove
  the ability. The body shape doesn't affect slots.
- Leadership: class + abilities 157 (`#command`) + 158 + 159 + 160. No command sets 160, the
  bonus 122 vanilla monsters carry (it doesn't apply to `#noleader` ones); `#command` doesn't
  clear it, so it's read-only rather than written as `#command`.
- Fixed names: game init copies all 417 vanilla fixed names (pretender gods included) into the
  table `#fixedname` writes.
- `#demon` and `#undead` also set the "almost undead" bit; 128 vanilla demons and undead lack
  it (written as `#almostliving`, which clears it).

## Weapons and armor (6.37)

- Weapon `#clear` (0x140227f90) sets the defaults: dmg 2, dt_normal, len 1, nratt 1, sound 7,
  no sprites, flags = full strength + a "not magic" bit that `#magic` clears.
- Weapons keep up to 7 (ability, value) pairs at +0x60 (setter 0x1402fd530): `#woodenweapon`
  268, `#iceweapon` 482, `#speedmult` 302, `#flail` 935, `#beam` 938, `#skip` 900, `#range0`
  930 = 100, `#range050` 930 = 50, `#melee50` 931 = 50, `#fireifhit` 911 etc. `#ironweapon`
  sets a flag bit and ability 266.
- `#range N` also sets len 0 and, if ammo is 0, ammo 12. `#explspr` always stores 9 frames
  (103 vanilla weapons have 5). `#secondaryeffect N` stores N, `#secondaryeffectalways N`
  stores -N in the same field. `#thirdstr` does what `#halfstr` does.
- Armor protection is a list of (body part, value): `#prot` sets the part(s) of its type
  (shield 5, helmet 1, else torso, arms and legs alike); `#protparts h b` sets head and body.
  Most vanilla body armor has a stronger torso than arms and legs; the game uses body and head
  protection only (the user), so it's written as `#prot <torso>` (or `#protparts <head>
  <torso>`). Part 6 (Twisting Thorns, Skull Necklace, Mail Barding, the bracers, Flame Helmet)
  has no command: modders reach it with `#copyarmor` of one of these, e.g. to give a helmet
  body protection. Those stay read-only. Armor abilities: 4 numbers at +0x48, values at +0x58
  (`#magicarmor` 557 = 1; 11 vanilla armors have 2).
- Commands the inspector writes that the weapon parser doesn't have: `#flammable` (is
  `#woodenweapon`), `#nofirebless` (`#iceweapon`), `#defnegate` (`#defroll`), `#mrcheckhalfdmg`
  (`#mrhalf`), `#usedinmelee`, `#dismounted`, `#hithead`, `#aironly`, `#demonimmune` (flag bits
  and abilities with no command: read-only).

## Items (6.37)

- Item `#clear` (0x1402290d0): main path 0 level 1, no second path, type 8, no flags.
- `#constlevel N` stores N / 2 (the manual's levels are 1, 3, 5, ...: written as 2s + 1).
  `#mainpath` also raises the main level to at least 1. `#type 9` stores type 6 plus ability
  1423 = 1, `#type 10` stores type 9. `#restricted N` appends ability 278 = N. `#spell` and
  `#autospell` are names at +0x30 and +0x54. `#magicboost` and `#gemprod` use the monster
  numbers; abilities go through the same generic handler.
- The restriction commands (`#nomounted`, `#noundead`, `#nofemale`, `#noinanim`, ...) OR a bit
  each into ability 1417.
- Record (0x210 bytes, table at 0x14318e910, "No Item" is item 0): name +0, construction
  level / 2 +0x24, main path +0x25, second path +0x26, main level +0x27, type +0x29. At start
  (0x140229190) every record after the game's items, up to 1999, is "an item" with level byte
  100 (free; 99 marks the end of the built-in list). `#newitem` takes the first free one from
  700 (0x140229150) and reads no number; `#selectitem N` takes any number (a free one becomes
  the mod's item once something is written to it); `#copyitem` copies the whole record
  (0x14023730e).
- Forging (0x1401ed910, used by the forge screen 0x1401fa290): -4 restricted to another nation
  (`#restricted`, ability 278) or `#notfornation` (279); -6 when the level (byte * 2 + 1) is
  above the nation's construction research, so `#constlevel 11` and up can't be forged; the
  screen lists one level per tab.
- Random items: 0x1402bd340 (a level, or -1 for any) and 0x1402bd420 (level, type, and an
  ability within a range) both skip free records and every item with `#restricted`. Callers:
  events' `#magicitem` (0x1401112f0: levels 0-3, re-rolled while the main level is 6+),
  "found a magic item" (0x1401846c0: levels 0-4), treasure texts "item"/"anything"
  (0x1401de386: levels 0-2 / 0-4), new commanders' misc items (0x1400c13ce: levels 0-3, type 8),
  0x1402b32c9 (level 3 only); at any level: a new commander's gear by tag (0x1400c0bc9: items
  with ability 567 equal to the unit's ability 566; no command sets 567: 9 game items such as
  the Dragon Pearls, the Crown of Ohya, the Slave Collar) and the arena's prize
  (0x140116667, "won the death match": items with `#champprize`, ability 259; the 7
  Champion's items). So an item with `#constlevel 11` is never forged nor picked at random,
  unless it has `#champprize` or ability 567. There's no command that removes a restriction
  (only `#clear`), so `#restricted` can't hide a copy that must be undone.
- The item ability getter has no intrinsic flags. Flag bits no command sets look like battle
  buffs: 0x1f8 bit 0x8 is what the inspector writes as `#airshield 80`.
- Compared with the inspector: it lacks `#hp`, `#itemdrawsize`, ~40 more abilities and repeated
  `#nationrebate`s; it expands `#magicboost 51` and `#elementrange`/`#allrange` into one line
  per path; it writes `#bers` for `#autoberserk` and `#type 9` where the record has 6 + 1423.

## Spells (6.37)

- `#newspell` reads no number: it takes the first free spell from 1500 (0x1401ad830: the first
  record from 1500 whose name starts with the empty marker 0xfe), whatever follows it on the
  line. `#selectspell N` takes any number from 1 to 7999, a free one too (a mod makes its
  numbered spells so). `#copyspell` (0x140246f29) copies the whole 0x118-byte record: name,
  school, everything.
- Spell `#clear` (0x140258af0): school -1, path 0 (fire) level 1, no second path, fatigue
  20, effect 2, range 5025, damage 10, nreff 1. `#path n p` / `#pathlevel n l` store at
  +0x26 + n / +0x28 + n. `#flightspr`/`#explspr` also store 1 / 9 frames. Abilities: 15 int32
  numbers at +0x64 with int64 values at +0xa0; `#restricted N` appends ability 278 = N.
- `#reqspellsinger`/`#reqtaskmaster`/`#reqseduce`/`#reqplant` append ability 718 = 616 / 379 /
  298 / 500 (`#reqno...` the same on 719).
- `#nogeosrc`/`#nogeodst`/`#onlygeosrc`/`#onlygeodst` clamp to 2^31 - 1: terrain bits above 31
  (vanilla Second Sun's 34359744512) can't be set by a mod.
- Compared with the inspector: it omits zero fields and `#sound`, `#flightspr`, `#strikesound`,
  `#spec2` and ~20 abilities; it writes commands the spell parser doesn't have
  (`#coldsummon`, `#uwsummon`, `#uniquetarget`, `#requiresench`) and other names for some
  abilities (`#onlyowndst` for `#onlyfriendlydst`, `#preventcast` for `#reqnoplant`,
  `#extraeffectgeo` for `#nextingeo`); `#casttime 100` where the record has no value.

## Sites (6.37)

- `#newsite N` keeps its number when it's 750-3999 (0x1402333bc).
- Site record: look +0x28, path +0x2a, level +0x2c, rarity +0x2e, 16 (ability, value) pairs
  from +0x30, terrain mask +0x130. `#gems p n` stores ability p + 1 = n.
- Vanilla sites use abilities no site command writes: 551/552/554/555 (the inspector writes
  them as `#summon`, which is 550) and 100-106 (written as `#decscale`, which is 102).
- Compared with the inspector: it lacks `#look`, `#claim`, `#gold`/`#minegold` on some, the
  per-path ranges, and 154 sites; it writes `#incunrest -50` for `#decunrest 5`.

## Nations (6.37)

- Nation record: name, epithet +0x24, era +0xac, 200 abilities (int32 numbers from +0xb0,
  int64 values from +0x3d0), the recruitment list +0xa10, the god list +0xab8.
- `#addreccom`/`#addforeignunit`/`#addforeigncom` insert into one int32 list with section
  markers -2/-3/-4 (`#addrecunit` before -2; -1 ends it). `#addgod N` appends N to the god
  list, `#delgod N` appends -N.
- Nation ability setter 0x14022a300; 0x140258820 removes an ability (`#startcom` first removes
  start units 91-96; `#clearsites` removes 52). `#startcom` 90, `#startunittype1-3` /
  `#startunitnbrs1-3` 91-96, `#startscout` 97, `#hero1-10` 139-148 (-1 removes), `#startsite`
  52 (appends), the rest through the generic handler.
- `#color`/`#secondarycolor`: three floats each (clamped to 0-1) at +0x94 and +0xa0, found
  from the branches' float stores; written with the shortest decimals that read back the same.
- `#epithet` reads up to 38 bytes, so it runs past its 36 into the 2 bytes before the
  abbreviation (+0x4a): Ind's "Magnificent Kingdom of Exalted Virtue" has 37 characters.
- The word at +0x90 is a status: `#name` turns -998 (an unused slot) into 0, and the game skips
  -998 nations; -999 ends the table (the record named "end", left out). Nations 114 ("Machaka
  xxxx") and 122 ("Oman") are unused slots with leftover data: written with `-- ro: status`.
- The nation's file name (+0x4f, "early_arcoscephale"; found from the code that formats the
  pretender files `newlords/<name>_N.2h`) has no command: `-- ro: file name`. The abbreviation
  isn't written.
- `#flag` and `#indepflag` load images: `#flag` replaces `flag.trs` image nation + 1 (through
  a table of replaced images the game keeps by archive and index), `#indepflag` images 0-5. The
  game builds the other nations' flags from parts and the nation's colors (Sprites below).
  `#nametype` is refused for nations ("#nametype cannot be used for nations"). The texts
  (`#descr`, `#summary`, `#brief`) live outside the record (Texts below) and aren't written.
- The other nation commands store abilities and are written when a nation has them (fort,
  temple and lab costs, `#idealcold`, `#likesterr`, `#fortera`, ...); a nation without one uses
  the game's default, which isn't a stored value.
- Compared with the inspector, which exports names, recruitment, heroes, start sites, home
  realms and cheap gods: the exe adds start units, defenders, wall and guard units, temple
  picture, fort era, god lists, AI and dominion settings (~100 commands per nation) and the
  colors. The inspector's 25 extra nations are empty slots it names `nation_35` etc. Against
  its `nations.csv`: name, era and file name agree on all 110 nations, the epithet on 109 (it
  has Ind's full epithet; the writer cut it at 36 bytes until 2026-10-06).

## Blesses, poptypes, nametypes and mercenaries (6.37, `vanilla_other.py`)

Each table is located from its parser's branches (the store each command makes, the record
size it multiplies by, the limit it compares with) and checked against an end marker or a
known value on every run.

- **Blesses** (93, numbers 0-92): `#selectbless N` picks record N (below 100) of a static table
  of 192-byte records: the name (32 bytes), `#path0`/`#cost0`/`#path1`/`#cost1` (int16), two
  battle-buff words, up to 7 (monster ability, value) effects and 4 (scale, value) bytes ended
  by 0xff. The scale commands jump to one setter with the scale's number (`#chaosscale` 0,
  `#slothscale` 1, `#coldscale` 2, `#deathscale` 3, `#misfortscale` 4, `#drainscale` 5,
  `#orderscale` 6, `#prodscale` 7, `#heatscale` 8, `#growthscale` 9, `#luckscale` 10,
  `#magicscale` 11; the parser compares `prodscale` twice). No command adds an effect
  (`#clearfx` only empties them), so effects and buffs are `-- ro:` lines: 138 effects (32
  named by the monster command that stores that ability and value, e.g. `#heat 3`; the rest,
  bless-only numbers, as "ability N") and 11 buff words. 550 is a marker: the effects after it
  apply only while the god is incarnated (the bless describer, 0x1400fe940, prints "(incarnate
  only)" for them; the effect lookup 0x1400fd7e0 can require an effect to come after it). 551
  isn't known: a bless with it goes on a nation-level list while a count (0x1400fec10) is
  below 5. `#cost1` also raises
  `#path1` to at least 0, so a bless without a second path gets neither. The record after the
  last is named "end". (The inspector has no bless data.)
- **Poptypes** (82, numbers 25-106): `#selectpoptype N` (0-249) picks entry N of two tables:
  recruitment (42 int32: units, -2, commanders, -1; `#addrecunit` inserts before -2,
  `#addreccom` before -1, `#clearrec` leaves -2, -1) and defenders (8 (key, value) int32 pairs
  ended by key 0: `#defunit1` 215, `#defmult1` 216, `#defcom1` 217, `#defunit1b` 218,
  `#defmult1b` 219, `#defunit1c` 220, `#defmult1c` 221). The game reads the first pair with a
  key (0x1402e9760), so a repeated key is `-- ro:` (5); keys 230 and 235 have no poptype
  command (3). Checked: Barbarians (25) recruit 139 and 140 with commander 141. Poptypes have
  no name in the game. (No inspector data.)
- **Nametypes** (67 lists, 14,532 names, numbers 100-168): `#selectnametype N` takes 100-499;
  each list is 1,500 name pointers ended by a pointer to "end" (`#addname` appends, `#clear`
  empties). The first `#selectnametype` a game reads empties lists 169 on, so 169-499 are free
  for mods; 127 and 128 are empty. The inspector's `nametypes.csv` has only labels (200 ids,
  from the manual's table): every exe list has an id there.
- **Mercenaries** (78): a table of 300 bands of 312 bytes; the vanilla ones are the records
  before the first whose `#level` byte is 99 (named "end"). There is no `#selectmerc`: a mod
  can't change a vanilla band. `#clearmercs` (a mod-level command read in the merc parser's
  entry chunk, with `#newmerc`; the catalog takes it from there) marks record 0 as the end, and `#newmerc` takes the first free record
  (`#eramask 7`, `#minpay 100` by default). So the vanilla bands are written as the `#newmerc`
  blocks that would make them; the editor shows them read-only. `#unit` sets `#nrunits` 10
  when it is 0, so `#nrunits` follows it. Up to 7 (nation, percent) pairs no command writes
  set a band's minimum pay for that nation (the hire price, 0x140224a00: `#minpay` x percent /
  100; other nations pay 100%, or the nation's `#merccost`): 90 `-- ro: minimum pay for nation
  N` lines on 50 bands (holy orders cost Ermor, Sceleria and Lemuria 300%, monkey bands cost
  the monkey nations 75%, ...). Against the inspector's `Mercenary.csv` (78 bands): all
  1,092 fields agree (name, boss, commander, unit, counts, level, pay, xp, equipment, rate,
  items, era mask).

## Texts (6.37)

- The game's texts aren't in the entity records. Two lists of string pointers in .data hold
  them: one for spells (0x1404fbb90, room for 16,000 entries, 3,379 used) and one for
  everything else (0x14051b3a0, 40,000, 8,707 used). Each text follows one or more key entries
  (`:Heavy Cavalry`, `:mon1712`, `:era1 Abysia`, `:details Encase in Ice`, `:portent Wild
  Hunt`); the list ends with `:end`. Some entries point past the file's bytes into the
  zero-filled part of .data: empty strings.
- Lookup (0x1401080d0, spells 0x1401084a0): build the key (with a prefix: `"%s %s"`, prefix and
  name), find the end key, then search back from it for a `::` key, then for a `:` key,
  ignoring ASCII case; the text is the first entry after the key's run of `:` entries. So the
  last of equal keys wins (21 keys repeat in the general list, 5 in the spell list), and a key
  shared by several entities (a name) gives them all the same text.
- Keys tried, in order: monster `mon<n>` then its name (the list has 475 `mon` keys, all for
  vanilla monsters, e.g. 208 War Shambler, whose name key has another text); item `item<n>` then its name (no
  vanilla item has an `item` key); nation `era<era> nation<n>` then `era<era> <name>`, and the
  same with `summary` and `brief`; spell its name, `details <name>`, `portent <name>`, `cure
  <name>`. A nation's era is the record's +0xac. An empty text means none.
- A mod's `#descr` (and `#summary`, `#brief`, a spell's `#details`, `#portent`, `#cure`)
  appends a `::` key and the text before `:end` (0x140228670, spells 0x1402288e0; the first
  one backs up the list; the key is copied into 52 bytes, the text into 2,000, so a longer mod
  text is cut), so a mod's text beats vanilla's and a later one beats an earlier one:
  `::mon%d`, `::item%d`, `::era%d nation%d`, `::summary%d %s`, `::brief%d %s` (a nation's
  summary and brief are keyed by its name, not its number), `::%s`, `::details %s`,
  `::portent %s`, `::cure %s` (spells: by name).
- `#copystats` copies the description: it looks up the source monster's and appends `:mon<n>`
  (one colon) with that text for the target.
- Sites have no texts (no site command writes one; no lookup reads one by a site).
- The game also looks up keys no entity command writes in the general list: `ab <...>`
  (abilities), `bless <...>`, `fort <...>`, `brief <...>`, `start story 1`, the age names and
  province values (`Unrest`, `Income`, ...).
- Found in vanilla (the exe's own tables): 4,104 monster descriptions, 528 item, 1,251 spell,
  346 details, 32 portents and 32 cures; 103 nation descriptions, summaries and briefs (the
  other 8 named nation records: Independents, the 4 Special Monsters slots, two unfinished
  nations, 114 and 122, and the table's end marker, 135).
- How it was found (`texts.py`), with nothing searched for by text: the `#descr`, `#summary`,
  `#brief`, `#details`, `#portent` and `#cure` branches of the parsers give each mod key format
  (a sprintf into a stack buffer) and the function they pass it to; that function loads the
  list, and the one other function that loads the list and checks entries for a second `:` is
  the lookup (its end key, capacity and `"%s %s"` come from it). Then every call of the lookup
  (directly, or through a name-only wrapper or a printf-like one) is followed back a few dozen
  instructions to see what it passes: a format (`mon%d`), a prefix (`details`), or a table
  record (an index times the record size plus the table's address: which table says whose
  name, +0xac in a nation record its era). A call whose result is tested and is followed by
  another lookup is a fallback. The lookups for a command are the ones in the same entity's
  table that also try the mod command's key without its colons.
- Compared with the dom6inspector's text folders (`unitdescr`, `itemdescr`, `spelldescr`): items
  all agree (528); monsters 4,017 of 4,033 shared agree (16 differ: older wording in the
  inspector's, or the name key's text where the `mon` key wins), the exe has
  71 more and the inspector 2 the exe doesn't (2192, 2193 Draugherse); spells 1,243 of 1,244.
- `texts-6.37.json` has the locations, never the texts: the .data section's mapping, each
  list's address, capacity, count and end key, each kind's keys, and the span of the file the
  editor reads (both lists and every string they point to, 2.5 MB) with its sha256. The editor
  uses the texts only if that checksum matches, so any other exe reads nothing.

## Spell effects and #damage (6.37)

A spell's `#damage` is a number the effect code interprets: a monster for a summoning, an
enchantment for a global, an event `#id` for 10042, bits for buffs and afflictions, and a plain
value (damage, years, gems) for the rest. A merge renumbers monsters, enchantments, event ids and
sites, so it has to know which; `spelleffects.py` reads it from the code that carries the effects
out (`data/spell-effects-6.37.json`, embedded in Dom5Edit as `Dom5Edit.GameData.spell-effects.json`).

**Where.** Each function is found by a string it prints (no addresses are hard-coded):
- Rituals (`#effect` 10000 and up): `castlabspell` (0x1401985c0, "castlabspell: bad spellnr"):
  the effect minus 10000 is compared one value at a time (`cmp r13d,N`; ranges as `lea eax,[r13-N];
  cmp eax,M`), each branch reading the record's `#damage` (+0x38) itself, or a level-scaled copy
  (`[rbp-0x58]`: 1000 and up is +1 per caster level).
- Combat spells land in `spellblastsquare` (0x1401caf10), which scales `#damage` by the caster's
  level except for the effects whose damage is an identifier (1, 10, 11, 21, 23, 31, 43, 54, 126,
  130, 144-150, 165, 166, 500-699), then calls `blastsquare` (0x1401b5ba0: the summons and clouds)
  with the effect and damage as stack arguments 7 and 5; per unit hit, `hitunit` (0x1401c5ec0;
  effect in r9d, damage in r8) does the rest.
- The sinks a `#damage` can reach: `resolve_summon` 0x1401cd6c0 ("*** Bad summondmg %d"),
  `unique_pick` 0x1401cea80, `newtempunit` 0x1400ac370, new units 0x1400a5a00 (both hand their
  first argument to resolve_summon), `newcom` 0x1400a5280, the polymorph 0x1401d22b0, `newench`
  0x1401a7fc0 ("newench %d by %s"), the spell event queue 0x1401c1e70 (read back by 0x14010ad50,
  "unknown spell event id %d", which looks for effect code 40, `#id`, in every event), `addfeatnr`
  0x1402ac0b0 (sites), `addunitfx`/`setunitfx` 0x1401a9290/0x1401a97e0, age 0x1401a8af0.
- A branch's `#damage` is followed back from each call (and each OR/AND/TEST into memory) along
  the branch's own instructions to the record's +0x38 or the damage argument; what the code alone
  can't name (years, a fort type, a gem path) is a note in `spelleffects.py`, checked against it.

**resolve_summon** (monster numbers): positive is the monster; -1 nothing; -2 .. -28 special picks
(random longdead, horrors, monsters with some ability; -26 becomes -27 or -28 by the province); -1000 and below
the monster tag -N (a random monster with `#montag N`, ability 637); -29 .. -999 nothing ("Bad
summondmg"). **unique_pick** (10089, 10114): 1-99 a key into hard-coded lists of uniques (1-14,
16-20 used: Bind Ice Devil 1, Heliophagus 3, ...; 15 and 21-99 summon nothing); 100 and up the
monster itself; negative through resolve_summon.

| `#damage` is | Effects |
|---|---|
| monster or tag | 1, 21, 31 (blastsquare), 43, 126 (border summons, newtempunit), 54, 165 (polymorph the target, hitunit); 10001, 10021, 10037, 10038, 10050, 10062, 10093, 10119, 10130, 10137 |
| monster (no tags) | 10026 (the mummy form; giants and some heroes get other hard-coded forms), 10141 (newcom of the monster **and the next number**: Call the Birds of Splendor summons the two Yllerion, 3382 and 3383; a merge has to keep such a pair in a row) |
| key 1-99, else monster or tag | 10089, 10114 |
| enchantment | 81, 133 (battlefield: the same numbers as the globals', `#req_ench` sees both), 10081, 10082, 10083, 10084, 10085 |
| event `#id` | 10042 |
| site | 10154 (added to the target province) |
| monster ability number | 500-599 (set to effect-499), 600-699 (add effect-599), 10500-10599 (on the caster) |
| bitmask | 10, 23 (buff words), 11 (afflictions: Web 536870912, False Fetters 131072, Slime 134217728, ...), 144-150 (cloud types), 10010, 10023 (caster's buff words), 10064, 10136 (afflictions), 10131, 10132 (afflictions cured) |
| a number | the damage effects (2, 3, 7, 24, 25, ...), 17 (morale), 67 (weakness), 101, 10101, 10111 (years), 162 (images), 10040, 10041, 10070, 10091, 10094, 10112 (damage), 10092, 10117, 10118, 10160, 10164 (counts, gold) |
| a code or index | 108 (where the target goes: -12 Inferno, -13 Kokytos), 10048 (gem path), 10063 (fort type), 10100 (terrain list 1-3), 10153, 10155, 10168 |
| not used | 0, 15, 20, 130 (no combat case), 166 (the new form is the target's `#animated`), 10019, 10022, 10030, 10034, 10035, 10039, 10044, 10045, 10049, 10052, 10053, 10057, 10068, 10076, 10077, 10079, 10090, 10095, 10098, 10102, 10110, 10113, 10115, 10116, 10125, 10127, 10135, 10152, 10156, 10157, 10161, 10163, 10167, 10169; 10086 has no branch at all (the spell does nothing) |

**Combat effects 1000-9999** (DomEnhanced's 6043 "6 turns border summoning", 4011 Slime Cloud, PS
Bloodwar's 2003): the AI's spell scoring reads them as effect % 1000 (evalspell 0x1401c3b08), and
the function that would set up a lasting effect (0x140124810, which fills the battle-sprite table
the round loop replays) is never called; blastsquare and hitunit have no case for them. So in 6.37
such a spell does nothing in battle beyond what the AI expects. Checked again 2026-10-07: casting
passes the effect on as written (spellblastsquare 0x1401cb5cf loads the 16-bit +0x2e into
blastsquare's eff argument), blastsquare makes a cloud only for effects 144-150 (0x1401b786c:
effect - 144 <= 6), hitunit compares the effect only with numbers up to 166 (and its damage path
takes only its sets and poison 7/139), and spellblastsquare treats #damage of any effect outside its
identifier sets as plain damage (level-scaled at 1000+). So Dom5Edit and the referee read them as
the game does: no case ("unhandled"), #damage a plain number (Gjallarhorn's 2531 isn't a unit);
the editor's report says so (SpellEffectData.NotHandledWhy). Dom6's clouds are 144-150 with #aoe.

**Compared with the inspector's tables** (`spell_effect_types.json`, which Dom5Edit used, and its
`spell_effects_mapping.json`, which writes ritual effects without their 10000: 1 for 10001, 85 for
10085): 10089/10114 were list keys only (DomEnhanced's 10089 spells name its own uniques, 7296
etc., and Confluence's 6894: in a merge they kept the old numbers); 10085 and 133 weren't
enchantments (DomEnhanced's 286 and 350-359, Confluence's 858); 165 and 6043 weren't monsters;
10154 wasn't a site; 120 and 10076 were lists of units and 10100 a list key (their units are
hard-coded, `#damage` unused or a key); 130 was a monster (no combat case; 10130 is);
10010/10023/10064/10131/10132/10136 and the clouds 144-150 weren't bitmasks (no harm: nothing
renumbered them).

## Events (6.37)

- Event table at 0x140639c60: 13,000 records of 2,920 bytes. Message +0 (2,400 bytes, so at
  most 2,399 characters), rarity +2400 (a signed byte), 12 requirement pairs +2408, 20 effect
  pairs +2600; a pair is two int64, (code, value). Vanilla events are records 0-3301; record
  3302 reads "end" with rarity 99.
- Requirements and effects are numbered separately: `#req_code` stores requirement 59,
  `#decscale3` effect 59. (The catalog's `a59` for both doesn't say which list.)
- `#selectevent N` picks record N. It ignores N above 12998 and records with rarity 99. The
  first `#newevent`, `#selectevent` or `#clearallevents` backs up the table, marks every
  record after the vanilla ones free (rarity 98, message "Insert event message here") and
  record 12999 as the end.
- `#newevent` takes the first free record from 3500 on, so mod events are numbered 3500, 3501,
  ... in the order the game reads them. Records 3302-3499 are only reachable with
  `#selectevent`.
- `#clear` empties the event: no pairs, the placeholder message, rarity 98 (free). The event
  never happens until a new `#rarity`, and a mod event left with rarity 98 (a `#newevent`
  block without `#rarity` too) is taken again by the next `#newevent`.
- Every other event command goes through the generic handler. A list holds 12 requirements
  and 20 effects; lines beyond that are dropped. A command appends its pair, or replaces the
  pair with the same code if it is not repeatable: most requirements replace (repeatable:
  `#req_code`, `#req_notcode`, `#req_anycode`, `#req_notanycode`, `#req_fornation`, the
  monster lists `#req_mnr`, `#req_nomnr`, `#req_targmnr`, ..., `#req_targorder`,
  `#req_targnoorder`, `#req_targpath1-4`, `#req_targnopath1-4`, `#req_var*`); most effects
  append (replacing: `#delay`, `#delay25`, `#delay50`, `#delayskip`, `#notext`, `#nolog`,
  `#header`, `#arena*`, `#resolvearena*`, `#setpoptype`, `#addascension`, `#minascension`,
  `#dispglobals`, `#newnbor`, `#remnbor`). Values are clamped to the command's range.
- The game reads a list up to its first empty slot (the event describers, the spell-event
  lookup by `#id`).
- `#delay N` plans record (this one + 1) N turns later; with `#delayskip p`, p% of the time
  record + 2. "The next event" is the next record number, not the next block in the file: a
  `#selectevent` block in between doesn't count.
- The event parser also compares `#2com`, `#4com`, `#5com`, `#1unit`, `#1d3units`-`#4d3units`,
  `#1d6units`-`#16d6units`, `#1d3vis`, `#1d6vis`, `#2d4vis`, `#2d6vis`, `#3d6vis` and
  `#4d6vis` by name (effects 151-153, 155, 156-159, 160-175, 14-19). All append (generic
  handler kind 10/11): nine `#1unit` lines give nine units. The catalog and the reading rules
  have them since 2026-10-11 (the name pattern needed a leading letter before).
- `#req_notnation` stores what `#req_notfornation` does, `#arena1` what `#arena` does.
- Vanilla stores 522 of its 20,095 pairs (2.6%) with codes no command writes: effect 190
  (gold with a random part, value x 1.0-2.0; the game's debug text calls it "gold" like
  `#gold`'s 42; 221 pairs), 39 (a value the next `#assassin` passes to its battle; 154),
  177/179/183 (18d6, 20d6, 24d6 units: one branch handles 160-187 as (code - 159)d6, the
  commands stop at 16d6; 68), 77 (`#researchaff`, documented but not read by the parser; 13),
  requirement 22 (`req_siege`; 14), 133 and 137 (`orpathearth`, `orpathglamour`), and 21 more
  codes used 1-7 times. They are `-- ro:` lines.
- Vanilla also has values a mod can't write: the same non-repeatable code twice (91, e.g. two
  `#req_monster` or two `#req_rare`: the game checks both), values outside the command's range
  (`#req_mydominion 2`/`3`, `#req_commander` with a monster number, `#req_noera 0`,
  `#req_story 3`), `#visitors` stored as 0 (the command stores 1), and one effect after an
  empty slot (3086, never read). The .dm marks each with a comment.
- Vanilla rarities the manual doesn't list: 14 (the two arena events), 15 (the two seasonal
  weather events), -9 (two empty placeholders, 3186 and 3204, message "...").
- Compared with the inspector's `events.csv` (3,300 events; it skips the two placeholders):
  rarity and the number of pairs agree on every event, and every code that occurs once in an
  event has the same value (77 after the inspector's 32-bit truncation). Where a code repeats,
  the inspector writes its first value each time (750 pairs). Messages agree except its
  Latin-1 reading of UTF-8 (25) and newlines (3). Some of its names are from an older
  numbering (effect 183 "16d6units", effect 91 "holyboost": 91 is `#bloodboost`).

## Sprites (6.37)

- A monster's sprite is the int at record +0x24 (written by `#newmonster`), an item's the int16
  at +0x2a (`#spr`, `#copyspr`). At start the game converts both tables in place (0x1401e2ad8
  for monsters, up to the -1 end marker; 0x140195449 for items, up to constlevel 99): a number
  below 1000 is an image index; n of 1000 or more becomes start(n / 1000) + n % 1000, where
  start(g) is the first image of the g-th group label in the archive (0x1400791a0 walks the
  labels; start(0) = 0). Monsters use `monster.trs` (archive 33 in the game's list), items
  `item.trs` (2). Monster 1, Logrian Slinger, 24036: group 24 is "man 2" (image 2863), so image
  2899.
- The unit draw code adds 1 for the attack frame. A vanilla unmounted sprite (ability 1017,
  262 monsters; `#unmountedspr1` removes it and sets 1135 to a mod image) is converted the same
  way when drawn (0x1401e3840).
- A site's picture (0x1401edf90): `sites.trs` group path + 1 (path clamped to 0-9: fire, air,
  water, earth, astral, death, nature, glamour, blood, holy), image look if look is 0-99, else
  image level (clamped to 0-3). Vanilla sites without `#look` have -1; `#newsite` zeroes the
  record (look 0, path 0, level 0; rarity -1, loc 0x303ff).
- A nation's flag is `flag.trs` (archive 8) image nation + 1 (`flags.py`, `"flag"` in the
  JSON). Images 1-5 (nations 0-4: Independents and the special monster slots; 0 is the unowned
  flag) are drawn as they are in the file. Every nation from 5 to 499 gets one built by the
  function the game's log calls "createflags" (0x140122b10; run at start and again after the
  mods are read, so a mod's colors count): image 501 (the pole), then image 502 (the cloth)
  with each channel times `int(c * 255) / 255` of `#color` (+0x94; float multiply, truncated),
  then image 503 (the cloth's border) times `#secondarycolor` (+0xa0), then for nations up to
  135 image 500 + nation (the emblem: Arcoscephale's caduceus, Ermor's eagle, T'ien Ch'i's
  roof, ...), each blended over the last by its alpha, then stored back as RGB565 (a pixel
  that comes out 0x0000 turns transparent). No record field picks the parts. The flag reads
  `#secondarycolor` as stored (the animated background, 0x1402b2d46, falls back to `#color`
  when it is 0 0 0; the flag doesn't), and a new nation's colors start at 0: a mod nation
  without colors shows a bare pole, one with colors a plain banner (no emblem above 135). The
  105 vanilla nations from 5 on (the unused slots 114 and 122 too) all get an emblem; the
  emblem images of unused numbers are mostly placeholders (a test pattern: 535-539, 545-549,
  583-584, 590-594, 622 for slot 122, 629-634, 636-640). A mod's `#newnation` takes the first
  unnamed record from 120 (0x1402296c0; 128 with only vanilla loaded), so it gets emblem 628
  or a placeholder unless it has a `#flag`. `#flag` replaces the built image afterwards. Checked by a contact sheet of all
  110 vanilla flags (`flags.py sheet`) and against the editor's flags (`flags.py check` and
  `Dom5Editor --snapshot --flags`, the same pixel checksum).
- `sprites.py` finds all of this from the code: the archive name list, the functions that
  subtract 1000 * (n / 1000) and the table field and archive each one uses, the single-number
  converter and the ability read before it, the site function by its clamps. Checked by contact
  sheets of decoded pictures against names (Moloch, Serpent, Elephant Rider, Fire Sword, Ice
  Lance, The Smouldercone, Swamps of Pythia, ...).

## Vanilla monsters compared with the inspector's vanilla.dm (6.37)

`vanilla` writes 4,136 monsters (the inspector's vanilla.dm has 4,091). Every flag bit a
command can set is written; 1,603 stored values have no command (read-only: 309 body shapes,
262 vanilla unmounted sprites, 122 leadership bonuses, 153 aura flags, 46 flags2 bits, and
abilities). Where the two files differ, by cause:

| Cause | Commands (monsters) |
|---|---|
| The inspector writes defaults or derived values the record doesn't hold | `#ressize` = size (3857), `#spiritsight` for horrors (22), leadership bonus folded into a class and `#command` (`#command` 109, leader classes 47) |
| The record holds values the inspector doesn't export | body shapes (`#quadruped` 287, `#miscshape` 190, ...), `#noitem` (581; the inspector writes only its slots), 153 fixed names, `#almostliving` (128), `#grandcom` 68, `#transformation` 64, `#bravemount`, `#icenatprot`, `#praise`, `#ainorec`, ... (~300), repeated `#batstartsum*`/`#armor`, 47 monsters |
| The inspector names a different command than the one that stores the ability | `#domsummon`/`#domsummon2` (22), `#battlesum5`/`#battlesum4` (4), `#summon5`/`#makemonsters5`, `#domimmortal` (is `#springimmortal`), `#secondshape` (ability 197 has no command; `#secondshape` is 194), `#stunimmunity` (no such command), `#magicskill 50 n` written as `#custommagic 32640 100n` |
| The inspector's value differs from the stored one | `#startage` (459: the inspector's is ~10% higher), `#fireres` +10 from a heat aura without the intrinsic flag (24), `#coldrec` (offset), `#itemslots` (10), `#templetrainer` (3) |
| Stored values no command reproduces (read-only here, written by the inspector) | `#heat 3`/`#cold 3` from the aura flags (153), `#horrormark` (3; stores 5), `#blessbers`, `#entangle` (stored 3, the commands store 1), `#stealthy -15` without the flag (4), `#sailing 5 0` (13) |
| Not found in the exe | `#startitem` (18), `#siegebonus` (1), `#eyeloss` (9) |

## Game updates: snapshot and changelog

`dom6exe.py snapshot --out DIR` keeps everything below from one exe in a folder (vanilla.dm,
events.dm without messages, the catalog, the reading rules, the spell effects table, a checksum
of each game text; `--texts` keeps the texts too, for local use only). `dom6exe.py changelog OLD
NEW [--out F.md]` compares two versions (snapshot folders, exes or vanilla .dm files) entity by
entity and writes patch notes: units by nation, every changed line with its command and the names
of what it refers to, events, which texts changed (never their words unless `--texts`), and the
modding side (commands, argument formats, text limits, spell effects). `changelog.py`;
`changelog_test.py` checks it; docs/GAME_UPDATES.md has the routine for a new version.

## Next

- Dom5Parser reads the `-- ro:` lines (`IDEntity.GameValues`); every page lists them.
- Bless effects and buff bits by name (they are battle buffs and monster abilities the bless
  gives sacred units); poptype defender keys 230 and 235; the nation abbreviation.
