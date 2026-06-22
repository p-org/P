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

/-- Reject any binder that introduces a fresh `GlobalState`-typed
identifier inside a `system <s> { … }` invariant body. Such a binder
shadows the outer `<s>` for the body it scopes, decoupling the invariant
from the per-handler state and letting a *false* property verify as a
clean pass (the 2026-06-10 soundness hole).

The original guard matched only the six leading `∀ … : GlobalState …`
shapes, so a `let`/`have`/`fun`/`λ`/`∃` binder of the same type — e.g.
`let s : GlobalState Sig := default; …` or `∃ s : GlobalState Sig, …` —
slipped past and re-opened the hole. Rather than enumerate every binder
form, we reject *any* occurrence of the `GlobalState` / `PLean.GlobalState`
type identifier in the body: a well-formed `system`-block invariant never
needs to name the state type (it references the bound `s` instead), so
this is a sound over-approximation that cannot be evaded by a new binder
syntax. The walk is recursive over the body `Syntax` and surfaces the
offending occurrence for `throwErrorAt`.

A bare top-level `invariant standalone : ∀ s : GlobalState Sig, P` is
still allowed — the materialiser uses a wildcard binder there, so there
is no `system`-block `s` to shadow; this check only runs when an
enclosing `system` block has registered a state binder. -/
private partial def containsExplicitStateBinder (stx : Syntax) : Option Syntax := Id.run do
  -- A bare reference to the state type — `GlobalState` or its qualified
  -- form `PLean.GlobalState` — anywhere in the body is the trigger.
  -- Matching the identifier (rather than a particular binder shape)
  -- catches `∀`/`∃`/`let`/`have`/`fun`/`λ` uniformly.
  if stx.isIdent then
    let n := stx.getId
    if n == ``PLean.GlobalState || n == `GlobalState
        || n == `PLean.GlobalState then
      return some stx
  for child in stx.getArgs do
    if let some hit := containsExplicitStateBinder child then
      return some hit
  return none

private def rejectExplicitStateBinder (id : Ident) (prop : TSyntax `term) :
    CommandElabM Unit := do
  match containsExplicitStateBinder prop with
  | some hit =>
    throwErrorAt hit m!"invariant `{id.getId}` lives inside a `system` block \
      but its body names the `GlobalState` type. A `∀`/`∃`/`let`/`have`/`fun` \
      binder of type `GlobalState <Sig>` shadows the outer `system` state \
      binder and silently decouples the invariant from per-handler state \
      (the soundness hole fixed 2026-06-10). Reference the `system`-block's \
      state binder directly in the body instead of introducing a new \
      `GlobalState`-typed variable."
  | none => pure ()

/-- Public guard for `init-holds` bodies (and any other state-bound
prop materialised with a hardcoded `s` binder): reject a body that names
the `GlobalState` type, since such a binder shadows the `s` the
materialiser introduces. Same soundness rationale as
`rejectExplicitStateBinder` for `system`-block invariants. -/
def rejectStateShadowIn (what : String) (prop : Syntax) : CommandElabM Unit := do
  match containsExplicitStateBinder prop with
  | some hit =>
    throwErrorAt hit m!"{what} body names the `GlobalState` type. A \
      `∀`/`∃`/`let`/`have`/`fun` binder of type `GlobalState <Sig>` shadows \
      the materialiser's state binder and decouples the clause from the \
      actual state (a soundness hole). Reference the state directly rather \
      than introducing a new `GlobalState`-typed variable."
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

/-! ## Field-projection sugar

Inside a `system <s> { … }` block, an invariant body may project
machine fields and event payload fields with the abbreviated dot
syntax `<binder>.<field>`. The materialiser walks the body and rewrites:

| User wrote | Materialiser emits |
|---|---|
| `n.<v>`  (where `n : <M>`, `<v>` is a machine var)        | `(<s>.machines n.ref).fields.<M>_<v>` |
| `e.<f>`  (where `e : <ev>`, `<f>` is a payload field)     | `(<ev>_payload_of e).<f>` |

Projections whose LHS doesn't track a machine / event kind, or whose
field name isn't registered, are left alone — `n.ref`, `e.action`,
`s.machines`, etc. still resolve through Lean's regular projection.

The pass runs **before** `injectKindGuards` so it sees the original
quantifier types (`∀ e : <ev>, …`); kind-guard injection retypes the
event binder to `Sig.Label` and would defeat the lookup. Bare top-level
invariants (no `system` block) skip this rewrite. -/

inductive KindRef where
  | machine (name : Name)
  | event   (name : Name)
  deriving Inhabited

/-! ### Quantifier binder collection.

Two surface shapes carry quantifier binders, and they nest the
`(binderIdent, type)` pair at different child positions:

* `Lean.Parser.Term.forall` — Lean's primitive `∀ x : T, body`. Args:
  `[«∀», binders, typeSpec, «,», body]`. Binder idents at `[1]`,
  shared type at `[2]` (a `typeSpec` whose `[1]` is the type term).

* `«term∀_,_»` / `«term∃_,_»` — mathlib's `notation`-defined
  quantifier macros. Args: `[keyword, explicitBinders, «,», body]`.
  The `explicitBinders` at `[1]` houses an `unbracketedExplicitBinders`
  whose `[0]` is a `null` of `Lean.binderIdent`s and `[1]` is a `null`
  containing `[«:», T]` when typed.

The macro kinds aren't reachable via `Name`-quotation (they're created
inside mathlib's notation file), so we keep them as simple-name
literals in one place and check membership via the `quantNotationKinds`
set. -/

private def quantNotationKinds : NameSet :=
  ({} : NameSet)
    |>.insert (Name.mkSimple "term∀_,_")
    |>.insert (Name.mkSimple "term∃_,_")

private partial def collectMacroQuantBinders : Syntax → Array (Syntax × Syntax)
  | s =>
    if s.getKind == ``Lean.unbracketedExplicitBinders then
      -- `unbracketedExplicitBinders` is `(ppSpace binderIdent)+ (" : " term)?`.
      -- `s[0]` is the `null` of binderIdents; `s[1]` is the optional
      -- type-annotation `null` — present-and-typed iff its children are
      -- `[«:», T]` (numArgs == 2), so `s[1][1]` is the type term.
      let ids := s[0].getArgs
      let tyOpt := s[1]
      if tyOpt.getNumArgs == 2 then
        let ty := tyOpt[1]
        ids.map fun i =>
          let x := if i.getKind == ``Lean.binderIdent then i[0] else i
          (x, ty)
      else #[]
    else s.getArgs.foldl (init := #[]) fun acc c =>
      acc ++ collectMacroQuantBinders c

/-- Return every `(binderIdent, typeIdent)` pair this quantifier
introduces. Returns `#[]` for non-quantifier syntax so the caller can
unconditionally fold the result into the kind environment. -/
private def collectQuantifierBinderPairs (stx : Syntax) :
    Array (Syntax × Syntax) :=
  let k := stx.getKind
  if k == ``Lean.Parser.Term.forall then
    collectBinderPairs stx[1].getArgs stx[2]
  else if quantNotationKinds.contains k then
    collectMacroQuantBinders stx[1]
  else
    #[]

private def buildFieldProjection
    (machineFields : NameMap NameSet)
    (eventPayloadFields : NameMap NameSet)
    (sBinder : Name) (binder : Ident) (field : Name) (kind : KindRef) :
    MacroM (Option (TSyntax `term)) := do
  match kind with
  | .machine mName =>
    let some flds := machineFields.find? mName | return none
    unless flds.contains field do return none
    let sIdent : Ident := mkIdent sBinder
    let qualField : Ident :=
      mkIdent (Name.mkSimple (mName.toString ++ "_" ++ field.toString))
    let out : TSyntax `term ←
      `((($sIdent).machines ($binder).ref).fields.$qualField:ident)
    return some out
  | .event evName =>
    let some flds := eventPayloadFields.find? evName | return none
    unless flds.contains field do return none
    let extractor : Ident :=
      mkIdent (Name.mkSimple (evName.toString ++ "_payload_of"))
    let fldId : Ident := mkIdent field
    let out : TSyntax `term ←
      `(($extractor $binder).$fldId:ident)
    return some out

private partial def rewriteFieldProjectionsAux
    (machineKinds eventKinds : NameSet)
    (machineFields : NameMap NameSet)
    (eventPayloadFields : NameMap NameSet)
    (sBinder : Name) (kindEnv : NameMap KindRef)
    (stx : Syntax) : MacroM Syntax := do
  -- Two surface shapes carry a `<binder>.<field>` projection:
  -- (1) explicit `Lean.Parser.Term.proj` (`stx[0]` ident, `stx[2]` ident);
  -- (2) a plain `ident` whose hierarchical name is the dotted form
  --     (`n1.has_lock` parses as a single Name `.str (.str .anonymous "n1") "has_lock"`
  --     when `n1` is a quantifier-bound name — not a constant — which
  --     is the common case for invariant bodies).
  if stx.getKind == ``Lean.Parser.Term.proj then
    let lhs := stx[0]
    let fldStx := stx[2]
    if lhs.isIdent && fldStx.isIdent then
      let bName := lhs.getId
      let fName := fldStx.getId
      if let some kind := kindEnv.find? bName then
        let bIdent : Ident := ⟨lhs⟩
        let rebuilt? ← buildFieldProjection
          machineFields eventPayloadFields sBinder bIdent fName kind
        if let some out := rebuilt? then
          return out.raw
  if stx.isIdent then
    match stx.getId with
    | .str (.str .anonymous head) field =>
      let headN := Name.mkSimple head
      let fieldN := Name.mkSimple field
      if let some kind := kindEnv.find? headN then
        let bIdent : Ident := mkIdent headN
        let rebuilt? ← buildFieldProjection
          machineFields eventPayloadFields sBinder bIdent fieldN kind
        if let some out := rebuilt? then
          return out.raw
    | _ => pure ()
  -- Extend `env` for any quantifier-bound binder whose type is a
  -- registered machine / event kind. `collectQuantifierBinderPairs`
  -- handles both quantifier surface shapes (Lean's `Term.forall` and
  -- mathlib's `«term∃_,_»`/`«term∀_,_»` macros) — see its docstring.
  let mut env := kindEnv
  for (xRaw, tRaw) in collectQuantifierBinderPairs stx do
    if xRaw.isIdent && tRaw.isIdent then
      let tName := tRaw.getId
      if machineKinds.contains tName then
        env := env.insert xRaw.getId (.machine tName)
      else if eventKinds.contains tName then
        env := env.insert xRaw.getId (.event tName)
  let args' ← stx.getArgs.mapM
    (rewriteFieldProjectionsAux machineKinds eventKinds machineFields
      eventPayloadFields sBinder env)
  return stx.setArgs args'

def rewriteFieldProjections
    (machineKinds eventKinds : NameSet)
    (machineFields : NameMap NameSet)
    (eventPayloadFields : NameMap NameSet)
    (sBinder : Name) (stx : Syntax) : MacroM Syntax :=
  rewriteFieldProjectionsAux machineKinds eventKinds machineFields
    eventPayloadFields sBinder {} stx

def materialiseInvariant (machineKinds eventKinds : NameSet)
    (machineFields : NameMap NameSet)
    (eventPayloadFields : NameMap NameSet)
    (d : PInvariantDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(invariant $id:ident : $prop:term) := stx
      | throwErrorAt stx "internal error: invariant defStx malformed"
    let sigId : Ident := mkIdent `Sig
    let gsTy : Ident := mkIdent ``PLean.GlobalState
    let binderIdent : Ident :=
      match d.stateBinder with
      | some sName => mkIdent sName
      | none       => mkIdent `_
    if d.stateBinder.isSome then
      rejectExplicitStateBinder id prop
    let prop' : TSyntax `term ←
      match d.stateBinder with
      | some sName =>
        let rewritten ← liftMacroM <|
          rewriteFieldProjections machineKinds eventKinds
            machineFields eventPayloadFields sName prop.raw
        let stxOut ← liftMacroM <|
          injectKindGuards machineKinds eventKinds sName rewritten
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

/-- Materialise `pinstance <id> : <Class> <T>`.

The original design ([`docs/PLAN_P0.md`](../../docs/PLAN_P0.md) §6,
following Veil) elaborated this to a single `variable [<id> : <Class>
<T>]`, but that has two SMT-visibility gaps:

1. **Auto-bound generalisation.** A later `invariant P : <body using
   Class.field>` gets type `[ord : <Class> <T>] → GS → Prop`, not
   `GS → Prop`. The bundle predicate `def L : GS → Prop := fun s =>
   P s ∧ True` then can't typecheck (`P s` is missing the instance
   argument) and the obligation skeleton renders as `sorry ∧ True`.
2. **Lctx invisibility.** `loom_smt [*]` collects only the local
   context, so even if the invariant body elaborated, the class's
   stated axioms about its functions would be invisible to SMT.

We close (1) by elaborating `pinstance <id> : <Class> <T>` into a
top-level `axiom` for the instance plus a real `instance` that wraps
it, so `<Class>.<field>` resolves via typeclass resolution without
inserting an explicit binder on the invariant's type. (2) is closed
by the per-field axiom synthesis in `emitInstanceAxioms` (below) —
called by `#gen_module` after `materialiseInstance` runs so the
synthesised axiom names land in `ctx.axioms` and flow through the
existing `paxiom → have hax_<name>` SMT bridge. -/
def materialiseInstance (d : PInstanceDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(pinstance $id:ident : $tp:term) := stx
      | throwErrorAt stx "internal error: pinstance defStx malformed"
    -- (1) + (2) wrapped in a `noncomputable section` so the synthesised
    -- witness — neither computational (it's axiomatic) nor inhabited
    -- in general — passes Lean's codegen check. `axiom <id> : <tp>`
    -- alone trips the code generator with "not supported by code
    -- generator"; `opaque` requires `Inhabited <tp>` (not provided for
    -- arbitrary classes); `noncomputable section` is the canonical
    -- escape hatch.
    elabCommand (← `(noncomputable section))
    -- Axiomatise the instance witness, then wrap it in a real
    -- `instance` (anonymous form — found by structural typeclass
    -- lookup; name doesn't matter) so `<Class>.<field>` resolves
    -- automatically in subsequent declarations.
    elabCommand (← `(axiom $id : $tp))
    elabCommand (← `(instance : $tp := $id))
    elabCommand (← `(end))

/-- Per-field axiom synthesis for `pinstance <id> : <Class> <T>`.

For every Prop-typed field of `<Class>`, emit a top-level
`noncomputable def <Mod>.<id>_<field> := @<Class>.<field> _ <id>`,
returning the array of registered theorem names so the caller can add
them to `ctx.axioms`. The obligation generator's existing
`paxiom → have hax_<name>` SMT bridge then carries them into every
VC's local context.

`modName` is the owning pmodule's full name; the witness `<id>` lives
under `modName.<id>` after `materialiseInstance` ran.

Called by `#gen_module` directly, *after* `materialiseInstance` has
emitted the instance and after the pmodule block has been closed — so
we cannot call `addAxiom` (which requires being inside a pmodule
block). Instead, return the names and let the caller update the
persistent `LocalPModuleCtx` via `setPModule`. -/
def synthInstanceFieldAxioms (modName : Name) (d : PInstanceDecl) :
    CommandElabM (Array PAxiomDecl) := do
  match d.defStx with
  | none => return #[]
  | some stx =>
    let `(pinstance $_id:ident : $tp:term) := stx
      | return #[]
    -- Elaborate the class application to find its head constant.
    let classNameAndArgs? ←
      try
        Lean.Elab.Command.liftTermElabM do
          let tpExpr ← Lean.Elab.Term.elabTerm tp.raw none
          let tpExpr ← Lean.instantiateMVars tpExpr
          let head := tpExpr.getAppFn
          match head.constName? with
          | some n => return some (n, tpExpr.getAppArgs)
          | none   => return none
      catch _ => pure none
    let some (className, _classArgs) := classNameAndArgs?
      | return #[]
    let env ← getEnv
    let some classInfo := Lean.getStructureInfo? env className
      | return #[]
    let instWitness : Ident := mkIdent (modName ++ d.name)
    let mut out : Array PAxiomDecl := #[]
    for fname in classInfo.fieldNames do
      let projName : Name := className ++ fname
      let some _ := env.find? projName | continue
      let thmName : Name :=
        Name.mkSimple (d.name.toString ++ "_" ++ fname.toString)
      let thmId : Ident := mkIdent thmName
      let projId : Ident := mkIdent projName
      let projInstantiated : Term ← `(@$projId _ $instWitness)
      let cmd ← `(
        set_option linter.unusedVariables false in
        noncomputable def $thmId := $projInstantiated)
      let okElaborated : Bool ← (do
        try
          elabCommand cmd
        catch _ => return false
        let env' ← getEnv
        match env'.find? (modName ++ thmName) with
        | some info =>
          try
            let isProp ← Lean.Elab.Command.liftTermElabM do
              Lean.Meta.isProp info.type
            return isProp
          catch _ => return false
        | none => return false)
      if okElaborated then
        out := out.push
          { name := thmName, leanName := modName ++ thmName
            defStx := none, ref := stx }
    return out

end PLean
