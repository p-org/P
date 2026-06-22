/-
Regression: `paxiom` declarations reach SMT in every obligation.

PLean materialises `paxiom <id> : <prop>` as a top-level Lean `axiom`,
but `loom_smt [*]` / lean-auto's `collectAllLemmas` reads only the
local context — top-level axioms are invisible by default. The
obligation generator (`Verify/Obligation.lean`) injects a
`have hax_<id> := @<id>` per pmodule axiom into both per-handler and
base-case obligations so the SMT pipeline sees them.

This file pins that bridge: the `base_block0_always_true` base case
needs `f_total` to discharge `∀ x, f x = true` at the init state, and
the per-handler step needs the same axiom (the handler doesn't touch
`f`, so the post-state property is literally the axiom). Without the
fix the base case is reported `disproved`.

Mirrored shape in the `RingLeader` benchmark: `le` / `btw` / `right` are
opaque `function`s whose defining axioms are stated as `paxiom`s rather
than `init-holds` clauses, and every obligation about them closes
without restating them as `invariant` bundles.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule AxiomProbe

  type N

  -- Opaque function over an opaque sort, with a stated axiom.
  function f (x : N) : Bool

  paxiom f_total : ∀ x : N, f x = true

  event ePing

  machine Server {
    start state S {
      on ePing { pure () }
    }
  }

  Lemma keep_f_true {
    invariant always_true : ∀ x : N, f x = true
  }
  Proof {
    prove keep_f_true ;
  }

end AxiomProbe

#gen_module AxiomProbe
#pwf        AxiomProbe

#pverify AxiomProbe
