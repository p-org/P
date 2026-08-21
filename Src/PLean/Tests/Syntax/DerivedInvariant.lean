/-
Pins the `prove <X> from <premises> via <thm>;` (derived-invariant) form.

Four probes:
1. A sound derivation discharges via the cited theorem, and the derived
   target gets NO per-handler VCs and NO base case (the count is the
   evidence).
2. A derivation whose premise is never `prove`d is reported as a missing
   premise — the derived target inherits the same soundness guard as a
   `using` citation, since its invariance rests on the premise's.
3. A `via` theorem that doesn't prove the stated implication fails the
   obligation rather than being trusted on name alone.
4. A sorried `via` theorem fails too.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

/-! ## Probe 1 — a sound derivation, premises proven -/

pmodule DerivedOk

  system s

  event ePing : PLean.MachineRef
  event ePong

  machine Server {
    start state Idle {
      on ePing (replyTo : PLean.MachineRef) {
        send replyTo, ePong
      }
    }
  }

  machine Client {
    start state Booting {
      ignore ePong
    }
  }

  -- Inductive: every sent label's count is below the live counter.
  Lemma counter_bound {
    invariant inc_count :
      ∀ a : Sig.Label, s.sent a = true → a.actionCount < s.actionCount
  }

  -- NOT separately inductive as stated (the handler grows `sent`), but
  -- pointwise implied by `counter_bound` — the shape `derive` is for.
  Lemma counter_ne {
    invariant ne_count :
      ∀ a : Sig.Label, s.sent a = true → a.actionCount ≠ s.actionCount
  }

  Proof Chain {
    prove counter_bound ;
    prove counter_ne from counter_bound via ne_of_bound ;
  }

end DerivedOk

#gen_module DerivedOk

namespace DerivedOk

/-- The pointwise implication: `<` gives `≠`. -/
theorem ne_of_bound (s : GlobalState Sig)
    (h : counter_bound s) : counter_ne s := by
  intro a ha
  exact Nat.ne_of_lt (h a ha)

end DerivedOk

-- The derived target contributes exactly ONE obligation (its
-- implication VC) — no per-handler consecution, no base case. So the
-- tally is `counter_bound`'s 1 base + 1 handler by SMT, plus the
-- 1 user-proved derivation.
/--
info: DerivedOk: well-formed (0 types, 2 events, 2 machines, 2 invariants, 0 axioms, 0 instances)
---
info: obligation DerivedOk.derive_Chain_counter_ne derived via `DerivedOk.ne_of_bound` (type checked)
---
info: DerivedOk: 3 obligations from 2 prove-directives
---
info: ── passed ──
  ✓ DerivedOk.base_Chain_inc_count  [SMT]
  ✓ DerivedOk.Server.Idle.ePing_correct_Chain_counter_bound  [SMT]
  ✓ DerivedOk.derive_Chain_counter_ne  [manual]
---
info: DerivedOk: 2 proved by SMT, 1 user-proved, 0 disproved, 0 unknown, 0 tactic-error, 0 no-diagnostic, 0 missing-premise
-/
#guard_msgs (info, drop warning) in
#pverify DerivedOk

/-! ## Probe 2 — derived from a premise nothing proves

`counter_bound` is declared and cited, but never `prove`d. The derived
target's invariance rests on it, so the missing-premise guard must fire
exactly as it does for a `using` citation. -/

pmodule DerivedUnprovenPremise

  system s

  event ePing : PLean.MachineRef
  event ePong

  machine Server {
    start state Idle {
      on ePing (replyTo : PLean.MachineRef) {
        send replyTo, ePong
      }
    }
  }

  machine Client {
    start state Booting {
      ignore ePong
    }
  }

  Lemma counter_bound {
    invariant inc_count2 :
      ∀ a : Sig.Label, s.sent a = true → a.actionCount < s.actionCount
  }

  Lemma counter_ne {
    invariant ne_count2 :
      ∀ a : Sig.Label, s.sent a = true → a.actionCount ≠ s.actionCount
  }

  Proof Chain {
    prove counter_ne from counter_bound via ne_of_bound2 ;
  }

end DerivedUnprovenPremise

#gen_module DerivedUnprovenPremise

namespace DerivedUnprovenPremise

theorem ne_of_bound2 (s : GlobalState Sig)
    (h : counter_bound s) : counter_ne s := by
  intro a ha
  exact Nat.ne_of_lt (h a ha)

end DerivedUnprovenPremise

/--
warning: DerivedUnprovenPremise: 0 proved by SMT, 1 user-proved, 0 disproved, 0 unknown, 0 tactic-error, 0 no-diagnostic, 1 missing-premise
1 `using` premise(s) cite a lemma that no `prove` directive proves; add the missing `prove <lemma>;` directives.
-/
#guard_msgs (warning, drop info) in
set_option pverify.failOnIncomplete false in
#pverify DerivedUnprovenPremise

/-! ## Probe 3 — a `via` theorem that proves the wrong thing

SOUNDNESS GUARD. The derive VC states the implication explicitly and
discharges it by `exact @<thm>`, so a theorem that happens to have the
right *name* but the wrong statement must fail. Were the registry
name-keyed instead, this pmodule would verify a target nothing proves. -/

pmodule DerivedWrongVia

  system s

  event ePing : PLean.MachineRef
  event ePong

  machine Server {
    start state Idle {
      on ePing (replyTo : PLean.MachineRef) {
        send replyTo, ePong
      }
    }
  }

  machine Client {
    start state Booting {
      ignore ePong
    }
  }

  Lemma counter_bound {
    invariant inc_count3 :
      ∀ a : Sig.Label, s.sent a = true → a.actionCount < s.actionCount
  }

  Lemma counter_ne {
    invariant ne_count3 :
      ∀ a : Sig.Label, s.sent a = true → a.actionCount ≠ s.actionCount
  }

  Proof Chain {
    prove counter_bound ;
    prove counter_ne from counter_bound via wrong_via ;
  }

end DerivedWrongVia

#gen_module DerivedWrongVia

namespace DerivedWrongVia

/-- Proves the premise from itself — NOT the required implication. -/
theorem wrong_via (s : GlobalState Sig)
    (h : counter_bound s) : counter_bound s := h

end DerivedWrongVia

/--
warning: DerivedWrongVia: 2 proved by SMT, 0 user-proved, 0 disproved, 0 unknown, 1 tactic-error, 0 no-diagnostic, 0 missing-premise
1 obligation(s) need a manual proof; fill in the skeletons above.
---
warning: declaration uses 'sorry'
-/
#guard_msgs (warning, drop info) in
set_option pverify.failOnIncomplete false in
#pverify DerivedWrongVia

/-! ## Probe 4 — a sorried `via` theorem

SOUNDNESS GUARD. Same shape as the `@[pverifyProof]` sorry guard: the
implication type-checks, so only inspecting the *value* catches it. -/

pmodule DerivedSorryVia

  system s

  event ePing : PLean.MachineRef
  event ePong

  machine Server {
    start state Idle {
      on ePing (replyTo : PLean.MachineRef) {
        send replyTo, ePong
      }
    }
  }

  machine Client {
    start state Booting {
      ignore ePong
    }
  }

  Lemma counter_bound {
    invariant inc_count4 :
      ∀ a : Sig.Label, s.sent a = true → a.actionCount < s.actionCount
  }

  Lemma counter_ne {
    invariant ne_count4 :
      ∀ a : Sig.Label, s.sent a = true → a.actionCount ≠ s.actionCount
  }

  Proof Chain {
    prove counter_bound ;
    prove counter_ne from counter_bound via sorry_via ;
  }

end DerivedSorryVia

#gen_module DerivedSorryVia

namespace DerivedSorryVia

set_option linter.unusedVariables false in
theorem sorry_via (s : GlobalState Sig)
    (h : counter_bound s) : counter_ne s := by
  sorry

end DerivedSorryVia

/--
warning: DerivedSorryVia: 2 proved by SMT, 0 user-proved, 0 disproved, 0 unknown, 0 tactic-error, 1 no-diagnostic, 0 missing-premise
1 obligation(s) need a manual proof; fill in the skeletons above.
-/
#guard_msgs (warning, drop info) in
set_option pverify.failOnIncomplete false in
#pverify DerivedSorryVia
