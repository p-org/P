/-
PLean.Syntax.ExtEq — P's `==` on containers.

`PSet` / `PMap` values are update histories, so Lean's `=` on them compares
histories: `{1, 2}` built in two orders, or `(∅.add x).del x` and `∅`, are
different values. P's `==` compares contents.

* In specifications, `pv_ext% p` elaborates `p` and replaces each `a = b` /
  `a ≠ b` between sets or maps by `PSet.Equiv a b` / `PMap.Equiv a b` (and
  its negation). Invariant, `init-holds`, `paxiom` and `function` bodies go
  through it. A value that contains a container (a tuple with a set field,
  a message, a map's values) is still compared structurally.
* In handlers, `==` on containers is extensional (`BEq` instances in
  `PLean.Semantics.Containers`); `containerDecideEq?` finds a decided `=`
  between containers (`if s = t`, `decide (s = t)`), which `#gen_module`
  rejects.
-/
import Lean
import PLean.Semantics.Containers

namespace PLean

open Lean Meta Elab Term

/-- The extensional equality of `a` and `b` when `α` is a set or map type. -/
private def extEq? (α a b : Expr) : MetaM (Option Expr) := do
  let α ← whnfR (← instantiateMVars α)
  match α.getAppFn.constName? with
  | some ``PSet => return some (← mkAppM ``PSet.Equiv #[a, b])
  | some ``PMap => return some (← mkAppM ``PMap.Equiv #[a, b])
  | _ => return none

/-- Replace `=` / `≠` between sets or maps by their extensional forms. -/
def extContainerEqs (e : Expr) : MetaM Expr :=
  Meta.transform e (pre := fun e => do
    match e.getAppFnArgs with
    | (``Eq, #[α, a, b]) =>
      match ← extEq? α a b with
      | some e' => return .visit e'
      | none => return .continue
    | (``Ne, #[α, a, b]) =>
      match ← extEq? α a b with
      | some e' => return .visit (mkNot e')
      | none => return .continue
    | _ => return .continue)

/-- Whether `α` is a set or map type. -/
private def isContainerType (α : Expr) : MetaM Bool := do
  let α ← if α.hasLooseBVars || α.hasMVar then pure α else whnfR α
  return match α.getAppFn.constName? with
    | some ``PSet | some ``PMap => true
    | _ => false

/-- The subterms of `e` that `f` selects (each shared subterm visited once). -/
private def collect (f : Expr → Option Expr) (e : Expr) : MetaM (Array Expr) := do
  let out ← IO.mkRef (#[] : Array Expr)
  e.forEach fun sub => do
    if let some x := f sub then out.modify (·.push x)
  out.get

/-- A proposition that a handler decides (an `if` condition, a `decide`) and
that compares sets or maps with `=` / `≠`, if any. -/
def containerDecideEq? (e : Expr) : MetaM (Option Expr) := do
  let conds ← collect (e := e) fun sub =>
    match sub.getAppFnArgs with
    | (``ite, #[_, c, _, _, _]) | (``dite, #[_, c, _, _, _]) | (``Decidable.decide, #[c, _]) => some c
    | _ => none
  for c in conds do
    let eqs ← collect (e := c) fun sub =>
      match sub.getAppFnArgs with
      | (``Eq, #[α, _, _]) | (``Ne, #[α, _, _]) => some α
      | _ => none
    for α in eqs do
      if ← isContainerType α then return some c
  return none

/-- `pv_ext% p`: `p` with `=` / `≠` between sets or maps read as P's `==`.
A decided `=` between containers (`if s = t …`, `decide (s = t)`) is
rejected: its `Decidable` instance is the structural one. -/
syntax (name := pvExt) "pv_ext% " term : term

@[term_elab pvExt] def elabPvExt : TermElab := fun stx expected? => do
  let e ← elabTerm stx[1] expected?
  synthesizeSyntheticMVarsNoPostponing
  let e ← instantiateMVars e
  if (← containerDecideEq? e).isSome then
    throwErrorAt stx[1] "`=` between sets or maps compares their update histories; compare \
      them with `==` (P's `==`, which compares their contents)"
  extContainerEqs e

end PLean
