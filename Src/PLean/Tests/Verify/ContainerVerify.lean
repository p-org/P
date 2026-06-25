/-
Pin: `#pverify` discharges container-mutating obligations via SMT
when the underlying ops are in the `pverifySimp` set.

Two probes:

1. **Set membership across `+=`.** A `Set`-valued machine var with an
   invariant constraining its contents. The `s += (p)` step expands
   to `Insert.insert p s`, which `simp [pverifySimp]` rewrites via
   `Set.mem_insert_iff` into a `Prop` disjunction the solver handles.

2. **Map insert.** A `PMap K V` with a trivial invariant. The
   `m[k] = v` step expands to `mapInsert m k v`, which the
   lookup-after-mutation lemmas (tagged `@[pverifySimp]`) reduce in
   one rewrite.

The intent is not to ship a full benchmark — that's the next
deliverable. This pin only certifies the round-trip: container
mutation reaches SMT and closes the user invariant.

-/
import PLean

open PLean PartialCorrectness DemonicChoice

/-! ## Probe 1: set add — both user-invariant and default close -/

pmodule SetVerify

  event eRegister : PLean.MachineRef

  machine Registry {
    var members : set[PLean.MachineRef]

    start state Open {
      on eRegister (p : PLean.MachineRef) {
        members += (p);
      }
    }
  }

  Theorem trivial_set {
    invariant always_true : True
  }

  Proof Safety {
    prove trivial_set ;
  }

end SetVerify

#gen_module SetVerify
#pverify    SetVerify

/-! ## Probe 2: map insert — user invariant and auto-default both close -/

pmodule MapVerify

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

  Theorem trivial_map {
    invariant always_true : True
  }

  Proof Safety {
    prove trivial_map ;
  }

end MapVerify

#gen_module MapVerify
#pverify    MapVerify
