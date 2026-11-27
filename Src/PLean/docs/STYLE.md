# PLean — Style Guide

Rules for PLean source, proofs, and tactics. Most are load-bearing:
a violation can silently waste solver time or introduce a soundness
gap. Each bullet says *when* the rule applies, not just what it is.

## Don'ts

- **Don't over-comment.** Default to no comments. Add one only when
  the *why* is non-obvious (a hidden invariant, a workaround, a
  soundness consequence). Well-named identifiers explain *what*;
  don't restate them.
- **Don't reference phase numbers or plan-doc section IDs in source
  comments.** Plan docs evolve, source comments don't get re-numbered.
  If a comment needs to justify a design decision, state the reason
  directly. References to `STATUS.md` are fine when pinning a
  named-bug-fix, but only when the fix is still load-bearing.
- **Don't dump deliberation into comments.** No "I tried X but it
  didn't work because …", no decision logs, no exploratory narration.
  Document the *invariant the code maintains*, not the path you took
  to find it.
- **Don't cite other codebases in source comments.** OK to mention a
  specific PVerifier construct or a Lean / crush API when the PLean
  piece exists to mirror or interface with it (e.g., "matches
  PVerifier's per-handler obligation shape"); not OK to gesture at
  "see paper X" or "similar to project Y". Citations belong in plan
  docs.
- **Don't re-run an example to recover output.** `#pverify` writes
  its full report — every obligation with its time, and the goals
  crush failed on — to `.lake/pverify/<Mod>.txt`. Read it, and use
  `pverify.only` / `pverify.profile` to iterate on single
  obligations; re-running a whole example is expensive (SMT solving
  dominates).
- **Don't leave probe code lying around.** When a probe (a temporary
  `example`, `#check`, `#eval`, or a one-off file under `Tests/`)
  has served its purpose, either delete it or convert it into a
  real regression that pins a behaviour worth keeping. Don't pin
  trivial facts that follow directly from the surrounding code —
  pins are load-bearing only when they catch a real regression
  class.
- **Don't introduce paths or repo URLs in source comments.** Describe
  a module by its role (e.g., "the obligation generator", "the
  kind-guard injection pass"), not its filesystem location. Paths
  rot when files move.

## Do's

- **Correctness first.** A soundness regression is far worse than a
  performance regression or a verbose proof. Two soundness guards
  are pinned (world-shadowing binders are rejected; a false
  invariant's theorem depends on `sorryAx`) by
  `Tests/Syntax/SoundnessRegression.lean` — don't regress them.
  Before adding a tactic that closes more goals, satisfy yourself
  it can't close *false* goals. Never hide a tactic's errors behind
  `try` when the error could be a bug (an unresolved name in a simp
  set fails the whole call).
- **Decompose deliberately.** When a module crosses ~500 lines or
  serves more than one cohesive purpose, split it — but only when
  the split *reduces* coupling. A new sub-module that re-exports
  the same surface with extra import overhead is worse than the
  original. Match the decomposition to the existing
  `PLean/{Syntax,Commands,Verify,Semantics,Internal}/` axes where
  it fits.
- **Prefer invariants to proofs.** An obligation that does not close
  usually needs a stronger inductive invariant, not a manual proof;
  see [`ProofSkill.md`](ProofSkill.md).
- **Tag new state-update helpers with `@[pverifySimp]`.** The
  normalisation before crush reduces them; an untagged helper
  reaches the solver as an opaque symbol.
