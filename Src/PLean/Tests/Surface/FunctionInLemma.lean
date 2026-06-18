/-
Regression: a `function` value is referenceable from inside a
`Lemma` / `Theorem` invariant body, not just from `init-holds`.

`#gen_module` must materialise `function` declarations BEFORE
invariants — invariant bodies may reference a function
(`n.server = lock_server`), but functions only reference types /
machine fields and never reference invariants. An earlier ordering
emitted invariants first, so a `Lemma`-block reference to a
`function` failed with `Unknown identifier`.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule FnInLemma

  event eGo

  -- Foreign machine-typed constant (P's `pure srv() : machine`).
  function srv : Server

  machine Server {
    var up : Bool
    start state Serving { on eGo { up = true } }
  }

  machine Node {
    var server : Server
    start state Working { on eGo { pure () } }
  }

  -- Reference from `init-holds` (always worked).
  init-holds ∀ n : Node, n.server = srv

  -- Reference from a `Lemma` invariant (the regressed path).
  Lemma cfg {
    system s {
      invariant const_server : ∀ n : Node, n.server = srv
    }
  }
  Proof { prove cfg ; }

  -- Reference from a `Theorem` invariant too.
  Theorem safety {
    system s {
      invariant srv_ref_stable : ∀ n : Node, n.server = srv
    }
  }
  Proof { prove safety using cfg ; }

end FnInLemma

#gen_module FnInLemma

-- The materialised invariant bodies mention `srv`; if they elaborate,
-- the ordering is correct. Pin the shape.
/-- info: FnInLemma.const_server : GlobalState FnInLemma.Sig → Prop -/
#guard_msgs in
#check @FnInLemma.const_server

/-- info: FnInLemma.srv_ref_stable : GlobalState FnInLemma.Sig → Prop -/
#guard_msgs in
#check @FnInLemma.srv_ref_stable
