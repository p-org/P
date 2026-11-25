/-
PLean.Commands.PVerify — `#pverify M`: emit the proof obligations of
pmodule `M` (docs/TransitionModel.md §4), discharge each with
`pverify_close`, and assemble the safety theorems:

  M.safe      : ∀ g, GReachable prog .bag  InitConditions g → AllInv g
  M.safe_fifo : ∀ g, GReachable prog .fifo InitConditions g → AllInv g
  M.<T>_holds : ∀ g, GReachable prog .fifo InitConditions g → T g

`AllInv` is the conjunction of every proved target (plus `NoHalt` when
no machine halts). Per target `T` (`prove T using U₁ …`), with
`Pre := T ∧ U₁ ∧ …` and `Post := T`:

* `<M>.<S>.<ev>_correct_<T>` per handled / ignored event of a state,
* `<M>.<S>.entry_correct_<T>` per state,
* `drop_correct_<T>` if a machine can halt,
* `base_<T>`: `InitConditions g → T g`;

and once per module `<M>.<S>.<ev>_never` per event a state neither
handles, ignores nor defers.

A failing obligation is reported (with its time and the error) and left
as `sorry`, so the remaining obligations and the assembly still run.
-/
import Lean
import PLean.Commands.GenModule
import PLean.Verify.Tactic

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

register_option pverify.trust : String := {
  defValue := "reconstructOrTrust"
  descr := "`crush.trust` for each obligation: reconstruct (checked proofs only), \
            reconstructOrTrust (checked when crush can replay the proof, otherwise trust the \
            solver; reported as `trusted`), or trust." }

/-- One obligation's outcome. -/
structure ObResult where
  name : Name
  ok   : Bool
  ms   : Nat
  msg  : String := ""
  /-- The goals `crush` failed on (report file only). -/
  goals : Array String := #[]
  /-- Proved, but some step trusts the solver (no checked reconstruction). -/
  trusted : Bool := false

/-- The kind of a failed obligation, from its error message. -/
def failureKind (msg : String) : String :=
  let has (s : String) := (msg.splitOn s).length > 1
  if has "returned `sat`" then "counterexample"
  else if has "reconstruction failed" then "reconstruct-failed"
  else if has "`unknown`" || has "timeout" then "unknown"
  else "error"

/-- Elaborate `cmd`, returning its error messages and the goals `crush`
failed on (both removed from the log). -/
private def elabCaptureGoals (cmd : Syntax) :
    CommandElabM (Array String × Array String × Bool) := do
  let saved := (← get).messages
  modify fun s => { s with messages := {} }
  elabCommand cmd
  let msgs := (← get).messages
  let mut errs : Array String := #[]
  let mut goals : Array String := #[]
  let mut trusted := false
  for m in msgs.toList do
    let s ← m.data.toString
    if m.severity == .error then errs := errs.push (← m.toString)
    else if s.startsWith "pverify-goal:" then goals := goals.push s
    else if (s.splitOn "crushSorry").length > 1 then trusted := true
  modify fun s => { s with messages := saved }
  return (errs, goals, trusted)

private def elabCapture (cmd : Syntax) : CommandElabM (Array String) :=
  return (← elabCaptureGoals cmd).1

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

syntax (name := pVerify) "#pverify " ident : command

@[command_elab pVerify]
def elabPVerify : CommandElab := fun stx => do
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
  let mut tgts : Array Name := #[]
  let mut usingOf : NameMap (Array Name) := {}
  for pd in ctx.proofs do
    for d in pd.directives do
      if d.isDefault then
        logWarningAt d.ref "`prove default` is no longer needed: the framework invariants are proved once, generically"
        continue
      unless tgts.contains d.target do tgts := tgts.push d.target
      usingOf := usingOf.insert d.target ((((usingOf.find? d.target).getD #[])) ++ d.usingLemmas)
  for t in tgts do
    for u in ((usingOf.find? t).getD #[]) do
      unless tgts.contains u do
        throwError "`prove {t} using {u}`: `{u}` is never proved (add `prove {u} ;`)"
  let hasNoHalt := env.contains (modName ++ `NoHalt)
  let allTargets := if hasNoHalt then tgts.push `NoHalt else tgts
  -- names to unfold / hints
  let mut defs : Array Ident := #[mkIdent `InitConditions]
  for n in ctx.invariantOrder do defs := defs.push (mkIdent n)
  for n in ctx.lemmaOrder do defs := defs.push (mkIdent n)
  for m in ctx.machineOrder do defs := defs.push (mkIdent (Name.mkSimple ("is_" ++ m.toString)))
  for e in ctx.eventOrder do defs := defs.push (mkIdent (Name.mkSimple ("is_" ++ e.toString)))
  if hasNoHalt then defs := defs.push (mkIdent `NoHalt)
  defs := defs.push (mkIdent `AllInv)
  let mut hints : Array (TSyntax `term) := #[]
  for e in ctx.eventOrder do
    if (ctx.events.find? e).any (·.hasPayload) then
      hints := hints.push (← `($(mkIdent (Name.mkSimple (e.toString ++ "_payload_of_spec")))))
  for (n, _) in ctx.axioms.toList do hints := hints.push (← `($(mkIdent n)))
  let closeTac (handlers : Array Ident) : CommandElabM (TSyntax ``Lean.Parser.Tactic.tacticSeq) :=
    `(Lean.Parser.Tactic.tacticSeq| pverify_close [$[$handlers],*] [$[$defs],*] [$[$hints],*])
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
  -- AllInv
  let gI := mkIdent `g
  elabCommand (← `(@[reducible] def $(mkIdent `AllInv) ($gI:ident : $(mkIdent `GW)) : Prop :=
      $(← conjAt allTargets gI)))
  let mut results : Array ObResult := #[]
  let runOb (obName : Name) (cmd : TSyntax `command) : CommandElabM ObResult := do
    let t0 ← IO.monoMsNow
    let (errs, goals, trusted) ← elabCaptureGoals (← wrap cmd)
    let t1 ← IO.monoMsNow
    return { name := obName, ok := errs.isEmpty, ms := t1 - t0, goals, trusted,
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
  -- per-target obligations
  for t in allTargets do
    let preMembers := #[t] ++ ((usingOf.find? t).getD #[]) ++ (if hasNoHalt && t != `NoHalt then #[] else #[])
    let pre ← conjAt preMembers g
    let post ← `(fun g' => $(mkIdent t) g')
    for (mname, sname, ev, act) in handled do
      let tag := mkIdent (`S ++ stateTagName mname sname)
      let obName := mname ++ sname ++ Name.mkSimple (ev.toString ++ "_correct_" ++ t.toString)
      let hyps ← deliverHyps g i src dst pre (← evVal ev) tag
      let concl ← `(PLean.RunOK $post (($g).consume $i) $dst (($g).machines $dst) $act)
      let stmt ← arrows hyps concl
      let hname := mkIdent (mname ++ sname ++ ev.appendAfter "_handler")
      let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($i:ident : Nat)
          ($src:ident $dst:ident : PLean.MachineRef) $(← plBinder ev)* : $stmt := by
        $(← closeTac #[hname]))
      results := results.push (← runOb obName cmd)
    for (mname, sname) in stateTags do
      let tag := mkIdent (`S ++ stateTagName mname sname)
      let obName := mname ++ sname ++ Name.mkSimple ("entry_correct_" ++ t.toString)
      let hyps := #[← `(PLean.Framework $g), ← `(($g).error = none), pre,
        ← `((($g).machines $m).state = some $tag), ← `((($g).machines $m).pending = some $arg),
        ← `((($g).machines $m).halted = false)]
      let entryFn := mkIdent (mname ++ sname ++ `entry)
      let concl ← `(PLean.RunOK $post $g $m { ($g).machines $m with pending := none } ($entryFn $arg))
      let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($m:ident : PLean.MachineRef)
          ($arg:ident : $(mkIdent `G)) : $(← arrows hyps concl) := by
        $(← closeTac #[entryFn]))
      results := results.push (← runOb obName cmd)
    if !hasNoHalt then
      let obName := Name.mkSimple ("drop_correct_" ++ t.toString)
      let p := mkIdent `p
      let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($i:ident : Nat) ($p:ident : PLean.Packet $(mkIdent `E)) :
          PLean.Framework $g → ($g).error = none → $pre → ($g).net $i = some $p →
          (($g).machines ($p).dst).halted = true → $(mkIdent t) (($g).consume $i) := by
        $(← closeTac #[]))
      results := results.push (← runOb obName cmd)
    let baseName := Name.mkSimple ("base_" ++ t.toString)
    let cmd ← `(command| theorem $(mkIdent baseName) ($g:ident : $gwTy) :
        $(mkIdent `InitConditions) $g → $(mkIdent t) $g := by
      $(← closeTac #[]))
    results := results.push (← runOb baseName cmd)
  -- never-receives
  let allPre ← `($(mkIdent `AllInv) $g)
  for (mname, sname, ev) in never do
    let tag := mkIdent (`S ++ stateTagName mname sname)
    let obName := mname ++ sname ++ Name.mkSimple (ev.toString ++ "_never")
    let hyps ← deliverHyps g i src dst allPre (← evVal ev) tag
    let cmd ← `(command| theorem $(mkIdent obName) ($g:ident : $gwTy) ($i:ident : Nat)
        ($src:ident $dst:ident : PLean.MachineRef) $(← plBinder ev)* : $(← arrows hyps (← `(False))) := by
      $(← closeTac #[]))
    results := results.push (← runOb obName cmd)
  -- assembly (not counted as obligations)
  let n := allTargets.size
  let hName (k : Nat) : Ident := mkIdent (Name.mkSimple s!"hpre{k}")
  let preNames := (List.range n).toArray.map hName
  let destruct ← if n ≤ 1 then `(tactic| skip)
    else `(tactic| obtain ⟨$[$preNames],*⟩ := hpre)
  let preOf (t : Name) : CommandElabM (TSyntax `term) := do
    let members := #[t] ++ ((usingOf.find? t).getD #[])
    let hs ← members.mapM fun u => do
      if n ≤ 1 then `(hpre) else pure (hName (allTargets.idxOf u))
    if hs.size = 1 then `($(hs[0]!)) else `(⟨$[$hs],*⟩)
  let combineRun (mk : Name → CommandElabM (TSyntax `term)) : CommandElabM (TSyntax `term) := do
    let parts ← allTargets.mapM mk
    let body ← parts.pop.foldrM (init := parts.back!) fun p acc => `(PLean.RunOK.and $p $acc)
    `(PLean.RunOK.mono (fun _ h => h) $body)
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
      if hasPl then `($ob $g $i $src $dst $pl hF herr $(← preOf t) hnet hlog hlt hs hpend hwait hhalt)
      else `($ob $g $i $src $dst hF herr $(← preOf t) hnet hlog hlt hs hpend hwait hhalt)
    elabCommand (← `(command| theorem $(mkIdent cn) ($g:ident : $gwTy) ($i:ident : Nat)
        ($src:ident $dst:ident : PLean.MachineRef) $(← plBinder ev)* : $(← arrows hyps concl) := by
      intro hF herr hpre hnet hlog hlt hs hpend hwait hhalt
      $destruct:tactic
      exact ⟨_, rfl, $body⟩))
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
    elabCommand (← `(command| theorem $(mkIdent cn) ($g:ident : $gwTy) ($i:ident : Nat)
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
    elabCommand (← `(command| theorem $(mkIdent cn) ($g:ident : $gwTy) ($m:ident : PLean.MachineRef)
        ($arg:ident : $(mkIdent `G)) : PLean.Framework $g → ($g).error = none → $allPre →
        (($g).machines $m).state = some $tag → (($g).machines $m).pending = some $arg →
        (($g).machines $m).halted = false →
        PLean.RunOK $(mkIdent `AllInv) $g $m { ($g).machines $m with pending := none }
          ($(mkIdent `prog).entry $tag $arg) := by
      intro hF herr hpre hs hpend hhalt
      $destruct:tactic
      exact $body))
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
      · intro g i p k _ _ _ _ _ _ _ act hr; simp [$(mkIdent `prog):ident] at hr
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
      obtain ⟨_, hn, hl, he, hu, _⟩ := hI'
      refine ⟨⟨fun j _ => hn j, fun j _ => hl j, ?_, hu⟩, he, $baseConj⟩
      intro j p h; rw [hn j] at h; cases h)
  let safeThm ← `(command|
    theorem $(mkIdent `safe) : ∀ g, PLean.GReachable $(mkIdent `prog) .bag $(mkIdent `InitConditions) g →
        $(mkIdent `AllInv) g := fun g hg =>
      (PLean.safety_of_step $(mkIdent `prog) $(mkIdent `InitConditions) $(mkIdent `AllInv)
        $(mkIdent `base_AllInv) $(mkIdent `step_AllInv) g hg).2.2)
  let safeFifo ← `(command|
    theorem $(mkIdent `safe_fifo) : ∀ g, PLean.GReachable $(mkIdent `prog) .fifo $(mkIdent `InitConditions) g →
        $(mkIdent `AllInv) g := fun g hg => $(mkIdent `safe) g hg.fifo_bag)
  let mut asmErrs : Array String := #[]
  for cmd in #[stepThm, baseThm, safeThm, safeFifo] do
    asmErrs := asmErrs ++ (← elabCapture (← wrap cmd))
  for (t, k) in allTargets.toList.zipIdx do
    if t == `NoHalt then continue
    let (gq, hgq) := (mkIdent `g, mkIdent `hg)
    let proj ← (List.range k).foldlM (init := ← `($(mkIdent `safe_fifo) $gq $hgq)) fun acc _ => `(($acc).2)
    let proj ← if k + 1 < n then `(($proj).1) else pure proj
    asmErrs := asmErrs ++ (← elabCapture (← `(command|
      theorem $(mkIdent (Name.mkSimple (t.toString ++ "_holds"))) : ∀ g,
          PLean.GReachable $(mkIdent `prog) .fifo $(mkIdent `InitConditions) g → $(mkIdent t) g :=
        fun $gq $hgq => $proj)))
  elabCommand (← `(end $name))
  -- report
  let failed := results.filter (!·.ok)
  let total := results.foldl (· + ·.ms) 0
  let showT := pverify.showTimes.get opts
  let tm (ms : Nat) : String := if showT then s!" ({ms} ms)" else ""
  let nTrusted := (results.filter fun r => r.ok && r.trusted).size
  let mut lines : Array String := #[s!"#pverify {modName}: {results.size} obligations, {results.size - failed.size} proved ({nTrusted} trusting the solver), {failed.size} failed{tm total}"]
  for r in results do
    let status := if !r.ok then s!"✗ [{failureKind r.msg}]" else if r.trusted then "✓ (trusted)" else "✓"
    lines := lines.push s!"  {status} {r.name}{tm r.ms}{if r.ok then "" else ": " ++ r.msg}"
  for e in asmErrs do lines := lines.push s!"  assembly error: {excerpt e}"
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
    for r in failed do
      for gl in r.goals do h.putStrLn s!"--- {r.name}\n{gl}"
  catch _ => pure ()
  if failed.isEmpty && asmErrs.isEmpty then logInfo report
  else if pverify.failOnIncomplete.get opts then logError report
  else logWarning report

end PLean
