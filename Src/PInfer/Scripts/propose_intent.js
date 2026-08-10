// Intent-lens invariant proposer for P — derives candidates from DESIGN INTENT (docs +
// source comments), not from the implementation's structure. This is the complement of
// propose_templated.js: that one systematically instantiates the PInfer grammar shapes
// (fill-in-the-blank enumeration); this one asks "what SHOULD this protocol guarantee?"
// through four soft lenses. The two proposers share the CAND_SCHEMA output contract and
// are deduped downstream by invariant_core.dedup_candidates (PLAN.md Phase 2b).
//
// Lens table mirrors invariant_core.INTENT_LENSES (Python is the source of truth);
// test_invariant_core.py::TestProposerParity guards drift.
//
// args = { benchmark, dir, events, designDoc? } — designDoc is optional prose (a
// README/design excerpt); when absent, agents read design intent from source comments.
// Returns { <benchmark>: { dir, candidates: [...] } } — the shape prep_candidates.py reads.
export const meta = {
  name: 'propose-intent',
  description: 'Propose P invariants from design intent through agreement/liveness/consistency/validity lenses',
  phases: [{ title: 'Propose' }],
}

const A = (typeof args === 'object' && args) ? args : JSON.parse(args)

// Same contract as propose_templated.js (mirrored by invariant_core.Candidate).
const CAND_SCHEMA = {
  type: 'object', additionalProperties: false, required: ['candidates'],
  properties: {
    candidates: {
      type: 'array',
      items: {
        type: 'object', additionalProperties: false,
        required: ['name', 'intent', 'category', 'predictedBucket', 'observes', 'specCode'],
        properties: {
          name: { type: 'string' }, intent: { type: 'string' }, category: { type: 'string' },
          provenance: { type: 'string', enum: ['templated', 'intent', 'enumerative'] },
          predictedBucket: { type: 'string', enum: ['verified', 'bug', 'spurious', 'vacuous'] },
          observes: { type: 'array', items: { type: 'string' } },
          formula: {
            type: 'object', additionalProperties: false,
            properties: {
              quantifiers: { type: 'array', items: { type: 'object' } },
              guards: { type: 'array', items: { type: 'string' } },
              relations: { type: 'array', items: { type: 'string' } },
              sc: { type: ['object', 'null'] },
              config_event: { type: ['string', 'null'] },
              uses_index: { type: 'boolean' },
            },
          },
          canary: { type: ['string', 'null'] },
          specCode: { type: 'string' },
        },
      },
    },
  },
}

const RULES = `P SPEC MONITOR SYNTAX RULES:
- NO inline var init (\`var x:T = e;\`): declare \`var x:T;\` at the TOP of a block, assign \`x = e;\` after.
- ALL var decls come before any statement in their block. No ternary \`?:\`. No \`not\`/\`not in\` (use \`!\`, \`!(x in s)\`).
- format strings use positional args: \`format("...{0}", x)\`. assert: \`assert <bool>, format(...);\`.
- State names unique within a spec. No \`is\` keyword. \`foreach\` not \`for\`. keys()/values() return seq, not set.
- Build sets with \`s += (elem)\`; remove with \`-=\`. Cast Node to machine via \`(x as machine)\` for set[machine] membership.
- EVERY observed event handled in EVERY state. Use a \`hot state\` only for genuine liveness.`

// Mirrors invariant_core.INTENT_LENSES — keep keys and focus text in sync (parity-tested).
const LENSES = [
  { key: 'agreement', focus: 'SAFETY / AGREEMENT / ATOMICITY: a decision is justified by prior events; no contradictory outcomes; all-or-nothing.' },
  { key: 'liveness', focus: 'LIVENESS / PROGRESS: every request/round is eventually answered (hot-state monitors).' },
  { key: 'consistency', focus: 'CONSISTENCY / ORDERING / CAUSALITY: read-your-writes, monotonicity, response-preceded-by-request.' },
  { key: 'validity', focus: 'VALIDITY / WELL-FORMEDNESS / UNIQUENESS: payload/status domains, unique ids, non-empty sets; include one deliberately-vacuous candidate + a `<Name>_canary` companion that asserts false in the same branch.' },
]

const DOC = A.designDoc ? `\nDESIGN INTENT (verbatim, from the design doc):\n${A.designDoc}\n` : ''

phase('Propose')
const results = (await parallel(LENSES.map(l => () =>
  agent(
`You are the INTENT-LENS PROPOSER of an agentic invariant-mining pipeline for the P language (cwd = repo root).

TARGET: ${A.benchmark} at ${A.dir}. Read its PSrc/*.p and PTst/*.p — for EXACT event/payload names AND for design-intent comments (the prose above machines/states/handlers that says what the protocol is SUPPOSED to guarantee).${DOC}
Event signatures:
${A.events}

Derive candidates from INTENT, not from the implementation: mirroring the code turns a bug into a "correct" invariant. The valuable candidates live in the gap between what the design promises and what the code does. Propose what a careful protocol designer would DEMAND, even if you suspect the implementation might violate it — a FAILS verdict on a required property is a found bug, the highest-value outcome.

YOUR LENS: ${l.focus}

Produce 3-6 candidates through this lens, each encoded as a compilable P spec monitor:
- UNIQUE names prefixed '${A.benchmark}_${l.key}_'.
- provenance: "intent".
- Fill the structured formula record: quantifiers ([{var,type,kind:"forall"|"exists"}]), guards (antecedent conjuncts as predicate strings, e.g. "e0.transId == e1.transId"), relations (consequent conjuncts), sc (quorum/cardinality {op,bound} or null), config_event (population-announcing event or null), uses_index (true if the property needs happens-before/ordering).
- observes: exactly the events the monitor observes.
- predictedBucket: your honest prediction (verified|bug|spurious|vacuous).

${RULES}`,
    { label: `propose:${l.key}`, phase: 'Propose', schema: CAND_SCHEMA }
  ).then(r => (r && r.candidates ? r.candidates.map(c => ({ lens: l.key, provenance: 'intent', ...c })) : []))
))).flat()

log(`${results.length} intent-lens candidates for ${A.benchmark}`)
return { [A.benchmark]: { dir: A.dir, candidates: results } }
