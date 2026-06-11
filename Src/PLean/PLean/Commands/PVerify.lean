/-
PLean.Commands.PVerify — `#pverify M`.

Phase 3 (post-2026-06-09 pivot): walk the registry, look up
user-provided `@[pverifyProof]` theorems, and for every remaining
obligation emit a `theorem ... := by pverify`. Failures are captured
per-obligation and reported. The user's manual-proof escape hatch
(via `@[pverifyProof]`) is the load-bearing piece — even when SMT
can't close every obligation, the user can always write a
`@[pverifyProof] theorem` to cover the gap.

The flow:
  1. Run `#pwf M` (well-formedness).
  2. Walk every `Proof { prove X using Y, Z; ... }` directive.
  3. For each (M, S, ev) handler in the program:
     a. If a `@[pverifyProof] theorem <Mod>.<M>.<S>.<ev>_correct_<X>`
        is in the env, count it as user-proved and skip emission.
     b. Otherwise emit `theorem <Mod>.<M>.<S>.<ev>_correct_<X>` with
        a `:= by pverify` body. On success, count as proved-by-SMT.
        On failure, capture the error and add to the failure report.
  4. After (3), auto-emit `prove default;` for every (M, S, ev) that
     the user didn't already cover (D24).
  5. Print a summary `<modName>: N obligations (M proved by SMT, K
     user-proved, J failed)`. If any failed, the user should write
     `@[pverifyProof]` theorems for the failed names.

Decisions: D17 (Phase 2's handler-def existence check) graduates;
D18 (per-handler obligation shape); D23 (registry walk); D24 (default
proof obligation; now auto-emitted); D25 (using-clause precondition
strengthening).

This mirrors Veil's `#check_invariants`: `@[invProof]` theorems
override the auto-discharge pass; failed obligations print a
copy-paste skeleton ("Run with the printed theorem statement and
add a `by ...` body").
-/
import Lean
import PLean.Commands.PWf
import PLean.Verify.Obligation
import PLean.Verify.ProofRegistry

open Lean Elab Command

namespace PLean

/-- When `true` (default), `#pverify` throws on any obligation it
couldn't discharge (matching CI-failure semantics — no silent
incompleteness). When `false`, failures are reported as warnings
plus a `logInfo` summary, but the build proceeds. Useful while
iterating on `@[pverifyProof]` theorems for hard handlers (the
benchmark file builds clean even with residual obligations, and
flips to `all ✓` once enough manual proofs land).

Mirrors Veil's `veil.failedCheckThrowsError`. -/
register_option pverify.failOnIncomplete : Bool := {
  defValue := true
  descr := "If true, `#pverify` throws on incomplete obligations \
            (CI-fail semantics). If false, reports them as warnings \
            and continues."
}

syntax (name := pverifyCmd) "#pverify " ident : command

@[command_elab pverifyCmd]
def elabPVerify : CommandElab := fun stx => do
  let `(#pverify $name:ident) := stx
    | throwUnsupportedSyntax
  let modName := name.getId
  -- 1. Run the same well-formedness check `#pwf` does.
  elabCommand (← `(#pwf $name))
  -- 2/3/4. Synthesise and discharge per-handler obligations.
  match ← getPModule? modName with
  | none => pure ()
  | some ctx =>
    -- Open the pmodule namespace so the emitted theorems land at
    -- `<Mod>.<M>.<S>.<ev>_correct_<X>` and can resolve `Sig` / `E` /
    -- handler defs without qualification. Open `PartialCorrectness
    -- DemonicChoice` too so the scoped `MAlgOrdered` instances are
    -- visible during obligation elaboration (needed by `wpgen`).
    elabCommand (← `(namespace $name))
    -- Open `PartialCorrectness DemonicChoice` (top-level Loom
    -- namespaces — see `Loom.MonadAlgebras.WP.Basic` and
    -- `Loom.MonadAlgebras.NonDetT.Extract`) so the scoped
    -- `MAlgOrdered` instances are visible during obligation
    -- elaboration. Without these the WP machinery `wpgen` invokes
    -- can't step through the `NonDetT (StateT _ DivM)` stack.
    let pcId : Ident := mkIdent `PartialCorrectness
    let dcId : Ident := mkIdent `DemonicChoice
    elabCommand (← `(open $pcId:ident $dcId:ident))
    let result ← Verify.synthesise modName ctx
    elabCommand (← `(end $name))
    -- 5. Report.
    let proofCount := ctx.proofs.foldl (init := 0) fun acc p =>
      acc + p.directives.size
    if result.attempted == 0 then
      logInfo m!"{modName}: no `Proof` directives — nothing to verify"
    else if result.failed.isEmpty then
      logInfo m!"{modName}: {result.attempted} obligations from \
                 {proofCount} prove-directives \
                 ({result.smtProved} proved by SMT, \
                  {result.userProved} user-proved, 0 failed)"
    else
      let failuresMsg := String.intercalate ", " (result.failed.toList.map fun (m, s, e, l, _) =>
        s!"{m}.{s}.{e} (lemma {l})")
      let skeletonMsg := String.intercalate "\n" (result.failed.toList.map fun (_, _, _, _, thmN) =>
        s!"  @[pverifyProof] theorem {thmN} := by sorry  -- supply manual proof")
      let summary :=
        m!"{modName}: {result.attempted} obligations from \
           {proofCount} prove-directives \
           ({result.smtProved} proved by SMT, \
            {result.userProved} user-proved, \
            {result.failed.size} failed)\n\
           Failed: {failuresMsg}\n\
           Write a manual proof for each via `@[pverifyProof]` \
           (paste these names verbatim):\n{skeletonMsg}"
      if pverify.failOnIncomplete.get (← getOptions) then
        throwError summary
      else
        logWarning summary

end PLean
