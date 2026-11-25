/-
PLean.Semantics.Handler — the handler monad (Verdi's state+writer
handler monad) and P's statement primitives.

A handler runs against its own machine's state only. It appends sends,
spawned machines and announcements to logs, which `commit` applies to
the world afterwards. Errors (failed `assert`, missing map key, …) abort
the handler through `Except`.
-/
import Std.WP
import PLean.Semantics.Core

set_option experimental.vcgen true

namespace PLean

open Std.WP

/-- Handler-local state. -/
structure HState (S E F G W : Type) where
  /-- `this` (read-only). -/
  ref   : MachineRef
  /-- The running machine's state. -/
  ms    : MachineState S E F G W
  /-- Sends, at their final packet ids (the log starts at the world's clock). -/
  out   : Log (Packet E)
  /-- Machines created by `new`, at their final references (the log starts at
  the world's first unallocated reference). -/
  spawn : Log (MachineState S E F G W)
  /-- `announce`d events. -/
  obs   : Log E
  /-- Nondeterministic choices. -/
  coins : Nat → Nat
  /-- Number of coins consumed. -/
  used  : Nat

/-- The handler monad. -/
abbrev HandlerM (S E F G W : Type) := StateT (HState S E F G W) (Except PError)

/-- The handler state after consuming one nondeterministic choice. -/
@[inline] def HState.tick {S E F G W : Type} (hs : HState S E F G W) : HState S E F G W :=
  { hs with used := hs.used + 1 }

/-- The machine state after `goto s, arg`: `s`'s entry runs next. -/
@[inline] def MachineState.gotoState {S E F G W : Type} (ms : MachineState S E F G W) (s : S)
    (arg : G) : MachineState S E F G W :=
  { ms with state := some s, pending := some arg }

/-- What a loop body leaves unchanged: the loop invariant conjunct that
`foreach` / `while` add automatically (`hs0` is the handler's start state).
Earlier sends are kept, and the machine state / outbox / spawned machines /
announcements are unchanged when the body cannot touch them. -/
@[reducible] def HState.frame {S E F G W : Type} (hs0 hs : HState S E F G W)
    (msFixed outFixed spawnFixed obsFixed : Bool) : Prop :=
  hs.ref = hs0.ref ∧ hs0.out.size ≤ hs.out.size ∧
  (∀ j, j < hs0.out.size → hs.out.get j = hs0.out.get j) ∧
  (msFixed = true → hs.ms = hs0.ms) ∧ (outFixed = true → hs.out = hs0.out) ∧
  (spawnFixed = true → hs.spawn = hs0.spawn) ∧ (obsFixed = true → hs.obs = hs0.obs)

namespace H

variable {S E F G W : Type}

/-- `this`. -/
@[inline] def self : HandlerM S E F G W MachineRef := return (← get).ref

/-- `send t, e`. -/
@[inline] def send (t : MachineRef) (e : E) : HandlerM S E F G W Unit :=
  modify fun hs => { hs with out := hs.out.push ⟨hs.ref, t, e⟩ }

/-- `announce e`. -/
@[inline] def announce (e : E) : HandlerM S E F G W Unit :=
  modify fun hs => { hs with obs := hs.obs.push e }

/-- `new M(x)`: allocate the next free reference. -/
@[inline] def new (m : MachineState S E F G W) : HandlerM S E F G W MachineRef := do
  let hs ← get
  set { hs with spawn := hs.spawn.push m }
  return hs.spawn.size

/-- `goto s, arg` (the caller returns right after); `s`'s entry runs next. -/
@[inline] def goto (s : S) (arg : G) : HandlerM S E F G W Unit :=
  modify fun hs => { hs with ms := hs.ms.gotoState s arg }

/-- `raise halt` (the caller returns right after). -/
@[inline] def halt : HandlerM S E F G W Unit :=
  modify fun hs => { hs with ms := { hs.ms with halted := true } }

/-- Suspend in a `receive`; `k` names the site and carries the saved
locals (the caller returns right after). -/
@[inline] def suspend (k : W) : HandlerM S E F G W Unit :=
  modify fun hs => { hs with ms := { hs.ms with waiting := some k } }

/-- Consume the next nondeterministic choice. -/
@[inline] def coin : HandlerM S E F G W Nat := do
  let hs ← get
  set { hs with used := hs.used + 1 }
  return hs.coins hs.used

/-- `$` / `$$`. -/
@[inline] def flip : HandlerM S E F G W Bool := do
  return (← coin) % 2 == 0

/-- `choose(n)`: a value in `[0, n]`. -/
@[inline] def chooseInt (n : Int) : HandlerM S E F G W Int := do
  if n < 0 then throw .badChoose
  return ((← coin) % (n.toNat + 1) : Nat)

/-- `choose(s)`: a member of `s`. -/
@[inline] def chooseSet {T : Type} [DecidableEq T] (s : PSet T) : HandlerM S E F G W T := do
  if h : s.support.length = 0 then throw .emptyChoose
  else
    let c ← coin
    return s.support[c % s.support.length]'(Nat.mod_lt _ (Nat.pos_of_ne_zero h))

/-- `assert c`. -/
@[inline] def passert (c : Bool) (id : Nat) : HandlerM S E F G W Unit :=
  if c then pure () else throw (.assertFail id)

/-- `m[k]` (read). -/
@[inline] def mget {K V : Type} [DecidableEq K] (m : PMap K V) (k : K) : HandlerM S E F G W V :=
  match m.get k with
  | some v => pure v
  | none => throw .keyNotFound

/-- `m += (k, v)`: insert a new key. -/
@[inline] def minsert {K V : Type} [DecidableEq K] (m : PMap K V) (k : K) (v : V) :
    HandlerM S E F G W (PMap K V) :=
  if m.has k then throw .duplicateKey else pure (m.put k v)

/-! ## `vcgen` specifications

`vcgen` does not unfold definitions, so every primitive carries a spec.
The nondeterministic ones quantify over all outcomes. -/

section Specs

variable {α : Type} {post : α → HState S E F G W → Prop} {epost : PError → Prop}

@[spec] theorem self_spec {post : MachineRef → HState S E F G W → Prop} :
    Triple (self ) (fun hs => post hs.ref hs) post epost := by
  unfold self; vcgen; all_goals assumption

@[spec] theorem send_spec {t : MachineRef} {e : E} {post : Unit → HState S E F G W → Prop} :
    Triple (send t e) (fun hs => post () { hs with out := hs.out.push ⟨hs.ref, t, e⟩ }) post epost := by
  unfold send; vcgen; all_goals assumption

@[spec] theorem announce_spec {e : E} {post : Unit → HState S E F G W → Prop} :
    Triple (announce e) (fun hs => post () { hs with obs := hs.obs.push e }) post epost := by
  unfold announce; vcgen; all_goals assumption

@[spec] theorem new_spec {m : MachineState S E F G W} {post : MachineRef → HState S E F G W → Prop} :
    Triple (new m)
      (fun hs => post hs.spawn.size { hs with spawn := hs.spawn.push m }) post epost := by
  unfold new; vcgen; all_goals assumption

@[spec] theorem goto_spec {s : S} {arg : G} {post : Unit → HState S E F G W → Prop} :
    Triple (goto (W := W) (F := F) (E := E) s arg)
      (fun hs => post () { hs with ms := hs.ms.gotoState s arg }) post epost := by
  unfold goto; vcgen; all_goals assumption

@[spec] theorem halt_spec {post : Unit → HState S E F G W → Prop} :
    Triple (halt )
      (fun hs => post () { hs with ms := { hs.ms with halted := true } }) post epost := by
  unfold halt; vcgen; all_goals assumption

@[spec] theorem suspend_spec {k : W} {post : Unit → HState S E F G W → Prop} :
    Triple (suspend k)
      (fun hs => post () { hs with ms := { hs.ms with waiting := some k } }) post epost := by
  unfold suspend; vcgen; all_goals assumption

@[spec] theorem flip_spec {post : Bool → HState S E F G W → Prop} :
    Triple (flip )
      (fun hs => post true hs.tick ∧ post false hs.tick) post epost := by
  unfold flip coin
  vcgen
  rename_i h
  cases (hs.coins hs.used % 2 == 0)
  · exact h.2
  · exact h.1

@[spec] theorem chooseInt_spec {n : Int} {post : Int → HState S E F G W → Prop} :
    Triple (chooseInt  n)
      (fun hs => (n < 0 → epost .badChoose) ∧
                 (0 ≤ n → ∀ i : Int, 0 ≤ i → i ≤ n → post i hs.tick)) post epost := by
  unfold chooseInt coin
  vcgen
  case vc1 => rename_i h hn; exact h.1 hn
  case vc2 =>
    rename_i h hn
    have hlt := Nat.mod_lt (hs.coins hs.used) (Nat.succ_pos n.toNat)
    generalize hs.coins hs.used % (n.toNat + 1) = c at hlt ⊢
    have h0 : (0 : Int) ≤ (c : Int) := by omega
    have h1 : (c : Int) ≤ n := by omega
    exact h.2 (by omega) _ h0 h1

@[spec] theorem chooseSet_spec {T : Type} [DecidableEq T] {s : PSet T} {post : T → HState S E F G W → Prop} :
    Triple (chooseSet  s)
      (fun hs => (s.card = 0 → epost .emptyChoose) ∧
                 (∀ x, s.mem x = true → post x hs.tick)) post epost := by
  unfold chooseSet coin
  vcgen
  case vc1 => rename_i h hlen; exact h.1 hlen
  case vc2 =>
    rename_i h hlen
    exact h.2 _ ((PSet.mem_support s _).mp (List.getElem_mem _))

@[spec] theorem passert_spec {c : Bool} {id : Nat} {post : Unit → HState S E F G W → Prop} :
    Triple (passert  c id)
      (fun hs => (c = true → post () hs) ∧ (c = false → epost (.assertFail id))) post epost := by
  unfold passert
  vcgen
  all_goals simp_all

@[spec] theorem mget_spec {K V : Type} [DecidableEq K] {m : PMap K V} {k : K}
    {post : V → HState S E F G W → Prop} :
    Triple (mget  m k)
      (fun hs => (∀ v, m.get k = some v → post v hs) ∧ (m.get k = none → epost .keyNotFound))
      post epost := by
  unfold mget
  vcgen
  all_goals simp_all

@[spec] theorem minsert_spec {K V : Type} [DecidableEq K] {m : PMap K V} {k : K} {v : V}
    {post : PMap K V → HState S E F G W → Prop} :
    Triple (minsert  m k v)
      (fun hs => (m.has k = true → epost .duplicateKey) ∧ (m.has k = false → post (m.put k v) hs))
      post epost := by
  unfold minsert
  vcgen
  all_goals simp_all

end Specs

end H

end PLean
