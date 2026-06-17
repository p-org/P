/-
PLean.Surface.Verify — `invariant`, `paxiom`, `init-holds`, `function`, `pinstance`.

  invariant <name> : <prop>
  paxiom    <name> : <prop>
  init-holds <prop>          -- assume-on-start
  function  <name> (params) : <retT> = <expr>     -- defined helper
  function  <name> (params) : <retT>              -- foreign helper
  pinstance <name> : <Class> <T>                  -- axiom bundle

`init-holds` is hyphenated to avoid colliding with Lean's `init :=` named
argument syntax. `function` matches P's existing `fun` decl spelling
(without the keyword collision Lean's `pure` would trigger).

## Two-phase elaboration

All verification declarations defer to `#gen_module` time, just like
machines/types/events. An invariant body may reference any module-level
name (machine vars, event payloads, types), so eager elaboration would
fail in any but the most trivial cases.
-/
import Lean
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Semantics.GlobalState

open Lean Elab Command

namespace PLean

/-! ## Invariant

Invariants are state-implicit: `invariant <name> : <body>` materialises
to `def <name> : GS → Prop := fun <s> => <body>`, where `<s>` is the
state binder introduced by an enclosing `system <s> { … }` block. A
bare `invariant <name> : <body>` (outside `system`) emits
`fun _ => <body>` — only valid for state-independent properties.

The `system <s> { … }` block is the user-facing way to bind the state
explicitly. It can appear at top level inside a `pmodule`, or nested
inside a `Lemma` / `Theorem` block. Multiple `system` blocks may
appear, possibly with different binder names — each invariant
remembers its own. -/

syntax (name := pInvariant) "invariant " ident " : " term : command

@[command_elab pInvariant]
def elabPInvariant : CommandElab := fun stx => do
  let `(invariant $id:ident : $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "invariant"
  let ns ← getCurrNamespace
  -- Bare top-level form: no state binder; the body must be
  -- state-independent. The materialiser emits `fun _ => <body>`.
  addInvariant
    { name := id.getId, leanName := ns ++ id.getId
      stateBinder := none
      defStx := some stx, ref := stx }

/-! ### `system <s> { invariant … ; … }` — state-implicit invariant block.

Top-level form. Each child invariant is registered with
`stateBinder := some <s>`, so the materialiser emits
`def <name> : GS → Prop := fun <s> => <body>`. -/

/-- Invariant-line grammar inside a `system` block. Shared between the
top-level and Lemma-nested forms so the pattern can be matched with
`` `(pSystemInv| invariant … : …) ``. We need a real syntax category
because `pInvariant` is a named *command* rule and can't be folded
into a sub-pattern. -/
declare_syntax_cat pSystemInv

syntax (name := pSystemInvItem)
  "invariant " ident " : " term : pSystemInv

syntax (name := pSystemBlock) "system " ident "{" pSystemInv* "}" : command

@[command_elab pSystemBlock]
def elabPSystemBlock : CommandElab := fun stx => do
  let `(system $sid:ident { $items:pSystemInv* }) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "system"
  let ns ← getCurrNamespace
  let sName := sid.getId
  for it in items do
    match it with
    | `(pSystemInv| invariant $iid:ident : $prop:term) =>
      -- Reconstruct the inner syntax so `materialiseInvariant`'s
      -- existing pattern match still works (`invariant <ident> : <term>`).
      -- Position info: `iid` and `prop` are spliced verbatim and retain
      -- their original source ranges, so any error targeting them via
      -- `throwErrorAt $iid` / `throwErrorAt $prop` still points at the
      -- user's source. Only the outermost `invariant` token sits at
      -- the macro expansion site; no current error path uses it.
      let invStx ← `(command| invariant $iid:ident : $prop)
      addInvariant
        { name := iid.getId, leanName := ns ++ iid.getId
          stateBinder := some sName
          defStx := some invStx.raw, ref := it }
    | _ => throwErrorAt it "internal: unexpected `system`-block child"

/-! ## Axiom (single-prop) -/

syntax (name := pAxiom) "paxiom " ident " : " term : command

@[command_elab pAxiom]
def elabPAxiom : CommandElab := fun stx => do
  let `(paxiom $id:ident : $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "paxiom"
  let ns ← getCurrNamespace
  addAxiom
    { name := id.getId, leanName := ns ++ id.getId, defStx := some stx, ref := stx }

/-! ## Init (assume-on-start) -/

syntax (name := pInit) "init-holds " term : command

@[command_elab pInit]
def elabPInit : CommandElab := fun stx => do
  let _ ← requireLocalPModuleCtx "init-holds"
  addInit { defStx := some stx, ref := stx }

/-! ## Pure (defined or foreign) -/

syntax (name := pPureDefined)
  "function " ident bracketedBinder* " : " term " = " term : command

syntax (name := pPureForeign)
  "function " ident bracketedBinder* " : " term : command

@[command_elab pPureDefined]
def elabPPureDefined : CommandElab := fun stx => do
  let `(function $id:ident $_:bracketedBinder* : $_:term = $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "function"
  let ns ← getCurrNamespace
  addPure
    { name := id.getId, leanName := ns ++ id.getId
      hasBody := true, defStx := some stx, ref := stx }

@[command_elab pPureForeign]
def elabPPureForeign : CommandElab := fun stx => do
  let `(function $id:ident $_:bracketedBinder* : $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "function"
  let ns ← getCurrNamespace
  addPure
    { name := id.getId, leanName := ns ++ id.getId
      hasBody := false, defStx := some stx, ref := stx }

/-! ## Pinstance (axiom bundle)

  `pinstance nm : Class T`  ↝  `variable [nm : Class T]`
-/

syntax (name := pInstance) "pinstance " ident " : " term : command

@[command_elab pInstance]
def elabPInstance : CommandElab := fun stx => do
  let `(pinstance $id:ident : $tp:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "pinstance"
  let typeRepr := tp.raw.prettyPrint.pretty
  addInstance
    { name := id.getId, classRepr := typeRepr, typeRepr := typeRepr
      defStx := some stx, ref := stx }

/-! ## Lemma / Theorem / Proof blocks

  Lemma   <name> { (invariant … | system <s> { invariant … }) … }
  Theorem <name> { (invariant … | system <s> { invariant … }) … }
  Proof   <name>? { prove <lemma> [using <l1>, …]; prove default; }

Each inner `invariant` is also registered as a free-standing
`PInvariantDecl` so `using`-clauses can reference it by name. The
Lemma/Theorem record retains the ordered name list so the bundle
predicate `def X : GS → Prop := fun s => P1 s ∧ P2 s ∧ …` can be
emitted at materialisation time. -/

declare_syntax_cat pLemmaBodyItem

syntax (name := pLemmaInvariant)
  "invariant " ident " : " term : pLemmaBodyItem

/-- `system <s> { invariant … }` inside a Lemma / Theorem block. Same
semantics as the top-level form. -/
syntax (name := pLemmaSystemBlock)
  "system " ident "{" pSystemInv* "}" : pLemmaBodyItem

syntax (name := pLemmaDeclSyntax)
  "Lemma " ident " {" pLemmaBodyItem* "}" : command

syntax (name := pTheoremDeclSyntax)
  "Theorem " ident " {" pLemmaBodyItem* "}" : command

declare_syntax_cat pProofItem

/-- `prove <name> [using <name1>, <name2>, …];`

`prove default;` uses the literal identifier `default`; the elaborator
dispatches on the name string. We do NOT introduce `default` as a
keyword token because that would break uses of `default` as a term
(e.g. `Inhabited`-generated `⟨ctor default⟩`). -/
syntax (name := pProofProve)
  "prove " ident (" using " ident,+)? ";" : pProofItem

syntax (name := pProofDeclSyntax)
  "Proof " (ident)? "{" pProofItem* "}" : command

private def collectLemmaInvariants (id : Ident) (items : Array Syntax)
    (isTheorem : Bool) (refStx : Syntax) :
    CommandElabM Unit := do
  let _ ← requireLocalPModuleCtx (if isTheorem then "Theorem" else "Lemma")
  let ns ← getCurrNamespace
  let mut invNames : Array Name := #[]
  -- Helper: register one invariant with the given (optional) state binder.
  let registerOne (iid : TSyntax `ident) (prop : TSyntax `term)
      (stateBinder : Option Name) (childRef : Syntax) :
      CommandElabM Unit := do
    let invStxReal ← `(command| invariant $iid:ident : $prop)
    addInvariant
      { name := iid.getId, leanName := ns ++ iid.getId
        stateBinder := stateBinder
        defStx := some invStxReal.raw, ref := childRef }
  for it in items do
    match it with
    | `(pLemmaBodyItem| invariant $iid:ident : $prop:term) =>
      -- Bare invariant inside a Lemma — state-independent.
      registerOne iid prop none it
      invNames := invNames.push iid.getId
    | `(pLemmaBodyItem| system $sid:ident { $invs:pSystemInv* }) =>
      -- Nested `system <s> { … }`: each child invariant binds the same
      -- state name `<s>` for its body.
      let sName := sid.getId
      for inv in invs do
        match inv with
        | `(pSystemInv| invariant $iid:ident : $prop:term) =>
          registerOne iid prop (some sName) inv
          invNames := invNames.push iid.getId
        | _ => throwErrorAt inv "internal: unexpected `system`-block child"
    | _ => throwErrorAt it "unrecognised lemma body item"
  addLemma
    { name := id.getId, isTheorem := isTheorem
      invariants := invNames, defStx := some refStx, ref := refStx }

@[command_elab pLemmaDeclSyntax]
def elabPLemma : CommandElab := fun stx => do
  let `(Lemma $id:ident { $items:pLemmaBodyItem* }) := stx
    | throwUnsupportedSyntax
  collectLemmaInvariants id (items.map (·.raw)) (isTheorem := false) stx

@[command_elab pTheoremDeclSyntax]
def elabPTheorem : CommandElab := fun stx => do
  let `(Theorem $id:ident { $items:pLemmaBodyItem* }) := stx
    | throwUnsupportedSyntax
  collectLemmaInvariants id (items.map (·.raw)) (isTheorem := true) stx

@[command_elab pProofDeclSyntax]
def elabPProof : CommandElab := fun stx => do
  let ctx ← requireLocalPModuleCtx "Proof"
  let (nm, items) ← match stx with
    | `(Proof $nm:ident { $items:pProofItem* }) =>
      pure (nm.getId, items)
    | `(Proof { $items:pProofItem* }) =>
      pure (Name.anonymous, items)
    | _ => throwUnsupportedSyntax
  let mut directives : Array PProveDirective := #[]
  for it in items do
    match it with
    | `(pProofItem| prove $tgt:ident $[using $usingIds,*]? ;) =>
      -- Keep the original `usingId` syntax tokens so we can point the
      -- error at the precise `using <name>` if any name is unknown.
      let useTokens : Array (TSyntax `ident) := match usingIds with
        | none     => #[]
        | some xs  => xs.getElems
      let useIds : Array Name := useTokens.map (·.getId)
      let tgtName := tgt.getId
      let isDefault := tgtName == `default
      -- Validate prove-target names early so typos surface at the
      -- `prove` line instead of as a cryptic later elaboration failure.
      -- `default` is the sanity-invariant sentinel and is always valid.
      unless isDefault || ctx.lemmas.contains tgtName do
        throwErrorAt tgt
          s!"`prove`: no `Lemma` or `Theorem` named '{tgtName}' in pmodule '{ctx.name}' (must be `default` or a previously-declared lemma)"
      for tok in useTokens do
        let uid := tok.getId
        unless ctx.lemmas.contains uid do
          throwErrorAt tok
            s!"`prove ... using`: no `Lemma` or `Theorem` named '{uid}' in pmodule '{ctx.name}'"
      directives := directives.push
        { target := tgtName, isDefault := isDefault
          usingLemmas := useIds, ref := it }
    | _ => throwErrorAt it "unrecognised Proof item"
  addProof { name := nm, directives := directives, ref := stx }

/-! ## Materialisation

Replay each verification declaration as a Lean def. Called by
`#gen_module` after machines have been materialised so invariant /
axiom bodies can reference machine fields and event payloads. -/

/-- Reject any `∀ <ident> : GlobalState <Sig>, …` (or `∀ <ident> :
PLean.GlobalState <Sig>, …`) anywhere in an invariant body when the
invariant lives inside a `system <s> { … }` block. Such a binder
shadows the outer `<s>` for the body it scopes — and the previous
fix only caught the leading-position case, so a body like
`True ∧ (∀ s : GlobalState Sig, P s)` evaded detection. The walk
below is recursive over the body Syntax.

A bare top-level `invariant standalone : ∀ s : GlobalState Sig, P` is
allowed — it's intentionally state-independent (the materialiser uses
a wildcard binder, so no shadowing risk). The check therefore only
fires when an enclosing `system` block has registered a state binder. -/
private partial def containsExplicitStateBinder (stx : Syntax) : Option Syntax := Id.run do
  -- Match-on-Syntax patterns return `Option (TSyntax k)`; we surface
  -- the offending sub-expression so the error can `throwErrorAt` it.
  match stx with
  | `(∀ _ : PLean.GlobalState $_, $_)
  | `(∀ _ : GlobalState $_, $_)
  | `(∀ $_:ident : PLean.GlobalState $_, $_)
  | `(∀ $_:ident : GlobalState $_, $_)
  | `(∀ ($_:ident : PLean.GlobalState $_), $_)
  | `(∀ ($_:ident : GlobalState $_), $_) => return some stx
  | _ => pure ()
  for child in stx.getArgs do
    if let some hit := containsExplicitStateBinder child then
      return some hit
  return none

private def rejectExplicitStateBinder (id : Ident) (prop : TSyntax `term) :
    CommandElabM Unit := do
  match containsExplicitStateBinder prop with
  | some hit =>
    throwErrorAt hit m!"invariant `{id.getId}` lives inside a `system` block \
      but its body contains `∀ <ident> : GlobalState <Sig>, …`. That \
      inner ∀-binder shadows the outer `system` state binder and \
      silently decouples the invariant from per-handler state (the \
      soundness hole fixed 2026-06-10). Drop the inner \
      `∀ … : GlobalState Sig,` and reference the `system`-block's \
      state binder directly in the body."
  | none => pure ()

/-! ## Auto-injection of kind guards over machines and events

PLean wraps **machines** in a struct `<M> := { ref : MachineRef }`
(D10) and emits a runtime kind predicate `is_<M> : MachineRef → GS → Prop`.
`∀ n : <M>, P n` quantifies over every `<M>`-wrapped ref, including
ones whose state slot is unallocated or has the wrong kind. The
convention is to follow each such quantifier with `is_<M> n.ref s →`
(or `∧` under `∃`) so the body can rely on the kind tag.

PVerifier's source surface treats events the same way: `∀ (e : eGrant), P`
means "every label whose action is an `eGrant` event." PLean has no
event wrapper struct — events flow through `Sig.Label` with a ctor
inside `.action`. We bridge by rewriting `∀ e : <ev>, body` (where
`<ev>` is a registered event) to `∀ e : Sig.Label, is_<ev> e → body`.
The binder's static type becomes `Sig.Label`; the body's references to
`e` now compile as ordinary `Sig.Label` projections (`e.action`,
`e.target`, …).

The rewriter walks the body Syntax: every `∀`/`∃` over a machine kind
gets the `is_<M> x.ref s` guard; every `∀`/`∃` over an event kind
gets its type retyped to `Sig.Label` and the `is_<ev> x` guard. Both
recurse through the body. -/

private def isMachineKindIdent (machineKinds : NameSet) (stx : Syntax) : Bool :=
  stx.isIdent && machineKinds.contains stx.getId

private def isEventKindIdent (eventKinds : NameSet) (stx : Syntax) : Bool :=
  stx.isIdent && eventKinds.contains stx.getId

/-- Extract `(idents, typeIdent)` pairs from a single binder node. A
`∀`/`∃` quantifier carries an array of binders in `stx[1]`; each is
either an ident (in the `∀ x y z : T, body` shape — multiple idents
sharing the type in `stx[2]`) or a bracketed `explicitBinder` (in the
`∀ (x : T) (y : U), body` shape — one ident + type per binder). The
trailing typeSpec in `stx[2]` is only meaningful when the binders are
bare idents. -/
private def collectBinderPairs (binders : Array Syntax) (typeSpec : Syntax) :
    Array (Syntax × Syntax) := Id.run do
  let mut out : Array (Syntax × Syntax) := #[]
  -- Type from the outer typeSpec, if present: `stx[2][0]` is a
  -- `Term.typeSpec` (`: T`); its `[1]` child is the type term.
  let sharedTypeIdent : Option Syntax :=
    if typeSpec.getNumArgs > 0 then some typeSpec[0][1] else none
  for b in binders do
    if b.isIdent then
      if let some ty := sharedTypeIdent then
        out := out.push (b, ty)
    else if b.getKind == ``Lean.Parser.Term.explicitBinder then
      -- `( idents : type )`: idents at `[1]`, type at `[2][1]`.
      let ids := b[1].getArgs
      let tyTerm := b[2][1]
      for i in ids do
        out := out.push (i, tyTerm)
  return out

/-- Expand a multi-binder `∀ x y z : T, body` / `∀ (x : T) (y : U), body`
(and the `∃` analogues) into nested single-binder form. Returns the
original syntax unchanged if it isn't a recognised quantifier or
already has a single binder pair. -/
private def expandMultiBinder (stx : Syntax) : MacroM (Option Syntax) := do
  let asTerm (s : Syntax) : TSyntax `term := ⟨s⟩
  let kind := stx.getKind
  let isForall := kind == ``Lean.Parser.Term.forall
  let isExists :=
    kind == `Lean.Parser.Term.exists || kind.toString.endsWith ".exists"
  unless isForall || isExists do return none
  let binders := stx[1].getArgs
  let typeSpec := stx[2]
  let pairs := collectBinderPairs binders typeSpec
  if pairs.size ≤ 1 then return none
  let body := stx[4]
  -- Build the nested form right-to-left.
  let mut acc : Syntax := body
  for (xRaw, tyRaw) in pairs.reverse do
    let xIdent : TSyntax `ident := ⟨xRaw⟩
    let typeT : TSyntax `term := ⟨tyRaw⟩
    let inner ←
      if isForall then `(∀ $xIdent:ident : $typeT, $(asTerm acc))
      else            `(∃ $xIdent:ident : $typeT, $(asTerm acc))
    acc := inner.raw
  return some acc

/-- Build a quantifier of the requested shape (`isForall`, `parens`)
binding `x : t` over `body`. -/
private def mkQuantifier (isForall parens : Bool)
    (x : TSyntax `ident) (t : TSyntax `term) (body : TSyntax `term) :
    MacroM (TSyntax `term) :=
  match isForall, parens with
  | true,  false => `(∀ $x:ident : $t, $body)
  | true,  true  => `(∀ ($x:ident : $t), $body)
  | false, false => `(∃ $x:ident : $t, $body)
  | false, true  => `(∃ ($x:ident : $t), $body)

partial def injectKindGuards (machineKinds eventKinds : NameSet)
    (sBinder : Name) (stx : Syntax) : MacroM Syntax := do
  let sIdent : Ident := mkIdent sBinder
  let sigLabelTy : TSyntax `term ← `(($(mkIdent `Sig)).Label)
  let asTerm (s : Syntax) : TSyntax `term := ⟨s⟩
  -- First, normalise multi-binder forms (`∀ x y : T, …`,
  -- `∀ (x : T) (y : U), …`) to nested single-binder shape.
  if let some expanded ← expandMultiBinder stx then
    return ← injectKindGuards machineKinds eventKinds sBinder expanded
  -- Single-binder rewrite: a small helper picks the right shape and
  -- splices the guard. Returns `none` if the type isn't a kind we
  -- recognise (the caller then re-emits without injection).
  let tryInject (x : TSyntax `ident) (t : TSyntax `term)
      (body' : TSyntax `term) (isForall parens : Bool) :
      MacroM (Option (TSyntax `term)) := do
    if isMachineKindIdent machineKinds t.raw then
      let isPred : Ident :=
        mkIdent (Name.mkSimple ("is_" ++ t.raw.getId.toString))
      let guard : TSyntax `term ← `(($isPred ($x).ref $sIdent))
      let combined : TSyntax `term ←
        if isForall then `($guard → $body') else `($guard ∧ $body')
      return some (← mkQuantifier isForall parens x t combined)
    if isEventKindIdent eventKinds t.raw then
      let isPred : Ident :=
        mkIdent (Name.mkSimple ("is_" ++ t.raw.getId.toString))
      let guard : TSyntax `term ← `(($isPred $x))
      let combined : TSyntax `term ←
        if isForall then `($guard → $body') else `($guard ∧ $body')
      -- Re-type the binder to `Sig.Label` — the event "type" isn't a
      -- real Lean type; it's just a tag the rewriter routes through
      -- `is_<ev>`.
      return some (← mkQuantifier isForall parens x sigLabelTy combined)
    return none
  match stx with
  | `(∀ $x:ident : $t, $body) =>
    let body' ← injectKindGuards machineKinds eventKinds sBinder body.raw
    match ← tryInject x t (asTerm body') true false with
    | some out => pure out
    | none     => `(∀ $x:ident : $t, $(asTerm body'))
  | `(∀ ($x:ident : $t), $body) =>
    let body' ← injectKindGuards machineKinds eventKinds sBinder body.raw
    match ← tryInject x t (asTerm body') true true with
    | some out => pure out
    | none     => `(∀ ($x:ident : $t), $(asTerm body'))
  | `(∃ $x:ident : $t, $body) =>
    let body' ← injectKindGuards machineKinds eventKinds sBinder body.raw
    match ← tryInject x t (asTerm body') false false with
    | some out => pure out
    | none     => `(∃ $x:ident : $t, $(asTerm body'))
  | `(∃ ($x:ident : $t), $body) =>
    let body' ← injectKindGuards machineKinds eventKinds sBinder body.raw
    match ← tryInject x t (asTerm body') false true with
    | some out => pure out
    | none     => `(∃ ($x:ident : $t), $(asTerm body'))
  | _ =>
    let args' ← stx.getArgs.mapM
      (injectKindGuards machineKinds eventKinds sBinder)
    return stx.setArgs args'

def materialiseInvariant (machineKinds eventKinds : NameSet)
    (d : PInvariantDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(invariant $id:ident : $prop:term) := stx
      | throwErrorAt stx "internal error: invariant defStx malformed"
    -- Body shape: `def <name> : GS → Prop := fun <binder> => <body>`,
    -- where `<binder>` is the user's `system <s>` name when present
    -- and the wildcard `_` otherwise (forcing state-independence;
    -- stray state references then fail loudly as `unknown identifier`).
    --
    -- The binder is built via `mkIdent` so it is UNHYGIENIC and
    -- therefore visible to the user's body. `\`(fun s => …)` would
    -- hygiene-mark `s` and break resolution.
    let sigId : Ident := mkIdent `Sig
    let gsTy : Ident := mkIdent ``PLean.GlobalState
    let binderIdent : Ident :=
      match d.stateBinder with
      | some sName => mkIdent sName
      | none       => mkIdent `_
    if d.stateBinder.isSome then
      rejectExplicitStateBinder id prop
    -- Auto-inject runtime kind guards: every `∀ n : <M>, …` becomes
    -- `∀ n : <M>, is_<M> n.ref <s> → …`. Only fires inside a
    -- `system <s> { … }` block, where the state binder is in scope.
    -- Bare top-level invariants have no `<s>` so the rewrite is skipped
    -- (and a user reference to a kind-typed binder there is a no-op
    -- anyway — the body can't touch state).
    let prop' : TSyntax `term ←
      match d.stateBinder with
      | some sName =>
        let stxOut ← liftMacroM <|
          injectKindGuards machineKinds eventKinds sName prop.raw
        pure ⟨stxOut⟩
      | none => pure prop
    let cmd ← `(command|
      def $id : ($gsTy $sigId) → Prop := fun $binderIdent => $prop')
    elabCommand cmd

def materialiseAxiom (d : PAxiomDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(paxiom $id:ident : $prop:term) := stx
      | throwErrorAt stx "internal error: paxiom defStx malformed"
    elabCommand (← `(axiom $id : $prop))

def materialiseInit (_d : PInitDecl) : CommandElabM Unit := do
  -- Per-clause materialisation is a no-op: aggregation lives in
  -- `Commands/GenModule.lean::emitInitConditions`, which folds every
  -- saved `init-holds <prop>` into `<Mod>.InitConditions : PProp Sig`.
  pure ()

def materialisePure (d : PPureDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    match stx with
    | `(function $id:ident $binders:bracketedBinder* : $ret:term = $body:term) =>
      elabCommand (← `(def $id $binders* : $ret := $body))
    | `(function $id:ident $binders:bracketedBinder* : $ret:term) =>
      elabCommand (← `(opaque $id $binders* : $ret))
    | _ => throwErrorAt stx "internal error: function defStx malformed"

def materialiseInstance (d : PInstanceDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(pinstance $id:ident : $tp:term) := stx
      | throwErrorAt stx "internal error: pinstance defStx malformed"
    elabCommand (← `(variable [$id : $tp]))

end PLean
