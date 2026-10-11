#!/usr/bin/env python3
"""
Groups a type's ability badges by the modding manual's sections (command_hints.json), so a
page reads "Movement", "Stealth & assassination", "Combat", ... instead of a long "General"
list. Each badge keeps its own definition (display name, kind, colors); sections the editor
draws itself (stats, magic paths, clears) are kept as they are.

Usage: python3 tools/badge_sections.py   (rewrites Dom5Editor.Core/Data/monster_badges.json and item_badges.json)
"""
import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, 'Dom5Editor.Core', 'Data')

# the manual's monster sections, merged into groups a page can show (in this order)
MONSTER_GROUPS = [
    ('creature', 'CREATURE TYPE', ['Creature Type & Status', 'Bodytype', 'Basic Commands', 'Visuals', 'Mounts & Riders', 'Mount commands', 'Item Slots', 'Weapons & Armor', 'Basic Attributes']),
    ('movement', 'MOVEMENT', ['Movement']),
    ('stealth', 'STEALTH & ASSASSINATION', ['Stealth & Assassination']),
    ('combat', 'COMBAT', ['Combat Abilities', 'Combat Auras']),
    ('resistances', 'RESISTANCES & PROTECTION', ['Damage Reduction', 'Immortality']),
    ('magicabilities', 'MAGIC', ['Other Magic Abilities', 'Spell Range', 'Magic Research', 'Gem Production', 'Magic Paths', 'Elemental & Scale Powers', 'Seasonal Powers']),
    ('leadership', 'LEADERSHIP & MORALE', ['Normal Leadership', 'Magic Leadership', 'Undead Leadership', 'Morale Related Abilities']),
    ('summoning', 'SUMMONING & SHAPES', ['Monster Summoning', 'Shape Changing']),
    ('aging', 'AGE, HEALING & AFFLICTIONS', ['Age, Healing & Afflictions']),
    ('noncombat', 'NON-COMBAT', ['Non-Combat Abilities']),
    ('recruitment', 'RECRUITMENT & COST', ['Recruitment Rules', 'Desertion', 'Gold & Resource Cost', 'Nametypes']),
    ('pretender', 'PRETENDER', ['Pretender God Commands']),
]
OTHER = ('more', 'OTHER ABILITIES')

# badge sections whose commands are regrouped (the rest, drawn by the editor, stay)
REGROUPED = {'types', 'general', 'combat', 'resistances', 'more', 'creature', 'movement', 'stealth',
             'magicabilities', 'leadership', 'summoning', 'aging', 'noncombat', 'recruitment', 'pretender'}


def group_for(name, hints, group_of):
    """The group of a command: its manual section's; for a numbered one, its family's (#summon2:
    #summon1); else by its name (#fireattuned: magic, #forestrec: recruitment)."""
    section = (hints.get(name) or {}).get('section')
    if section is None:
        family = re.sub(r'\d+', '1', name)
        section = (hints.get(family) or hints.get(re.sub(r'\d+(d\d+)?$', '1', name)) or {}).get('section')
    if section in group_of:
        return group_of[section]
    for pattern, group in [(r'attuned$|^casttime$|^magiconly$|^polygetmagic$|scale$', 'magicabilities'),
                           (r'rec$|recpt$|cost\d*$|^restricted$|^onlymnr$|^notmnr$|^natmon$', 'recruitment'),
                           (r'sail', 'movement'), (r'aging', 'aging'), (r'^battlesum|^batstartsum|^summon|^makemonsters', 'summoning'),
                           (r'^stealth|^startscout$', 'stealth'), (r'unrest$|income$|supply$|^likespop$|^tradecoast$', 'noncombat')]:
        if re.search(pattern, name):
            return group
    return OTHER[0]


# items: the item chapter's sections; the monster abilities an item grants ("Monster Modding
# Commands") by the monster group they're in
ITEM_GROUPS = [
    ('itemmagic', 'MAGIC & SPELLS', ['Magic & Spells'], ['magicabilities']),
    ('itemattributes', 'ATTRIBUTES & RESISTANCES', ['Attributes & Resistances'], ['resistances']),
    ('itemcombat', 'MOVEMENT & COMBAT', ['Movement & Combat'], ['movement', 'combat', 'stealth']),
    ('itemleadership', 'LEADERSHIP & MORALE', [], ['leadership']),
    ('itemsummoning', 'SUMMONING & SHAPES', [], ['summoning']),
    ('itemnoncombat', 'NON-COMBAT', [], ['noncombat']),
    ('itemcurses', 'CURSES & AFFLICTIONS', ['Curses & Afflictions'], ['aging']),
    ('itemrestrictions', 'RESTRICTIONS', ['Restrictions'], []),
    ('itemdancing', 'DANCING WEAPON', ['Dancing Weapon'], []),
    ('itemcreature', 'BODY & TYPE', [], ['creature', 'recruitment', 'pretender']),
]
ITEM_REGROUPED = {'properties', 'more'} | {g for g, _, _, _ in ITEM_GROUPS}


def regroup_items(all_hints):
    item_hints, monster_hints = all_hints['item'], all_hints['monster']
    path = os.path.join(DATA, 'item_badges.json')
    config = json.load(open(path, encoding='utf-8'))
    kept, commands = [], []
    for section in config['sections']:
        if section['id'] in ITEM_REGROUPED:
            commands.extend(c for c in section['commands'] if c['name'] not in {x['name'] for x in commands})
        else:
            kept.append(section)
    monster_group_of = {sec: gid for gid, _, secs in MONSTER_GROUPS for sec in secs}
    grouped = {g: [] for g, _, _, _ in ITEM_GROUPS}
    grouped[OTHER[0]] = []
    for c in commands:
        section = (item_hints.get(c['name']) or {}).get('section')
        target = next((g for g, _, item_secs, _ in ITEM_GROUPS if section in item_secs), None)
        if target is None:
            mgroup = group_for(c['name'], monster_hints, monster_group_of)
            target = next((g for g, _, _, mgroups in ITEM_GROUPS if mgroup in mgroups), OTHER[0])
        grouped[target].append(c)
    sections = []
    for gid, title in [(g, t) for g, t, _, _ in ITEM_GROUPS] + [OTHER]:
        if grouped[gid]:
            sections.append({'id': gid, 'displayName': title, 'description': f'{title.title()} (the modding manual\'s sections)',
                             'renderer': 'badge', 'readOnly': False,
                             'commands': sorted(grouped[gid], key=lambda c: c.get('display', c['name']).lower())})
    config['sections'] = kept + sections
    with open(path, 'w', encoding='utf-8') as f:
        f.write(json.dumps(config, indent=2, ensure_ascii=False) + '\n')
    print('items: ' + ', '.join(f"{s['id']} {len(s['commands'])}" for s in sections))


def main():
    all_hints = json.load(open(os.path.join(DATA, 'command_hints.json'), encoding='utf-8'))['types']
    regroup_items(all_hints)
    hints = all_hints['monster']
    path = os.path.join(DATA, 'monster_badges.json')
    config = json.load(open(path, encoding='utf-8'))
    kept, commands, template = [], [], None
    for section in config['sections']:
        if section['id'] in REGROUPED:
            template = template or section
            commands.extend(c for c in section['commands'] if c['name'] not in {x['name'] for x in commands})
        else:
            kept.append(section)
    group_of = {sec: gid for gid, _, secs in MONSTER_GROUPS for sec in secs}
    grouped = {gid: [] for gid, _, _ in MONSTER_GROUPS}
    grouped[OTHER[0]] = []
    for c in commands:
        grouped[group_for(c['name'], hints, group_of)].append(c)
    sections = []
    for gid, title in [(g, t) for g, t, _ in MONSTER_GROUPS] + [OTHER]:
        if not grouped[gid]:
            continue
        sections.append({
            'id': gid, 'displayName': title, 'description': f'{title.title()} (the modding manual\'s sections)',
            'renderer': 'coloredBadge' if gid == 'resistances' else 'badge', 'readOnly': False,
            'commands': sorted(grouped[gid], key=lambda c: c.get('display', c['name']).lower()),
        })
    # the editor's own sections first (stats, clears, magic), then the abilities
    config['sections'] = kept + sections
    with open(path, 'w', encoding='utf-8') as f:
        f.write(json.dumps(config, indent=2, ensure_ascii=False) + '\n')
    print(', '.join(f"{s['id']} {len(s['commands'])}" for s in sections))


if __name__ == '__main__':
    main()
