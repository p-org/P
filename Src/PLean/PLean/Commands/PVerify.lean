/-
PLean.Commands.PVerify — `#pverify M`.

Phase 3: walk the registry, synthesise per-handler Hoare-triple
obligations from `Proof` directives, and discharge each via the
`pverify` tactic.

The flow:
  1. Run `#pwf M` (well-formedness).
  2. Walk every `Proof { prove X using Y, Z; ... }` directive.
  3. For each (M, S, ev) handler in the program, emit one
     `theorem <Mod>.<M>.<S>.<ev>_correct_<X>` and discharge with
     `pverify` (or `pverify_default` for `prove default`). Failures
     are captured per-obligation (see PLAN_P3 R19) and reported.
  4. Print a summary; throw if any obligation failed.

Decisions: D17 (Phase 2's handler-def existence check) graduates;
D18 (per-handler obligation shape); D23 (registry walk); D24 (default
proof obligation); D25 (using-clause precondition strengthening).
-/
import Lean
import PLean.Commands.PWf
import PLean.Verify.Obligation

open Lean Elab Command

namespace PLean

syntax (name := pverifyCmd) "#pverify " ident : command

@[command_elab pverifyCmd]
def elabPVerify : CommandElab := fun stx => do
  let `(#pverify $name:ident) := stx
    | throwUnsupportedSyntax
  let modName := name.getId
  -- 1. Run the same well-formedness check `#pwf` does.
  elabCommand (← `(#pwf $name))
  -- 2/3. Synthesise and discharge per-handler obligations.
  match ← getPModule? modName with
  | none => pure ()
  | some ctx =>
    -- Open the pmodule namespace so the emitted theorems land at
    -- `<Mod>.<M>.<S>.<ev>_correct_<X>` and can resolve `Sig` / `E` /
    -- handler defs without qualification.
    elabCommand (← `(namespace $name))
    let result ← Verify.synthesise modName ctx
    elabCommand (← `(end $name))
    -- 4. Report.
    let proofCount := ctx.proofs.foldl (init := 0) fun acc p =>
      acc + p.directives.size
    if result.attempted == 0 then
      logInfo m!"{modName}: no `Proof` directives — nothing to verify"
    else if result.failed.isEmpty then
      logInfo m!"{modName}: {result.attempted} obligations from {proofCount} prove-directives, all ✓"
    else
      let failuresMsg := String.intercalate ", " (result.failed.toList.map fun (m, s, e, l) =>
        s!"{m}.{s}.{e} (lemma {l})")
      throwError "{modName}: {result.failed.size}/{result.attempted} obligations failed: {failuresMsg}"

end PLean
