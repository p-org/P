/-
PLean.Verify.Tactic — `pverify_close`, the automation for an obligation.

  pverify_close [h₁, …] [d₁, …] [l₁, …]

1. unfolds `RunOK` and introduces the coin stream (handler obligations),
2. unfolds the handler definitions `hᵢ` and runs `vcgen`,
3. substitutes the start state, normalises with `pverifySimp` plus the
   definitions `dᵢ` (invariants, kind predicates, …),
4. destructures message / machine-wrapper locals and conjunctive /
   existential hypotheses (`pv_destruct`), splits the goal's conjunctions, and
5. discharges each conjunct with `crush`, given the hints `lᵢ`.
-/
import PLean.Verify.Norm
import Crush

namespace PLean

syntax (name := pverifyClose) "pverify_close" "[" ident,* "]" "[" ident,* "]" "[" term,* "]" : tactic

open Lean Elab Tactic in
@[tactic pverifyClose]
def evalPverifyClose : Tactic := fun stx => do
  let hs : Array Ident := stx[2].getSepArgs.map (⟨·⟩)
  let ds : Array Ident := stx[5].getSepArgs.map (⟨·⟩)
  let ls : Array (TSyntax `term) := stx[8].getSepArgs.map (⟨·⟩)
  evalTactic (← `(tactic| intros))
  evalTactic (← `(tactic| try (unfold PLean.RunOK; intro coins)))
  for h in hs do evalTactic (← `(tactic| try unfold $h:ident))
  evalTactic (← `(tactic| try vcgen))
  let simpArgs ← ds.mapM fun d => `(Lean.Parser.Tactic.simpLemma| $d:ident)
  let norm ← `(tactic| all_goals (try simp +zetaDelta only [pverifySimp, $[$simpArgs],*] at *))
  evalTactic (← `(tactic| all_goals (try subst_vars)))
  evalTactic norm
  evalTactic (← `(tactic| all_goals pv_destruct))
  evalTactic (← `(tactic| all_goals (try subst_vars)))
  evalTactic norm
  evalTactic (← `(tactic| all_goals pv_destruct))
  evalTactic (← `(tactic| all_goals (try subst_vars)))
  evalTactic (← `(tactic| all_goals (try and_intros)))
  if ls.isEmpty then
    evalTactic (← `(tactic| all_goals crush [*]))
  else
    let hints ← ls.mapM fun l => `(Crush.crushHintElem| $l:term)
    evalTactic (← `(tactic| all_goals crush [*, $[$hints],*]))

end PLean
