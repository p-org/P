/-
PLean.Commands.PVerify — `#pverify M`: emit the proof obligations of
pmodule `M` (docs/TransitionModel.md §4), discharge each with
`pverify_close`, and assemble the safety theorems:

  M.safe      : ∀ g, GReachable prog .bag  InitConditions g → AllInv g ∧ g.error = none
  M.safe_fifo : ∀ g, GReachable prog .fifo InitConditions g → AllInv g ∧ g.error = none
  M.<T>_holds : ∀ g, GReachable prog .fifo InitConditions g → T g

`AllInv` is the conjunction of every proved target (plus the automatic
invariants `NoHalt` and `Transient` when `#gen_module` emitted them). Per target `T` (`prove T using U₁ …`), with
`Pre := T ∧ U₁ ∧ …` and `Post := T`:

* `<M>.<S>.<ev>_correct_<T>` per handled / ignored event of a state,
* `<M>.<S>.entry_correct_<T>` per state,
* `drop_correct_<T>` if a machine can halt,
* `base_<T>`: `InitConditions g → T g`

(these assume the handler does not fail); and, assuming `AllInv`, once per
module:

* `<M>.<S>.<ev>_safe` / `<M>.<S>.entry_safe`: the handler never fails
  (`assert`, `choose`, …),
* `<M>.<S>.<ev>_never` per event a state neither handles, ignores nor
  defers.

Each proved obligation is reported with how its goals were closed (`[norm]`
when none was left after `vcgen` and normalisation; else how many needed
an SMT solver, were closed by crush before SMT translation, or were
hypotheses). In the editor, one message shows the progress, updated in
place: the obligations done, the one being proved, and the goal and solver
it is on.

A failing obligation is reported (with its time, and a counterexample
when z3 finds one: the invariant that fails, the step, and a minimised
pre-state, see `PLean.Cex`) and left as `sorry`, so the remaining
obligations and the assembly still run.

Obligations are proved in parallel, `pverify.jobs` at a time (default: one
per hardware thread), each kernel-checked on its own copy of the
environment; the report keeps their order.

The report ends with what `M.safe` assumes beyond Lean's axioms (`paxiom`s,
`pinstance`s, the solver's answers in trusted obligations, `sorry`), and
warns when `InitConditions` is unsatisfiable.
-/
import Lean
import PLean.Commands.GenModule
import PLean.Verify.Tactic
import PLean.Verify.Progress

open Lean Elab Command

namespace PLean

register_option pverify.failOnIncomplete : Bool := {
  defValue := true
  descr := "Make `#pverify` an error when some obligation is not proved." }

register_option pverify.report : String := {
  defValue := ""
  descr := "Where `#pverify` writes its full report (default `.lake/pverify/<Mod>.txt`)." }

register_option pverify.timeout : Nat := {
  defValue := 20
  descr := "`crush.timeout` (seconds) for each obligation." }

register_option pverify.showTimes : Bool := {
  defValue := true
  descr := "Show per-obligation times in `#pverify`'s report (off for deterministic tests)." }

register_option pverify.only : String := {
  defValue := ""
  descr := "Comma-separated name fragments: run only the obligations whose name contains one \
            (and skip the assembly). For iterating on a few obligations." }

register_option pverify.jobs : Nat := {
  defValue := 0
  descr := "How many obligations `#pverify` proves at once (0: the environment variable \
            `PVERIFY_JOBS`, or else one per hardware thread)." }

register_option pverify.progress : Bool := {
  defValue := true
  descr := "Show `#pverify`'s progress in the editor (one message, updated in place)." }

register_option pverify.trust : String := {
  defValue := "reconstructOrTrust"
  descr := "`crush.trust` for each obligation: reconstruct (checked proofs only), \
            reconstructOrTrust (checked when crush can replay the proof, otherwise trust the \
            solver; reported as `trusted`), or trust." }

/-- The kind of a failed obligation, from its error message. -/
def failureKind (msg : String) : String :=
  let has (s : String) := (msg.splitOn s).length > 1
  if has "returned `sat`" || has "found a model" then "counterexample"
  else if has "reconstruction failed" then "reconstruct-failed"
  else if has "`unknown`" || has "timeout" then "unknown"
  else "error"

/-- One obligation's outcome. -/
structure ObResult where
  name : Name
  ok   : Bool
  ms   : Nat
  msg  : String := ""
  /-- The goals `crush` failed on (report file only). -/
  goals : Array String := #[]
  /-- The full error, e.g. the solver's model (report file only). -/
  fullMsg : String := ""
  /-- Proved, but some step trusts the solver (no checked reconstruction). -/
  trusted : Bool := false
  /-- Per-goal times (`pverify.profile`, report file only). -/
  notes : Array String := #[]
  /-- The invariant of the goal that was not proved, if known. -/
  inv : Option Name := none
  /-- The decoded counterexample (`PLean.Cex.render`). -/
  cex : Array String := #[]
  /-- The SMT script and z3 model it was decoded from. -/
  smt : String := ""
  /-- How its goals were closed: by a hypothesis, by crush before SMT
  translation, by an SMT solver (`none`: no goal was left after `vcgen` and
  normalisation). -/
  stats : Option (Nat × Nat × Nat) := none
  /-- Time in `vcgen` and normalisation, and in crush (ms). -/
  normMs : Nat := 0
  crushMs : Nat := 0
  /-- Proved by a `@[pverifyProof]` theorem. -/
  manual : Bool := false
  /-- The statement of a failed obligation, as a `@[pverifyProof]` skeleton. -/
  skeleton : String := ""

/-- How an obligation's goals were closed: `[norm]` when none was left after
`vcgen` and normalisation, otherwise the number closed by an SMT solver, by
crush before SMT translation, and by a hypothesis. -/
def ObResult.route (r : ObResult) : String :=
  match r.stats with
  | none => "[norm]"
  | some (h, p, s) =>
    if h + p + s == 0 then "[norm]"
    else
      let parts := [(s, "smt"), (p, "pre"), (h, "hyp")].filterMap fun (n, l) =>
        if n == 0 then none else some s!"{l} {n}"
      "[" ++ ", ".intercalate parts ++ "]"

/-- The report line of an obligation. -/
def ObResult.line (r : ObResult) (showTimes : Bool) : String :=
  let kind := if !r.cex.isEmpty then "counterexample" else failureKind r.msg
  let status := if !r.ok then s!"✗ [{kind}]"
    else if r.manual then (if r.trusted then "✓ (manual, trusted)" else "✓ (manual)")
    else if r.trusted then "✓ (trusted)" else "✓"
  let tm := if showTimes then s!" ({r.ms} ms)" else ""
  let detail := if !r.ok then (if r.cex.isEmpty then ": " ++ r.msg else "")
    else if r.manual then "" else s!" {r.route}"
  s!"{status} {r.name}{tm}{detail}"

/-- Elaborate `cmd`, returning its error messages and the goals `crush`
failed on (both removed from the log). -/
private structure Captured where
  errs : Array String := #[]
  goals : Array String := #[]
  trusted : Bool := false
  notes : Array String := #[]
  inv : Option Name := none
  script : String := ""
  model : String := ""
  stats : Option (Nat × Nat × Nat) := none
  normMs : Nat := 0
  crushMs : Nat := 0

/-- Elaborate `cmd`, returning its error messages and what `pverify_close`
logged (all removed from the log). -/
private def elabCaptureGoals (cmd : Syntax) : CommandElabM Captured := do
  let saved := (← get).messages
  modify fun s => { s with messages := {} }
  elabCommand cmd
  let msgs := (← get).messages
  let mut c : Captured := {}
  for m in msgs.toList do
    let s ← m.data.toString
    if m.severity == .error then c := { c with errs := c.errs.push s }
    else if s.startsWith "pverify-goal:" then c := { c with goals := c.goals.push s }
    else if s.startsWith "pverify-inv: " then
      c := { c with inv := some ((s.drop "pverify-inv: ".length).toString.toName) }
    else if s.startsWith "pverify-smt-script:\n" then
      c := { c with script := (s.drop "pverify-smt-script:\n".length).toString }
    else if s.startsWith "pverify-smt-model:\n" then
      c := { c with model := (s.drop "pverify-smt-model:\n".length).toString }
    else if s.startsWith "pverify-stats: " then
      match ((s.drop "pverify-stats: ".length).toString.splitOn " ").map String.toNat? with
      | [some h, some p, some n, some tn, some tc] =>
        c := { c with stats := some (h, p, n), normMs := tn, crushMs := tc }
      | _ => pure ()
    else if s.startsWith "pverify-time:" || s.startsWith "crush profile" then
      c := { c with notes := c.notes.push s }
    else if (s.splitOn "crushSorry").length > 1 then c := { c with trusted := true }
  modify fun s => { s with messages := saved }
  return c

private def elabCapture (cmd : Syntax) : CommandElabM (Array String) :=
  return (← elabCaptureGoals cmd).errs

private def excerpt (s : String) (n : Nat := 300) : String :=
  let s := s.replace "\n" " "
  if s.length > n then (s.take n).toString ++ "…" else s

/-- `t₁ g ∧ … ∧ tₙ g` (`True` when empty). -/
private def conjAt (ts : Array Name) (g : Ident) : CommandElabM (TSyntax `term) := do
  match ts.back? with
  | none => `(True)
  | some last =>
    ts.pop.foldrM (init := ← `($(mkIdent last) $g)) fun t acc => `($(mkIdent t) $g ∧ $acc)

/-- Statement prefix shared by deliver obligations. -/
private def deliverHyps (g i src dst : Ident) (pre : TSyntax `term) (evVal : TSyntax `term)
    (tag : Ident) : CommandElabM (Array (TSyntax `term)) := do
  return #[← `(PLean.Framework $g), ← `(($g).error = none), pre,
    ← `(($g).net $i = some ⟨$src, $dst, $evVal⟩),
    ← `(($g).log $i = some ⟨$src, $dst, $evVal⟩),
    ← `($i < ($g).clock),
    ← `((($g).machines $dst).state = some $tag),
    ← `((($g).machines $dst).pending = none),
    ← `((($g).machines $dst).waiting = none),
    ← `((($g).machines $dst).halted = false)]

private def arrows (hyps : Array (TSyntax `term)) (concl : TSyntax `term) :
    CommandElabM (TSyntax `term) :=
  hyps.foldrM (init := concl) fun h acc => `($h → $acc)

/-- Why the `@[pverifyProof]` theorem `full` does not prove obligation `cmd`
(`theorem <name> <binders> : <statement> := …`), if it does not: its
statement must be the obligation's (up to definitional unfolding). -/
private def manualMismatch? (full : Name) (cmd : Syntax) : CommandElabM (Option String) := do
  let `(command| theorem $_:ident $bs:bracketedBinder* : $ty := $_) := cmd
    | return some "internal: unexpected obligation shape"
  let some ci := (← getEnv).find? full | return some s!"`{full}` is missing"
  liftTermElabM do
    let expected ← Term.elabBinders bs fun xs => do
      let t ← Term.elabType ty
      Term.synthesizeSyntheticMVarsNoPostponing
      Meta.mkForallFVars xs t
    let expected ← instantiateMVars expected
    -- the same binders, explicit as the obligation's (the assembly applies it)
    let rec sameBinders : Expr → Expr → Bool
      | .forallE _ _ a bi, .forallE _ _ b bi' => bi == bi' && sameBinders a b
      | _, _ => true
    if (← Meta.isDefEq ci.type expected) && sameBinders ci.type expected then return none
    return some s!"the statement of `@[pverifyProof]` theorem `{full}` is not the obligation's, \
      which is: {← Meta.ppExpr expected}"

/-- Why the axioms the `@[pverifyProof]` theorem `full` uses make it no proof,
if they do: `sorry`, or an axiom the module does not declare (Lean's own and
the solver's trust are allowed; the latter makes it trusted). -/
private def manualAxiomsProblem? (ctx : LocalPModuleCtx) (modName full : Name) :
    CommandElabM (Option String) := do
  let axs ← collectAxioms full
  if axs.contains ``sorryAx then return some s!"`{full}` uses `sorry`"
  let declared (ax : Name) : Bool :=
    let short := ax.replacePrefix modName .anonymous
    modName.isPrefixOf ax && (ctx.axioms.contains short || ctx.instances.contains short)
  let other := axs.filter fun ax =>
    !(ax == ``propext || ax == ``Classical.choice || ax == ``Quot.sound ||
      ax == ``Crush.crushSorry || declared ax)
  if other.isEmpty then return none
  return some s!"`{full}` uses {", ".intercalate (other.toList.map (s!"`{·}`"))}, \
    which the module does not declare"

/-- A `@[pverifyProof]` skeleton for the obligation `full`, from its statement. -/
private def skeletonOf (obName full : Name) : CommandElabM String := do
  let some ci := (← getEnv).find? full | return ""
  let ty ← liftTermElabM (Meta.ppExpr ci.type)
  let ty := "\n".intercalate ((toString ty).splitOn "\n" |>.map ("    " ++ ·))
  return s!"@[pverifyProof] theorem {obName} :\n{ty} := by\n  sorry"

/-- `pverify_auto [M]`, for a manual proof of one of `M`'s obligations:
`pverify_close` with the module's definitions, hints and facts (`M` is the
innermost enclosing namespace that is a `pmodule` when not given). It
unfolds the module's handlers that occur in the goal, and leaves open the
goals the solvers cannot prove, each tagged with its invariant
(`case pv_inv.<invariant> => …`), for the proof to finish. -/
syntax (name := pverifyAuto) "pverify_auto" (ppSpace colGt ident)? : tactic

open Lean Elab Tactic in
@[tactic pverifyAuto] def evalPverifyAuto : Tactic := fun stx => do
  let env ← getEnv
  let modules := pmoduleExt.getState env
  let given? : Option Name := (stx.getArgs.extract 1 stx.getNumArgs).findSome? fun a =>
    if a.isIdent then some a.getId
    else a.getArgs.findSome? fun b => if b.isIdent then some b.getId else none
  let modName ← match given? with
    | some m => pure m
    | none => do
      let mut ns ← getCurrNamespace
      let mut found : Option Name := none
      while !ns.isAnonymous do
        if modules.contains ns then
          found := some ns
          break
        ns := ns.getPrefix
      let some m := found
        | throwError "pverify_auto: not inside the namespace of a pmodule; name it, `pverify_auto M`"
      pure m
  let some ctx := modules.find? modName | throwError "pverify_auto: no `pmodule {modName}`"
  let (defs, hints, facts) := closeLists ctx env modName
  let full (n : Name) : Ident := mkIdent (modName ++ n)
  -- the module's handlers in the goal (definitions of type `… → HM Unit`)
  let hm := modName ++ `HM
  let rec codomain : Expr → Expr
    | .forallE _ _ b _ => codomain b
    | e => e
  let goal ← instantiateMVars (← getMainTarget)
  let handlers := goal.getUsedConstants.filter fun c =>
    modName.isPrefixOf c && (env.find? c).any fun ci => (codomain ci.type).getAppFn.constName? == some hm
  let hs : Array Ident := handlers.map mkIdent
  let ds : Array Ident := defs.map full
  let ls : Array (TSyntax `term) := hints.map fun n => ⟨full n⟩
  let fs : Array (TSyntax `term) := facts.map fun n => ⟨full n⟩
  -- `#pverify`'s settings for an obligation
  let opts ← getOptions
  let timeout := Syntax.mkNumLit (toString (pverify.timeout.get opts))
  let trust := Syntax.mkStrLit (pverify.trust.get opts)
  evalTactic (← `(tactic|
    set_option maxHeartbeats 0 in
    set_option experimental.vcgen true in
    set_option crush.backend "cvc5" in
    set_option crush.timeout $timeout:num in
    set_option crush.trust $trust:str in
    set_option pverify.leaveGoals true in
    set_option pverify.cex false in
    pverify_close [$[$hs],*] [$[$ds],*] [$[$ls],*] [$[$fs],*]))

syntax (name := pVerify) "#pverify " ident : command

private def pverifyBody (stx : Syntax) : CommandElabM Unit := do
  let `(#pverify $name:ident) := stx | throwUnsupportedSyntax
  let modName := name.getId
  let some ctx ← getPModule? modName | throwError "no `pmodule {modName}` is registered"
  unless ctx.machines.toList.all (·.2.materialised) do
    throwError "run `#gen_module {modName}` before `#pverify {modName}`"
  let env ← getEnv
  let opts ← getOptions
  let timeout := pverify.timeout.get opts
  let trust := pverify.trust.get opts
  -- targets
  let tgts := proofTargets ctx
  let mut usingOf : NameMap (Array Name) := {}
  for pd in ctx.proofs do
    for d in pd.directives do
      if d.isDefault then
        logWarningAt d.ref "`prove default` is no longer needed: the framework invariants are proved once, generically"
        continue
      usingOf := usingOf.insert d.target ((((usingOf.find? d.target).getD #[])) ++ d.usingLemmas)
  for t in tgts do
    for u in ((usingOf.find? t).getD #[]) do
      unless tgts.contains u do
        throwError "`prove {t} using {u}`: `{u}` is never proved (add `prove {u} ;`)"
  let hasNoHalt := env.contains (modName ++ `NoHalt)
  let hasTransient := env.contains (modName ++ `Transient)
  -- `AllInv`'s conjuncts, as `#gen_module` emitted them
  let allTargets := PLean.allTargets ctx env modName
  -- names to unfold / hints / facts
  let (defNames, hintNames, factNames) := closeLists ctx env modName
  let defs : Array Ident := defNames.map mkIdent
  let hints : Array (TSyntax `term) := hintNames.map fun n => ⟨mkIdent n⟩
  let facts : Array (TSyntax `term) := factNames.map fun n => ⟨mkIdent n⟩
  let closeTac (handlers : Array Ident) : CommandElabM (TSyntax ``Lean.Parser.Tactic.tacticSeq) :=
    `(Lean.Parser.Tactic.tacticSeq|
        pverify_close [$[$handlers],*] [$[$defs],*] [$[$hints],*] [$[$facts],*])
  -- `T x` as a conjunction of its invariants, each marked with its name
  -- (`PostInv`), so a failing goal names its invariant.
  let markedPost (t : Name) (x : TSyntax `term) : CommandElabM (TSyntax `term) := do
    let invsOf := ((ctx.lemmas.find? t).map (·.invariants)).getD #[t]
    let parts ← invsOf.mapM fun n =>
      `(PLean.PostInv $(Syntax.mkStrLit n.toString) ($(mkIdent n) $x))
    match parts.back? with
    | none => `(True)
    | some last => parts.pop.foldrM (init := last) fun p acc => `($p ∧ $acc)
  let wrap (cmd : TSyntax `command) : CommandElabM (TSyntax `command) :=
    `(command|
      set_option experimental.vcgen true in
      set_option crush.backend "cvc5" in
      set_option crush.timeout $(Syntax.mkNumLit (toString timeout)):num in
      set_option crush.trust $(Syntax.mkStrLit trust):str in
      set_option maxHeartbeats 0 in
      set_option Elab.async false in
      $cmd:command)
  elabCommand (← `(namespace $name))
  elabCommand (← `(open PLean Std.WP))
  -- `AllInv` (emitted by `#gen_module`) has the conjuncts `allTargets`, in order.
  unless (← getEnv).contains (modName ++ `AllInv) do
    throwError "run `#gen_module {modName}` again: `{modName}.AllInv` is missing"
  let mut results : Array ObResult := #[]
  let only := ((pverify.only.get opts).splitOn ",").map (fun s => s.trimAscii.toString) |>.filter (!·.isEmpty)
  -- what the counterexample decoder needs to know about the program
  let cexNames : Cex.Names := Id.run do
    let mut tags : Std.HashMap String (String × String) := {}
    for mname in ctx.machineOrder do
      let some md := ctx.machines.find? mname | continue
      for (sname, _, _) in machineStates md.body do
        tags := tags.insert (stateTagName mname sname).toString (mname.toString, sname.toString)
    let structFields (base : String) : Option (Array String) :=
      [modName ++ base.toName, `PLean ++ base.toName, base.toName].findSome? fun n =>
        if isStructure env n then some ((getStructureFields env n).map (·.toString)) else none
    return { tags, machines := ctx.machineOrder.map (·.toString)
             fFields := ((structFields "F").getD #[])
             structFields
             functions := ctx.pures.toList.toArray.map (·.1.toString) }
  -- The obligations, run once all are generated (so progress can count them).
  let queue ← IO.mkRef (#[] : Array (Name × TSyntax `command × (Option String → Cex.Kind)))
  let queueOb (obName : Name) (cmd : TSyntax `command) (kind : Option String → Cex.Kind) :
      CommandElabM Unit := do
    if only.isEmpty || only.any (fun o => (obName.toString.splitOn o).length > 1) then
      queue.modify (·.push (obName, cmd, kind))
  let runOb (obName : Name) (cmd : TSyntax `command) (kind : Option String → Cex.Kind) :
      CommandElabM ObResult := do
    let t0 ← IO.monoMsNow
    let full := modName ++ obName
    -- a manual proof
    if pverifyProofAttr.hasTag (← getEnv) full then
      let err? ← match ← manualMismatch? full cmd with
        | some e => pure (some e)
        | none => manualAxiomsProblem? ctx modName full
      let trusted := err?.isNone && (← collectAxioms full).contains ``Crush.crushSorry
      return { name := obName, ok := err?.isNone, ms := (← IO.monoMsNow) - t0, manual := true,
               trusted, msg := (err?.map excerpt).getD "", fullMsg := err?.getD "" }
    let c ← elabCaptureGoals (← wrap cmd)
    let t1 ← IO.monoMsNow
    let errs := c.errs
    -- Under `pverify.trust trust` crush trusts the solver without a warning.
    let trusted ← if errs.isEmpty then
        pure (c.trusted || (← collectAxioms (modName ++ obName)).contains ``Crush.crushSorry)
      else pure false
    let cex := if errs.isEmpty || c.model.isEmpty then #[] else
      (Cex.render cexNames (kind (c.inv.map (·.toString))) c.script c.model).getD #[]
    let smt := if c.model.isEmpty then "" else
      c.script ++ "\n\n; z3's model:\n" ++ "\n".intercalate ((c.model.splitOn "\n").map ("; " ++ ·))
    let skeleton ← if errs.isEmpty then pure "" else skeletonOf obName full
    return { name := obName, ok := errs.isEmpty, ms := t1 - t0, goals := c.goals, skeleton,
             trusted, notes := c.notes, inv := c.inv, cex, smt, stats := c.stats,
             normMs := c.normMs, crushMs := c.crushMs,
             fullMsg := if errs.isEmpty then "" else errs[0]!,
             msg := if errs.isEmpty then "" else excerpt errs[0]! }
  let (g, i, src, dst, pl, m, arg) :=
    (mkIdent `g, mkIdent `i, mkIdent `src, mkIdent `dst, mkIdent `pl, mkIdent `m, mkIdent `arg)
  let gwTy := mkIdent `GW
  -- the (state, event) table
  let mut handled : Array (Name × Name × Name × TSyntax `term) := #[]   -- M, S, ev, act
  let mut never : Array (Name × Name × Name) := #[]
  let mut stateTags : Array (Name × Name) := #[]
  for mname in ctx.machineOrder do
    let some md := ctx.machines.find? mname | continue
    for (sname, _, items) in machineStates md.body do
      stateTags := stateTags.push (mname, sname)
      let sd? := md.states.find? (·.name == sname)
      let ign := sd?.map (·.ignoredEvents) |>.getD #[]
      let dfr := sd?.map (·.deferredEvents) |>.getD #[]
      for ev in ctx.eventOrder do
        let hasPl := (ctx.events.find? ev).any (·.hasPayload)
        let h := mkIdent (mname ++ sname ++ ev.appendAfter "_handler")
        if items.any (itemEvent? · == some ev) then
          handled := handled.push (mname, sname, ev, ← if hasPl then `($h $pl) else `($h))
        else if ign.contains ev then
          handled := handled.push (mname, sname, ev, ← `(pure ()))
        else if !dfr.contains ev then
          never := never.push (mname, sname, ev)
  let evVal (ev : Name) : CommandElabM (TSyntax `term) := do
    if (ctx.events.find? ev).any (·.hasPayload) then `($(mkIdent (`E ++ ev)) $pl) else `($(mkIdent (`E ++ ev)))
  let plBinder (ev : Name) : CommandElabM (Array (TSyntax ``Lean.Parser.Term.bracketedBinder)) := do
    if (ctx.events.find? ev).any (·.hasPayload) then
      return #[← `(Lean.Parser.Term.bracketedBinderF| ($pl : $(mkIdent (ev.appendAfter "_payload"))))]
    else return #[]
  -- A handler of a transient state runs only where `Transient` fails, so
  -- its obligations may assume `Transient`.
  let transientIn (mname sname : Name) : Array Name :=
    if hasTransient && ((ctx.machines.find? mname).map (transientStates · |>.contains sname)).getD false
    then #[`Transient] else #[]
  -- A resumer runs on a machine suspended at its site: `Waiting` gives the state.
  let premisesOf (t : Name) (extra : Array Name) : Array Name :=
    extra.foldl (init := #[t] ++ ((usingOf.find? t).getD #[])) fun ms x =>
      if ms.contains x then ms else ms.push x
  let (_, sites) ← planReceives ctx
  let siteArgs (s : RecvSite) : Array Ident :=
    (List.range s.saved.size).toArray.map fun j => mkIdent (Name.mkSimple s!"a{j}")
  let siteBinders (s : RecvSite) : CommandElabM (Array (TSyntax ``Lean.Parser.Term.bracketedBinder)) :=
    (s.saved.zip (siteArgs s)).mapM fun (v, a) =>
      `(Lean.Parser.Term.bracketedBinderF| ($a:ident : $(v.ty)))
  let recvHyps (pre : TSyntax `term) (s : RecvSite) (ev : Name) :
      CommandElabM (Array (TSyntax `term)) := do
    let site ← `($(mkIdent (`W ++ siteName s.idx)) $(siteArgs s)*)
    let evv ← evVal ev
    return #[← `(PLean.Framework $g), ← `(($g).error = none), pre,
      ← `(($g).net $i = some ⟨$src, $dst, $evv⟩),
      ← `(($g).log $i = some ⟨$src, $dst, $evv⟩),
      ← `($i < ($g).clock),
      ← `((($g).machines $dst).pending = none),
      ← `((($g).machines $dst).waiting = some $site),
      ← `((($g).machines $dst).halted = false)]
  let recvAct (s : RecvSite) (ev : Name) : CommandElabM (TSyntax `term) := do
    let fn := mkIdent (s.mname ++ resumerName s.sname s.idx ev)
    if (ctx.events.find? ev).any (·.hasPayload) then `($fn $(siteArgs s)* $pl) else `($fn $(siteArgs s)*)
  -- per-target obligations
  for t in allTargets do
    let pre ← conjAt (premisesOf t #[]) g
    let post ← `(fun g' => $(← markedPost t (← `(g'))))
    for (mname, sname, ev, act) in handled do
      let tag := mkIdent (`S ++ stateTagName mname sname)
      let obName := mname ++ sname ++ Name.mkSimple (ev.toString ++ "_correct_" ++ t.toString)
      let pre ← conjAt (premisesOf t (transientIn mname sname)) g
      let hyps ← deliverHyps g i src dst pre (← evVal ev) tag
      let concl ← `(PLean.RunPost $post (($g).consume $i) $dst (($g).machines $dst) $act)
      let stmt ← arrows hyps concl
      let hname := mkIdent (mname ++ sname ++ ev.appendAfter "_handler")
      let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($i:ident : Nat)
          ($src:ident $dst:ident : PLean.MachineRef) $(← plBinder ev)* : $stmt := by
        $(← closeTac #[hname]))
      queueOb obName cmd
        (.correct (.deliver mname.toString sname.toString ev.toString) t.toString)
    for (mname, sname) in stateTags do
      let tag := mkIdent (`S ++ stateTagName mname sname)
      let obName := mname ++ sname ++ Name.mkSimple ("entry_correct_" ++ t.toString)
      let hyps := #[← `(PLean.Framework $g), ← `(($g).error = none), pre,
        ← `((($g).machines $m).state = some $tag), ← `((($g).machines $m).pending = some $arg),
        ← `((($g).machines $m).halted = false)]
      let entryFn := mkIdent (mname ++ sname ++ `entry)
      let concl ← `(PLean.RunPost $post $g $m { ($g).machines $m with pending := none } ($entryFn $arg))
      let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($m:ident : PLean.MachineRef)
          ($arg:ident : $(mkIdent `G)) : $(← arrows hyps concl) := by
        $(← closeTac #[entryFn]))
      queueOb obName cmd
        (.correct (.entry mname.toString sname.toString) t.toString)
    for s in sites do
      for c in s.cases do
        let obName := (s.mname ++ resumerName s.sname s.idx c.ev).appendAfter ("_correct_" ++ t.toString)
        let preW ← conjAt (premisesOf t #[`Waiting]) g
        let hyps ← recvHyps preW s c.ev
        let concl ← `(PLean.RunPost $post (($g).consume $i) $dst
            { ($g).machines $dst with waiting := none } $(← recvAct s c.ev))
        let fn := mkIdent (s.mname ++ resumerName s.sname s.idx c.ev)
        let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($i:ident : Nat)
            ($src:ident $dst:ident : PLean.MachineRef) $((← siteBinders s) ++ (← plBinder c.ev))* :
            $(← arrows hyps concl) := by
          $(← closeTac #[fn]))
        queueOb obName cmd
          (.correct (.resume s.mname.toString s.sname.toString c.ev.toString) t.toString)
    if !hasNoHalt then
      let obName := Name.mkSimple ("drop_correct_" ++ t.toString)
      let p := mkIdent `p
      let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($i:ident : Nat) ($p:ident : PLean.Packet $(mkIdent `E)) :
          PLean.Framework $g → ($g).error = none → $pre → ($g).net $i = some $p →
          (($g).machines ($p).dst).halted = true → $(← markedPost t (← `(($g).consume $i))) := by
        $(← closeTac #[]))
      queueOb obName cmd (.correct .drop t.toString)
    let baseName := Name.mkSimple ("base_" ++ t.toString)
    let cmd ← `(command| theorem $(mkIdent baseName) ($g:ident : $gwTy) :
        $(mkIdent `InitConditions) $g → $(← markedPost t g) := by
      $(← closeTac #[]))
    queueOb baseName cmd (.correct .base t.toString)
  let allPre ← `($(mkIdent `AllInv) $g)
  -- error freedom (under every invariant)
  for (mname, sname, ev, act) in handled do
    let tag := mkIdent (`S ++ stateTagName mname sname)
    let obName := mname ++ sname ++ Name.mkSimple (ev.toString ++ "_safe")
    let hyps ← deliverHyps g i src dst allPre (← evVal ev) tag
    let concl ← `(PLean.RunSafe (($g).consume $i) $dst (($g).machines $dst) $act)
    let hname := mkIdent (mname ++ sname ++ ev.appendAfter "_handler")
    let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($i:ident : Nat)
        ($src:ident $dst:ident : PLean.MachineRef) $(← plBinder ev)* : $(← arrows hyps concl) := by
      $(← closeTac #[hname]))
    queueOb obName cmd
      fun _ => .safe (.deliver mname.toString sname.toString ev.toString)
  for (mname, sname) in stateTags do
    let tag := mkIdent (`S ++ stateTagName mname sname)
    let obName := mname ++ sname ++ `entry_safe
    let hyps := #[← `(PLean.Framework $g), ← `(($g).error = none), allPre,
      ← `((($g).machines $m).state = some $tag), ← `((($g).machines $m).pending = some $arg),
      ← `((($g).machines $m).halted = false)]
    let entryFn := mkIdent (mname ++ sname ++ `entry)
    let concl ← `(PLean.RunSafe $g $m { ($g).machines $m with pending := none } ($entryFn $arg))
    let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($m:ident : PLean.MachineRef)
        ($arg:ident : $(mkIdent `G)) : $(← arrows hyps concl) := by
      $(← closeTac #[entryFn]))
    queueOb obName cmd fun _ => .safe (.entry mname.toString sname.toString)
  for s in sites do
    for c in s.cases do
      let obName := (s.mname ++ resumerName s.sname s.idx c.ev).appendAfter "_safe"
      let hyps ← recvHyps allPre s c.ev
      let concl ← `(PLean.RunSafe (($g).consume $i) $dst
          { ($g).machines $dst with waiting := none } $(← recvAct s c.ev))
      let fn := mkIdent (s.mname ++ resumerName s.sname s.idx c.ev)
      let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($i:ident : Nat)
          ($src:ident $dst:ident : PLean.MachineRef) $((← siteBinders s) ++ (← plBinder c.ev))* :
          $(← arrows hyps concl) := by
        $(← closeTac #[fn]))
      queueOb obName cmd
        fun _ => .safe (.resume s.mname.toString s.sname.toString c.ev.toString)
  -- never-receives
  for (mname, sname, ev) in never do
    let tag := mkIdent (`S ++ stateTagName mname sname)
    let obName := mname ++ sname ++ Name.mkSimple (ev.toString ++ "_never")
    let hyps ← deliverHyps g i src dst allPre (← evVal ev) tag
    let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($i:ident : Nat)
        ($src:ident $dst:ident : PLean.MachineRef) $(← plBinder ev)* : $(← arrows hyps (← `(False))) := by
      $(← closeTac #[]))
    queueOb obName cmd fun _ => .never mname.toString sname.toString ev.toString
  -- discharge, reporting progress
  let queued ← queue.get
  let showT := pverify.showTimes.get opts
  let status (k : Nat) (rs : Array ObResult) : String :=
    let nFailed := (rs.filter (!·.ok)).size
    let failedS := if nFailed == 0 then "" else s!", {nFailed} failed"
    s!"#pverify {modName}: {k}/{queued.size} obligations done{failedS}"
  -- Obligations are proved in parallel by a pool of workers. Each worker
  -- elaborates an obligation's theorem, kernel-checking it, on its own copy of
  -- the environment as it is now (so no obligation waits for another), and
  -- returns the theorem with any auxiliary declarations its proof added.
  -- They are then added here, in order, without a second kernel check: each
  -- was checked against this environment, which only gains the other
  -- obligations. Manual proofs exist already and are checked here.
  let jobs ← match pverify.jobs.get opts with
    | 0 => do
      match (← IO.getEnv "PVERIFY_JOBS").bind String.toNat? with
      | some j => pure (max 1 j)
      | none => pure (max 1 (System.Platform.Internal.getHardwareConcurrency ()).toNat)
    | j => pure j
  let slots ← IO.mkRef (Array.replicate queued.size (none : Option ObResult))
  let declSlots ← IO.mkRef (Array.replicate queued.size (#[] : Array ConstantInfo))
  let doneRef ← IO.mkRef (0 : Nat)
  let failedRef ← IO.mkRef (0 : Nat)
  let runningRef ← IO.mkRef (#[] : Array Name)
  let showProgress : BaseIO Unit := do
    let k ← doneRef.get
    let nFailed ← failedRef.get
    let failedS := if nFailed == 0 then "" else s!", {nFailed} failed"
    let running ← runningRef.get
    let names := ", ".intercalate ((running.toList.take 3).map toString)
    let more := if running.size > 3 then s!", … ({running.size} running)" else ""
    Progress.setHead s!"#pverify {modName}: {k}/{queued.size} obligations done{failedS}"
      (if running.isEmpty then "" else s!"[Proving] {names}{more}")
  let record (k : Nat) (r : ObResult) : BaseIO Unit := do
    slots.modify (·.set! k (some r))
    doneRef.modify (· + 1)
    unless r.ok do failedRef.modify (· + 1)
  let cancelTk? := (← read).cancelTk?
  let baseConsts : NameSet :=
    (← getEnv).constants.map₂.foldl (init := {}) fun acc n _ => acc.insert n
  let mut work : Array (Nat × Name × (Unit → EIO Exception (ObResult × Array ConstantInfo))) := #[]
  for (obName, cmd, kind) in queued, k in [0:queued.size] do
    if pverifyProofAttr.hasTag (← getEnv) (modName ++ obName) then
      record k (← runOb obName cmd kind)
      continue
    let act ← Command.wrapAsync (cancelTk? := cancelTk?) fun (_ : Unit) => do
      let r ← runOb obName cmd kind
      let added := (← getEnv).constants.map₂.foldl (init := #[]) fun acc n ci =>
        if baseConsts.contains n then acc else acc.push ci
      return (r, added)
    work := work.push (k, obName, act)
  let nextRef ← IO.mkRef (0 : Nat)
  let worker : IO Unit := do
    repeat
      -- once the command is cancelled (the file was edited), take no more
      if let some tk := cancelTk? then
        if ← tk.isSet then break
      let i ← nextRef.modifyGet fun n => (n, n + 1)
      let some (k, obName, act) := work[i]? | break
      runningRef.modify (·.push obName)
      showProgress
      match ← (act ()).toBaseIO with
      | .ok (r, added) =>
        declSlots.modify (·.set! k added)
        record k r
      | .error e =>
        record k { name := obName, ok := false, ms := 0, msg := excerpt (← e.toMessageData.toString) }
      runningRef.modify (·.erase obName)
      showProgress
  if jobs > 1 then Progress.details.set false
  let tProve ← IO.monoMsNow
  try
    let workers ← (List.range (min jobs work.size)).mapM fun _ => IO.asTask worker .dedicated
    for t in workers do
      match t.get with
      | .ok () => pure ()
      | .error e => logError m!"#pverify: a worker failed: {e}"
  finally
    Progress.details.set true
  let wallMs := (← IO.monoMsNow) - tProve
  let nJobs := min jobs work.size
  -- add the obligations' theorems (and their auxiliary declarations)
  for added in ← declSlots.get do
    for ci in added do
      -- an auxiliary declaration several proofs created (an equation lemma
      -- made on demand) is added once; the same name with another statement
      -- would make the unchecked addition unsound
      if let some old := (← getEnv).find? ci.name then
        -- the same statement suffices for theorems (proofs are irrelevant);
        -- anything else must be the same declaration
        let same := old.type == ci.type && old.levelParams == ci.levelParams &&
          (if old.isTheorem && ci.isTheorem then true
           else old.isTheorem == ci.isTheorem &&
             old.value? (allowOpaque := true) == ci.value? (allowOpaque := true))
        unless same do
          logError m!"#pverify: two proofs declare `{ci.name}` differently"
        continue
      let decl? : Option Declaration := match ci with
        | .thmInfo v => some (.thmDecl v)
        | .defnInfo v => some (.defnDecl v)
        | .opaqueInfo v => some (.opaqueDecl v)
        | .axiomInfo v => some (.axiomDecl v)
        | _ => none
      let some decl := decl?
        | logError m!"#pverify: cannot add `{ci.name}` from a parallel proof"
      try
        -- (a failed obligation's `sorry` is in the report already)
        liftCoreM <| withOptions (fun o => ((debug.skipKernelTC.set o true).setBool `Elab.async false
            ).setBool `warn.sorry false) <|
          addDecl decl
      catch e => logError m!"#pverify: cannot add `{ci.name}`: {e.toMessageData}"
  results := (← slots.get).filterMap id
  if only.isEmpty then
    Progress.setHead (status queued.size results) s!"[Assembling] {modName}.safe"
  -- assembly (not counted as obligations; skipped under `pverify.only`)
  let asm (cmd : TSyntax `command) : CommandElabM Unit := do
    if only.isEmpty then elabCommand cmd
  let n := allTargets.size
  let hName (k : Nat) : Ident := mkIdent (Name.mkSimple s!"hpre{k}")
  let preNames := (List.range n).toArray.map hName
  let destruct ← if n ≤ 1 then `(tactic| skip)
    else `(tactic| obtain ⟨$[$preNames],*⟩ := hpre)
  let preOf (t : Name) (extra : Array Name := #[]) : CommandElabM (TSyntax `term) := do
    let members := premisesOf t extra
    let hs ← members.mapM fun u => do
      if n ≤ 1 then `(hpre) else pure (hName (allTargets.idxOf u))
    if hs.size = 1 then `($(hs[0]!)) else `(⟨$[$hs],*⟩)
  let combineRun (mk : Name → CommandElabM (TSyntax `term)) : CommandElabM (TSyntax `term) := do
    let parts ← allTargets.mapM mk
    let body ← parts.pop.foldrM (init := parts.back!) fun p acc => `(PLean.RunPost.and $p $acc)
    `(PLean.RunPost.mono (fun _ h => h) $body)
  let mut caseNames : Array Name := #[]
  for (mname, sname, ev, _) in handled do
    let tag := mkIdent (`S ++ stateTagName mname sname)
    let cn := mname ++ sname ++ Name.mkSimple (ev.toString ++ "_case")
    caseNames := caseNames.push cn
    let hyps ← deliverHyps g i src dst allPre (← evVal ev) tag
    let concl ← `(∃ act, $(mkIdent `prog).handler $tag $(← evVal ev) = some act ∧
        PLean.RunOK $(mkIdent `AllInv) (($g).consume $i) $dst (($g).machines $dst) act)
    let hasPl := (ctx.events.find? ev).any (·.hasPayload)
    let body ← combineRun fun t => do
      let ob := mkIdent (mname ++ sname ++ Name.mkSimple (ev.toString ++ "_correct_" ++ t.toString))
      let pr ← preOf t (transientIn mname sname)
      if hasPl then `($ob $g $i $src $dst $pl hF herr $pr hnet hlog hlt hs hpend hwait hhalt)
      else `($ob $g $i $src $dst hF herr $pr hnet hlog hlt hs hpend hwait hhalt)
    let so := mkIdent (mname ++ sname ++ Name.mkSimple (ev.toString ++ "_safe"))
    let safe ← if hasPl then `($so $g $i $src $dst $pl hF herr hall hnet hlog hlt hs hpend hwait hhalt)
      else `($so $g $i $src $dst hF herr hall hnet hlog hlt hs hpend hwait hhalt)
    asm (← `(command| theorem $(mkIdent cn) ($g:ident : $gwTy) ($i:ident : Nat)
        ($src:ident $dst:ident : PLean.MachineRef) $(← plBinder ev)* : $(← arrows hyps concl) := by
      intro hF herr hpre hnet hlog hlt hs hpend hwait hhalt
      have hall := hpre
      $destruct:tactic
      exact ⟨_, rfl, PLean.RunOK.of_safe_post $safe $body⟩))
  for (mname, sname, ev) in never do
    let tag := mkIdent (`S ++ stateTagName mname sname)
    let cn := mname ++ sname ++ Name.mkSimple (ev.toString ++ "_case")
    caseNames := caseNames.push cn
    let hyps ← deliverHyps g i src dst allPre (← evVal ev) tag
    let concl ← `(∃ act, $(mkIdent `prog).handler $tag $(← evVal ev) = some act ∧
        PLean.RunOK $(mkIdent `AllInv) (($g).consume $i) $dst (($g).machines $dst) act)
    let nv := mkIdent (mname ++ sname ++ Name.mkSimple (ev.toString ++ "_never"))
    let hasPl := (ctx.events.find? ev).any (·.hasPayload)
    let app ← if hasPl then `($nv $g $i $src $dst $pl hF herr hpre hnet hlog hlt hs hpend hwait hhalt)
      else `($nv $g $i $src $dst hF herr hpre hnet hlog hlt hs hpend hwait hhalt)
    asm (← `(command| theorem $(mkIdent cn) ($g:ident : $gwTy) ($i:ident : Nat)
        ($src:ident $dst:ident : PLean.MachineRef) $(← plBinder ev)* : $(← arrows hyps concl) := by
      intro hF herr hpre hnet hlog hlt hs hpend hwait hhalt
      exact ($app).elim))
  let mut entryCases : Array Name := #[]
  for (mname, sname) in stateTags do
    let tag := mkIdent (`S ++ stateTagName mname sname)
    let cn := mname ++ sname ++ `entry_case
    entryCases := entryCases.push cn
    let body ← combineRun fun t => do
      let ob := mkIdent (mname ++ sname ++ Name.mkSimple ("entry_correct_" ++ t.toString))
      `($ob $g $m $arg hF herr $(← preOf t) hs hpend hhalt)
    asm (← `(command| theorem $(mkIdent cn) ($g:ident : $gwTy) ($m:ident : PLean.MachineRef)
        ($arg:ident : $(mkIdent `G)) : PLean.Framework $g → ($g).error = none → $allPre →
        (($g).machines $m).state = some $tag → (($g).machines $m).pending = some $arg →
        (($g).machines $m).halted = false →
        PLean.RunOK $(mkIdent `AllInv) $g $m { ($g).machines $m with pending := none }
          ($(mkIdent `prog).entry $tag $arg) := by
      intro hF herr hpre hs hpend hhalt
      have hall := hpre
      $destruct:tactic
      exact PLean.RunOK.of_safe_post ($(mkIdent (mname ++ sname ++ `entry_safe)) $g $m $arg hF herr hall hs hpend hhalt) $body))
  let mut recvTacs : Array (TSyntax `tactic) := #[]
  for s in sites do
    for c in s.cases do
      let base := s.mname ++ resumerName s.sname s.idx c.ev
      let cn := base.appendAfter "_case"
      let hasPl := (ctx.events.find? c.ev).any (·.hasPayload)
      let args := siteArgs s
      let plArg ← if hasPl then pure #[pl] else pure #[]
      let body ← combineRun fun t => do
        `($(mkIdent (base.appendAfter ("_correct_" ++ t.toString))) $g $i $src $dst $args* $plArg*
            hF herr $(← preOf t #[`Waiting]) hnet hlog hlt hpend hw hhalt)
      let safe ← `($(mkIdent (base.appendAfter "_safe")) $g $i $src $dst $args* $plArg*
            hF herr hall hnet hlog hlt hpend hw hhalt)
      asm (← `(command| theorem $(mkIdent cn) ($g:ident : $gwTy) ($i:ident : Nat)
          ($src:ident $dst:ident : PLean.MachineRef) $((← siteBinders s) ++ (← plBinder c.ev))* :
          $(← arrows (← recvHyps allPre s c.ev) (← `(PLean.RunOK $(mkIdent `AllInv) (($g).consume $i) $dst
            { ($g).machines $dst with waiting := none } $(← recvAct s c.ev)))) := by
        intro hF herr hpre hnet hlog hlt hpend hw hhalt
        have hall := hpre
        $destruct:tactic
        exact PLean.RunOK.of_safe_post $safe $body))
      let holes := (List.replicate (2 + args.size + plArg.size) (← `(_))).toArray
      recvTacs := recvTacs.push (← `(tactic| exact $(mkIdent cn) g i $holes* hF herr hpre hnet
        (PLean.Framework.log_of_net hF hnet) (PLean.Framework.lt_clock_of_net hF hnet) hpend hw hh))
  let resumeTac ← if sites.isEmpty then
      `(tactic| (intro g i p k _ _ _ _ _ _ _ act hr; simp [$(mkIdent `prog):ident] at hr))
    else
      `(tactic| (intro g i p k hF herr hpre hnet hpend hw hh act hr
                 obtain ⟨src, dst, ev⟩ := p
                 cases k <;> cases ev <;> simp [$(mkIdent `prog):ident] at hr <;> subst hr <;>
                   first $[| $recvTacs:tactic]*))
  let caseTacs ← caseNames.mapM fun cn =>
    `(tactic| exact $(mkIdent cn) g i src dst _ hF herr hpre hnet (PLean.Framework.log_of_net hF hnet) (PLean.Framework.lt_clock_of_net hF hnet) hs hpend hwait hhalt)
  let caseTacs2 ← caseNames.mapM fun cn =>
    `(tactic| exact $(mkIdent cn) g i src dst hF herr hpre hnet (PLean.Framework.log_of_net hF hnet) (PLean.Framework.lt_clock_of_net hF hnet) hs hpend hwait hhalt)
  let entryTacs ← entryCases.mapM fun cn =>
    `(tactic| exact $(mkIdent cn) g m arg hF herr hpre hs hpend hhalt)
  let dropTac ← if hasNoHalt then
      `(tactic| (intro g i p _ _ hpre _ hh; exfalso; have := (hpre : $(mkIdent `AllInv) g);
                 simp only [$(mkIdent `AllInv):ident, $(mkIdent `NoHalt):ident] at this;
                 pv_split_ands; simp_all))
    else
      let parts ← allTargets.mapM fun t => do
        `($(mkIdent (Name.mkSimple ("drop_correct_" ++ t.toString))) g i p hF herr $(← preOf t) hnet hh)
      if parts.size == 1 then
        `(tactic| (intro g i p hF herr hpre hnet hh; exact $(parts[0]!)))
      else
        `(tactic| (intro g i p hF herr hpre hnet hh; $destruct:tactic; exact ⟨$[$parts],*⟩))
  let stepThm ← `(command|
    theorem $(mkIdent `step_AllInv) : ∀ g c coins, PLean.Framework g → g.error = none →
        $(mkIdent `AllInv) g → $(mkIdent `prog).enabled .bag g.toWorld c = true →
        (PLean.gstep $(mkIdent `prog) g c coins).error = none ∧
          $(mkIdent `AllInv) (PLean.gstep $(mkIdent `prog) g c coins) := by
      apply PLean.step_ok $(mkIdent `prog) $(mkIdent `AllInv) $(mkIdent `AllInv)
      · intro g i p s hF herr hpre hnet hs hpend hwait hhalt hdef
        obtain ⟨src, dst, ev⟩ := p
        cases s <;> cases ev <;> first
          $[| $caseTacs:tactic]*
          $[| $caseTacs2:tactic]*
          | (simp [$(mkIdent `prog):ident] at hdef)
      · $resumeTac:tactic
      · intro g m s arg hF herr hpre hs hpend hhalt
        cases s <;> first $[| $entryTacs:tactic]*
      · $dropTac:tactic)
  let baseParts := allTargets.map fun t => mkIdent (Name.mkSimple ("base_" ++ t.toString))
  let baseApps ← baseParts.mapM fun b => `($b g hI)
  let baseConj ← if baseApps.size = 1 then pure baseApps[0]! else `(⟨$[$baseApps],*⟩)
  let baseThm ← `(command|
    theorem $(mkIdent `base_AllInv) : ∀ g, $(mkIdent `InitConditions) g →
        PLean.Framework g ∧ g.error = none ∧ $(mkIdent `AllInv) g := by
      intro g hI
      have hI' := hI
      simp only [$(mkIdent `InitConditions):ident] at hI'
      obtain ⟨_, hn, hl, he, hu, ⟨hpos, hnull⟩, _⟩ := hI'
      refine ⟨⟨fun j _ => hn j, fun j _ => hl j, ?_, hu, hpos, hnull⟩, he, $baseConj⟩
      intro j p h; rw [hn j] at h; cases h)
  let safeThm ← `(command|
    theorem $(mkIdent `safe) : ∀ g, PLean.GReachable $(mkIdent `prog) .bag $(mkIdent `InitConditions) g →
        $(mkIdent `AllInv) g ∧ g.error = none := fun g hg =>
      let h := PLean.safety_of_step $(mkIdent `prog) $(mkIdent `InitConditions) $(mkIdent `AllInv)
        $(mkIdent `base_AllInv) $(mkIdent `step_AllInv) g hg
      ⟨h.2.2, h.2.1⟩)
  let safeFifo ← `(command|
    theorem $(mkIdent `safe_fifo) : ∀ g, PLean.GReachable $(mkIdent `prog) .fifo $(mkIdent `InitConditions) g →
        $(mkIdent `AllInv) g ∧ g.error = none := fun g hg => $(mkIdent `safe) g hg.fifo_bag)
  let mut asmErrs : Array String := #[]
  for cmd in (if only.isEmpty then #[stepThm, baseThm, safeThm, safeFifo] else #[]) do
    asmErrs := asmErrs ++ (← elabCapture (← wrap cmd))
  for (t, k) in allTargets.toList.zipIdx do
    if t == `NoHalt || t == `Transient || t == `Waiting || !only.isEmpty then continue
    let (gq, hgq) := (mkIdent `g, mkIdent `hg)
    let proj ← (List.range k).foldlM (init := ← `(($(mkIdent `safe_fifo) $gq $hgq).1)) fun acc _ => `(($acc).2)
    let proj ← if k + 1 < n then `(($proj).1) else pure proj
    asmErrs := asmErrs ++ (← elabCapture (← `(command|
      theorem $(mkIdent (Name.mkSimple (t.toString ++ "_holds"))) : ∀ g,
          PLean.GReachable $(mkIdent `prog) .fifo $(mkIdent `InitConditions) g → $(mkIdent t) g :=
        fun $gq $hgq => $proj)))
  -- `InitConditions` (with the axioms) must be satisfiable, or every theorem
  -- holds vacuously: a quick attempt to refute it.
  let mut vacuous := false
  if only.isEmpty then
    Progress.setHead (status queued.size results) s!"[Checking] {modName}.InitConditions is satisfiable"
    let saved ← get
    let cmd ← `(command| theorem $(mkIdent `init_unsat) ($g:ident : $gwTy) :
        $(mkIdent `InitConditions) $g → False := by
      $(← closeTac #[]))
    let errs ← elabCapture (← `(command|
      set_option pverify.cex false in
      set_option pverify.quick true in
      $(← wrap cmd):command))
    set saved
    vacuous := errs.isEmpty
  elabCommand (← `(end $name))
  -- What `M.safe` rests on besides Lean's own axioms.
  let mut assumes : Array String := #[]
  let mut usesSorry := false
  if (← getEnv).contains (modName ++ `safe) then
    for ax in ← collectAxioms (modName ++ `safe) do
      if ax == ``propext || ax == ``Classical.choice || ax == ``Quot.sound then continue
      if ax == ``sorryAx then
        usesSorry := true
        assumes := assumes.push "`sorry`"
      else if ax == ``Crush.crushSorry then
        assumes := assumes.push "the solver's answers in the trusted obligations (`Crush.crushSorry`)"
      else
        let short := ax.replacePrefix modName .anonymous
        if ctx.axioms.contains short then assumes := assumes.push s!"paxiom `{short}`"
        else if ctx.instances.contains short then assumes := assumes.push s!"pinstance `{short}`"
        else assumes := assumes.push s!"`{ax}`"
  -- report
  let failed := results.filter (!·.ok)
  let total := results.foldl (· + ·.ms) 0
  let nTrusted := (results.filter fun r => r.ok && r.trusted).size
  let proved := results.filter fun r => r.ok && !r.manual
  let nManual := (results.filter fun r => r.ok && r.manual).size
  let (nNorm, nPre, nSmt) := proved.foldl (init := (0, 0, 0)) fun (a, b, c) r =>
    match r.stats with
    | some (_, _, s) => if s > 0 then (a, b, c + 1) else
        match r.stats with
        | some (_, p, _) => if p > 0 then (a, b + 1, c) else (a + 1, b, c)
        | none => (a + 1, b, c)
    | none => (a + 1, b, c)
  let secs (ms : Nat) : String := s!"{(ms + 500) / 1000} s"
  let normT := results.foldl (· + ·.normMs) 0
  let crushT := results.foldl (· + ·.crushMs) 0
  let timeSplit := if showT then
      s!" (time: {secs normT} in vcgen and normalisation, {secs crushT} in crush, \
        {secs (total - min total (normT + crushT))} elaborating and checking)"
    else ""
  let mut lines : Array String := #[s!"#pverify {modName}: {results.size} obligations, {results.size - failed.size} proved ({nTrusted} trusting the solver), {failed.size} failed{if showT then s!" ({wallMs} ms, {nJobs} at a time)" else ""}",
    s!"  proved by normalisation or a hypothesis: {nNorm}, by crush before SMT: {nPre}, with an SMT solver: {nSmt}{if nManual > 0 then s!", by hand: {nManual}" else ""}{timeSplit}"]
  for r in results do
    lines := lines.push s!"  {r.line showT}"
    for l in r.cex do lines := lines.push s!"      {l}"
  for e in asmErrs do lines := lines.push s!"  assembly error: {excerpt e}"
  -- `@[pverifyProof]` theorems of this module that prove no obligation
  if only.isEmpty then
    let names := queued.map (modName ++ ·.1)
    let env ← getEnv
    let mut tagged : Array Name := (pverifyProofAttr.ext.getState env).toArray
    for i in [0:env.allImportedModuleNames.size] do
      tagged := tagged ++ pverifyProofAttr.ext.getModuleEntries env i
    -- a theorem belongs to the module with the longest name prefixing it
    let others := (← allPModules).map (·.1) |>.filter fun m =>
      m != modName && modName.isPrefixOf m
    for n in tagged do
      if modName.isPrefixOf n && !names.contains n && !others.any (·.isPrefixOf n) then
        lines := lines.push s!"  `@[pverifyProof]` theorem `{n}` is not an obligation"
  unless assumes.isEmpty do
    lines := lines.push s!"  {modName}.safe assumes: {", ".intercalate assumes.toList}"
  if vacuous then
    lines := lines.push s!"  warning: no world satisfies {modName}.InitConditions (with the axioms), \
      so every theorem holds vacuously"
  let report := "\n".intercalate lines.toList
  -- Always keep the full report (with failing goals) on disk.
  let defaultPath : System.FilePath := ".lake" / "pverify" / s!"{modName}.txt"
  let path : System.FilePath := match pverify.report.get opts with
    | "" => defaultPath
    | p => p
  try
    if let some dir := path.parent then IO.FS.createDirAll dir
    let h ← IO.FS.Handle.mk path .write
    h.putStrLn report
    for r in results do
      unless r.notes.isEmpty do h.putStrLn s!"--- {r.name} (profile)\n{"\n".intercalate r.notes.toList}"
    for r in failed do
      h.putStrLn s!"--- {r.name}\n{r.fullMsg}"
      for gl in r.goals do h.putStrLn gl
      unless r.skeleton.isEmpty do
        h.putStrLn s!"A manual proof, after `#gen_module {modName}` and before `#pverify {modName}`, \
          inside `namespace {modName}` with `open PLean Std.WP`:\n{r.skeleton}"
    -- the SMT script and model of each counterexample
    for r in failed do
      unless r.smt.isEmpty do
        let p := path.withExtension "" / s!"{r.name}.smt2"
        if let some dir := p.parent then IO.FS.createDirAll dir
        IO.FS.writeFile p r.smt
  catch _ => pure ()
  Progress.finish (status queued.size results)
  -- A vacuous result is as incomplete as a `sorry`.
  if failed.isEmpty && asmErrs.isEmpty && !usesSorry && !vacuous then logInfo report
  else if pverify.failOnIncomplete.get opts then logError report
  else logWarning report

open Language in
/-- `#pverify`. Incremental, so it can report progress in the editor: it
resolves its snapshot at once with one message whose widget shows the
progress, updated in place (`PLean.Progress`). -/
@[command_elab pVerify, incremental]
def elabPVerify : CommandElab := fun stx => do
  let `(#pverify $name:ident) := stx | throwUnsupportedSyntax
  if let some snap := (← read).snap? then
    if pverify.progress.get (← getOptions) then
      let text := s!"#pverify {name.getId}: starting"
      let id ← Progress.start text
      let fileMap ← getFileMap
      let log := MessageLog.empty.add {
        fileName := ← getFileName
        pos := fileMap.toPosition (stx.getPos?.getD 0)
        endPos := stx.getTailPos?.map fileMap.toPosition
        severity := .information
        data := ← liftCoreM (Progress.message id text) }
      snap.new.resolve (.ofTyped (SnapshotTree.mk
        { diagnostics := ← Snapshot.Diagnostics.ofMessageLog log } #[]))
  let scopes := (← get).scopes
  try
    -- The obligations are commands themselves; they must not take over the snapshot.
    withReader ({ · with snap? := none }) (pverifyBody stx)
  catch e =>
    -- close the namespace `pverifyBody` opened
    modify fun st => { st with scopes }
    throw e
  finally
    Progress.details.set true
    Progress.finish s!"#pverify {name.getId}: stopped"

end PLean
