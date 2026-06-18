/-
PLean.Verify.CexModel — decode parsed model entries into a readable
counter-example and render it.

The solver returns the violating `GlobalState` as model functions over
the program's union types. `machines` maps a `MachineRef` to a
`MachineState.mk stage currentState fields kind` value; `sent` is a
predicate over `Label.mk target action actionCount` values, often shaped
`(or (= … lbl₁) (= … lbl₂))` with the labels in `let` bindings.

Rendering needs registry data the model doesn't carry — which machine
owns a state constructor, the field names behind a positional
`Fields.mk`, an event's payload field names. `CexNameCtx` supplies those
(built in `Obligation.lean` from the pmodule registry, passed in via
`cexNameCtxRef`). With it, a machine renders as
`Node@Act(epoch=9, held=false)` and an event as `eGrant(node=4, epoch=7)`.

Every decode is best-effort: missing context or an unexpected shape
degrades to a de-mangled raw value, so the output is never worse than
the verbatim dump.
-/
import PLean.Verify.CexParse

open Auto.Parser.SMTSexp

namespace PLean
namespace Verify

/-! ## Name context (from the registry) -/

/-- Registry-derived names the model can't supply. `stateCtors` maps a
`<machine>_<state>` constructor key to its parts; `fieldOrder` is the
global `Fields` struct order as `(machine, var)` pairs; `eventFields`
maps an event name to its payload field names in declaration order;
`refFields` is the set of payload/var field names whose type is
`MachineRef`, so their values render as machine labels. -/
structure CexNameCtx where
  stateCtors   : Array (String × String × String) := #[]
  fieldOrder   : Array (String × String) := #[]
  eventFields  : Array (String × Array String) := #[]
  refFields    : Array String := #[]
  machineKinds : Array String := #[]
  deriving Inhabited

/-- Set by `#pverify` before walking obligations; read by the renderer
when an obligation is disproved. -/
initialize cexNameCtxRef : IO.Ref (Option CexNameCtx) ← IO.mkRef none

private def CexNameCtx.lookupState (c : CexNameCtx) (key : String) :
    Option (String × String) :=
  (c.stateCtors.find? (fun (k, _, _) => k == key)).map (fun (_, m, s) => (m, s))

private def CexNameCtx.fieldsOf (c : CexNameCtx) (machine : String) :
    Array (Nat × String) := Id.run do
  let mut out : Array (Nat × String) := #[]
  for h : i in [0:c.fieldOrder.size] do
    let (m, v) := c.fieldOrder[i]'h.upper
    if m == machine then out := out.push (i, v)
  return out

private def CexNameCtx.payloadOf (c : CexNameCtx) (event : String) :
    Option (Array String) :=
  (c.eventFields.find? (fun (e, _) => e == event)).map (·.2)

private def CexNameCtx.isRefField (c : CexNameCtx) (field : String) : Bool :=
  c.refFields.contains field

/-- Maps a machine ref (a `Nat` in the model) to its kind, learned from
the `machines` table. Lets ref-typed values elsewhere render as
`<Kind>#<ref>` rather than a bare number. -/
abbrev RefKinds := Array (Int × String)

private def RefKinds.kindOf (rk : RefKinds) (r : Int) : Option String :=
  (rk.find? (·.1 == r)).map (·.2)

/-- Render a machine ref as `<Kind>#<ref>` when its kind is known, else
`#<ref>`. -/
private def renderRef (rk : RefKinds) (r : Int) : String :=
  match rk.kindOf r with
  | some k => s!"{k}#{r}"
  | none   => s!"#{r}"

/-! ## Sexp helpers -/

private def appHead : Sexp → Option String
  | .app xs =>
    match xs[0]? with
    | some (Sexp.atom (LexVal.symb h)) => some h
    | _ => none
  | _ => none

private def appArgs : Sexp → Array Sexp
  | .app xs => xs.extract 1 xs.size
  | _       => #[]

/-- A value as a `Nat`, accepting both `5` and the negated form `(- 5)`
solvers emit for `Int`-typed fields in spurious models. -/
private def asInt? : Sexp → Option Int
  | .atom (.nat n) => some (Int.ofNat n)
  | .app xs =>
    match xs[0]?, xs[1]? with
    | some (Sexp.atom (LexVal.symb "-")), some (Sexp.atom (LexVal.nat n)) =>
      some (-(Int.ofNat n))
    | _, _ => none
  | _ => none

private def isTrueVal : Sexp → Bool
  | .atom (.symb "true") => true
  | _ => false

/-- Inline SMT `let` bindings so the bound values (where solvers stash
`Label` constructors) appear at their use sites. -/
private partial def substVars (pairs : Array (String × Sexp)) : Sexp → Sexp
  | .atom (.symb v) =>
    match pairs.find? (·.1 == v) with
    | some (_, t) => t
    | none        => .atom (.symb v)
  | .atom l => .atom l
  | .app xs => .app (xs.map (substVars pairs))

private partial def substLet : Sexp → Sexp
  | .app xs =>
    match xs[0]? with
    | some (Sexp.atom (LexVal.symb "let")) =>
      match xs[1]?, xs[2]? with
      | some (Sexp.app binds), some body =>
        let pairs := binds.filterMap (fun b =>
          match b with
          | .app ys =>
            match ys[0]?, ys[1]? with
            | some (Sexp.atom (LexVal.symb v)), some t => some (v, substLet t)
            | _, _ => none
          | _ => none)
        substLet (substVars pairs body)
      | _, _ => .app (xs.map substLet)
    | _ => .app (xs.map substLet)
  | s => s

/-- Collect every `Label.mk …` subterm of a `sent`/`received` body.
Robust to the `or`-of-equalities and `ite`-chain shapes alike — the
boolean skeleton is ignored, only the label constructors matter. -/
private partial def collectLabels : Sexp → Array Sexp
  | .app xs =>
    let here : Array Sexp :=
      match xs[0]? with
      | some (Sexp.atom (LexVal.symb h)) => if h == "Label.mk" then #[.app xs] else #[]
      | _ => #[]
    xs.foldl (fun acc x => acc ++ collectLabels x) here
  | _ => #[]

/-! ## Value rendering -/

/-- Compact value printer: bare symbols/numerals/Bools, `(f a b)` for
applications. Friendlier than `Sexp.toString`, which bar-quotes every
symbol. -/
partial def renderValue : Sexp → String
  | .atom (.symb s) => s
  | .atom (.nat n)  => toString n
  | .atom (.rat n m) => s!"{n}/{m}"
  | .atom (.str s)  => "\"" ++ s ++ "\""
  | .atom (.kw s)   => ":" ++ s
  | .atom (.comment _) => ""
  | .atom .lparen   => "("
  | .atom .rparen   => ")"
  | .app xs         =>
    -- Collapse the unary-minus application `(- n)` solvers emit for
    -- negative `Int` literals into `-n`.
    match xs[0]?, xs[1]?, xs.size with
    | some (Sexp.atom (LexVal.symb "-")), some (Sexp.atom (LexVal.nat n)), 2 =>
      s!"-{n}"
    | _, _, _ =>
      "(" ++ " ".intercalate (xs.toList.map renderValue) ++ ")"

/-- Extract a machine ref as an `Int` from either a bare numeral or a
wrapper-constructor application `(<M>.mk <n>)`. A `MachineRef`-typed
field/var whose declared type is a wrapper struct stores the wrapper
form; the bare form appears when the type is the raw `MachineRef`. -/
private def asRefInt? : Sexp → Option Int
  | .app xs =>
    match xs[0]? with
    | some (Sexp.atom (LexVal.symb h)) =>
      if h == "mk" || h.endsWith ".mk" then xs[1]?.bind asInt? else asInt? (.app xs)
    | _ => asInt? (.app xs)
  | s => asInt? s

/-- Render a field value: a `MachineRef`-typed field as `<Kind>#<ref>`,
anything else with `renderValue`. -/
private def renderField (ctx : CexNameCtx) (rk : RefKinds)
    (field : String) (v : Sexp) : String :=
  if ctx.isRefField field then
    match asRefInt? v with
    | some r => renderRef rk r
    | none   => renderValue v
  else renderValue v

/-- Render a payload constructor `(<ty>.mk a b …)` against an event's
field names: `eGrant(node=Node#4, epoch=7)`. Without names (or on shape
mismatch) falls back to positional `eGrant(4, 7)`. -/
private def renderPayload (ctx : CexNameCtx) (rk : RefKinds) (event : String)
    (fields : Option (Array String)) (payload : Sexp) : String :=
  let args := appArgs payload
  if args.isEmpty then event
  else
    let parts : Array String :=
      match fields with
      | some fs =>
        if fs.size == args.size then
          (Array.zip fs args).map (fun (f, a) => s!"{f}={renderField ctx rk f a}")
        else args.map renderValue
      | none => args.map renderValue
    s!"{event}(" ++ ", ".intercalate parts.toList ++ ")"

/-- Render a label's action `(EventOrGoto.event (E.<ev> payload))` as
`<ev>(field=val, …)`. Gotos render as `goto(<payload>)`. -/
private def renderAction (ctx : CexNameCtx) (rk : RefKinds) (action : Sexp) :
    String :=
  match appHead action, (appArgs action)[0]? with
  | some "EventOrGoto.event", some ev =>
    let evName :=
      (((appHead ev).getD (renderValue ev)).splitOn ".").getLastD "?"
    match appHead ev with
    | some _ =>
      match (appArgs ev)[0]? with
      | some payload => renderPayload ctx rk evName (ctx.payloadOf evName) payload
      | none => evName
    | none =>
      -- Bare `E.<ev>` atom (no payload).
      ((renderValue ev).splitOn ".").getLastD (renderValue ev)
  | some "EventOrGoto.goto", some g => s!"goto({renderValue g})"
  | _, _ => renderValue action

/-! ## Machine decode -/

/-- One machine's decoded state. `display` is the rendered
`Machine@State(field=val, …)` (or a best-effort fallback). `isDefault`
marks the `ite` chain's `else` value, which stands for every ref not
named by an explicit case — frequently where the solver hides the
machine that violates the invariant. -/
structure MachineEntry where
  refKey    : Int
  display   : String
  isDefault : Bool := false
  deriving Inhabited

/-- The machine kind named by a `MachineState.mk`'s `currentState`, if
the state constructor is known. Drives the ref → kind map. -/
private def machineKindOf (ctx : CexNameCtx) (st : Sexp) : Option String :=
  match (appArgs st)[1]? with
  | some csVal =>
    let csName := ((renderValue csVal).splitOn ".").getLastD (renderValue csVal)
    (ctx.lookupState csName).map (·.1)
  | none => none

/-- Render a `MachineState.mk stage currentState fields kind` value.
Identifies the machine + state from `currentState`, then pairs the
`Fields.mk` positional args with the machine's var names (ref-typed vars
render as `<Kind>#<ref>`). -/
private def renderMachineState (ctx : CexNameCtx) (rk : RefKinds) (st : Sexp) :
    String :=
  let args := appArgs st
  match args[1]?, args[2]? with
  | some csVal, some fieldsVal =>
    let csName := ((renderValue csVal).splitOn ".").getLastD (renderValue csVal)
    match ctx.lookupState csName with
    | some (machine, state) =>
      let fieldArgs := appArgs fieldsVal
      let owned := ctx.fieldsOf machine
      let parts : Array String := owned.filterMap (fun (idx, vname) =>
        match fieldArgs[idx]? with
        | some v => some s!"{vname}={renderField ctx rk vname v}"
        | none   => none)
      if parts.isEmpty then s!"{machine}@{state}"
      else s!"{machine}@{state}(" ++ ", ".intercalate parts.toList ++ ")"
    | none => renderValue st
  | _, _ => renderValue st

/-- Find a def by exact (de-mangled) name. -/
private def findDef (defs : Array ModelDef) (name : String) : Option ModelDef :=
  defs.find? (fun d => d.name == name)

/-- A function body decoded as `ite`-equality cases plus a fallthrough. -/
structure FnTable where
  cases : Array (Sexp × Sexp)
  els   : Sexp
  deriving Inhabited

/-- Evaluate a decoded function at `key`: the matching case's result, or
the `else` value. -/
private def FnTable.at (t : FnTable) (key : Sexp) : Sexp :=
  match t.cases.find? (fun (k, _) => k == key) with
  | some (_, v) => v
  | none        => t.els

private def argName? (d : ModelDef) : Option String :=
  match d.args[0]? with
  | some (Sexp.app ys) =>
    match ys[0]? with
    | some (Sexp.atom (LexVal.symb nm)) => some nm
    | _ => none
  | _ => none

private def guardKey? (argName : Option String) (guard : Sexp) : Option Sexp :=
  match guard with
  | .app xs =>
    match xs[0]?, xs[1]?, xs[2]? with
    | some (Sexp.atom (LexVal.symb "=")), some a, some b =>
      match argName with
      | some nm =>
        if a == .atom (.symb nm) then some b
        else if b == .atom (.symb nm) then some a
        else some b
      | none => some b
    | _, _, _ => none
  | _ => none

private partial def decodeIte (argName : Option String) (body : Sexp) : FnTable :=
  let rec go (acc : Array (Sexp × Sexp)) (s : Sexp) : FnTable :=
    match substLet s with
    | .app xs =>
      match xs[0]?, xs[1]?, xs[2]?, xs[3]? with
      | some (Sexp.atom (LexVal.symb "ite")), some guard, some thenV, some elseV =>
        match guardKey? argName guard with
        | some key => go (acc.push (key, thenV)) elseV
        | none     => { cases := acc, els := .app xs }
      | _, _, _, _ => { cases := acc, els := .app xs }
    | leaf => { cases := acc, els := leaf }
  go #[] body

/-- Decode the `machines` table into one entry per `MachineRef`, sorted
by ref, plus the ref → kind map every other section renders against. A
first pass learns each ref's kind from its `currentState`; a second pass
renders displays with the complete map (so a machine's ref-typed `var`
resolves even to a kind learned from a different table row). -/
private def decodeMachines (ctx : CexNameCtx) (defs : Array ModelDef) :
    Array MachineEntry × RefKinds := Id.run do
  let some md := findDef defs "machines" | return (#[], #[])
  let tbl := decodeIte (argName? md) md.body
  let mut rk : RefKinds := #[]
  for (refKey, st) in tbl.cases do
    if let some k := machineKindOf ctx st then
      rk := rk.push ((asInt? refKey).getD 0, k)
  let mut entries : Array MachineEntry := #[]
  for (refKey, st) in tbl.cases do
    let r := (asInt? refKey).getD 0
    entries := entries.push { refKey := r, display := renderMachineState ctx rk st }
  let sorted := entries.qsort (fun a b => a.refKey < b.refKey)
  -- The `else` value stands for every other ref; surface it unless it is
  -- the unallocated sentinel (kind 0 / `_none`).
  let elseEntry : Array MachineEntry :=
    if machineKindOf ctx tbl.els |>.isSome then
      #[{ refKey := 0, display := renderMachineState ctx rk tbl.els, isDefault := true }]
    else #[]
  return (sorted ++ elseEntry, rk)

/-! ## Sent-trace decode -/

/-- One sent label: its `actionCount` (sort key), `target` machine ref,
rendered action, and whether `received` marks it delivered. -/
structure SentEntry where
  actionCount : Option Int
  target      : Option String
  action      : String
  delivered   : Bool
  deriving Inhabited

/-- The `received` set, as the rendered `Label.mk …` strings delivered. -/
private def receivedLabels (defs : Array ModelDef) : Array String :=
  match findDef defs "received" with
  | some d => (collectLabels (substLet d.body)).map renderValue
  | none   => #[]

/-- Decode the `sent` set: collect every `Label.mk target action
actionCount`, render each (target as `<Kind>#<ref>`), mark delivered
against `received`, sort by `actionCount`. Empty (or absent) `sent`
yields `#[]`, rendered as `[]`. -/
private def decodeSent (ctx : CexNameCtx) (rk : RefKinds) (defs : Array ModelDef) :
    Array SentEntry := Id.run do
  let some md := findDef defs "sent" | return #[]
  let labels := collectLabels (substLet md.body)
  let delivered := receivedLabels defs
  let mut entries : Array SentEntry := #[]
  let mut seen : Array String := #[]
  for lbl in labels do
    let key := renderValue lbl
    if seen.contains key then continue
    seen := seen.push key
    let args := appArgs lbl
    let target := (args[0]?.bind asInt?).map (renderRef rk)
    let action := match args[1]? with | some a => renderAction ctx rk a | none => "?"
    let ac := args[2]?.bind asInt?
    entries := entries.push
      { actionCount := ac, target, action, delivered := delivered.contains key }
  return entries.qsort (fun a b =>
    match a.actionCount, b.actionCount with
    | some x, some y => x < y
    | some _, none   => true
    | none,   some _ => false
    | none,   none   => false)

/-! ## Witnesses + top-level decode -/

/-- Names consumed by the structured sections or known solver internals,
excluded from the witnesses list. -/
private def isInternalName (name : String) : Bool :=
  name == "sent" || name == "received" || name == "machines"
  || name == "actionCount"
  || name.startsWith "is_"
  || name.endsWith "_payload_of"
  || name.endsWith "_st"
  || name.startsWith "k!"

/-- A value that is a bare uninterpreted-sort universe element
(`Type!val!0`, `@S_0`, …) carries no information — these appear when the
solver had to inhabit one of the program's union sorts. -/
private def isUniverseVal (v : String) : Bool :=
  (v.splitOn "!val!").length > 1 || v.startsWith "@"

/-- The decoded counter-example. `witnesses` are the obligation's bound
values (`this`, the event payload, skolem witnesses for a failing ∀). -/
structure CexModel where
  machines    : Array MachineEntry
  sent        : Array SentEntry
  actionCount : Option String
  witnesses   : Array (String × String)
  deriving Inhabited

/-- Render a witness value. A `Label.mk …` shows as its action; a
machine-wrapper `<Machine>.mk <ref>` (`this`, lemma binders) shows as
`<Kind>#<ref>` with the ref's state appended (so `n1 = Node#1 =
Node@Working(…)`); payload constructors (`tGrant.mk …`) render plainly.

Machine-reference annotations are kind-erased to a flat `MachineRef`
(the static wrapper type carries no runtime obligation — kind comes from
quantifier guards / invariants, as in PVerifier). So the *runtime slot*
in the `machines` table is authoritative for the kind label, not the
wrapper constructor: a `Server`-typed witness whose slot is a `Node`
renders `Node#3` (the honest, unconstrained value a missing
`const_server`-style invariant would forbid), never a contradictory
`Server#3 = Node@…`. -/
private def renderWitness (ctx : CexNameCtx) (rk : RefKinds)
    (machines : FnTable) (body : Sexp) : String :=
  match appHead body with
  | some "Label.mk" =>
    renderAction ctx rk ((appArgs body)[1]?.getD body)
  | some h =>
    let ctorKind : Option String :=
      if h.endsWith ".mk" then
        let ty := (h.splitOn ".").head!
        if ctx.machineKinds.contains ty then some ty else none
      else none
    let isWrapper := h == "mk" || ctorKind.isSome
    if isWrapper then
      match (appArgs body)[0]?.bind asInt? with
      | some r =>
        let st := machines.at (.atom (.nat r.toNat))
        let slotKind := machineKindOf ctx st
        -- Prefer the runtime slot's kind (authoritative) over the
        -- static wrapper ctor; fall back to the ref→kind map, then bare.
        let label :=
          match slotKind.orElse (fun _ => rk.kindOf r) with
          | some k => s!"{k}#{r}"
          | none   => s!"#{r}"
        if slotKind.isSome then
          s!"{label} = {renderMachineState ctx rk st}"
        else label
      | none => renderValue (substLet body)
    else renderValue (substLet body)
  | none => renderValue (substLet body)

/-- Decode all entries into a `CexModel`. Nullary, non-internal defs
become witnesses (with values rendered through `ctx`); function-typed
internal tables (`is_*`, `*_payload_of`) are dropped. -/
def CexModel.decode (ctx : CexNameCtx) (defs : Array ModelDef) : CexModel :=
  let (machines, rk) := decodeMachines ctx defs
  let machTbl :=
    match findDef defs "machines" with
    | some md => decodeIte (argName? md) md.body
    | none    => { cases := #[], els := .atom (.symb "?") }
  let sent := decodeSent ctx rk defs
  let actionCount := (findDef defs "actionCount").map (fun d => renderValue d.body)
  let witnesses := defs.filterMap (fun d =>
    if d.args.isEmpty && !isInternalName d.name then
      let val := renderWitness ctx rk machTbl d.body
      if isUniverseVal val then none else some (d.name, val)
    else none)
  { machines, sent, actionCount, witnesses }

/-! ## Rendering -/

private def renderSentEntry (e : SentEntry) : String :=
  let ac := match e.actionCount with | some n => s!"@{n}" | none => "@?"
  let tgt := match e.target with | some t => s!" → {t}" | none => ""
  let mark := if e.delivered then " [delivered]" else ""
  s!"  {ac}{tgt}  {e.action}{mark}"

/-- Render a decoded model. The `sent` section always prints (`[]` when
empty); machine / witness / counter sections print only when populated. -/
def CexModel.render (m : CexModel) : String := Id.run do
  let mut lines : Array String := #[]
  unless m.machines.isEmpty do
    lines := lines.push "machines:"
    for me in m.machines do
      let key := if me.isDefault then "else" else toString me.refKey
      lines := lines.push s!"  machine[{key}] = {me.display}"
  if m.sent.isEmpty then
    lines := lines.push "sent (ordered by actionCount): []"
  else
    lines := lines.push "sent (ordered by actionCount):"
    for se in m.sent do
      lines := lines.push (renderSentEntry se)
  match m.actionCount with
  | some ac => lines := lines.push s!"actionCount = {ac}"
  | none    => pure ()
  unless m.witnesses.isEmpty do
    lines := lines.push "witnesses (handler & skolem bindings):"
    for (n, v) in m.witnesses do
      lines := lines.push s!"  {n} = {v}"
  return "\n".intercalate lines.toList

/-- Whether the decode produced any structured content beyond the
always-present `sent` line. -/
def CexModel.nonEmpty (m : CexModel) : Bool :=
  !m.machines.isEmpty || !m.sent.isEmpty || m.actionCount.isSome
  || !m.witnesses.isEmpty

/-- End-to-end: parse a model string and render it against `ctx`.
Returns `none` when the text doesn't parse or decodes to nothing, so the
caller can fall back to the cleaned raw dump. -/
def renderModelText (modelText : String) (ctx : CexNameCtx := {}) :
    Option String := do
  let defs ← parseModel modelText
  let model := CexModel.decode ctx defs
  if model.nonEmpty then some model.render else none

end Verify
end PLean
