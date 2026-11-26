/-
PLean.Verify.Tactic — `pverify_close`, the automation for an obligation.

  pverify_close [h₁, …] [d₁, …] [l₁, …] [a₁, …]

1. unfolds `RunPost` / `RunSafe` and introduces the coin stream (handler
   obligations),
2. unfolds the handler definitions `hᵢ`, runs `vcgen`, splits the
   post-condition into one goal per invariant (`PostInv`), tagged with the
   invariant's name (`pv_split_post`), and records each `foreach` cursor's
   membership (`pv_cursor_mem`),
3. adds the facts `aᵢ` (axioms) as hypotheses, substitutes the start
   state, and normalises with `pverifySimp` plus the definitions `dᵢ`
   (invariants, kind predicates, …),
4. destructures message / machine-wrapper locals and conjunctive /
   existential hypotheses (`pv_destruct`), splits the goal's conjunctions, and
5. discharges each conjunct with `crush`, given the hints `lᵢ`: briefly
   with z3 and cvc5 without crush's ground instantiation, then with it at
   the full timeout (cvc5, then z3).

When a goal is not proved, it logs the goal (`pverify-goal:`), the
invariant it belongs to (`pverify-inv:`) and, when z3 finds one, a small
model of the goal's encoding (`pverify-smt-script:` / `pverify-smt-model:`,
see `PLean.Cex`), for `#pverify`'s report.
-/
import PLean.Verify.Norm
import PLean.Verify.Cex
import PLean.Semantics.Msg
import Crush

namespace PLean

register_option pverify.profile : Bool := {
  defValue := false
  descr := "Record, per goal handed to the solver, its time, the solver that closed it, and \
            crush's phase breakdown (in `#pverify`'s report file)." }

register_option pverify.cex : Bool := {
  defValue := true
  descr := "Search for a counterexample (a small model of the goal's encoding) when a goal is \
            not proved." }

register_option pverify.heartbeats : Nat := {
  defValue := 0
  descr := "Heartbeat budget (in thousands, 0 = unlimited) of each solver attempt, so a slow \
            proof reconstruction gives up (the attempt then trusts the solver, under \
            `crush.trust reconstructOrTrust`)." }

open Lean Elab Tactic in
/-- A small model of `g`'s encoding by `crushTac` (`Cex.findModel`): the
script z3 solved and its model. -/
def findCex (g : MVarId) (crushTac : TSyntax `tactic) : TacticM (Option (String × String)) := do
  let (h, path) ← IO.FS.createTempFile
  h.flush
  let s ← saveState
  try
    setGoals [g]
    evalTactic (← `(tactic| (set_option crush.backend "z3" in set_option crush.timeout 1 in
      set_option crush.inst.fuel 0 in set_option crush.save $(Syntax.mkStrLit path.toString):str in
      $crushTac:tactic)))
  catch _ => pure ()
  s.restore (restoreInfo := true)
  let script ← IO.FS.readFile path
  try IO.FS.removeFile path catch _ => pure ()
  if script.isEmpty then return none
  Cex.findModel script

syntax (name := pverifyClose)
  "pverify_close" "[" ident,* "]" "[" ident,* "]" "[" term,* "]" "[" term,* "]" : tactic

private def has (s pat : String) : Bool := (s.splitOn pat).length > 1

open Lean Elab Tactic in
@[tactic pverifyClose]
def evalPverifyClose : Tactic := fun stx => do
  let hs : Array Ident := stx[2].getSepArgs.map (⟨·⟩)
  let ds : Array Ident := stx[5].getSepArgs.map (⟨·⟩)
  let ls : Array (TSyntax `term) := stx[8].getSepArgs.map (⟨·⟩)
  let as : Array (TSyntax `term) := stx[11].getSepArgs.map (⟨·⟩)
  evalTactic (← `(tactic| intros))
  evalTactic (← `(tactic|
    try ((first | unfold PLean.RunPost | unfold PLean.RunSafe | unfold PLean.RunOK); intro coins)))
  for h in hs do evalTactic (← `(tactic| try unfold $h:ident))
  evalTactic (← `(tactic| try vcgen))
  evalTactic (← `(tactic| pv_split_post))
  evalTactic (← `(tactic| pv_cursor_mem))
  -- Facts join the hypotheses, so they are normalised like them.
  for a in as do
    evalTactic (← `(tactic| all_goals (try have := $a:term)))
  let simpArgs ← ds.mapM fun d => `(Lean.Parser.Tactic.simpLemma| $d:ident)
  -- The first pass keeps `sentSince` folded, so a loop's sends are peeled off
  -- one `push` at a time (`sentSince_push`).
  let norm0 ← `(tactic| all_goals (simp -failIfUnchanged +zetaDelta only
    [pverifySimp, reduceCtorEq, $[$simpArgs],*] at *))
  let norm ← `(tactic| all_goals (simp -failIfUnchanged +zetaDelta only
    [pverifySimp, reduceCtorEq, $(mkIdent ``PLean.sentSince):ident, $[$simpArgs],*] at *))
  evalTactic (← `(tactic| all_goals (try subst_vars)))
  evalTactic norm0
  for _ in [0:3] do
    evalTactic norm
    evalTactic (← `(tactic| all_goals pv_destruct))
    evalTactic (← `(tactic| all_goals (try subst_vars)))
  evalTactic norm
  evalTactic (← `(tactic| all_goals pv_destruct))
  evalTactic (← `(tactic| all_goals (try and_intros)))
  evalTactic (← `(tactic| all_goals pv_clear_lets))
  -- A hint about `<ev>_payload_of` matters only to goals that mention it (a
  -- fact about a symbol nothing else mentions cannot help a proof), and
  -- asserting it elsewhere keeps the solvers from finding models.
  let hintKeys ← ls.mapM fun l => do
    let some n ← (try pure (some (← realizeGlobalConstNoOverload l.raw)) catch _ => pure none)
      | return #[]
    let some ci := (← getEnv).find? n | return #[]
    return ci.type.getUsedConstants.filter fun c =>
      match c with
      | .str _ s => s.endsWith "_payload_of"
      | _ => false
  let profile := pverify.profile.get (← getOptions)
  -- Solver portfolio: quick attempts without crush's ground instantiation
  -- (the solvers' own quantifier instantiation usually suffices, and is much
  -- cheaper), then full-length attempts with it.
  let portfolioFor (g : MVarId) : TacticM (TSyntax `tactic × List (String × TSyntax `tactic)) :=
      g.withContext do
    let mut used : NameSet := {}
    for c in (← instantiateMVars (← g.getType)).getUsedConstants do used := used.insert c
    for d in ← getLCtx do
      unless d.isImplementationDetail do
        for c in (← instantiateMVars d.type).getUsedConstants do used := used.insert c
    let hints ← ((ls.zip hintKeys).filter fun (_, ks) => ks.isEmpty || ks.any used.contains).mapM
      fun (l, _) => `(Crush.crushHintElem| $l:term)
    let crush1 ← if hints.isEmpty then `(tactic| crush [*]) else `(tactic| crush [*, $[$hints],*])
    let prof ← if profile then
        `(Lean.Parser.Tactic.tacticSeq| set_option crush.profile true in $crush1:tactic)
      else `(Lean.Parser.Tactic.tacticSeq| $crush1:tactic)
    return (crush1, [
      ("z3 (5s)", ← `(tactic| (set_option crush.backend "z3" in set_option crush.timeout 5 in
          set_option crush.inst.fuel 0 in ($prof)))),
      ("cvc5 (5s)", ← `(tactic| (set_option crush.backend "cvc5" in set_option crush.timeout 5 in
          set_option crush.inst.fuel 0 in ($prof)))),
      ("cvc5", ← `(tactic| (set_option crush.backend "cvc5" in ($prof)))),
      ("z3", ← `(tactic| (set_option crush.backend "z3" in ($prof))))])
  let budget := pverify.heartbeats.get (← getOptions)
  let bounded (tac : TSyntax `tactic) : TacticM Unit :=
    if budget == 0 then evalTactic tac
    else withTheReader Core.Context (fun c => { c with maxHeartbeats := budget * 1000 })
      (Core.withCurrHeartbeats (evalTactic tac))
  -- Conjuncts that are hypotheses (a loop invariant carried through an
  -- iteration) need no solver.
  evalTactic (← `(tactic| all_goals (try assumption)))
  for g in ← getGoals do
    let t0 ← IO.monoMsNow
    let mut closedBy : Option String := none
    let mut errs : Array (String × Exception) := #[]
    let (crushTac, portfolio) ← portfolioFor g
    for (solver, tac) in portfolio do
      setGoals [g]
      let s ← saveState
      try
        bounded tac
        closedBy := some solver
        break
      catch e =>
        s.restore (restoreInfo := true)
        errs := errs.push (solver, e)
        -- A model of the whole query: the later attempts (the same query, plus
        -- instances of its quantified facts) cannot prove the goal either.
        let msg ← e.toMessageData.toString
        if has msg "found a model" && !has msg "refused part of the query" then break
    if profile then
      let ms := (← IO.monoMsNow) - t0
      let slow ← if ms > 5000 && closedBy.isSome then
          pure m!"\n{← Meta.ppGoal g}" else pure m!""
      logInfo m!"pverify-time: {(← g.getTag).eraseMacroScopes}: {ms} ms, {closedBy.getD "failed"}{slow}"
    if closedBy.isNone then
      -- Record the goal handed to `crush` and its invariant (for `#pverify`'s report).
      logInfo m!"pverify-goal: {← Meta.ppGoal g}"
      if let some inv := invOfTag (← g.getTag).eraseMacroScopes then
        logInfo m!"pverify-inv: {inv}"
      let mut refuted := false
      for (_, e) in errs do
        if has (← e.toMessageData.toString) "reported `unsat`" then refuted := true
      -- A goal a solver refuted (but no proof could be replayed) has no model.
      if pverify.cex.get (← getOptions) && !refuted then
        if let some (script, model) ← findCex g crushTac then
          logInfo m!"pverify-smt-script:\n{script}"
          logInfo m!"pverify-smt-model:\n{model}"
      -- Fail with a model when some attempt found one (z3's, which `#pverify`
      -- decodes, first; the last of them has the most instantiations).
      let mut best : Option Exception := errs.back?.map (·.2)
      let mut bestRank := 0
      for (solver, e) in errs do
        if has (← e.toMessageData.toString) "found a model" then
          let rank := if solver.startsWith "z3" then 2 else 1
          if rank ≥ bestRank then
            best := some e
            bestRank := rank
      if let some e := best then throw e
  setGoals []

end PLean
