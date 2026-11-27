/-
End-to-end: invariants about a container that normalisation cannot compute
with, because the update is not the lookup's own argument. The solvers get
the lookup's equation at each update term of the goal (`pv_lookup_facts`),
and `Option.isSome`'s equations for `∈` on a map:
- the values of a map that starts empty;
- the members of the sets stored in a map;
- a message that carries a set, or a map, its sender has just updated.
Every proof must replay in Lean (`pverify.trust "reconstruct"`), so an
obligation that only the solver's word supports fails the test.
(`Tests/Verify/CexE2E.lean` pins that a goal about a set that does not hold
still gets its counterexample.)
-/
import PLean

open PLean

/-! The values of a map that starts empty: `¬ i ∈ data` rules out
`data[i]? = some v`. -/

pmodule MapValT

  system s

  type tPut = (key : Int, val : Int)
  event ePut : tPut

  machine Store {
    var data : map[Int, Int]

    start state Serve {
      on ePut (p : tPut) {
        if p.val ≥ 0 then do
          data[p.key] = p.val
      }
    }
  }

  init-holds ∀ (x : Store) (i : Int), ¬ (i ∈ x.data)

  Lemma values {
    invariant nonneg : ∀ (x : Store) (i v : Int), x.data[i]? = some v → v ≥ 0
  }

  Proof {
    prove values ;
  }

end MapValT

#gen_module MapValT

set_option pverify.trust "reconstruct" in
#pverify MapValT

/-! The members of the sets stored in a map. -/

pmodule SetInMapT

  system s

  type tAdd = (key : Int, val : Int)
  event eAdd : tAdd

  machine Store {
    var groups : map[Int, set[Int]]

    start state Serve {
      on eAdd (p : tAdd) {
        if p.val ≥ 0 then do
          if ¬ p.key ∈ groups then do
            groups[p.key] = ∅
          groups[p.key] += (p.val)
      }
    }
  }

  init-holds ∀ (x : Store) (i : Int), ¬ (i ∈ x.groups)

  Lemma members {
    invariant nonneg :
      ∀ (x : Store) (i v : Int) (q : set[Int]), x.groups[i]? = some q → v ∈ q → v ≥ 0
  }

  Proof {
    prove members ;
  }

end SetInMapT

#gen_module SetInMapT

set_option pverify.trust "reconstruct" in
#pverify SetInMapT

/-! A message that carries a set its sender has just updated. -/

pmodule SnapSetT

  system s

  type tSnap = (seen : set[Int])
  event eAdd  : Int
  event eSnap : tSnap

  machine Store {
    var seen : set[Int]

    start state Serve {
      on eAdd (v : Int) {
        if v ≥ 0 then do
          seen += (v)
          send this.ref, eSnap, (seen = seen)
      }
      ignore eSnap
    }
  }

  init-holds ∀ (x : Store) (v : Int), ¬ (v ∈ x.seen)

  Lemma members {
    invariant nonneg : ∀ (x : Store) (v : Int), v ∈ x.seen → v ≥ 0
    invariant snap_nonneg : ∀ (e : eSnap) (v : Int), sent e s → v ∈ e.seen → v ≥ 0
  }

  Proof {
    prove members ;
  }

end SnapSetT

#gen_module SnapSetT

set_option pverify.trust "reconstruct" in
#pverify SnapSetT

/-! A message that carries a map its sender has just updated. -/

pmodule SnapMapT

  system s

  type tPut  = (key : Int, val : Int)
  type tSnap = (data : map[Int, Int])
  event ePut  : tPut
  event eSnap : tSnap

  machine Store {
    var data : map[Int, Int]

    start state Serve {
      on ePut (p : tPut) {
        if p.val ≥ 0 then do
          data[p.key] = p.val
          send this.ref, eSnap, (data = data)
      }
      ignore eSnap
    }
  }

  init-holds ∀ (x : Store) (i : Int), ¬ (i ∈ x.data)

  Lemma values {
    invariant nonneg : ∀ (x : Store) (i v : Int), x.data[i]? = some v → v ≥ 0
    invariant snap_nonneg :
      ∀ (e : eSnap) (i v : Int), sent e s → e.data[i]? = some v → v ≥ 0
  }

  Proof {
    prove values ;
  }

end SnapMapT

#gen_module SnapMapT

set_option pverify.trust "reconstruct" in
#pverify SnapMapT
