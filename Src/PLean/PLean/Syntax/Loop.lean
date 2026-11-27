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
visited / still to visit. Every loop invariant automatically includes the
frame `HState.frame __loop0 hs …`: what the body cannot change, relative
to the handler state `__loop0` at loop entry. `new_sends e => P` states
that every message the loop has sent so far satisfies `P`. `foreach` iterates
sets (their members), maps (their keys) and sequences.

`vcgen` proves `while` loops totally correct, so a `while` loop in a
verified handler needs a `decreasing` measure.
-/
import Lean
import PLean.Semantics.Containers
import PLean.Semantics.Msg
import PLean.Syntax.Stmt

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

/-- In a loop invariant: every message the loop has sent so far satisfies
`P`. -/
syntax (name := pNewSends) "new_sends " ident " => " term : term

macro_rules
  | `(new_sends $x:ident => $p) =>
    `($(mkIdent ``PLean.sentSince) $(mkIdent `__loop0) $(mkIdent `hs) (fun $x => $p))

/-- Whether `stx` contains a node of one of these kinds. -/
private partial def mentionsKind (ks : Array SyntaxNodeKind) (stx : Syntax) : Bool :=
  ks.contains stx.getKind || stx.getArgs.any (mentionsKind ks)

/-- The automatic frame conjunct `HState.frame __loop0 hs …` for a loop body. -/
private def frameOf (body : Syntax) : MacroM (TSyntax `term) := do
  -- a raised event's handler may do anything
  let raises := mentionsKind #[``pRaisePayload, ``pRaiseNoPayload] body
  let writesMs := raises || mentionsKind #[``pAssign, ``pSetAdd, ``pSetRemove, ``pMapAssign,
    ``pMapAdd, ``pMapRemove, ``pGotoNoPayload, ``pGotoPayload] body
  let sends := raises || mentionsKind #[``pSendNamed, ``pSendPayload, ``pSendNoPayload] body
  let spawns := raises || mentionsKind #[``pNewNoArg, ``pNewArg] body
  let announces := raises || mentionsKind #[``pAnnouncePayload, ``pAnnounceNoPayload] body
  let b (x : Bool) : MacroM (TSyntax `term) := if x then `(true) else `(false)
  `($(mkIdent ``PLean.HState.frame) $(mkIdent `__loop0) $(mkIdent `hs) $(← b !writesMs)
      $(← b !sends) $(← b !spawns) $(← b !announces))

/-- `frame ∧ I1 ∧ … ∧ Ik`. -/
private def invariantConj (invs : Array (TSyntax `pLoopInv)) (body : Syntax) :
    MacroM (TSyntax `term) := do
  let props ← invs.mapM fun inv => match inv with
    | `(pLoopInv| invariant $_:ident : $prop:term ;) => pure prop
    | _ => Macro.throwErrorAt inv "internal: unexpected loop-invariant clause"
  props.foldrM (init := ← frameOf body) fun p acc => `($acc ∧ $p)

macro_rules
  | `(doElem| foreach ($x:ident in $xs:term) $[$invs:pLoopInv]* { $body:doSeq }) => do
    let hs := mkIdent `hs
    let conj ← invariantConj invs body
    `(doElem| do
        let $(mkIdent `__loop0):ident ← get
        for $x:ident in PLean.PIterable.toList $xs
          invariant $(mkIdent `pref) $(mkIdent `suff) $hs:ident => $conj do $body)
  | `(doElem| while ($cond:term) $[$invs:pLoopInv]* $[$dw:pLoopDoneWith]? $[$decr:pLoopDecreasing]?
              { $body:doSeq }) => do
    let hs := mkIdent `hs
    let exit := mkIdent `exit
    let doneWith ← match dw with
      | some d => match d with
        | `(pLoopDoneWith| done_with $t:term ;) => pure t
        | _ => Macro.throwErrorAt d "internal: malformed done_with"
      | none => `(¬ $cond)
    let invConj ← invariantConj invs body
    match decr with
    | some d => match d with
      | `(pLoopDecreasing| decreasing $m:term ;) =>
        -- a measure over locals only is state-independent
        if (m.raw.find? fun s => s.isIdent && s.getId == `hs).isSome then
          `(doElem| do
              let $(mkIdent `__loop0):ident ← get
              while $cond:term
                invariant $exit:ident $hs:ident => $invConj ∧ ($exit = true → $doneWith)
                decreasing $hs:ident => $m
                do $body)
        else
          `(doElem| do
              let $(mkIdent `__loop0):ident ← get
              while $cond:term
                invariant $exit:ident $hs:ident => $invConj ∧ ($exit = true → $doneWith)
                decreasing $m
                do $body)
      | _ => Macro.throwErrorAt d "internal: malformed decreasing"
    | none =>
      `(doElem| do
          let $(mkIdent `__loop0):ident ← get
          while $cond:term
            invariant $exit:ident $hs:ident => $invConj ∧ ($exit = true → $doneWith)
            do $body)

end PLean
