# Writing inductive invariants for `#pverify`

`#pverify` proves each target by induction over steps of the transition
system: every invariant holds initially (`base_<T>`), and every handler
preserves it (`<M>.<S>.<ev>_correct_<T>`, `entry_correct_<T>`). When an
obligation does not close, the fix is almost always a stronger
invariant, not a proof.

## Reading the report

`#pverify M` prints one line per obligation and writes the full report,
with the goals the solver failed on, to `.lake/pverify/M.txt`.

| Outcome | Meaning | What to do |
|---|---|---|
| `✓` | proved, with a checked proof | — |
| `✓ (trusted)` | the solver proved it, but its proof could not be replayed | usually fine; simplify the invariant if a checked proof matters |
| `✗ [counterexample]` | the solver found a state where the step breaks the invariant | the invariant is false or not inductive: strengthen it |
| `✗ [unknown]` | the solver timed out | the step needs too much reasoning in one query: add intermediate invariants |

A counterexample is a pre-state satisfying the premises, not necessarily
a reachable one. The usual cause is a fact that holds in every reachable
state but is not among the premises: add it as an invariant (and
`prove` it).

## Patterns

**Routing.** Never-receives obligations (`<M>.<S>.<ev>_never`) need to
know where each event can go, e.g.

```lean
invariant yes_to_coordinator : ∀ e : eYes, inflight e s → e.target = coordinator
```

together with a kind fact (`is_Coordinator coordinator s`).

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

**`using`.** `prove T using U ;` gives `T`'s obligations `U` as an extra
premise (`U` must be proved too). Smaller premise sets make queries
faster; add what a counterexample shows is missing.

## Iterating on one obligation

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

`pverify.profile` records, for each goal handed to the solver, its
time, which solver closed it, and the goal itself when it was slow.
