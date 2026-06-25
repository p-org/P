/-
PLean.Semantics.Containers — SMT-prep tags + map helpers for P's
container types.

Surface macros (`Syntax/Containers.lean`) desugar to bare Lean /
Mathlib types:

  set[T]     ↝ Set T                -- Mathlib (α → Prop)
  seq[T]     ↝ List T               -- core; no SMT support
  map[K, V]  ↝ PMap K V := K → Option V
  option[T]  ↝ Option T             -- core inductive

`PMap` matches PVerifier's UCLID5 `[K]Option V` array layout. `Set α`
is `α → Prop`; lean-auto handles function-typed atoms when *applied*,
so the SMT prep recipe unfolds `Membership.mem` / `Set.insert` /
`Set.diff` via the `pverifySimp` set before the solver runs, leaving
the goal as boolean combinations of `s x : Prop` atoms.

`List α` mutates fine but isn't SMT-decidable in general; verification
of seq-mutating handlers is out of scope.

Custom ADTs (user `type N = (...)` named tuples, `enum N`) round-trip
too: lean-auto's inductive translator accepts any non-typeclass
inductive whose constructors are simple (parameter args, no dependent
args). The `type` / `enum` materialisers already emit `deriving
Inhabited, DecidableEq`, satisfying the prerequisites.
-/
import Mathlib.Data.Set.Basic
import Mathlib.Data.Set.Insert
import PLean.Verify.SimpAttrs

namespace PLean

variable {K V : Type}

/-- `PMap K V` — the encoding of P's `map[K, V]`. A total function
into `Option V`, mirroring PVerifier's UCLID5 `[K]Option V` array.
`@[reducible] def` (not `abbrev`) so the symbol survives as the head
in saved projection types — `abbrev` here would fully unfold at
elaboration time and lean-auto's monomorphizer rejects the resulting
`K → Option V` projection signature with "`PMap K V is not a ∀`". -/
@[reducible] def PMap (K V : Type) : Type := K → Option V

/-- The empty map. Equivalent to `default` under the `Pi`-`Option`
`Inhabited` instance. Named for readability in error messages /
counter-examples. -/
@[reducible] def mapEmpty : PMap K V := fun _ => Option.none

/-- `mapInsert m k v` — set `m[k] := v`. -/
@[reducible] def mapInsert [DecidableEq K]
    (m : PMap K V) (k : K) (v : V) : PMap K V :=
  fun x => if x = k then Option.some v else m x

/-- `mapErase m k` — drop key `k`. -/
@[reducible] def mapErase [DecidableEq K]
    (m : PMap K V) (k : K) : PMap K V :=
  fun x => if x = k then Option.none else m x

/-- `mapModify m k f` — apply `f` to the value at `k` if present.
Used by `m[k] += (e)` / `m[k] -= (e)` to update a nested container. -/
@[reducible] def mapModify [DecidableEq K]
    (m : PMap K V) (k : K) (f : V → V) : PMap K V :=
  fun x => if x = k then (m x).map f else m x

/-- `k ∈ m` for `m : PMap K V` reads as "`k` has an entry" — i.e. the
underlying `m k` is `some _`. Mirrors P's `k in m` semantics. -/
instance : Membership K (PMap K V) where
  mem m k := ∃ v, m k = Option.some v

/-- `k ∈ m` reduces to `m k = some _`. Tagged into `pverifySimp` so
SMT prep rewrites the membership atom to a constructor equality
SMT decides directly. -/
@[pverifySimp, simp] theorem mem_pmap_iff (m : PMap K V) (k : K) :
    k ∈ m ↔ ∃ v, m k = Option.some v := Iff.rfl

/-- `k ∈ m` is decidable: inspect the `Option`. Lets the user write
`if h : k ∈ m then …` in handler bodies. -/
instance [DecidableEq K] (m : PMap K V) (k : K) :
    Decidable (k ∈ m) :=
  match h : m k with
  | .some v => isTrue ⟨v, h⟩
  | .none   => isFalse (fun ⟨_, hv⟩ => by rw [h] at hv; exact Option.noConfusion hv)

/-! ## Removal — one operator over `Set` and `PMap`

`s -= (e)` in a handler body expands to `PLean.containerErase s e`.
The typeclass dispatches to `Set` set-difference for `Set T`, to
`mapErase` for `PMap K V`. PVerifier's surface uses the same `-=`
form for both kinds, so we keep parity. -/

class PContainerErase (C : Type) (E : outParam Type) where
  erase : C → E → C

instance {T : Type} [DecidableEq T] : PContainerErase (Set T) T where
  erase s e := s \ Set.singleton e

instance {K V : Type} [DecidableEq K] : PContainerErase (PMap K V) K where
  erase m k := mapErase m k

@[reducible] def containerErase {C E : Type} [PContainerErase C E]
    (c : C) (e : E) : C := PContainerErase.erase c e

attribute [pverifySimp]
  PLean.containerErase
  PLean.PContainerErase.erase

/-! ## Lookup-after-mutation lemmas

These spell out how `mapInsert` / `mapErase` / `mapModify` interact
with map lookup. They tag into `pverifySimp` so SMT prep rewrites a
post-state lookup directly to the underlying value without case-
splitting on `decide (k' = k)`.

Sound: each unfolds the helper's body and simplifies the conditional;
there is no information loss.
-/

/-- `mapInsert m k v` at the inserted key yields `some v`. -/
@[pverifySimp, simp] theorem mapInsert_eq [DecidableEq K]
    (m : PMap K V) (k : K) (v : V) :
    mapInsert m k v k = some v := by
  unfold mapInsert; simp

/-- `mapInsert m k v` at any other key is unchanged. -/
@[pverifySimp, simp] theorem mapInsert_ne [DecidableEq K]
    (m : PMap K V) (k k' : K) (v : V) (h : k' ≠ k) :
    mapInsert m k v k' = m k' := by
  unfold mapInsert; simp [h]

/-- `mapErase m k` at the erased key yields `none`. -/
@[pverifySimp, simp] theorem mapErase_eq [DecidableEq K]
    (m : PMap K V) (k : K) :
    mapErase m k k = none := by
  unfold mapErase; simp

/-- `mapErase m k` at any other key is unchanged. -/
@[pverifySimp, simp] theorem mapErase_ne [DecidableEq K]
    (m : PMap K V) (k k' : K) (h : k' ≠ k) :
    mapErase m k k' = m k' := by
  unfold mapErase; simp [h]

/-- `mapModify m k f` at the modified key threads `f` through the
`Option`: present values become `some (f v)`, absent values stay
absent. -/
@[pverifySimp, simp] theorem mapModify_eq [DecidableEq K]
    (m : PMap K V) (k : K) (f : V → V) :
    mapModify m k f k = (m k).map f := by
  unfold mapModify; simp

/-- `mapModify m k f` at any other key is unchanged. -/
@[pverifySimp, simp] theorem mapModify_ne [DecidableEq K]
    (m : PMap K V) (k k' : K) (f : V → V) (h : k' ≠ k) :
    mapModify m k f k' = m k' := by
  unfold mapModify; simp [h]

/-- `mapEmpty` is `none` everywhere. -/
@[pverifySimp, simp] theorem mapEmpty_apply (k : K) :
    (mapEmpty : PMap K V) k = none := rfl

/-- `mapInsert` is idempotent in the value: re-inserting at the same
key replaces the prior value. -/
@[pverifySimp, simp] theorem mapInsert_mapInsert [DecidableEq K]
    (m : PMap K V) (k : K) (v v' : V) :
    mapInsert (mapInsert m k v) k v' = mapInsert m k v' := by
  funext x
  unfold mapInsert
  by_cases hx : x = k <;> simp [hx]

/-- `mapErase` after `mapInsert` at the same key reverts to no entry. -/
@[pverifySimp, simp] theorem mapErase_mapInsert [DecidableEq K]
    (m : PMap K V) (k : K) (v : V) :
    mapErase (mapInsert m k v) k = mapErase m k := by
  funext x
  unfold mapErase mapInsert
  by_cases hx : x = k <;> simp [hx]

/-! ## `pverifySimp` tags for SMT prep

After `simp only [pverifySimp]`, every container atom appears as a
pure FOL combination of applied uninterpreted symbols and concrete
constructors — the shape lean-auto's monomorphizer accepts.

- `Set` ops unfold via Mathlib's `mem_insert_iff` / `mem_diff` /
  `mem_singleton_iff` / `mem_empty_iff_false` / `mem_union` /
  `mem_inter_iff` / `mem_univ`.
- `Option`'s pattern-match reduces by the standard simp set already.
- `mapInsert` / `mapErase` / `mapModify` reduce to their `if`-lambda
  bodies, then `simp` lifts the `if` through application.
-/

attribute [pverifySimp]
  Set.mem_insert_iff
  Set.mem_singleton_iff
  Set.mem_diff
  Set.mem_union
  Set.mem_inter_iff
  Set.mem_empty_iff_false
  Set.mem_univ
  PLean.PMap
  PLean.mapEmpty
  PLean.mapInsert
  PLean.mapErase
  PLean.mapModify

end PLean
