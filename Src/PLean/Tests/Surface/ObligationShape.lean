/-
`#guard_msgs`-pinned obligation-shape regression. Records the exact
`theorem` types `#pverify` emits — precondition, postcondition,
dispatcher contract, theorem-name suffix scheme — so a refactor of
`emitOneObligation` cannot silently change the shape.

Two `Proof` blocks target the same lemma to exercise the proof-tag
disambiguation embedded in the theorem name.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule ObligationShapeMod

  event eHello

  machine M {
    start state S {
      on eHello { pure () }
    }
  }

  Theorem safety {
    invariant always_true : True
  }

  Proof Block1 {
    prove safety ;
  }

  Proof Block2 {
    prove default ;
  }

end ObligationShapeMod

#gen_module ObligationShapeMod
#pverify    ObligationShapeMod

namespace ObligationShapeMod

/-! ## Pinned obligation-shape signatures -/

-- Non-default obligation: `DefaultInvariants` appears in NEITHER pre nor
-- post (the sanity invariants are a separate well-formedness bundle,
-- proven only by `prove default`; see `emitOneObligation`). Pre carries
-- the target lemma + dispatcher contract only; post carries the target.
/--
info: M.S.eHello_correct_Block1_safety : ∀ (this : M),
  triple
    (fun s ↦
      (safety s ∧ True) ∧
        ∃ lbl,
          inflight lbl s ∧
            lbl.target = this.ref ∧
              (s.machines this.ref).currentState = M.S_st ∧ lbl.action = EventOrGoto.event E.eHello)
    (M.S.eHello_handler this) fun x s ↦ safety s ∧ True
-/
#guard_msgs in
#check @M.S.eHello_correct_Block1_safety

-- Default obligation: the target IS `DefaultInvariants`, so it appears
-- once in pre and once in post (assumed-and-checked), with no separate
-- duplicate conjunct.
/--
info: M.S.eHello_correct_Block2_default : ∀ (this : M),
  triple
    (fun s ↦
      (DefaultInvariants s ∧ True) ∧
        ∃ lbl,
          inflight lbl s ∧
            lbl.target = this.ref ∧
              (s.machines this.ref).currentState = M.S_st ∧ lbl.action = EventOrGoto.event E.eHello)
    (M.S.eHello_handler this) fun x s ↦ DefaultInvariants s ∧ True
-/
#guard_msgs in
#check @M.S.eHello_correct_Block2_default

-- Pin a base-case obligation's emitted shape too. Base-case VCs are
-- pmodule-scoped (one per individual invariant in the directive's
-- target lemma), not handler-scoped — so they live directly under
-- `<Mod>.`, not `<Mod>.<M>.`. `#check` strips the open namespace when
-- pretty-printing, so the expected message uses the short name.
/--
info: base_Block1_always_true : ∀ (s : GlobalState Sig), InitConditions s → always_true s
-/
#guard_msgs in
#check @base_Block1_always_true

end ObligationShapeMod
