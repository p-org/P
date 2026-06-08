/-
PLean Phase-3 — auto-discharged obligations via `#pverify`.

This is the *successful* Phase-3 demo: a small pmodule with no machine
`var` blocks (so no `_get`/`_set` accessors that `wpgen` can't step
through without extra spec registration) and only trivial handler
bodies (`pure ()`), so the `pverify` tactic's
`intro s h; exact h` branch closes every obligation.

The test exercises the Phase-3 surface — `Lemma`/`Theorem`/`Proof`
blocks (D19), the obligation generator (D18/D23), and the rewired
`#pverify` (D22) — without depending on the harder cases the tactic
doesn't yet handle (R15).

Compare with `Tests/Surface/Phase2PingPong.lean` (M2): Phase 2 had
hand-written `theorem ..._correct ... := by wpgen ...` per handler;
Phase 3 collapses those into a single `Proof { prove safety; }`
declaration that `#pverify` walks for the user.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule Phase3PingPongTrivial

  event eHello

  machine Greeter {
    start state Idle {
      on eHello { pure () }
    }
  }

  Theorem trivial_safety {
    -- Trivial invariant — true in every state. The `pverify` tactic's
    -- trivial-handler branch (intro s h; exact h) closes this without
    -- needing wpgen to step through any state-mutating primitives.
    invariant always_true : ∀ s : GlobalState Sig, True
  }

  Proof Safety {
    prove trivial_safety ;
  }

end Phase3PingPongTrivial

#gen_module Phase3PingPongTrivial
#pwf        Phase3PingPongTrivial
#pverify    Phase3PingPongTrivial
