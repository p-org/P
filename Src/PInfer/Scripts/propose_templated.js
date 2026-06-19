// Template-scaffolded invariant proposer for P — uses PInfer's formula grammar
// (forall* G -> exists* F /\ R, bounded by term-depth/arity/#guards/#filters) as a
// fill-in-the-blank scaffold, instead of soft natural-language "intent lenses".
//
// args = { benchmark, dir, events } where `events` documents the event signatures.
// Returns { <benchmark>: { dir, candidates: [...] } } — the shape prep_candidates.py reads.
export const meta = {
  name: 'propose-templated',
  description: 'Propose invariants by systematically instantiating PInfer templates over a benchmark',
  phases: [{ title: 'Templates' }],
}

const A = (typeof args === 'object' && args) ? args : JSON.parse(args)

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
          predictedBucket: { type: 'string', enum: ['verified', 'bug', 'spurious', 'vacuous'] },
          observes: { type: 'array', items: { type: 'string' } },
          specCode: { type: 'string' },
        },
      },
    },
  },
}

const GRAMMAR = `PInfer TEMPLATE GRAMMAR — the search space you are instantiating:

    forall e1:E1, ..., eA:EA ::  Guards(e1..eA)  =>  exists* f1:F1, ..., fX:FX :: Filters /\\ Relations

where:
- TERMS are payload-field accesses on the quantified events, up to TERM-DEPTH 2
  (e.g. e1.trial, e2.node, e1.transId; one level of tuple/field nesting).
- PREDICATES are comparisons between same-typed terms: ==, !=, <, <=, >, >=, and set/map membership (in).
- GUARDS = a conjunction of predicates that constrain which event tuples the property ranges over
  (e.g. e1.transId == e2.transId, e1.node == e2.node).
- RELATIONS/FILTERS = the predicates asserted to hold on the guarded tuples.
- ARITY A = number of forall-quantified events; EXISTS X = number of existentials (0 = pure universal).

Systematically enumerate this shape for your assigned configuration over the benchmark's events.
For each instantiation that expresses a meaningful property, emit a P spec monitor that ENCODES it
(monitors observe events and accumulate the terms needed to evaluate the predicate across a trace).
A "forall ... exists" template becomes a monitor that records the forall-side facts and asserts the
exists-side was eventually satisfied. Skip trivially-true or type-incompatible instantiations.`

const SHAPES = [
  { key: 'arity1', focus: `ARITY=1, EXISTS=0. forall e:E :: Relations(e.fields). Single-event well-formedness/domain invariants: every payload field stays in its valid range/domain, status enums are restricted, identifiers are bounded. Enumerate over EACH event type.` },
  { key: 'arity2-same', focus: `ARITY=2, EXISTS=0, both events the SAME type, guarded by an equality on a key field. forall e1:E,e2:E :: e1.key==e2.key => Relation(e1.term, e2.term). Captures monotonicity, uniqueness, and "no two conflicting" properties (e.g. same node/txn never gets two contradictory values; a counter only grows).` },
  { key: 'arity2-cross', focus: `ARITY=2, EXISTS=0, DIFFERENT event types, guarded by a cross-event key equality. forall e1:E1,e2:E2 :: e1.id==e2.id => Relation. Captures agreement/causality/consistency between a request and its response, a vote and an outcome, a write and a read.` },
  { key: 'exists', focus: `ARITY>=1, EXISTS>=1. forall e:E :: Guards => exists f:F :: f.id==e.id /\\ Relation. Captures completeness/response/justification: every X is eventually matched by some Y (e.g. every shutdown is eventually reported; every reported-down node had a triggering event). Encode as a monitor tracking outstanding forall-facts and asserting the existential match arrives.` },
]

phase('Templates')
const results = (await parallel(SHAPES.map(s => () =>
  agent(
`You are a TEMPLATE-SCAFFOLDED invariant proposer for the P language (cwd = repo root).

TARGET: ${A.benchmark} at ${A.dir}. Read its PSrc/*.p and PTst/*.p for EXACT event/payload names and design intent. Event signatures:
${A.events}

${GRAMMAR}

YOUR ASSIGNED TEMPLATE CONFIGURATION:
${s.focus}

Enumerate this configuration systematically over the events above (try the relevant event/field combinations, not just the obvious one). Derive properties from the protocol's INTENT (what it should guarantee), not from mirroring the code. Tag each predictedBucket (verified/bug/spurious/vacuous). Produce 3-6 candidates with UNIQUE names prefixed "${A.benchmark}_tmpl_${s.key}_". Each specCode must be a self-contained, compilable P spec monitor (single-state preferred; every observed event handled in every state; var decls at top; format("...{0}", x) for messages; build sets with += ; no ternary/not/inline-var-init). Return structured candidates.`,
    { label: `tmpl:${s.key}`, phase: 'Templates', schema: CAND_SCHEMA, agentType: 'Explore' }
  ).then(r => ({ candidates: (r && r.candidates) || [], shape: s.key }))
))).filter(Boolean)

const candidates = []
for (const r of results) for (const c of r.candidates) candidates.push({ ...c, lens: 'tmpl_' + r.shape })
log(`${A.benchmark}: ${candidates.length} template-instantiated candidates`)
const out = {}
out[A.benchmark] = { dir: A.dir, candidates }
return out
