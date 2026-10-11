#!/usr/bin/env python3
"""Editor coverage: for each entity type, how the editor offers the commands the game reads
(Dom5Edit/GameData, from tools/dom6exe).

Every command the game reads is editable on an entity page: as a badge (a JSON badge section,
Dom5Editor.Core/Data/*_badges.json), in a panel (Dom5Editor.Core/UI/ViewModels/EntityPages.cs: commands a
type's page covers), or as text in the page's "other lines". This counts the commands that only
"other lines" offers (editable, but as raw arguments): the ones worth a badge or a panel.

    python3 tools/editor_coverage.py [--missing]
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
# game parser context -> (badge file stem, page class name)
TYPES = [('monster', 'monster', 'MonsterPageViewModel'), ('weapon', 'weapon', None), ('armor', 'armor', None),
         ('item', 'item', 'ItemPageViewModel'), ('spell', 'spell', 'SpellPageViewModel'), ('site', 'site', 'SitePageViewModel'),
         ('nation', 'nation', None), ('merc', 'mercenary', None), ('poptype', 'poptype', None),
         ('nametype', 'nametype', None), ('event', 'event', None), ('bless', 'bless', None),
         ('template', 'template', None)]
# shown by every page itself: name, description, copy and clear lines
STRUCTURAL = {'end', 'name', 'descr', 'copystats', 'copyspr', 'copyweapon', 'copyarmor', 'copyitem', 'copyspell', 'copysite',
              'clear', 'clearweapons', 'cleararmor', 'clearmagic', 'clearspec', 'clearrec', 'cleardef', 'cleargods',
              'clearsites', 'clearnation'}


def page_commands(src, cls):
    """Command.X names a page class refers to (its panels)."""
    m = re.search(r'class %s\b(.*?)(?=\n    public (?:sealed )?class |\Z)' % cls, src, re.S)
    return set(re.findall(r'Command\.([A-Z0-9_]+)', m.group(1))) if m else set()


def main():
    catalog = json.load(open(sorted(glob.glob(os.path.join(ROOT, 'Dom5Edit/GameData/game-commands-*.json')))[-1]))
    top = set(catalog['top'])
    src = open(os.path.join(ROOT, 'Dom5Edit/Commands/Command.cs'), encoding='utf-8-sig').read()
    name_of = {m.group(2): m.group(1) for m in re.finditer(r'_commandMap\.Add\("#([a-z0-9_]+)", Command\.(\w+)\)', src)}
    pages = open(os.path.join(ROOT, 'Dom5Editor.Core/UI/ViewModels/EntityPages.cs'), encoding='utf-8-sig').read()
    show_missing = '--missing' in sys.argv
    print('%-9s %6s %8s %8s %11s' % ('type', 'game', 'badges', 'panels', 'text only'))
    for ctx, badge_stem, cls in TYPES:
        game = set(catalog['contexts'].get(ctx, {}).get('commands', [])) - top - STRUCTURAL
        badges = set()
        path = os.path.join(ROOT, 'Dom5Editor.Core/Data/%s_badges.json' % badge_stem)
        if os.path.exists(path):
            for section in json.load(open(path, encoding='utf-8-sig'))['sections']:
                badges |= {c['name'].lstrip('#') for c in section['commands']}
        panels = {name_of[c] for c in page_commands(pages, cls) if c in name_of} if cls else set()
        text_only = sorted(game - badges - panels)
        print('%-9s %6d %8d %8d %11d' % (ctx, len(game), len(game & badges - panels), len(game & panels), len(text_only)))
        if show_missing and text_only:
            print('          ' + ' '.join(text_only))


if __name__ == '__main__':
    main()
