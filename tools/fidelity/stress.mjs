// Stress edits for the fidelity suite (stage 5): a few hundred editor operations over every entity
// type of a mod, each with an effect that follows from the edit itself, so the oracle's differences
// after all of them must be exactly the union of those effects. Seeded and deterministic.
//
// What an edit is expected to change is worked out from the edit and the oracle's own parse of the
// unedited save ("base"; "vanilla" is its parse of an empty mod, the game's data): "set unit hp 14"
// -> hp: base -> 14; "add weapon 8" -> weapons: base + [8]; "add a recruit" -> the nation's recruit
// set gains it; "reset hp" on a vanilla unit the mod changes -> hp: base -> the game's; "delete the
// mod's changes to it" -> every field back to the game's. Never from Dom5Parser's output. Field
// names and value forms are the oracle's (its scripts/parsemod.js).
//
// Entities are picked so one edit can't reach another entity: no copy sources (an edit to a
// template carries to its copies), at most one edit per field of an entity. Commands the oracle
// doesn't read (start army, nametypes, poptypes, blesses) are checked by re-reading the save with
// Dom5Parser ("reread"), and must change nothing the oracle sees; so is the order of an event's
// lines. The edits are applied in a shuffled order (created entities' edits stay after their create).

// mulberry32: small, seedable, deterministic
function rng(seed) {
	let a = seed >>> 0;
	return () => {
		a = (a + 0x6D2B79F5) >>> 0;
		let t = a;
		t = Math.imul(t ^ (t >>> 15), t | 1);
		t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
		return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
	};
}

// The mod's blocks, read from its text: which entities it writes (new or #select), the commands in
// their blocks (and the lines as written), and what its copy commands copy from.
const HEADERS = {
	newmonster: ['unit', 'new'], selectmonster: ['unit', 'select'],
	newweapon: ['wpn', 'new'], selectweapon: ['wpn', 'select'],
	newarmor: ['armor', 'new'], selectarmor: ['armor', 'select'],
	newitem: ['item', 'new'], selectitem: ['item', 'select'],
	newspell: ['spell', 'new'], selectspell: ['spell', 'select'],
	newsite: ['site', 'new'], selectsite: ['site', 'select'],
	newnation: ['nation', 'new'], selectnation: ['nation', 'select'],
	newevent: ['event', 'new'], selectevent: ['event', 'select'],
	newmerc: ['merc', 'new'],
	selectnametype: ['nametype', 'select'], selectpoptype: ['poptype', 'select'], selectbless: ['bless', 'select'],
	newtemplate: ['template', 'new'], selectsound: ['sound', 'select'],
};
const COPIES = { copystats: 'unit', copyspr: 'unit', copyweapon: 'wpn', copyarmor: 'armor', copyitem: 'item', copyspell: 'spell', copysite: 'site' };

function argOf(rest) {
	const s = rest.replace(/\s--.*$/, '').trim();
	const q = s.match(/^"([^"]*)"/);
	if (q) return { name: q[1].trim().toLowerCase() };
	const n = s.match(/^-?\d+/);
	return n ? { id: n[0] } : {};
}

function scanMod(text) {
	const blocks = []; // { type, how, arg: {id|name}, commands: Map(cmd -> count), lines: [raw], msg }
	const copies = []; // { type, arg }
	let cur = null;
	for (const raw of text.split(/\r?\n/)) {
		const line = raw.trim();
		if (!line.startsWith('#')) continue;
		const m = line.match(/^#(\w+)\s*(.*)$/);
		if (!m) continue;
		const cmd = m[1].toLowerCase();
		if (HEADERS[cmd]) {
			cur = { type: HEADERS[cmd][0], how: HEADERS[cmd][1], arg: argOf(m[2]), commands: new Map(), lines: [], index: blocks.length };
			blocks.push(cur);
			continue;
		}
		if (cmd === 'end') { cur = null; continue; }
		if (!cur) continue;
		cur.commands.set(cmd, (cur.commands.get(cmd) || 0) + 1);
		cur.lines.push(raw);
		if (cmd === 'msg' && cur.msg === undefined) cur.msg = (m[2].match(/^"([^"]*)/) || [])[1];
		if (COPIES[cmd]) copies.push({ type: COPIES[cmd], arg: argOf(m[2]) });
	}
	return { blocks, copies };
}

// name -> id as the oracle resolves it (the lowest id with that name)
function nameIndex(snap) {
	const idx = {};
	for (const [t, byId] of Object.entries(snap)) {
		const m = idx[t] = new Map();
		for (const [id, o] of Object.entries(byId)) {
			if (!o.name) continue;
			const k = String(o.name).trim().toLowerCase();
			if (!m.has(k) || Number(id) < Number(m.get(k))) m.set(k, id);
		}
	}
	return idx;
}

const sortedSet = (a) => [...new Set(a.filter((x) => x != null && x !== ''))].sort();
const PATHS = ['F', 'A', 'W', 'E', 'S', 'D', 'N', 'G', 'B', 'H'];
const same = (a, b) => JSON.stringify(a ?? null) === JSON.stringify(b ?? null);

export function generateStress({ text, vanillaText, gameCommands = null, oracleCommands = null, base, vanilla, seed = 1, size = 300 }) {
	const rand = rng(seed);
	const pick = (a) => a[Math.floor(rand() * a.length)];
	const int = (lo, hi) => lo + Math.floor(rand() * (hi - lo + 1));
	const shuffle = (a) => { const b = a.slice(); for (let i = b.length - 1; i > 0; i--) { const j = Math.floor(rand() * (i + 1)); [b[i], b[j]] = [b[j], b[i]]; } return b; };

	const { blocks, copies } = scanMod(text);
	const names = nameIndex(base);
	// the game's entities both parsers know: in the oracle's game data and in the editor's vanilla.dm
	// (the oracle's tables have placeholders, e.g. nations 82-84 "nation_82"), with vanilla.dm's commands
	const known = {};
	for (const b of scanMod(vanillaText).blocks) if (b.arg.id !== undefined) (known[b.type] ??= new Map()).set(String(Number(b.arg.id)), b.commands);
	const game = (t) => Object.keys(vanilla[t] || {}).filter((id) => Number(id) > 0 && known[t]?.has(id));
	const idOf = (type, arg) => (arg.id !== undefined ? String(Number(arg.id)) : arg.name !== undefined ? names[type]?.get(arg.name) : undefined);

	// copy sources can't be edited without reaching their copies
	const sources = {};
	for (const c of copies) { const id = idOf(c.type, c.arg); if (id) (sources[c.type] ??= new Set()).add(id); }
	// the mod's entities by type: id -> { how, commands (all blocks), lines (all blocks), blocks, vanilla }
	const own = {};
	for (const b of blocks) {
		if (b.type === 'event' && b.how === 'new') continue; // by index, below
		const id = idOf(b.type, b.arg);
		if (id === undefined) continue;
		const e = ((own[b.type] ??= new Map()).get(id)) || { how: b.how, commands: new Map(), lines: [], blocks: 0, vanilla: vanilla[b.type]?.[id] !== undefined, byName: false };
		for (const [c, n] of b.commands) e.commands.set(c, (e.commands.get(c) || 0) + n);
		e.lines.push(...b.lines);
		e.blocks++;
		e.byName ||= b.arg.id === undefined;
		// a #new on a game entity's number (Cataclysm's #newweapon 825): the game replaces the
		// entity, the oracle refuses the block, so it can't judge edits to it
		e.newOnGame ||= b.how === 'new' && b.arg.id !== undefined && vanilla[b.type]?.[id] !== undefined;
		own[b.type].set(id, e);
	}
	const isSource = (t, id) => sources[t]?.has(String(id));
	const copiesOrClears = (e) => [...e.commands.keys()].some((c) => c.startsWith('copy') || c.startsWith('clear'));
	// what an edit may target: the mod's new entities, vanilla ones it changes, vanilla ones it doesn't touch
	function pools(t) {
		const mine = [...(own[t]?.entries() || [])].filter(([id, e]) => base[t]?.[id] && !isSource(t, id) && !e.newOnGame);
		const fresh = mine.filter(([, e]) => !e.vanilla).map(([id]) => id);
		const selected = mine.filter(([id, e]) => e.vanilla && known[t]?.has(id)).map(([id]) => id);
		const untouched = game(t).filter((id) => !own[t]?.has(id) && !isSource(t, id) && base[t]?.[id]);
		return { fresh, selected, untouched };
	}

	const groups = [], expect = [], reread = [], skipped = [], counts = {};
	let group = null; // the edits one generator call makes (kept together when the order is shuffled)
	const used = new Set(); // type:id:field touched
	const whole = new Set(); // type:id with an edit to the whole entity (deleted)
	const touched = new Set(); // type:id with any edit
	const claim = (t, id, ...fields) => {
		if (whole.has(`${t}:${id}`) || fields.some((f) => used.has(`${t}:${id}:${f}`))) return false;
		for (const f of fields) used.add(`${t}:${id}:${f}`);
		touched.add(`${t}:${id}`);
		return true;
	};
	const claimWhole = (t, id) => {
		if (touched.has(`${t}:${id}`)) return false;
		whole.add(`${t}:${id}`);
		touched.add(`${t}:${id}`);
		return true;
	};
	const ENTITY = { unit: 'monster', wpn: 'weapon', armor: 'armor', item: 'item', spell: 'spell', site: 'site', nation: 'nation', merc: 'merc', event: 'event' };
	const value = (t, id, f) => base[t]?.[id]?.[f];
	// expect a field change unless the value stays the same
	function change(t, id, field, to) {
		const from = value(t, id, field) ?? null;
		if (same(from, to)) return false;
		expect.push({ type: t, id: String(id), field, from, to });
		return true;
	}
	const counted = (k) => { counts[k] = (counts[k] || 0) + 1; };
	const address = (t, id) => (t === 'event' && eventIndex.has(String(id)) ? { index: eventIndex.get(String(id)) } : t === 'merc' ? { index: 0 } : { id: Number(id) });
	function edit(t, id, e, kind) {
		group.push({ ...e, entity: ENTITY[t], ...address(t, id) });
		counted(kind);
	}
	// runs one generator in its own group; true if it made an edit
	function attempt(fn, id) {
		group = [];
		const made = fn(id);
		if (made && group.length) groups.push(group);
		group = null;
		return made;
	}

	// the mod's #newevent blocks, numbered by the oracle in file order after the game's events
	const modEventBlocks = blocks.filter((b) => b.type === 'event' && b.how === 'new');
	const modEventIds = Object.keys(base.event || {}).filter((id) => !(vanilla.event || {})[id]).sort((x, y) => x - y);
	const eventIndex = new Map();
	if (modEventIds.length === modEventBlocks.length && modEventBlocks.every((b, k) => (b.msg ?? null) === null || String(base.event[modEventIds[k]].description || '').startsWith(b.msg.trim().slice(0, 20))))
		modEventIds.forEach((id, k) => eventIndex.set(id, k));
	else if (modEventBlocks.length) skipped.push(`events: the oracle's numbering (${modEventIds.length}) doesn't line up with the mod's ${modEventBlocks.length} #newevent blocks`);
	const modEventBlock = new Map(modEventIds.map((id, k) => [id, modEventBlocks[k]]));

	// reference targets: the game's entities and the mod's own
	const modOwn = (t) => [...(own[t]?.entries() || [])].filter(([id, e]) => !e.vanilla && base[t]?.[id]).map(([id]) => id);
	const vanillaUnits = [...game('unit'), ...modOwn('unit')];
	const vanillaWeapons = [...game('wpn'), ...modOwn('wpn')];
	const vanillaArmor = [...game('armor'), ...modOwn('armor')];
	const mounts = sortedSet(Object.values(base.unit || {}).map((u) => u.mountmnr).filter((m) => /^\d+$/.test(String(m)) && Number(m) > 0 && base.unit[m]));
	const coriders = sortedSet(Object.values(base.unit || {}).map((u) => u.coridermnr).filter((m) => /^\d+$/.test(String(m)) && base.unit[m]));
	const pretenders = sortedSet(Object.values(base.nation || {}).flatMap((n) => n.addgod || []).filter((m) => base.unit[m]));

	// ---- one generator per kind of edit: (id) -> true if it made an edit ----
	const STATS = { unit: ['hp', 'att', 'def', 'prec', 'mr', 'mor', 'str', 'enc', 'prot', 'mapmove', 'ap'], wpn: ['dmg', 'att', 'def', 'len', 'nratt', 'rcost'], armor: ['def', 'enc', 'rcost'], item: ['constlevel', 'mainlevel'], site: ['gold', 'res', 'rarity', 'level'], merc: ['nrunits', 'level', 'minmen', 'minpay', 'xp', 'randequip', 'recrate', 'eramask'] };
	const RANGE = { hp: [1, 60], att: [0, 20], def: [0, 20], prec: [0, 20], mr: [4, 20], mor: [5, 30], str: [5, 25], enc: [0, 6], prot: [0, 20], mapmove: [2, 30], ap: [4, 30], dmg: [0, 25], len: [0, 6], nratt: [1, 4], rcost: [0, 30], constlevel: [0, 8], mainlevel: [1, 4], gold: [0, 200], res: [0, 100], rarity: [0, 5], level: [0, 4], nrunits: [1, 60], minmen: [1, 20], minpay: [10, 500], xp: [0, 100], randequip: [0, 3], recrate: [10, 100], eramask: [1, 7] };
	// the entity's one line "#field N" as written (no comment), for an edit as text; null if none or several
	function textLine(t, id, field) {
		const e = t === 'event' ? modEventBlock.get(id) : own[t]?.get(id);
		if (!e || (t !== 'event' && e.blocks !== 1)) return null;
		const found = e.lines.filter((l) => l.match(/^#(\w+)\s/)?.[1].toLowerCase() === field);
		return found.length === 1 && new RegExp(`^#${field} -?\\d+$`).test(found[0]) ? found[0] : null;
	}
	function setStat(t, id, kind = 'set ' + t + ' stat') {
		for (const f of shuffle(STATS[t])) {
			const [lo, hi] = RANGE[f];
			const n = int(lo, hi);
			if (String(value(t, id, f)) === String(n) || !claim(t, id, f)) continue;
			change(t, id, f, String(n));
			// some as the page's text box would: the line rewritten in the block
			const line = rand() < 0.3 ? textLine(t, id, f) : null;
			if (line) edit(t, id, { op: 'text', replace: [['\n' + line + '\n', `\n#${f} ${n}\n`]] }, kind + ' (as text)');
			else edit(t, id, { op: 'set', command: '#' + f, value: String(n) }, kind);
			return true;
		}
		return false;
	}
	// reset: the mod's own lines go, the game's value is back (a vanilla entity the mod changes, with no copy or clear)
	function resetStat(t, id) {
		const e = own[t]?.get(id);
		if (!e || !e.vanilla || copiesOrClears(e)) return false;
		const f = shuffle(STATS[t] || []).find((x) => e.commands.has(x) && !used.has(`${t}:${id}:${x}`));
		if (!f || !claim(t, id, f)) return false;
		change(t, id, f, vanilla[t][id][f] ?? null);
		edit(t, id, { op: 'reset', command: '#' + f }, 'reset ' + t + ' to the game\'s value');
		return true;
	}
	// delete the mod's changes to a vanilla entity: every field is the game's again. Not an entity
	// with a line the game or the oracle doesn't read (#protinspector): the oracle reports those
	// (a diagnostic), and they'd go too.
	const CONTEXT = { unit: 'monster', wpn: 'weapon', armor: 'armor', item: 'item', spell: 'spell', site: 'site', nation: 'nation' };
	const allRead = (t, e) => [...e.commands.keys()].every((c) => (!gameCommands || gameCommands[CONTEXT[t]]?.has(c))
		&& (!oracleCommands || oracleCommands[t]?.has(c) || oracleCommands[t]?.has('_' + c)));
	function deleteSelected(t, id) {
		const e = own[t]?.get(id);
		if (!e || !e.vanilla || e.byName || e.commands.has('name') || copiesOrClears(e) || !allRead(t, e) || !claimWhole(t, id)) return false;
		const a = base[t][id], b = vanilla[t][id];
		for (const f of new Set([...Object.keys(a), ...Object.keys(b)])) change(t, id, f, b[f] ?? null);
		edit(t, id, { op: 'delete' }, 'delete the mod\'s ' + t + ' changes');
		return true;
	}
	// a list entry added at the end (#weapon, #armor; a site's #homemon/#homecom/#mon/#com)
	// The oracle drops a reference list that is just "0" (#weapon 0: none) from its snapshot, so an
	// empty unit list may hide a "0" the edit is added after: those units are left out.
	const REF_LISTS = new Set(['weapons', 'armor']);
	const listOrNone = (list) => (!list.length || String(list) === '0' ? null : list); // the oracle's "reference 0 = none"
	function addToList(t, id, command, field, pool, kind) {
		if (REF_LISTS.has(field) && value(t, id, field) === undefined) return false;
		if (!claim(t, id, field)) return false;
		const w = pick(pool);
		change(t, id, field, [...(value(t, id, field) || []), String(w)]);
		edit(t, id, { op: 'add', command, value: String(w) }, kind);
		return true;
	}
	function removeFromList(t, id, command, field, kind) {
		const list = value(t, id, field) || [];
		const candidates = list.filter((x) => /^\d+$/.test(String(x)) && String(x) !== '0');
		if (!candidates.length || !claim(t, id, field)) return false;
		const w = pick(candidates);
		change(t, id, field, listOrNone(list.filter((x) => x !== w)));
		edit(t, id, { op: 'remove', command, value: String(w) }, kind);
		return true;
	}
	const FLAGS = ['flying', 'amphibian', 'female', 'holy', 'forestsurvival', 'mountainsurvival', 'swampsurvival', 'wastesurvival', 'trample', 'pierceres', 'slashres', 'bluntres'];
	function addFlag(id) {
		const f = shuffle(FLAGS).find((x) => value('unit', id, x) === undefined && !used.has(`unit:${id}:${x}`));
		if (!f || !claim('unit', id, f)) return false;
		change('unit', id, f, '1');
		edit('unit', id, { op: 'add', command: '#' + f }, 'add unit flag');
		return true;
	}
	// numeric abilities the game removes with "#x 0" (the oracle keeps "0"); the mod's own line of a #new unit goes
	const ABILITIES = ['fear', 'awe', 'regeneration', 'berserk', 'ambidextrous', 'darkvision', 'siegebonus', 'patrolbonus', 'castledef', 'supplybonus', 'invulnerable', 'reinvigoration', 'standard', 'inspirational', 'researchbonus', 'pillagebonus', 'animalawe', 'beastmaster', 'taskmaster', 'formationfighter', 'bodyguard'];
	function removeAbility(id) {
		const e = own.unit?.get(id);
		const inGame = known.unit?.get(id);
		const f = shuffle(ABILITIES).find((x) => {
			if (value('unit', id, x) === undefined || used.has(`unit:${id}:${x}`)) return false;
			if (!e) return inGame?.has(x); // untouched: vanilla.dm has it too
			if (e.vanilla) return !e.commands.has(x) && inGame?.has(x) && !copiesOrClears(e);
			return e.commands.get(x) === 1 && !copiesOrClears(e);
		});
		if (!f || !claim('unit', id, f)) return false;
		change('unit', id, f, e && !e.vanilla ? null : '0');
		edit('unit', id, { op: 'remove', command: '#' + f }, 'remove unit ability');
		return true;
	}
	function setPath(id) {
		const p = int(0, 8);
		const L = int(1, 4);
		if (String(value('unit', id, PATHS[p])) === String(L) || !claim('unit', id, PATHS[p])) return false;
		change('unit', id, PATHS[p], String(L));
		edit('unit', id, { op: 'set', command: '#magicskill', value: `${p} ${L}` }, 'set unit magic path');
		return true;
	}
	// mounted units: the rider's mount, co-rider and riding skill (fields mountmnr, coridermnr, skilledrider)
	function setMount(id) {
		const m = pick(mounts.filter((x) => x !== String(value('unit', id, 'mountmnr'))));
		if (!m || !claim('unit', id, 'mountmnr')) return false;
		change('unit', id, 'mountmnr', m);
		edit('unit', id, { op: 'set', command: '#mountmnr', value: m }, 'change rider mount');
		return true;
	}
	function setCorider(id) {
		const c = pick(coriders.length ? coriders : vanillaUnits);
		if (!c || String(value('unit', id, 'coridermnr')) === c || !claim('unit', id, 'coridermnr')) return false;
		change('unit', id, 'coridermnr', c);
		edit('unit', id, { op: 'set', command: '#coridermnr', value: c }, 'set rider co-rider');
		return true;
	}
	function setSkilledrider(id) {
		const n = String(int(1, 3));
		if (String(value('unit', id, 'skilledrider')) === n || !claim('unit', id, 'skilledrider')) return false;
		change('unit', id, 'skilledrider', n);
		edit('unit', id, { op: 'set', command: '#skilledrider', value: n }, 'set rider skill');
		return true;
	}
	// removing the mount: an inherited one is removed by "#mountmnr 0" (the game's ability setter
	// removes on 0; the oracle keeps "0"); the unit's own line of a #new unit with no copy goes
	function removeMount(id) {
		const e = own.unit?.get(id);
		const inherited = !e ? true : e.vanilla ? !e.commands.has('mountmnr') : false;
		const ownOnly = e && !e.vanilla && !copiesOrClears(e) && e.commands.get('mountmnr') === 1;
		if (!(inherited || ownOnly) || !claim('unit', id, 'mountmnr')) return false;
		change('unit', id, 'mountmnr', inherited ? '0' : null);
		edit('unit', id, { op: 'remove', command: '#mountmnr' }, 'remove rider mount');
		return true;
	}

	function spellEdit(id) {
		const s = base.spell[id];
		const r = rand();
		if (r < 0.4) {
			const n = String(int(0, 9));
			if (String(s.researchlevel) === n || !claim('spell', id, 'researchlevel')) return false;
			change('spell', id, 'researchlevel', n);
			edit('spell', id, { op: 'set', command: '#researchlevel', value: n }, 'set spell research level');
			return true;
		}
		if (r < 0.7) {
			// "#fatiguecost N": the oracle keeps N % 100 as fatiguecost and N / 100 as gemcost
			const n = int(0, 12) * 100 + pick([0, 0, 50]);
			if (!claim('spell', id, 'fatiguecost', 'gemcost')) return false;
			const f = change('spell', id, 'fatiguecost', String(n % 100));
			const g = change('spell', id, 'gemcost', String(Math.floor(n / 100)));
			if (!f && !g) return false;
			edit('spell', id, { op: 'set', command: '#fatiguecost', value: String(n) }, 'set spell cost');
			return true;
		}
		// the unit a summoning spell makes (#damage)
		if (!['1', '10001', '21', '10021', '37', '10037', '38', '10038'].includes(String(s.effect)) || !(Number(s.damage) > 0)) return false;
		const u = pick(vanillaUnits);
		if (String(s.damage) === u || !claim('spell', id, 'damage')) return false;
		change('spell', id, 'damage', u);
		edit('spell', id, { op: 'set', command: '#damage', value: u }, 'change summoned unit');
		return true;
	}

	function itemEdit(id) {
		const it = base.item[id];
		if (it.weapon && /^\d+$/.test(String(it.weapon)) && rand() < 0.4) {
			const w = pick(vanillaWeapons);
			if (w === String(it.weapon) || !claim('item', id, 'weapon')) return false;
			change('item', id, 'weapon', w);
			edit('item', id, { op: 'change', command: '#weapon', from: String(it.weapon), value: w }, 'change item weapon');
			return true;
		}
		return setStat('item', id);
	}

	const SITE_LISTS = [['#homemon', 'hmon'], ['#homecom', 'hcom'], ['#mon', 'mon'], ['#com', 'com']];
	function siteEdit(id) {
		if (rand() < 0.5) {
			const [command, field] = pick(SITE_LISTS);
			return addToList('site', id, command, field, vanillaUnits, 'add site unit');
		}
		return setStat('site', id);
	}

	// a nation's recruits, commanders and pretenders are sets to the oracle
	const NATION_LISTS = [['#addrecunit', 'units'], ['#addreccom', 'commanders'], ['#addgod', 'addgod']];
	function nationEdit(id) {
		const [command, field] = pick(NATION_LISTS);
		const pool = field === 'addgod' ? pretenders : vanillaUnits;
		const now = value('nation', id, field) || [];
		const u = pick(pool.filter((x) => !now.includes(x)));
		if (!u || !claim('nation', id, field)) return false;
		change('nation', id, field, sortedSet([...now, u]));
		edit('nation', id, { op: 'add', command, value: u }, 'add nation ' + field);
		return true;
	}
	// the mod's own recruit line removed (at parse time the oracle's lists hold only the mod's lines)
	function nationRemoveRecruit(id) {
		const e = own.nation?.get(id);
		if (!e || copiesOrClears(e)) return false;
		const now = value('nation', id, 'units') || [];
		const once = now.filter((u) => e.lines.filter((l) => new RegExp(`^#addrecunit\\s+${u}(\\s|$)`).test(l.trim())).length === 1);
		if (!once.length || !claim('nation', id, 'units')) return false;
		const u = pick(once);
		change('nation', id, 'units', listOrNone(now.filter((x) => x !== u)));
		edit('nation', id, { op: 'remove', command: '#addrecunit', value: u }, 'remove nation recruit');
		return true;
	}
	// what the oracle ignores: checked by re-reading the save
	function nationStartArmy(id) {
		if (!claim('nation', id, 'startcom')) return false;
		const u = pick(vanillaUnits);
		edit('nation', id, { op: 'set', command: '#startcom', value: u }, 'set nation start commander (re-read)');
		reread.push({ entity: 'nation', id: Number(id), command: '#startcom', values: [u] });
		return true;
	}

	// events: the rarity, a requirement or effect the event has (changed in place), the message, the
	// order of its lines
	const EVENT_FIELDS = ['gold', 'req_minpop', 'req_maxpop', 'req_turn', 'req_maxturn', 'req_mindef', 'req_maxdef', 'req_minunrest', 'req_maxunrest', 'req_dominion', 'req_mydominion', 'unrest', 'incdom', 'incpop', 'defence', 'req_rare', 'req_lab', 'req_temple', 'req_fort'];
	const RARITIES = ['-2', '-1', '0', '1', '2', '5', '10', '11', '12', '13'];
	let msgCount = 0;
	function eventEdit(id) {
		const ev = base.event[id];
		const b = modEventBlock.get(id);
		const cmds = b?.commands || new Map();
		const r = rand();
		if (r < 0.25) {
			// (an event without a rarity is an error the oracle reports; giving it one would change that)
			if (ev.rarity === undefined || !claim('event', id, 'rarity')) return false;
			const n = pick(RARITIES.filter((x) => x !== String(ev.rarity)));
			change('event', id, 'rarity', n);
			const line = rand() < 0.3 ? textLine('event', id, 'rarity') : null;
			if (line) edit('event', id, { op: 'text', replace: [['\n' + line + '\n', `\n#rarity ${n}\n`]] }, 'set event rarity (as text)');
			else edit('event', id, { op: 'set', command: '#rarity', value: n }, 'set event rarity');
			return true;
		}
		if (r < 0.65) {
			const f = shuffle(EVENT_FIELDS).find((x) => ev[x] !== undefined && /^-?\d+$/.test(String(ev[x])) && cmds.get(x) === 1 && !used.has(`event:${id}:${x}`));
			if (!f || !claim('event', id, f)) return false;
			const n = String(Number(ev[f]) + pick([-3, -2, -1, 1, 2, 5, 10]));
			change('event', id, f, n);
			edit('event', id, { op: 'change', command: '#' + f, from: String(ev[f]), value: n }, 'change event line');
			return true;
		}
		if (r < 0.85) {
			// the message: description, and the name the oracle makes from the first 25 characters
			if (cmds.get('msg') !== 1 || cmds.has('header') || !claim('event', id, 'description', 'name')) return false;
			const msg = `Stress message ${++msgCount}: the province ${pick(['rejoices', 'mourns', 'trembles', 'wonders'])} at ##landname##.`;
			change('event', id, 'description', msg);
			change('event', id, 'name', msg.substr(0, 25));
			edit('event', id, { op: 'set', command: '#msg', value: '"' + msg + '"' }, 'set event message');
			return true;
		}
		// line order (the oracle doesn't keep it): the message moved to the top, re-read
		const first = b?.lines[0]?.match(/^\s*#(\w+)/)?.[1].toLowerCase();
		if (!first || first === 'msg' || cmds.get('msg') !== 1 || cmds.has('header') || !claim('event', id, 'order')) return false;
		edit('event', id, { op: 'move', command: '#msg', before: '#' + first }, 'move event line (re-read)');
		reread.push({ entity: 'event', index: eventIndex.get(id), order: ['#msg', '#' + first] });
		return true;
	}

	// a band's #com and #unit are names to the game (Dominions6.exe's merc parser): the editor
	// writes the picked unit's name, which the oracle keeps lowercased. Units whose name finds
	// them (the lowest number with it), else the editor writes "Name (id)", which the oracle can't
	// resolve
	const mercUnits = vanillaUnits.filter((u) => base.unit?.[u]?.name && names.unit?.get(String(base.unit[u].name).trim().toLowerCase()) === u);
	function mercEdit(id) {
		if (rand() < 0.3 && mercUnits.length) {
			const [command, field] = pick([['#com', 'com'], ['#unit', 'unit']]);
			const u = pick(mercUnits);
			const name = String(base.unit[u].name).trim().toLowerCase();
			if (String(value('merc', id, field)) === name || !claim('merc', id, field)) return false;
			change('merc', id, field, name);
			edit('merc', id, { op: 'set', command, value: u }, 'change merc unit');
			return true;
		}
		return setStat('merc', id);
	}

	// ---- plan: how many of each, over which pools ----
	const plan = [];
	const unitPools = pools('unit');
	const riders = (ids) => ids.filter((id) => /^\d+$/.test(String(value('unit', id, 'mountmnr') ?? '')) && Number(value('unit', id, 'mountmnr')) > 0);
	const unitOps = [(id) => setStat('unit', id), (id) => addToList('unit', id, '#weapon', 'weapons', vanillaWeapons, 'add unit weapon'),
		(id) => removeFromList('unit', id, '#weapon', 'weapons', 'remove unit weapon'), (id) => addToList('unit', id, '#armor', 'armor', vanillaArmor, 'add unit armor'),
		(id) => removeFromList('unit', id, '#armor', 'armor', 'remove unit armor'), addFlag, setPath, removeAbility];
	const riderOps = [setMount, setCorider, setSkilledrider, removeMount, (id) => setStat('unit', id, 'set rider stat')];
	const share = (n) => Math.max(1, Math.round(size * n));
	plan.push(['unit (new)', unitPools.fresh, unitOps, share(0.11)]);
	plan.push(['unit (selected)', unitPools.selected, [...unitOps, (id) => resetStat('unit', id)], share(0.08)]);
	plan.push(['unit (untouched)', unitPools.untouched, unitOps, share(0.07)]);
	plan.push(['mounted unit (new)', riders(unitPools.fresh), riderOps, share(0.04)]);
	plan.push(['mounted unit (selected/untouched)', riders([...unitPools.selected, ...unitPools.untouched]), riderOps, share(0.05)]);
	plan.push(['mount', mounts.filter((id) => !isSource('unit', id)), [(id) => setStat('unit', id, 'set mount stat')], share(0.03)]);
	for (const [t, n, op] of [['wpn', 0.06, (id) => setStat('wpn', id)], ['armor', 0.04, (id) => setStat('armor', id)], ['item', 0.05, itemEdit],
		['spell', 0.07, spellEdit], ['site', 0.07, siteEdit], ['nation', 0.05, nationEdit]]) {
		const p = pools(t);
		plan.push([t + ' (mod)', [...p.fresh, ...p.selected], [op], share(n * 0.7)]);
		plan.push([t + ' (untouched)', p.untouched, [op], share(n * 0.3)]);
		if (t !== 'nation' && STATS[t]) plan.push([t + ' (selected: reset)', p.selected, [(id) => resetStat(t, id)], share(0.006)]);
	}
	for (const t of ['unit', 'wpn', 'armor', 'item', 'spell', 'site'])
		plan.push([t + ' (selected: delete)', pools(t).selected, [(id) => deleteSelected(t, id)], share(0.004)]);
	const np = pools('nation');
	plan.push(['nation (mod: remove recruit)', [...np.fresh, ...np.selected], [nationRemoveRecruit], share(0.01)]);
	plan.push(['event', [...eventIndex.keys()], [eventEdit], share(0.12)]);
	plan.push(['merc', Object.keys(base.merc || {}).filter((id) => !(vanilla.merc || {})[id]).slice(0, 1), [mercEdit], share(0.02)]);
	plan.push(['nation start army', [...np.fresh, ...np.selected, ...np.untouched], [nationStartArmy], share(0.02)]);

	for (const [label, ids, ops, n] of plan) {
		if (!ids.length) { skipped.push(`${label}: none in the mod`); continue; }
		let made = 0;
		const order = shuffle(ids);
		for (let tries = 0; made < n && tries < n * 20; tries++)
			if (attempt(pick(ops), order[tries % order.length])) made++;
		if (made < n) skipped.push(`${label}: ${made} of ${n}`);
	}

	// oracle-blind types: one value each, re-read
	const blind = [];
	const blindSeen = new Set();
	for (const b of blocks) {
		const key = b.arg.id ?? b.arg.name;
		if (!['nametype', 'bless', 'poptype'].includes(b.type) || key === undefined || blindSeen.has(b.type + ':' + key)) continue;
		blindSeen.add(b.type + ':' + key);
		// by number, or by name as the mod selects it (#selectbless "Fear")
		const at = b.arg.id !== undefined ? { id: Number(b.arg.id) } : { match: b.arg.name };
		if (b.type === 'nametype') blind.push(() => {
			const name = `Stressname${groups.length}`;
			group.push({ op: 'add', entity: 'nametype', ...at, command: '#addname', value: `"${name}"` });
			reread.push({ entity: 'nametype', ...at, command: '#addname', includes: name });
			counted('add nametype name (re-read)');
			return true;
		});
		if (b.type === 'bless') blind.push(() => {
			const n = String(int(1, 9));
			group.push({ op: 'set', entity: 'bless', ...at, command: '#cost0', value: n });
			reread.push({ entity: 'bless', ...at, command: '#cost0', values: [n] });
			counted('set bless cost (re-read)');
			return true;
		});
		if (b.type === 'poptype') blind.push(() => {
			const u = pick(vanillaUnits);
			group.push({ op: 'add', entity: 'poptype', ...at, command: '#addrecunit', value: u });
			reread.push({ entity: 'poptype', ...at, command: '#addrecunit', includes: u });
			counted('add poptype recruit (re-read)');
			return true;
		});
	}
	for (const f of shuffle(blind).slice(0, share(0.03))) attempt(f);

	// new entities, made in the editor and filled in
	const mk = (type, entity, as, name, sets, fields) => {
		group.push({ op: 'create', entity, name, as });
		for (const [command, v] of sets) group.push({ op: 'set', entity, ref: as, command, value: v });
		expect.push({ type, ref: as, new: { name, ...fields } });
		counted('create ' + entity);
		return true;
	};
	for (let k = 0; k < Math.max(1, Math.round(size / 100)); k++) {
		const hp = String(int(5, 40)), w = pick(vanillaWeapons), m = pick(mounts.length ? mounts : vanillaUnits);
		attempt(() => mk('unit', 'monster', 'unit' + k, `Stress Unit ${k}`, [['#hp', hp], ['#weapon', w], ['#mountmnr', m]], { hp, weapons: [w], mountmnr: m }));
		const dmg = String(int(1, 20));
		attempt(() => mk('wpn', 'weapon', 'wpn' + k, `Stress Weapon ${k}`, [['#dmg', dmg]], { dmg }));
		const prot = String(int(1, 20));
		attempt(() => mk('armor', 'armor', 'armor' + k, `Stress Armor ${k}`, [['#prot', prot], ['#type', '5']], { prot, type: '5' }));
		const gold = String(int(1, 100));
		attempt(() => mk('site', 'site', 'site' + k, `Stress Site ${k}`, [['#gold', gold]], { gold }));
	}
	const edits = shuffle(groups).flat();
	return { edits, expect, reread, skipped, counts };
}
