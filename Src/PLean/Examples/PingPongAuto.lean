/-
Surface-syntax ping-pong, verified by `#pverify`:

  Every sent `ePong` is preceded (`≺`) by some sent `ePing`.

The Server sends a fresh `ePong` in response to an `ePing`; the
delivered `ePing` was sent earlier (packet ids record send order), so
it precedes the new `ePong`.
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
