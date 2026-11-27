/-
A lemma named like a handler local. `#pverify` hands the module's definitions
to `simp` by their full names, so the local does not capture the lemma's name
(it used to fail with "Variable `merged` is not a proposition or
let-declaration").
-/
import PLean

open PLean

pmodule ClashT

  system s

  event eGo : Int

  machine M {
    var total : Int
    var items : set[Int]

    start state Run {
      on eGo (n : Int) {
        var merged : Int
        merged = 0
        foreach (i in items) invariant nonneg : merged ≥ 0 ; {
          merged = merged + 1
        }
        total = merged
      }
    }
  }

  init-holds ∀ m : M, m.total = 0

  Lemma merged {
    invariant go_routing : ∀ e : eGo, sent e s → is_M e.target s
    invariant total_nonneg : ∀ m : M, m.total ≥ 0
  }

  Proof {
    prove merged ;
  }

end ClashT

#gen_module ClashT

#pverify ClashT
