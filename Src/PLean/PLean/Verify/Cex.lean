/-
PLean.Verify.Cex — counterexamples for obligations that are not proved.

When no solver proves a goal, `pverify_close` has `crush` write the goal's
SMT encoding and asks z3 for a model of it (`findModel`). Two changes make
z3 find models where `crush`'s own attempts time out:

* the recursive well-formedness predicates `crush` emits for recursive
  datatypes (`Nat` elements of a `PSet`, …) become `true`, since they keep
  z3's model-based quantifier instantiation from terminating; a model may
  then hold a negative number where the program has a `Nat`;
* the integers are bounded (tightly first, then loosely, then not at all).

The model is then minimised, keeping each added constraint that stays
satisfiable: the fewest machines, the fewest packet ids, as few packets in
flight and delivered as possible, and the smallest integers.

`#pverify` decodes the model into the obligation's pre-state (`render`):
the machines grouped by kind with their state and fields, the packets in
flight, the packets delivered earlier, and the module's `function`s. Sets
and maps are read through the model's `mem` / `get` when it interprets
them. The model satisfies the obligation's premises; it need not be
reachable.
-/
import Crush

namespace PLean.Cex

open Crush.SMT (Sexp parseSexps)

/-- `xs[i]?`, typed. -/
private abbrev nth (xs : Array Sexp) (i : Nat) : Option Sexp := xs[i]?

/-! ## The SMT script -/

/-- `s` without a trailing `_<digits>` (`crush` suffixes every symbol with a
counter). -/
def baseName (s : String) : String :=
  match (s.splitOn "_").reverse with
  | last :: rest@(_ :: _) =>
    if !last.isEmpty && last.all Char.isDigit then "_".intercalate rest.reverse else s
  | _ => s

/-- The declarations of an SMT script. -/
structure Script where
  /-- Constructor → its datatype and its selectors with their sorts. -/
  ctorOf : Std.HashMap String (String × Array (String × String)) := {}
  /-- Datatype → its constructors. -/
  ctors : Std.HashMap String (Array String) := {}
  /-- Selector → its constructor and field index. -/
  selOf : Std.HashMap String (String × Nat) := {}
  /-- Function → its parameter sorts and result sort. -/
  funs : Std.HashMap String (Array String × String) := {}
  /-- Functions in declaration order. -/
  funOrder : Array String := #[]
  /-- Selectors of `Nat` fields (recorded by `relax`). -/
  natSels : Std.HashSet String := {}

private def sortStr : Sexp → String
  | .atom s => s
  | e => toString e

/-- Read the declarations of `sexps`. -/
def Script.ofSexps (sexps : Array Sexp) : Script := Id.run do
  let mut sc : Script := {}
  for e in sexps do
    let .list xs := e | continue
    match (nth xs 0), (nth xs 1), (nth xs 2), (nth xs 3) with
    | some (.atom "declare-datatypes"), some (.list heads), some (.list bodies), _ =>
      for (h, b) in heads.zip bodies do
        let .list hd := h | continue
        let some (.atom dt) := (nth hd 0) | continue
        let .list cs := b | continue
        let mut names := #[]
        for c in cs do
          let (cname, fields) : String × Array (String × String) := match c with
            | .atom n => (n, #[])
            | .list ys => match (nth ys 0) with
              | some (.atom n) => (n, (ys.extract 1 ys.size).filterMap fun (f : Sexp) => match f with
                  | Sexp.list #[Sexp.atom sel, srt] => some (sel, sortStr srt)
                  | _ => none)
              | _ => ("", #[])
            | _ => ("", #[])
          if cname.isEmpty then continue
          names := names.push cname
          sc := { sc with ctorOf := sc.ctorOf.insert cname (dt, fields) }
          for ((sel, _), k) in fields.zipIdx do
            sc := { sc with selOf := sc.selOf.insert sel (cname, k) }
        sc := { sc with ctors := sc.ctors.insert dt names }
    | some (.atom "declare-fun"), some (.atom f), some (.list ps), some r =>
      sc := { sc with funs := sc.funs.insert f (ps.map sortStr, sortStr r), funOrder := sc.funOrder.push f }
    | some (.atom "declare-const"), some (.atom f), some r, _ =>
      sc := { sc with funs := sc.funs.insert f (#[], sortStr r), funOrder := sc.funOrder.push f }
    | some (.atom "set-info"), some (.atom ":pverify-nat-selectors"), some (.str ss), _ =>
      sc := { sc with natSels := (ss.splitOn " ").foldl (init := sc.natSels) fun acc x => acc.insert x }
    | _, _, _, _ => pure ()
  return sc

/-- The function whose base name is `base` and whose parameter sorts satisfy `ok`. -/
def Script.findFun (sc : Script) (base : String) (ok : Array String → Bool := fun _ => true) :
    Option String :=
  sc.funOrder.find? fun f => baseName f == base && (sc.funs[f]?.map (ok ·.1)).getD false

private partial def mentions (name : String) : Sexp → Bool
  | .atom a => a == name
  | .str _ => false
  | .list xs => xs.any (mentions name)

/-- The selectors a well-formedness predicate requires to be `≥ 0`. -/
private partial def natSelsOf : Sexp → Array String
  | .list #[.atom ">=", .list #[.atom sel, _], .atom "0"] => #[sel]
  | .list xs => xs.foldl (init := #[]) fun acc x => acc ++ natSelsOf x
  | _ => #[]

/-- Replace `crush`'s recursive well-formedness predicates (`wf_…`) by `true`
and the non-recursive `define-funs-rec` by `define-fun`; drop the commands
that ask for a verdict or a proof. Records the selectors of `Nat` fields
(`set-info :pverify-nat-selectors`). -/
def relax (sexps : Array Sexp) : Array Sexp := Id.run do
  let mut out := #[]
  let mut nats : Array String := #[]
  for e in sexps do
    if let .list #[.atom "define-funs-rec", .list decls, .list bodies] := e then
      for (d, b) in decls.zip bodies do
        if let .list ys := d then
          if ((nth ys 0).bind (·.atom?) |>.map (·.startsWith "wf_")).getD false then
            nats := nats ++ natSelsOf b
  for e in sexps do
    match e with
    | .list #[.atom "define-funs-rec", .list decls, .list bodies] =>
      let names := decls.filterMap fun d => match d with
        | .list ys => (nth ys 0).bind (·.atom?)
        | _ => none
      let recursive := bodies.any fun b => names.any (mentions · b)
      for (d, b) in decls.zip bodies do
        let .list ys := d | out := out.push e; continue
        match (nth ys 0), (nth ys 1), (nth ys 2) with
        | some n, some ps, some r =>
          if recursive then
            if r == .atom "Bool" && (n.atom?.map (·.startsWith "wf_")).getD false then
              out := out.push (.list #[.atom "define-fun", n, ps, r, .atom "true"])
            else out := out.push (.list #[.atom "define-funs-rec", .list #[d], .list #[b]])
          else out := out.push (.list #[.atom "define-fun", n, ps, r, b])
        | _, _, _ => out := out.push e
    | .list xs =>
      match (nth xs 0) with
      | some (.atom c) =>
        unless ["check-sat", "get-model", "get-proof", "get-unsat-core", "exit"].contains c do
          out := out.push e
      | _ => out := out.push e
    | _ => out := out.push e
  return out.push (.list #[.atom "set-info", .atom ":pverify-nat-selectors", .str (" ".intercalate nats.toList)])

/-- The pre-state world `toWorld g`, its ghost world `g`, and the world
functions, when the script has them. -/
structure WorldSyms where
  g : String
  gw : String
  w : Sexp
  wSort : String
  machines : Option String
  nextRef : Option String
  net : Option String
  clock : Option String
  log : Option String

def Script.worldSyms (sc : Script) : Option WorldSyms := do
  let toWorld ← sc.findFun "toWorld" (·.size == 1)
  let (ps, wSort) ← sc.funs[toWorld]?
  let gw ← ps[0]?
  let g ← (sc.findFun "g" (·.isEmpty)).filter (fun f => (sc.funs[f]?.map (·.2)) == some gw) <|>
    sc.funOrder.find? fun f => sc.funs[f]? == some (#[], gw)
  let onW (ps : Array String) := ps == #[wSort, "Int"]
  return { g, gw, wSort, w := .list #[.atom toWorld, .atom g]
           machines := sc.findFun "machines" onW
           nextRef := sc.findFun "nextRef" (· == #[wSort])
           net := sc.findFun "net" onW
           clock := sc.findFun "clock" (· == #[wSort])
           log := sc.findFun "log" (· == #[gw, "Int"]) }

/-! ## Evaluating a model -/

/-- A value of the model. -/
inductive Val where
  | int (n : Int)
  | bool (b : Bool)
  | ctor (c : String) (args : Array Val)
  /-- An element of an uninterpreted sort. -/
  | elem (e : String)
  | unknown
  deriving Inhabited, BEq

private partial def Val.known : Val → Bool
  | .unknown => false
  | .ctor _ args => args.all Val.known
  | _ => true

/-- z3's `define-fun`s: name → parameters and body. -/
abbrev Defs := Std.HashMap String (Array String × Sexp)

def parseModel (model : String) : Defs := Id.run do
  let mut defs : Defs := {}
  for top in parseSexps model do
    let .list items := top | continue
    for it in items do
      let .list xs := it | continue
      match (nth xs 0), (nth xs 1), (nth xs 2), xs.back? with
      | some (.atom "define-fun"), some (.atom n), some (.list ps), some body =>
        let params := ps.filterMap fun p => match p with
          | .list ys => (nth ys 0).bind (·.atom?)
          | _ => none
        defs := defs.insert n (params, body)
      | _, _, _, _ => pure ()
  return defs

private def parseInt? (s : String) : Option Int :=
  if !s.isEmpty && s.all Char.isDigit then some s.toNat! else none

private def asInt : Val → Option Int
  | .int n => some n
  | _ => none

private def asBool : Val → Option Bool
  | .bool b => some b
  | _ => none

/-- Evaluate `e` in `defs` (`fuel` bounds the nesting of calls). -/
partial def eval (sc : Script) (defs : Defs) (env : Std.HashMap String Val) (fuel : Nat) (e : Sexp) : Val :=
  let ev := eval sc defs env fuel
  match e with
  | .atom a =>
    if let some v := env[a]? then v
    else if a == "true" then .bool true
    else if a == "false" then .bool false
    else if let some n := parseInt? a then .int n
    else if sc.ctorOf.contains a then .ctor a #[]
    else if let some (ps, body) := defs[a]? then
      if ps.isEmpty && fuel > 0 then eval sc defs {} (fuel - 1) body else .unknown
    else if (a.splitOn "!val!").length > 1 then .elem a
    else .unknown
  | .str _ => .unknown
  | .list xs =>
    let args := xs.extract 1 xs.size
    let ints : Option (Array Int) := args.mapM fun x => asInt (ev x)
    let bools : Option (Array Bool) := args.mapM fun x => asBool (ev x)
    let cmp (f : Int → Int → Bool) : Val := match ints with
      | some #[a, b] => .bool (f a b)
      | _ => .unknown
    match (nth xs 0) with
    | some (.list #[.atom "_", .atom "is", .atom c]) =>
      match (nth args 0).map ev with
      | some (.ctor c' _) => .bool (c == c')
      | _ => .unknown
    | some (.atom op) =>
      match op with
      | "ite" =>
        match (nth args 0).map ev, (nth args 1), (nth args 2) with
        | some (.bool true), some a, _ => ev a
        | some (.bool false), _, some b => ev b
        | _, _, _ => .unknown
      | "let" =>
        match (nth args 0), (nth args 1) with
        | some (.list bs), some body =>
          let env' := bs.foldl (init := env) fun acc b => match b with
            | .list #[.atom v, x] => acc.insert v (ev x)
            | _ => acc
          eval sc defs env' fuel body
        | _, _ => .unknown
      | "!" => ((nth args 0).map ev).getD .unknown
      | "as" => ((nth args 0).map ev).getD .unknown
      | "and" =>
        if args.any (fun x => ev x == .bool false) then .bool false
        else (bools.map fun bs => .bool (bs.all id)).getD .unknown
      | "or" =>
        if args.any (fun x => ev x == .bool true) then .bool true
        else (bools.map fun bs => .bool (bs.any id)).getD .unknown
      | "not" => match bools with | some #[b] => .bool !b | _ => .unknown
      | "=>" => match bools with | some #[a, b] => .bool (!a || b) | _ => .unknown
      | "=" =>
        let vs := args.map ev
        if vs.all Val.known then .bool (vs.all (· == vs[0]!)) else .unknown
      | "distinct" =>
        let vs := args.map ev
        if vs.all Val.known then
          .bool ((List.range vs.size).all fun i => (List.range vs.size).all fun j => i == j || vs[i]! != vs[j]!)
        else .unknown
      | "<=" => cmp (· ≤ ·)
      | "<" => cmp (· < ·)
      | ">=" => cmp (· ≥ ·)
      | ">" => cmp (· > ·)
      | "+" => (ints.map fun ns => .int (ns.foldl (· + ·) 0)).getD .unknown
      | "*" => (ints.map fun ns => .int (ns.foldl (· * ·) 1)).getD .unknown
      | "-" => match ints with
        | some #[a] => .int (-a)
        | some ns => match ns[0]? with
          | some n0 => .int (n0 - (ns.extract 1 ns.size).foldl (· + ·) 0)
          | none => .unknown
        | none => .unknown
      | "div" => match ints with | some #[a, b] => .int (a.fdiv b) | _ => .unknown
      | "mod" => match ints with | some #[a, b] => .int (a.fmod b) | _ => .unknown
      | "abs" => match ints with | some #[a] => .int a.natAbs | _ => .unknown
      | _ =>
        if sc.ctorOf.contains op then .ctor op (args.map ev)
        else if let some (c, k) := sc.selOf[op]? then
          match (nth args 0).map ev with
          | some (.ctor c' vs) => if c == c' then vs[k]?.getD .unknown else .unknown
          | _ => .unknown
        else if let some (ps, body) := defs[op]? then
          if fuel == 0 || ps.size != args.size then .unknown
          else eval sc defs ((ps.zip (args.map ev)).foldl (init := {}) fun m (p, v) => m.insert p v)
            (fuel - 1) body
        else .unknown
    | _ => .unknown

/-- Integers `e` compares against (the breakpoints of a piecewise function). -/
partial def intLits (sc : Script) (defs : Defs) (e : Sexp) (seen : Std.HashSet String := {}) :
    Array Int :=
  match e with
  | .atom a =>
    if let some n := parseInt? a then #[n]
    else if seen.contains a then #[]
    else match defs[a]? with
      | some (#[], body) => match eval sc defs {} 8 body with
        | .int n => #[n]
        | _ => #[]
      | some (_, body) => intLits sc defs body (seen.insert a)
      | none => #[]
  | .str _ => #[]
  | .list xs => xs.foldl (init := #[]) fun acc x => acc ++ intLits sc defs x seen

/-! ## Finding a small model -/

private def leB (b : Int) (t : Sexp) : Sexp :=
  .list #[.atom "and", .list #[.atom "<=", .list #[.atom "-", .atom (toString b)], t],
    .list #[.atom "<=", t, .atom (toString b)]]

private def app (f : String) (args : Array Sexp) : Sexp := .list (#[.atom f] ++ args)

private def tester (c : String) (x : Sexp) : Sexp := .list #[.list #[.atom "_", .atom "is", .atom c], x]

/-- The Int selectors of datatype `dt`, with their constructors. -/
private def intSels (sc : Script) (dt : String) : Array (String × String) :=
  ((sc.ctors[dt]?).getD #[]).foldl (init := #[]) fun acc c =>
    acc ++ (((sc.ctorOf[c]?).map (·.2)).getD #[]).filterMap fun (sel, srt) =>
      if srt == "Int" then some (c, sel) else none

/-- Assertions bounding the model's integers, one per integer term (by the
bound given): the goal's integer variables and integer fields, the
machines' integer fields and the packets' endpoints. -/
def boundGroups (sc : Script) : Array (Int → Sexp) := Id.run do
  let mut out : Array (Int → Sexp) := #[]
  for f in sc.funOrder do
    let some (ps, r) := sc.funs[f]? | continue
    unless ps.isEmpty do continue
    if r == "Int" then out := out.push fun b => app "assert" #[leB b (.atom f)]
    else
      for (c, sel) in intSels sc r do
        out := out.push fun b => app "assert" #[app "=>" #[tester c (.atom f), leB b (app sel #[.atom f])]]
  let some ws := sc.worldSyms | return out
  let forallNat (body : Sexp → Sexp) : Sexp :=
    app "forall" #[.list #[.list #[.atom "x_cex", .atom "Int"]],
      app "=>" #[app ">=" #[.atom "x_cex", .atom "0"], body (.atom "x_cex")]]
  if let some ms := ws.machines then
    if let some (_, msSort) := sc.funs[ms]? then
      if let some msCtor := (sc.ctors[msSort]?).bind (·[0]?) then
        if let some (_, msFields) := sc.ctorOf[msCtor]? then
          if let some (fSel, fSort) := msFields[1]? then
            for (c, sel) in intSels sc fSort do
              out := out.push fun b => app "assert" #[forallNat fun x =>
                let fv := app fSel #[app ms #[ws.w, x]]
                app "=>" #[tester c fv, leB b (app sel #[fv])]]
  for (fn, world) in [(ws.net, ws.w), (ws.log, Sexp.atom ws.g)] do
    let some f := fn | continue
    let some (_, optSort) := sc.funs[f]? | continue
    for c in (sc.ctors[optSort]?).getD #[] do
      let some (_, #[(someSel, pSort)]) := sc.ctorOf[c]? | continue
      for (pc, sel) in intSels sc pSort do
        out := out.push fun b => app "assert" #[forallNat fun x =>
          let v := app f #[world, x]
          app "=>" #[tester c v, app "=>" #[tester pc (app someSel #[v]),
            leB b (app sel #[app someSel #[v]])]]]
  return out

/-- Every integer term bounded by `b`. -/
def bounds (sc : Script) (b : Int) : Array Sexp := (boundGroups sc).map (· b)

/-- The script that asks for a model of `cmds`. -/
private def scriptText (cmds : Array Sexp) : String :=
  "\n".intercalate ((#[app "set-option" #[.atom ":produce-models", .atom "true"]] ++ cmds ++
    #[app "check-sat" #[], app "get-model" #[]]).toList.map toString)

/-- z3's model of `cmds`, if it finds one within `timeout` seconds. -/
private def solve (cmds : Array Sexp) (timeout : Nat) : IO (Option String) := do
  let (h, path) ← IO.FS.createTempFile
  h.putStr (scriptText cmds)
  h.flush
  let out ← try
      some <$> IO.Process.output { cmd := "z3", args := #[s!"-T:{timeout}", path.toString] }
    catch _ => pure none
  try IO.FS.removeFile path catch _ => pure ()
  match out with
  | some o => return if o.stdout.startsWith "sat" then some o.stdout else none
  | none => return none

/-- Ask z3 for a small model of the goal encoded by `script`: relax the
recursive well-formedness predicates, bound the integers (tightly if it can
be), then minimise, keeping each constraint that stays satisfiable, the
number of machines (`nextRef`), the range of packet ids (`clock`), the
packets in flight and delivered, and each integer's magnitude. Returns the
script solved last and its model. -/
def findModel (script : String) (timeout : Nat := 5) (calls : Nat := 80) : IO (Option (String × String)) := do
  let sexps := parseSexps script
  if sexps.isEmpty then return none
  let relaxed := relax sexps
  let sc := Script.ofSexps relaxed
  -- a first model
  let mut found : Option (Array Sexp × String) := none
  let mut tight := false
  for (extra, isTight) in [(bounds sc 2, true), (bounds sc 16, false), (bounds sc 256, false), (#[], false)] do
    if let some m ← solve (relaxed ++ extra) timeout then
      found := some (relaxed ++ extra, m)
      tight := isTight
      break
  let some (base, model0) := found | return none
  let some ws := sc.worldSyms | return some (scriptText base, model0)
  let mut cmds := base
  let mut model := model0
  let mut budget := calls
  let valueIn (m : String) (e : Sexp) : Val := eval sc (parseModel m) {} 64 e
  let intIn (m : String) (e : Sexp) : Option Int := match valueIn m e with
    | .int n => some n
    | _ => none
  -- the fewest machines, then the fewest packet ids
  for fn in [ws.nextRef, ws.clock] do
    let some f := fn | continue
    let term := app f #[ws.w]
    let some hi0 := intIn model term | continue
    let mut lo : Int := 0
    let mut hi := hi0
    while lo < hi && budget > 0 do
      budget := budget - 1
      let mid := (lo + hi) / 2
      match ← solve (cmds.push (app "assert" #[app "<=" #[term, .atom (toString mid)]])) timeout with
      | some m =>
        model := m
        hi := (intIn m term).getD mid
      | none => lo := mid + 1
    cmds := cmds.push (app "assert" #[app "<=" #[term, .atom (toString hi)]])
  -- the fewest packets in flight, then delivered
  if let some ck := ws.clock then
    let ckv := (intIn model (app ck #[ws.w])).getD 0
    for (fn, world) in [(ws.net, ws.w), (ws.log, Sexp.atom ws.g)] do
      let some f := fn | continue
      let some (_, optSort) := sc.funs[f]? | continue
      let some noneC := ((sc.ctors[optSort]?).getD #[]).find? fun c =>
          ((sc.ctorOf[c]?).map (·.2.isEmpty)).getD false
        | continue
      for j in List.range ckv.toNat do
        if budget == 0 then break
        let slot := app f #[world, .atom (toString j)]
        if valueIn model slot == .ctor noneC #[] then continue
        -- a packet in flight stays in the log
        if f != (ws.net.getD "") then
          if let some net := ws.net then
            if valueIn model (app net #[ws.w, .atom (toString j)]) != .ctor noneC #[] then continue
        budget := budget - 1
        let c := app "assert" #[app "=" #[slot, .atom noneC]]
        if let some m ← solve (cmds.push c) timeout then
          cmds := cmds.push c
          model := m
  -- the smallest magnitude of each integer, one at a time
  unless tight do
    for grp in boundGroups sc do
      for b in [2, 16] do
        if budget == 0 then break
        budget := budget - 1
        if let some m ← solve (cmds.push (grp b)) timeout then
          cmds := cmds.push (grp b)
          model := m
          break
  return some (scriptText cmds, model)

/-! ## Rendering -/

/-- What the renderer needs to know about the program. -/
structure Names where
  /-- State tag (`<Machine>_<State>`) → machine and state. -/
  tags : Std.HashMap String (String × String)
  /-- Machine kinds. -/
  machines : Array String
  /-- The fields of `F`, in order (`<Machine>_<var>`). -/
  fFields : Array String
  /-- A structure's field names, by type name. -/
  structFields : String → Option (Array String)
  /-- `function` declarations of the module (named machines, …). -/
  functions : Array String := #[]

/-- Where an obligation runs. -/
inductive Site where
  | deliver (mach state ev : String)
  | entry (mach state : String)
  | resume (mach state ev : String)
  | drop
  | base
  deriving Inhabited

/-- What an obligation says. -/
inductive Kind where
  /-- `target` holds after the step (`inv`: the invariant whose goal failed). -/
  | correct (site : Site) (target : String) (inv : Option String)
  /-- The handler does not fail. -/
  | safe (site : Site)
  /-- `ev` is never delivered to a machine in this state. -/
  | never (mach state ev : String)
  deriving Inhabited

/-- A decoded model. -/
structure Model where
  sc : Script
  defs : Defs
  names : Names
  ws : WorldSyms

namespace Model

def evalE (md : Model) (e : Sexp) : Val := eval md.sc md.defs {} 64 e

def call (md : Model) (f : String) (args : Array Val) : Val :=
  match md.defs[f]? with
  | some (ps, body) =>
    if ps.size != args.size then .unknown
    else eval md.sc md.defs ((ps.zip args).foldl (init := {}) fun m (p, v) => m.insert p v) 64 body
  | none => .unknown

def wVal (md : Model) : Val := md.evalE md.ws.w

def gVal (md : Model) : Val := md.evalE (.atom md.ws.g)

/-- The value of the scalar `base` (a local of the goal). -/
def scalar? (md : Model) (base : String) : Option Val :=
  (md.sc.funOrder.find? fun f => baseName f == base && (md.sc.funs[f]?.map (·.1.isEmpty)).getD false)
    |>.map fun f => md.evalE (.atom f)

def int? (md : Model) (base : String) : Option Int :=
  match md.scalar? base with
  | some (.int n) => some n
  | _ => none

/-- `(sort, Lean constructor name)` of an SMT constructor. -/
def ctorName (md : Model) (c : String) : String × String :=
  match md.sc.ctorOf[c]? with
  | some (dt, _) => (baseName dt, (c.drop (dt.length + 1)).toString)
  | none => ("", c)

/-- Segments `[a, b)` of `[lo, hi)` on which `f(args, ·)` is constant, with
its value. `split` are points to keep apart. -/
def segments (md : Model) (f : String) (args : Array Val) (lo hi : Int) (split : Array Int := #[]) :
    Array (Int × Int × Val) := Id.run do
  if hi ≤ lo then return #[]
  let pts : Array Int :=
    if hi - lo ≤ 64 then (List.range (hi - lo).toNat).toArray.map fun (k : Nat) => lo + (k : Int)
    else
      let lits := match md.defs[f]? with
        | some (_, body) => intLits md.sc md.defs body
        | none => #[]
      let cands := (lits ++ split).foldl (init := #[lo]) fun acc c => acc.push c |>.push (c + 1)
      let cands := cands.filter fun c => lo ≤ c && c < hi
      let sorted := cands.qsort (· < ·)
      sorted.foldl (init := #[]) fun acc c => if acc.back? == some c then acc else acc.push c
  let mut out : Array (Int × Int × Val) := #[]
  for k in [0:pts.size] do
    let a := pts[k]!
    let b := pts[k + 1]?.getD hi
    let v := md.call f (args.push (.int a))
    match out.back? with
    | some (a0, b0, v0) =>
      if b0 == a && v0 == v then out := out.pop.push (a0, b, v0) else out := out.push (a, b, v)
    | none => out := out.push (a, b, v)
  return out

/-- The model's interpretation of lookup `base` (`mem`, `get`) on datatype
`dt`, with its key sort, when the model has one. -/
def lookupFn (md : Model) (base dt : String) : Option (String × String) :=
  md.sc.funOrder.findSome? fun f =>
    match md.sc.funs[f]? with
    | some (#[d, k], _) => if baseName f == base && d == dt && md.defs.contains f then some (f, k) else none
    | _ => none

/-- The integer keys `x` with `keep (f v x)`, as segments `[a, b)` with the
value, and whether they go on past the window. -/
def intKeys (md : Model) (f : String) (v : Val) (keep : Val → Bool) (nat : Bool) :
    Array (Int × Int × Val) × Bool :=
  let lits := match md.defs[f]? with
    | some (_, body) => intLits md.sc md.defs body
    | none => #[]
  let lo := if nat then 0 else lits.foldl min 0 - 1
  let hi := lits.foldl max 7 + 2
  let segs := (md.segments f #[v] lo hi lits).filter fun (_, _, x) => keep x
  (segs, keep (md.call f #[v, .int (hi + 1000)]) || (!nat && keep (md.call f #[v, .int (lo - 1000)])))

/-- Whether field `k` of constructor `<dt>_<ctor>` holds a `Nat`. -/
def natField (md : Model) (dt ctor : String) (k : Nat) : Bool :=
  md.sc.natSels.contains s!"{dt}_{ctor}_{k}"

end Model

/-- `#a` or `#a–#b`. -/
private def rangeStr (a b : Int) : String :=
  if b == a + 1 then s!"#{a}" else s!"#{a}–#{b - 1}"

private def isMachineFieldOf (machines : Array String) (m f : String) : Bool :=
  f.startsWith (m ++ "_") &&
    !machines.any fun m' => m' != m && m'.length > m.length && f.startsWith (m' ++ "_")

/-- Render a value. -/
partial def renderVal (md : Model) (v : Val) : String :=
  match v with
  | .int n => toString n
  | .bool b => toString b
  | .elem _ | .unknown => "?"
  | .ctor c args =>
    let (srt, ctor) := md.ctorName c
    let r := renderVal md
    let paren (s : String) := if s.contains ' ' && !s.startsWith "{" && !s.startsWith "[" &&
      !s.startsWith "(" then s!"({s})" else s
    match srt, ctor, args with
    | "Option", "none", _ => "none"
    | "Option", "some", #[x] => s!"some {paren (r x)}"
    | "PSet", _, _ =>
      let dt := ((md.sc.ctorOf[c]?).map (·.1)).getD ""
      match md.lookupFn "mem" dt with
      | some (memF, "Int") =>
        let (segs, more) := md.intKeys memF v (· == .bool true) (md.natField dt "add" 1)
        let xs := segs.toList.map fun (a, b, _) => if b == a + 1 then toString a else s!"{a}..{b - 1}"
        "{" ++ ", ".intercalate (xs ++ if more then ["…"] else []) ++ "}"
      | _ =>
      let rec goSet : Val → List String → Option (List String)
        | .ctor c' xs, acc =>
          match (md.ctorName c').2, xs with
          | "empty", _ => some acc
          | "add", #[s, x] => goSet s (if acc.contains (r x) then acc else r x :: acc)
          | "del", #[s, x] => goSet s ((acc.erase (r x)) ++ [s!"-{r x}"])
          | _, _ => none
        | _, _ => none
      match goSet v [] with
      | some xs => "{" ++ ", ".intercalate (xs.filter (!·.startsWith "-")) ++ "}"
      | none => "?"
    | "PMap", _, _ =>
      let dt := ((md.sc.ctorOf[c]?).map (·.1)).getD ""
      match md.lookupFn "get" dt with
      | some (getF, "Int") =>
        let isSome (x : Val) : Bool := match x with
          | .ctor c' #[_] => (md.ctorName c').2 == "some"
          | _ => false
        let (segs, more) := md.intKeys getF v isSome (md.natField dt "put" 1)
        let xs := segs.toList.map fun (a, b, x) =>
          let k := if b == a + 1 then toString a else s!"{a}..{b - 1}"
          let y := match x with
            | .ctor _ #[y] => r y
            | _ => "?"
          s!"{k} ↦ {y}"
        "{" ++ ", ".intercalate (xs ++ if more then ["…"] else []) ++ "}"
      | _ =>
      let rec goMap : Val → List (String × String) → Option (List (String × String))
        | .ctor c' xs, acc =>
          match (md.ctorName c').2, xs with
          | "empty", _ => some acc
          | "put", #[m, k, x] => goMap m (if acc.any (·.1 == r k) then acc else (r k, r x) :: acc)
          | "del", #[m, k] => goMap m (if acc.any (·.1 == r k) then acc else (r k, "-") :: acc)
          | _, _ => none
        | _, _ => none
      match goMap v [] with
      | some xs => "{" ++ ", ".intercalate ((xs.filter (·.2 != "-")).map fun (k, x) => s!"{k} ↦ {x}") ++ "}"
      | none => "?"
    | "List", _, _ =>
      let rec goList : Val → Array String → Option (Array String)
        | .ctor c' xs, acc =>
          match (md.ctorName c').2, xs with
          | "nil", _ => some acc
          | "cons", #[h, t] => goList t (acc.push (r h))
          | _, _ => none
        | _, _ => none
      match goList v #[] with
      | some xs => "[" ++ ", ".intercalate xs.toList ++ "]"
      | none => "?"
    | "Prod", _, #[a, b] => s!"({r a}, {r b})"
    | "PUnit", _, _ => "()"
    | _, _, _ =>
      match md.names.structFields srt with
      | some #["ref"] =>
        if md.names.machines.contains srt then s!"{srt} #{args[0]?.map r |>.getD "?"}" else s!"⟨{args[0]?.map r |>.getD "?"}⟩"
      | some fs =>
        if fs.size == args.size && ctor == "mk" then
          "{" ++ ", ".intercalate ((fs.zip args).toList.map fun (f, x) => s!"{f} := {r x}") ++ "}"
        else if args.isEmpty then ctor else s!"{ctor} " ++ " ".intercalate (args.toList.map (paren ∘ r))
      | none => if args.isEmpty then ctor else s!"{ctor} " ++ " ".intercalate (args.toList.map (paren ∘ r))

/-- A machine's local state. -/
structure MState where
  mach : Option String
  state : Option String
  fields : Array (String × String)
  flags : Array String

private def decodeMachine (md : Model) (v : Val) : Option MState := do
  let .ctor _ #[st, fs, pend, wait, halted] := v | none
  let (mach, state) ← match st with
    | .ctor c #[tag] =>
      if (md.ctorName c).2 == "some" then
        match tag with
        | .ctor t _ => pure ((md.names.tags[(md.ctorName t).2]?).map (some ·.1) |>.getD none,
                             (md.names.tags[(md.ctorName t).2]?).map (some ·.2) |>.getD none)
        | _ => pure (none, none)
      else pure (none, none)
    | _ => pure (none, none)
  let fields : Array (String × String) := match fs, mach with
    | .ctor _ vs, some m =>
      (md.names.fFields.zip vs).filterMap fun (f, x) =>
        if isMachineFieldOf md.names.machines m f then
          some ((f.drop (m.length + 1)).toString, renderVal md x)
        else none
    | _, _ => #[]
  let mut flags := #[]
  match pend with
  | .ctor c #[arg] =>
    if (md.ctorName c).2 == "some" then
      let a := match arg with
        | .ctor _ #[x] => s!" (argument {renderVal md x})"
        | _ => ""
      flags := flags.push s!"entry pending{a}"
  | _ => pure ()
  match wait with
  | .ctor c #[site] =>
    if (md.ctorName c).2 == "some" then
      flags := flags.push s!"waiting at a `receive` ({renderVal md site})"
  | _ => pure ()
  if halted == .bool true then flags := flags.push "halted"
  return { mach, state, fields, flags }

/-- Machine `r`'s kind, if allocated. -/
private def kindOf (machines : Array (Int × Int × Val)) (md : Model) (nextRef : Option Int) (r : Int) :
    String :=
  match machines.find? (fun (a, b, _) => a ≤ r && r < b) with
  | some (_, _, v) => match (decodeMachine md v).bind (·.mach) with
    | some m => s!"{m} #{r}"
    | none => s!"#{r} (unallocated)"
  | none =>
    match nextRef with
    | some n => if n ≤ r then s!"#{r} (unallocated)" else s!"#{r}"
    | none => s!"#{r}"

/-- Render a packet. -/
private def renderPacket (md : Model) (who : Int → String) (v : Val) : Option String := do
  let .ctor c #[p] := v | none
  guard ((md.ctorName c).2 == "some")
  let .ctor _ #[src, dst, ev] := p | none
  let evs := match ev with
    | .ctor e args =>
      let name := (md.ctorName e).2
      match args with
      | #[pl] => s!"{name} {renderVal md pl}"
      | _ => name
    | _ => "?"
  let r (x : Val) := match x with
    | .int n => who n
    | _ => "?"
  return s!"{r src} → {r dst}  {evs}"

/-- The obligation's verdict and the pre-state it fails in. -/
def render (names : Names) (kind : Kind) (script model : String) : Option (Array String) := do
  let sc := Script.ofSexps (parseSexps script)
  let ws ← sc.worldSyms
  let md : Model := { sc, defs := parseModel model, names, ws }
  let w := md.wVal
  let nextRef := match ws.nextRef.map (md.call · #[w]) with
    | some (.int n) => some n
    | _ => none
  let clock := match ws.clock.map (md.call · #[w]) with
    | some (.int n) => some n
    | _ => none
  -- the machine the step runs on, and the packet it takes
  let site? := match kind with
    | .correct s _ _ => some s
    | .safe s => some s
    | .never m s e => some (.deliver m s e)
  let actor := match site? with
    | some (.entry ..) => md.int? "m"
    | some (.deliver ..) | some (.resume ..) => md.int? "dst"
    | _ => none
  let pkt := match site? with
    | some (.deliver ..) | some (.resume ..) => md.int? "i"
    | _ => none
  -- the handled packet's endpoints, when the goal no longer names them
  let pktEnds : Option (Int × Int) := match pkt, ws.net with
    | some i, some net => match md.call net #[w, .int i] with
      | .ctor _ #[.ctor _ #[.int a, .int b, _]] => some (a, b)
      | _ => none
    | _, _ => none
  let actor := actor <|> pktEnds.map (·.2)
  let focus := #[actor, pktEnds.map (·.1)].filterMap id
  let machines := match ws.machines with
    | some f => md.segments f #[w] 0 (min (nextRef.getD 8) 1000000) focus
    | none => #[]
  let who := kindOf machines md nextRef
  let pktFrom := match pkt with
    | some i => match ws.net.map (md.call · #[w, .int i]) with
      | some (.ctor _ #[.ctor _ #[.int src, _, _]]) => s!" (packet #{i} from {who src})"
      | _ => s!" (packet #{i})"
    | none => ""
  let mref (m : String) := match actor with
    | some r => s!"{m} #{r}"
    | none => m
  let verdict := match kind with
    | .correct site target inv =>
      let what := match inv with
        | some i => s!"invariant `{i}`"
        | none => match site with
          | .base | .drop => s!"`{target}`"
          | _ => "a loop invariant or `decreasing` measure of the handler"
      match site with
      | .deliver m s e => s!"{what} can fail after {mref m}, in state {s}, handles {e}{pktFrom}."
      | .entry m s => s!"{what} can fail after {mref m} runs the entry of state {s}."
      | .resume m s e => s!"{what} can fail after {mref m}, waiting at the `receive` in state {s}, takes {e}{pktFrom}."
      | .drop => s!"{what} can fail when a halted machine drops a packet."
      | .base => s!"{what} can fail in an initial world."
    | .safe site =>
      let how := "(an `assert` fails, or `choose` has nothing to choose from)"
      match site with
      | .deliver m s e => s!"{mref m}, in state {s}, can fail handling {e}{pktFrom} {how}."
      | .entry m s => s!"{mref m} can fail running the entry of state {s} {how}."
      | .resume m s e => s!"{mref m}, waiting at the `receive` in state {s}, can fail taking {e}{pktFrom} {how}."
      | _ => s!"the step can fail {how}."
    | .never m s e => s!"{e}{pktFrom} can reach {mref m} in state {s}, which neither handles, defers nor ignores it."
  let isBase := match site? with
    | some .base => true
    | _ => false
  let mut lines : Array String := #[verdict,
    if isBase then "Initial world:" else "Pre-state (it satisfies the premises; it need not be reachable):",
    "Machines:"]
  -- machines, by kind
  let decoded := machines.filterMap fun (a, b, v) => (decodeMachine md v).map (a, b, ·)
  let named : Array (String × Int) := names.functions.filterMap fun fn =>
    match md.scalar? fn with
    | some (.int r) => some (fn, r)
    | some (.ctor _ #[.int r]) => some (fn, r)
    | _ => none
  let row (a b : Int) (ms : MState) : String :=
    let fs := ", ".intercalate (ms.fields.toList.map fun (f, x) => s!"{f} = {x}")
    let fs := if fs.isEmpty then "" else s!"  {fs}"
    let tagged := (named.filter fun (_, r) => a ≤ r && r < b).map (·.1)
    let nm := if tagged.isEmpty then "" else s!" ({", ".intercalate tagged.toList})"
    let isActor := actor.any fun r => a ≤ r && r < b
    let cnt := if b > a + 1 then s!" ({b - a} machines)" else ""
    let fl := if ms.flags.isEmpty then "" else s!"  [{", ".intercalate ms.flags.toList}]"
    let mark := if isActor then "  ◀" else ""
    s!"    {rangeStr a b}{nm}{cnt}  {ms.state.getD "?"}{fs}{fl}{mark}"
  for m in names.machines do
    let rows := decoded.filter fun (_, _, ms) => ms.mach == some m
    unless rows.isEmpty do
      lines := lines.push s!"  {m}"
      for (a, b, ms) in rows do lines := lines.push (row a b ms)
  let unalloc := decoded.filter fun (_, _, ms) => ms.mach.isNone
  for (a, b, _) in unalloc do
    lines := lines.push s!"  {rangeStr a b}: no machine"
  if let some n := nextRef then
    lines := lines.push s!"  (references from #{n} on are unallocated)"
  -- packets
  if let (some net, some ck) := (ws.net, clock) then
    let inflight := (md.segments net #[w] 0 (min ck 1000000) (pkt.toArray)).filterMap fun (a, b, v) =>
      (renderPacket md who v).map (a, b, ·)
    lines := lines.push "In flight:"
    if inflight.isEmpty then lines := lines.push "  (none)"
    for (a, b, p) in inflight do
      let cnt := if b > a + 1 then s!" ({b - a} packets)" else ""
      let mark := if pkt.any (fun i => a ≤ i && i < b) then "  ◀" else ""
      lines := lines.push s!"  {rangeStr a b}{cnt}  {p}{mark}"
    if let some lg := ws.log then
      let g := md.gVal
      let delivered := (md.segments lg #[g] 0 (min ck 1000000)).filterMap fun (a, b, v) =>
        if inflight.any (fun (a', b', _) => a' ≤ a && b ≤ b') then none
        else (renderPacket md who v).map (a, b, ·)
      unless delivered.isEmpty do
        lines := lines.push "Delivered earlier:"
        for (a, b, p) in delivered do
          let cnt := if b > a + 1 then s!" ({b - a} packets)" else ""
          lines := lines.push s!"  {rangeStr a b}{cnt}  {p}"
  -- the module's `function`s: constants, and functions of one machine
  let fnVals := names.functions.filterMap fun fn =>
    match sc.funOrder.find? (baseName · == fn) with
    | none => none
    | some f => match sc.funs[f]? with
      | some (#[], _) =>
        let v := md.evalE (.atom f)
        if v.known then some s!"{fn} = {renderVal md v}" else none
      | some (#["Int"], _) =>
        match nextRef with
        | some n =>
          let pts := (List.range (min n 16).toNat).filterMap fun (r : Nat) =>
            let v := md.call f #[.int (r : Int)]
            if v.known then some s!"#{r} ↦ {renderVal md v}" else none
          if pts.isEmpty then none else some s!"{fn}: {", ".intercalate pts}"
        | none => none
      | _ => none
  unless fnVals.isEmpty do
    lines := lines.push "Functions:"
    for l in fnVals do lines := lines.push s!"  {l}"
  return lines

end PLean.Cex
