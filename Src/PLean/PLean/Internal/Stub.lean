/-
PLean.Internal.Stub — stub `PM` monad for Phase 0.

Phase 0 macros need *something* to elaborate handler bodies into so the Lean
elaborator type-checks them. The real semantics (`PM α := StateT GlobalState
(NonDetT DivM) α` plus actual buffer/state updates) lands in Phase 1; this
file is replaced wholesale at that point.

Until then, `PM α := Id α` and every primitive is a no-op. This is enough
for `#pwf` to walk the registered metadata.
-/
namespace PLean.Stub

abbrev PM (α : Type) : Type := Id α

abbrev MachineRef : Type := Nat
abbrev EventTag   : Type := Nat
abbrev StateTag   : Type := Nat

-- Primitives mirroring PVerifier's send/raise/goto/new/announce/assign.
-- All no-ops until Phase 1.

/-- `send target evt payload` — Phase 0 stub.

The target is intentionally polymorphic: in Phase 0 a machine `var server :
BankServer` resolves to a value of type `BankServer`, not a `MachineRef`,
because Phase 0 doesn't yet model machine references. Phase 1 will tighten
the signature (and emit a coercion from `BankServer` to `MachineRef`). -/
@[inline] def send {τ : Type} {α : Type} (_target : τ) (_evt : EventTag)
    (_payload : α) : PM Unit := pure ()

/-- `raise evt payload` — Phase 0 stub. -/
@[inline] def raise {α : Type} (_evt : EventTag) (_payload : α) : PM Unit :=
  pure ()

/-- `goto stateName payload?` — Phase 0 stub. -/
@[inline] def goto {α : Type} (_st : StateTag) (_payload : α) : PM Unit :=
  pure ()

/-- `new MachineKind args` — Phase 0 stub. Returns a placeholder ref. -/
@[inline] def new {α : Type} (_machineTag : Nat) (_args : α) : PM MachineRef :=
  pure 0

/-- `announce evt payload` — Phase 0 stub. -/
@[inline] def announce {α : Type} (_evt : EventTag) (_payload : α) : PM Unit :=
  pure ()

end PLean.Stub
