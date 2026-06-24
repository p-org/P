/-
PLean.Syntax.Notation — front-end sugar for the predicate primitives
defined in `Semantics/Predicates.lean`.

  `a ≺ b`         ↔ `precedes a b`     -- temporal precedence
  `lbl is ev`     ↔ `is_<ev> lbl`      -- tag-only event check
  `lbl targets m` ↔ `Label.targets? lbl m`

`is` is a *tag* check, not a payload-equality check (P's surface
semantics asks "is the label carrying *some* instance of `<ev>`?",
ignoring the payload). The `is_<ev>` predicate is emitted per event
in `Commands/GenModule.lean`; `lbl is <ev>` rewrites to
`is_<ev> lbl` at parse time and resolves against the surrounding
pmodule namespace.

`inflight l` and `sent l` stay as plain function applications;
the predicates are already named after their P keyword.
-/
import PLean.Semantics.Predicates

namespace PLean

/-- `a ≺ b` — label `a` was sent before label `b`. -/
scoped notation:50 a " ≺ " b => PLean.precedes a b

/-- `lbl is ev` — the label carries some instance of event `ev`,
ignoring payload. Expands to a per-event predicate `is_<ev>` emitted
by `#gen_module`. The predicate is constructed with `mkIdentFrom` so
the resulting identifier is unhygienic and resolves against
`<Mod>.is_<ev>` (the same way `Sig`/`E` resolve from handler bodies).

We use a macro rather than a `notation` because the rewrite depends
on the RHS being a *name* — a `notation` can't decompose its RHS
into "concat with `is_`". -/
scoped syntax:50 (name := pIsTermMacro) term:51 " is " ident : term

scoped macro_rules
  | `($lbl is $ev:ident) => do
    let predName : Lean.Name :=
      Lean.Name.mkSimple ("is_" ++ ev.getId.toString)
    let predIdent := Lean.mkIdentFrom ev predName
    -- Parenthesise the expansion: without the outer parens, the
    -- splice site treats `($predIdent $lbl) ∧ rest` as
    -- `is_<ev> (lbl ∧ rest)`, since application is left-associative
    -- and binds tighter than ∧.
    `(($predIdent $lbl))

/-- `lbl targets m` — the label's target ref is `m`. Mirrors P's
`<lbl> targets <m>` keyword. -/
scoped notation:50 lbl " targets " m => PLean.Label.targets? lbl m

end PLean
