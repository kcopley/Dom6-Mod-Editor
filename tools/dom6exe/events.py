"""The game's own events, read from Dominions6.exe (dom6exe.py events).

Vanilla events are a static table in .data, one record per event: the message text, the rarity
and two lists of (code, value) pairs, one for requirements and one for effects. The .dm event
parser writes into the same table (#selectevent N picks record N, #newevent the first free
record from the first mod slot on), so a stored pair is written back as the command whose
parser branch stores that code in that list.

Everything is read from the code, not from fixed addresses:
  table, record size, message size   the #msg branch (imul by the record size, the table's
                                     address, the length passed to the string reader)
  rarity                             the #rarity branch (a byte store)
  end marker, last #selectevent      the #selectevent branch (it rejects records whose rarity
                                     byte is the end marker, and numbers above a maximum)
  free marker, first mod slot        the #newevent branch (it scans for the free marker)
  requirement / effect lists         the generic handler: per "current event" global, the
                                     list it appends to and its number of slots; the event
                                     parser sets one global before the requirement commands
                                     and the other before the effect commands
  code of each command               the generic handler call's arguments (dom6exe.Parser)
Checked on every run: the record size against three consecutive copies of a vanilla message,
the end record's text ("end"), and that one list gets only #req_ commands and the other none.
"""
import bisect, collections, os, re, struct

from dom6exe import Parser, num, reg64

MANUAL = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'docs', 'pdf_extracted', 'eventman.txt')
# a vanilla message three consecutive events share (6.37: events 8, 9, 10)
CHECK_MESSAGE = b'A prospector genius has found a small gold deposit.'
# commands the event parser compares by name whose names start with a digit (#2com, #1d6vis,
# #1unit, #3d6units); dom6exe's IDENT pattern takes them since 2026-10-11
DIGIT_NAME = re.compile(r'[0-9][a-z0-9_]*$')
# Names the game itself gives codes, as hints for codes no command writes: (list, a string the
# function loads, the name's pattern). The debug describers print an event as "rare 25,
# temple 1, ..." and "kill 10, gold 100, ..."; the requirement checker asserts "bad req_siege".
DEBUG_NAMES = [('requirement', 'rare %d, ', r'([a-z0-9_]+) %[ds]'),
               ('effect', 'kill %d, ', r'([a-z0-9_]+) %[ds]'),
               ('requirement', 'Unknown req_%d for event', r'bad (req_[a-z0-9_]+)$')]
# the effect executor's dice-units branch prints this
UNITS_ANCHOR = '  event_xd6units'


class EventTable:
    def __init__(self, exe, p=None):
        self.exe = exe
        self.p = p = p or Parser(exe)
        fs = p.contexts['event']
        if len(fs) != 1:
            raise SystemExit('event parser not found (functions: %s)' % sorted(map(hex, fs)))
        self.func = next(iter(fs))
        funcs = exe.functions()
        self._funcs = funcs
        self._starts = [f[0] for f in funcs]
        end = next(e for b, e in funcs if b == self.func)
        self.lo = p.index[self.func]
        self.hi = bisect.bisect_left([x[0] for x in p.ins], end)
        self._layout()
        self._commands()

    # -----------------------------------------------------------------------------------------
    def _branch(self, name):
        """Instructions of a special command's branch: from its inline name check
        (cmp BYTE PTR [name],0) to the next command's."""
        p = self.p
        cmps = [i for i in range(self.lo, self.hi) if i in p.refs and p.ins[i][1] == 'cmp']
        start = [i for i in cmps if p.refs[i][1] == name]
        if not start:
            raise SystemExit('event parser: no #%s branch' % name)
        nxt = [i for i in cmps if i > start[0] and p.refs[i][1] != name]
        return p.ins[start[0]:nxt[0] if nxt else start[0] + 200]

    def _layout(self):
        # #msg: imul by the record size, the table's address, the message size
        br = self._branch('msg')
        k = next(k for k, (_, o, a, _) in enumerate(br) if o == 'imul' and a.count(',') == 2)
        self.record = num(br[k][2].split(',')[2])
        self.base = next(t for _, o, a, t in br[k + 1:k + 4] if o == 'lea' and t is not None)
        self.msg_size = next(num(a.split(',')[1]) for _, o, a, _ in br if o == 'mov' and a.startswith('r8d,0x'))
        # #rarity: a byte store into the record
        br = self._branch('rarity')
        m = next(re.match(r'BYTE PTR \[\w+\+\w+\*1\+(0x[0-9a-f]+)\],\w+$', a) for _, o, a, _ in br
                 if o == 'mov' and re.match(r'BYTE PTR \[\w+\+\w+\*1\+0x', a))
        self.rarity_off = num(m.group(1))
        # #selectevent: the highest number it takes, the end marker it refuses
        br = self._branch('selectevent')
        self.select_max = next(num(a.split(',')[1]) for _, o, a, _ in br if o == 'cmp' and re.match(r'e[a-z]{2},0x', a))
        m = next(re.match(r'BYTE PTR \[\w+\+\w+\*1\+(0x[0-9a-f]+)\],(0x[0-9a-f]+)$', a) for _, o, a, _ in br
                 if o == 'cmp' and a.startswith('BYTE PTR [') and '*1+' in a)
        if num(m.group(1)) != self.rarity_off:
            raise SystemExit('#selectevent checks offset %s, #rarity stores %s' % (m.group(1), hex(self.rarity_off)))
        self.end_marker = num(m.group(2))
        if not any(o == 'imul' and a.endswith(',%s' % hex(self.record)) for _, o, a, _ in br):
            raise SystemExit('#selectevent does not index the event table directly')
        # #newevent: the free marker it scans for, and where the scan starts
        br = self._branch('newevent')
        self.free_marker = next(num(a.split(',')[1]) for _, o, a, _ in br if o == 'cmp' and re.match(r'BYTE PTR \[r\w\w\],0x', a))
        first = next(t for _, o, a, t in br if o == 'lea' and t is not None and t > self.base
                     and (t - self.base - self.rarity_off) % self.record == 0)
        self.first_mod = (first - self.base - self.rarity_off) // self.record
        # the lists: in the generic handler, per "current event" global, the list it writes
        calls = collections.Counter(a.split()[0] for _, o, a, _ in self.p.ins[self.lo:self.hi] if o == 'call')
        self.handler = next(c for c, n in calls.most_common() if c in self.p.generic)
        hb = num(self.handler)
        hend = next(e for b, e in self._funcs if b == hb)
        hi = self.p.index[hb]
        lists = {}
        h = self.p.ins
        while h[hi][0] < hend:
            a, o, args, t = h[hi]
            if o == 'movsxd' and t is not None:
                for a2, o2, args2, t2 in h[hi + 1:hi + 14]:
                    if o2 == 'lea' and t2 is not None and self.base < t2 < self.base + self.record:
                        cap = next(num(x.split(',')[1]) for _, oo, x, _ in h[hi + 1:hi + 30]
                                   if oo == 'cmp' and re.match(r'edx,0x', x))
                        lists[t] = (t2 - self.base, (cap + 1) // 2)
                        break
            hi += 1
        # which global the parser sets before which commands
        sets = [(i, t) for i in range(self.lo, self.hi) for a, o, args, t in [self.p.ins[i]]
                if o == 'mov' and t in lists and args.endswith(',ebp')]
        if len(sets) != 2 or len({t for i, t in sets}) != 2:
            raise SystemExit('event parser: expected two list globals, found %s' % sets)
        self._sections = sets          # (instruction index, global), in code order
        self._lists = lists
        self.check()

    def check(self):
        """The layout against known facts."""
        d = self.exe.data
        a = d.find(CHECK_MESSAGE)
        b = d.find(CHECK_MESSAGE, a + 1)
        c = d.find(CHECK_MESSAGE, b + 1)
        if not (b - a == c - b == self.record):
            raise SystemExit('event record size %d does not match the vanilla table (%d, %d)' % (self.record, b - a, c - b))
        n = self.count()
        if self.exe.cstr(self.base + n * self.record) != 'end':
            raise SystemExit('event table: end record %d is not "end"' % n)

    def _commands(self):
        """(list, code) -> [command info], for every generic handler call in the event parser."""
        p = self.p
        for i in range(self.lo, self.hi):
            a, o, args, t = p.ins[i]
            if o == 'lea' and t is not None and args.startswith('rdx,'):
                s = self.exe.cstr(t)
                if s and DIGIT_NAME.match(s):
                    p.refs[i] = (self.func, s)
        self.by_code = collections.defaultdict(list)
        self.commands = {}
        names = {}
        for c in p.generic_call_args():
            if c['context'] != 'event' or c['key'] is None:
                continue
            g = [t for i, t in self._sections if i < c['at']]
            if not g:
                continue
            off, cap = self._lists[g[-1]]
            if c['kind'] is None:
                k = self._register_kind(c['at'])
                if k is not None:
                    c['kind'], c['repeat'] = k % 10, k // 10
            c['list'] = off
            self.by_code[(off, c['key'])].append(c)
            self.commands[c['command']] = c
            names.setdefault(off, []).append(c['command'])
        # the list the #req_ commands write is the requirement list
        offs = [self._lists[t][0] for i, t in self._sections]
        req = [o for o in offs if all(n.startswith('req_') for n in names.get(o, []))]
        eff = [o for o in offs if not any(n.startswith('req_') for n in names.get(o, []))]
        if len(req) != 1 or len(eff) != 1:
            raise SystemExit('event lists: cannot tell requirements from effects')
        self.req_off, self.eff_off = req[0], eff[0]
        self.slots = {o: cap for o, cap in self._lists.values()}
        documented = set(re.findall(r'^#(\w+)', open(MANUAL, encoding='utf-8').read(), re.M)) if os.path.exists(MANUAL) else set()
        # several commands storing one code: the documented one, else the first in the parser
        self.aliases = {}
        self.command_of = {}
        for key, cs in self.by_code.items():
            best = sorted(cs, key=lambda c: (c['command'] not in documented, c['at']))
            self.command_of[key] = best[0]
            if len(cs) > 1:
                self.aliases[best[0]['command']] = [c['command'] for c in best[1:]]

    def _register_kind(self, at):
        """The kind argument ([rsp+0x28]) when it is passed in a register: that register's last
        constant write above the call (the parser zeroes it before the requirement commands)."""
        ins = self.p.ins
        reg = None
        for j in range(at - 1, max(at - 40, self.lo), -1):
            m = re.match(r'DWORD PTR \[rsp\+0x28\],(\w+)$', ins[j][2])
            if ins[j][1] == 'mov' and m:
                reg = reg64(m.group(1))
                break
            if ins[j][1] == 'call':
                return None
        if reg is None:
            return None
        for j in range(at - 1, self.lo, -1):
            a, o, args, t = ins[j]
            if reg64(args.split(',')[0]) != reg or o in ('cmp', 'test', 'push') or o.startswith('j'):
                continue
            src = args.split(',', 1)[1] if ',' in args else ''
            if o == 'xor' and reg64(src) == reg:
                return 0
            if o == 'mov' and re.match(r'0x[0-9a-f]+$', src):
                return num(src)
            return None
        return None

    # -----------------------------------------------------------------------------------------
    def count(self):
        """Vanilla events: the records before the first one carrying the end marker."""
        o = self.exe.offset(self.base)
        n = 0
        while self.exe.data[o + n * self.record + self.rarity_off] != self.end_marker:
            n += 1
            if n > self.select_max:
                raise SystemExit('event table: no end marker')
        return n

    def events(self):
        d = self.exe.data
        o = self.exe.offset(self.base)
        out = []
        for n in range(self.count()):
            r = d[o + n * self.record:o + (n + 1) * self.record]
            msg = r[:self.msg_size]
            e = {'number': n, 'message': msg[:msg.find(b'\0')].decode('utf-8'),
                 'rarity': struct.unpack_from('<b', r, self.rarity_off)[0]}
            for kind, off in (('requirements', self.req_off), ('effects', self.eff_off)):
                pairs = [struct.unpack_from('<qq', r, off + 16 * k) for k in range(self.slots[off])]
                end = next((k for k, (c, v) in enumerate(pairs) if c == 0), len(pairs))
                e[kind] = pairs[:end]
                # the game stops at the first empty slot; anything after it is never read
                e[kind + '_unread'] = [(c, v) for c, v in pairs[end:] if c or v]
            out.append(e)
        return out

    def debug_names(self):
        """(list offset, code) -> the game's own name for a code, from the functions that print
        or check events (DEBUG_NAMES: a compare with the code, then the name's string), and
        the effect executor's dice-units branch. Hints for codes no command writes."""
        ins = self.p.ins
        lists = {'requirement': self.req_off, 'effect': self.eff_off}
        where = self._functions_with_strings([s for _, s, _ in DEBUG_NAMES] + [UNITS_ANCHOR])
        names = {}
        for kind, anchor, pattern in DEBUG_NAMES:
            if anchor not in where:
                continue
            lo, hi, _ = where[anchor]
            pending = None
            for k in range(lo, hi):
                a, o, args, t = ins[k]
                m = re.match(r'(\w+),(0x[0-9a-f]+)$', args)
                if o == 'cmp' and m:
                    pending = (num(m.group(2)), k)
                elif pending and o == 'lea' and t is not None and k - pending[1] < 6:
                    m = re.match(pattern, self.exe.cstr(t, 80) or '')
                    if m:
                        names.setdefault((lists[kind], pending[0]), m.group(1))
                    pending = None
        # one branch handles effects first..first+n as (code - first + 1)d6 units, where first is
        # the code #1d6units writes; the commands stop at #16d6units, vanilla uses more
        one = self.commands.get('1d6units')
        if UNITS_ANCHOR in where and one:
            lo, hi, at = where[UNITS_ANCHOR]
            for k in range(at, max(at - 80, lo), -1):
                m = re.match(r'eax,\[rbx-(0x[0-9a-f]+)\]$', ins[k][2])
                if ins[k][1] == 'lea' and m and num(m.group(1)) == one['key']:
                    top = next(num(x.split(',')[1]) for _, oo, x, _ in ins[k + 1:k + 3] if oo == 'cmp')
                    for code in range(one['key'], one['key'] + top + 1):
                        names[(self.eff_off, code)] = '%dd6units' % (code - one['key'] + 1)
                    break
        return names

    def _functions_with_strings(self, anchors):
        """anchor -> (first, end, loading instruction) of the one function that loads a string
        starting with it."""
        hits = collections.defaultdict(set)
        for i, (a, o, args, t) in enumerate(self.p.ins):
            if o == 'lea' and t is not None:
                v = self.exe.cstr(t, 80)
                for s in anchors:
                    if v and v.startswith(s):
                        hits[s].add((bisect.bisect_right(self._starts, a) - 1, i))
        out = {}
        addrs = [x[0] for x in self.p.ins]
        for s, h in hits.items():
            if len({f for f, i in h}) == 1:
                f, i = min(h)
                b, e = self._funcs[f]
                out[s] = (self.p.index[b], bisect.bisect_left(addrs, e), i)
        return out


# ---------------------------------------------------------------------------------------------

def write(exe, path, messages=False):
    """The events as #selectevent blocks. Without `messages`, the messages (the game's text) are
    left out: the editor reads them from the player's own Dominions6.exe at the place the
    header names (this exe's checksum, the table's file offset, record and message sizes)."""
    t = EventTable(exe)
    events = t.events()
    hints = t.debug_names()
    label = {t.req_off: 'requirement', t.eff_off: 'effect'}
    stats = collections.Counter()
    unmapped = collections.Counter()
    notes = collections.Counter()
    alias_text = ', '.join('#%s (as #%s)' % (', #'.join(v), k) for k, v in sorted(t.aliases.items()))
    text = ['-- Dominions %s vanilla events, written from Dominions6.exe by tools/dom6exe (exe %s).' % (exe.version, exe.sha),
            '-- #selectevent N is the game\'s event N. Requirements, then effects, in the order the game stores them.',
            '-- "-- ro:" lines are stored codes no command writes (shown read-only), with the game\'s own',
            '-- debug name for the code when it has one.',
            '-- Commands that store the same code as a documented one (written as that one): %s.' % alias_text]
    if not messages:
        # the game's text stays in the game: where to read it (Dom5Editor: VanillaEventMessages)
        text.append('-- messages: exe %s offset %d record %d size %d count %d' % (
            exe.sha, exe.offset(t.base), t.record, t.msg_size, len(events)))
    text.append('')
    for e in events:
        text.append('#selectevent %d' % e['number'])
        text.append('#rarity %d' % e['rarity'])
        for kind, off in (('requirements', t.req_off), ('effects', t.eff_off)):
            seen = collections.Counter()
            for code, val in e[kind]:
                stats[kind] += 1
                c = t.command_of.get((off, code))
                if c is None:
                    name = hints.get((off, code))
                    what = '%s %d' % (label[off], code) + (' (%s)' % name if name else '')
                    text.append('-- ro: %s = %d' % (what, val))
                    unmapped[what] += 1
                    continue
                stats['mapped'] += 1
                note = []
                if c['min'] is not None and c['min'] == c['max']:
                    line = '#%s' % c['command']
                    if val != c['min']:
                        note.append('stored %d; the command stores %d' % (val, c['min']))
                        notes['no-argument command, other stored value'] += 1
                else:
                    line = '#%s %d' % (c['command'], val)
                    if c['min'] is not None and c['max'] is not None and not c['min'] <= val <= c['max']:
                        note.append('outside the command\'s range %d..%d' % (c['min'], c['max']))
                        notes['value outside the command\'s range'] += 1
                if seen[code] and c['repeat'] == 0:
                    note.append('stored twice; a second #%s in a mod replaces the first' % c['command'])
                    notes['repeated code a command would replace'] += 1
                seen[code] += 1
                text.append(line + ('  -- ' + '; '.join(note) if note else ''))
            for code, val in e[kind + '_unread']:
                c = t.command_of.get((off, code))
                text.append('-- not read by the game (after an empty slot): %s' %
                            ('#%s %d' % (c['command'], val) if c else '%s %d = %d' % (label[off], code, val)))
                notes['pairs after an empty slot'] += 1
        if messages:
            text.append('#msg "%s"' % e['message'])
        text.append('#end')
        text.append('')
    if path:
        open(path, 'w', encoding='utf-8').write('\n'.join(text))
    pairs = stats['requirements'] + stats['effects']
    return {
        'game_version': exe.version, 'exe_sha256_16': exe.sha,
        'table': {'address': hex(t.base), 'record_size': t.record, 'message_size': t.msg_size,
                  'rarity_offset': t.rarity_off,
                  'requirements': {'offset': t.req_off, 'slots': t.slots[t.req_off]},
                  'effects': {'offset': t.eff_off, 'slots': t.slots[t.eff_off]},
                  'end_marker': t.end_marker, 'free_marker': t.free_marker,
                  'selectevent_max': t.select_max, 'first_mod_event': t.first_mod},
        'events': len(events),
        'pairs': {'requirements': stats['requirements'], 'effects': stats['effects'], 'mapped': stats['mapped'],
                  'mapped_percent': round(100.0 * stats['mapped'] / pairs, 1) if pairs else None},
        'unmapped': dict(unmapped.most_common()),
        'notes': dict(notes),
        'aliases': t.aliases,
        'rarity': {str(k): v for k, v in sorted(collections.Counter(e['rarity'] for e in events).items())},
    }
