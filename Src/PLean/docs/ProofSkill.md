# Writing inductive invariants for `#pverify`

`#pverify` proves each target by induction over steps of the transition
system: every invariant holds initially (`base_<T>`), and every handler
preserves it (`<M>.<S>.<ev>_correct_<T>`, `entry_correct_<T>`). Assuming
every invariant, it also proves that no handler fails (`_safe`) and that
no state receives an event it does not handle (`_never`).

When an obligation does not close, the fix is almost always a stronger
invariant. The usual loop:

1. Read the failing obligation's counterexample (below).
2. Find the fact that rules the pre-state out, add it as an invariant, and
   `prove` it.
3. Re-run only the obligations concerned.
4. Once the invariants are in place and the solver still gives up, prove
   the obligation by hand (last section).

## Reading the report

`#pverify M` prints one line per obligation and writes the full report to
`.lake/pverify/M.txt`. For each failed obligation, the report file has the
goals the solver failed on and the obligation's statement as a
`@[pverifyProof]` skeleton. While `#pverify` runs, the editor shows one
progress message, updated in place: the obligations done, the one being
proved, and the goal and solver it is on.

| Outcome | Meaning | What to do |
|---|---|---|
| `✓` | proved, with a checked proof | — |
| `✓ (trusted)` | the solver proved it, but its proof could not be replayed | usually fine; for a checked proof, simplify the invariant or prove the step's key lemma by hand (last section) |
| `✓ (manual)` | proved by a `@[pverifyProof]` theorem | — |
| `✗ [counterexample]` | z3 found a state where the step breaks the invariant | the invariant is false or not inductive: strengthen it |
| `✗ [unknown]` | the solver timed out, and no counterexample was found | the query is too big or needs too much reasoning: give the target fewer premises, add intermediate invariants, or prove it by hand |
| `✗ [error]` | something else failed, e.g. a manual proof whose statement is not the obligation's | read the message |

A proved obligation's line says how its goals (one per invariant and
path through the handler, after normalisation) were closed:

| Mark | Meaning |
|---|---|
| `[norm]` | no goal was left after `vcgen` and normalisation |
| `smt n` | `n` goals needed an SMT solver |
| `pre n` | `n` goals were closed by crush before SMT translation: a Lean proof built from the hypotheses (a contradictory hypothesis, a hypothesis applied directly, an existential witness), checked by the kernel, with no solver call |
| `hyp n` | `n` goals were hypotheses |

The summary line counts the obligations proved by normalisation or a
hypothesis, by crush before SMT only, with an SMT solver, and by hand, and
splits the time between `vcgen` and normalisation, crush, and the rest
(elaborating the statement and checking the proof). The last lines say:

- what `M.safe` assumes beyond Lean's axioms: `paxiom`s, `pinstance`s, the
  solver's word for trusted obligations, and `sorry` (left by a failed
  obligation, or written in the program);
- which `@[pverifyProof]` theorems are not obligations;
- whether no world satisfies `InitConditions`. Contradictory `init-holds`
  make every theorem vacuous, so this is an error.

A counterexample names the invariant and the step, and shows a smallest
pre-state z3 finds:

```
✗ [counterexample] Coordinator.WaitForResponses.eYes_correct_commit_sent
    invariant `commit_sent_implies_all_yes` can fail after Coordinator #1, in state WaitForResponses, handles eYes (packet #0 from Coordinator #1).
    Pre-state (it satisfies the premises; it need not be reachable):
    Machines:
      Coordinator
        #1 (coordinator)  WaitForResponses  yesVotes = {2}  ◀
      Participant
        #2  Rejected
      (references from #3 on are unallocated)
    In flight:
      #0  Coordinator #1 → Coordinator #1  eYes {source := 2}  ◀
    Functions:
      preference: #1 ↦ false, #2 ↦ false
      coordinator = 1
      participants = {2}
```

`◀` marks the machine taking the step and the packet it takes. Reference
`0` is `null`, never a machine. The pre-state satisfies the premises but
need not be reachable (here, the Coordinator holds a yes vote from a
participant that prefers no). The usual cause is a fact that holds in
every reachable state but is not among the premises: add it as an
invariant (and `prove` it), here `votes_all_prefer`.

A `_safe` counterexample shows a handler that can fail: an `assert`, a
send to `null`, a missing or duplicate map key, an index out of range, or a
`choose` from nothing.

Counterexamples are found with the well-formedness of recursive
datatypes dropped, so a value where the program has a `Nat` (inside a
set, say) can be negative. The SMT script and model are kept in
`.lake/pverify/<M>/<obligation>.smt2`.

## Patterns

**Routing.** Never-receives obligations (`<M>.<S>.<ev>_never`) need to
know where each event can go, e.g.

```lean
invariant yes_to_coordinator : ∀ e : eYes, inflight e s → e.target = coordinator
```

together with a kind fact (`is_Coordinator coordinator s`).

**Send targets.** A send to `null` (reference `0`, the default of a
machine reference) fails, so a handler's `_safe` obligation needs each
target to be a machine. A kind fact gives it (`null` is never
allocated): `is_Coordinator coordinator s` for a field, and for a reply
to a payload field, an invariant on the request,
`∀ e : eReq, inflight e s → is_Client e.client s`. For an opaque
function used as a target, say what it returns for the machines that use
it: RingLeader's successor function maps ring members to ring members,
and the Servers are exactly the members (`init-holds`, and an invariant).

**Assumptions about all references.** `MachineRef` is all of ℕ, most of
it no machine. A `paxiom` or `pinstance` law that quantifies over every
reference (where P's `forall (x: machine)` ranges over the program's
machines) can hold only for infinitely many references, and then every
initial world may have to be empty. A successor that is immediate in a
cyclic order of all of ℕ, for instance, closes no finite set of
references. State such laws for the machines (or a set of them, like
RingLeader's `mem`) and relate the set to the machines with `init-holds`
and an invariant. The vacuity check cannot catch this, since the empty
world satisfies `InitConditions`. A `paxiom` is also a hypothesis of every
goal, which can slow the solver on unrelated ones; a `pinstance` law is
only a hint.

**Bridging invariants for deep arguments.** If a step needs a long
chain (an axiom, then an existential witness, then a uniqueness fact),
state the conclusion of the chain as its own invariant. Consensus does
this with `vote_owner` and `votes_disjoint`, so that election safety
needs one axiom instance and ground facts.

**Loops.** A loop that sends needs `new_sends e => P` in its invariant
whenever an invariant depends on what the loop sends (`e is eVoteReq`,
`e.target ∈ participants`, …). Everything the body cannot change is
added automatically.

**Message fields versus payload fields.** In an invariant, `e.f` on an
event binder is the payload field `f` when the event has one. To reach
the message's own `target` / `src` when the payload also has such a
field, quantify over `PLean.Msg E` and guard with `is_<ev> e`.

**Containers.** In invariants, `=` between sets or maps compares their
contents (P's `==`), not how they were built.

**`using`.** `prove T using U ;` gives `T`'s obligations `U` as an extra
premise (`U` must be proved too). Smaller premise sets make queries
faster; add what a counterexample shows is missing.

**Small lemmas.** Every invariant of every premise is a hypothesis of
every goal of the target. With a few dozen quantified facts in the
context, z3 can time out on a step that changes nothing, which it proves
at once from the invariant alone (seen with invariants of the shape
`∀ … → ∃ …`). So group invariants by what uses them, into many small
lemmas, and give each target the few it needs. When one step of a target
needs many premises and the others none, prove that step by hand (last
section).

**Existential conclusions.** Witness a message, not a machine: write
`∃ e : eReq, sent e s ∧ …`, not `∃ n : Node, …`.
- For a goal `∃ r : MachineRef, …`, crush first tries candidate witnesses
  in Lean, about a minute per solver attempt in a large context. It leaves
  a message existential to the solver.
- An invariant `∀ p : M, … → ∃ q : M, …` re-triggers itself in z3 (its
  witness is one more machine), which can then diverge even on a step
  that changes nothing.

Keep a disjunction out of the top of a conclusion too: write `A ∨ B` as
`¬A → B`. (In the one case tried, a conclusion `(∃ …) ∨ (∃ …)` that read a
set stalled crush's own search in Lean; the implication did not.)

**Sets a handler has just changed.** Lookups (`x ∈ s`) are unfolded by
normalisation, where they occur in the goal. Inside the solver, `∈` on
`votes.add x` (what `votes += (x)` leaves) is uninterpreted. So a fact
quantified over sets, `∀ q : set[…], …`, cannot be used at the set a
handler has just extended, which is where a size test (`votes.card ≥ k`)
needs it. State the fact for a set and the element added: a binder `x`
beside `q`, the size premise on `q.add x`, and a conclusion that speaks
of `x` and `q` separately.

```lean
-- cannot be used at `votes.add x`
∀ q : set[T], (q.card : Int) ≥ k → ∃ a, a ∈ q ∧ P a
-- can
∀ (q : set[T]) (x : T), ((q.add x).card : Int) ≥ k → ¬ P x → ∃ a, a ∈ q ∧ P a
```

`(q.add x).card` is the term the handler's test leaves, so the solver
finds the instance.

**Facts about `sizeof`.** `card` is opaque to the solvers. A fact about
sizes that the proof needs (two large enough subsets of a set share a
member, say) can be an invariant that does not mention the state. Prove
its `base_<T>` by hand from the `PSet` lemmas (`card_add`,
`card_le_of_sub`, `quorum_intersect`); its other obligations close as
hypotheses. Targets that name its lemma in `using` then have the fact.

**Source and target.** An invariant whose conclusion reads the state of
both `e.src` and `e.target` (`is_A e.target s ∧ is_B e.src s`) costs z3
its quick attempt on each goal that needs it, ten seconds a goal. State
the two halves as two invariants.

**Fields an entry sets.** A field that a state's entry initialises is
arbitrary while that entry is pending. An invariant about its value needs
the guard `(s.machines n.ref).pending = none` together with the state.
What the entry relies on when it runs (that nothing has yet been derived
from the old value) is an invariant with the opposite guard,
`(s.machines n.ref).pending ≠ none`.

**Sugar.** `n.items.card` is not rewritten: write `(n.items).card`. Nor is
`∃ (a : A) (b : B), …`: write `∃ a : A, ∃ b : B, …`.

## Iterating on one obligation

Verification is slow: an example takes from seconds to over ten minutes,
and a goal the solver cannot prove costs its full timeout, plus the
search for a counterexample. Plan each run so that it answers a question.
Decide beforehand what you want to learn (which obligation fails, which
invariant, whether a new invariant helps), run only the obligations that
bear on it, and read the report and the `.smt2` files it leaves before
running again. Re-run a whole example only to confirm a fix.

```lean
set_option pverify.only "Waiting.eGlobalResponse_correct_causal" in
set_option pverify.profile true in
#pverify ClockBound
```

or, without editing the file,

```bash
lake env lean -Dweak.pverify.only=Waiting.eGlobalResponse -Dweak.pverify.profile=true \
  Examples/ClockBound.lean
```

- Proofs are cached between runs (`pverify.cache`). An obligation whose
  statement and the definitions it uses did not change replays its proof
  (`[cached]`), and in one that changed, each goal that is the same as
  before (the same conclusion, from hypotheses it still has) reuses its
  proof (`cache n`). Adding an invariant or editing one therefore sends
  only the goals it affects to the solver; editing a handler re-solves
  that handler's goals. The final theorems (`step_AllInv`, `M.safe`) are
  reused the same way when nothing they depend on changed. Every reused
  proof is checked by the kernel, and one that trusts the solver is not
  reused under `pverify.trust "reconstruct"`.
- `pverify.only` runs the obligations whose names contain one of the
  comma-separated fragments, and skips the assembly of the theorems.
- `pverify.profile` records, for each goal handed to the solver, its
  time, which solver closed it, and the goal itself when it was slow.
- `pverify.jobs` is how many obligations are proved at once (default:
  `PVERIFY_JOBS`, or one per hardware thread). The report's header gives
  the wall time; its time split adds up the obligations' times. Set it to
  `1` to time obligations alone, and lower it when other builds share the
  machine (solver timeouts are wall-clock).
- `pverify.cex false` skips the counterexample search, when you only need
  to know whether a goal closes; `pverify.timeout` (seconds) bounds the
  full-length solver attempts (the first, quick ones take 5 s each).
- A file open in the editor is checked again on every edit, `#pverify`
  included. That run competes with one from the command line (solver
  timeouts are wall-clock) and writes the same report file. While
  iterating, work on a copy outside the project (`lake env lean
  /path/to/Copy.lean`, from `Src/PLean`), or leave `#pverify` out of the
  open file.
- The report is written when the run ends. A run that makes no progress
  while no solver process is running is replaying a solver's proof in
  Lean, which can take very long on a goal that needs several
  instantiations. `pverify.heartbeats` (the unit of `maxHeartbeats`, whose
  default is 200000) bounds each attempt; the goal is then `(trusted)`.
  Take that as a sign that the step needs a key lemma (last section), and
  set the budget in a manual proof, not for the module: an attempt that
  runs out of budget before the solver answers fails its obligation.
- Before calling a proof done, run it once from scratch with every
  obligation in parallel (`-Dweak.pverify.cache=false`). A goal close to
  the timeout passes alone and fails under load, and the cache replays the
  lucky run.

To see the query a goal becomes, put the obligation's statement (the
report's skeleton) in an `example`, and save the script of what
`pverify_auto` leaves:

```lean
example : … := by
  set_option pverify.quick true in pverify_auto
  set_option crush.backend "z3" in set_option crush.inst.fuel 0 in
  set_option crush.save "/tmp/goal.smt2" in crush [*]
```

The script is written even when that `crush` call fails, as it does on a
goal the solver cannot prove. Each hypothesis is an assertion named
`crush_fact_n`. Running `z3` on the script with some of them removed tells
an invariant that is too weak (still no proof with everything) from a
context that is too large (a proof at once from a few).

## Proving an obligation by hand

When you believe the inductive invariants an obligation needs are in
place (the counterexamples are gone, or the goals in the report look
provable) but the solver still cannot prove it, prove the obligation by
hand instead of weakening the invariants. A theorem tagged
`@[pverifyProof]`, with the obligation's name and statement, written
after `#gen_module M` and before `#pverify M` inside `namespace M`,
replaces the automatic proof:

```lean
#gen_module ManualT

namespace ManualT
open PLean Std.WP

@[pverifyProof] theorem base_safety (g : GW) :
    InitConditions g → PostInv "nonneg" (nonneg g) := by
  intro h
  obtain ⟨-, -, -, -, -, -, -, -, hinit⟩ := h
  exact fun n hn => by simp [hinit n hn]

end ManualT

#pverify ManualT
```

Start from the skeleton the report file gives for the failed obligation:
it has the exact statement. `#pverify` checks that the theorem's
statement is the obligation's (up to unfolding definitions, with the same
explicit binders) and reports it as `✓ (manual)`. A manual proof fails
when its statement differs, when it uses `sorry`, or when it uses an axiom
the module does not declare (a `paxiom` or `pinstance` is fine; the
solver's trust makes it `✓ (manual, trusted)`). Only a theorem can be
tagged.

The obligation covers every invariant of its target, so a manual proof
usually lets the solver do most of it. `pverify_auto` runs the solver
with the module's definitions and facts, as `#pverify` does, and leaves
the goals it cannot prove open, one per invariant and path, tagged with
the invariant:

```lean
@[pverifyProof] theorem Node.Run.ePing_correct_lemmas : … := by
  pverify_auto
  case pv_inv.NoBypass => …
```

`Tests/Verify/ManualProof.lean` has these examples in full.

### Fewer premises

`pverify_auto` uses the hypotheses it finds. When a step times out with
all the target's premises and is easy with a few, introduce the premises
yourself and keep what the step uses (`-` clears a conjunct):

```lean
@[pverifyProof] theorem Node.Run.entry_correct_T : ∀ (g : GW) (m : MachineRef) (arg : G),
    Framework g → g.error = none → T g ∧ U g ∧ V g → … := by
  intro g m arg hF hE hInv
  obtain ⟨hT, -, -⟩ := hInv
  pverify_auto
```

### A key lemma

A few steps carry the argument of the protocol: the one that completes a
quorum, say. There the solver may answer and its proof still not replay,
or the step may need an induction. Prove what the step needs as an
ordinary theorem about one world, from the invariants, and hand it to
`pverify_auto` in their place:

```lean
/-- What the step that counts the vote `pl` and reaches `k` votes needs. -/
theorem vote_key (g : GW) (i : Nat) (src dst : MachineRef) (pl : eVote_payload)
    (hU : U g) (hV : V g)
    (hlog : g.log i = some { src := src, dst := dst, ev := E.eVote pl })
    (hst : (g.machines dst).state = some S.Node_Collecting)
    (hfull : k ≤ ((g.machines dst).fields.Node_votes.add pl.voter).card) :
    … := by
  …

@[pverifyProof] theorem Node.Collecting.eVote_correct_T : … := by
  intro g i src dst pl hF hE hInv hnet hlog hi hst
  obtain ⟨hT, hU, hV⟩ := hInv
  have key := vote_key g i src dst pl hU hV hlog hst
  clear hU hV
  pverify_auto
```

The solver is left the bookkeeping: which branch the handler took, and
that the other machines did not change.

- The lemma's hypotheses are the invariants it uses, the facts of the
  step it needs (`hlog`, `hst`), and what the handler decides as it runs
  (`hfull`: a branch condition). Leave the last kind as hypotheses of
  `key`; the solver has them on the path that needs it.
- `#print M.<invariant>` shows the statement to apply. A message is
  `⟨i, src, dst, E.<ev> pl⟩ : Msg E`, with `⟨pl, rfl⟩ : is_<ev> _` and the
  step's `log` hypothesis as its `sent`. A machine is `⟨r⟩`, with a proof
  of `is_<M> r g` (a disjunction of its states).
- An invariant applied to `⟨r⟩` speaks of `(g.machines { ref := r }.ref)`,
  which `omega` does not identify with `(g.machines r)`. State what you
  get with a type ascription (`have h : (g.machines r).fields.… = … :=
  hinv ⟨r⟩ hr …`).
- `clear` what the lemma used. What stays in the context is what the
  solver, and then the replay of its proof, have to search.
- The theorem may need `set_option maxHeartbeats 1000000 in`.
