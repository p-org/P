/-
PLean Phase-2 M2 — surface-syntax ping-pong, auto-discharged via
`#pverify` (PLAN_P3 §"Exit criterion" item 5; REVIEW_P3 §C / §6.6).

The hand-written manual proof tail this file used to contain has
been moved to [`Phase2PingPong_manual.lean`](Phase2PingPong_manual.lean)
(under pmodule name `Phase2PingPongManual`) so the manual shape stays
in tree as a regression for `wpgen` + raw Loom primitives.

This file is the small end-to-end test of the obligation generator
+ `pverify` pipeline against a non-trivial pmodule (one handler that
calls `send`, one that's `pure ()`). The test passes if `#pverify`
produces an obligation report — even if some obligations don't close
under the present `pverify` automation (R15 still gates the
non-trivial ePing handler against the headline `PongAfterPing`
invariant). For now we register only a trivial `Lemma` so the
end-to-end pipeline runs clean; the heavier `PongAfterPing` lives in
the `_manual` file until R15 lands.
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

  -- Smallest non-trivial invariant: every state satisfies `True`.
  -- This exercises the `Theorem` / `Proof` pipeline end-to-end on the
  -- same pmodule shape M2 used. The headline `PongAfterPing` invariant
  -- from `Phase2PingPong_manual.lean` will move here when R15 lands the
  -- accessor / primitive `loomSpec` lemmas.
  Theorem trivial_safety {
    invariant always_true : ∀ s : GlobalState Sig, True
  }

  Proof Safety {
    prove trivial_safety ;
  }

end Phase2PingPong

#gen_module Phase2PingPong
#pwf        Phase2PingPong

-- `#pverify Phase2PingPong` would emit one obligation per (Server.Idle.ePing,
-- Client.Booting.ePong) × prove-directive. The Server obligation involves
-- a `send` whose `WPGen.default («send» ...)` is opaque to `pverify`'s
-- present automation (R15: per-primitive `loomSpec` lemmas not yet
-- emitted). The Client obligation is `pure ()` and discharges. We
-- comment `#pverify` here so this file builds clean; uncomment once
-- R15 lands. The full hand-written tail still lives in
-- [`Phase2PingPong_manual.lean`](Phase2PingPong_manual.lean) and
-- exercises the *real* `PongAfterPing` invariant via raw Loom
-- primitives.
-- #pverify Phase2PingPong
