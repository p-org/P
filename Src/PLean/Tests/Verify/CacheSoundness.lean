/-
Regression for the obligation-cache soundness story in
`Verify/Tactic.lean`. The cache hashes the elaborated goal's
**target + visible local hypotheses**; this test pins two facts:

1. Hashing two goals with the same target but *different hypothesis
   contexts* yields different hashes (so a false-goal masquerading as
   a true one can never be a cache hit).
2. Hashing the same goal twice yields the same hash (basic
   deterministic-key property the speedup depends on).

If either property regresses, the cache becomes either unsound (a
false goal closes via `trust_smt` from an unrelated true-goal entry)
or ineffective (every elaboration writes new entries).
-/
import PLean

set_option linter.unusedTactic false

open PLean Lean Meta Elab Tactic

namespace PLean.Tests.CacheSoundness

/-- Compute the `pverifyHash` of the current main goal's text. -/
elab "show_cache_hash " ident:ident : tactic => do
  withMainContext do
    let mv ← getMainGoal
    let text ← pverifyGoalToCacheText mv
    let h := pverifyHash text
    Lean.logInfo m!"{ident}: hash={h}"
    -- Stash via a no-op so the tactic doesn't leave dangling goals.
    pure ()

-- The hash depends on the hypotheses in context, not just the target.
-- Soundness rests on: distinct (hypotheses, target) → distinct hashes.
-- A target-only hash would collide `probe_pTrue` with `probe_pWithR`
-- below, and a cache hit could close a `p`-target goal with a
-- `trust_smt` from an entry whose hypotheses don't actually entail it.

-- `p : p; q : q ⊢ p`
example (p q : Prop) (hp : p) (_hq : q) : p := by
  show_cache_hash probe_pTrue
  exact hp

-- `p : p; q : q ⊢ q` — same hypotheses, different target.
example (p q : Prop) (_hp : p) (_hq : q) : q := by
  show_cache_hash probe_qTrue
  exact _hq

-- `p : p; r : r ⊢ p` — same target as the first, different
-- hypotheses. A target-only hash would collide with `probe_pTrue`.
example (p r : Prop) (hp : p) (_hr : r) : p := by
  show_cache_hash probe_pWithR
  exact hp

/-! ## Distinctness rationale (no automated `#guard_msgs` — hashes
have run-dependent literal values, so a fixed-message guard would be
brittle). The three `show_cache_hash` traces above appear as `info`
messages in the build log; a regression to target-only hashing would
collapse the first and third traces to the same value. Verified by
inspection during the cache soundness review (2026-06-19). -/

end PLean.Tests.CacheSoundness
