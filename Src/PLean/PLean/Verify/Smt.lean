/-
PLean.Verify.Smt — the SMT bridge: lean-auto translation + z3/cvc5
process driver + the `plean_smt` tactic.

Ported from Loom's `Loom/SMT.lean` (verse-lab/loom @ d103408) when
PLean dropped its Loom dependency. Behaviour is unchanged:

- the goal is negated (`Classical.byContradiction`) and handed, with
  the hint set, to lean-auto's monomorphisation + reification +
  `lamFOL2SMT` pipeline, which yields an SMT-LIB query string;
- the query is piped to the solver binary that `lakefile.lean`'s
  `downloadDependencies` target places in `<PLean>/.lake/build/`;
- `unsat` closes the goal via the `trust_smt` axiom; `sat` throws
  `"<solver>: the goal is false:<MODEL>"` (parsed by `CexParse`);
  `unknown` optionally retries with the other solver.

Options (formerly `loom.solver*`):
- `pverify.smt.solver`         — `"cvc5"` (default) or `"z3"`;
- `pverify.smt.timeout`        — per-query timeout in seconds;
- `pverify.smt.seed`           — solver random seed;
- `pverify.smt.retryOnUnknown` — retry with the other solver on
  `unknown`.
-/
import Lean
import Auto

namespace PLean.Smt

open Lean

/-! ## Solver selection and options -/

/-- The two SMT backends PLean drives. -/
inductive SmtSolver where
  | z3
  | cvc5
  deriving BEq, Hashable, Inhabited

instance : ToString SmtSolver where
  toString
    | .z3   => "z3"
    | .cvc5 => "cvc5"

/-- Parse a solver name as it appears in `set_option pverify.smt.solver`. -/
def SmtSolver.ofString? : String → Option SmtSolver
  | "z3"   => some .z3
  | "cvc5" => some .cvc5
  | _      => none

register_option pverify.smt.solver : String := {
  defValue := "cvc5"
  descr := "SMT solver used by `plean_smt` / `pverify_smt`: `\"cvc5\"` (default) or `\"z3\"`."
}

register_option pverify.smt.timeout : Nat := {
  defValue := 1
  descr := "Per-query timeout for cvc5 / z3, in seconds (default 1)."
}

register_option pverify.smt.seed : Nat := {
  defValue := 0
  descr := "Random seed passed to cvc5 / z3 (default 0)."
}

register_option pverify.smt.retryOnUnknown : Bool := {
  defValue := true
  descr := "Retry a query with the other solver when the first answers `unknown` (default true)."
}

initialize
  registerTraceClass `pverify.smt.debug
  registerTraceClass `pverify.smt.result
  registerTraceClass `pverify.smt.query

/-- The solver selected by `pverify.smt.solver`; falls back to cvc5 on
an unrecognised value. -/
def selectedSolver (opts : Options) : SmtSolver :=
  (SmtSolver.ofString? (pverify.smt.solver.get opts)).getD .cvc5

/-- If the solver returns `unknown`, the one to retry with. -/
def smtSolverToTryOnUnknown : SmtSolver → SmtSolver
  | .z3   => .cvc5
  | .cvc5 => .z3

/-! ## Results -/

inductive SmtResult
  /-- `modelString` contains the raw model returned by the solver. -/
  | Sat (modelString : Option String)
  | Unsat
  | Unknown (reason : Option String)
  | Failure (reason : Option String)
  deriving Inhabited

instance : ToString SmtResult where
  toString
    | .Sat none        => "sat"
    | .Sat (some m)    => s!"sat\n{m}"
    | .Unsat           => "unsat"
    | .Unknown none    => "unknown"
    | .Unknown (some r) => s!"unknown ({r})"
    | .Failure none    => "failure"
    | .Failure (some r) => s!"failure ({r})"

/-! ## Locating the solver binaries -/

/-- The directory of the file being compiled. Used to locate the
solver binaries relative to this source file. -/
scoped syntax (name := currentDirectory) "currentDirectory!" : term

open Elab Term in
@[term_elab currentDirectory] def elabCurrentFilePath : TermElab
  | `(currentDirectory!), _ => do
    let ctx ← readThe Core.Context
    let srcPath := System.FilePath.mk ctx.fileName
    let some srcDir := srcPath.parent
      | throwError "cannot compute parent directory of '{srcPath}'"
    return mkStrLit s!"{srcDir}"
  | _, _ => throwUnsupportedSyntax

/-- `<PLean>/.lake/build`: this file lives at `<PLean>/PLean/Verify/`. -/
private def buildDir : System.FilePath :=
  System.mkFilePath [currentDirectory!] / ".." / ".." / ".lake" / "build"

def z3Path   : System.FilePath := buildDir / "z3"
def cvc5Path : System.FilePath := buildDir / "cvc5"

/-! ## lean-auto query construction -/

open Elab Tactic Meta Auto in
/-- Build the SMT-LIB query for `mv` through lean-auto (faster than
lean-smt; queries are not human-readable). Operates on a clone of the
goal and restores the original main goal before returning. -/
def prepareLeanAutoQuery (mv : MVarId) (hints : TSyntax `Auto.hints) : TacticM String := do
  let goal := (← mkFreshExprMVar (← mv.getType)).mvarId!
  let (goalBinders, newGoal) ← goal.intros
  let [nngoal] ← newGoal.apply (.const ``Classical.byContradiction [])
    | throwError "prepareLeanAutoQuery: unexpected result after applying Classical.byContradiction"
  let (ngoal, absurd) ← MVarId.intro1 nngoal
  replaceMainGoal [absurd]
  withMainContext do
    let (lemmas, inhFacts) ← collectAllLemmas hints #[] (goalBinders.push ngoal)
    let query ← getQueryFromAuto lemmas inhFacts
    replaceMainGoal [mv]
    return query
where
  -- Mirrors lean-auto's `runAuto` / `querySMT`, stopping at the query.
  getQueryFromAuto (lemmas : Array Lemma) (inhFacts : Array Lemma) : MetaM String := do
    let lemmas ← rewriteIteCondDecide lemmas
    let (cmdString, _) ← Monomorphization.monomorphize lemmas inhFacts
      (@id (Reif.ReifM String) do
        let s ← get
        let u ← computeMaxLevel s.facts
        (afterReify s.facts s.inhTys s.inds).run' (← LamReif.State.init u))
    return cmdString
  afterReify (uvalids : Array UMonoFact) (uinhs : Array UMonoFact)
      (minds : Array SimpleMutIndVal) : LamReif.ReifM String := do
    let exportFacts ← LamReif.reifFacts uvalids
    let exportFacts := exportFacts.map (Embedding.Lam.REntry.valid [])
    let _ ← LamReif.reifInhabitations uinhs
    let indBlocks ← LamReif.reifMutInds minds
    LamReif.printValuation
    let (exportFacts, exportInds) ← LamReif.preprocess exportFacts (indBlocks.map (·.info))
    getAutoQueryString exportFacts exportInds
  getAutoQueryString (exportFacts : Array Embedding.Lam.REntry)
      (exportInds : Array Embedding.Lam.MutualIndInfo) : LamReif.ReifM String := do
    let lamVarTy := (← LamReif.getVarVal).map Prod.snd
    let lamEVarTy ← LamReif.getLamEVarTy
    let exportLamTerms ← exportFacts.mapM fun re => do
      match re with
      | .valid [] t => return t
      | _ => throwError "prepareLeanAutoQuery: unexpected non-valid entry"
    let laterInterp := SMT.LaterSMTInterp.ofSpecAvail (← LamReif.getLaterInfo).specAvail
    let sni : SMT.SMTNamingInfo :=
      { tyVal := (← LamReif.getTyVal), varVal := (← LamReif.getVarVal)
        lamEVarTy := (← LamReif.getLamEVarTy), laterInterp }
    let ((commands, _validFacts, _), _state) ←
      (lamFOL2SMT sni lamVarTy lamEVarTy exportLamTerms exportInds).run
    return String.intercalate "\n" (commands.toList.map toString)

/-! ## Solver process driver -/

abbrev SolverProc := IO.Process.Child ⟨.piped, .piped, .piped⟩

def emitCommandStr (p : SolverProc) (c : String) : MetaM Unit := do
  trace[pverify.smt.query] "{c}"
  p.stdin.putStr c
  p.stdin.flush

def emitCommand (p : SolverProc) (c : Auto.IR.SMT.Command) : MetaM Unit :=
  emitCommandStr p s!"{c}\n"

private def spawnSolver (path : String) (args : Array String) : MetaM SolverProc := do
  trace[pverify.smt.debug] "invoking {path} {args}"
  IO.Process.spawn { stdin := .piped, stdout := .piped, stderr := .piped,
                     cmd := path, args := args }

def createSolver (name : SmtSolver) (timeoutSec : Nat) : MetaM SolverProc := do
  let seed := pverify.smt.seed.get (← getOptions)
  match name with
  | .z3 =>
    spawnSolver s!"{z3Path}"
      #["-in", "-smt2", s!"-t:{timeoutSec * 1000}",
        s!"sat.random_seed={seed}", s!"smt.random_seed={seed}"]
  | .cvc5 =>
    let proc ← spawnSolver s!"{cvc5Path}"
      #["--lang", "smt", s!"--tlimit-per={timeoutSec * 1000}", "--seed", s!"{seed}"]
    emitCommand proc (.setLogic "ALL")
    pure proc

/-- Drain and log the solver's remaining output, then kill it. -/
private def finishSolver (solver : SolverProc) : MetaM Unit := do
  let (_, solver) ← solver.takeStdin
  let stdout ← solver.stdout.readToEnd
  let stderr ← solver.stderr.readToEnd
  trace[pverify.smt.debug] "stdout: {stdout}"
  trace[pverify.smt.debug] "stderr: {stderr}"
  solver.kill

def querySolver (goalQuery : String) (timeoutSec : Nat)
    (solverName : SmtSolver) (retryOnUnknown : Bool := false) :
    MetaM (SmtResult × SmtSolver) := do
  try
    trace[pverify.smt.debug] "solver: {solverName}"
    let solver ← createSolver solverName timeoutSec
    emitCommandStr solver goalQuery
    emitCommand solver .checkSat
    let stdout ← solver.stdout.getLine
    trace[pverify.smt.debug] "[checkSat] stdout: {stdout}"
    let (checkSatResponse, _) ← Auto.Solver.SMT.getSexp stdout
    match checkSatResponse with
    | .atom (.symb "sat") =>
      trace[pverify.smt.result] "{solverName} says sat"
      emitCommand solver .getModel
      let (_, solver) ← solver.takeStdin
      let stdout ← solver.stdout.readToEnd
      let stderr ← solver.stderr.readToEnd
      trace[pverify.smt.debug] "stdout: {stdout}"
      trace[pverify.smt.debug] "stderr: {stderr}"
      let (model, _) ← Auto.Solver.SMT.getSexp stdout
      solver.kill
      return (.Sat s!"{model}", solverName)
    | .atom (.symb "unsat") =>
      trace[pverify.smt.result] "{solverName} says unsat"
      finishSolver solver
      return (.Unsat, solverName)
    | .atom (.symb "unknown") =>
      trace[pverify.smt.result] "{solverName} says unknown"
      if retryOnUnknown then
        finishSolver solver
        let other := smtSolverToTryOnUnknown solverName
        trace[pverify.smt.debug] "retrying with {other}"
        querySolver goalQuery timeoutSec other (retryOnUnknown := false)
      else
        finishSolver solver
        return (.Unknown none, solverName)
    | _ => throwError s!"unexpected response from solver: {checkSatResponse}"
  catch e =>
    return (.Failure s!"{← e.toMessageData.toString}", solverName)
termination_by if retryOnUnknown then 1 else 0

/-! ## Trust axiom and diagnostic strings -/

/-- We trust the SMT solver's judgement: when z3 / cvc5 reports the
negated goal `unsat`, the goal is closed with this axiom. Every
SMT-discharged obligation therefore depends on `trust_smt`. -/
axiom trust_smt : ∀ (p : Prop), p

private def solverPrefix (solver : Option SmtSolver) : String :=
  match solver with
  | some s => s!"{s}: "
  | none   => ""

/-- Diagnostic for `sat`. `CexParse` keys on the `"the goal is false"`
substring, so keep it stable. -/
def satGoalStr (solver : Option SmtSolver := none) : String :=
  s!"{solverPrefix solver}the goal is false"
def unknownGoalStr (solver : Option SmtSolver := none) : String :=
  s!"{solverPrefix solver}the goal is unknown"
def failureGoalStr (solver : Option SmtSolver := none) : String :=
  s!"{solverPrefix solver}solver invocation failed"

/-! ## The `plean_smt` tactic -/

/-- Close the goal by SMT: translate it through lean-auto, ask the
solver selected by `pverify.smt.solver`, and on `unsat` assign
`trust_smt`. Throws on `sat` (with the model), `unknown`, or solver
failure. -/
syntax (name := pleanSmt) "plean_smt" Auto.hints : tactic

open Elab Tactic in
@[tactic pleanSmt] def elabPleanSmt : Tactic := fun stx => withMainContext do
  let hints : TSyntax `Auto.hints := ⟨stx[1]⟩
  let mv ← getMainGoal
  let opts ← getOptions
  let cmdString ← prepareLeanAutoQuery mv hints
  let (res, solverUsed) ← querySolver cmdString (pverify.smt.timeout.get opts)
    (selectedSolver opts) (retryOnUnknown := pverify.smt.retryOnUnknown.get opts)
  match res with
  | .Sat none       => throwError s!"{satGoalStr solverUsed}"
  | .Sat (some m)   => throwError s!"{satGoalStr solverUsed}:{m}"
  | .Unknown reason =>
    throwError "{unknownGoalStr solverUsed}{(reason.map (s!": {·}")).getD ""}"
  | .Failure reason =>
    throwError "{failureGoalStr solverUsed}{(reason.map (s!": {·}")).getD ""}"
  | .Unsat =>
    logInfo m!"Goal proven by {solverUsed}. Trusting SMT solver result."
    mv.assign (mkApp (mkConst ``trust_smt) (← mv.getType))

end PLean.Smt
