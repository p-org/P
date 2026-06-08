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

open Lean Elab Command

namespace PLean

/-! ## Invariant -/

syntax (name := pInvariant) "invariant " ident " : " term : command

@[command_elab pInvariant]
def elabPInvariant : CommandElab := fun stx => do
  let `(invariant $id:ident : $_:term) := stx
    | throwUnsupportedSyntax
  let _ ← requireLocalPModuleCtx "invariant"
  let ns ← getCurrNamespace
  addInvariant
    { name := id.getId, leanName := ns ++ id.getId, defStx := some stx, ref := stx }

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

/-! ## Pinstance (axiom bundle, Veil-style)

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

/-! ## Lemma / Theorem / Proof blocks (D19)

P's grammar:
  ```
  Lemma   <name> { invariant a: ...; invariant b: ...; }
  Theorem <name> { invariant safety: ...; }
  Proof   <name>? { prove <lemma> [using <l1>, ...]; prove default; }
  ```

`Lemma X` and `Theorem Y` register a *named group* of invariants; each
inner `invariant` registers as a free-standing `PInvariantDecl` so
references via `using` can resolve to the individual prop. The lemma
record itself remembers the ordered list of invariant names so it can
emit `def X.bundle : PProp Sig := fun s => P1 s ∧ P2 s` at materialisation
time.

`Proof <name>?` registers a list of `prove …` directives; multiple
`Proof` blocks accumulate. -/

declare_syntax_cat pLemmaBodyItem

syntax (name := pLemmaInvariant)
  "invariant " ident " : " term : pLemmaBodyItem

syntax (name := pLemmaDeclSyntax)
  "Lemma " ident " {" pLemmaBodyItem* "}" : command

syntax (name := pTheoremDeclSyntax)
  "Theorem " ident " {" pLemmaBodyItem* "}" : command

declare_syntax_cat pProofItem

/-- `prove <name> [using <name1>, <name2>, ...];`

    The special form `prove default;` uses the literal identifier
    `default` as `<name>`; the elaborator dispatches on the name string.
    We do NOT introduce `default` as a keyword token because that would
    break any place in the codebase that uses `default` as a term
    (e.g., `Inhabited`-generated `⟨ctor default⟩` instances). -/
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
  -- Each `invariant <id> : <prop>` inside the block is also added as a
  -- free-standing `PInvariantDecl`. We re-use the existing materialisation
  -- path so cross-references via `using` resolve to a Lean def of the
  -- name.
  for it in items do
    match it with
    | `(pLemmaBodyItem| invariant $iid:ident : $prop:term) =>
      -- Re-emit as a free-standing `invariant <name> : <prop>` syntax so
      -- the existing `materialiseInvariant` path can replay it. The
      -- `PInvariantDecl.defStx` field expects a `(invariant ... : ...)`
      -- top-level command pattern.
      let invStxReal ← `(command| invariant $iid:ident : $prop)
      addInvariant
        { name := iid.getId, leanName := ns ++ iid.getId
          defStx := some invStxReal.raw, ref := it }
      invNames := invNames.push iid.getId
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
  -- `Proof <name>? { <items>* }`. Children:
  --   stx[0] = "Proof"
  --   stx[1] = optional ident
  --   stx[2] = "{"
  --   stx[3] = items*
  --   stx[4] = "}"
  let nameOpt := stx[1]
  let nm : Name :=
    if nameOpt.getNumArgs == 1 then
      let i : Ident := ⟨nameOpt[0]⟩
      i.getId
    else
      Name.anonymous
  let items := stx[3].getArgs
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
      -- REVIEW_P3 §4.6: validate the prove-target names early so a typo
      -- surfaces a clear error at the `prove` line, not as a cryptic
      -- elaboration failure later in `pverify`. `default` is the
      -- sanity-invariant sentinel and is always valid.
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

def materialiseInvariant (d : PInvariantDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(invariant $id:ident : $prop:term) := stx
      | throwErrorAt stx "internal error: invariant defStx malformed"
    elabCommand (← `(def $id : Prop := $prop))

def materialiseAxiom (d : PAxiomDecl) : CommandElabM Unit := do
  match d.defStx with
  | none => pure ()
  | some stx =>
    let `(paxiom $id:ident : $prop:term) := stx
      | throwErrorAt stx "internal error: paxiom defStx malformed"
    elabCommand (← `(axiom $id : $prop))

def materialiseInit (_d : PInitDecl) : CommandElabM Unit := do
  -- Phase 3: per-init-clause materialisation is a no-op — the
  -- aggregation happens in `Commands/GenModule.lean` (`emitInitConditions`),
  -- which folds every saved `init-holds <prop>` into a single
  -- `<Mod>.InitConditions : PProp Sig` predicate. See PLAN_P3 D21.
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
