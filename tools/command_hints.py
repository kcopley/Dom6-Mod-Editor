#!/usr/bin/env python3
"""
Hover hints for every mod command, from Illwinter's manuals: per entity type (the manual's
chapters), each command's arguments, its description, and the value table printed with it
(magic path numbers, gem types, rarities, ...). The editor shows these as tooltips
(Dom5Editor.Core/Data/command_hints.json, read by Data/CommandHints.cs).

The manuals' two columns are read in order by tools/manual_text.py (the older extraction in
docs/pdf_extracted ran them together, which garbled many descriptions).

Usage: python3 tools/command_hints.py [dom6modman.pdf] [dom6eventman.pdf]
"""
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(__file__))
from manual_text import pages_of  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The main manual's chapters (its table of contents), by their heading lines, as entity types.
CHAPTER_HEADINGS = {
    'Requirements': None, 'Mod Info': 'modinfo', 'Sound Modding': 'sound', 'Weapon Modding': 'weapon',
    'Armor Modding': 'armor', 'Monster Modding, Basic': 'monster', 'Monster Modding, Special Abilities': 'monster',
    'Monster Modding, Leadership & Morale': 'monster', 'Monster Modding, Magic': 'monster',
    'Monster Modding, New Abilities for Dominions 5': 'monster', 'Name Modding': 'nametype',
    'Bless Modding': 'bless', 'Magic Site Modding': 'site', 'Nation Modding': 'nation', 'Spell Modding': 'spell',
    'Magic Item Modding': 'item', 'General Modding': 'general', 'Poptype Modding': 'poptype',
    'Mercenary Modding': 'merc', 'AI Modding': 'template', 'Event Modding': None, 'Minimal Example Mod': None,
    'Important Notes': None,
    'Examples': None,  # the event manual's sample events (not command descriptions)
}

# value tables by their titles ("magic path numbers"), for commands that refer to them
TABLES = {}

COMMAND = re.compile(r'^#([a-z0-9_]+)\s*(.*)$')
# what follows a command name when the line introduces it: nothing, or its arguments
# ("<value>", "\"<name>\" | <nbr>", "<0 | 1>", "...5", "-1"); else it's a sentence mentioning it
# (a number after the name is an example in a description: "#dmg 8")
ARGUMENTS = re.compile(r'^($|<|"|\(|\[|\.\.\.|\|)')
TABLE_ROW = re.compile(r'^(-?\d+)\s+(\S.*)$')
PAGE_NUMBER = re.compile(r'^\d{1,3}$')


def headings_of(pages):
    """Section titles from the table of contents (lines "  Title  |  page")."""
    titles = set()
    for page in pages[:2]:
        for line in page:
            m = re.match(r'^\s*(.+?)\s+\|\s+\d+$', line)
            if m:
                titles.add(m.group(1).strip())
    return titles


def parse(pages, headings, default_chapter=None):
    """Command blocks: (chapter, section, command, args, text lines, table rows)."""
    blocks = []
    chapter = default_chapter
    section = ''
    current = None
    waiting = []  # commands listed one under another, sharing the description that follows
    in_table = False
    rows = []
    title = None
    for page in pages[2:] if default_chapter is None else pages:
        for raw in page:
            line = raw.strip()
            if not line or PAGE_NUMBER.match(line):
                continue
            if line in headings or line in CHAPTER_HEADINGS:
                if line in CHAPTER_HEADINGS:
                    chapter = CHAPTER_HEADINGS[line]
                section = line
                current = None
                in_table = False
                continue
            m = COMMAND.match(line)
            if m and chapter is not None and ARGUMENTS.match(m.group(2).strip()):
                current = {'chapter': chapter, 'section': section, 'command': m.group(1),
                           'args': m.group(2).strip(), 'text': [], 'values': []}
                if waiting and not waiting[-1]['text']:
                    waiting.append(current)
                else:
                    waiting = [current]
                blocks.append(current)
                in_table = False
                continue
            if current is None:
                continue
            row = TABLE_ROW.match(line)
            following = next_line(page, raw) or ''
            if row and in_table:
                rows.append([int(row.group(1)), row.group(2).strip()])
                continue
            if not row and is_table_header(line) and TABLE_ROW.match(following):
                in_table = True  # the header ("Nbr Magic Path"); its rows follow
                rows = []
                current['values'] = rows
                current['header'] = line
                continue
            if in_table:
                in_table = False
                if is_caption(line):
                    TABLES[line.lower()] = (current.get('header'), rows)  # the table's title under it ("Magic path numbers")
                    continue
                if title:
                    TABLES[title.lower()] = (current.get('header'), rows)
            if not row and is_caption(line) and is_table_header(following):
                title = line  # a title above a table ("Path masks")
                continue
            current['text'].append(line)
            for w in waiting:
                if w is not current and not w['text']:
                    w['shared'] = current
    return blocks


def is_table_header(line):
    """Whether a line is a table's column header ("Nbr Magic Path", "2^x Affliction", "Mask Event Order")."""
    return (bool(re.match(r'^(Nbr|Number|2\^x|Mask|Rarity|Value|Bit|Nr|Type)\b', line))
            and len(line.split()) <= 5 and not re.search(r'[.,:]', line))


def is_caption(line):
    """A table's title: a few words, no sentence punctuation."""
    return len(line.split()) <= 4 and not re.search(r'[.,:;#<>"]', line)


def next_line(page, raw):
    i = page.index(raw)
    return page[i + 1].strip() if i + 1 < len(page) else None


def clean(text):
    s = ' '.join(text)
    s = re.sub(r'\s+', ' ', s).strip()
    s = s.replace('- ', '-') if re.search(r'\w- \w', s) is None else s
    return s


def main():
    modman = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'dom6modman.pdf')
    eventman = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, 'dom6eventman.pdf')
    hints = {}

    pages = pages_of(modman)
    blocks = parse(pages, headings_of(pages))
    pages = pages_of(eventman)
    blocks += parse(pages, headings_of(pages), default_chapter='event')

    for b in blocks:
        text = b['text'] or (b.get('shared') or {}).get('text', [])
        values = b['values'] or (b.get('shared') or {}).get('values', [])
        header = b.get('header') or (b.get('shared') or {}).get('header')
        entry = {'args': b['args'], 'text': clean(text)}
        if b['section']:
            entry['section'] = b['section']
        if not values:
            # "see table \"Magic schools\"", "the \"Magic path numbers\" table", "Table 18" (path masks)
            for name in re.findall(r'[Tt]able "([^"]+)"|"([^"]+)" table', entry['text']):
                name = (name[0] or name[1]).lower()
                if name in TABLES:
                    header, values = TABLES[name]
                    entry['table'] = name
                    break
        if values:
            entry['values'] = values
            if header:
                entry['header'] = header
        by_type = hints.setdefault(b['chapter'], {})
        if b['command'] in by_type:
            # listed twice (e.g. a summary list and its description): keep the one that says more
            if len(by_type[b['command']]['text']) >= len(entry['text']):
                continue
        by_type[b['command']] = entry

    out = os.path.join(ROOT, 'Dom5Editor.Core', 'Data', 'command_hints.json')
    with open(out, 'w', encoding='utf-8') as f:
        json.dump({'source': 'Illwinter: Dominions 6 Modding Manual (6.33) and Event Modding Manual (6.29); tools/command_hints.py',
                   'types': hints}, f, indent=1, ensure_ascii=False, sort_keys=True)
    total = sum(len(v) for v in hints.values())
    print(f'{out}: {total} commands; ' + ', '.join(f'{k} {len(v)}' for k, v in sorted(hints.items())))


if __name__ == '__main__':
    main()
