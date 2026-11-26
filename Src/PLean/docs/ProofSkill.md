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
| `✓ (trusted)` | the solver proved it, but its proof could not be replayed | usually fine; simplify the invariant if a checked proof matters |
| `✓ (manual)` | proved by a `@[pverifyProof]` theorem | — |
| `✗ [counterexample]` | z3 found a state where the step breaks the invariant | the invariant is false or not inductive: strengthen it |
| `✗ [unknown]` | the solver timed out, and no counterexample was found | the step needs too much reasoning in one query: add intermediate invariants, or prove it by hand |
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
