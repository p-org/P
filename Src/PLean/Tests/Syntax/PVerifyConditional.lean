/-
PLean Phase-3 — SMT discharge on conditional handlers.

Probe 1: an `if cond then do <updates> else pure ()` handler with no
`send` — closes via SMT.

Probe 2: same shape plus a `send` call — exercises the
accessor + primitive unfold path through `wpgen`.
-/
import PLean

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 8

open PLean PartialCorrectness DemonicChoice

pmodule PVerifyConditionalProbe

  type tGrant  = (epoch : Int)
  event eGrant : tGrant

  machine Node {
    var epoch : Int
    var held  : Bool

    start state Act {
      on eGrant (payload : tGrant) {
        if (held && decide (payload.epoch > epoch)) then do
          held = false
      }
    }
  }

  Theorem trivial {
    invariant always_true : True
  }

  Proof Safety {
    prove default ;
  }

end PVerifyConditionalProbe

#gen_module PVerifyConditionalProbe

set_option pverify.failOnIncomplete false in
#pverify PVerifyConditionalProbe

/-! ## Probe 2 — conditional handler with a `send` call. -/

pmodule PVerifyConditionalProbeSend

  type tGrant  = (epoch : Int)
  event eGrant : tGrant
  event eAccept

  machine Node {
    var epoch : Int
    var held  : Bool

    start state Act {
      on eGrant (payload : tGrant) {
        if (held && decide (payload.epoch > epoch)) then do
          held = false
          send this.ref, eAccept
      }
    }
  }

  Proof Safety {
    prove default ;
  }

end PVerifyConditionalProbeSend

#gen_module PVerifyConditionalProbeSend

set_option pverify.failOnIncomplete false in
#pverify PVerifyConditionalProbeSend
