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
  specific PVerifier construct or a Loom API when the PLean piece
  exists to mirror or interface with it (e.g., "matches PVerifier's
  per-handler obligation shape"); not OK to gesture at "see paper X"
  or "similar to project Y". Citations belong in plan docs.
- **Don't trim `lake build` output on `#pverify` files.** A failing
  verification produces a structured report — failing-obligation
  names, copy-paste manual-proof skeletons, per-stage profile —
  *after* the `✖` line. Re-running the build to recover output is
  expensive (SMT solving dominates), so always read the full output
  the first time. Pipe to a file (`lake build Examples.Foo 2>&1 |
  tee build.log`) if it's long, and grep over the file rather than
  re-running.
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
  are pinned (`GlobalState`-shadowed binders, sorried
  `@[pverifyProof]` failure) by
  `Tests/Syntax/SoundnessRegression.lean` — don't regress them.
  Before adding a tactic that closes more goals, satisfy yourself
  it can't close *false* goals.
- **Decompose deliberately.** When a module crosses ~500 lines or
  serves more than one cohesive purpose, split it — but only when
  the split *reduces* coupling. A new sub-module that re-exports
  the same surface with extra import overhead is worse than the
  original. Match the decomposition to the existing
  `PLean/{Syntax,Commands,Verify,Semantics,Internal}/` axes where
  it fits.
- **Read [`ProofSkill.md`](ProofSkill.md) before writing manual
  proofs.** It covers the one-step-at-a-time workflow, the
  higher-order-rejection workaround, bundle-sizing for SMT, and
  the `using`-chain pattern — most novel proofs hit one of these.
- **Lean on `Verify/Tactic.lean` and extend it.** When a manual
  proof uses the same shape twice — `pverify_carry_after_recv`,
  `pverify_not_inflight[_by]`, `pverify_inflight_by`,
  `pverify_machine_has_type`, `pverify_split_smt` — that's the
  catalogue of "common pattern → atomic tactic". Before
  duplicating a proof shape, check whether a tactic already covers
  it; if a *new* recurring shape shows up, add it. The docstrings
  on each tactic spell out the calling pattern.
- **Shorten proofs after they close.** Once an obligation is green,
  cross-check it against neighbouring proofs. If the same
  boilerplate appears in 2+ places, factor it into a tactic and
  replace the inlined copies. Smaller proofs make future
  obligations easier to diagnose when SMT regresses.
- **Keep tactics atomic; compose for the macro shapes.** A tactic
  should do *one* recognisable proof step. If a pattern is "prove
  A, then use A to close B", implement two atomic tactics and a
  `tactic|`-macro that sequences them. Don't bake A+B into a
  single monolithic tactic that you can't reuse for "prove A but
  close C differently" later. The existing `pverify_*` family
  follows this rule — preserve it.
- **Tag new `GlobalState` update helpers with `@[pverifySimp]`.**
  SMT prep reduces them before lean-auto runs; an untagged helper
  reaches the solver as an opaque symbol and the obligation
  returns `unknown`.
