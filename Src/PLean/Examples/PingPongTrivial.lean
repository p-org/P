/-
Smoke test for the `#pverify` auto-discharge path: a pmodule with
trivial-`pure ()` handlers and a `True` invariant. Every obligation
closes via the `pverify` tactic's trivial-handler branch.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule PingPongTrivial

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

end PingPongTrivial

#gen_module PingPongTrivial
#pwf        PingPongTrivial
#pverify    PingPongTrivial
