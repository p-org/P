/-
PLean.Semantics.World — the system-level transition semantics (Verdi's
network semantics).

A `World` holds every machine's state and the packets in flight. A step
either delivers one in-flight packet to its destination or runs one
machine's pending control continuation (`goto` entry / `raise`). The
handler runs against the destination's local state; `commit` applies its
outputs to the world. `GWorld` adds the ghost log of every packet ever
sent, which invariants about message history refer to.
-/
import PLean.Semantics.Handler

namespace PLean

variable {S E F G W : Type}

/-- The real world: what execution and model checking store. -/
structure World (S E F G W : Type) where
  machines : MachineRef → MachineState S E F G W
  /-- References `≥ nextRef` are unallocated, and so is `0` (`null`). -/
  nextRef  : Nat
  /-- In-flight packets by id; ids record send order. -/
  net      : Nat → Option (Packet E)
  /-- The next packet id. -/
  clock    : Nat
  error    : Option PError

/-- The ghost world: the real world plus the log of every packet ever sent. -/
structure GWorld (S E F G W : Type) extends World S E F G W where
  log : Nat → Option (Packet E)

/-- A program: its handlers, entries, `receive` resumers and deferral sets,
indexed by state tag (a state tag determines its machine kind). -/
structure Program (S E F G W : Type) where
  /-- Handler of event `e` in state `s`; `none` = unhandled. `ignore`d events map to
  `some (pure ())`. -/
  handler  : S → E → Option (HandlerM S E F G W Unit)
  /-- Entry of state `s` with argument `arg` (`pure ()` for a state without one). -/
  entry    : S → G → HandlerM S E F G W Unit
  /-- Resumer of a suspended `receive` for event `e`; `none` = not one of its cases. -/
  resume   : W → E → Option (HandlerM S E F G W Unit)
  /-- Events state `s` defers. -/
  deferred : S → E → Bool

/-- Network semantics: `bag` delivers any in-flight packet (PVerifier, Verdi's
reordering semantics); `fifo` delivers each machine's oldest acceptable packet
(P's semantics). -/
inductive NetSem where
  | bag
  | fifo
  deriving DecidableEq, Repr

/-- A scheduling choice. -/
inductive Choice where
  /-- Deliver in-flight packet `i`. -/
  | deliver (i : Nat)
  /-- Run machine `m`'s pending entry. -/
  | control (m : MachineRef)
  deriving DecidableEq, Repr

namespace Program

variable (prog : Program S E F G W)

/-- Whether a machine in local state `ms` accepts event `e` now. A halted
machine accepts (and drops) everything; a suspended `receive` accepts
exactly its cases; otherwise everything not deferred. -/
def accepts (ms : MachineState S E F G W) (e : E) : Bool :=
  ms.halted ||
    match ms.waiting with
    | some k => (prog.resume k e).isSome
    | none =>
      match ms.state with
      | some s => !prog.deferred s e
      | none => false

/-- Whether in-flight packet `i` can be delivered (bag semantics). -/
def deliverable (w : World S E F G W) (i : Nat) : Bool :=
  match w.net i with
  | none => false
  | some p =>
    let ms := w.machines p.dst
    ms.state.isSome && ms.pending.isNone && prog.accepts ms p.ev

/-- FIFO: no older deliverable packet to the same destination. -/
def oldest (w : World S E F G W) (i : Nat) : Bool :=
  match w.net i with
  | none => false
  | some p =>
    (List.range i).all fun j =>
      match w.net j with
      | some q => !(decide (q.dst = p.dst) && prog.deliverable w j)
      | none => true

/-- Whether choice `c` is enabled in `w` under `sem`. -/
def enabled (sem : NetSem) (w : World S E F G W) : Choice → Bool
  | .deliver i =>
    w.error.isNone && prog.deliverable w i && (sem == .bag || prog.oldest w i)
  | .control m =>
    let ms := w.machines m
    w.error.isNone && ms.state.isSome && ms.pending.isSome && !ms.halted

end Program

/-! ## Running a handler and committing its effects -/

/-- The handler state for machine `m` running with local state `ms`. -/
def HState.start (w : World S E F G W) (m : MachineRef) (ms : MachineState S E F G W)
    (coins : Nat → Nat) : HState S E F G W :=
  { ref := m, ms := ms, out := .startingAt w.clock, spawn := .startingAt w.nextRef, obs := .empty,
    coins := coins, used := 0 }

section
variable [Inhabited F]

/-- Apply a finished handler of machine `m` to the world (Verdi's DELIVER
conclusion): write back the machine, allocate spawned machines, put sends
in flight. -/
def commit (w : World S E F G W) (m : MachineRef) (hs : HState S E F G W) : World S E F G W :=
  { machines := fun r =>
      if r = m then hs.ms
      else if w.nextRef ≤ r ∧ r < hs.spawn.size then (hs.spawn.get r).getD default
      else w.machines r
    nextRef := max w.nextRef hs.spawn.size
    net := fun j => if w.clock ≤ j ∧ j < hs.out.size then hs.out.get j else w.net j
    clock := max w.clock hs.out.size
    error := w.error }

/-- The ghost log after a handler with final state `hs` committed on a world
with packet clock `clock`. -/
def logAfter (log : Nat → Option (Packet E)) (clock : Nat) (hs : HState S E F G W) :
    Nat → Option (Packet E) :=
  fun j => if clock ≤ j ∧ j < hs.out.size then hs.out.get j else log j

/-- `commit`, also extending the ghost log. -/
def gcommit (g : GWorld S E F G W) (m : MachineRef) (hs : HState S E F G W) : GWorld S E F G W :=
  { commit g.toWorld m hs with log := logAfter g.log g.clock hs }

/-- Remove packet `i` from flight. -/
def World.consume (w : World S E F G W) (i : Nat) : World S E F G W :=
  { w with net := fun j => if j = i then none else w.net j }

/-- Remove packet `i` from flight (the ghost log keeps it). -/
def GWorld.consume (g : GWorld S E F G W) (i : Nat) : GWorld S E F G W :=
  { g with net := fun j => if j = i then none else g.net j }

/-- Run `act` as machine `m` with local state `ms`, and commit the result.
Also returns the handler's final state, for the ghost log. -/
def World.run (w : World S E F G W) (m : MachineRef) (ms : MachineState S E F G W)
    (coins : Nat → Nat) (act : HandlerM S E F G W Unit) :
    World S E F G W × Option (HState S E F G W) :=
  match act.run (HState.start w m ms coins) with
  | .ok ((), hs) => (commit w m hs, some hs)
  | .error e => ({ w with error := some e }, none)

/-- One step: the next world, and the committed handler state, if a handler
ran to completion. -/
def stepOut (prog : Program S E F G W) (w : World S E F G W) (c : Choice) (coins : Nat → Nat) :
    World S E F G W × Option (HState S E F G W) :=
  match c with
  | .deliver i =>
    match w.net i with
    | none => (w, none)
    | some p =>
      let w0 := w.consume i
      let ms := w.machines p.dst
      if ms.halted then (w0, none)
      else
        match ms.waiting with
        | some k =>
          match prog.resume k p.ev with
          | some act => w0.run p.dst { ms with waiting := none } coins act
          | none => ({ w0 with error := some .unhandled }, none)
        | none =>
          match ms.state with
          | some s =>
            match prog.handler s p.ev with
            | some act => w0.run p.dst ms coins act
            | none => ({ w0 with error := some .unhandled }, none)
          | none => (w0, none)
  | .control m =>
    let ms := w.machines m
    match ms.pending, ms.state with
    | some arg, some s => w.run m { ms with pending := none } coins (prog.entry s arg)
    | _, _ => (w, none)

/-- One step of the real world. -/
def step (prog : Program S E F G W) (w : World S E F G W) (c : Choice) (coins : Nat → Nat) :
    World S E F G W :=
  (stepOut prog w c coins).1

/-- One step of the ghost world: the real step plus the log update. -/
def gstep (prog : Program S E F G W) (g : GWorld S E F G W) (c : Choice) (coins : Nat → Nat) :
    GWorld S E F G W :=
  let r := stepOut prog g.toWorld c coins
  { r.1 with log := match r.2 with
                    | some hs => logAfter g.log g.clock hs
                    | none => g.log }

/-! ## Reachability -/

/-- Worlds reachable from `Init` under `sem`. -/
inductive Reachable (prog : Program S E F G W) (sem : NetSem) (Init : World S E F G W → Prop) : World S E F G W → Prop
  | init {w} : Init w → Reachable prog sem Init w
  | step {w c coins} : Reachable prog sem Init w → prog.enabled sem w c = true →
      Reachable prog sem Init (step prog w c coins)

/-- Ghost worlds reachable from `Init` under `sem`. -/
inductive GReachable (prog : Program S E F G W) (sem : NetSem) (Init : GWorld S E F G W → Prop) :
    GWorld S E F G W → Prop
  | init {g} : Init g → GReachable prog sem Init g
  | step {g c coins} : GReachable prog sem Init g → prog.enabled sem g.toWorld c = true →
      GReachable prog sem Init (gstep prog g c coins)

end

/-! ## Framework invariants -/

/-- Facts every reachable ghost world satisfies, proved once for all
programs (`framework_gstep`). Reference `0` (`null`) is never allocated. -/
def Framework (g : GWorld S E F G W) : Prop :=
  (∀ j, g.clock ≤ j → g.net j = none) ∧
  (∀ j, g.clock ≤ j → g.log j = none) ∧
  (∀ j p, g.net j = some p → g.log j = some p) ∧
  (∀ r, g.nextRef ≤ r → (g.machines r).state = none) ∧
  0 < g.nextRef ∧ (g.machines MachineRef.null).state = none

end PLean
