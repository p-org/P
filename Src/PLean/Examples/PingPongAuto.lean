/-
Surface-syntax ping-pong auto-discharged via `#pverify`. One handler
calls `send`, the other is `pure ()`; the invariant is `True`.

The hand-written manual-proof variant (`PongAfterPing` over the real
`PM`) lives in [`PingPongManual.lean`](PingPongManual.lean) as a
regression for `wpgen` + raw Loom primitives.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule PingPongAuto

  event ePing : PLean.MachineRef
  event ePong

  machine Server {
    start state Idle {
      on ePing (replyTo : PLean.MachineRef) {
        send replyTo, ePong
      }
    }
  }

  machine Client {
    start state Booting {
      on ePong { pure () }
    }
  }

  Theorem trivial_safety {
    invariant always_true : True
  }

  Proof Safety {
    prove trivial_safety ;
  }

end PingPongAuto

#gen_module PingPongAuto
#pwf        PingPongAuto
#pverify    PingPongAuto
