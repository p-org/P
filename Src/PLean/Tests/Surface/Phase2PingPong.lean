/-
Surface-syntax ping-pong auto-discharged via `#pverify`. One handler
calls `send`, the other is `pure ()`; the invariant is `True`.

The hand-written manual-proof variant (`PongAfterPing` over the real
`PM`) lives in [`Phase2PingPong_manual.lean`](Phase2PingPong_manual.lean)
as a regression for `wpgen` + raw Loom primitives.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule Phase2PingPong

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

end Phase2PingPong

#gen_module Phase2PingPong
#pwf        Phase2PingPong
set_option pverify.failOnIncomplete false in
#pverify Phase2PingPong
