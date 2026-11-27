/-
PLean.Verify.Cache — reusing proofs across runs of `#pverify`.

Two levels, both replayed through the kernel, so a stale entry only misses
(the obligation or goal is then proved afresh) and cannot make a false
theorem pass:

* **Obligations** (and `#pverify`'s final theorems, `step_AllInv`, `safe`, ...).
  A proof term, keyed by a hash of the theorem's statement and of every
  definition of the module it depends on (handlers, invariants, types; a
  theorem by its statement only), and the facts and hints the solver is
  given. When nothing it depends on changed, `#pverify` kernel-checks the
  stored proof instead of running `vcgen`, the normalisation and the
  solvers.
* **Goals.** A goal a solver closed, as a lemma `∀ deps, target` over just
  the hypotheses its proof uses, keyed by the shape of `target`. When an
  obligation changes (an invariant is added or edited, a handler changes),
  its goals are normalised again, and a goal that matches a stored lemma
  (same conclusion, and its hypotheses among the goal's) reuses it without a
  solver call: only the goals the change affects reach the solver.

A reused proof may use no axiom beyond Lean's own, the module's `paxiom`s and
`pinstance`s, and the solver's word when `pverify.trust` allows it; a cached
file never adds an axiom.

A goal lemma inlines the run-specific auxiliary theorems its proof uses
(`…._proof_3`), so it does not depend on their names, and records the
obligation it came from. Entries live in memory for the session and on disk
in `.lake/pverify/cache/<M>.olean` (Lean's module-data format), which a full
run of `#pverify M` prunes to the entries it used and the goal lemmas of the
obligations it replayed.
-/
import Lean

namespace PLean.Cache

open Lean Meta

register_option pverify.cache : Bool := {
  defValue := true
  descr := "Reuse proofs of obligations and goals from earlier runs of `#pverify` (replayed \
            through the kernel)." }

register_option pverify.cacheModule : String := {
  defValue := ""
  descr := "(internal) the module whose goal cache `pverify_close` uses; set by `#pverify`." }

/-- A module's cache: entries (and the auxiliary declarations their proofs
use) by name, and what this run used or added. -/
structure Store where
  consts : NameMap ConstantInfo := {}
  used : NameSet := {}
  /-- The obligations whose cached proofs this run replayed. -/
  replayed : NameSet := {}
  /-- The axioms, besides Lean's own and the solver's word, that a cached
  proof may use: the module's `paxiom`s and `pinstance`s (set by `#pverify`). -/
  allowed : NameSet := {}

instance : Inhabited Store := ⟨{}⟩

initialize stores : IO.Ref (Std.HashMap Name Store) ← IO.mkRef {}
/-- Regions of loaded cache files: their objects live as long as the process. -/
initialize regions : IO.Ref (Array CompactedRegion) ← IO.mkRef #[]

def dir : System.FilePath := ".lake" / "pverify" / "cache"

def fileOf (mod : Name) : System.FilePath := dir / s!"{mod}.olean"

/-- Load module `mod`'s cache from disk, once per process. -/
def ensureLoaded (mod : Name) : IO Unit := do
  if (← stores.get).contains mod then return
  let mut st : Store := {}
  let f := fileOf mod
  if ← f.pathExists then
    try
      let (data, region) ← readModuleData f
      regions.modify (·.push region)
      for c in data.constants do st := { st with consts := st.consts.insert c.name c }
    catch _ => pure ()
  stores.modify fun m => if m.contains mod then m else m.insert mod st

def find? (mod n : Name) : BaseIO (Option ConstantInfo) :=
  return (← stores.get)[mod]? >>= (·.consts.find? n)

def touch (mod n : Name) : BaseIO Unit :=
  stores.modify fun m => m.modify mod fun st => { st with used := st.used.insert n }

def insert (mod : Name) (c : ConstantInfo) : BaseIO Unit :=
  stores.modify fun m => m.modify mod fun st =>
    { st with consts := st.consts.insert c.name c, used := st.used.insert c.name }

/-- Record that this run replayed the cached proof of obligation `owner`. -/
def markReplayed (mod owner : Name) : BaseIO Unit :=
  stores.modify fun m => m.modify mod fun st => { st with replayed := st.replayed.insert owner }

/-- Set the axioms a cached proof of `mod` may use besides Lean's own and the
solver's word. -/
def setAllowed (mod : Name) (axs : NameSet) : BaseIO Unit :=
  stores.modify fun m => m.modify mod fun st => { st with allowed := axs }

/-- The number of obligation proofs and goal lemmas cached for `mod`. -/
def counts (mod : Name) : BaseIO (Nat × Nat) := do
  let some st := (← stores.get)[mod]? | return (0, 0)
  return st.consts.foldl (init := (0, 0)) fun (o, g) n _ =>
    if (`_pvcache.ob).isPrefixOf n then (o + 1, g)
    else if (`_pvcache.goal).isPrefixOf n then (o, g + 1) else (o, g)

/-- The obligation an entry belongs to (stored as the theorem's `all`). -/
def ownerOf (c : ConstantInfo) : Option Name :=
  match c with
  | .thmInfo v => v.all.head?
  | _ => none

/-- Write `mod`'s cache to disk. With `prune`, keep what this run used or added,
the goal lemmas of the obligations whose proofs it replayed (their goals were
not normalised again, and they are needed when the obligation changes), and
the auxiliary declarations they need; an obligation proved afresh keeps only
the lemmas its proof used or added. The file is written under a name of its
own and then renamed into place, so concurrent saves (the editor and `lake
build`) cannot mix, and a loaded file, which may still be mapped, is not
overwritten. -/
def save (mod : Name) (prune : Bool) : IO Unit := do
  let some st := (← stores.get)[mod]? | return
  let mut keep := st.consts
  if prune then
    keep := {}
    let mut todo := st.used.toList ++ st.consts.foldl (init := []) fun acc n c =>
      if (`_pvcache.goal).isPrefixOf n && ((ownerOf c).any st.replayed.contains) then n :: acc else acc
    while !todo.isEmpty do
      let n := todo.head!
      todo := todo.tail!
      if keep.contains n then continue
      let some c := st.consts.find? n | continue
      keep := keep.insert n c
      for d in c.type.getUsedConstants ++ ((c.value? (allowOpaque := true)).map (·.getUsedConstants)).getD #[] do
        if st.consts.contains d && !keep.contains d then todo := d :: todo
  let cs := keep.foldl (init := #[]) fun acc _ c => acc.push c
  IO.FS.createDirAll dir
  let tmp := dir / s!"{mod}.olean.{← IO.Process.getPID}.{← IO.monoNanosNow}.tmp"
  try
    saveModuleData tmp (`PLeanCache ++ mod)
      { isModule := false, imports := #[], constNames := cs.map (·.name), constants := cs,
        extraConstNames := #[], entries := #[] }
    IO.FS.rename tmp (fileOf mod)
  catch e =>
    try IO.FS.removeFile tmp catch _ => pure ()
    throw e
  stores.modify fun m => m.modify mod fun st =>
    { st with consts := keep, used := {}, replayed := {} }

/-- Marks the conclusion of a cached goal lemma `∀ hyps, Concl target`, so that
opening its hypotheses stops before `target`'s own quantifiers. -/
def Concl (p : Prop) : Prop := p

/-! ## Keys -/

/-- The name of the cached proof of an obligation with key `k`. -/
def obName (k : UInt64) : Name := `_pvcache.ob ++ Name.mkSimple (toString k)

/-- The name of the `i`-th cached lemma for goals with key `k`. -/
def goalName (k : UInt64) (i : Nat) : Name :=
  `_pvcache.goal ++ Name.mkSimple (toString k) ++ Name.mkSimple (toString i)

/-- The key of an obligation with statement `stmt`: the statement, every
constant of the current module it depends on (transitively: their types and
values), and the constants in `extra` (facts and hints) likewise. -/
def obKey (env : Environment) (stmt : Expr) (extra : Array Name) : UInt64 := Id.run do
  let isLocal (n : Name) := (env.getModuleIdxFor? n).isNone && env.contains n
  let mut seen : NameSet := {}
  let mut todo : List Name := (stmt.getUsedConstants ++ extra).toList
  while !todo.isEmpty do
    let n := todo.head!
    todo := todo.tail!
    if seen.contains n || !isLocal n then continue
    seen := seen.insert n
    let some c := env.find? n | continue
    let deps := c.type.getUsedConstants ++
      (if c.isTheorem then #[] else ((c.value? (allowOpaque := true)).map (·.getUsedConstants)).getD #[]) ++
      (match c with
       | .inductInfo v => v.ctors.toArray
       | .ctorInfo v => #[v.induct]
       | _ => #[])
    todo := deps.toList ++ todo
  let names := seen.toArray.qsort Name.lt
  let mut h : UInt64 := hash stmt
  for n in names do
    let some c := env.find? n | continue
    -- a theorem counts by its statement only (any proof of it will do)
    let vh := if c.isTheorem then 0 else ((c.value? (allowOpaque := true)).map hash).getD 0
    h := mixHash h (mixHash (hash n) (mixHash (hash c.type) vh))
  return h

/-- How many lemmas are kept per goal key. -/
def slots : Nat := 64

/-- How many unifications a lookup of a goal may try, over all the lemmas of
its key. -/
def matchBudget : Nat := 32000

/-- The invariant of a goal tagged `pv_inv.<invariant>…` (`PLean.splitPost`). -/
def invOf (tag : Name) : Name :=
  match tag.components with
  | `pv_inv :: i :: _ => i
  | _ => .anonymous

/-- The key of goals of invariant `inv` with conclusion `target`: the
invariant and the conclusion's shape, with the local variables erased (which
of them a lemma applies to is checked on use). -/
def goalKey (inv : Name) (target : Expr) : MetaM UInt64 := do
  let t ← instantiateMVars target
  return mixHash (hash inv)
    (hash (t.replace fun e => if e.isFVar then some (mkConst `_pvcache.var) else none))

/-! ## Replay -/

/-- The declaration of a cached constant. Never an axiom: a cached proof may
use only axioms the environment already has. -/
def declOf : ConstantInfo → Option Declaration
  | .thmInfo v => some (.thmDecl v)
  | .defnInfo v => some (.defnDecl v)
  | .opaqueInfo v => some (.opaqueDecl v)
  | _ => none

/-- Add to the environment the cached auxiliary declarations `e` uses that it
lacks (kernel-checked, dependencies first); `false` when one is not cached. -/
partial def ensureAux (mod : Name) (e : Expr) : CoreM Bool := do
  for c in e.getUsedConstants do
    if (← getEnv).contains c then continue
    let some ci ← find? mod c |
      if (← IO.getEnv "PVERIFY_DEBUG_CACHE").isSome then IO.eprintln s!"NOAUX {c}"
      return false
    unless ← ensureAux mod ci.type do return false
    if let some v := ci.value? (allowOpaque := true) then
      unless ← ensureAux mod v do return false
    let some decl := declOf ci | return false
    try
      withOptions (fun o => (o.setBool `Elab.async false).insert `maxHeartbeats (.ofNat 0)) <| addDecl decl
    catch _ => return false
    touch mod c
  return true

/-- The axioms `e` depends on (its constants must be in the environment). -/
def axiomsOf (e : Expr) : CoreM (Array Name) := do
  let env ← getEnv
  let mut seen : NameSet := {}
  let mut axs : NameSet := {}
  let mut todo := e.getUsedConstants.toList
  while !todo.isEmpty do
    let c := todo.head!
    todo := todo.tail!
    if seen.contains c then continue
    seen := seen.insert c
    -- (an imported constant's axioms are recorded in its module)
    if (env.getModuleIdxFor? c).isSome then
      for a in ← collectAxioms c do axs := axs.insert a
      continue
    let some ci := env.find? c | continue
    if ci matches .axiomInfo _ then axs := axs.insert c
    todo := ci.type.getUsedConstants.toList ++
      (((ci.value? (allowOpaque := true)).map (·.getUsedConstants.toList)).getD []) ++
      (match ci with
       | .inductInfo v => v.ctors
       | _ => []) ++ todo
  return axs.toArray

/-- The axioms among `axs` that a cached proof of `mod` may not use: any but
Lean's own, the module's `paxiom`s and `pinstance`s (`setAllowed`), the
solver's word (`Crush.crushSorry`) when `trusted`, and `sorry` when
`allowSorry`. -/
def foreign (mod : Name) (trusted allowSorry : Bool) (axs : Array Name) : BaseIO (Array Name) := do
  let allowed := (((← stores.get)[mod]?).map (·.allowed)).getD {}
  return axs.filter fun a =>
    !(a == ``propext || a == ``Classical.choice || a == ``Quot.sound || allowed.contains a ||
      (trusted && a == `Crush.crushSorry) || (allowSorry && a == ``sorryAx))

/-- Instantiate the hypotheses `xs` (metavariables) with local hypotheses
`hyps` whose types unify with theirs, backtracking; at most `budget`
unification attempts. -/
partial def matchHyps (xs : List Expr) (hyps : Array LocalDecl) (budget : IO.Ref Nat) :
    MetaM Bool := do
  match xs with
  | [] => return true
  | x :: rest =>
    if ← x.mvarId!.isAssigned then return ← matchHyps rest hyps budget
    let xt ← instantiateMVars (← inferType x)
    unless ← isProp xt do return ← matchHyps rest hyps budget
    for d in hyps do
      if (← budget.get) == 0 then return false
      budget.modify (· - 1)
      let s ← saveState
      if ← isDefEq xt d.type then
        x.mvarId!.assign d.toExpr
        if ← matchHyps rest hyps budget then return true
      s.restore
    return false

/-- Close goal `g` of obligation `owner` with a cached lemma, if one applies:
its conclusion unifies with the goal's, each of its hypotheses with one of the
goal's, and its proof uses no axiom it may not (`foreign`; the solver's word
only when `trusted`). The obligation's own lemmas are tried first. -/
def tryGoal (mod : Name) (g : MVarId) (owner : Name) (trusted : Bool) : MetaM Bool := g.withContext do
  let target ← instantiateMVars (← g.getType)
  let k ← goalKey (invOf (← g.getTag).eraseMacroScopes) target
  let hyps := (← getLCtx).foldl (init := #[]) fun acc d =>
    if d.isImplementationDetail then acc else acc.push d
  let mut mine : Array (Name × ConstantInfo) := #[]
  let mut others : Array (Name × ConstantInfo) := #[]
  for i in [0:slots] do
    let n := goalName k i
    let some ci ← find? mod n | continue
    if ownerOf ci == some owner then mine := mine.push (n, ci) else others := others.push (n, ci)
  let budget ← IO.mkRef matchBudget
  let dbg := (← IO.getEnv "PVERIFY_DEBUG_CACHE").isSome
  for (n, ci) in mine ++ others do
    if (← budget.get) == 0 then return false
    let some v := ci.value? (allowOpaque := true) | continue
    unless ← ensureAux mod ci.type do continue
    let s ← saveState
    let pf? ← withReducible do
      let (xs, _, body) ← forallMetaTelescopeReducing ci.type
      unless body.isAppOfArity ``Concl 1 do return none
      let body := body.appArg!
      unless ← isDefEq body target do
        if dbg then IO.eprintln ((s!"MISS concl {n}: {← ppExpr body} VS {← ppExpr target}").take 400).toString
        return none
      unless ← matchHyps xs.toList hyps budget do
        if dbg then
          for x in xs do
            unless ← x.mvarId!.isAssigned do
              let xt ← instantiateMVars (← inferType x)
              if ← isProp xt then
                let mut any := false
                for d in hyps do
                  let s' ← saveState
                  if ← isDefEq xt d.type then any := true
                  s'.restore
                  if any then break
                unless any do
                  IO.eprintln ((s!"MISS hyp {n}: {← ppExpr xt}").take 600).toString
                  break
        return none
      let pf ← instantiateMVars (mkAppN v xs)
      if pf.hasExprMVar then return none
      return some pf
    if let some pf := pf? then
      -- the auxiliary declarations its proof uses, and the axioms
      if ← ensureAux mod v then
        let bad ← foreign mod trusted false (← axiomsOf v)
        if bad.isEmpty then
          g.assign pf
          touch mod n
          return true
        if dbg then IO.eprintln s!"AXIOMS {n}: {bad}"
    s.restore
  return false

/-- Whether `c` is an internal auxiliary theorem of the current module
(`…._proof_3`, `_auxLemma…`), whose name is specific to one run. -/
def isRunAux (env : Environment) (c : Name) : Bool :=
  (env.getModuleIdxFor? c).isNone &&
    c.components.any (fun x => match x with | .str _ s => s.startsWith "_" | _ => false) &&
    ((env.find? c).any (· matches .thmInfo _))

/-- `e` with the run-specific auxiliary theorems it uses replaced by their
proofs, so that a cached lemma does not depend on their names. -/
partial def inlineRunAux (e : Expr) : MetaM Expr := do
  let env ← getEnv
  unless e.getUsedConstants.any (isRunAux env) do return e
  Meta.transform e (post := fun x => do
    if let .const c us := x then
      if isRunAux env c then
        if let some ci := env.find? c then
          if let some v := ci.value? (allowOpaque := true) then
            return .done (← inlineRunAux (v.instantiateLevelParams ci.levelParams us))
    return .continue)

/-- Store lemma `type := value`, of obligation `owner`, among the lemmas of
goal key `k`: in place of a lemma with the same statement (which was not
reused, so this proof is the one to keep), in a free slot, or else in place of
a lemma this run did not use (one of `owner`'s own first, then one of an
obligation not replayed). In one step, as the obligations' proofs run in
parallel. -/
def insertGoal (mod : Name) (k : UInt64) (owner : Name) (type value : Expr) : BaseIO Unit :=
  stores.modify fun m => m.modify mod fun st => Id.run do
    let mut same? : Option Nat := none
    let mut free? : Option Nat := none
    let mut victim? : Option (Nat × Nat) := none
    for i in [0:slots] do
      let n := goalName k i
      match st.consts.find? n with
      | some ci =>
        if same?.isNone && ci.type == type then same? := some i
        else if !st.used.contains n then
          let rank := if ownerOf ci == some owner then 0
            else if (ownerOf ci).any st.replayed.contains then 2 else 1
          match victim? with
          | some (_, r) => if rank < r then victim? := some (i, rank)
          | none => victim? := some (i, rank)
      | none => if free?.isNone then free? := some i
    let some i := same? <|> free? <|> victim?.map (·.1) | return st
    let n := goalName k i
    return { st with
      consts := st.consts.insert n (.thmInfo { name := n, levelParams := [], type, value, all := [owner] })
      used := st.used.insert n }

/-- Store solved goal `g` (with conclusion `target`) of obligation `owner` as
a lemma over the hypotheses its proof uses. -/
def recordGoal (mod : Name) (g : MVarId) (target : Expr) (owner : Name) : MetaM Unit := g.withContext do
  let pf ← inlineRunAux (← zetaReduce (← instantiateMVars (.mvar g)))
  let target ← zetaReduce (← instantiateMVars target)
  if pf.hasMVar || target.hasMVar then return
  let lctx ← getLCtx
  -- the local variables the proof and the conclusion use, closed under types
  let mut set : FVarIdSet := ((collectFVars {} pf).fvarSet).union (collectFVars {} target).fvarSet
  let mut changed := true
  while changed do
    changed := false
    for fv in set.toList do
      let some d := lctx.find? fv | continue
      let inDecl := (collectFVars {} (← instantiateMVars d.type)).fvarSet.union
        ((d.value?.map fun v => (collectFVars {} v).fvarSet).getD {})
      for fv' in inDecl.toList do
        unless set.contains fv' do
          set := set.insert fv'
          changed := true
  let mut deps : Array Expr := #[]
  for d in lctx do
    if set.contains d.fvarId then
      -- (a `let` becomes a `let` of the lemma, unfolded when it is opened)
      deps := deps.push d.toExpr
  let type ← instantiateMVars (← mkForallFVars deps (mkApp (mkConst ``Concl) target))
  let value ← instantiateMVars (← mkLambdaFVars deps pf)
  if type.hasFVar || value.hasFVar || type.hasMVar || value.hasMVar then return
  let k ← goalKey (invOf (← g.getTag).eraseMacroScopes) target
  insertGoal mod k owner type value

end PLean.Cache
