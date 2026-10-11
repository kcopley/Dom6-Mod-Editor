#!/usr/bin/env python3
"""Reads Dominions 6 game facts straight from Dominions6.exe.

The dom6inspector's CSV game data is extracted from the exe by a tool that is rarely
maintained. This reads the exe directly instead:

  commands   which mod commands the game's .dm parser actually reads, per entity type
             (#selectmonster, #newweapon, ...), and for monster and item commands the
             numbered ability each one sets
  monsters   the vanilla monster table: name, base stats, numbered abilities
  readonly   abilities vanilla monsters have that no mod command can set; an editor shows
             them read-only
  events     the vanilla events as #selectevent blocks (events.py)
  texts      where the game's texts are (descriptions, nation summaries; texts.py)
  snapshot   everything the change log compares, from one exe, into a folder (changelog.py)
  changelog  what changed between two versions: OLD NEW, each a snapshot folder, an exe or
             a vanilla .dm (changelog.py; docs/GAME_UPDATES.md)

Usage:
  python3 tools/dom6exe/dom6exe.py [--exe PATH] [--inspector DIR] commands|monsters|readonly [--out FILE]

Nothing is hard-coded to one game build: the parser functions are found by the command
names they compare against, the generic command handlers by how often they are called with
a command name, and the monster table by locating known vanilla names. Layout facts that
were worked out by reading the code (record size, where the abilities sit) are checked
against known vanilla values on every run.

Needs Python 3.8+ and GNU objdump (binutils) for the disassembly.
"""
import argparse, bisect, collections, csv, hashlib, json, os, re, struct, subprocess, sys, tempfile

DEFAULT_EXE = '/mnt/c/Games/Steam/steamapps/common/Dominions6/Dominions6.exe'
BASE = 0x140000000

# A command each parser function checks for, and only that function (besides the top-level
# dispatcher, which reads #new* / #select*).
CONTEXT_ANCHORS = {
    'monster': 'copystats', 'item': 'copyitem', 'weapon': 'copyweapon', 'armor': 'copyarmor',
    'spell': 'copyspell', 'site': 'copysite', 'nation': 'clearnation', 'merc': 'bossname',
    'poptype': 'cleardef', 'nametype': 'addname', 'event': 'req_lab', 'global': 'gemlongevity',
    'bless': 'clearfx', 'template': 'researchgoal', 'sound': 'selectsound', 'top': 'newmonster',
}
# ('template' reads #bless, #domstr, #form, #magic, #prison, #scale, #favrit, #researchgoal:
# AI pretender designs, Dom5Parser's Template entity.)


class Exe:
    def __init__(self, path):
        self.path = path
        self.data = open(path, 'rb').read()
        d = self.data
        pe = struct.unpack_from('<I', d, 0x3c)[0]
        nsec = struct.unpack_from('<H', d, pe + 6)[0]
        opt = struct.unpack_from('<H', d, pe + 20)[0]
        self.sections = {}
        for i in range(nsec):
            o = pe + 24 + opt + 40 * i
            name = d[o:o + 8].rstrip(b'\0').decode()
            vsz, va, rsz, raw = struct.unpack_from('<IIII', d, o + 8)
            self.sections[name] = (BASE + va, raw, rsz, vsz)
        self.sha = hashlib.sha256(d).hexdigest()[:16]
        m = re.search(rb'\0version (\d+\.\d+)\0', d)
        self.version = m.group(1).decode() if m else None

    def offset(self, addr):
        for va, raw, rsz, vsz in self.sections.values():
            if va <= addr < va + rsz:
                return raw + addr - va
        return None

    def address(self, off):
        for va, raw, rsz, vsz in self.sections.values():
            if raw <= off < raw + rsz:
                return va + off - raw
        return None

    def cstr(self, addr, limit=200):
        o = self.offset(addr)
        if o is None:
            return None
        e = self.data.find(b'\0', o, o + limit)
        if e < 0:
            return None
        try:
            return self.data[o:e].decode('ascii')
        except UnicodeDecodeError:
            return None

    def functions(self):
        """(start, end) of every function, from the exception table (.pdata)."""
        va, raw, rsz, vsz = self.sections['.pdata']
        out = []
        for k in range(0, rsz, 12):
            b, e, u = struct.unpack_from('<III', self.data, raw + k)
            if b:
                out.append((BASE + b, BASE + e))
        return sorted(out)

    def disassembly(self):
        """Parsed objdump output, cached per exe (it takes a few seconds), and kept once parsed."""
        if getattr(self, '_ins', None) is not None:
            return self._ins
        cache = os.path.join(tempfile.gettempdir(), 'dom6exe-%s.asm' % self.sha)
        if not os.path.exists(cache):
            with open(cache + '.tmp', 'w') as f:
                subprocess.run(['objdump', '-d', '--no-show-raw-insn', '-M', 'intel', self.path], stdout=f, check=True)
            os.replace(cache + '.tmp', cache)
        pat = re.compile(r'^\s+([0-9a-f]+):\s+(\S+)\s*(.*?)(?:\s+# 0x([0-9a-f]+))?$')
        ins = []
        for line in open(cache):
            m = pat.match(line)
            if m:
                ins.append((int(m.group(1), 16), m.group(2), m.group(3), int(m.group(4), 16) if m.group(4) else None))
        self._ins = ins
        return ins


REG64 = {}
for _r in ('ax', 'bx', 'cx', 'dx', 'si', 'di', 'bp', 'sp'):
    for _v in ('r' + _r, 'e' + _r, _r, _r[0] + 'l' if _r[1] == 'x' else _r + 'l'):
        REG64[_v] = 'r' + _r
for _n in range(8, 16):
    for _suf in ('', 'd', 'w', 'b'):
        REG64['r%d%s' % (_n, _suf)] = 'r%d' % _n


def reg64(r):
    """The 64-bit register a register name is part of (ecx -> rcx, r8d -> r8), or None."""
    return REG64.get(r)


def num(x):
    try:
        return int(x, 16) if x.startswith('0x') else int(x)
    except (ValueError, AttributeError):
        return None


IDENT = re.compile(r'[a-z0-9_]*[a-z_][a-z0-9_]*$')  # #1unit, #2com, #3d6units start with a digit


def text(b):
    """Strings in the exe are UTF-8 (a few older ones Latin-1)."""
    try:
        return b.decode('utf-8')
    except UnicodeDecodeError:
        return b.decode('latin1')


def signed(v, bits=64):
    return v - (1 << bits) if v >= 1 << (bits - 1) else v


class Parser:
    """The game's .dm parser, as found in the code."""

    def __init__(self, exe):
        self.exe = exe
        self.ins = exe.disassembly()
        funcs = exe.functions()
        starts = [f[0] for f in funcs]

        def func_of(a):
            i = bisect.bisect_right(starts, a) - 1
            return funcs[i][0] if i >= 0 and a < funcs[i][1] else None

        # every instruction that loads the address of an identifier-like string
        self.refs = {}
        for idx, (a, op, args, tgt) in enumerate(self.ins):
            if tgt is None or op not in ('lea', 'mov', 'cmp'):
                continue
            s = exe.cstr(tgt)
            if s and IDENT.match(s):
                self.refs[idx] = (func_of(a), s)
        # parser functions per context, found by their anchor commands
        by_func = collections.defaultdict(set)
        for f, s in self.refs.values():
            by_func[f].add(s)
        # the top-level dispatcher compares against every #new* / #select*; each entity parser
        # against its own anchor. A function belongs to one context.
        top = {f for f, names in by_func.items() if {'newmonster', 'newweapon', 'newspell', 'selectsite'} <= names}
        self.contexts = {'top': top}
        for ctx, anchor in CONTEXT_ANCHORS.items():
            if ctx != 'top':
                self.contexts[ctx] = {f for f, names in by_func.items() if anchor in names and f not in top}
        self.by_func = by_func
        # generic handlers: the callees most often called with a command name in rdx
        callee = collections.Counter()
        for idx, (a, op, args, tgt) in enumerate(self.ins):
            if op == 'call':
                for j in range(idx - 1, max(idx - 12, 0), -1):
                    if self.ins[j][1] in ('call',) or self.ins[j][1].startswith('j'):
                        break
                    if j in self.refs and self.ins[j][2].startswith('rdx,'):
                        callee[args.split()[0]] += 1
                        break
        self.generic = [c for c, n in callee.most_common(2)]
        self.index = {a: i for i, (a, op, args, tgt) in enumerate(self.ins)}

    MEM = re.compile(r'(BYTE|WORD|DWORD|QWORD) PTR \[(\w+)(?:\+(\w+)\*(\d))?(?:\+(0x[0-9a-f]+))?\],(\w+)$')
    SIZES = {'BYTE': 1, 'WORD': 2, 'DWORD': 4, 'QWORD': 8}

    def branches(self, ctx):
        """What each command's branch in an entity parser does, from its instructions:
        stores into the entity record (base, offset, size), bits set or cleared in a flags
        word, abilities added (generic handler, ability setter, or a shared tail reached with
        the ability number in edx), and other calls. A branch is the code between the command
        name check (lea rdx,name; call matcher; test; je next) and the next check."""
        fs = self.contexts[ctx]
        setter = getattr(self, '_setter', None)
        out = collections.defaultdict(list)
        # A branch runs from its command's name to the next command name in the same parser.
        # (The monster parser calls a matcher per command; others inline the comparison.)
        order = sorted(k for k, (f, s) in self.refs.items() if f in fs)
        for n, i in enumerate(order):
            cmd = self.refs[i][1]
            w = self.ins[i:i + 6]
            if any(x[1] == 'call' and x[2].split()[0] in self.generic for x in w):
                continue
            later = [j for j in order[n + 1:] if self.refs[j][1] != cmd]
            nxt = self.ins[later[0]][0] if later else self.ins[min(i + 400, len(self.ins) - 1)][0]
            k = i + 1
            bases, const, edx, rmw, adds, prev_test = {}, {}, None, {}, {}, None
            while k < len(self.ins) and self.ins[k][0] < nxt and k - i < 400:
                aa, oo, rr, tt = self.ins[k]
                dst = rr.split(',')[0]
                # a register written by anything else no longer holds the address it was loaded with
                if reg64(dst) and not (oo == 'lea' and tt is not None) and oo not in ('cmp', 'test', 'push'):
                    bases.pop(reg64(dst), None)
                if oo == 'call':
                    for r in ('rax', 'rcx', 'rdx', 'r8', 'r9', 'r10', 'r11'):
                        bases.pop(r, None)
                if oo == 'lea' and tt is not None and reg64(dst):
                    bases[reg64(dst)] = tt
                # read-modify-write of a flags word in a register: mov reg,[rec+off]; and/or/bts/btr; mov [rec+off],reg
                m = re.match(r'(\w+),(?:BYTE|WORD|DWORD|QWORD) PTR \[(\w+)(?:\+(\w+)\*\d)?(?:\+(0x[0-9a-f]+))?\]$', rr)
                if oo == 'mov' and m and (bases.get(reg64(m.group(2))) or bases.get(reg64(m.group(3)))):
                    rmw[m.group(1)] = [0, 0]
                m = re.match(r'(\w+),(0x[0-9a-f]+|\d+)$', rr)
                if m and m.group(1) in rmw and oo == 'add':
                    v = -signed(num(m.group(2)))        # bt reg,N; jae; add reg,-(1 << N): clear a bit
                    if v > 0 and v & (v - 1) == 0:
                        oo = 'btr'
                        m = re.match(r'(\w+),(.*)$', '%s,%d' % (m.group(1), v.bit_length() - 1))
                if m and m.group(1) in rmw and oo in ('or', 'and', 'bts', 'btr'):
                    v = num(m.group(2))
                    if oo == 'or':
                        rmw[m.group(1)][0] |= v
                    elif oo == 'and':
                        rmw[m.group(1)][1] |= (~v) & 0xffffffffffffffff
                    elif oo == 'bts':
                        rmw[m.group(1)][0] |= 1 << v
                    else:
                        rmw[m.group(1)][1] |= 1 << v
                # constants in registers: the ability number (edx) and value (r8) of setter calls
                d64 = reg64(dst)
                m = re.match(r'(\w+),(0x[0-9a-f]+)$', rr)
                m2 = re.match(r'(\w+),\[(\w+)([+-])(0x[0-9a-f]+)\]$', rr)
                m3 = re.match(r'(\w+),(\w+)$', rr)
                if oo in ('mov', 'movabs') and m and d64:
                    # a 32-bit register write zero-extends; 64-bit immediates are sign-extended
                    const[d64] = signed(num(m.group(2))) if dst == d64 else num(m.group(2))
                elif oo == 'xor' and m3 and m3.group(1) == m3.group(2) and d64:
                    const[d64] = 0
                elif oo == 'mov' and m3 and d64 and reg64(m3.group(2)) in const:
                    const[d64] = const[reg64(m3.group(2))]
                elif oo == 'lea' and m2 and d64 and reg64(m2.group(2)) in const:
                    v = num(m2.group(4))
                    const[d64] = const[reg64(m2.group(2))] + (v if m2.group(3) == '+' else -v)
                elif oo in ('add', 'sub') and m and d64:
                    v = signed(num(m.group(2)), 32 if dst != d64 else 64)
                    if d64 in const:
                        const[d64] += v if oo == 'add' else -v
                    else:   # arithmetic on a parsed argument: #eyes stores N - 2
                        adds[d64] = adds.get(d64, 0) + (v if oo == 'add' else -v)
                elif oo == 'movsxd' and m3 and d64:
                    const.pop(d64, None)
                    adds[d64] = adds.get(reg64(m3.group(2)), 0)
                elif oo == 'jne' and prev_test:
                    const[prev_test] = 0        # falling through 'test r,r; jne' means r == 0
                elif d64 and oo not in ('cmp', 'test', 'push', 'call', 'jmp') and not oo.startswith('j'):
                    const.pop(d64, None)
                    if not (oo == 'mov' and m3 and reg64(m3.group(2)) in adds):
                        adds.pop(d64, None)
                    else:
                        adds[d64] = adds[reg64(m3.group(2))]
                prev_test = reg64(m3.group(1)) if oo == 'test' and m3 and m3.group(1) == m3.group(2) else None
                edx = const.get('rdx')
                m = self.MEM.match(rr)
                if m and oo == 'mov' and m.group(6) in rmw:
                    size, r1, r2, sc, disp, val = m.groups()
                    base = bases.get(reg64(r1)) or bases.get(reg64(r2))
                    setb, clrb = rmw.pop(val)
                    if base is not None and setb:
                        out[cmd].append(('set_bits', base, num(disp or '0x0'), self.SIZES[size], setb))
                    if base is not None and clrb:
                        out[cmd].append(('clear_bits', base, num(disp or '0x0'), self.SIZES[size], (~clrb) & 0xffffffffffffffff))
                elif m and oo in ('mov', 'or', 'and', 'add', 'sub', 'xor'):
                    size, r1, r2, sc, disp, val = m.groups()
                    base = bases.get(reg64(r1)) or bases.get(reg64(r2))
                    if base is not None:
                        v = num(val) if val.startswith('0x') else const.get(reg64(val))
                        kind = 'store' if oo in ('mov', 'add', 'sub') else ('set_bits' if oo == 'or' else ('clear_bits' if oo == 'and' else oo))
                        if v is None and kind == 'store' and oo == 'mov' and reg64(val):
                            v = ('arg', adds.get(reg64(val), 0))    # a parsed value (plus an offset)
                        out[cmd].append((kind, base, num(disp or '0x0'), self.SIZES[size], v))
                if oo == 'call':
                    target = rr.split()[0]
                    if target == setter and edx is not None:
                        # value: a constant, or the parsed argument plus an offset
                        val = const['r8'] if 'r8' in const else ('arg', adds.get('r8', 0))
                        out[cmd].append(('ability', None, edx, None, val))
                    else:
                        out[cmd].append(('call', None, target, None, None))
                    for r in ('rax', 'rcx', 'rdx', 'r8', 'r9', 'r10', 'r11'):
                        const.pop(r, None)
                        adds.pop(r, None)
                if oo == 'jmp' and edx is not None and setter:
                    t = self.index.get(num(rr.split()[0]))
                    if t is not None and any(x[1] == 'call' and x[2].split()[0] == setter for x in self.ins[t:t + 10]):
                        out[cmd].append(('ability', None, edx, None, self._tail_value(t, const, adds, setter)))
                k += 1
        return out

    def _tail_value(self, t, const, adds, setter):
        """The value a shared tail (several commands jump to one setter call) passes, given
        the registers at the jump: constants moved into r8 directly or via another register;
        else the parsed argument."""
        c = dict(const)
        for a, oo, rr, tt in self.ins[t:t + 10]:
            if oo == 'call':
                if rr.split()[0] != setter:
                    return None
                return c['r8'] if 'r8' in c else ('arg', adds.get('r8', 0))
            dst = rr.split(',')[0]
            d64 = reg64(dst)
            m = re.match(r'(\w+),(0x[0-9a-f]+)$', rr)
            m2 = re.match(r'(\w+),\[(\w+)([+-])(0x[0-9a-f]+)\]$', rr)
            m3 = re.match(r'(\w+),(\w+)$', rr)
            if oo == 'mov' and m and d64:
                c[d64] = num(m.group(2))
            elif oo == 'xor' and m3 and m3.group(1) == m3.group(2):
                c[d64] = 0
            elif oo == 'mov' and m3 and reg64(m3.group(2)) in c:
                c[d64] = c[reg64(m3.group(2))]
            elif oo == 'lea' and m2 and reg64(m2.group(2)) in c:
                c[d64] = c[reg64(m2.group(2))] + (1 if m2.group(3) == '+' else -1) * num(m2.group(4))
            elif d64:
                c.pop(d64, None)
        return None

    def record_layout(self, ctx):
        """Fields, flag bits and abilities of an entity record, with the commands that write them."""
        br = self.branches(ctx)
        base_use = collections.Counter(e[1] for effs in br.values() for e in effs if e[1] is not None)
        if not base_use:
            return None
        base = base_use.most_common(1)[0][0]
        fields = collections.defaultdict(set)
        bits = collections.defaultdict(lambda: collections.defaultdict(set))
        abilities = collections.defaultdict(set)
        for cmd, effs in br.items():
            for kind, b, off, size, val in effs:
                if kind == 'ability':
                    abilities[off].add(cmd)
                elif b != base:
                    continue
                elif kind == 'store':
                    fields[(off, size)].add(cmd)
                elif kind in ('set_bits', 'clear_bits') and val is not None:
                    v = val if kind == 'set_bits' else (~val) & ((1 << (8 * size)) - 1)
                    for bit in range(8 * size):
                        if v >> bit & 1:
                            bits[off][1 << bit].add(cmd + ('' if kind == 'set_bits' else ' (clears)'))
        return {
            'record_base': hex(base),
            'fields': {'0x%x/%d' % k: sorted(v) for k, v in sorted(fields.items())},
            'flag_bits': {hex(off): {hex(bit): sorted(c) for bit, c in sorted(bv.items())} for off, bv in sorted(bits.items())},
            'abilities_direct': {str(k): sorted(v) for k, v in sorted(abilities.items())},
        }

    def context_commands(self):
        out = {}
        for ctx, fs in self.contexts.items():
            out[ctx] = sorted({s for f in fs for s in self.by_func[f]})
        return out

    def generic_calls(self):
        """(context, command, ability key) for every call to a generic handler."""
        return [(c['context'], c['command'], c['key']) for c in self.generic_call_args()]

    def const_registers(self, f):
        """Callee-saved registers a function only ever sets to one constant (the compiler keeps
        0 or 1 in them and builds other constants from them: lea r9d,[rdi+0x2])."""
        if not hasattr(self, '_const_regs'):
            self._const_regs = {}
        if f not in self._const_regs:
            end = next(e for b, e in self.exe.functions() if b == f)
            writes = collections.defaultdict(set)
            for a, oo, rr, tt in self.ins[self.index[f]:]:
                if a >= end:
                    break
                d = reg64(rr.split(',')[0])
                if d in ('rbx', 'rbp', 'rsi', 'rdi', 'r12', 'r13', 'r14', 'r15') and oo not in ('push', 'pop', 'cmp', 'test') \
                        and not oo.startswith('j') and ',' in rr:
                    src = rr.split(',', 1)[1]
                    if oo == 'xor' and reg64(src) == d:
                        writes[d].add(0)
                    elif oo == 'mov' and re.match(r'0x[0-9a-f]+$', src):
                        writes[d].add(num(src))
                    else:
                        writes[d].add(None)
            self._const_regs[f] = {r: w.pop() for r, w in writes.items() if len(w) == 1 and None not in w}
        return self._const_regs[f]

    def generic_call_args(self):
        """Every call to a generic handler with its arguments: ability key (r8d), minimum (r9),
        maximum ([rsp+0x20]), kind ([rsp+0x28]: % 10 = 0 number, 1 monster, 2 spell, 3 item,
        4 site, 6 nation; the tens look like repeatability) and an offset added to the stored
        value ([rsp+0x30]). A command whose minimum equals its maximum takes no argument and
        stores that value. Values come from following register constants from the previous
        call (plus registers the function keeps at 0); unknown ones are None."""
        ctx_of = {f: c for c, fs in self.contexts.items() for f in fs}
        starts = sorted(ctx_of)
        res = []
        for i, (a, op, args, tgt) in enumerate(self.ins):
            if op != 'call' or args.split()[0] not in self.generic:
                continue
            j = i - 1
            while j > 0 and self.ins[j][1] != 'call' and i - j < 60:
                j -= 1
            fn = cmd = None
            for k in range(j + 1, i):
                if k in self.refs and self.ins[k][2].startswith('rdx,'):
                    fn, cmd = self.refs[k]
            if not cmd:
                continue
            reg = dict(self.const_registers(fn)) if fn in ctx_of else {}
            stack = {}
            for k in range(j + 1, i):
                aa, oo, rr, tt = self.ins[k]
                dst = rr.split(',')[0]
                d = reg64(dst)
                m = re.match(r'(\w+),(0x[0-9a-f]+|\d+)$', rr)
                m2 = re.match(r'(\w+),\[(\w+)([+-])(0x[0-9a-f]+)\]$', rr)
                m3 = re.match(r'(\w+),(\w+)$', rr)
                ms = re.match(r'(?:QWORD|DWORD) PTR \[rsp\+(0x20|0x28|0x30)\],(\w+)$', rr)
                if oo == 'mov' and ms:
                    v = ms.group(2)
                    if re.match(r'0x[0-9a-f]+$|\d+$', v):
                        stack[ms.group(1)] = signed(num(v), 64 if rr.startswith('QWORD') else 32)
                    else:
                        stack[ms.group(1)] = reg.get(reg64(v))
                elif oo in ('mov', 'movabs') and m and d:
                    reg[d] = num(m.group(2)) if dst != d else signed(num(m.group(2)))
                elif oo == 'xor' and m3 and reg64(m3.group(2)) == d and d:
                    reg[d] = 0
                elif oo == 'mov' and m3 and d and reg64(m3.group(2)) in reg:
                    reg[d] = reg[reg64(m3.group(2))]
                elif oo == 'lea' and m2 and d and reg64(m2.group(2)) in reg:
                    reg[d] = reg[reg64(m2.group(2))] + (1 if m2.group(3) == '+' else -1) * num(m2.group(4))
                elif oo == 'or' and m and d and m.group(2) in ('0xffffffffffffffff', '0xffffffff'):
                    reg[d] = -1
                elif d and oo not in ('cmp', 'test', 'push') and not oo.startswith('j'):
                    reg.pop(d, None)
            reg = {'r8': reg.get('r8'), 'r9': reg.get('r9')}
            kind = stack.get('0x28')
            res.append({'context': ctx_of.get(fn), 'command': cmd, 'key': reg.get('r8'), 'min': reg.get('r9'),
                        'max': stack.get('0x20'), 'kind': None if kind is None else kind % 10,
                        'repeat': None if kind is None else kind // 10, 'offset': stack.get('0x30') or 0, 'at': i})
        return res

    def direct_ability_sets(self, setter):
        """(command, key) for direct calls to the monster ability setter (#magicboost, #sailing, ...)."""
        res = []
        for i, (a, op, args, tgt) in enumerate(self.ins):
            if op != 'call' or args.split()[0] != setter:
                continue
            key = cmd = None
            for j in range(i - 1, max(i - 60, 0), -1):
                aa, oo, rr, tt = self.ins[j]
                m = re.match(r'edx,(0x[0-9a-f]+|\d+)$', rr)
                if key is None and oo == 'mov' and m:
                    key = num(m.group(1))
                if j in self.refs and rr.startswith('rdx,'):
                    cmd = self.refs[j][1]
                    break
            if cmd:
                res.append((cmd, key))
        return res

    def immediates_in(self, ctx, values):
        """For each value, the commands whose branch in the given parser uses it as a constant:
        the nearest command name compared before the instruction."""
        fs = self.contexts[ctx]
        found = collections.defaultdict(set)
        last = None
        for i, (a, op, args, tgt) in enumerate(self.ins):
            if i in self.refs:
                f, s = self.refs[i]
                last = s if f in fs else None
            if last is None:
                continue
            for m in re.finditer(r'(?:,|\+)(0x[0-9a-f]+)\]?$', args):
                v = int(m.group(1), 16)
                if v in values:
                    found[v].add(last)
        return found

    def ability_setter(self):
        """The function both generic handlers end in to add an ability to a monster: the callee
        of the #magicboost branch (the game's own name for the call is unknown)."""
        # #magicboost 51 <n> adds ability 20 (all paths): the call right after loading edx = 20
        idx = [i for i, (f, s) in self.refs.items() if s == 'magicboost' and f in self.contexts['monster']]
        for i in idx:
            window = self.ins[i:i + 80]
            for k, (a, op, args, tgt) in enumerate(window):
                if op == 'mov' and args == 'edx,0x14':
                    for a2, op2, args2, t2 in window[k + 1:k + 6]:
                        if op2 == 'call':
                            return args2.split()[0]
        return None


# ---------------------------------------------------------------------------------------------
# Monster table

MON_RECORD_CHECK = [(1, 'Logrian Slinger'), (2, 'Standard'), (3, 'Serpent Cataphract')]
MON_NAME_LEN = 0x24
MON_STATS = (0x28, 12)          # 12 int16
# names from the parser's store instructions (dom6exe.py layout)
MON_STAT_NAMES = ['ap', 'mapmove', 'size', 'hp', 'prot', 'str', 'enc', 'prec', 'att', 'def', 'mr', 'mor']
MON_FIELDS = {'sprite': (0x24, '<i'), 'gcost': (0x35e, '<h'), 'rcost': (0x360, '<h'), 'rpcost': (0x364, '<i'),
              'flags': (0x368, '<Q'), 'flags2': (0x370, '<I'), 'body': (0x374, '<I')}
MON_WEAPONS = (0x340, 10)       # int16 weapon numbers (#weapon)
MON_ARMOR = (0x354, 5)          # int16 armor numbers (#armor)
MON_ABILITIES = (0x40, 48)      # 48 (int64 key, int64 value)
MON_MAX = 20000                 # the parser rejects monster numbers above 19999 (cmp 0x4e1f)


def monster_table(exe):
    d = exe.data
    a = d.find(b'\0' + MON_RECORD_CHECK[0][1].encode() + b'\0') + 1
    b = d.find(b'\0' + MON_RECORD_CHECK[1][1].encode() + b'\0', a) + 1
    size = b - a
    for i, name in MON_RECORD_CHECK:
        o = a + (i - 1) * size
        if d[o:o + len(name)].decode('latin1') != name:
            raise SystemExit('monster table not recognized (record %d is not %r)' % (i, name))
    return a - size, size     # file offset of record 0, record size


def monsters(exe):
    start, size = monster_table(exe)
    va, raw, rsz, vsz = exe.sections['.data']
    out = {}
    i = 0
    while i < MON_MAX and start + (i + 1) * size <= raw + rsz:
        r = exe.data[start + i * size:start + (i + 1) * size]
        name = text(r[:MON_NAME_LEN].split(b'\0')[0])
        if name:
            ab, ablist = {}, []
            for k in range(MON_ABILITIES[1]):
                key, val = struct.unpack_from('<qq', r, MON_ABILITIES[0] + 16 * k)
                if key:
                    ab.setdefault(key, val)
                    ablist.append((key, val))
            rec = {'name': name}
            rec.update(zip(MON_STAT_NAMES, struct.unpack_from('<%dh' % MON_STATS[1], r, MON_STATS[0])))
            for f, (off, fmt) in MON_FIELDS.items():
                rec[f] = struct.unpack_from(fmt, r, off)[0]
            rec['weapons'] = [w for w in struct.unpack_from('<%dh' % MON_WEAPONS[1], r, MON_WEAPONS[0]) if w]
            rec['armor'] = [w for w in struct.unpack_from('<%dh' % MON_ARMOR[1], r, MON_ARMOR[0]) if w]
            rec['abilities'] = ab           # first value per ability
            rec['ability_list'] = ablist    # in order; repeatable abilities (#startitem, ...) can recur
            out[i] = rec
        i += 1
    # layout check: Heavy Cavalry rides mount 3515 (ability 1015) in Dominions 6.37
    hc = out.get(20)
    if not hc or hc['name'] != 'Heavy Cavalry' or not hc['abilities']:
        raise SystemExit('monster abilities not where expected; the record layout changed')
    return out


def flag_names(mons, inspector_dir, word, bits):
    """Name flag bits after the dom6inspector unit CSV column that is set exactly on the monsters
    with the bit (hints only)."""
    path = os.path.join(inspector_dir, 'gamedata', 'BaseU.csv') if inspector_dir else None
    if not path or not os.path.exists(path):
        return {}
    rows = {int(r['id']): r for r in csv.DictReader(open(path), delimiter='\t')}
    cols = [c for c in next(iter(rows.values())) if c not in ('id', 'name', 'end')]
    names = {}
    for bit in bits:
        have = {i for i, m in mons.items() if i in rows and m[word] & bit}
        best = None
        for c in cols:
            cset = {i for i, r in rows.items() if (r.get(c) or '') not in ('', '0')}
            if not cset & have:
                continue
            ratio = len(cset & have) / len(cset | have)
            if best is None or ratio > best[1]:
                best = (c, ratio)
        if best and best[1] >= 0.9:
            names[bit] = best[0]
    return names


# ---------------------------------------------------------------------------------------------
# All vanilla tables. Each is found from a few vanilla names at consecutive ids; `count` is the
# table's size (the game's id range for that type: the memory after it belongs to another table).

TABLES = {
    'monster': {'names': ['Logrian Slinger', 'Standard', 'Serpent Cataphract'], 'first': 1, 'count': 20000, 'csv': 'BaseU.csv'},
    'weapon': {'names': ['Spear', 'Pike', 'Trident'], 'first': 1, 'count': 4000, 'csv': 'weapons.csv'},
    'armor': {'names': ['Buckler', 'Shield', 'Kite Shield'], 'first': 1, 'count': 2000, 'csv': 'armors.csv'},
    'item': {'names': ['Fire Sword', 'Ice Sword'], 'first': 1, 'count': 2000, 'csv': 'BaseI.csv'},
    'spell': {'names': ['Minor Area Shock', 'Major Area Shock'], 'first': 1, 'count': 8000, 'csv': 'spells.csv'},
    'site': {'names': ['The Smouldercone', 'The Coral Towers', 'Cathedral of the Spheres'], 'first': 1, 'count': 4000, 'csv': 'MagicSites.csv'},
    'nation': {'names': ['Independents', 'Special Monsters'], 'first': 0, 'count': 500, 'csv': 'nations.csv'},
}
FIELD_FORMATS = {'i8': '<b', 'u8': '<B', 'i16': '<h', 'u16': '<H', 'i32': '<i', 'u32': '<I', 'i64': '<q'}


def find_table(exe, spec):
    """(file offset of id 0, record size): the names at consecutive ids, one record apart."""
    d = exe.data
    names = [n.encode() for n in spec['names']]
    i = 0
    while True:
        i = d.find(b'\0' + names[0] + b'\0', i)
        if i < 0:
            raise SystemExit('table not found: %r' % spec['names'])
        a = i + 1
        j = d.find(b'\0' + names[1] + b'\0', a, a + 0x4000)
        if j > 0:
            size = j + 1 - a
            if all(d[a + k * size:a + k * size + len(n)] == n and d[a + k * size + len(n)] == 0 for k, n in enumerate(names)):
                return a - spec['first'] * size, size
        i += 1


def table_records(exe, spec):
    start, size = find_table(exe, spec)
    out = {}
    for i in range(spec['count']):
        r = exe.data[start + i * size:start + (i + 1) * size]
        if r[:1] != b'\0' or any(r[:64]):
            out[i] = r
    return start, size, out


def match_fields(records, csv_path):
    """CSV column -> (offset, format) whose value agrees on every record (exact matches only)."""
    rows = {int(r['id']): r for r in csv.DictReader(open(csv_path), delimiter='\t')}
    ids = sorted(i for i in rows if i in records)
    if not ids:
        return {}
    size = len(records[ids[0]])
    vectors = collections.defaultdict(list)
    for t, f in FIELD_FORMATS.items():
        w = struct.calcsize(f)
        for off in range(0, size - w + 1):
            vec = tuple(struct.unpack_from(f, records[i], off)[0] for i in ids)
            vectors[vec].append('0x%x/%s' % (off, t))
    out = {}
    for col in rows[ids[0]]:
        if col in ('id', 'name', 'end'):
            continue
        vals = [(rows[i].get(col) or '0') for i in ids]
        if not all(re.fullmatch(r'-?\d+', v) for v in vals) or len(set(vals)) < 2:
            continue
        hit = vectors.get(tuple(int(v) for v in vals))
        if hit:
            out[col] = hit
    return out


def ability_names(mons, inspector_dir):
    """Name ability keys after the dom6inspector unit CSV column whose values agree (hints only)."""
    path = os.path.join(inspector_dir, 'gamedata', 'BaseU.csv') if inspector_dir else None
    if not path or not os.path.exists(path):
        return {}
    rows = {int(r['id']): r for r in csv.DictReader(open(path), delimiter='\t')}
    cols = [c for c in next(iter(rows.values())) if c not in ('id', 'name', 'end')]
    nonzero = {c: {i for i, r in rows.items() if (r.get(c) or '') not in ('', '0')} for c in cols}
    holders = collections.defaultdict(dict)
    for i, m in mons.items():
        if i in rows:
            for k, v in m['abilities'].items():
                holders[k][i] = str(v)
    names = {}
    for key, h in holders.items():
        match = collections.Counter(c for i, v in h.items() for c in cols if rows[i].get(c) == v)
        best = None
        for c, agree in match.items():
            ratio = agree / len(set(h) | nonzero[c])
            if best is None or ratio > best[1]:
                best = (c, ratio)
        if best and best[1] >= 0.9:
            names[key] = best[0]
    return names


# ---------------------------------------------------------------------------------------------

def cmd_commands(exe, args):
    p = Parser(exe)
    gen = p.generic_calls()
    keys = collections.defaultdict(dict)
    for ctx, cmd, key in gen:
        if ctx in ('monster', 'item') and key is not None:
            keys[ctx][cmd] = key
    setter = p.ability_setter()
    p._setter = setter
    if setter:
        for cmd, key in p.direct_ability_sets(setter):
            if key is not None:
                keys['monster'].setdefault(cmd, key)
    return {
        'game_version': exe.version, 'exe_sha256_16': exe.sha,
        'generic_handlers': p.generic,
        'ability_setter': setter,
        'contexts': p.context_commands(),
        'ability_keys': {ctx: dict(sorted(v.items())) for ctx, v in keys.items()},
    }


def cmd_layout(exe, args):
    p = Parser(exe)
    p._setter = p.ability_setter()
    out = {'game_version': exe.version, 'exe_sha256_16': exe.sha}
    for ctx in p.contexts:
        if ctx != 'top':
            lay = p.record_layout(ctx)
            if lay:
                out[ctx] = lay
    return out


# Contexts whose parser compares every command name it reads; the event parser also reads
# commands by pattern (#2d6units, #3com), so its list is incomplete.
CATALOG_COMPLETE = {'monster', 'item', 'weapon', 'armor', 'spell', 'site', 'nation', 'merc', 'poptype',
                    'nametype', 'bless', 'template'}


def command_effects(p):
    """What each command does to its entity's record, per context, for Dom5Parser's resolver
    (what an entity ends up with after a mod's lines). Keys name what a command writes: 'f<off>'
    a record field, 'a<n>' an ability, '<off>:<mask>' flag bits. Per command:
      set    keys it replaces (a later command setting all of an earlier one's keys replaces it)
      add    keys it appends to (repeatable: #batstartsum1, #restricted)
      or     keys it ORs into (item restrictions)
      del    abilities it removes (#humanoid removes the item-slot ability)
      bits / clears   flag bits it sets / clears
      values          the constant a command stores, by key, when it isn't the argument
                      (#quadruped stores item slots 786432)
      min / max       the argument's range, for commands read by a generic handler (it clamps
                      to it); min = max: the command takes no argument
      optional        the argument may be left out
    Commands with none of these do something the reader doesn't model (#weapon, #copystats, the
    nation lists); Dom5Parser has rules for those."""
    import vanilla_dm
    setters = {'monster': p.ability_setter(),
               'weapon': vanilla_dm.setter_of(p, 'weapon', 'fireifhit'),
               'armor': vanilla_dm.setter_of(p, 'armor', 'ironarmor'),
               'nation': vanilla_dm.setter_of(p, 'nation', 'startunitnbrs1')}
    generic = collections.defaultdict(list)
    for c in p.generic_call_args():
        if c['key'] is not None:
            generic[(c['context'], c['command'])].append(c)
    out = {}
    for ctx in CATALOG_COMPLETE | {'event'}:
        p._setter = setters.get(ctx)
        br = p.branches(ctx)
        bases = collections.Counter(e[1] for effs in br.values() for e in effs if e[1] is not None)
        base = bases.most_common(1)[0][0] if bases else None
        direct = vanilla_dm.nation_direct_keys(p, p._setter) if ctx == 'nation' else {}
        effects = {}
        for cmd in sorted(p.context_commands().get(ctx, [])):
            e = collections.defaultdict(set)
            arg = {}
            for c in generic.get((ctx, cmd), []):
                mode = {0: 'set', 1: 'add', 2: 'or'}.get(c['repeat'])
                if mode:
                    e[mode].add('a%d' % c['key'])
                if not arg and c['min'] is not None and c['max'] is not None:
                    arg = {'min': c['min'], 'max': c['max']}
                    if c['kind'] == 5:
                        arg['optional'] = True
            values = {}
            for kind, b, off, size, val in br.get(cmd, []):
                if kind == 'ability':
                    e['set' if val != 0 else 'del'].add('a%d' % off)
                    if isinstance(val, int) and val != 0:
                        values['a%d' % off] = val - (1 << 64) if val >= 1 << 63 else val
                elif b != base or base is None:
                    continue
                elif kind == 'store':
                    e['set'].add('f%d' % off)
                    if isinstance(val, int):
                        values['f%d' % off] = val - (1 << 64) if val >= 1 << 63 else val
                elif kind == 'set_bits' and val:
                    e['bits'].add('%d:%d' % (off, val & ((1 << 8 * size) - 1)))
                elif kind == 'clear_bits' and val is not None and ~val & ((1 << 8 * size) - 1):
                    e['clears'].add('%d:%d' % (off, ~val & ((1 << 8 * size) - 1)))
            if cmd in direct:
                e['set'].add('a%d' % direct[cmd])
            if values:
                arg['values'] = dict(sorted(values.items()))
            if e or arg:
                effects[cmd] = dict({k: sorted(v) for k, v in sorted(e.items())}, **arg)
        out[ctx] = effects
    return out


def cmd_catalog(exe, args):
    """The commands Dominions reads, per entity type, for Dom5Parser (Dom5Edit/GameData)."""
    p = Parser(exe)
    ctx = p.context_commands()
    top = set(ctx.pop('top', []))
    # a parser's entry chunk can read a mod-level command besides its #new: the merc parser's
    # compares "newmerc" and "clearmercs" (#clearmercs removes every vanilla band), in a chunk
    # without the dispatcher's anchors
    for names in p.by_func.values():
        if 'newmerc' in names:
            top |= {n for n in names if n.startswith('clear')}
    effects = command_effects(p)
    return {
        'game_version': exe.version, 'exe_sha256_16': exe.sha,
        'note': 'Written by tools/dom6exe (catalog). A command missing from a complete context is '
                'not read by the game for that entity type. "effects": what each command writes '
                '(command_effects in dom6exe.py).',
        'top': sorted(top),
        'contexts': {c: {'complete': c in CATALOG_COMPLETE, 'commands': sorted(v), 'effects': effects.get(c, {})}
                     for c, v in sorted(ctx.items())},
    }


def cmd_vanilla(exe, args):
    """Vanilla data as .dm commands (vanilla_dm.py, vanilla_other.py); --out names the .dm file."""
    import vanilla_dm
    res = vanilla_dm.write(exe, args.out)
    args.out = None
    return res


def cmd_events(exe, args):
    """Vanilla events as #selectevent blocks (events.py); --out names the .dm file."""
    import events
    res = events.write(exe, args.out, messages=args.messages)
    args.out = None
    return res


def cmd_sprites(exe, args):
    """Vanilla monster and item sprite numbers, and the site picture rule (sprites.py)."""
    import sprites
    return sprites.collect(exe)


def cmd_texts(exe, args):
    """Where the game's texts (descriptions, nation summaries, ...) are (texts.py); --out names the JSON."""
    import texts
    res = texts.write(exe, args.out)
    args.out = None
    return res


def cmd_spelleffects(exe, args):
    """What a spell's #damage means for each #effect (spelleffects.py)."""
    import spelleffects
    return spelleffects.collect(exe)


def cmd_dmread(exe, args):
    """How the game reads a .dm file (dmread.py): without --mod, the rules read from the exe;
    with --mod, where the game reads that file differently from a line-by-line reading, or with
    --lines A-B what each pass reads on those lines."""
    import dmread
    rules = dmread.Rules(exe)
    if not args.mod:
        return dict({'game_version': exe.version, 'exe_sha256_16': exe.sha}, **rules.summary())
    data = open(args.mod, 'rb').read()
    ctxs = args.context.split(',') if args.context else None
    if args.lines:
        a, _, b = args.lines.partition('-')
        for line in dmread.lines(data, rules, int(a), int(b or a), ctxs):
            print(line)
        return None
    return dmread.differences(data, rules, ctxs)


def cmd_tables(exe, args):
    out = {'game_version': exe.version, 'exe_sha256_16': exe.sha}
    for typ, spec in TABLES.items():
        start, size, recs = table_records(exe, spec)
        named = [i for i, r in recs.items() if r[:1] != b'\0']
        entry = {'file_offset': hex(start), 'address': hex(exe.address(start) or 0), 'record_size': size,
                 'count': spec['count'], 'vanilla_entries': len(named), 'last_vanilla_id': max(named) if named else None}
        csv_path = os.path.join(args.inspector, 'gamedata', spec['csv']) if args.inspector else None
        if csv_path and os.path.exists(csv_path):
            entry['fields_matching_inspector_csv'] = match_fields({i: recs[i] for i in named}, csv_path)
        out[typ] = entry
    return out


def cmd_monsters(exe, args):
    mons = monsters(exe)
    return {'game_version': exe.version, 'exe_sha256_16': exe.sha, 'count': len(mons), 'monsters': mons}


def cmd_readonly(exe, args):
    c = cmd_commands(exe, args)
    mons = monsters(exe)
    settable = set(c['ability_keys'].get('monster', {}).values())
    # #magicboost writes 10 + path (0-9) and 20-22 for the multi-path forms
    if 'magicboost' in c['contexts']['monster']:
        settable |= set(range(10, 23))
    names = ability_names(mons, args.inspector)
    used = collections.Counter(k for m in mons.values() for k in m['abilities'])
    # commands with their own handler: the ability number appears as a constant in their branch
    p = Parser(exe)
    candidates = p.immediates_in('monster', {k for k in used if k not in settable and k > 30})
    ro = []
    for k, n in sorted(used.items(), key=lambda kv: -kv[1]):
        if k in settable:
            continue
        holders = [i for i, m in mons.items() if k in m['abilities']]
        ro.append({'ability': k, 'name_hint': names.get(k), 'monsters': n,
                   'possibly_set_by': sorted(candidates.get(k, ())),
                   'examples': ['%d %s = %s' % (i, mons[i]['name'], mons[i]['abilities'][k]) for i in holders[:3]]})
    # flag bits: set by a command's OR into the flags words (layout), else read-only
    p._setter = p.ability_setter()
    lay = p.record_layout('monster')
    for k in lay['abilities_direct']:
        settable.add(int(k))
    ro = [x for x in ro if x['ability'] not in settable]
    flag_ro = []
    for word, off in (('flags', 0x368), ('flags2', 0x370)):
        setb = {int(b, 16) for b, cmds in lay['flag_bits'].get(hex(off), {}).items() if any('(clears)' not in c for c in cmds)}
        counts = collections.Counter(1 << b for m in mons.values() for b in range(64) if m[word] >> b & 1)
        unset = [bit for bit in counts if bit not in setb]
        fnames = flag_names(mons, args.inspector, word, unset)
        for bit, n in sorted(counts.items()):
            if bit not in setb:
                holders = [i for i, m in mons.items() if m[word] & bit]
                flag_ro.append({'word': word, 'bit': hex(bit), 'name_hint': fnames.get(bit), 'monsters': n,
                                'examples': ['%d %s' % (i, mons[i]['name']) for i in holders[:4]]})
    return {'game_version': exe.version, 'exe_sha256_16': exe.sha, 'vanilla_ability_keys': len(used),
            'settable_by_monster_commands': len(set(used) & settable), 'not_settable': ro, 'flag_bits_not_settable': flag_ro}


def cmd_snapshot(exe, args):
    """Everything the change log compares, from this exe, into the folder --out (changelog.py)."""
    import changelog
    if not args.out:
        raise SystemExit('snapshot: --out FOLDER (keep it outside the repository)')
    meta = changelog.snapshot(exe, args.out, texts=args.texts)
    print('wrote a snapshot of Dominions %s (exe %s) to %s%s' % (meta['game_version'], meta['exe_sha256_16'], args.out,
          ': it holds the game\'s texts (texts.json), keep it to yourself' if args.texts else ''), file=sys.stderr)
    args.out = None
    return None


def cmd_changelog(args):
    """What changed between two versions (changelog.py): OLD NEW, each a snapshot folder, an exe or a vanilla .dm."""
    import changelog
    if len(args.paths) != 2:
        raise SystemExit('changelog OLD NEW: two snapshot folders, Dominions6.exe files or vanilla .dm files')
    old, new = changelog.Version(args.paths[0], Exe), changelog.Version(args.paths[1], Exe)
    try:
        text = changelog.changelog(old, new, show_texts=args.texts)
    finally:
        old.close()
        new.close()
    if args.out:
        open(args.out, 'w', encoding='utf-8').write(text)
        print('wrote', args.out, file=sys.stderr)
    else:
        sys.stdout.write(text)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('what', choices=['catalog', 'changelog', 'commands', 'dmread', 'events', 'layout', 'monsters', 'readonly', 'snapshot', 'spelleffects', 'sprites', 'tables', 'texts', 'vanilla'])
    ap.add_argument('paths', nargs='*', help='changelog: OLD NEW (snapshot folders, Dominions6.exe files or vanilla .dm files)')
    ap.add_argument('--texts', action='store_true',
                    help='snapshot: keep the game\'s texts too (texts.json, local use only); changelog: show old and new texts')
    ap.add_argument('--exe', default=os.environ.get('DOM6_EXE', DEFAULT_EXE))
    ap.add_argument('--inspector', default=os.environ.get('DOM6INSPECTOR', '/mnt/c/Projects/dom6inspector'),
                    help='dom6inspector checkout, for naming ability numbers (hints only)')
    ap.add_argument('--out', help='write JSON here (default: stdout)')
    ap.add_argument('--messages', action='store_true',
                    help='events: include the messages (the game\'s text: for local use, not for the repo)')
    ap.add_argument('--mod', help='dmread: a .dm file to replay the game\'s reading on')
    ap.add_argument('--lines', help='dmread: A-B, list what each pass reads on these lines')
    ap.add_argument('--context', help='dmread: only these passes (monster,spell,...)')
    args = ap.parse_args()
    if args.what == 'changelog':
        return cmd_changelog(args)
    exe = Exe(args.exe)
    res = {'commands': cmd_commands, 'layout': cmd_layout, 'monsters': cmd_monsters, 'readonly': cmd_readonly,
           'tables': cmd_tables, 'vanilla': cmd_vanilla, 'catalog': cmd_catalog, 'events': cmd_events,
           'sprites': cmd_sprites, 'texts': cmd_texts, 'dmread': cmd_dmread, 'spelleffects': cmd_spelleffects,
           'snapshot': cmd_snapshot}[args.what](exe, args)
    if res is None:
        return
    text = json.dumps(res, indent=1, default=str)
    if args.out:
        open(args.out, 'w').write(text + '\n')
        print('wrote', args.out, file=sys.stderr)
    else:
        print(text)


if __name__ == '__main__':
    main()
