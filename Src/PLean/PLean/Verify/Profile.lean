/-
PLean.Verify.Profile — opt-in instrumentation for `#pverify`.

When `pverify.profile` is true, `pverify_smt` records per-stage
wall-clock times into the IORefs declared here. `#pverify` consumes the
records at end-of-command and emits a summary table via `logInfo`.

Stages recorded per obligation:
- `cache.pp`    — `Lean.Meta.ppExpr` on the goal's local context + target;
- `cache.hash`  — `String.hash` of the canonicalised text;
- `cache.fs`    — `IO.FS.pathExists` of the cache file;
- `smt.prep`    — `pverify_smt_prep` (defunctionalisation simp chain);
- `smt.auto`    — `prepareLeanAutoQuery` (lean-auto translation);
- `smt.solver`  — `querySolver` (cvc5/z3 child process);
- `smt.assign`  — `mv.assign (trust_smt _)` (proof term construction).

The profile path is exact-equivalent in *what* it does to the
unprofiled path (same trust_smt axiom, same lean-auto translation,
same solver invocation). It differs only in being inlined into PLean
rather than going through `loom_smt`, so the segment timings sum to
approximately the same wall-clock the unprofiled path would have.

To avoid divergence from the upstream `loom_smt`, the profile path is
ONLY taken when the option is explicitly enabled. Default `false`.

Output: a per-obligation row plus a stage-aggregate table emitted on
`#pverify` completion. We don't write CSV — the inline `logInfo`
output is enough for the in-tree profiling we need; CSV can be added
later if cross-run diffing matters.
-/
import Lean

namespace PLean.Verify.Profile

/-- A single per-obligation profile row. Stage timings are in
nanoseconds; `0` means the stage was skipped (e.g. cache lookup on a
profile-off path, solver call on a cache hit). -/
structure Row where
  obligation : String
  cached     : Bool := false
  elabCmd    : Nat := 0   -- elabCommand: theorem elaboration + tactic eval + kernel typecheck
  cachePp    : Nat := 0
  cacheHash  : Nat := 0
  cacheFs    : Nat := 0
  smtPrep    : Nat := 0
  smtAuto    : Nat := 0
  smtSolver  : Nat := 0
  smtAssign  : Nat := 0
  deriving Inhabited

/-- Aggregate state across all obligations of a single `#pverify`
command. `rows` accumulates per-obligation rows in emission order. -/
structure State where
  rows : Array Row := #[]
  deriving Inhabited

/-- Accumulator across all obligations of one `#pverify` run. -/
initialize stateRef : IO.Ref State ← IO.mkRef {}

/-- Per-obligation rows, keyed by obligation full name. Two
obligations elaborating concurrently land in distinct slots and can't
race. Each row is flushed into `stateRef.rows` at obligation end. -/
initialize inFlightRowsRef : IO.Ref (Std.HashMap String Row) ← IO.mkRef ∅

def beginObligation (name : String) : IO Unit :=
  inFlightRowsRef.modify (·.insert name { obligation := name })

def endObligation (key : String) : IO Unit := do
  let m ← inFlightRowsRef.get
  match m.get? key with
  | some row =>
    stateRef.modify fun s => { s with rows := s.rows.push row }
    inFlightRowsRef.modify (·.erase key)
  | none => pure ()

def reset : IO Unit := do
  stateRef.set {}
  inFlightRowsRef.set ∅

def modifyRow (key : String) (f : Row → Row) : IO Unit :=
  inFlightRowsRef.modify fun m =>
    let cur := (m.get? key).getD { obligation := key }
    m.insert key (f cur)

def recordElabCmd (key : String) (nanos : Nat) : IO Unit :=
  modifyRow key fun r => { r with elabCmd := r.elabCmd + nanos }

/-- Run a `BaseIO` action, return its result and the elapsed nanos. -/
def timeNanos (act : IO α) : IO (α × Nat) := do
  let t0 ← IO.monoNanosNow
  let r ← act
  let t1 ← IO.monoNanosNow
  return (r, t1 - t0)

/-- Run a `MetaM` action and return elapsed nanos in `BaseIO`. -/
def timeMetaNanos (act : Lean.MetaM α) : Lean.MetaM (α × Nat) := do
  let t0 ← IO.monoNanosNow
  let r ← act
  let t1 ← IO.monoNanosNow
  return (r, t1 - t0)

/-- Run a `TacticM` action and return elapsed nanos. -/
def timeTacticNanos (act : Lean.Elab.Tactic.TacticM α) :
    Lean.Elab.Tactic.TacticM (α × Nat) := do
  let t0 ← IO.monoNanosNow
  let r ← act
  let t1 ← IO.monoNanosNow
  return (r, t1 - t0)

/-! ## Reporting -/

/-- Format `nanos` as milliseconds (1 decimal). -/
def fmtMs (n : Nat) : String :=
  let ms := n.toFloat / 1_000_000.0
  s!"{ms.toString.take 7}"

/-- Pad a string to width `w` on the right. -/
def padRight (s : String) (w : Nat) : String :=
  let pad := w - s.length
  if pad ≤ 0 then s else s ++ String.mk (List.replicate pad ' ')

/-- Render the per-obligation rows table. -/
def renderRows (rows : Array Row) : String :=
  let header := s!"  {padRight "obligation" 60}  {padRight "elabCmd" 8}  {padRight "wall" 8}  {padRight "cache.pp" 8}  {padRight "cache.h" 8}  {padRight "cache.fs" 8}  {padRight "prep" 8}  {padRight "auto" 8}  {padRight "solver" 8}  {padRight "assign" 8}  cached"
  let lines := rows.toList.map fun r =>
    let wall := r.cachePp + r.cacheHash + r.cacheFs + r.smtPrep + r.smtAuto + r.smtSolver + r.smtAssign
    s!"  {padRight r.obligation 60}  {padRight (fmtMs r.elabCmd) 8}  {padRight (fmtMs wall) 8}  {padRight (fmtMs r.cachePp) 8}  {padRight (fmtMs r.cacheHash) 8}  {padRight (fmtMs r.cacheFs) 8}  {padRight (fmtMs r.smtPrep) 8}  {padRight (fmtMs r.smtAuto) 8}  {padRight (fmtMs r.smtSolver) 8}  {padRight (fmtMs r.smtAssign) 8}  {r.cached}"
  String.intercalate "\n" (header :: lines)

/-- Render the stage-aggregate table (totals across all rows). -/
def renderAggregate (rows : Array Row) : String :=
  let total (proj : Row → Nat) : Nat := rows.foldl (init := 0) (fun acc r => acc + proj r)
  let calls := rows.size
  let cachedCount := (rows.filter (·.cached)).size
  let elabCmd   := total Row.elabCmd
  let cachePp   := total Row.cachePp
  let cacheHash := total Row.cacheHash
  let cacheFs   := total Row.cacheFs
  let smtPrep   := total Row.smtPrep
  let smtAuto   := total Row.smtAuto
  let smtSolver := total Row.smtSolver
  let smtAssign := total Row.smtAssign
  -- We report `elabCmd` separately (outer-wall, includes the others) and
  -- the "tactic-time" sum which is elabCmd minus what the tactic stages
  -- captured. That difference reveals where elaboration spends time
  -- outside the SMT path (e.g. theorem-type elaboration, kernel typecheck).
  let smtSum := cachePp + cacheHash + cacheFs + smtPrep + smtAuto + smtSolver + smtAssign
  let elabOuter := if elabCmd ≥ smtSum then elabCmd - smtSum else 0
  let grandTotal := if elabCmd > 0 then elabCmd else smtSum
  let pct (n : Nat) : String :=
    if grandTotal == 0 then "0.0%"
    else s!"{((Float.ofNat n / Float.ofNat grandTotal) * 100.0).toString.take 5}%"
  s!"  obligations:        {calls} ({cachedCount} cache hits)\n"
    ++ s!"  elabCmd (outer):    {padRight (fmtMs elabCmd) 8} ms  ({pct elabCmd})\n"
    ++ s!"  └─ tactic stages:   {padRight (fmtMs smtSum) 8} ms  ({pct smtSum})\n"
    ++ s!"  └─ rest (elab/kc):  {padRight (fmtMs elabOuter) 8} ms  ({pct elabOuter})\n"
    ++ s!"  cache.pp:           {padRight (fmtMs cachePp) 8} ms  ({pct cachePp})\n"
    ++ s!"  cache.hash:         {padRight (fmtMs cacheHash) 8} ms  ({pct cacheHash})\n"
    ++ s!"  cache.fs:           {padRight (fmtMs cacheFs) 8} ms  ({pct cacheFs})\n"
    ++ s!"  smt.prep:           {padRight (fmtMs smtPrep) 8} ms  ({pct smtPrep})\n"
    ++ s!"  smt.auto:           {padRight (fmtMs smtAuto) 8} ms  ({pct smtAuto})\n"
    ++ s!"  smt.solver:         {padRight (fmtMs smtSolver) 8} ms  ({pct smtSolver})\n"
    ++ s!"  smt.assign:         {padRight (fmtMs smtAssign) 8} ms  ({pct smtAssign})\n"
    ++ s!"  total:              {padRight (fmtMs grandTotal) 8} ms"

/-- Render a sorted top-N table (by wall time descending). -/
def renderTopN (rows : Array Row) (n : Nat) : String :=
  let wallOf (r : Row) : Nat :=
    r.cachePp + r.cacheHash + r.cacheFs + r.smtPrep + r.smtAuto + r.smtSolver + r.smtAssign
  let sorted := rows.toList.toArray.qsort (fun a b => wallOf a > wallOf b)
  let top := sorted.extract 0 (min n sorted.size)
  renderRows top

end PLean.Verify.Profile
