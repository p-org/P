/-
Smoke test for the `#pverify` auto-discharge path: a pmodule with an
`ignore`d event and a `True` invariant. The ignored event has no
handler def and is skipped by the obligation generator, so the only
obligation left is the base case for `always_true`, which closes by
`trivial`.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule PingPongTrivial

  event eHello

  machine Greeter {
    start state Idle {
      ignore eHello
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
