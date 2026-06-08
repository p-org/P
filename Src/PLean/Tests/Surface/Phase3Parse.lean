/-
PLean Phase-3 — parse-only smoke test for `Lemma` / `Theorem` / `Proof`.

Confirms the new D19 surface shapes register correctly and round-trip
through `#print_pmodule`. No verification — that's the job of
`Phase3DistributedLock.lean` and friends.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule Phase3Parse

  event eHello

  machine Greeter {
    start state Idle {
      on eHello { pure () }
    }
  }

  Lemma sanity {
    invariant trivial1 : ∀ s : GlobalState Sig, True
    invariant trivial2 : ∀ s : GlobalState Sig, 1 + 1 = 2
  }

  Theorem safety {
    invariant top : ∀ s : GlobalState Sig, True
  }

  Proof Sanity {
    prove sanity ;
    prove default ;
  }

  Proof {
    prove safety using sanity ;
  }

end Phase3Parse

#gen_module Phase3Parse
#pwf        Phase3Parse
