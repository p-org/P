/-
Parse-only smoke test for `Lemma` / `Theorem` / `Proof`.
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
    invariant trivial1 : True
    invariant trivial2 : 1 + 1 = 2
  }

  Theorem safety {
    invariant top : True
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
