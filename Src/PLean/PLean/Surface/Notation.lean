/-
PLean.Surface.Notation — front-end sugar for the predicate primitives
defined in `Semantics/Predicates.lean`.

Decision D16: notation lands in Phase 2 (rather than waiting until
Phase 3) because it's the smallest piece of new surface and the
headline P feature that distinguishes PLean from PVerifier — the
temporal precedence operator `≺`. Bringing it in now means Phase-2's
PingPong invariant can be stated in P-style syntax.

  `a ≺ b`        ↔ `precedes a b`     -- temporal precedence
  `lbl is ev`    ↔ `is_ev lbl`         -- tag-only event check
  `lbl targets m` ↔ `Label.targets? lbl m`

`is` is a *tag* check, not a payload-equality check. P's surface
semantics: `<lbl> is <ev>` asks "is the label carrying *some*
instance of `<ev>`?", regardless of payload. We encode this by
emitting a `is_<ev>` predicate per event in `Commands/GenModule.lean`
and rewriting `lbl is <evIdent>` to `is_<evIdent> lbl` at parse time.
The resulting constant `is_<evIdent>` resolves against the
surrounding namespace — handler bodies and invariants inside the
pmodule see `is_ePing` as `<Mod>.is_ePing`.

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
