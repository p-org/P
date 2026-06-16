/-
PLean.Commands.PVerify — `#pverify M`.

Walks the registry, runs the obligation generator, and prints a
per-obligation report classifying each result as proved by SMT,
proved manually (via `@[pverifyProof]`), disproved (with
counter-example), unknown, tactic-error, or unfinished. Failures
print a copy-paste skeleton echoing the full theorem signature so
the user can fill in a manual proof.
-/
import Lean
import PLean.Commands.PWf
import PLean.Verify.Obligation
import PLean.Verify.ProofRegistry

open Lean Elab Command

namespace PLean

/-- When `true` (default), `#pverify` throws on any obligation it
couldn't discharge — CI-fail semantics. When `false`, failures
surface as warnings and the build proceeds, which is useful while
iterating on `@[pverifyProof]` skeletons. -/
register_option pverify.failOnIncomplete : Bool := {
  defValue := true
  descr := "If true, `#pverify` throws on incomplete obligations \
            (CI-fail semantics). If false, reports them as warnings \
            and continues."
}

private def renderRow (rec : Verify.ObligationRecord) : String :=
  s!"  {rec.outcome.glyph} {rec.thmName}  {rec.outcome.tag}"

/-- Diagnostic body shown indented under a failing obligation's row.
Empty for successful obligations and for failures with no captured
diagnostic. -/
private def renderDiagnostic (rec : Verify.ObligationRecord) : String :=
  match rec.outcome with
  | .disproved cex =>
    if cex.isEmpty then "" else
      "      counter-example:\n" ++ indentBy 8 cex
  | .unknown reason =>
    if reason.isEmpty then "" else
      "      reason:\n" ++ indentBy 8 reason
  | .tacticError msg =>
    if msg.isEmpty then "" else
      "      tactic error:\n" ++ indentBy 8 msg
  | _ => ""
where
  indentBy (n : Nat) (s : String) : String :=
    let pad := String.mk (List.replicate n ' ')
    String.intercalate "\n" (s.splitOn "\n" |>.map (pad ++ ·))

/-- Render the manual-proof skeleton for a single failed obligation,
echoing the elaborated signature so the user can paste a complete
theorem instead of guessing the binders. -/
private def renderSkeleton (rec : Verify.ObligationRecord) : String :=
  -- `ppSignature` produces `name (binders) : type`; we prepend the
  -- attribute and wrap with `by sorry` for a ready-to-paste decl.
  if rec.signature.isEmpty then
    s!"@[pverifyProof] theorem {rec.thmName} := by sorry"
  else
    s!"@[pverifyProof] theorem {rec.signature} := by\n  sorry"

syntax (name := pverifyCmd) "#pverify " ident : command

@[command_elab pverifyCmd]
def elabPVerify : CommandElab := fun stx => do
  let `(#pverify $name:ident) := stx
    | throwUnsupportedSyntax
  let modName := name.getId
  elabCommand (← `(#pwf $name))
  match ← getPModule? modName with
  | none => pure ()
  | some ctx =>
    -- Open `<Mod>` and `PartialCorrectness.DemonicChoice` so emitted
    -- theorems resolve unqualified module names and so `wpgen` sees
    -- the scoped `MAlgOrdered` instances it needs.
    elabCommand (← `(namespace $name))
    elabCommand (← `(open $(mkIdent `PartialCorrectness):ident
                          $(mkIdent `DemonicChoice):ident))
    let result ← Verify.synthesise modName ctx
    elabCommand (← `(end $name))
    let proofCount := ctx.proofs.foldl (init := 0) fun acc p =>
      acc + p.directives.size
    if result.attempted == 0 then
      logInfo m!"{modName}: no `Proof` directives — nothing to verify"
      return
    logInfo m!"{modName}: {result.attempted} obligations from \
               {proofCount} prove-directives"
    for rec in result.records do
      let diag := renderDiagnostic rec
      if diag.isEmpty then
        logInfo m!"{renderRow rec}"
      else
        logInfo m!"{renderRow rec}\n{diag}"
    let summary :=
      m!"{modName}: {result.smtProved} proved by SMT, \
         {result.userProved} user-proved, \
         {result.disproved} disproved, \
         {result.unknown} unknown, \
         {result.tacticErr} tactic-error, \
         {result.unfinished} no-diagnostic"
    if result.failures == 0 then
      logInfo summary
    else
      -- Skeletons go in their own info messages so the warning /
      -- error stays a single short line tests can pin.
      let failedRecs := result.records.filter (·.outcome.isFailure)
      for rec in failedRecs do
        logInfo m!"{renderSkeleton rec}"
      let body :=
        m!"{summary}\n{result.failures} obligation(s) need a manual \
           proof; skeletons printed above."
      if pverify.failOnIncomplete.get (← getOptions) then
        throwError body
      else
        logWarning body

end PLean
