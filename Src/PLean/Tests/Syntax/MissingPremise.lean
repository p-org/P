import PLean

open PLean PartialCorrectness DemonicChoice

pmodule MissingPremiseProbe

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

  -- `framework` is declared so the parse-time `using` check accepts it,
  -- but no `prove framework ;` directive exists. Pre-fix the obligation
  -- would assume `framework` in `safety`'s precondition unproven; the
  -- post-fix detection emits a `.missingPremise` failure.
  Lemma framework {
    invariant inc_count :
      ∀ a : Sig.Label, s.sent a = true → a.actionCount < s.actionCount
  }

  Theorem safety {
    invariant pong_after_ping :
      ∀ q : ePong, s.sent q = true →
        ∃ p : ePing, s.sent p = true ∧ p ≺ q
  }

  Proof Safety {
    prove safety using framework ;
    prove default ;
  }

end MissingPremiseProbe

#gen_module MissingPremiseProbe

/--
warning: MissingPremiseProbe: 6 proved by SMT, 0 user-proved, 0 disproved, 0 unknown, 0 tactic-error, 0 no-diagnostic, 1 missing-premise
1 `using` premise(s) cite a lemma that no `prove` directive proves; add the missing `prove <lemma>;` directives.
-/
#guard_msgs (warning, drop info) in
set_option pverify.failOnIncomplete false in
#pverify MissingPremiseProbe
