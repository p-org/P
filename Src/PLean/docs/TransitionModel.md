# PLean transition model

> **Status: implemented (phases P0–P5), revision 4.** This document
> describes the model as built. Where the original plan chose
> differently, the implementation is described and the difference noted.
> It replaced the interim `PM := StateT (GlobalState P) Id` model that
> the Lean v4.36 bump (`3db989bba`) left in place.

## Revision history

| Rev | Change |
|---|---|
| 2 | Added `$` and `choose`, and `receive` as defunctionalized continuations (§3.6). Moved to simple encodings (functions, counters, datatypes). Kept specs as state formulas, not traces (§4.6). |
| 2.1 | crush defunctionalizes goal lambdas, so discharge is `vcgen; crush` with no normalisation pass (§4.4). |
| 3 | Answers to the open questions recorded (§0). **User containers became finite history datatypes** instead of bare functions (§3.1). Without that, `sizeof` lemmas are unsound, and enumeration and model-checker state hashing are impossible for int/string keys. Crush still sees the same lookup equations. Explicit-state BMC is now the first model checker (§6). |
| 3.1 | Loop invariants are evaluated over a tentative world, with PVerifier's `new` quantifier ranging over the handler's outbox, plus auto-generated frame conjuncts (§4.7). Model-checking items moved to a backlog (§10). P0 solver and sequencing notes. |
| 4 | Aligned with the implementation: errors are exceptions of the handler monad, `raise` is a direct call, only top-level `receive`, loop invariants over the handler state (§4.7). Reference `0` is P's `null`, and a send to it fails. P's `==` on containers and map errors (§3.1). |

## 0. Decisions

| # | Decision |
|---|---|
| D1 | Adopt Verdi's transition semantics (Wilcox et al., PLDI'15): a world of machine states plus network state, changed by small-step rules. |
| D2 | **Handlers are local**, as Verdi's `HandleMsg`. A handler sees its own machine state and the delivered message. It returns its new state plus what it sent and created. One generic `commit` performs the world update. |
| D3 | Handlers are do-notation programs. Obligations are produced by core `vcgen` (`Std.WP`). |
| D4 | The model is executable (compiled, and kernel-reducible where possible), so that P's model-checking algorithms can be built on it. |
| D5 | **Message history is ghost state.** The real world holds only in-flight packets. The ghost layer adds the log of everything ever sent. |
| D6 | SMT discharge goes through **lean-crush**, branch `bump/4.36.0`. lean-auto and Mathlib are dropped. |
| D7 | Proofs are relative to `InitConditions`. **`init-holds` describes the set of initial states symbolically.** As in PVerifier, every machine starts in its start state with its start entry pending. Model checking starts from a concrete initial world that satisfies `InitConditions`. |
| D8 | **Simple encodings.** Verification conditions only mention:<ul><li>total functions with counter-bounded domains, for world-level state;</li><li>`Nat` counters;</li><li>non-recursive datatypes;</li><li>the two container history datatypes of §3.1. Their lookup equations are exactly the defining equations of the corresponding closures.</li></ul> |
| D9 | **Specs are formulas** over the ghost world. There is no trace semantics (§4.6). |
| D10 | `$`, `$$` and `choose` are in scope. **`choose(n)` ranges over `[0, n]`**, PVerifier's contract. |
| D11 | **Unhandled events are errors**, checked by "never receives" obligations. |
| D12 | `receive` is supported, but **not inside recursive functions** yet. |
| D13 | **`defer` is supported; `exit` handlers are not.** |
| D14 | **Default trust policy: `reconstructOrTrust`,** with trusted obligations reported as such; `reconstruct` is opt-in. |
| D15 | **First model checker: explicit-state BMC** (§6). |

**Goals.**
- A single semantics serves three uses: inductive proofs, execution, and model checking.
- `#pverify` ends in a Lean theorem `∀ w, Reachable w → Inv w ∧ w.error = none`.
- PVerifier's per-handler obligation granularity is kept.

**Non-goals for now.**
- Liveness. `$$` is treated as `$`.
- Spec monitors: ROADMAP Phase 4, with a hook in §3.9.
- Fault models beyond reordering.
- `exit` handlers.
- `receive` in recursive functions.
- `seq[T]` in SMT. It stays an executable `List` with no SMT support, as today.

---

## 1. Verdi, mapped onto P

| Verdi | PLean |
|---|---|
| `Name` (fixed) | `MachineRef := Nat`. `0` is P's `null`; allocation is dynamic via `new`, from `1`, and refs at or above `nextRef` are unallocated. |
| `State n` | `MachineState`: P state tag (which determines the kind), fields, pending entry argument, `receive` wait |
| packet `(src, dst, m)` in bag `P` | `net : Nat → Option Packet`, in-flight packets keyed by send-order id |
| `HandleMsg` in a state+writer monad | `HandlerM := StateT HState (Except PError)`. It writes to `ms` and appends to the `out` / `spawn` / `obs` logs; a runtime error aborts it. |
| Rule DELIVER | `step w (.deliver i)`, which runs the handler and then `commit` |
| Reordering vs other fault semantics | `NetSem.bag` (proofs) vs `NetSem.fifo` (P's semantics, used for model checking) |
| Inductive state invariant on `(Σ, P)` | `Inv` on the ghost world, plus `Framework`, proved once |
| Ghost variables, indistinguishability | `GWorld extends World` with one field, `log`. Erasure is `toWorld`. |
| IO trace `T` with trace specs | Not needed (§4.6) |

---

## 2. Layering

```
 surface (pmodule)
        │  #gen_module (+ receive splitting, §3.6)
        ▼
 handlers / receive-resumers: do-programs in HandlerM   ← vcgen operates on these
        │  runHandler
        ▼
 commit : World → Choice → HState → World               ← one generic function
        │
        ▼
 Enabled (bag | fifo) / step, Reachable                 ← executable; model checker API
        │  toWorld (erase ghost)
 GWorld (adds `log`) ─────────────────────────────────► World
```

---

## 3. Semantics

### 3.1 Collections

**Handler outputs** use an append-only log:
```lean
structure Log (α : Type) where
  get  : Nat → Option α
  size : Nat
def Log.push (l : Log α) (a : α) : Log α :=
  ⟨fun i => if i = l.size then some a else l.get i, l.size + 1⟩
```

**User containers are finite history datatypes,** i.e. defunctionalized closures:
```lean
inductive PSet (T : Type) | empty | add (s : PSet T) (x : T) | del (s : PSet T) (x : T)
inductive PMap (K V : Type) | empty | put (m : PMap K V) (k : K) (v : V) | del (m : PMap K V) (k : K)

@[crush_unfold] def PSet.mem [DecidableEq T] : PSet T → T → Bool
  | .empty,   _ => false
  | .add s x, y => decide (y = x) || s.mem y
  | .del s x, y => !decide (y = x) && s.mem y

@[crush_unfold] def PMap.get [DecidableEq K] : PMap K V → K → Option V
  | .empty,      _  => none
  | .put m k v,  k' => if k' = k then some v else m.get k'
  | .del m k,    k' => if k' = k then none else m.get k'
```

**Why not bare functions `T → Bool` / `K → Option V`?** Three reasons:

1. **`sizeof` needs finiteness.** Over arbitrary functions, the lemmas "add raises the count by one" and "delete lowers it by one" are inconsistent. Start from `fun _ => true` with count `c` and delete `c + 1` elements, which takes the count to 0. Then reach the same set two ways: by deleting the first `c` elements directly (count 0), or by re-adding the `(c+1)`-th element (count 1). History values are finite by construction, so the lemmas hold unconditionally.
2. **Enumeration needs data.** `foreach`, `choose(s)`, `keys`/`values` as sequences, and `==` can't enumerate a closure over `int`/`string`. A history datatype lists its support.
3. **Explicit-state BMC (D15) needs canonical, hashable states.** A history value can be compacted to a canonical form; a closure can't.

**What crush sees is unchanged.**
- For a post-state term like `(s.add x).mem y`, crush unfolds one equation, giving `y = x ∨ s.mem y`.
- `s.mem y` on a pre-state `s` stays an uninterpreted application, exactly as `s y` did.

Recursive datatypes with structurally recursive accessors are supported by crush; P1 measures them.

| P | Encoding | Verification view | Execution |
|---|---|---|---|
| `default(set)`, `default(map)` | `.empty` | | |
| `x in s` / `k in m` | `s.mem x` / `(m.get k).isSome` | `isSome` is the datatype tester | ✓ |
| `s += (x)`, `s -= (x)` | `.add`, `.del` (absent → no-op) | unfold `mem` | ✓ |
| `m[k] = v`, `m -= (k)` | `.put` (upsert), `.del` (absent → no-op) | unfold `get` | ✓ |
| `m += (k, v)` | `minsert`: a key already present fails (`duplicateKey`), else `.put` | `⦃(m.has k → Q_err) ∧ (¬m.has k → Q (m.put k v))⦄` | ✓ |
| `m[k]`, handler | `(← cindex m k)`, which `#gen_module` writes for a read `m[k]` (absent → `keyNotFound`); `m[k] += (e)` reads the same way | `⦃(∀ v, m.get k = some v → Q v) ∧ (m.get k = none → Q_err)⦄` | ✓ |
| `m[k]`, formula | `(m.get k).getD default` | guard with `k in m` where it matters | ✓ |
| `x in keys(m)` | `(m.get x).isSome` | | ✓ |
| `v in values(m)` | `∃ k, m.get k = some v` | quantifier | bounded by the support |
| `sizeof(s)`, `sizeof(m)` | `card := support.length` | **not unfolded.** Lemma hints: `card_add`, `card_del`, `card_empty`, `card_le_of_sub`, `quorum_intersect` | ✓ |
| `foreach (x in s)`, `foreach (k in keys(m))` | core `for x in s.support invariant …` | PVerifier's visited-set rule, with loop invariants as in §4.7. Lemma hints: `mem_support`, `List.mem_append`. | ✓ |
| `choose(s)`, `choose(m)` | a coin indexes `support`; empty → `err` | `⦃(∀ x, s.mem x → Q x) ∧ (card s = 0 → Q_err)⦄` | ✓ |
| `keys(m)`, `values(m)` as a seq | `List` from the support | seq: no SMT (as today) | ✓ |
| `s1 == s2` | handlers: `==` (`PSet.beq` / `PMap.beq`). Specs and `function` bodies: `=` / `≠` between containers is read as `PSet.Equiv` / `PMap.Equiv` (Lean's `=` compares histories). A decided `=` between containers is rejected in both | `∀ x, s1.mem x = s2.mem x` | decidable via supports |

Further details:
- **Support and card.** `support : PSet T → List T` gives the members without duplicates, and `card := support.length`. Its lemmas are proved once, by induction on the history: membership iff in the support, no duplicates, the count updates, and pigeonhole.
- **Quorums.** Keep `isQuorum` uninterpreted with the intersection `paxiom` (crush accepts it applied to a set; lean-auto did not). Or define it by `card`, which makes the axiom the theorem `quorum_intersect`.
- **Opaque sets from `pure` functions** (`participants()`) are constants of type `PSet`. The model checker's concrete initial world supplies their values.
- **Iteration order.** `foreach` and `values` follow `support`, whose order depends on the history. P leaves the order unspecified, so a property that depends on it (say, `≺` between two sends of one loop) is proved for this order only.
- **Nested containers.** In handlers, `==` on a map's set values and on an anonymous tuple's sets compares contents (through the `Option` / `Prod` `BEq` instances). In specs, `PMap.Equiv` compares a map's values with `=`, so inner containers by history. Named tuples (which derive `DecidableEq`) and sets of sets compare inner containers structurally in both.
- **History growth.** A history grows with every update. `compact`, a canonical rebuild with the same `mem`/`get`, is used for fingerprints (§6). It may also be applied at runtime, but never inside the verified definitions.

### 3.2 Machine-local state

```lean
structure MachineState (S E F G W : Type) where
  state   : Option S      -- none = unallocated; the state tag determines the kind
  fields  : F             -- vars, containers included (no hoisting)
  pending : Option G      -- the state's entry still has to run, with this argument
  waiting : Option W      -- suspended in a `receive`: one W constructor per site, with its locals
  halted  : Bool
```

The program-specific types are parameters (`S` state tags, `E` events,
`F` fields, `G` entry arguments, `W` suspended `receive`s), emitted by
`#gen_module`. The plan's `raise` continuation is gone: `raise e` calls
the current state's handler for `e` directly and returns. Handlers are
emitted so that each comes after every handler it raises into; a cycle
of raises is rejected, and so is raising an event the state neither
handles nor ignores.

### 3.3 Handler monad and primitives

```lean
structure HState (S E F G W : Type) where
  ref   : MachineRef                     -- read-only: `this`
  ms    : MachineState S E F G W
  out   : Log (Packet E)                 -- sends, at their packet ids (from the world's clock)
  spawn : Log (MachineState S E F G W)   -- `new`, at their refs (from the world's nextRef)
  obs   : Log E                          -- `announce`
  coins : Nat → Nat                      -- nondeterministic choices (a stream)
  used  : Nat                            -- number of coins consumed
abbrev HandlerM S E F G W := StateT (HState S E F G W) (Except PError)
```

A runtime error (`PError`: a failed `assert`, a missing or duplicate map
key, an empty or negative `choose`, a send to `null`, a sequence index
out of range) is an exception: the handler commits nothing and the world's
`error` is set.

| P construct | Effect | Spec, as `vcgen` sees it |
|---|---|---|
| field get / set | read or write `ms.fields` | standard `get`/`set` |
| `send t, e` | `t = null` fails; else `out := out.push ⟨this, t, e⟩` | `⦃(t = null → Q_err) ∧ Q (out.push …)⦄` |
| `new M(x)` | `spawn.push` a machine with `pending := some x`; return its ref `spawn.size` | direct |
| `goto S, x` | set `state := S` and `pending := some x`, then `return` (no `exit` handlers, D13). `on e goto S` passes `e`'s payload. The argument must match S's entry, as P checks | direct |
| `raise e` | call the current state's handler for `e`, then `return` | the handler's code |
| `$`, `$$` | `flip`: read the next coin; `true` iff it is even | `⦃Q true ∧ Q false⦄` |
| `choose(n)`, int | a value in **`[0, n]`** (D10); `n < 0` gives `err` | `⦃(0 ≤ n → ∀ i, 0 ≤ i ≤ n → Q i) ∧ (n < 0 → Q_err)⦄` |
| `choose(s)` / `choose(m)` | see §3.1 | see §3.1 |
| `assert c` | if `¬c`, fail | `⦃(c → Q) ∧ (¬c → Q_err)⦄` |
| `foreach` | see §3.1 | see §3.1 |
| `receive { … }` | save the locals, set `waiting`, `return` (§3.6) | direct |

`[0, n]` gives one more value than PChecker's `[0, n)`. Proofs cover a
superset of PChecker's behaviours, and BMC explores `n + 1` values.

### 3.4 Real world and ghost world

```lean
structure Packet (P : ProgramSig) where
  src dst : MachineRef
  ev : P.E

structure World (P : ProgramSig) where
  machines : MachineRef → MachineState P   -- Σ
  nextRef  : Nat                           -- refs ≥ nextRef, and `null` (0), are unallocated
  net      : Nat → Option (Packet P)       -- in-flight packets by id (Verdi's bag)
  clock    : Nat                           -- next packet id; ids record send order
  error    : Option PError

structure GWorld (P : ProgramSig) extends World P where
  log : Nat → Option (Packet P)            -- ghost: every packet ever sent, never cleared
```

`machines` and `net` stay functions. Their domains are bounded by the
counters `nextRef` and `clock`, so they can be enumerated and
fingerprinted.

Derived predicates:
- `sent i ↔ log i ≠ none`
- `inflight i ↔ net i ≠ none`
- `received i ↔ sent i ∧ ¬inflight i`
- `i ≺ j ↔ i < j`

**Erasure** is `toWorld`. No guard reads `log`, so the simulation is
essentially by definition.

### 3.5 Choices, steps, network semantics

```lean
inductive Choice
  | deliver (i : Nat)              -- deliver in-flight packet i to its destination
  | control (m : MachineRef)       -- run m's pending entry / raise

def Enabled (sem : NetSem) (w : World P) : Choice → Prop    -- decidable
def step (w : World P) (c : Choice) (coins : Nat → Nat) : World P

inductive Reachable (sem : NetSem) (Init : World P → Prop) : World P → Prop
  | init : Init w → Reachable sem Init w
  | step : Reachable sem Init w → Enabled sem w c → Reachable sem Init (step w c coins)
```

Let `m := p.dst` where `net i = some p`. Then `Accepts w m ev` holds when:
- `m` is waiting at site `k`, and `ev` is one of `k`'s cases (every other event is implicitly deferred, as P's manual specifies); or
- `m` is not waiting, and `ev` is not deferred in `m`'s state.

| Choice | Enabled under `bag` | Extra guard under `fifo` | Effect |
|---|---|---|---|
| `deliver i` | `net i = some p`; `m` allocated with `pending = none`; and either `Accepts w m p.ev` or `m` is halted | `∀ j < i, net j = some q → q.dst = m → ¬Accepts w m q.ev` | Set `net i := none`. If waiting: run the resumer for `(k, ev)`. Else run the handler for `(kind, state, ev)`. `ignore` is a no-op. A halted target drops the packet. An unhandled event sets `error` (D11). |
| `control m` | `pending = some x` | none | runs the current state's entry with `x` |

Because `fifo` only adds a conjunct, `Reachable fifo ⊆ Reachable bag`.

### 3.6 `receive` as defunctionalized continuations

**As implemented,** only a `receive` at the top level of a handler or
entry body is supported (not inside `if`, a loop or another `receive`
case); steps 1 and 3's loop re-entry below are not implemented. The body
is split there. The code before it suspends with `W.site<k>`, which holds
the parameter and the `var` locals declared so far (a `let` before the
`receive` that is used after it is rejected). Each case's resumer
restores them, runs the case, then the rest of the body, which may
suspend at the next site.

The plan: `waiting := some ⟨k, frame⟩` stores the continuation as data.
`k` is a static site tag, and `frame` holds the handler's locals and
parameters. P declares locals at the top of a function. `#gen_module`:

1. **Inlines receive-bearing functions** at their call sites. Recursion through a `receive` is rejected (D12).
2. **At every site `k`:** save the locals, set `ms.waiting := some ⟨k, frame⟩`, and `return`.
3. **Builds the continuation `K_k`:** the rest of the enclosing block, then each enclosing loop re-entered (from the innermost outwards), followed by the code after it. Receives inside `K_k` suspend as in step 2. A loop whose body always passes through a `receive` doesn't iterate within one micro-step. Loops where some paths skip it keep a residual `while` with an early `return`.
4. **Emits one resumer per `(k, case)`:** restore the frame, set `ms.waiting := none`, run the case body, then `K_k`.

The state stays first-order, and there is one obligation per
`(site, case)`. Invariants can say that machine `n` waits at site `k`,
and read `frame.x`.

Closure continuations are rejected even though crush would translate
them. `vcgen` needs concrete per-site code, and invariants need to name
the frame.

### 3.7 Commit

`commit w m hs` (and `gcommit`, which also writes `log`) applies a
handler that completed; the delivered packet was removed before it ran
(`consume`). The logs `out` and `spawn` are indexed by their final packet
ids and refs: they start at the world's `clock` and `nextRef`.
```
machines' r = if r = m then hs.ms
              else if nextRef ≤ r < spawn.size then spawn.get r
              else machines r
nextRef'    = max nextRef spawn.size
net' j      = if clock ≤ j < out.size then out.get j else net j
clock'      = max clock out.size
log' j      = (as net')                                   -- ghost only
error'      = error
```
A handler that fails sets `error` and commits nothing.

### 3.8 Initial worlds (D7)

- **Proofs:** any `g : GWorld` with `InitConditions g`. That means:
  - `clock = 0`, `net = log = fun _ => none`;
  - every `r` in `[1, nextRef)` is allocated and in its start state, with its start entry pending (any argument) and no `waiting`;
  - every `r ≥ nextRef` is unallocated, and so is `null` (`0`), with `0 < nextRef`;
  - the user's `init-holds` facts, which describe the initial states symbolically.
- **Model checking:** a concrete `World` that satisfies `InitConditions`. It is either given by the user, or generated, then validated with `decide` when `InitConditions` is decidable on concrete worlds (§6).

### 3.9 Hook for spec monitors (ROADMAP Phase 4)

Monitor states become a world component. They are updated inside
`commit` by folding monitor handlers over `obs` and `out`, in program
order. A failed monitor `assert` sets `error`.

---

## 4. Verification

### 4.1 What `#pverify` proves

```lean
theorem <Mod>.safe      : ∀ g, GReachable prog .bag  InitConditions g → AllInv g ∧ g.error = none
theorem <Mod>.safe_fifo : ∀ g, GReachable prog .fifo InitConditions g → AllInv g ∧ g.error = none
theorem <Mod>.<T>_holds : ∀ g, GReachable prog .fifo InitConditions g → T g
```
`AllInv` is the conjunction of the proved targets and the automatic
invariants. A `_holds_real` theorem on the real world is not generated;
`GReachable.erase` and `Reachable.lift` move between ghost and real runs.
The report lists what `safe` assumes beyond Lean's axioms (`paxiom`s,
`pinstance`s, the solver's word for trusted obligations, `sorry`), and
warns when no world satisfies `InitConditions` (with the axioms).
These are assembled by a generic lemma:
- **Base case:** `InitConditions g → Inv g`.
- **Step:** for every `c` and every coin stream, `Inv ∧ Framework ∧ Enabled bag g c` implies `Inv` and no error after `gstep g c`.
- **Conclusion:** `Reachable → Inv ∧ error = none`.

Handler coverage is part of the proof. `using` premises keep their
meaning: a premise that isn't proved is a `[missing premise]` error.

### 4.2 Obligation shapes

| Obligation | One per | Statement |
|---|---|---|
| deliver | `(M, S, ev)` handler | `⦃hs = HState.start g p.dst coins ∧ Inv g ∧ Framework g ∧ DeliverGuard g i (M,S,ev) pl⦄  M.S.ev pl  ⦃hs'.err = none ∧ Inv (gcommit g (.deliver i) hs')⦄` |
| resume | `(site, case)` | same shape, with `ResumeGuard` and the resumer |
| entry | `(M, S)` with an entry | same shape, with `ControlGuard g m (.entry S x)` |
| raise | `(M, S, ev)` that can be raised | same shape, with `ControlGuard g m (.raise ev)` |
| never receives | `(M, S, ev)` with no handler, `ignore` or `defer` (D11) | `Inv g ∧ Framework g ∧ DeliverGuard … → False` |
| base | each invariant | `InitConditions g → Inv_i g` |

### 4.3 Framework invariants (proved once, in Lean)

`Framework g`:
- **F1:** `∀ j ≥ clock, net j = none ∧ log j = none`.
- **F2:** `net j = some p → log j = some p`.
- **F3:** `∀ r ≥ nextRef, (machines r).state = none`.
- **F4:** `0 < nextRef` and `(machines null).state = none`.

(The plan's F4, that packet endpoints are below `nextRef`, is not
implemented.)

These are proved inductive generically, about `gcommit`. As a result:
- `DefaultInvariants`, `prove default`, `using default` and `pverify.autoProveDefault` are retired;
- `prove default ;` keeps parsing, as a deprecated no-op.

### 4.4 Discharge: `vcgen` → `crush`

1. `vcgen`, using the primitive specs (§3.1, §3.3).
2. `crush`, with `commit`, `Log.push`, `PSet.mem`, `PMap.get` and the derived predicates tagged `@[crush_unfold]`. `card` and `support` are not unfolded; their lemmas are passed as hints.
   - World-level updates are lambdas, which crush defunctionalizes.
   - Container updates are constructor terms, which crush unfolds through `mem`/`get`.
   - A `dsimp` projection pass is kept only if P1 shows it helps.
3. **Trust (D14):** `pverify.trust` defaults to `reconstructOrTrust`: an obligation that the solver proves but reconstruction can't replay is reported as `✓ (trusted)` (its theorem then depends on crush's trust axiom). `reconstruct` makes such obligations fail.
4. **Counterexamples:** each post-condition invariant is marked with its name (`PostInv`), so a goal that is not proved names its invariant. For such a goal, crush writes the SMT encoding; z3 is asked for a model of it with the recursive well-formedness predicates dropped, and the model is minimised (fewest machines, fewest packet ids, fewest packets in flight and delivered, smallest integers). `#pverify` decodes the model into the pre-state world: the machines by kind, with their state and fields, and the packets.

**Removed:**
- `PLean/Verify/Smt.lean`;
- lean-auto and Mathlib;
- `ProgramSig.C` container hoisting;
- the `@[pverifySimp]` lookup-after-update lemma library;
- the Veil-style prep (`sdestruct_state`, `destruct_machine_state`, `abstract_machine_lookups`, `split_conjunction_hyps`);
- the lean-auto model parser.
- **Likely removed, if P1 confirms:** the sealed `<ev>_payload_of` extractors, and the `_spec` / `paxiom` facts lifted into the local context with `have`.

### 4.5 Invariant surface: old → new meaning

| Surface | Today (`s : GlobalState`) | New (`s : GWorld`) |
|---|---|---|
| `∀ n : M, …` | `∀ n, is_M n.ref s → …` | `∀ r, (s.machines r).kind = some .M → …` |
| `n.x` | `(s.machines n.ref).fields.M_x` | the same, with no `Containers` indirection |
| `stateOf n`, `n is S` | `currentState` | `(s.machines r).state` |
| `∀ e : eVote, sent e → …` | over `Label`, `s.sent` | `∀ i p, s.log i = some p → p.ev is eVote → …` |
| `inflight e`, `received e` | `sent ∧ ¬received` | `s.net i ≠ none`; `s.log i ≠ none ∧ s.net i = none` |
| `e targets n` | `lbl.target = n.ref` | `p.dst = r` |
| `e.f` | `(ev_payload_of e).f` | the same, on `p.ev` |
| `a ≺ b` | `actionCount` order | `i < j` |
| `x in s`, `k in m`, `sizeof(s)` | `PSet` / `PMap` atoms | `s.mem x`, `(m.get k).isSome`, `card s` |
| (new) `n waits_at k`, `n.frame.x` | none | `(s.machines r).waiting = some ⟨k, f⟩`, `f.x` |

The field-projection sugar and kind-guard injection passes are retargeted
to these meanings. The soundness pins are re-stated, not dropped.

### 4.6 Why formulas, not Verdi's traces

Verdi's top-level specs are trace predicates because its guarantees
concern externally visible IO. They must also survive system
transformers that change the internal state. PLean needs neither layer:
- **P/PVerifier specs are already state formulas.**
- **The ghost `log` is the send trace,** with `≺` as its order.
- **Our transfers act on states:** `fifo ⊆ bag` and ghost erasure.
- **Spec monitors run as world state** (§3.9), so "the monitor never fails" is the state formula `error = none`.

A trace layer would only matter for refinement-style specs, such as
linearizability. The ghost log would supply that trace.

### 4.7 Loop invariants and sends inside loops

**The problem.** A `send` inside a `foreach`/`while` changes what the
handler will put on the network, so the loop invariant must track it.
PVerifier writes this with `new`-quantifiers over the events sent so far
by the current handler, for example in 2PC:
```
foreach (p in participants())
  invariant forall new (e: event) :: forall (m: machine) :: e targets m ==> m in participants();
  invariant forall new (e: eVoteReq) :: e.round == r;
{ send p, eVoteReq, (round = r,); }
```

**Meaning in this model.** Handlers are local (D2), so a send only
appends to the outbox `hs.out`. The world `g` itself doesn't change until
`commit`. A loop invariant is a predicate over the handler state `hs` and
the loop's locals: `hs.ms` is this machine's state, `hs.out` its sends so
far. (The plan evaluated loop invariants as world formulas over a
tentative world `gcommit g c hs`; that is not implemented.)
- **`new` events.** `new_sends e => P` is PVerifier's `forall new (e: E) :: P e`: every message the loop has sent so far (`sentSince __loop0 hs`) satisfies `P`.
- **`pref` / `suff`** (foreach only) name the items already visited and still to visit. PVerifier's `visited` is not implemented.

**Frame conjuncts are generated automatically.** A core `vcgen` loop
invariant is a predicate on the whole `HState` plus the loop locals, so
whatever the loop doesn't change must be restated. The loop macros add
`HState.frame __loop0 hs …` from a syntactic write-set analysis of the
loop body:
- `hs.ref` is preserved, and the outbox only grows (the pre-loop outbox is a prefix);
- the machine state, the outbox, the spawned machines and the announcements are unchanged when the body cannot change them.

Errors are exceptions, so no `err = none` conjunct is needed.

**Simpler than PVerifier.** `Inv g` and `Framework g` mention only the
pre-state world, which is constant during the handler. They stay
available as ordinary hypotheses in every loop VC. So the top-level
invariants don't need to be re-asserted as loop invariants (PVerifier
injects all of them). The old "loop-aware `default_inv`" problem doesn't
arise.

User loop invariants that restate facts about other machines (`forall
(m: Proposer) :: m != this ==> …`) stay valid and are simply redundant.
Facts about `this`'s own fields are still needed when the loop changes
those fields.

---

## 5. Execution

- Handlers compile, and `step` is computable.
- The kernel reduces structural recursion (`mem`, `get`, `support`, `card`) and `for` over a `List`, but not `while` (a `partial` definition, checked on v4.36).
- Kernel `decide` works for runs without `while`. The model checker uses compiled code.
- `#prun <Mod> from <init> [choices]` runs a schedule. This is for tests and counterexample replay.

---

## 6. Model checking: explicit-state BMC first (D15), backlogged until verification lands

The design constraints below still shape the semantics now. The
implementation waits until P5 is done.

- **Search:** breadth-first, depth-bounded exploration of the real `World` under `fifo`, P's semantics.
  - Successors come from every `c` with `Enabled fifo w c`, found by scanning `i < clock` and `m < nextRef`.
  - States are concrete, so every successor fixes each `choose(n)` / `$` / `choose(s)` to one concrete value. BMC branches over all of them; a choice's coins are enumerated *exhaustively*:
    - `flip`: 2 values;
    - `choose(n)`: `n + 1` values;
    - `choose(s)`: `card s` values.

    The checker records the bound at each coin read, so it can re-execute a handler to enumerate its choice tree.
- **Visited set,** keyed by a canonical fingerprint `fp : World → Fp` (derived `Hashable`/`BEq` on `Fp`):
  - machines `< nextRef`, tabulated;
  - in-flight packets `< clock`, renumbered by order (FIFO depends only on relative order);
  - container fields `compact`ed to sorted canonical form, which needs `Ord` on key types (derived for P types);
  - frames and monitor state.

  The ghost `log` is excluded.
- **Checked properties:**
  - `error = none` (asserts, never-receives, bad lookup or `choose`);
  - user invariants that mention only real state and are decidable on concrete worlds.

  Ghost-history invariants need a `GWorld` run without caching, which is a later mode.
- **Initial world:** concrete and satisfying `InitConditions` (D7). It is user-given, or generated by enumerating small worlds and filtering with `decide`.
- **Counterexample:** the list of `(Choice × coins)` steps to an error. It can be replayed with `#eval`, or with `decide` when no `while` is on the path.
- **Link to proofs:** every proved invariant holds on every state BMC visits. This doubles as a cross-check of both.
- **Later:** random and PCT schedulers over the same `Enabled`/`step` interface.

---

## 7. Phases

| Phase | Deliverable | Exit criterion |
|---|---|---|
| **P0 Toolchain** | `require crush … @ "bump/4.36.0"`. The bundled z3 4.15.4 and cvc5 1.3.4 live in `.lake/build/bin` and must be on `PATH`. Default `crush.backend "cvc5"`. lean-auto and Mathlib are removed with the old discharge chain in P4. | `crush` closes the round-trip goals with `reconstruct` |
| **P1 Prototype** | Hand-written `World`/`GWorld`/`HandlerM`/`commit`/`PSet`/`PMap` and a hand-written DistributedLock, plus one `receive` site, `$`, `choose(n)`, `choose(s)`, one `foreach` and one `sizeof` quorum. No code generation. | One obligation of each shape (deliver, resume, entry, never-receives, base) closes with `vcgen` + `crush` under `reconstruct`. `#eval` runs a schedule. Encoding questions answered: closure depth, recursive `mem`/`get`, `card` hints, the visited-set rule. |
| **P2 Semantics library** | `Semantics/{Log,Containers,Handler,World,Ghost,Step,Framework,Induction}.lean`: `Framework` inductive, erasure, `fifo ⊆ bag`, the generic `Reachable → Inv ∧ error = none` lemma, and the container lemma library (`mem_support`, the `card` lemmas, `quorum_intersect`) | All proved, with no `sorry` and no `trust` |
| **P3 Code generation** | `#gen_module` emits types, handlers, `dispatch`, entries, and the `receive` split (non-recursive only). Also `$`/`choose`/`foreach`/`sizeof`, and the `defer` surface. | Ported `Combinators` run tests pass. EspressoMachine `Users.p` (receive in a loop, in a function) generates and runs. |
| **P4 Obligations** | New shapes (§4.2), final theorems (§4.1), surface retargeting (§4.5), the crush chain with `reconstruct`, counterexample decoding | `ObligationShape`, `HandlerCoverage` and `SoundnessRegression` are re-pinned; DistributedLock and LockServer reach full closure |
| **P5 Port** | Remaining tests and examples, including ClockBound and the manual `@[pverifyProof]` proofs. Optionally, Consensus with `isQuorum` defined by `card`. | Back to the pre-bump closure numbers |
| **P6 Explicit-state BMC** (backlog) | `#prun`, fingerprints and `compact`, BFS with a visited set, exhaustive coin enumeration, counterexample replay, generation of initial worlds | Finds a seeded bug in an example; its replay is kernel-checked; all proved invariants hold on every visited state of the verified examples |
| later | Random/PCT; spec monitors (§3.9); Verdi fault semantics | none |

---

## 8. Risks

- **Crush on recursive container accessors** (`mem`, `get` over histories) and on deep closure chains in `commit`. P1 measures both. The fallback is a `dsimp` projection pass.
- **Reconstruction coverage under `reconstruct`.** Some solver-proved obligations may fail to replay. They are reported as `reconstruct-failed`, not silently trusted.
- **Visited-set `foreach` rule:** the `support`-prefix membership must stay manageable for crush. P1 checks this.
- **`receive` splitting:** code-size growth, and loops where only some paths hit a `receive`.
- **Semantic shift for existing proofs:**
  - start entries are pending in every initial world, so they run like any other entry;
  - `raise` is local;
  - goto labels leave the network.
- **`vcgen` is experimental:** its API may change across toolchain bumps.
- **BMC state explosion,** from coin enumeration (`choose(n)` gives `n + 1` values) and from history-induced fingerprint cost. Mitigation: `compact` on commit at runtime, and depth bounds.

---

## 9. Open questions

None right now. The `foreach` invariant surface is settled in §4.7.

## 10. Backlog (model checking; after verification)

1. **`compact` at runtime:** apply it after every model-checker step, or only when fingerprinting?
2. **BMC initial worlds:** require a user-given concrete world first, and generate from `InitConditions` later?
3. **P6 itself:** explicit-state BMC, where `choose(n)` etc. take concrete values per successor (§6).
