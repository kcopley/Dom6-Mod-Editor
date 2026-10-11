#!/usr/bin/env python3
"""Adds a badge for every command the game reads that no badge section or page panel shows yet
(the "text only" commands tools/editor_coverage.py lists), to a "more" section at the end of the
type's badge file (Dom5Editor.Core/Data/<type>_badges.json). The badge's kind comes from the entity's
property map (Dom5Edit/Entities/<Type>.cs: a flag, a number, a reference to a monster/weapon/...),
its description from the manual (docs/pdf_extracted/commands_by_entity_clean.json).

    python3 tools/badge_fill.py [--dry-run]

Run it again after adding commands; badges already in a file are left alone.
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'tools'))
import editor_coverage as cov  # noqa: E402

# context -> (badge file stem, entity class file, manual section)
FILES = {'monster': ('monster', 'Monster', 'monster'), 'weapon': ('weapon', 'Weapon', 'weapon'),
         'armor': ('armor', 'Armor', 'armor'), 'item': ('item', 'Item', 'item'), 'spell': ('spell', 'Spell', 'spell'),
         'site': ('site', 'Site', 'site'), 'nation': ('nation', 'Nation', 'nation'),
         'merc': ('mercenary', 'Mercenary', 'mercenary'), 'event': ('event', 'Event', 'event'),
         'bless': ('bless', 'Bless', 'bless')}
# property class -> (badge type, reference type)
KINDS = {'CommandProperty': ('flag', None), 'IntProperty': ('int', None), 'BitmaskProperty': ('int', None),
         'IntIntProperty': ('intint', None), 'StringProperty': ('string', None), 'FilePathProperty': ('string', None),
         'MonsterOrMontagRef': ('ref', 'monster'), 'MonsterRef': ('ref', 'monster'), 'ShapechangeRef': ('ref', 'monster'),
         'WeaponRef': ('weaponRef', None), 'ArmorRef': ('ref', 'armor'), 'SpellRef': ('ref', 'spell'), 'ItemRef': ('ref', 'item'),
         'SiteRef': ('ref', 'site'), 'NationRef': ('ref', 'nation')}
SKIP = {'newmerc', 'newevent', 'selectevent', 'selectbless', 'end'}


def main():
    dry = '--dry-run' in sys.argv
    catalog = json.load(open(sorted(glob.glob(os.path.join(ROOT, 'Dom5Edit/GameData/game-commands-*.json')))[-1]))
    src = open(os.path.join(ROOT, 'Dom5Edit/Commands/Command.cs'), encoding='utf-8-sig').read()
    name_of = {m.group(2): m.group(1) for m in re.finditer(r'_commandMap\.Add\("#([a-z0-9_]+)", Command\.(\w+)\)', src)}
    manual = json.load(open(os.path.join(ROOT, 'docs/pdf_extracted/commands_by_entity_clean.json')))
    pages = open(os.path.join(ROOT, 'Dom5Editor.Core/UI/ViewModels/EntityPages.cs'), encoding='utf-8-sig').read()
    for ctx, (stem, cls, section) in FILES.items():
        entity_src = open(os.path.join(ROOT, 'Dom5Edit/Entities/%s.cs' % cls), encoding='utf-8-sig').read()
        prop = {name_of[c]: k for c, k in re.findall(r'_propertyMap\.Add\(Command\.(\w+), *(\w+)\.Create', entity_src) if c in name_of}
        descr = {}
        for entry in manual.get(section, []):
            descr.setdefault(entry['name'].lstrip('#'), entry.get('description') or '')
        path = os.path.join(ROOT, 'Dom5Editor.Core/Data/%s_badges.json' % stem)
        raw = open(path, 'rb').read()
        bom = raw.startswith(b'\xef\xbb\xbf')
        config = json.loads(raw.decode('utf-8-sig'))
        have = {c['name'].lstrip('#') for s in config['sections'] for c in s['commands']}
        cls_page = [c for t, b, c in cov.TYPES if t == ctx][0]
        panel = {name_of[c] for c in cov.page_commands(pages, cls_page) if c in name_of} if cls_page else set()
        game = set(catalog['contexts'][ctx]['commands']) - set(catalog['top']) - cov.STRUCTURAL - SKIP
        missing = sorted(c for c in game - have - panel if c in prop)
        if not missing:
            continue
        more = next((s for s in config['sections'] if s['id'] == 'more'), None)
        if more is None:
            more = {'id': 'more', 'displayName': 'MORE', 'description': 'Other commands the game reads for this type',
                    'renderer': 'badge', 'readOnly': False, 'commands': []}
            config['sections'].append(more)
        for c in missing:
            kind, ref = KINDS.get(prop[c], ('int', None))
            badge = {'name': c, 'display': c[0].upper() + c[1:], 'type': kind}
            if ref:
                badge['refType'] = ref
            text = re.sub(r'\s+', ' ', descr.get(c, '')).strip()
            if text:
                badge['description'] = text[:300]
            more['commands'].append(badge)
        print('%-8s +%d: %s' % (ctx, len(missing), ' '.join(missing)[:200]))
        if not dry:
            open(path, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + json.dumps(config, indent=2, ensure_ascii=False).encode('utf-8'))


if __name__ == '__main__':
    main()
