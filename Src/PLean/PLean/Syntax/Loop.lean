/-
PLean.Syntax.Loop — `foreach` / `while` with loop invariants.

  foreach (x in c) invariant N : I ; … { body }
  while (c) invariant N : I ; … [done_with D ;] [decreasing M ;] { body }

Both desugar to Lean core's loop syntax, which `vcgen` reads:
`for x in xs invariant pref suff hs => …` and
`while c invariant exit hs => … decreasing hs => …`. An invariant is a
proposition over the handler state `hs` (this machine's state `hs.ms`,
its sends so far `hs.out`, …) and the handler's mutable locals;
`foreach` invariants may also name `pref` / `suff`, the items already
visited / still to visit. `foreach` iterates sets (their members), maps
(their keys) and sequences.

`vcgen` proves `while` loops totally correct, so a `while` loop in a
verified handler needs a `decreasing` measure.
-/
import Lean
import PLean.Semantics.Containers

namespace PLean

open Lean

declare_syntax_cat pLoopInv
syntax (name := pLoopInvItem) "invariant " ident " : " term ";" : pLoopInv

declare_syntax_cat pLoopDoneWith
declare_syntax_cat pLoopDecreasing
syntax (name := pLoopDoneWithItem) "done_with " term ";" : pLoopDoneWith
syntax (name := pLoopDecreasingItem) "decreasing " term ";" : pLoopDecreasing

syntax (name := pForeach)
  "foreach " "(" ident " in " term ")" pLoopInv* "{" doSeq "}" : doElem

syntax (name := pWhile)
  "while " "(" term ")" pLoopInv* (pLoopDoneWith)? (pLoopDecreasing)? "{" doSeq "}" : doElem

/-- `I1 ∧ … ∧ Ik`, or `none` without invariants. -/
private def invariantConj (invs : Array (TSyntax `pLoopInv)) : MacroM (Option (TSyntax `term)) := do
  let props ← invs.mapM fun inv => match inv with
    | `(pLoopInv| invariant $_:ident : $prop:term ;) => pure prop
    | _ => Macro.throwErrorAt inv "internal: unexpected loop-invariant clause"
  match props.back? with
  | none => return none
  | some last => some <$> props.pop.foldrM (init := last) fun p acc => `($p ∧ $acc)

macro_rules
  | `(doElem| foreach ($x:ident in $xs:term) $[$invs:pLoopInv]* { $body:doSeq }) => do
    let hs := mkIdent `hs
    match ← invariantConj invs with
    | some conj =>
      `(doElem| for $x:ident in PLean.PIterable.toList $xs
                  invariant $(mkIdent `pref) $(mkIdent `suff) $hs:ident => $conj do $body)
    | none => `(doElem| for $x:ident in PLean.PIterable.toList $xs do $body)
  | `(doElem| while ($cond:term) $[$invs:pLoopInv]* $[$dw:pLoopDoneWith]? $[$decr:pLoopDecreasing]?
              { $body:doSeq }) => do
    let hs := mkIdent `hs
    let exit := mkIdent `exit
    let doneWith ← match dw with
      | some d => match d with
        | `(pLoopDoneWith| done_with $t:term ;) => pure t
        | _ => Macro.throwErrorAt d "internal: malformed done_with"
      | none => `(¬ $cond)
    let invConj ← match ← invariantConj invs with
      | some conj => pure conj
      | none => `(True)
    match decr with
    | some d => match d with
      | `(pLoopDecreasing| decreasing $m:term ;) =>
        `(doElem| while $cond:term
                    invariant $exit:ident $hs:ident => $invConj ∧ ($exit = true → $doneWith)
                    decreasing $hs:ident => $m
                    do $body)
      | _ => Macro.throwErrorAt d "internal: malformed decreasing"
    | none =>
      if invs.isEmpty && dw.isNone then `(doElem| while $cond:term do $body)
      else
        `(doElem| while $cond:term
                    invariant $exit:ident $hs:ident => $invConj ∧ ($exit = true → $doneWith)
                    do $body)

end PLean
