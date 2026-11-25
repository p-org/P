/-
PLean.Semantics.Msg — what an event binder in an invariant ranges over.

`∀ e : eVote, …` quantifies over messages: a packet together with its id
(its position in the ghost log). The P predicates on events are then
statements about the ghost world:

  `inflight e s`  — `e` is in flight
  `sent e s`      — `e` was sent
  `received e s`  — `e` was sent and has been delivered
  `a ≺ b`         — `a` was sent before `b`
  `e targets r`   — `e` is addressed to `r`
-/
import PLean.Semantics.World
import PLean.Verify.SimpAttrs

namespace PLean

/-- A sent packet with its id. -/
structure Msg (E : Type) where
  id     : Nat
  src    : MachineRef
  target : MachineRef
  ev     : E

variable {S E F G W : Type}

/-- The packet a message denotes. -/
@[pverifySimp] def Msg.packet (m : Msg E) : Packet E := ⟨m.src, m.target, m.ev⟩

/-- `m` is in flight. -/
@[pverifySimp] def inflight (m : Msg E) (s : GWorld S E F G W) : Prop :=
  s.net m.id = some m.packet

/-- `m` was sent. -/
@[pverifySimp] def sent (m : Msg E) (s : GWorld S E F G W) : Prop :=
  s.log m.id = some m.packet

/-- `m` was sent and has been delivered. -/
@[pverifySimp] def received (m : Msg E) (s : GWorld S E F G W) : Prop :=
  s.log m.id = some m.packet ∧ s.net m.id = none

/-- The state of machine `r` (`none` when unallocated). -/
@[pverifySimp] def stateOf (r : MachineRef) (s : GWorld S E F G W) : Option S := (s.machines r).state

/-- Every message a handler sent between handler states `hs0` and `hs`
satisfies `P`. -/
def sentSince (hs0 hs : HState S E F G W) (P : Msg E → Prop) : Prop :=
  ∀ j p, hs0.out.size ≤ j → j < hs.out.size → hs.out.get j = some p → P ⟨j, p.src, p.dst, p.ev⟩

/-! A loop's sends, one at a time (applied before `sentSince` is unfolded). -/

@[pverifySimp] theorem sentSince_self (hs : HState S E F G W) (P : Msg E → Prop) :
    sentSince hs hs P ↔ True :=
  ⟨fun _ => trivial, fun _ j _ h1 h2 => absurd (Nat.lt_of_lt_of_le h2 h1) (Nat.lt_irrefl _)⟩

@[pverifySimp] theorem sentSince_push (hs0 : HState S E F G W) (r : MachineRef)
    (ms : MachineState S E F G W) (out : Log (Packet E)) (p : Packet E)
    (sp : Log (MachineState S E F G W)) (ob : Log E) (c : Nat → Nat) (u : Nat) (P : Msg E → Prop) :
    sentSince hs0 ⟨r, ms, out.push p, sp, ob, c, u⟩ P ↔
      sentSince hs0 ⟨r, ms, out, sp, ob, c, u⟩ P ∧
        (hs0.out.size ≤ out.size → P ⟨out.size, p.src, p.dst, p.ev⟩) := by
  simp only [sentSince, Log.push_size, Log.push_get]
  constructor
  · intro h
    refine ⟨fun j q h1 h2 h3 => ?_, fun h1 => h out.size p h1 (Nat.lt_succ_self _) (by simp)⟩
    have hne : j ≠ out.size := Nat.ne_of_lt h2
    exact h j q h1 (Nat.lt_succ_of_lt h2) (by simp [hne, h3])
  · intro ⟨h, hnew⟩ j q h1 h2 h3
    by_cases hj : j = out.size
    · subst hj; simp at h3; subst h3; exact hnew h1
    · simp [hj] at h3
      exact h j q h1 (Nat.lt_of_le_of_ne (Nat.le_of_lt_succ h2) hj) h3

/-- `a` was sent before `b`. -/
@[pverifySimp] def precedes (a b : Msg E) : Prop := a.id < b.id

/-- `m` is addressed to `r`. -/
@[pverifySimp] def Msg.targets? (m : Msg E) (r : MachineRef) : Prop := m.target = r

/-! Normalisation for obligations: quantifiers over messages range over their
fields, so the solver sees first-order packets. -/

@[pverifySimp] theorem Msg.forall_iff (P : Msg E → Prop) :
    (∀ m, P m) ↔ ∀ id src target ev, P ⟨id, src, target, ev⟩ :=
  ⟨fun h _ _ _ _ => h _, fun h m => h m.id m.src m.target m.ev⟩

@[pverifySimp] theorem Msg.exists_iff (P : Msg E → Prop) :
    (∃ m, P m) ↔ ∃ id src target ev, P ⟨id, src, target, ev⟩ :=
  ⟨fun ⟨m, h⟩ => ⟨m.id, m.src, m.target, m.ev, h⟩, fun ⟨a, b, c, d, h⟩ => ⟨_, h⟩⟩

attribute [pverifySimp] Msg.mk.injEq Packet.mk.injEq Option.some.injEq
  forall_exists_index and_imp exists_prop forall_eq forall_eq' exists_eq_left exists_eq_right
  exists_eq_left' exists_eq_right' forall_apply_eq_imp_iff₂

/-- An event-kind guard `ev = E.<k> p` under `∀ ev p` substitutes the
constructor (`∀ e : eV, …` after the guard is split out). -/
@[pverifySimp] theorem forall_eq_ctor_imp {α β : Type} {f : β → α} {P : α → β → Prop} :
    (∀ a b, a = f b → P a b) ↔ ∀ b, P (f b) b :=
  ⟨fun h b => h _ b rfl, fun h _ b e => e ▸ h b⟩

/-- The `∃ e : eV, …` counterpart. -/
@[pverifySimp] theorem exists_eq_ctor_and {α β : Type} {f : β → α} {P : α → Prop} :
    (∃ a, (∃ b, a = f b) ∧ P a) ↔ ∃ b, P (f b) :=
  ⟨fun ⟨_, ⟨b, e⟩, h⟩ => ⟨b, e ▸ h⟩, fun ⟨b, h⟩ => ⟨_, ⟨b, rfl⟩, h⟩⟩

/-- `a ≺ b`: `a` was sent before `b`. -/
scoped notation:50 a " ≺ " b => PLean.precedes a b

/-- `m targets r`. -/
scoped notation:50 m " targets " r => PLean.Msg.targets? m r

/-- `m is ev`: the message carries some instance of event `ev`. Expands to
the per-event predicate `is_<ev>` that `#gen_module` emits. -/
scoped syntax:50 (name := pIsTermMacro) term:51 " is " ident : term

scoped macro_rules
  | `($m is $ev:ident) => do
    let predIdent := Lean.mkIdentFrom ev (Lean.Name.mkSimple ("is_" ++ ev.getId.toString))
    `(($predIdent $m))

end PLean
