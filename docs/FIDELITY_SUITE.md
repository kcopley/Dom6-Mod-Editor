# Fidelity Suite

Automated, end-to-end checks that Dom5Parser preserves mod data, judged by what the
**dom6inspector holds in memory**, not by file text. File differences are expected
(ordering, formatting, names vs IDs). The inspector's parsed data must be identical, or
differ only in ways we've explicitly listed as expected.

**Runner:** `tools/fidelity/run.mjs` · **Config:** `tools/fidelity/suite.json` ·
**Baselines:** `tools/fidelity/baselines/` · **CI:** `.github/workflows/fidelity.yml`

**Last Updated:** 2026-10-07

---

## The stages

| Stage | Question | How | Compared at |
|---|---|---|---|
| **1. Inspector self-check** | Can the oracle write back out everything it read from a mod? | Inspector loads vanilla + mod, exports every entity the mod touched in full, after `#clear` (`export-mod.js`), reloads the export | **final**: after the inspector's full post-processing (functional equality) |
| ~~**2. Vanilla data**~~ | Retired 2026-10-05: `vanilla.dm` is written from Dominions6.exe by `tools/dom6exe` (README there compares it with the inspector's), so the inspector's own export isn't checked any more | — | — |
| **3. Save fidelity** | Does saving in Dom5Parser change any data? | `Dom5Tests roundtrip` (load → save) → oracle compares original vs saved | **parse**: strict, right after parsing (catches even a dropped command that only restated a default) |
| **4. Edits** | Does an edit change exactly what it should, and nothing else? And does undo put it all back? | `Dom5Tests edit` applies scripted edits → oracle compares an unedited save vs the edited save → must match the case's `expect` list exactly; then every edit undone (save = the unedited save, byte for byte) and redone (= the edited save) | parse |
| **5. Stress** | Do a few hundred edits of every type at once change exactly what each one should? | `tools/fidelity/stress.mjs` generates them from the oracle's view of a big mod (seeded), each with the change it implies → one `Dom5Tests edit` session → the oracle's differences must be exactly their union; undo all / redo all byte for byte | parse |
| **6. Merges** | Does a merged mod read in game as its parts one after another (each as when it's alone where they'd interfere)? | `Dom5Tests merge` (suite.json `merges`; docs/MERGING.md) reads the merged file back: nothing defined twice that wasn't, every entity one part alone has with its part's values, every event; then `gameread.py --merge` replays the game's reading of the parts against the merged file, numbers mapped through the moves (and the snapshots' lines), numbers the merged file's spells, items and nations as the game does (an unnumbered `#new...` takes the first free one) and flags blocks of two independent parts on one number, and checks units chained by number (#shrinkhp, #growhp, #xpshape) keep their neighbours. The counts (numbers moved by kind, copies kept apart, dangling references kept, things across parts) are a baseline in `baselines/<case>.stage6.json`: a change shows as CHNG, not a failure | the game's reading |

Stages 4 and 5 compare against an *unedited Dom5Parser save*, not against the original mod.
That isolates the edit from any existing save losses, which stage 3 tracks separately.
Once stage 3 is clean, the two are equivalent. They take the oracle's parse of each file
(`roundtrip_check.js --snapshot`: its normalized data) and compare them the way
`roundtrip_check.js` does, so the unedited save is parsed once for all the cases on it.

The oracle is the kcopley/dom6inspector fork (`scripts/headless/`, see its README), pinned
for CI by `tools/fidelity/oracle-ref.txt`.

## Running it

```bash
# prerequisites: build Dom5Tests (Debug: the suite runs bin/Debug); a checkout of kcopley/dom6inspector (branch export-test)
"/mnt/c/Program Files/dotnet/dotnet.exe" build "D:\\Projects\\Dom5Parser\\Dom5Tests\\Dom5Tests.csproj"

node tools/fidelity/run.mjs --quick          # stages 3, 4 and 6's fixture merges, ~2 min: the everyday loop
node tools/fidelity/run.mjs                  # all stages (stage 1 is slow, see below)
node tools/fidelity/run.mjs --stress         # stage 5 alone (~1 min); --seed N for another set of edits
node tools/fidelity/run.mjs --stress-mod MOD.dm   # stage 5 on any mod (repeatable), instead of suite.json's
node tools/fidelity/run.mjs --only e04       # a single case (substring match)
node tools/fidelity/run.mjs --update-baselines   # accept current counts as the new baseline
```
Options: `--oracle DIR` (or env `DOM6INSPECTOR`; default looks in `../dom6inspector` and
`C:\Projects\dom6inspector`), `--stages 1,3`, `--jobs N` (checks run in parallel, default half
the cores, at most 6), `--seed N` / `--stress-size N` (stage 5; default seed 1, 300 edits, from
`suite.json` `stress`), `--json summary.json`. Work files land in `Dom5Tests/bin/fidelity/`, which
is gitignored. Every check writes its oracle report there (stage 4/5: `<case>.edit.report.json`,
the generated stress edits: `stress-<mod>-<seed>.json`).

Stage 1 needs the inspector's full post-processing, about 50s per load: `MNation` takes
~22s and `MSite` ~19s, from nested loops over all events and units for each site. So it's
skipped by `--quick` and runs in CI, after oracle or `vanilla.dm` changes, and weekly, with the
stress run (stage 5, about a minute).

## Results and policies

| Status | Meaning |
|---|---|
| `PASS` | zero differences, or for baseline cases, no count rose |
| `PASS+` | baseline case improved; run `--update-baselines` to lock it in |
| `FAIL` | unexpected differences, or a baseline count rose |
| `xfail` | listed as known-failing, with a reason |
| `XPASS` | known-failing case now passes; remove its `knownFailing` |
| `NEW` | baseline case without a baseline file yet |

**Expected differences:** values a mod command cannot write are listed with the reason in
`suite.json`: `vanillaExpected` (stage 2c) and `stage1Expected` (stage 1, e.g. game
attributes with no command, which the full re-export's `#clear` removes). They are counted
and printed separately and don't fail a check.

**Baselines (ratchet):** large real mods (DomEnhanced) can't be at zero yet, so their
per-field difference counts are committed. The suite fails if any field's count rises, or
a new field appears. Fixes lower the counts, and `--update-baselines` records the
progress. Commit baseline changes with the fix that caused them.

## Adding cases

**A mod** (stages 1 and 3): add it to `suite.json` with `"expect": "pass"` (must be identical)
or `"baseline"`. Mark a stage known-failing with
`"knownFailing": { "3": "reason" }`.

**An edit case** (stage 4): add `Dom5Tests/fixtures/edits/<name>.json`:
```json
{
  "description": "What this checks.",
  "base": "base_types.dm",
  "edits": [ { "op": "set", "entity": "monster", "id": 5101, "command": "#hp", "value": "12" } ],
  "expect": [ { "type": "unit", "id": "5101", "field": "hp", "from": "9", "to": "12" } ],
  "reread": [ { "entity": "nation", "id": 160, "command": "#startcom", "values": ["5101"] } ],
  "knownFailing": "optional reason"
}
```
- `op` (the editor's operations, `Dom5Edit.Editing.ModEditor`, as the GUI makes them):
  - `set` sets a single-valued command (in place if the entity has the line, else a line that
    overrides the inherited value); on a repeatable one (`#weapon`) it adds an entry.
  - `add` adds one more entry of a repeatable command (or sets a single-valued one).
  - `remove` removes every value of the command, or only those whose argument (or referenced
    ID) is `value`, until the entity has none in game: an inherited one by `#x 0` or by the
    group's clear and the rest added back.
  - `change` changes the value whose argument is `from` to `value`.
  - `reset` drops the entity's own lines for the command (back to what it inherits).
  - `text` edits the entity's block as text (the page's "in the file" box), applying
    `replace` pairs `[find, replacement]` to it.
  - `create` makes a new entity (`name` optional) and names it with `as` for later edits
    (`"ref": "<as>"`) and expectations; `delete` deletes one (or the mod's changes to a vanilla one);
    `move` moves one of the entity's own lines (`command`, optional `value`) by `delta` or to just
    `before` another command's line.
  - `"fails": "text"` on an edit: the editor must refuse it with a message containing the text.
- `entity`: `monster`, `weapon`, `armor`, `item`, `spell`, `site`, `nation`, `merc`, `event`,
  `nametype`, `poptype`, `bless`, `template`, addressed by `id`; an entity with no number
  (`#newevent`, `#newmerc`) by `index` (the mod's Nth one in file order, 0 first) or `match` (the
  one event whose `#msg` contains the text; another entity by name, as a `#select` by name finds
  it); a created one by `ref`. Vanilla entities are selected into the mod on the first edit
  (copy-on-write).
- `value` is the argument as written in a `.dm` file. Surrounding quotes are optional. `@<as>` in
  a value (and in an expectation) is the number a created entity got.
- `expect` uses the **inspector's** field names and values (its `scripts/parsemod.js`):
  references as IDs, lists as arrays (a nation's lists as sorted sets), unset as `null`.
  `from`/`to` can be omitted to accept any value. A new entity:
  `{ "type": "unit", "ref": "m", "new": { "hp": "15" } }` (it must be only in the edited save, with
  at least those values); a deleted one: `{ "type": "wpn", "id": "1201", "deleted": true }`. The
  oracle numbers events (and a band with no number) itself: the mod's events after the game's
  (3302, 3303, ... on these bases), a `#newmerc` "". Write expectations from intent, never by
  copying observed output; where the oracle's form of an unchanged game value differs, say why
  in a `note`.
- `reread`: values checked by re-reading the edited save with Dom5Parser, for what the oracle
  doesn't read (start armies, defenders, nametypes, poptypes, blesses, an event's line order):
  `command` with `values` (every value in game, in order; a reference matches its ID),
  `includes` / `excludes`, or `order` (commands of the entity's lines as saved, in this order).
  Not independent of Dom5Parser: it shows the edit reached the file and reads back as made.
- Every case also runs undo all / redo all: `Dom5Tests edit ... undo` saves `<out>.undo.dm` and
  `<out>.redo.dm`, which must equal the unedited and the edited save byte for byte.

**Stress** (stage 5): `suite.json` `stress` lists the mods (`seed`, `size`). The generator
(`tools/fidelity/stress.mjs`) works only from the mod's text (which entities it writes, what
its copies copy) and the oracle's parses of the unedited save and of vanilla. Each edit's
expected change follows from the edit: a stat set to N is N; an added weapon is appended; a
removed one is gone; a nation's recruit set gains one; a reset stat is the game's value; a
deleted `#select` is the game's entity again; an inherited ability removed is `"0"`. Copy sources
are never edited (their copies would change too), and an entity gets at most one edit per field.

## What the edit tests cover

Bases: `base_edits.dm` (monster templates and copies: e01-e19) and `base_types.dm` (every type
the oracle compares, each as a mod's own entity, a vanilla one the mod `#select`s, and a copy;
references across types; mounted units: t01-t30) and `base_empty.dm` (a mod built from scratch
in the editor, saved whole since it has no blocks to follow: t31). Every case also checks undo
all and redo all.
"new / sel / untouched" = the edit is made on a mod's `#new` entity, a vanilla entity the mod
selects, one it doesn't touch (copy-on-write).

| Type (oracle) | set | add / remove an entry | change a reference | reset | remove inherited | text | copies (rule C, a copy's own value) | create / delete | new / sel / untouched |
|---|---|---|---|---|---|---|---|---|---|
| unit | t01 t03 t04 e01-e03 e06 | t01 t02 t03 t04 e04 e05 e09-e11 (weapons, armor, flags, paths) | t02 (armor) t04 (armor) e15 | t23 t02 | t04 (weapon, armor, `#fear 0`, path) t05 e12-e14 e16 | e18 e19 | t05 e07 e11 e17 | t11 t25 | ✓ ✓ ✓ |
| unit, mounted | t06 t07 t08 (riding skill, riders), t11 (rider sprites `#unmountedspr1/2`) | t07 t08 (mount: `#mountmnr 0`) | t06 t07 t08 (mount, co-rider) | — | t08 (mount) t09 (a flag: mount kept) t10 (xfail: rider sprite lost) | — | t06 (to the copy) t08 t11 (`#copystats` of cavalry) | t11 t25 (new rider) | ✓ ✓ ✓ |
| unit, mount | t06 t07 (its own stats: riders unchanged) | t02 | t02 | — | — | — | — | — | ✓ — ✓ |
| wpn | t12 | t12 (flags) | — | t23 | t13 (`#clear`, rest added back) | t24 | t12 t23 | t25 | ✓ ✓ ✓ |
| armor | t14 | t14 (magic armor) | — | t23 | t13 | t24 (unchanged) | t14 | t25 | ✓ ✓ ✓ |
| item | t15 | t15 (nation restriction) | t15 (weapon, armor) | t23 | t30 (weapon) | t24 | t15 t30 | t26 | ✓ ✓ ✓ |
| spell | t16 (level, cost, school) | t16 (restriction) | t16 (summoned unit `#damage`, path) | t23 | t30 | t24 | t16 t30 | t26 | ✓ ✓ ✓ |
| site | t17 | t17 (home/summoned units) | t17 | t23 | t17 t30 | t24 | t17 t30 | t25 | ✓ ✓ ✓ |
| nation | t18 (era) | t18 (recruits, commanders, pretenders, start site) | t18 (hero) | t23 | t19 (`#clearrec`, rest added back) | t24 | — | t26 | ✓ ✓ ✓ |
| nation start army, defenders | t18 t22 (re-read) | — | t18 (re-read) | — | — | — | — | — | ✓ ✓ ✓ |
| merc | t20 | t20 (items: re-read) | t20 (commander) | t23 | — (no inheritance) | t24 | — | t27 | ✓ (refused) — |
| event | t21 (rarity, message) | t21 (requirements) | t21 (commander, target, units, code) | t23 (game event) | — | t24 t28 | — | t21 t25 | ✓ ✓ — |
| event line order | t21 (`move`, re-read) | — | — | — | — | t24 (re-read) | — | — | ✓ — — |
| nametype, poptype, bless | t22 (re-read) | t22 | — | — | t22 (poptype `#clearrec`) | — | — | t22 (a poptype; a bless: refused) | ✓ ✓ ✓ |
| mod header | t29 | — | — | — | — | — | — | — | |
| a mod from scratch | t31: every type made and filled in, referring to each other (rider on a new mount, its weapon and armor; item, spell, site, nation, band, event using them), a vanilla unit given the new mount | | | | | | | t31 | |

Stage 5 runs every row's kinds of edit at once (except the oracle-blind ones it re-reads, and
merc only where the mod has a band), ~300 per mod: on DomEnhanced (3,486 new monsters, 1,698
events, ...) and on `base_types.dm`, edits in a shuffled order. Kinds of edit (counts per run
vary with the seed): unit stats (also as text), weapons and armor added and removed, flags,
paths, abilities removed (`#x 0`), resets to the game's value; riders' mounts changed or
removed, co-riders, riding skill, mounts' own stats; weapon, armor, item, site, spell stats
(cost split by the oracle into fatigue and gems), item weapons, summoned units, site units,
nation recruits/commanders/pretenders added and own recruits removed; event rarity (also as
text), requirements and effects changed in place, messages, a line moved (re-read); the mod's
changes to vanilla entities deleted (back to the game's); new units (with a mount), weapons,
armor and sites; start commanders, nametypes, poptypes, blesses (re-read).

Not compared (the oracle doesn't read them; re-read instead): a nation's start army
(`#startcom`, `#startunittype*`) and defenders (`#defcom*`, `#defunit*`, `#wallunit`, ...),
nametype names, poptypes, blesses, templates, an event's line order. `#mountedspr1/2` isn't read
by the game (not in Dominions6.exe's parser): only `#unmountedspr1/2` is tested.

## Current state (2026-10-07, oracle ff69107, game 6.37)

| Stage | Case | Result |
|---|---|---|
| 1 | 5 copy fixtures, duplicate names, edits-base | pass |
| 1 | edits-base-types | xfail: the oracle's export leaves out a mod's `#selectevent` of a game event |
| 1 | DomEnhanced 2.13 | baseline **6** unexpected differences (8,945 at first, 6,269 on 2026-10-04), plus 152 fields listed in `stage1Expected` (game attributes with no mod command). Every touched entity is now re-exported in full after `#clear`, so the export has to carry every value itself. |
| 3 | copy fixtures, duplicate names, edits-base, edits-base-types | pass |
| 3 | 5 mods made from scratch in the editor (`TestDMs/created`, 2026-10-07), every line regenerated | pass |
| 3 | DomEnhanced 2.13 | baseline **0** (the save is byte-identical); regenerated: 2 (baseline 6) |
| 4 | e01-e19, t01-t31 (50 cases) | pass, except t10 (xfail: a vanilla rider loses its rider sprite when an inherited flag is removed) |
| 5 | DomEnhanced 2.13 and base_types, ~300 edits each, seed 1 | pass (seeds 1-25 pass); ~40 s |

Timing (12 cores, 6 jobs): `--quick` ~1.5 min (60 checks), `--stress` ~40 s, full run ~9 min.

What changed in the oracle to get stage 1 there is in the fork's `docs/EXPORT_RULES.md`. In
short, the inspector held the same data in two forms: the game data's, and the parser's for
the mod command. Some game data was also applied after mods were read. Both are now aligned.
`vanilla.dm` gained data it never had:
- leadership for over 2,000 units (the tables had Dom5's scale);
- 1,468 spell effects plus spell range and area;
- ritual gem costs;
- slow recruitment;
- site summons;
- item spells;
- item slots for every unit.

Dom5Parser loads it with the same 127 warnings as before.

## Open findings (need decisions or work)

1. ~~`vanilla.dm` carries display values~~ **Resolved 2026-10-04.** The exporter now
   writes the game's base values. Rules, evidence and expected differences are in the
   fork's `docs/EXPORT_RULES.md`. Highlights:
   - map move: 3,171 units fixed (Serpent Cataphract 14 -> 18);
   - resource cost: 228 fixed;
   - national spell restrictions: they were missing entirely, 852 lines now;
   - `#magicarmor`: missing on all 175 armors;
   - affliction weapons: exported as plain damage (`#dmg 256` without `#dt_aff`);
   - weapon damage types: `#dt_*` from the game data's own annotations
     (placeholders 190 -> 37);
   - nations: first hero, multiheroes and underwater commanders were lost; Dom6
     `#uwrec`/`#coastrec` forms now used;
   - no defaults or display tags written as data;
   - no `%` or `NaN` in values.
2. ~~Live templates (stage 4 `e07`)~~ **Resolved 2026-10-05** by original-order saving
   (`SAVE_FLOW.md`): the edit is saved in the template's block, so copies made after it carry
   it. e09-e11 test where edited and added properties are saved.
3. ~~`name_before_copy` (stage 3)~~ **Resolved 2026-10-05** the same way: saved in place.
4. **Game attributes with no mod command.** About 120 fields (`stage1Expected`). Settled from
   the game's parser (tools/dom6exe): `tightrein` is `#undisleader`; `aboleth`, `popspy` and
   `landenc` are other abilities than `#mindslime`, `#spy` and `#landdamage`.
5. ~~Inspector: copies don't inherit attributes~~ **Resolved 2026-10-05.** Game attributes are
   applied before mods are read.
6. ~~Dom5Parser: read-only vanilla lines~~ **Resolved 2026-10-05.** Dom5Parser knows from the
   game's parser which commands each entity type reads (`GameCommandCatalog`); others are
   flagged `NotReadByGame` and shown read-only. About 25 warnings still come from
   `##placeholders##` in event message text being parsed as commands.
7. **Upstream candidates** for larzm42/dom6inspector (all fixed in the fork):
   - the `#uwcom`/`#coastcom`/`#coastrec`/`#uwrec` crashes and overrides;
   - `#dt_aff` double decoding;
   - armor `type` erased for mod armors;
   - `#prot NaN`;
   - post-processing performance.
8. ~~4 mercenaries not re-imported~~ resolved.
9. **Found by the edit tests (2026-10-07), fixed:**
   - an item's `#weapon`/`#armor` were treated as lists (as a monster's): changing a vanilla
     item's weapon wrote `#clear` and the whole item again, losing what no command sets (Bow of
     the Titans: strength required 18) (stress; `t15`);
   - a block edited as text rewrote its unchanged multi-line `#msg` with LF line ends in a CRLF
     file: another message to the game and the oracle (stress on DomEnhanced; `t28`);
   - a nation made in the editor was saved as `#newnation 150`, which takes no number in Dom6
     (the game uses the first free one from 120): now `#selectnation 150`, as spells and items (`t26`).
10. **Open: removing an inherited flag from a vanilla rider loses its rider sprite** (`t10`,
    xfail). The rider's sprite without its mount is a game value (ability 1017); `#clearspec`
    clears it and only `#unmountedspr1` (an image file) sets one. The same holds for any
    read-only game value `#clearspec`/`#clear` removes (an item's or site's): the editor
    should warn before such a rewrite.
11. **Oracle limits met by the edit tests:** a second `#newmerc` without a number is rejected
    ("id already in use"; the first is numbered ""), so bases have one band; nation start
    armies, defenders, nametype names, poptypes and blesses aren't read (re-read instead);
    `#mountmnr` isn't resolved by name and `#coridermnr` names aren't canonicalized (tests use
    numbers); `#protinspector` (not a game command, 22 in DomEnhanced) is read as `#prot` by
    truncation; `export-mod.js` leaves out a mod's `#selectevent` of a game event (stage 1 of
    `edits-base-types`: xfail); the oracle's `#clearspec` drops `ressize` even when it only restates `#size` (t09/t10
    expect it, with a note); a band's items are one field (the last `#item`).
