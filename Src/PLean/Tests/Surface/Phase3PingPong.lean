/-
Smoke test for the `#pverify` auto-discharge path: a pmodule with
trivial-`pure ()` handlers and a `True` invariant. Every obligation
closes via the `pverify` tactic's trivial-handler branch.
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
    invariant always_true : True
  }

  Proof Safety {
    prove trivial_safety ;
  }

end Phase3PingPongTrivial

#gen_module Phase3PingPongTrivial
#pwf        Phase3PingPongTrivial
#pverify    Phase3PingPongTrivial
