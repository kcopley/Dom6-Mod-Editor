#!/usr/bin/env node
// Fidelity suite: checks Dom5Parser against the dom6inspector oracle, by comparing what the
// inspector *holds in memory* after parsing, not file text.
//
//   1. Inspector self-check: load a mod in the inspector, export what it read, reload the
//      export -> the inspector's data must be functionally the same (compared after its
//      full post-processing). Validates the oracle itself.
//   2. (Retired 2026-10-05: the inspector's vanilla.dm export. vanilla.dm is now written from
//      Dominions6.exe by tools/dom6exe.)
//   3. Save fidelity: Dom5Parser load -> save -> the inspector's parse of the saved file must
//      equal its parse of the original (strict: compared right after parsing).
//   4. Edits: Dom5Parser load -> scripted edits -> save -> must differ from an unedited save
//      by exactly the expected changes. Every case is also undone (the save must be the
//      unedited save, byte for byte) and redone (the edited save, byte for byte).
//   5. Stress: a few hundred generated edits over every entity type of a big mod
//      (tools/fidelity/stress.mjs), in one session: exactly their expected changes, then
//      undo all / redo all byte for byte.
//
// Usage: node tools/fidelity/run.mjs [--oracle DIR] [--stages 1,3,4,5,6] [--only TEXT] [--quick]
//                                    [--stress] [--seed N] [--stress-size N] [--stress-mod FILE.dm] [--jobs N]
//                                    [--update-baselines] [--json summary.json]
// --quick = stages 3-4 (about 1-2 min); the default runs all stages (~15 min); --stress = stage 5
// only; --stress-mod FILE.dm (repeatable) stresses that mod instead of suite.json's (a workshop mod,
// say; its saves stay in the work folder). Checks run --jobs at a time (default: half the cores, at most 6).
// Needs: Node 18+, a built Dom5Tests (Dom5Tests/bin/Debug/net8.0), and a checkout of the
// kcopley/dom6inspector fork (branch export-test). Exit 0 = no failures.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn, spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import { generateStress } from './stress.mjs';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
const SUITE = JSON.parse(fs.readFileSync(path.join(ROOT, 'tools/fidelity/suite.json'), 'utf8'));
const BASELINES = path.join(ROOT, 'tools/fidelity/baselines');
const WORK = path.join(ROOT, 'Dom5Tests/bin/fidelity');

// --- arguments & environment -------------------------------------------------
const opt = {
	stages: null, only: null, quick: false, update: false, json: null, oracle: process.env.DOM6INSPECTOR || null,
	stress: false, seed: null, stressSize: null, stressMods: [], jobs: Math.max(1, Math.min(6, Math.floor(os.cpus().length / 2))),
};
for (let i = 2; i < process.argv.length; i++) {
	const a = process.argv[i];
	if (a === '--oracle') opt.oracle = process.argv[++i];
	else if (a === '--stages') opt.stages = process.argv[++i].split(',').map(Number);
	else if (a === '--only') opt.only = process.argv[++i];
	else if (a === '--quick') opt.quick = true;
	else if (a === '--stress') opt.stress = true;
	else if (a === '--seed') opt.seed = Number(process.argv[++i]);
	else if (a === '--stress-size') opt.stressSize = Number(process.argv[++i]);
	else if (a === '--stress-mod') { opt.stressMods.push(path.resolve(process.argv[++i])); opt.stress = true; }
	else if (a === '--jobs') opt.jobs = Math.max(1, Number(process.argv[++i]));
	else if (a === '--update-baselines') opt.update = true;
	else if (a === '--json') opt.json = process.argv[++i];
	else { console.error('unknown argument ' + a); process.exit(2); }
}
// --quick: the everyday loop. Skips the oracle self-check (stage 1 needs the inspector's full
// post-processing, ~50s per load; run it in CI or after oracle changes) and the stress run.
if (!opt.stages) opt.stages = opt.stress ? [5] : opt.quick ? [3, 4, 6] : [1, 3, 4, 5, 6];
const ORACLE = [opt.oracle, path.join(ROOT, '..', 'dom6inspector'), '/mnt/c/Projects/dom6inspector', 'C:\\Projects\\dom6inspector']
	.filter(Boolean).map((p) => path.resolve(ROOT, p)).find((p) => fs.existsSync(path.join(p, 'scripts/headless/roundtrip_check.js')));
if (!ORACLE) { console.error('dom6inspector oracle not found; pass --oracle DIR or set DOM6INSPECTOR'); process.exit(2); }
fs.mkdirSync(WORK, { recursive: true });
fs.mkdirSync(BASELINES, { recursive: true });

const IS_WSL = process.platform === 'linux' && /microsoft/i.test(os.release());
const BIN = path.join(ROOT, 'Dom5Tests/bin/Debug/net8.0');
function winPath(p) { return spawnSync('wslpath', ['-w', p], { encoding: 'utf8' }).stdout.trim(); }

function run(cmd, args) {
	return new Promise((resolve) => {
		const p = spawn(cmd, args, { stdio: ['ignore', 'pipe', 'pipe'] });
		let out = '';
		p.stdout.on('data', (d) => { out += d; });
		p.stderr.on('data', (d) => { out += d; });
		p.on('error', (e) => resolve({ code: -1, out: out + String(e) }));
		p.on('close', (code) => resolve({ code, out }));
	});
}

function dom5tests(argv) {
	const exe = path.join(BIN, 'Dom5Tests.exe');
	let cmd = 'dotnet', args = [path.join(BIN, 'Dom5Tests.dll'), ...argv];
	if (process.platform === 'win32') { cmd = exe; args = argv; }
	else if (IS_WSL && fs.existsSync(exe)) { cmd = exe; args = argv.map((a) => (a.startsWith('/') ? winPath(a) : a)); }
	return run(cmd, args);
}

function oracle(script, argv) {
	return run(process.execPath, [path.join(ORACLE, 'scripts/headless', script), ...argv]);
}

const lastLines = (out, n = 2) => out.trim().split('\n').slice(-n).join(' | ');

// roundtrip_check.js -> parsed JSON report (throws on setup error)
async function compare(a, b, at, tag) {
	const json = path.join(WORK, tag + '.' + at + '.json');
	const r = await oracle('roundtrip_check.js', [a, b, '--at', at, '--json', json, '--show', '0']);
	if (r.code !== 0 && r.code !== 1) throw new Error('oracle failed: ' + lastLines(r.out, 3));
	return JSON.parse(fs.readFileSync(json, 'utf8'));
}

// The oracle's parse of one file (roundtrip_check.js --snapshot: the data it compares, already
// normalized), cached per file: stage 4 and 5 compare many edited saves with one unedited save.
const snapCache = new Map();
function snapshot(file, at = 'parse') {
	const key = at + ' ' + file;
	if (!snapCache.has(key)) snapCache.set(key, (async () => {
		const json = path.join(WORK, 'snap-' + path.basename(file, '.dm') + '.' + at + '.json');
		const r = await run(process.execPath, ['--max-old-space-size=4096', path.join(ORACLE, 'scripts/headless/roundtrip_check.js'), '--snapshot', at, json, file]);
		if (r.code !== 0) throw new Error('oracle snapshot failed for ' + path.basename(file) + ': ' + lastLines(r.out, 3));
		return JSON.parse(fs.readFileSync(json, 'utf8'));
	})());
	return snapCache.get(key);
}

const stable = (v) => (v === null || v === undefined || typeof v !== 'object' ? JSON.stringify(v ?? null)
	: Array.isArray(v) ? '[' + v.map(stable).join(',') + ']'
		: '{' + Object.keys(v).sort().map((k) => JSON.stringify(k) + ':' + stable(v[k])).join(',') + '}');

// The same comparison as roundtrip_check.js (entities by type and id, fields by stable value,
// parser diagnostics by message count), on two snapshots.
const TYPES = ['unit', 'spell', 'wpn', 'item', 'armor', 'nation', 'site', 'event', 'nametype', 'merc'];
function diffSnapshots(A, B) {
	const rep = { dataDiffs: 0, diagnosticDiffs: 0, fieldCounts: {}, entities: [], diagnostics: [] };
	const bump = (f) => { rep.fieldCounts[f] = (rep.fieldCounts[f] || 0) + 1; };
	for (const t of TYPES) {
		const a = A.snap[t] || {}, b = B.snap[t] || {};
		const ids = [...new Set([...Object.keys(a), ...Object.keys(b)])].sort((x, y) => x - y);
		for (const id of ids) {
			let fields = null;
			if (!(id in a)) { fields = { '(entity)': [null, 'only in roundtrip'] }; bump('(entity only in roundtrip)'); }
			else if (!(id in b)) { fields = { '(entity)': ['only in original', null] }; bump('(entity only in original)'); }
			else if (stable(a[id]) !== stable(b[id])) {
				fields = {};
				for (const k of [...new Set([...Object.keys(a[id]), ...Object.keys(b[id])])].sort()) {
					if (stable(a[id][k]) !== stable(b[id][k])) { fields[k] = [a[id][k] ?? null, b[id][k] ?? null]; bump(t + '.' + k); }
				}
			}
			if (!fields) continue;
			rep.dataDiffs++;
			rep.entities.push({ type: t, id, fields });
		}
	}
	for (const m of new Set([...Object.keys(A.diag), ...Object.keys(B.diag)])) {
		const ca = A.diag[m] || 0, cb = B.diag[m] || 0;
		if (ca === cb) continue;
		rep.diagnosticDiffs++;
		rep.diagnostics.push({ message: m, original: ca, roundtrip: cb });
	}
	return rep;
}

// --- judging ------------------------------------------------------------------
// Checks run concurrently; each collects its results, and they're printed in the checks' order.
const results = [];
const MARK = { PASS: 'PASS ', FAIL: 'FAIL ', XFAIL: 'xfail', XPASS: 'XPASS', IMPROVED: 'PASS+', NEW: 'NEW  ', SKIP: 'skip ', CHANGED: 'CHNG ' };
class Check {
	constructor(stage, name, fn) { Object.assign(this, { stage, name, fn, lines: [], records: [] }); }
	note(line) { this.lines.push('      ' + line); }
	record(status, detail, name = this.name) {
		this.records.push({ stage: this.stage, name, status, detail });
		this.lines.push(`  [${MARK[status] || status}] ${name}${detail ? '  ' + detail : ''}`);
	}
	async run() {
		const t0 = Date.now();
		try { await this.fn(this); } catch (e) { this.record('FAIL', 'error: ' + e.message); }
		this.ms = Date.now() - t0;
	}
}

async function runChecks(checks) {
	let next = 0, printed = 0;
	const done = new Array(checks.length).fill(false);
	const flush = () => {
		while (printed < checks.length && done[printed]) {
			for (const l of checks[printed].lines) console.log(l);
			results.push(...checks[printed].records);
			printed++;
		}
	};
	const worker = async () => {
		while (next < checks.length) {
			const i = next++;
			await checks[i].run();
			done[i] = true;
			flush();
		}
	};
	await Promise.all(Array.from({ length: Math.min(opt.jobs, checks.length) }, worker));
}

// What the game reads (tools/dom6exe/gameread.py: the game's own passes, rules from the exe):
// the save must read the same as the original. Texts whose closing quote is missing in the
// original read on differently once a rewrite closes them, and a text set more times to the same
// final value: both counted, not failures.
async function gameRead(check, original, save, knownFailing) {
	const json = path.join(WORK, check.name + '.game.json');
	const r = await run(process.env.PYTHON || 'python3', [path.join(ROOT, 'tools/dom6exe/gameread.py'), original, save, '--show', '3', '--json', json]);
	if (r.code !== 0 && r.code !== 1) throw new Error('gameread failed: ' + lastLines(r.out));
	const rep = JSON.parse(fs.readFileSync(json, 'utf8'));
	const extra = [rep.texts_closed ? rep.texts_closed + ' open text(s) closed' : '', rep.same_final_text ? rep.same_final_text + ' text(s) set again to the same' : ''].filter(Boolean).join(', ');
	const name = check.name + ' (game\'s reading)';
	const ok = rep.differences === 0;
	const what = ok ? extra : `${rep.differences} difference(s) (${Object.entries(rep.by_pass).map(([k, n]) => k + ' ' + n).join(', ')})` + (extra ? '; ' + extra : '');
	if (!ok)
		for (const l of r.out.trim().split('\n').slice(1, 9)) check.note(l.trim());
	if (knownFailing) check.record(ok ? 'XPASS' : 'XFAIL', ok ? 'now passes; remove knownFailing' : knownFailing, name);
	else check.record(ok ? 'PASS' : 'FAIL', what, name);
}

function summarize(rep) { return rep.dataDiffs + ' data / ' + rep.diagnosticDiffs + ' diagnostic diffs'; }
function topFields(counts, n = 4) { return Object.entries(counts).sort((x, y) => y[1] - x[1]).slice(0, n).map(([f, c]) => f + ' ' + c).join(', '); }

function judgePass(check, rep, knownFailing) {
	const ok = rep.dataDiffs === 0 && rep.diagnosticDiffs === 0;
	if (knownFailing) check.record(ok ? 'XPASS' : 'XFAIL', ok ? 'now passes; remove knownFailing' : knownFailing);
	else check.record(ok ? 'PASS' : 'FAIL', ok ? '' : summarize(rep) + (rep.dataDiffs ? ' (' + topFields(rep.fieldCounts) + ')' : ''));
}

// Ratchet: fail only if a field count (or the diagnostic count) rises above the baseline.
function judgeBaseline(check, current) {
	const file = path.join(BASELINES, `${check.name}.stage${check.stage}.json`);
	const label = current.dataDiffs + ' diffs';
	if (opt.update) {
		fs.writeFileSync(file, JSON.stringify(current, null, 1) + '\n');
		return check.record('PASS', label + ' (baseline written)');
	}
	if (!fs.existsSync(file)) return check.record('NEW', label + '; no baseline yet, run with --update-baselines');
	const base = JSON.parse(fs.readFileSync(file, 'utf8'));
	const worse = Object.entries(current.fieldCounts).filter(([f, n]) => n > (base.fieldCounts[f] || 0)).map(([f, n]) => `${f} ${base.fieldCounts[f] || 0}->${n}`);
	if (current.diagnosticDiffs > base.diagnosticDiffs) worse.push(`diagnostics ${base.diagnosticDiffs}->${current.diagnosticDiffs}`);
	if (worse.length) return check.record('FAIL', `${label} (baseline ${base.dataDiffs}); regressed: ${worse.slice(0, 6).join(', ')}`);
	if (current.dataDiffs < base.dataDiffs) return check.record('IMPROVED', `${label} (baseline ${base.dataDiffs}); run --update-baselines to lock in`);
	check.record('PASS', label + ' (= baseline)');
}

function baselineOf(rep) { return { dataDiffs: rep.dataDiffs, diagnosticDiffs: rep.diagnosticDiffs, fieldCounts: rep.fieldCounts }; }

// Take documented expected differences (suite.json, "type.field": reason) out of a report; they are
// counted and printed separately. An entity counts as differing only if a non-expected field differs.
function withoutExpected(check, rep, expected) {
	const expectedCounts = {}, fieldCounts = {};
	let dataDiffs = 0;
	for (const e of rep.entities || []) {
		let differs = false;
		for (const f of Object.keys(e.fields)) {
			const k = f !== '(entity)' ? e.type + '.' + f
				: e.fields[f][0] == null ? '(entity only in roundtrip)' : '(entity only in original)'; // oracle's labels
			if (expected[k]) { expectedCounts[k] = (expectedCounts[k] || 0) + 1; continue; }
			fieldCounts[k] = (fieldCounts[k] || 0) + 1;
			differs = true;
		}
		if (differs) dataDiffs++;
	}
	const exp = Object.entries(expectedCounts).sort((x, y) => y[1] - x[1]).map(([k, n]) => k + ' ' + n).join(', ');
	if (exp) check.note('expected (suite.json stage1Expected): ' + exp);
	return { ...rep, dataDiffs, fieldCounts };
}

// An edit run's expectations against the oracle's differences. An expectation is
// { type, id | ref, field, from?, to? } (from/to omitted = any value), or for an entity made by the
// edits { type, id | ref, new: { field: value, ... } } (only in the edited save, with at least those
// values), or for a deleted one { type, id, deleted: true }. "ref" names an entity a "create" edit
// made ("as"); its number comes from the edit run (<out>.created.json).
function judgeExpectations(rep, B, expect, created) {
	const actual = [];
	for (const e of rep.entities) for (const [f, [a, b]] of Object.entries(e.fields)) actual.push({ type: e.type, id: String(e.id), field: f, from: a, to: b });
	const idOf = (x) => (x.ref !== undefined ? (created[x.ref] !== undefined ? String(created[x.ref]) : '?' + x.ref) : String(x.id));
	// "@label" in a value: the number a created entity got
	const sub = (v) => (typeof v === 'string' ? v.replace(/@(\w+)/g, (m, k) => (created[k] !== undefined ? String(created[k]) : m))
		: Array.isArray(v) ? v.map(sub) : v && typeof v === 'object' ? Object.fromEntries(Object.entries(v).map(([k, x]) => [k, sub(x)])) : v);
	const problems = [];
	// a nation's lists are sets to the oracle (sorted, no repeats): so an expected list is one too
	const asSet = (x) => (x.type !== 'nation' ? x : Object.fromEntries(Object.entries(x).map(([k, v]) => [k, (k === 'from' || k === 'to') && Array.isArray(v) ? [...new Set(v)].sort() : v])));
	const want = expect.map(sub).map(asSet).map((x) => {
		const id = idOf(x);
		if (x.new) return { type: x.type, id, field: '(entity)', from: null, to: 'only in roundtrip', fields: x.new };
		if (x.deleted) return { type: x.type, id, field: '(entity)', from: 'only in original', to: null };
		return { ...x, id };
	});
	const same = (x, y) => x.type === y.type && x.id === y.id && x.field === y.field
		&& (x.from === undefined || stable(x.from) === stable(y.from)) && (x.to === undefined || stable(x.to) === stable(y.to));
	const missing = want.filter((x) => !actual.some((y) => same(x, y)));
	const unexpected = actual.filter((y) => !want.some((x) => same(x, y)));
	for (const x of want.filter((x) => x.fields && !missing.includes(x))) {
		const got = B.snap[x.type]?.[x.id] || {};
		for (const [k, v] of Object.entries(x.fields))
			if (stable(got[k]) !== stable(v)) problems.push(`new ${x.type} #${x.id} ${k}: ${stable(got[k])}, expected ${stable(v)}`);
	}
	return { missing, unexpected, problems };
}

const fmt = (d) => `${d.type} #${d.id} ${d.field}: ${stable(d.from)} -> ${stable(d.to)}`;
function sameBytes(a, b) { return fs.existsSync(a) && fs.existsSync(b) && fs.readFileSync(a).equals(fs.readFileSync(b)); }
function firstDifference(a, b) {
	if (!fs.existsSync(a)) return path.basename(a) + ' missing';
	const x = fs.readFileSync(a, 'utf8').split('\n'), y = fs.readFileSync(b, 'utf8').split('\n');
	for (let i = 0; i < Math.max(x.length, y.length); i++)
		if (x[i] !== y[i]) return `line ${i + 1}: ${JSON.stringify((x[i] ?? '(end)').trim().slice(0, 60))} vs ${JSON.stringify((y[i] ?? '(end)').trim().slice(0, 60))}`;
	return 'same text, different bytes';
}
const sideFile = (p, tag) => p.replace(/\.dm$/, '.' + tag + '.dm');

// One edit run (stage 4 case or stage 5 stress): Dom5Tests edit with undo/redo, then the oracle.
async function editRun(check, { base, save, specFile, out, expect, knownFailing, maxShown = 3 }) {
	const r = await dom5tests(['edit', base, specFile, out, 'undo']);
	if (r.code !== 0 && r.code !== 3) throw new Error('Dom5Tests edit failed: ' + lastLines(r.out));
	const problems = [];
	if (r.code === 3) problems.push('reread: ' + r.out.split('\n').filter((l) => l.includes('reread FAIL')).map((l) => l.trim().replace(/^reread FAIL\s+/, '')).slice(0, maxShown).join('; '));
	if (!sameBytes(sideFile(out, 'undo'), save)) problems.push('undo all: not the unedited save (' + firstDifference(sideFile(out, 'undo'), save) + ')');
	if (!sameBytes(sideFile(out, 'redo'), out)) problems.push('redo all: not the edited save (' + firstDifference(sideFile(out, 'redo'), out) + ')');
	const [A, B] = await Promise.all([snapshot(save), snapshot(out)]);
	const rep = diffSnapshots(A, B);
	const createdFile = out.replace(/\.dm$/, '.created.json');
	const created = fs.existsSync(createdFile) ? JSON.parse(fs.readFileSync(createdFile, 'utf8')) : {};
	const j = judgeExpectations(rep, B, expect, created);
	if (j.missing.length) problems.push('missing: ' + j.missing.slice(0, maxShown).map(fmt).join('; ') + (j.missing.length > maxShown ? ` (+${j.missing.length - maxShown})` : ''));
	if (j.unexpected.length) problems.push('unexpected: ' + j.unexpected.slice(0, maxShown).map(fmt).join('; ') + (j.unexpected.length > maxShown ? ` (+${j.unexpected.length - maxShown})` : ''));
	problems.push(...j.problems.slice(0, maxShown));
	if (rep.diagnosticDiffs) problems.push(rep.diagnosticDiffs + ' diagnostic diffs: ' + rep.diagnostics.slice(0, 2).map((d) => `${d.message} ${d.original}->${d.roundtrip}`).join('; '));
	const ok = problems.length === 0;
	const detail = problems.join(' | ');
	fs.writeFileSync(out.replace(/\.dm$/, '.report.json'), JSON.stringify({ problems, missing: j.missing, unexpected: j.unexpected, rep }, null, 1));
	if (knownFailing) check.record(ok ? 'XPASS' : 'XFAIL', ok ? 'now passes; remove knownFailing' : knownFailing + (detail ? ' [' + detail + ']' : ''));
	else check.record(ok ? 'PASS' : 'FAIL', detail);
	return { ok, rep, j };
}

// --- corpus selection ---------------------------------------------------------
const mods = SUITE.mods
	.filter((m) => !opt.only || m.name.includes(opt.only))
	.map((m) => ({ ...m, abs: path.join(ROOT, m.path) }));
const saveCache = new Map(); // base .dm -> Dom5Parser save of it

function dom5Save(abs, tag, extra = []) {
	const key = abs + ' ' + extra.join(' ');
	if (!saveCache.has(key)) saveCache.set(key, (async () => {
		const out = path.join(WORK, tag + '.save.dm');
		const r = await dom5tests(['roundtrip', abs, out, ...extra]);
		if (r.code !== 0 || !fs.existsSync(out)) throw new Error('Dom5Tests roundtrip failed: ' + lastLines(r.out));
		return out;
	})());
	return saveCache.get(key);
}

// --- stages -------------------------------------------------------------------
console.log(`oracle: ${ORACLE}\nwork:   ${WORK}\njobs:   ${opt.jobs}\n`);
const started = Date.now();

if (opt.stages.includes(1)) {
	console.log('Stage 1: inspector loads mod -> exports -> reloads (functional, after post-processing)');
	await runChecks(mods.filter((m) => !m.stages || m.stages.includes(1)).map((m) => new Check(1, m.name, async (check) => {
		const out = path.join(WORK, m.name + '.inspector.dm');
		const r = await oracle('export-mod.js', [m.abs, out]);
		if (r.code !== 0) throw new Error('export-mod failed: ' + lastLines(r.out));
		const rep = withoutExpected(check, await compare(m.abs, out, 'final', m.name + '.stage1'), SUITE.stage1Expected || {});
		if (m.expect === 'baseline') judgeBaseline(check, baselineOf(rep));
		else judgePass(check, rep, m.knownFailing?.['1']);
	})));
}

// Stage 2 (the inspector's vanilla.dm export) was retired on 2026-10-05: vanilla data now comes
// from Dominions6.exe (tools/dom6exe), and the inspector is only an independent parser here.

if (opt.stages.includes(3)) {
	console.log('\nStage 3: Dom5Parser load -> save -> inspector sees the same data (strict, after parsing), and the game reads it the same');
	await runChecks(mods.filter((m) => !m.stages || m.stages.includes(3)).map((m) => new Check(3, m.name, async (check) => {
		const save = await dom5Save(m.abs, m.name, m.roundtripArgs || []);
		const rep = await compare(m.abs, save, 'parse', m.name + '.stage3');
		if (m.expect === 'baseline') judgeBaseline(check, baselineOf(rep));
		else judgePass(check, rep, m.knownFailing?.['3']);
		await gameRead(check, m.abs, save, m.knownFailing?.['3game']);
	})));
}

if (opt.stages.includes(4)) {
	console.log('\nStage 4: Dom5Parser load -> edit -> save -> differs from an unedited save by exactly the edits; undo all / redo all byte for byte');
	const dir = path.join(ROOT, SUITE.edits);
	const checks = [];
	for (const file of fs.readdirSync(dir).filter((f) => f.endsWith('.json')).sort()) {
		const name = path.basename(file, '.json');
		if (opt.only && !name.includes(opt.only)) continue;
		const spec = JSON.parse(fs.readFileSync(path.join(dir, file), 'utf8'));
		const base = path.resolve(dir, spec.base);
		checks.push(new Check(4, name, async (check) => {
			const save = await dom5Save(base, 'edit-base-' + path.basename(base, '.dm'));
			await editRun(check, { base, save, specFile: path.join(dir, file), out: path.join(WORK, name + '.edit.dm'), expect: spec.expect, knownFailing: spec.knownFailing });
		}));
	}
	await runChecks(checks);
}

if (opt.stages.includes(5)) {
	const cfg = SUITE.stress || { mods: [] };
	const seed = opt.seed ?? cfg.seed ?? 1;
	const size = opt.stressSize ?? cfg.size ?? 300;
	console.log(`\nStage 5: stress, ${size} generated edits per mod (seed ${seed}) -> exactly their changes; undo all / redo all byte for byte`);
	// the commands the game reads per entity type (Dom5Edit's catalog, from Dominions6.exe)
	const catalog = JSON.parse(fs.readFileSync(path.join(ROOT, 'Dom5Edit/GameData', fs.readdirSync(path.join(ROOT, 'Dom5Edit/GameData')).find((f) => /^game-commands-.*\.json$/.test(f))), 'utf8'));
	const gameCommands = Object.fromEntries(Object.entries(catalog.contexts).map(([k, v]) => [k, new Set(v.commands)]));
	// and the commands the oracle reads (its parser's tables)
	const { modctx } = createRequire(import.meta.url)(path.join(ORACLE, 'scripts/headless/boot.js')).boot({ beforePostMod: () => {} });
	const oracleCommands = Object.fromEntries(TYPES.map((t) => [t, new Set(Object.keys(modctx[t + 'commands'] || {}))]));
	const empty = path.join(WORK, 'empty.dm');
	fs.writeFileSync(empty, '#modname "empty"\n#description "vanilla only (fidelity stress: which entities are the game\'s)"\n');
	const stressMods = opt.stressMods.length
		? opt.stressMods.map((p) => ({ name: path.basename(p, '.dm').replace(/[^\w.-]+/g, '_'), path: p }))
		: cfg.mods;
	await runChecks(stressMods.filter((m) => fs.existsSync(path.resolve(ROOT, m.path)) && (!opt.only || m.name.includes(opt.only))).map((m) => new Check(5, `${m.name} (seed ${seed})`, async (check) => {
		const abs = path.resolve(ROOT, m.path);
		const save = await dom5Save(abs, m.name);
		const [base, vanilla] = await Promise.all([snapshot(save), snapshot(empty)]);
		const gen = generateStress({ text: fs.readFileSync(abs, 'utf8'), vanillaText: fs.readFileSync(path.join(ROOT, 'vanilla.dm'), 'utf8'), gameCommands, oracleCommands, base: base.snap, vanilla: vanilla.snap, seed, size });
		const specFile = path.join(WORK, `stress-${m.name}-${seed}.json`);
		fs.writeFileSync(specFile, JSON.stringify({ description: `generated by tools/fidelity/stress.mjs from ${m.path}, seed ${seed}`, edits: gen.edits, expect: gen.expect, reread: gen.reread }, null, 1));
		check.note(`${gen.edits.length} edits (${Object.entries(gen.counts).map(([k, n]) => k + ' ' + n).join(', ')}), ${gen.expect.length} expected changes, ${gen.reread.length} re-read checks; ${path.relative(ROOT, specFile)}`);
		if (gen.skipped.length) check.note('fewer than planned (the mod has too few): ' + gen.skipped.join('; '));
		const out = path.join(WORK, `stress-${m.name}-${seed}.edit.dm`);
		await editRun(check, { base: abs, save, specFile, out, expect: gen.expect, maxShown: 6 });
	})));
}

// Stage 6: merges (docs/MERGING.md). A case's parts merge with Dom5Tests merge (it reads the merged
// file back: nothing defined twice that wasn't before, every entity one part alone has the values
// it had in its part, every event the same), then gameread.py --merge replays the game's reading of
// the parts one after another against the merged file (numbers mapped through the moves). Those
// must hold. The counts (numbers moved by kind, copies kept apart, dangling references kept, things
// across parts, the referee's known kinds) are a baseline: a change is shown (CHNG), not a failure;
// --update-baselines keeps the new ones. Cases marked slow run only in the full suite; parts that
// aren't installed (workshop mods) skip the case.
if (opt.stages.includes(6)) {
	console.log('\nStage 6: merges -> nothing defined twice, every entity as in its part, the game reads the merged file as its parts one after another; counts as baselines');
	const cases = (SUITE.merges || []).filter((c) => (!opt.quick || !c.slow) && (!opt.only || c.name.includes(opt.only)));
	const resolve = (p) => (path.isAbsolute(p) ? p : path.resolve(ROOT, p));
	const forTests = (p) => (IS_WSL && p.startsWith('/') ? winPath(p) : p);
	await runChecks(cases.map((c) => new Check(6, c.name, async (check) => {
		const files = c.parts.map(resolve);
		const missing = files.filter((f) => !fs.existsSync(f));
		if (missing.length) return check.record('SKIP', 'not installed here: ' + missing.map((f) => path.basename(f)).join(', '));
		const dir = path.join(WORK, 'merge-' + c.name);
		fs.mkdirSync(dir, { recursive: true });
		const out = path.join(dir, c.name + '.dm');
		const json = path.join(dir, c.name + '.check.json');
		const args = ['merge', out, c.title || c.name, ...files];
		for (const [sub, parent] of Object.entries(c.needs || {}))
			args.push('--needs', forTests(resolve(sub)) + '=' + forTests(resolve(parent)));
		args.push('--json', json);
		const r = await dom5tests(args);
		if (r.code !== 0 || !fs.existsSync(json)) throw new Error('Dom5Tests merge failed: ' + lastLines(r.out, 3));
		const m = JSON.parse(fs.readFileSync(json, 'utf8'));
		const gjson = path.join(dir, c.name + '.game.json');
		const g = await run(process.env.PYTHON || 'python3', [path.join(ROOT, 'tools/dom6exe/gameread.py'), '--merge', out, '--show', '3', '--json', gjson]);
		if (g.code !== 0 && g.code !== 1) throw new Error('gameread --merge failed: ' + lastLines(g.out, 3));
		const gr = JSON.parse(fs.readFileSync(gjson, 'utf8'));
		const problems = [];
		if (m.definedTwice > m.definedTwiceBefore) problems.push(`${m.definedTwice - m.definedTwiceBefore} number(s) defined twice`);
		if (m.differ) problems.push(`${m.differ} entit${m.differ === 1 ? 'y differs' : 'ies differ'} from their part`);
		if (m.eventsDiffer || m.events !== m.eventsExpected) problems.push(`events ${m.events} of ${m.eventsExpected}, ${m.eventsDiffer} differ`);
		if (gr.differences) problems.push(`the game reads it differently from its parts: ${gr.differences}`);
		if (gr.chain) problems.push(`${gr.chain} chained unit(s) out of order`);
		if (problems.length)
			for (const l of g.out.trim().split('\n').slice(1, 7)) check.note(l.trim());
		const kinds = Object.entries(m.movedByKind).map(([k, n]) => `${n} ${k}`).join(', ');
		check.record(problems.length ? 'FAIL' : 'PASS', problems.join('; ') ||
			`${m.moved} moved${kinds ? ' (' + kinds + ')' : ''}, ${m.copiesKeptApart} copies kept apart, ${m.dangling} dangling kept, ${m.acrossParts} across parts; ${m.compared} entities as in their parts`);
		// the counts, against the baseline
		const counts = { moved: m.moved, movedByKind: m.movedByKind, renamed: m.renamed, copiesKeptApart: m.copiesKeptApart, dangling: m.dangling,
			acrossParts: m.acrossParts, notCompared: { shared: m.shared, across: m.across }, referee: gr.known };
		const file = path.join(BASELINES, `${c.name}.stage6.json`);
		if (opt.update) fs.writeFileSync(file, JSON.stringify(counts, null, 1) + '\n');
		else if (!fs.existsSync(file)) check.record('NEW', 'no baseline yet; run with --update-baselines', c.name + ' (counts)');
		else {
			const base = JSON.parse(fs.readFileSync(file, 'utf8'));
			const changed = Object.keys(counts).filter((k) => JSON.stringify(counts[k]) !== JSON.stringify(base[k]));
			if (changed.length) check.record('CHANGED', changed.map((k) => `${k} ${JSON.stringify(base[k])} -> ${JSON.stringify(counts[k])}`).join('; '), c.name + ' (counts)');
		}
	})));
}

// --- summary ------------------------------------------------------------------
const count = (s) => results.filter((r) => r.status === s).length;
const failed = results.filter((r) => r.status === 'FAIL' || r.status === 'NEW');
console.log(`\n${results.length} checks: ${count('PASS') + count('IMPROVED')} pass, ${count('XFAIL')} known-failing, ${failed.length} failing` + (count('XPASS') ? `, ${count('XPASS')} unexpectedly passing` : '')
	+ (count('SKIP') ? `, ${count('SKIP')} skipped` : '') + (count('CHANGED') ? `, ${count('CHANGED')} with changed counts` : '') + ` (${Math.round((Date.now() - started) / 1000)} s)`);
if (opt.json) fs.writeFileSync(opt.json, JSON.stringify({ oracle: ORACLE, results }, null, 1));
process.exit(failed.length ? 1 : 0);
