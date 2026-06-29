/-
PLean.Commands.PVerify — `#pverify M`.

Walks the registry, runs the obligation generator, and prints the
report grouped by outcome in three separate message boxes —
`── failed ──` (with counter-examples), `── manual-proof skeletons ──`
(copy-paste `@[pverifyProof]` decls echoing the full theorem signature),
then `── passed ──` — followed by a concise pass/fail summary. Grouping
by outcome keeps failures and their fixes together rather than
interleaved with passes. The summary is a separate trailing message
(info on success, warning/error on incomplete) so callers can pin the
verdict with `#guard_msgs (…, drop info)`.
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
  | .missingPremise lem refBy =>
    s!"      `{lem}` is cited by `prove {refBy} using …` but no \
       `prove {lem} ;` directive exists in this pmodule.\n      \
       Add `prove {lem} ;` to a Proof block (before the citing `prove`), \
       or drop `{lem}` from the `using` list."
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
    -- Reset the profile aggregator at command entry; obligation rows
    -- accumulate during `synthesise` and we emit a report at the end
    -- when `pverify.profile` is enabled. The reset is a no-op (one
    -- ref assignment) when profiling is off — cheap to always do.
    liftM (PLean.Verify.Profile.reset : IO Unit)
    liftM (PLean.resetDiagMap : IO Unit)
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
    -- Grouped by outcome (not emission order), in three separate
    -- message boxes — failed obligations (+ diagnostics), manual-proof
    -- skeletons, then passed obligations — so failures and their fixes
    -- stay together rather than interleaved with passes. The concise
    -- pass/fail summary is a separate trailing message (info on success,
    -- warning/error on incomplete) so callers can pin the verdict with
    -- `#guard_msgs (…, drop info)`.
    let failedRecs := result.records.filter (·.outcome.isFailure)
    let passedRecs := result.records.filter (!·.outcome.isFailure)
    logInfo m!"{modName}: {result.attempted} obligations from \
               {proofCount} prove-directives"
    unless failedRecs.isEmpty do
      let mut sec : Array String := #["── failed ──"]
      for rec in failedRecs do
        let diag := renderDiagnostic rec
        sec := sec.push (if diag.isEmpty then renderRow rec
                         else s!"{renderRow rec}\n{diag}")
      logInfo ("\n".intercalate sec.toList)
      -- Missing-premise failures are structural — no theorem to write —
      -- so they get no skeleton. Filter them out before emitting the
      -- skeletons box, and skip the box entirely if nothing remains.
      let skeletonRecs := failedRecs.filter fun rec =>
        match rec.outcome with
        | .missingPremise .. => false
        | _ => true
      unless skeletonRecs.isEmpty do
        let mut skel : Array String := #["── manual-proof skeletons ──"]
        for rec in skeletonRecs do
          skel := skel.push (renderSkeleton rec)
        logInfo ("\n".intercalate skel.toList)
    unless passedRecs.isEmpty do
      let mut sec : Array String := #["── passed ──"]
      for rec in passedRecs do
        sec := sec.push (renderRow rec)
      logInfo ("\n".intercalate sec.toList)
    let summary :=
      m!"{modName}: {result.smtProved} proved by SMT, \
         {result.userProved} user-proved, \
         {result.disproved} disproved, \
         {result.unknown} unknown, \
         {result.tacticErr} tactic-error, \
         {result.unfinished} no-diagnostic, \
         {result.missingPremise} missing-premise"
    -- Emit the profile breakdown when `pverify.profile` is set. Two
    -- tables: per-obligation top-10 by wall time, then per-stage
    -- aggregate with % of total. The instrumented branch in
    -- `pverify_smt` writes into `Profile.stateRef` only when the
    -- option is on, so when it's off this dumps zero-valued rows and
    -- we just skip the message entirely.
    if pverify.profile.get (← getOptions) then
      let prof ← liftM (PLean.Verify.Profile.stateRef.get : IO _)
      logInfo m!"── profile (per obligation, top 10 by wall) ──\n{
        PLean.Verify.Profile.renderTopN prof.rows 10}"
      logInfo m!"── profile (stage aggregate) ──\n{
        PLean.Verify.Profile.renderAggregate prof.rows}"
    if result.failures == 0 then
      logInfo summary
    else
      let tail : MessageData :=
        if result.missingPremise > 0 ∧
            result.failures == result.missingPremise then
          m!"{result.missingPremise} `using` premise(s) cite a lemma \
             that no `prove` directive proves; add the missing \
             `prove <lemma>;` directives."
        else if result.missingPremise > 0 then
          m!"{result.failures} obligation(s) need attention: \
             {result.missingPremise} missing `using`-premise(s) need a \
             `prove <lemma>;` directive added; the rest need a manual \
             proof — fill in the skeletons above."
        else
          m!"{result.failures} obligation(s) need a manual proof; \
             fill in the skeletons above."
      let body := m!"{summary}\n{tail}"
      if pverify.failOnIncomplete.get (← getOptions) then
        throwError body
      else
        logWarning body

end PLean
