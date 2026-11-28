/-
PLean.Verify.Tactic — `pverify_close`, the automation for an obligation.

  pverify_close [h₁, …] [d₁, …] [l₁, …] [a₁, …]

1. unfolds `RunPost` / `RunSafe` and introduces the coin stream (handler
   obligations), and unfolds the handler definitions `hᵢ`,
2. adds the facts `aᵢ` (axioms) as hypotheses and normalises the premises
   once, with `pverifySimp` plus the definitions `dᵢ` (invariants, kind
   predicates, …): every goal made below starts from them,
3. runs `vcgen` with the premises out of its sight (`stashHyps`: `vcgen`
   preprocesses each hypothesis it sees), splits the post-condition into
   one goal per invariant (`PostInv`), tagged with the invariant's name
   (`pv_split_post`), and records each `foreach` cursor's membership
   (`pv_cursor_mem`),
4. substitutes the start state and, goal by goal, normalises and
   destructures message / machine-wrapper locals and conjunctive /
   existential hypotheses (`pv_destruct`) until a round changes nothing,
   splits the goal's conjunctions, adds the lookup equations of the set
   and map updates in each goal (`pv_lookup_facts`), and
5. discharges each conjunct with `crush`, given the hints `lᵢ`: briefly
   with z3 and cvc5 without crush's ground instantiation, then with it at
   the full timeout (cvc5, then z3). A solver's `unsat` is replayed with
   `grind` on its unsat core first, then by crush's own search.

It logs how its goals were closed and where the time went (`pverify-stats:
<by a hypothesis> <by crush before SMT translation> <by an SMT solver>
<ms in vcgen and normalisation> <ms in crush>`). When a goal is not
proved, it logs the goal (`pverify-goal:`), the invariant it belongs to
(`pverify-inv:`) and, when z3 finds one, a small model of the goal's
encoding (`pverify-smt-script:` / `pverify-smt-model:`, see `PLean.Cex`),
for `#pverify`'s report.
-/
import PLean.Verify.Norm
import PLean.Verify.Cex
import PLean.Verify.Progress
import PLean.Verify.Cache
import PLean.Semantics.Msg
import Crush

namespace PLean

/-! `k ∈ m` on a map is `(m.get k).isSome`. Without `Option.isSome`'s two equations a
solver takes it for an unknown function, and a key's presence says nothing about the key's
value; every query that mentions it gets them. (What the solvers know about the lookups
themselves: `pv_lookup_facts`.) -/
attribute [crush_unfold] Option.isSome

register_option pverify.profile : Bool := {
  defValue := false
  descr := "Record, per goal handed to the solver, its time, the solver that closed it, and \
            crush's phase breakdown (in `#pverify`'s report file)." }

register_option pverify.cex : Bool := {
  defValue := true
  descr := "Search for a counterexample (a small model of the goal's encoding) when a goal is \
            not proved." }

register_option pverify.leaveGoals : Bool := {
  defValue := false
  descr := "Leave the goals the solvers cannot prove open, instead of failing (and log \
            nothing for `#pverify`'s report); `pverify_auto` sets it." }

register_option pverify.quick : Bool := {
  defValue := false
  descr := "One short solver attempt per goal (z3, 3 s), e.g. for `#pverify`'s check that \
            `InitConditions` is satisfiable." }

register_option pverify.heartbeats : Nat := {
  defValue := 0
  descr := "Heartbeat budget (in thousands, 0 = unlimited) of each solver attempt, so a slow \
            proof reconstruction gives up (the attempt then trusts the solver, under \
            `crush.trust reconstructOrTrust`)." }

open Lean Elab Tactic in
/-- A small model of `g`'s encoding by `crushTac` (`Cex.findModel`): the
script z3 solved and its model. -/
def findCex (g : MVarId) (crushTac : TSyntax `tactic) : TacticM (Option (String × String)) := do
  let (h, path) ← IO.FS.createTempFile
  h.flush
  let s ← saveState
  try
    setGoals [g]
    evalTactic (← `(tactic| (set_option crush.backend "z3" in set_option crush.timeout 1 in
      set_option crush.inst.fuel 0 in set_option crush.save $(Syntax.mkStrLit path.toString):str in
      $crushTac:tactic)))
  catch _ => pure ()
  s.restore (restoreInfo := true)
  let script ← IO.FS.readFile path
  try IO.FS.removeFile path catch _ => pure ()
  if script.isEmpty then return none
  Cex.findModel script

open Lean Elab Tactic Meta in
/-- Run the `simp` call `stx` (written without a location) on the main goal's proposition
hypotheses, leaving the target as it is. -/
def simpHypsOnly (stx : Syntax) : TacticM Unit := withMainContext do
  let { ctx, simprocs, dischargeWrapper, .. } ← mkSimpContext stx (eraseLocal := false)
  let g ← getMainGoal
  let fvars ← g.getNondepPropHyps
  let (r, _) ← dischargeWrapper.with fun discharge? =>
    simpGoal g ctx simprocs discharge? (simplifyTarget := false) (fvarIdsToSimp := fvars)
  match r with
  | none => replaceMainGoal []
  | some (_, g') => replaceMainGoal [g']

/-- A proposition kept as data: a local `h : Stashed P` is not a hypothesis (`stashHyps`). -/
structure Stashed (P : Prop) : Type where
  /-- The proof. -/
  down : P

open Lean Elab Tactic Meta in
/-- Replace each proposition hypothesis `h : P` of the main goal by `h : Stashed P`, in
place (`unstashHyps` undoes it).

`vcgen` preprocesses every hypothesis in the context before it looks at the program
(it unfolds them and prepares each quantified one for instantiation). For an obligation
that assumes many invariants that takes far longer than generating its conditions, which
need none of them. A local whose type is not a proposition is left alone. -/
def stashHyps : TacticM Unit := liftMetaTactic fun g => g.withContext do
  let mut g := g
  -- Last first: `assertAfter` renames only the locals after the one it is given.
  for h in (← g.getNondepPropHyps).reverse do
    let d ← g.withContext h.getDecl
    let r ← g.assertAfter h d.userName (← mkAppM ``Stashed #[d.type])
      (← mkAppOptM ``Stashed.mk #[d.type, mkFVar h])
    g ← r.mvarId.clear h
  return [g]

open Lean Elab Tactic Meta in
/-- Undo `stashHyps` in the main goal: each `h : Stashed P` is `h : P` again, in place. -/
def unstashHyps : TacticM Unit := liftMetaTactic fun g => do
  let mut g := g
  repeat
    let some d ← g.withContext do
        return (← getLCtx).foldl (init := none) fun acc d =>
          if !d.isImplementationDetail && d.type.isAppOfArity ``Stashed 1 then some d else acc
      | break
    let r ← g.assertAfter d.fvarId d.userName (d.type.getArg! 0)
      (← g.withContext (mkAppM ``Stashed.down #[d.toExpr]))
    g ← r.mvarId.clear d.fvarId
  return [g]

syntax (name := pverifyClose)
  "pverify_close" "[" ident,* "]" "[" ident,* "]" "[" term,* "]" "[" term,* "]" : tactic

private def has (s pat : String) : Bool := (s.splitOn pat).length > 1

open Lean Elab Tactic in
@[tactic pverifyClose]
def evalPverifyClose : Tactic := fun stx => do
  let hs : Array Ident := stx[2].getSepArgs.map (⟨·⟩)
  let ds : Array Ident := stx[5].getSepArgs.map (⟨·⟩)
  let ls : Array (TSyntax `term) := stx[8].getSepArgs.map (⟨·⟩)
  let as : Array (TSyntax `term) := stx[11].getSepArgs.map (⟨·⟩)
  let tStart ← IO.monoMsNow
  Progress.setDetail "vcgen and normalisation"
  evalTactic (← `(tactic| intros))
  evalTactic (← `(tactic|
    try ((first | unfold PLean.RunPost | unfold PLean.RunSafe | unfold PLean.RunOK); intro coins)))
  for h in hs do evalTactic (← `(tactic| try unfold $h:ident))
  let simpArgs ← ds.mapM fun d => `(Lean.Parser.Tactic.simpLemma| $d:ident)
  -- The first pass keeps `sentSince` folded, so a loop's sends are peeled off
  -- one `push` at a time (`sentSince_push`).
  let norm0 ← `(tactic| all_goals (simp -failIfUnchanged +zetaDelta only
    [pverifySimp, reduceCtorEq, $[$simpArgs],*] at *))
  let normHyps ← `(tactic| simp -failIfUnchanged +zetaDelta only
    [pverifySimp, reduceCtorEq, $(mkIdent ``PLean.sentSince):ident, $[$simpArgs],*])
  let norm ← `(tactic| all_goals (simp -failIfUnchanged +zetaDelta only
    [pverifySimp, reduceCtorEq, $(mkIdent ``PLean.sentSince):ident, $[$simpArgs],*] at *))
  -- Facts join the hypotheses, so they are normalised like them.
  for a in as do
    evalTactic (← `(tactic| all_goals (try have := $a:term)))
  -- The premises are the same in every goal `vcgen` makes: they are normalised once,
  -- here, and kept out of `vcgen`'s sight (`stashHyps`).
  let single := (← getGoals).length == 1
  if single then simpHypsOnly normHyps
  let stashed := single && (← getGoals).length == 1
  if stashed then stashHyps
  evalTactic (← `(tactic| try vcgen))
  if stashed then
    let mut gs := []
    for g in ← getGoals do
      setGoals [g]
      unstashHyps
      gs := gs ++ (← getGoals)
    setGoals gs
  evalTactic (← `(tactic| pv_split_post))
  evalTactic (← `(tactic| pv_cursor_mem))
  evalTactic (← `(tactic| all_goals (try subst_vars)))
  evalTactic norm0
  -- Goal by goal: normalise, destructure, substitute, and again (at most four times)
  -- while destructuring or substitution changed the goal. A pass over a goal they left
  -- as it was would find nothing to rewrite.
  let mut done : Array MVarId := #[]
  let mut todo : List (MVarId × Nat) := (← getGoals).map (·, 0)
  while !todo.isEmpty do
    let (g, round) := todo.head!
    todo := todo.tail!
    setGoals [g]
    evalTactic norm
    let normed ← getGoals
    evalTactic (← `(tactic| all_goals pv_destruct))
    if round ≥ 3 then
      done := done ++ (← getGoals).toArray
      continue
    evalTactic (← `(tactic| all_goals (try subst_vars)))
    let after ← getGoals
    if after == normed then done := done ++ after.toArray
    else todo := after.map (·, round + 1) ++ todo
  setGoals done.toList
  evalTactic (← `(tactic| all_goals (try and_intros)))
  evalTactic (← `(tactic| all_goals pv_clear_lets))
  evalTactic (← `(tactic| all_goals pv_lookup_facts))
  -- A hint about `<ev>_payload_of` matters only to goals that mention it (a
  -- fact about a symbol nothing else mentions cannot help a proof), and
  -- asserting it elsewhere keeps the solvers from finding models.
  let hintKeys ← ls.mapM fun l => do
    let some n ← (try pure (some (← realizeGlobalConstNoOverload l.raw)) catch _ => pure none)
      | return #[]
    let some ci := (← getEnv).find? n | return #[]
    return ci.type.getUsedConstants.filter fun c =>
      match c with
      | .str _ s => s.endsWith "_payload_of"
      | _ => false
  let profile := pverify.profile.get (← getOptions)
  -- Solver portfolio: quick attempts without crush's ground instantiation
  -- (the solvers' own quantifier instantiation usually suffices, and is much
  -- cheaper), then full-length attempts with it.
  let portfolioFor (g : MVarId) : TacticM (TSyntax `tactic × List (String × TSyntax `tactic)) :=
      g.withContext do
    let mut used : NameSet := {}
    for c in (← instantiateMVars (← g.getType)).getUsedConstants do used := used.insert c
    for d in ← getLCtx do
      unless d.isImplementationDetail do
        for c in (← instantiateMVars d.type).getUsedConstants do used := used.insert c
    let hints ← ((ls.zip hintKeys).filter fun (_, ks) => ks.isEmpty || ks.any used.contains).mapM
      fun (l, _) => `(Crush.crushHintElem| $l:term)
    -- crush replays a solver's `unsat` from its unsat core: by a search for a hypothesis to
    -- apply backwards, then by `grind` and other tactics. On these goals `grind` usually
    -- closes the core at once and the search before it can take most of a minute, so
    -- `grind` goes first (`using`, which crush refuses when no proof is replayed).
    let replay := (← getOptions).get `crush.trust "trust" != "trust"
    let crush1 ←
      if replay then
        if hints.isEmpty then `(tactic| crush [*] using grind)
        else `(tactic| crush [*, $[$hints],*] using grind)
      else if hints.isEmpty then `(tactic| crush [*]) else `(tactic| crush [*, $[$hints],*])
    -- `crush.result` traces say how crush closed the goal.
    let traced ← `(tactic| set_option trace.crush.result true in $crush1:tactic)
    let prof ← if profile then
        `(Lean.Parser.Tactic.tacticSeq| set_option crush.profile true in $traced:tactic)
      else `(Lean.Parser.Tactic.tacticSeq| $traced:tactic)
    if pverify.quick.get (← getOptions) then
      return (crush1, [("z3 (3s)", ← `(tactic| (set_option crush.backend "z3" in
          set_option crush.timeout 3 in set_option crush.inst.fuel 0 in ($prof))))])
    return (crush1, [
      ("z3 (5s)", ← `(tactic| (set_option crush.backend "z3" in set_option crush.timeout 5 in
          set_option crush.inst.fuel 0 in ($prof)))),
      ("cvc5 (5s)", ← `(tactic| (set_option crush.backend "cvc5" in set_option crush.timeout 5 in
          set_option crush.inst.fuel 0 in ($prof)))),
      ("cvc5", ← `(tactic| (set_option crush.backend "cvc5" in ($prof)))),
      ("z3", ← `(tactic| (set_option crush.backend "z3" in ($prof))))])
  let budget := pverify.heartbeats.get (← getOptions)
  let bounded (tac : TSyntax `tactic) : TacticM Unit :=
    if budget == 0 then evalTactic tac
    else withTheReader Core.Context (fun c => { c with maxHeartbeats := budget * 1000 })
      (Core.withCurrHeartbeats (evalTactic tac))
  -- Conjuncts that are hypotheses (a loop invariant carried through an
  -- iteration) need no solver.
  let before := (← getGoals).length
  evalTactic (← `(tactic| all_goals (try assumption)))
  -- How the goals were closed: by a hypothesis (here, or crush finding the
  -- goal among its facts), by crush before SMT translation (a checked Lean
  -- proof), or by an SMT solver.
  let mut nHyp : Nat := before - (← getGoals).length
  let mut nPre : Nat := 0
  let mut nSmt : Nat := 0
  let normMs := (← IO.monoMsNow) - tStart
  let mut crushMs : Nat := 0
  let goals ← getGoals
  let leave := pverify.leaveGoals.get (← getOptions)
  let mut left : List MVarId := []
  -- the goal cache of `#pverify`'s module (`PLean.Cache`)
  let cacheMod := (Cache.pverify.cacheModule.get (← getOptions)).toName
  let cacheOn := Cache.pverify.cache.get (← getOptions) && !cacheMod.isAnonymous
  let allowTrusted := (← getOptions).get `crush.trust "reconstructOrTrust" != "reconstruct"
  let owner := (← Term.getDeclName?).getD .anonymous
  let mut nCache : Nat := 0
  for g in goals, k in [0:goals.length] do
    let t0 ← IO.monoMsNow
    if cacheOn then
      if ← Cache.tryGoal cacheMod g owner allowTrusted then
        nCache := nCache + 1
        crushMs := crushMs + ((← IO.monoMsNow) - t0)
        continue
    let target ← g.getType
    let mut closedBy : Option String := none
    let mut errs : Array (String × Exception) := #[]
    let (crushTac, portfolio) ← portfolioFor g
    let invS := match invOfTag (← g.getTag).eraseMacroScopes with
      | some i => s!" (invariant `{i}`)"
      | none => ""
    let goalS := s!"goal {k + 1}/{goals.length}{invS}"
    for (solver, tac) in portfolio do
      Progress.setDetail s!"{goalS}: {solver}"
      setGoals [g]
      let s ← saveState
      let traces0 ← getTraceState
      modifyTraceState fun t => { t with traces := {} }
      try
        bounded tac
        closedBy := some solver
        let mut route := 2
        for t in (← getTraceState).traces.toList do
          let m ← t.msg.toString
          if has m "goal is one of the selected facts" then route := 0
          else if has m "pre-translation checked proof succeeded" then route := 1
        match route with
        | 0 => nHyp := nHyp + 1
        | 1 => nPre := nPre + 1
        | _ => nSmt := nSmt + 1
        modifyTraceState fun _ => traces0
        if cacheOn then
          Cache.recordGoal cacheMod g target owner
        break
      catch e =>
        s.restore (restoreInfo := true)
        modifyTraceState fun _ => traces0
        errs := errs.push (solver, e)
        -- A model of the whole query: the later attempts (the same query, plus
        -- instances of its quantified facts) cannot prove the goal either.
        let msg ← e.toMessageData.toString
        if has msg "found a model" && !has msg "refused part of the query" then break
    crushMs := crushMs + ((← IO.monoMsNow) - t0)
    if profile then
      let ms := (← IO.monoMsNow) - t0
      let slow ← if ms > 5000 && closedBy.isSome then
          pure m!"\n{← Meta.ppGoal g}" else pure m!""
      logInfo m!"pverify-time: {(← g.getTag).eraseMacroScopes}: {ms} ms, {closedBy.getD "failed"}{slow}"
    if closedBy.isNone && leave then
      left := left ++ [g]
      continue
    if closedBy.isNone then
      -- Record the goal handed to `crush` and its invariant (for `#pverify`'s report).
      logInfo m!"pverify-goal: {← Meta.ppGoal g}"
      if let some inv := invOfTag (← g.getTag).eraseMacroScopes then
        logInfo m!"pverify-inv: {inv}"
      let mut refuted := false
      for (_, e) in errs do
        if has (← e.toMessageData.toString) "reported `unsat`" then refuted := true
      -- A goal a solver refuted (but no proof could be replayed) has no model.
      if pverify.cex.get (← getOptions) && !refuted then
        Progress.setDetail s!"{goalS}: searching for a counterexample"
        if let some (script, model) ← findCex g crushTac then
          logInfo m!"pverify-smt-script:\n{script}"
          logInfo m!"pverify-smt-model:\n{model}"
      -- Fail with a model when some attempt found one (z3's, which `#pverify`
      -- decodes, first; the last of them has the most instantiations).
      let mut best : Option Exception := errs.back?.map (·.2)
      let mut bestRank := 0
      for (solver, e) in errs do
        if has (← e.toMessageData.toString) "found a model" then
          let rank := if solver.startsWith "z3" then 2 else 1
          if rank ≥ bestRank then
            best := some e
            bestRank := rank
      logInfo m!"pverify-stats: {nHyp} {nPre} {nSmt} {nCache} {normMs} {crushMs}"
      if let some e := best then throw e
  unless leave do logInfo m!"pverify-stats: {nHyp} {nPre} {nSmt} {nCache} {normMs} {crushMs}"
  setGoals left

end PLean
