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

Both forms target Lean's native `for _ in Lean.Loop.mk do …` (for
`while`) or `for x in xs do …` (for `foreach`), with Loom's gadget
chain placed at the top of the body:

```
  for _ in Lean.Loop.mk do
    let s ← get
    PLean.loopInvariantGadget [I1 s, …, Ik s]
    onDoneGadget <doneWith>
    decreasingGadget <measure?>
    if <cond> then <body> else break
```

The shape matches Loom's `WPGen.forWithInvariantLoop`
(`@[loomSpec, loomWpSimp]`).

## Done-with / decreasing

`done_with` defaults to `¬cond`; `decreasing` defaults to `none`.
Under `PartialCorrectness DemonicChoice` (PLean's default lattice
mode) the measure is informational only; a future total-correctness
mode picks it up via the same WPGen entry.

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
import PLean.Semantics.Loop
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
private partial def collectInvariantProps
    (invs : Array (TSyntax `pLoopInv)) :
    MacroM (Array (TSyntax `term)) := do
  let mut out : Array (TSyntax `term) := #[]
  for inv in invs do
    match inv with
    | `(pLoopInv| invariant $_:ident : $prop:term ;) =>
      out := out.push prop
    | _ => Macro.throwErrorAt inv "internal: unexpected loop-invariant clause"
  return out

/-- Build the gadget-prelude `doElem`s the macro splices into the
loop body. Three items in this fixed order (the order Loom's
`WPGen.forWithInvariantLoop` matches on):

1. `loopInvariantGadget [P1, …, Pk]` — the WP gadget. Each `Pi` is
   a `PProp Sig = GlobalState → Prop`; the WP evaluates them against
   the live state automatically (the gadget operates at the
   assertion-lattice level, not at the program-state level).
2. `onDoneGadget <doneWith>` — always emitted (defaults to `⊤` /
   `fun _ => True`). The pattern Loom matches requires a 3-item
   chain regardless of whether the user wrote `done_with` or not.
3. `decreasingGadget <measure?>` — always emitted; `none` is fine.

The pattern is rigid: Loom's `WPGen.forWithInvariantLoop` matches the
exact body shape `do invariantGadget … ; onDoneGadget … ;
decreasingGadget … ; f u b`. Inserting extra `do`-statements before
or between the gadgets makes the match fail and `wpgen` falls back to
`WPGen.default`, leaving an opaque `wp (forIn …)` term that lean-auto
can't translate.

The state binder `s` is named-only (each user prop becomes
`fun s => <body>` — the WP applies the live state under the
hood). The name avoids shadowing a user `s` from an enclosing
`system` block. -/
private def buildGadgetPrelude
    (invs : Array (TSyntax `pLoopInv))
    (doneWith? : Option (TSyntax `term))
    (measure? : Option (TSyntax `term))
    (iterBinder : Ident) :
    MacroM (Array (TSyntax `doElem)) := do
  let props ← collectInvariantProps invs
  let sBinder : Ident := mkIdent `s
  let sigIdent : Ident := mkIdent `Sig
  let mut entries : Array (TSyntax `term) := #[]
  for p in props do
    -- Wrap in `pLoopInvWrap% (s) <body>` so kind-guard injection
    -- happens at term-elab time (when the registry is available).
    entries := entries.push
      (← `(((fun $sBinder =>
              pLoopInvWrap% ($sBinder) $p) : PLean.PProp $sigIdent)))
  let listLit : TSyntax `term ← `([ $entries,* ])
  -- The gadgets take `inv : β → List l` / `on_done' : β → l` /
  -- `measure : β → Option ℕ`. We thread the iteration binder
  -- through (`fun <iter> => …`) even when the user's invariants
  -- don't reference it — Loom's pattern match requires the
  -- gadget arg to be β-parameterised.
  let doneTerm : TSyntax `term ← match doneWith? with
    | some t => `(((fun $sBinder => $t) : PLean.PProp $sigIdent))
    | none   => `(((fun _ => True) : PLean.PProp $sigIdent))
  let measureTerm : TSyntax `term ← match measure? with
    | some t => `(fun $iterBinder => some $t)
    | none   => `(fun _ => (none : Option Nat))
  let mut out : Array (TSyntax `doElem) := #[]
  -- Iteration-parameterised wrappers — `inv b` / `on_done' b` /
  -- `measure b`. For `Lean.Loop.mk` (β = Unit) the binder is `_`; for
  -- a foreach over a list it's the element. The actual list /
  -- predicate / option-Nat is constant inside.
  out := out.push (← `(doElem|
    PLean.loopInvariantGadget (P := $sigIdent) ((fun $iterBinder => $listLit) $iterBinder)))
  out := out.push (← `(doElem|
    onDoneGadget (m := PLean.PM $sigIdent)
      ((fun $iterBinder => $doneTerm) $iterBinder)))
  out := out.push (← `(doElem|
    decreasingGadget (m := PLean.PM $sigIdent) ($measureTerm $iterBinder)))
  return out

/-! ## Macro expansions. -/

/-- Splice an array of `doElem`s into a single `doSeq` followed by
the user's `body` `doSeq`. We can't write `$[$p:doElem]* $body:doSeq`
inside a `for … do` quotation because Lean's `do`-grammar accepts a
single `doSeq`, not a sequence of items + a trailing doSeq. Instead
we build the combined `doSeq` as a leaf via `mkNullNode`. -/
private def mkLoopBody (prelude : Array (TSyntax `doElem))
    (body : TSyntax ``Parser.Term.doSeq) :
    MacroM (TSyntax ``Parser.Term.doSeq) := do
  let bodyItems : Array (TSyntax `doElem) :=
    if body.raw.getKind == ``Parser.Term.doSeqIndent then
      body.raw[0].getArgs.map fun arg => ⟨arg[0]⟩
    else if body.raw.getKind == ``Parser.Term.doSeqBracketed then
      body.raw[1].getArgs.map fun arg => ⟨arg[0]⟩
    else
      #[]
  let allItems : Array (TSyntax `doElem) := prelude ++ bodyItems
  `(Parser.Term.doSeq| $[$allItems:doElem]*)

/-- Build the `[I1, …, Ik]` literal of `PProp Sig`-typed invariants
the `pforeach` / `WPGen.forWithInvariantLoop` specs both consume. -/
private def buildInvariantList (invs : Array (TSyntax `pLoopInv)) :
    MacroM (TSyntax `term) := do
  let props ← collectInvariantProps invs
  let sBinder : Ident := mkIdent `s
  let sigIdent : Ident := mkIdent `Sig
  let mut entries : Array (TSyntax `term) := #[]
  for p in props do
    -- Same `pLoopInvWrap%` indirection as `buildGadgetPrelude` —
    -- elab-time kind-guard injection on each invariant body.
    entries := entries.push
      (← `(((fun $sBinder =>
              pLoopInvWrap% ($sBinder) $p) : PLean.PProp $sigIdent)))
  `([ $entries,* ])

macro_rules
  | `(doElem| foreach ($x:ident in $xs:term)
                $[$invs:pLoopInv]*
              { $body:doSeq }) => do
    -- `pforeach xs invList (fun x => do body)` — matches
    -- `WPGen.pforeach`'s `@[loomSpec]` registration so `wpgen` peels
    -- the iteration. The body Lean expects is a single `PM P Unit`
    -- term, so we wrap the user's doSeq in `do …`.
    let invList ← buildInvariantList invs
    let sigIdent : Ident := mkIdent `Sig
    `(doElem|
        PLean.pforeach (P := $sigIdent) $xs $invList (fun $x => do $body))
  | `(doElem| while ($cond:term)
                $[$invs:pLoopInv]*
                $[$dw:pLoopDoneWith]?
                $[$decr:pLoopDecreasing]?
              { $body:doSeq }) => do
    let doneWith? : Option (TSyntax `term) ←
      match dw with
      | some dwStx => match dwStx with
        | `(pLoopDoneWith| done_with $t:term ;) => pure (some t)
        | _ => Macro.throwErrorAt dwStx "internal: malformed done_with"
      | none =>
        let cNeg : TSyntax `term ← `(¬ $cond)
        pure (some cNeg)
    let measure? : Option (TSyntax `term) ←
      match decr with
      | some dStx => match dStx with
        | `(pLoopDecreasing| decreasing $t:term ;) => pure (some t)
        | _ => Macro.throwErrorAt dStx "internal: malformed decreasing"
      | none => pure none
    -- The `Lean.Loop.mk`-driven loop binds `_ : Unit` per iteration.
    let unitBinder : Ident := mkIdent `__lpUnit
    let prelude ← buildGadgetPrelude invs doneWith? measure? unitBinder
    let guardedBody : TSyntax ``Parser.Term.doSeq :=
      ⟨(← `(doSeq|
          if $cond then
            $body:doSeq
          else
            break)).raw⟩
    let loopBody ← mkLoopBody prelude guardedBody
    `(doElem|
        for $unitBinder:ident in Lean.Loop.mk do
          $loopBody:doSeq)

end PLean
