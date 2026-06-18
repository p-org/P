/-
PLean.Verify.CexParse — parse a solver `(get-model)` reply into
`define-fun` entries and de-mangle lean-auto's atom names.

`loom_smt` throws `"<solver>: the goal is false:<MODEL>"` where
`<MODEL>` is the solver's model re-stringified from a parsed
S-expression. This module recovers the structure: it strips the prose
prefix, re-parses the S-expression with `Auto.Parser.SMTSexp`, keeps
the user-relevant `define-fun` entries, and maps lean-auto's mangled
symbols back to readable names.

lean-auto names atoms `"_" ++ delab(originalExpr)`, with a `.<gensym>_`
suffix on record fields freed by `sdestruct_state`. The de-mangling is
a string transform — `_sent.1546_ → sent`, `_Label.actionCount →
Label.actionCount` — sufficient for v1. Exact symbol→`Expr` recovery
via lean-auto's name map is a later refinement.
-/
import Auto.Parser.SMTSexp
import Auto.Solver.SMT

open Lean
open Auto.Parser.SMTSexp

namespace PLean
namespace Verify

/-- One `(define-fun NAME (ARGS) SORT BODY)` entry from a model.
`args` is empty for a nullary constant. `name` is already de-mangled;
`body`/`sort` keep their parsed S-expression form. -/
structure ModelDef where
  name : String
  args : Array Sexp
  sort : Sexp
  body : Sexp
  deriving Inhabited

/-- Drop lean-auto's leading `_` and the `.<digits>_` gensym suffix a
field acquires after `sdestruct_state`, leaving the readable base.

  `_sent.1546_`        → `sent`
  `_machines.116_`     → `machines`
  `_Label.actionCount` → `Label.actionCount`
  `_Fields.Bad_x`      → `Fields.Bad_x`
  `_b.99_`             → `b`

A trailing `.<digits>` segment whose component is all digits is treated
as a gensym tag and dropped; a `.<member>` segment (e.g. `actionCount`)
is kept. -/
def demangle (s : String) : String :=
  let s := if s.startsWith "_" then s.drop 1 else s
  let s := if s.endsWith "_" then s.dropRight 1 else s
  let parts := s.splitOn "."
  -- Drop any trailing all-digit segments (gensym counters).
  let isDigits (p : String) : Bool := !p.isEmpty && p.all Char.isDigit
  let rec trimTrailing : List String → List String
    | []      => []
    | [p]     => [p]
    | p :: ps =>
      let rest := trimTrailing ps
      match rest with
      | [last] => if isDigits last then [p] else p :: rest
      | _      => p :: rest
  ".".intercalate (trimTrailing parts)

/-- Re-map a parsed symbol atom through `demangle`; leave every other
lexeme untouched. Recurses into application nodes so the body of a
`define-fun` is de-mangled too. -/
partial def demangleSexp : Sexp → Sexp
  | .atom (.symb s) => .atom (.symb (demangle s))
  | .atom l         => .atom l
  | .app xs         => .app (xs.map demangleSexp)

/-- Symbols that are lean-auto / solver boilerplate rather than program
state: re-printed negated hypotheses (`valid_fact_*`), solver-internal
helpers, and the trust axiom. These carry no model assignment and only
clutter the report. -/
def isBoilerplateName (s : String) : Bool :=
  s.startsWith "valid_fact"
  || s.startsWith "_valid_fact"
  || s.startsWith "k!"
  || s.startsWith "_uniq"
  || s == "trust_smt"

/-- Pull the model S-expression out of `loom_smt`'s thrown message.
Returns the text after the first `the goal is false:` marker, or `none`
if the marker is absent (so a non-SAT diagnostic falls through). -/
def extractModelText (msg : String) : Option String :=
  let marker := "the goal is false:"
  match (msg.splitOn marker) with
  | _ :: rest@(_ :: _) => some (marker.intercalate rest).trimLeft
  | _                  => none

/-- Recognise a `(define-fun NAME (ARGS…) SORT BODY)` node. -/
private def asDefineFun? : Sexp → Option ModelDef
  | .app xs => do
    let #[hd, nm, .app args, sort, body] := xs | none
    let .atom (.symb dfn) := hd | none
    unless dfn == "define-fun" do none
    let .atom (.symb rawName) := nm | none
    some { name := demangle rawName
           args
           sort := demangleSexp sort
           body := demangleSexp body }
  | _ => none

/-- Parse a model string into its `define-fun` entries.

The model is either a single `(model …)` application or a bare
`(…)` list of `define-fun`s, depending on the solver. We accept both,
parse the leading S-expression, walk its top-level children, and keep
the non-boilerplate `define-fun`s. Returns `none` only when the text
doesn't parse as an S-expression at all (caller falls back to raw). -/
def parseModel (modelText : String) : Option (Array ModelDef) :=
  match parseSexp modelText 0 {} with
  | .complete sexp _ =>
    let children :=
      match sexp with
      | .app xs =>
        -- `(model d1 d2 …)` keeps the `model` head; a bare `(d1 d2 …)`
        -- list does not. Drop a leading `model`/`sat` symbol if present.
        match xs[0]? with
        | some (Sexp.atom (LexVal.symb h)) =>
          if h == "model" || h == "sat" then xs.extract 1 xs.size else xs
        | _ => xs
      | other => #[other]
    let defs := children.filterMap asDefineFun?
    some (defs.filter (fun d => !isBoilerplateName d.name))
  | _ => none

end Verify
end PLean
