/-
Pin: the `set[T]` / `map[K, V]` / `seq[T]` / `option[T]` surface
parses, desugars to the right Lean types, and the `+=` / `-=` /
`m[k] = v` doElem macros expand inside a handler body so that
machine-`var`s of container type can be read and written.

The probe verifies the macro surface — type elaboration + handler
materialisation. SMT-side verification of a container-using
invariant lives in `Tests/Verify/ContainerVerify.lean`.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

/-! ## Set container -/

pmodule SetContainer

  event eAdd : PLean.MachineRef
  event eRemove : PLean.MachineRef

  machine Bag {
    var contents : set[PLean.MachineRef]

    start state Active {
      on eAdd (p : PLean.MachineRef) {
        contents += (p);
      }
      on eRemove (p : PLean.MachineRef) {
        contents -= (p);
      }
    }
  }

end SetContainer

#gen_module SetContainer

-- Both handlers materialised.
#check @SetContainer.Bag.Active.eAdd_handler
#check @SetContainer.Bag.Active.eRemove_handler

/-! ## Map container with set values (the 2PC/Paxos shape) -/

pmodule MapContainer

  event eVote : PLean.MachineRef

  machine Coordinator {
    var votes : map[Nat, set[PLean.MachineRef]]

    start state Collecting {
      on eVote (m : PLean.MachineRef) {
        votes[0] += (m);
      }
    }
  }

end MapContainer

#gen_module MapContainer

#check @MapContainer.Coordinator.Collecting.eVote_handler

/-! ## Map with primitive values: `m[k] = v` -/

pmodule MapAssign

  type tEntry = (key : Nat, value : Nat)
  event eSet : tEntry

  machine Store {
    var kv : map[Nat, Nat]

    start state Open {
      on eSet (req : tEntry) {
        kv[req.key] = req.value;
      }
    }
  }

end MapAssign

#gen_module MapAssign

#check @MapAssign.Store.Open.eSet_handler

/-! ## Option container in payload type -/

pmodule OptionContainer

  type tLookup = (key : Nat, hit : option[Nat])
  event eLookup : tLookup

  machine Cache {
    var last : option[Nat]

    start state Idle {
      on eLookup (req : tLookup) {
        last = req.hit;
      }
    }
  }

end OptionContainer

#gen_module OptionContainer

#check @OptionContainer.Cache.Idle.eLookup_handler
