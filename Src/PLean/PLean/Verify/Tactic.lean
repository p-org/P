/-
PLean.Verify.Tactic — `pverify_close`, the automation for an obligation.

  pverify_close [h₁, …] [d₁, …] [l₁, …] [a₁, …]

1. unfolds `RunPost` / `RunSafe` and introduces the coin stream (handler
   obligations),
2. unfolds the handler definitions `hᵢ`, runs `vcgen`, and records each
   `foreach` cursor's membership (`pv_cursor_mem`),
3. adds the facts `aᵢ` (axioms) as hypotheses, substitutes the start
   state, and normalises with `pverifySimp` plus the definitions `dᵢ`
   (invariants, kind predicates, …),
4. destructures message / machine-wrapper locals and conjunctive /
   existential hypotheses (`pv_destruct`), splits the goal's conjunctions, and
5. discharges each conjunct with `crush`, given the hints `lᵢ`: briefly
   with z3 and cvc5 without crush's ground instantiation, then with it at
   the full timeout (cvc5, then z3).
-/
import PLean.Verify.Norm
import PLean.Semantics.Msg
import Crush

namespace PLean

register_option pverify.profile : Bool := {
  defValue := false
  descr := "Record, per goal handed to the solver, its time, the solver that closed it, and \
            crush's phase breakdown (in `#pverify`'s report file)." }

register_option pverify.heartbeats : Nat := {
  defValue := 0
  descr := "Heartbeat budget (in thousands, 0 = unlimited) of each solver attempt, so a slow \
            proof reconstruction gives up (the attempt then trusts the solver, under \
            `crush.trust reconstructOrTrust`)." }

syntax (name := pverifyClose)
  "pverify_close" "[" ident,* "]" "[" ident,* "]" "[" term,* "]" "[" term,* "]" : tactic

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
  let crush1 ← if ls.isEmpty then `(tactic| crush [*])
    else
      let hints ← ls.mapM fun l => `(Crush.crushHintElem| $l:term)
      `(tactic| crush [*, $[$hints],*])
  -- Solver portfolio.
  let profile := pverify.profile.get (← getOptions)
  let prof ← if profile then `(Lean.Parser.Tactic.tacticSeq| set_option crush.profile true in $crush1:tactic)
    else `(Lean.Parser.Tactic.tacticSeq| $crush1:tactic)
  -- Quick attempts without crush's ground instantiation (the solvers' own
  -- quantifier instantiation usually suffices, and is much cheaper), then
  -- full-length attempts with it.
  let portfolio : List (String × TSyntax `tactic) := [
    ("z3 (5s)", ← `(tactic| (set_option crush.backend "z3" in set_option crush.timeout 5 in
        set_option crush.inst.fuel 0 in ($prof)))),
    ("cvc5 (5s)", ← `(tactic| (set_option crush.backend "cvc5" in set_option crush.timeout 5 in
        set_option crush.inst.fuel 0 in ($prof)))),
    ("cvc5", ← `(tactic| (set_option crush.backend "cvc5" in ($prof)))),
    ("z3", ← `(tactic| (set_option crush.backend "z3" in ($prof))))]
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
    let mut lastErr : Option Exception := none
    for (solver, tac) in portfolio do
      setGoals [g]
      let s ← saveState
      try
        bounded tac
        closedBy := some solver
        break
      catch e =>
        s.restore (restoreInfo := true)
        lastErr := some e
    if profile then
      let ms := (← IO.monoMsNow) - t0
      let slow ← if ms > 5000 && closedBy.isSome then
          pure m!"\n{← Meta.ppGoal g}" else pure m!""
      logInfo m!"pverify-time: {(← g.getTag).eraseMacroScopes}: {ms} ms, {closedBy.getD "failed"}{slow}"
    if closedBy.isNone then
      -- Record the goal handed to `crush` (for `#pverify`'s report).
      logInfo m!"pverify-goal: {← Meta.ppGoal g}"
      if let some e := lastErr then throw e
  setGoals []

end PLean
