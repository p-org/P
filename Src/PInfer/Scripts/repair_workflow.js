export const meta = {
  name: 'repair-candidates',
  description: 'LLM-repair P spec monitors that failed to compile, given the exact compiler error',
  phases: [{ title: 'Repair' }],
}

const SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['fixedSource', 'changes'],
  properties: {
    fixedSource: { type: 'string', description: 'the full corrected `spec ... { ... }` P source, same property, compilable' },
    changes: { type: 'string', description: 'one-line summary of what was fixed' },
  },
}

const RULES = `P SPEC MONITOR SYNTAX RULES (the candidate violated one or more):
- NO inline var init. Write \`var x: T;\` at the TOP of a handler body, then assign \`x = e;\` as a statement. A spec-level field is \`var x: T;\` only — it CANNOT be initialized at declaration; if you need a constant, inline the literal at use sites, or assign it in the start state's \`entry { }\`.
- ALL \`var\` declarations must come BEFORE any statement in their block (function/entry/handler). Hoist them to the top.
- NO ternary \`cond ? a : b\`. Use if/else with a temp var.
- NO \`not\` / \`not in\`. Use \`!\` and \`!(x in s)\`.
- format strings use POSITIONAL args: \`format("... {0} ... {1}", a, b)\`. NEVER \`"... {a} ..."\` interpolation. assert form: \`assert <bool>, format(...);\` or \`assert <bool>, "literal";\`.
- State names must be UNIQUE within a spec (do not reuse a name across hot/cold/normal states).
- NO \`is\` keyword (PVerifier-only). Compare fields/enum values instead.
- \`keys(m)\` / \`values(m)\` return seq; iterate with \`foreach (x in keys(m))\`. Use \`foreach\`, never \`for\`.
- Membership: \`x in set\`/\`k in map\`. Build sets with \`s += (elem)\`; remove with \`s -= (elem)\` / \`m -= (key)\`. There is NO \`delete\`.
- A set element type must match exactly; cast a Node to machine via \`(x as machine)\` when testing membership in set[machine].
- EVERY observed event must be handled in EVERY state of the monitor.
- A liveness monitor uses a \`hot state\`; do not mark a state hot unless the system must always leave it.`

const FINDINGS = Array.isArray(args) ? args : JSON.parse(args)

phase('Repair')
const repaired = await parallel(FINDINGS.map(f => () =>
  agent(
`You are the REPAIR stage of an agentic invariant-mining pipeline for the P language (cwd = repo root). A candidate spec monitor FAILED TO COMPILE. Fix ONLY its syntax/encoding so it compiles — do NOT change the property it expresses.

Benchmark: ${f.benchmark} (source dir: ${f.dir}). Read ${f.dir}/PSrc/*.p AND ${f.dir}/PTst/*.p to get EXACT event names and payload field names. NEVER invent an event name (e.g. do not assume an "eSpec_..._Init" event exists — only use events that actually appear in the source). Build a set with successive \`s += (elem)\` (keys()/values() return seq, NOT set). A reference to an undeclared event/identifier is a hard error.

COMPILER ERROR:
${f.error}

BROKEN SPEC SOURCE:
${f.source || `(no source inline — read the spec named "${f.name}" from the candidate file at ${f.sourceFile || `<your candidates file for ${f.benchmark}>`}, grep for "spec ${f.name}". On a repeat round, start from your PRIOR fix for this spec and address the NEW error above.)`}

${RULES}

Return the corrected full \`spec ${f.name} ... { ... }\` source (keep the same spec name and the same intent/property) plus a one-line summary of what you changed. If a property fundamentally cannot be expressed as a P monitor (e.g. it needs the ternary only for a value the property doesn't really need), simplify minimally while preserving the property's meaning.`,
    { label: `repair:${f.name}`, phase: 'Repair', schema: SCHEMA, agentType: 'Explore' }
  ).then(r => ({ benchmark: f.benchmark, name: f.name, changes: r && r.changes, fixedSource: r && r.fixedSource }))
)).then(a => a.filter(Boolean))

return repaired
