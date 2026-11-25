/-
Surface-syntax ping-pong auto-discharged via `#pverify`. Proves the
same safety property as the hand-written
[`PingPongManual.lean`](PingPongManual.lean) variant:

  Every sent `ePong` is preceded (`≺`) by some sent `ePing`.

The Server handler sends a fresh `ePong` in response to an `ePing`;
since `≺` compares `actionCount` and `actionCount` strictly
increments per send, the dispatched `ePing` (in-flight at pre-state,
hence in `sent`) precedes the freshly sent `ePong`. The Client
handler is a no-op so the invariant transfers verbatim.
-/
import PLean

open PLean Std.WP

pmodule PingPongAuto

  system s

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
      ignore ePong
    }
  }

  Theorem safety {

    invariant pong_after_ping :
      ∀ q : ePong, sent q s →
        ∃ p : ePing, sent p s ∧ p ≺ q

    -- Routing: needed for the "never receives" checks (a Server never
    -- gets an `ePong`, a Client never gets an `ePing`).
    invariant ping_routing :
      ∀ e : ePing, sent e s → is_Server e.target s ∧ is_Client (ePing_payload_of e) s

    invariant pong_routing :
      ∀ e : ePong, sent e s → is_Client e.target s

  }

  Proof Safety {
    prove safety ;
  }

end PingPongAuto

#gen_module PingPongAuto
#pwf        PingPongAuto
#pverify    PingPongAuto
