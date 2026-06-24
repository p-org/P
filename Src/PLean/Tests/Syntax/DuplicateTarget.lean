/-
Two `Proof` blocks targeting the same lemma must produce distinct
theorem names. The Proof-block tag (`Block1` / `Block2`) is embedded
in the obligation name to disambiguate.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

pmodule Phase3DuplicateTargetMod

  event eHello

  machine M {
    start state S {
      on eHello { pure () }
    }
  }

  Theorem safety {
    invariant always_true : True
  }

  Proof Block1 {
    prove safety ;
  }

  Proof Block2 {
    prove safety ;
  }

end Phase3DuplicateTargetMod

#gen_module Phase3DuplicateTargetMod
#pverify    Phase3DuplicateTargetMod

namespace Phase3DuplicateTargetMod

-- Both disambiguated names must resolve.
#check @M.S.eHello_correct_Block1_safety
#check @M.S.eHello_correct_Block2_safety

end Phase3DuplicateTargetMod
