/-
PLean.Syntax.Loop — `foreach` and `while` loops with invariants.

Surface (close to P's grammar, modulo brace-blocks):

```
  foreach (x in xs)
    invariant N1 : I1;
    invariant N2 : I2;
    { body }

  while (cond)
    invariant N : I;
    [done_with <prop>]
    [decreasing <measure>]
    { body }
```

`invariant` clauses may appear zero or more times between the loop
header and the body block. Each `<prop>` is a state-implicit predicate
of the same shape as a `system <s> { invariant N : <body> }` clause.
The surrounding `s` binder is fresh per loop (named `s`) and
is read via a `get` immediately before the gadget call.

## Desugaring

Both forms target the loop syntax of Lean core's `do` notation, whose
`invariant` clause the `do` elaborator turns into the `Std.WP` loop
gadgets that `vcgen` reads:

```
  foreach (x in xs) invariant N1 : I1; … { body }
    ↝ for x in xs invariant _pref _suff s => I1 ∧ … ∧ Ik do body

  while (cond) invariant N1 : I1; … [done_with D;] { body }
    ↝ while cond invariant exit s => (I1 ∧ … ∧ Ik) ∧ (exit = true → D)
        do body
```

`s` is the state argument of `PProp Sig = GlobalState Sig → Prop`
(the assertion language `Std.WP` assigns `PM Sig`); invariant bodies
refer to the live state through it. A loop without invariant clauses
desugars to the plain core loop.

## Done-with / decreasing

`done_with` defaults to `¬cond`. `decreasing` is parsed but not
forwarded: PLean's loops are verified for partial correctness, and
core's `decreasing` clause would make `vcgen` demand a termination
proof.

## Limitations

- Loop invariants don't yet flow through the registry-aware
  field-projection rewrites that `system`-block invariants get;
  users spell `n.<v>` projections out as
  `(s.machines n.ref).fields.<M>_<v>`. Kind-guard injection for
  `∀ n : <M>, …` / `∀ e : <ev>, …` quantifiers *does* run on loop
  invariant bodies via the `pLoopInvWrap%` elab-time wrapper.
- `foreach` over `Set` / `PMap` requires a `ForIn` instance that
  PLean doesn't yet provide.
-/
import Lean
import PLean.Semantics.Monad
import PLean.Internal.Registry
import PLean.Syntax.Verify

open Lean Elab Term

namespace PLean

/-! ## Elab-time kind-guard injection for loop-invariant bodies.

Loop invariants written inside a `foreach` / `while` desugar at macro
time, before the pmodule's registry has resolved its full machine /
event kind set. Unlike `system`-block invariants (which materialise at
`#gen_module` time and pick up registry-aware kind-guard injection),
loop invariants would land at SMT with bare `∀ n : <M>, …` quantifiers
— letting the solver fabricate machines whose runtime `kind` slot
doesn't match `<M>_kind`.

`pLoopInvWrap` is a sentinel `term` whose elaborator reads the env at
elaboration time (any `pmodule` registration has completed by then),
computes the machine / event kind sets, runs `injectKindGuards` over
the body, then elaborates the rewritten term in place. Soundness:
guards only weaken the body (`∀ n : <M>, P` → `∀ n : <M>, is_<M> n →
P`), so injection cannot make a real obligation harder. -/

scoped syntax (name := pLoopInvWrap)
  "pLoopInvWrap%" "(" ident ")" term : term

private def collectMachineKinds (ctxs : Array LocalPModuleCtx) : NameSet :=
  ctxs.foldl (init := ({} : NameSet)) fun acc ctx =>
    ctx.machines.foldl (init := acc) fun s n _ => s.insert n

private def collectEventKinds (ctxs : Array LocalPModuleCtx) : NameSet :=
  ctxs.foldl (init := ({} : NameSet)) fun acc ctx =>
    ctx.events.foldl (init := acc) fun s n _ => s.insert n

open Lean Elab Term in
@[term_elab pLoopInvWrap]
def elabPLoopInvWrap : TermElab := fun stx expected => do
  match stx with
  | `(pLoopInvWrap% ($s:ident) $body:term) =>
    -- Read every registered pmodule's machine / event kinds. We don't
    -- know which pmodule the loop belongs to at term-elab time, so we
    -- accept ALL — kind injection only fires on quantifiers whose
    -- type name matches a registered kind, so off-pmodule references
    -- don't get spurious guards (a `∀ x : Nat, …` body is untouched).
    let all := (pmoduleExt.getState (← getEnv)).toList.map (·.2) |>.toArray
    let mks := collectMachineKinds all
    let eks := collectEventKinds all
    -- Opt into the wrapper→MachineRef rewrite in loop invariants —
    -- the iteration VC's `∀ x : <wrapper>` quantifier trips lean-auto;
    -- surface invariants keep wrapper types for manual-proof ergonomics.
    let rewritten ← liftMacroM <|
      PLean.injectKindGuards mks eks s.getId body.raw
        (rewriteWrapperToRef := true)
    elabTerm rewritten expected
  | _ => throwUnsupportedSyntax

/-! ## Loop-invariant clause grammar.

Shared between `foreach` and `while`. -/

declare_syntax_cat pLoopInv

syntax (name := pLoopInvItem)
  "invariant " ident " : " term ";" : pLoopInv

/-! ## `done_with` / `decreasing` clauses (while-only). -/

declare_syntax_cat pLoopDoneWith
declare_syntax_cat pLoopDecreasing

syntax (name := pLoopDoneWithItem)
  "done_with " term ";" : pLoopDoneWith

syntax (name := pLoopDecreasingItem)
  "decreasing " term ";" : pLoopDecreasing

/-! ## `foreach` and `while` doElem grammars. -/

syntax (name := pForeach)
  "foreach " "(" ident " in " term ")"
    pLoopInv*
  "{" doSeq "}" : doElem

syntax (name := pWhile)
  "while " "(" term ")"
    pLoopInv*
    (pLoopDoneWith)?
    (pLoopDecreasing)?
  "{" doSeq "}" : doElem

/-! ## Macro helpers. -/

/-- Collect the prop syntax from a `pLoopInv` clause array. -/
private def collectInvariantProps
    (invs : Array (TSyntax `pLoopInv)) :
    MacroM (Array (TSyntax `term)) := do
  let mut out : Array (TSyntax `term) := #[]
  for inv in invs do
    match inv with
    | `(pLoopInv| invariant $_:ident : $prop:term ;) =>
      out := out.push prop
    | _ => Macro.throwErrorAt inv "internal: unexpected loop-invariant clause"
  return out

/-- The conjunction `I1 ∧ … ∧ Ik` of the loop's invariant bodies over
the state binder `s`, each wrapped in `pLoopInvWrap% (s)` so
kind-guard injection runs at term-elab time (when the registry is
available). `none` when the loop has no invariant clauses. -/
private def buildInvariantConj (invs : Array (TSyntax `pLoopInv))
    (sBinder : Ident) : MacroM (Option (TSyntax `term)) := do
  let props ← collectInvariantProps invs
  let wrapped ← props.mapM fun p => `(pLoopInvWrap% ($sBinder) $p)
  match wrapped.back? with
  | none => return none
  | some last =>
    some <$> wrapped.pop.foldrM (init := last) fun p acc => `($p ∧ $acc)

/-! ## Macro expansions. -/

macro_rules
  | `(doElem| foreach ($x:ident in $xs:term)
                $[$invs:pLoopInv]*
              { $body:doSeq }) => do
    let sBinder : Ident := mkIdent `s
    match ← buildInvariantConj invs sBinder with
    | some conj =>
      `(doElem| for $x:ident in $xs invariant _pref _suff $sBinder:ident => $conj do $body)
    | none =>
      `(doElem| for $x:ident in $xs do $body)
  | `(doElem| while ($cond:term)
                $[$invs:pLoopInv]*
                $[$dw:pLoopDoneWith]?
                $[$_decr:pLoopDecreasing]?
              { $body:doSeq }) => do
    let sBinder : Ident := mkIdent `s
    let doneWith : TSyntax `term ←
      match dw with
      | some dwStx => match dwStx with
        | `(pLoopDoneWith| done_with $t:term ;) => pure t
        | _ => Macro.throwErrorAt dwStx "internal: malformed done_with"
      | none => `(¬ $cond)
    let invConj ← match ← buildInvariantConj invs sBinder with
      | some conj => pure conj
      | none => `(True)
    -- With no invariant and no `done_with`, emit the plain core loop
    -- and let `vcgen` handle it unannotated.
    if invs.isEmpty && dw.isNone then
      `(doElem| while $cond:term do $body)
    else
      `(doElem| while $cond:term
                  invariant exit $sBinder:ident => $invConj ∧ (exit = true → $doneWith)
                  do $body)

end PLean
