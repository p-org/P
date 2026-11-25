/-
PLean.Semantics.Containers — P's `set[T]` and `map[K, V]`.

A container value is its update history. Lookups (`PSet.mem`,
`PMap.get`) are structural recursion over the history, so a post-state
lookup unfolds to an `if` over the pre-state lookup. Values are finite by
construction, which makes `support` / `card` (P's `sizeof`, `foreach`,
`choose`) executable and their lemmas unconditional.
-/
import PLean.Verify.SimpAttrs

namespace PLean

/-! ## `PSet` -/

/-- P's `set[T]`. -/
inductive PSet (T : Type) where
  | empty
  | add (s : PSet T) (x : T)
  | del (s : PSet T) (x : T)
  deriving Repr, DecidableEq

instance {T : Type} : Inhabited (PSet T) := ⟨.empty⟩

namespace PSet

variable {T : Type} [DecidableEq T]

/-- Membership. -/
def mem : PSet T → T → Bool
  | .empty,   _ => false
  | .add s x, y => decide (y = x) || s.mem y
  | .del s x, y => !decide (y = x) && s.mem y

/-- The members, without duplicates. -/
def support : PSet T → List T
  | .empty   => []
  | .add s x => if s.mem x then s.support else x :: s.support
  | .del s x => s.support.erase x

/-- P's `sizeof`. -/
def card (s : PSet T) : Nat := s.support.length

@[simp] theorem mem_empty (y : T) : (PSet.empty : PSet T).mem y = false := rfl
@[simp] theorem mem_add (s : PSet T) (x y : T) :
    (s.add x).mem y = (decide (y = x) || s.mem y) := rfl
@[simp] theorem mem_del (s : PSet T) (x y : T) :
    (s.del x).mem y = (!decide (y = x) && s.mem y) := rfl

theorem support_nodup_mem : ∀ s : PSet T,
    s.support.Nodup ∧ ∀ y, y ∈ s.support ↔ s.mem y = true
  | .empty => by simp [support, mem]
  | .add s x => by
    obtain ⟨hnd, hmem⟩ := support_nodup_mem s
    by_cases hx : s.mem x = true
    · refine ⟨by simp [support, hx, hnd], fun y => ?_⟩
      simp only [support, hx, ite_true, hmem, mem_add, Bool.or_eq_true, decide_eq_true_eq]
      constructor
      · intro h; exact Or.inr h
      · rintro (rfl | h) <;> assumption
    · refine ⟨?_, fun y => ?_⟩
      · simp only [support, hx]
        exact List.nodup_cons.mpr ⟨fun h => hx ((hmem x).mp h), hnd⟩
      · simp [support, hx, hmem]
  | .del s x => by
    obtain ⟨hnd, hmem⟩ := support_nodup_mem s
    refine ⟨hnd.erase x, fun y => ?_⟩
    simp only [support, hnd.mem_erase_iff, hmem, mem_del, Bool.and_eq_true, Bool.not_eq_eq_eq_not,
      Bool.not_true, decide_eq_false_iff_not]

theorem nodup_support (s : PSet T) : s.support.Nodup := (support_nodup_mem s).1

theorem mem_support (s : PSet T) (y : T) : y ∈ s.support ↔ s.mem y = true :=
  (support_nodup_mem s).2 y

@[simp] theorem card_empty : (PSet.empty : PSet T).card = 0 := rfl

theorem card_add (s : PSet T) (x : T) :
    (s.add x).card = s.card + (if s.mem x = true then 0 else 1) := by
  by_cases hx : s.mem x = true <;> simp [card, support, hx]

theorem card_del (s : PSet T) (x : T) :
    (s.del x).card = s.card - (if s.mem x = true then 1 else 0) := by
  by_cases hx : s.mem x = true
  · simp [card, support, hx, List.length_erase_of_mem ((mem_support s x).mpr hx)]
  · have : x ∉ s.support := fun h => hx ((mem_support s x).mp h)
    simp [card, support, hx, List.erase_of_not_mem this]

/-- A duplicate-free list contained in another is no longer. -/
theorem length_le_of_nodup_subset :
    ∀ {l m : List T}, l.Nodup → (∀ y, y ∈ l → y ∈ m) → l.length ≤ m.length
  | [], _, _, _ => Nat.zero_le _
  | a :: l, m, hnd, hsub => by
    have ha : a ∈ m := hsub a (List.mem_cons_self ..)
    have ⟨hnotin, hnd'⟩ := List.nodup_cons.mp hnd
    have hsub' : ∀ y, y ∈ l → y ∈ m.erase a := fun y hy => by
      have hne : y ≠ a := fun h => hnotin (h ▸ hy)
      exact (List.mem_erase_of_ne hne).mpr (hsub y (List.mem_cons_of_mem _ hy))
    have := length_le_of_nodup_subset hnd' hsub'
    rw [List.length_erase_of_mem ha] at this
    have : 0 < m.length := List.length_pos_of_mem ha
    simp only [List.length_cons]; omega

theorem card_le_of_sub {s t : PSet T} (h : ∀ y, s.mem y = true → t.mem y = true) :
    s.card ≤ t.card :=
  length_le_of_nodup_subset (nodup_support s) fun y hy =>
    (mem_support t y).mpr (h y ((mem_support s y).mp hy))

/-- Quorum intersection: two subsets of `u` whose sizes sum past `u`'s
share a member. -/
theorem quorum_intersect {s t u : PSet T}
    (hs : ∀ y, s.mem y = true → u.mem y = true) (ht : ∀ y, t.mem y = true → u.mem y = true)
    (hq : s.card + t.card > u.card) : ∃ y, s.mem y = true ∧ t.mem y = true := by
  apply Classical.byContradiction
  intro hdis
  have hdis' : ∀ y, s.mem y = true → t.mem y = false := fun y h1 => by
    cases h2 : t.mem y
    · rfl
    · exact absurd ⟨y, h1, h2⟩ hdis
  have hnd : (s.support ++ t.support).Nodup := by
    refine List.nodup_append.mpr ⟨nodup_support s, nodup_support t, ?_⟩
    intro a ha b hb hab
    subst hab
    have := hdis' a ((mem_support s a).mp ha)
    rw [(mem_support t a).mp hb] at this
    exact Bool.noConfusion this
  have := length_le_of_nodup_subset hnd fun y hy => by
    rcases List.mem_append.mp hy with h | h
    · exact (mem_support u y).mpr (hs y ((mem_support s y).mp h))
    · exact (mem_support u y).mpr (ht y ((mem_support t y).mp h))
  simp only [List.length_append] at this
  unfold card at hq; omega

/-- P's `==` on sets: extensional equality, decided via the supports. -/
def beq (s t : PSet T) : Bool :=
  s.support.all (fun y => t.mem y) && t.support.all (fun y => s.mem y)

end PSet

/-! ## `PMap` -/

/-- P's `map[K, V]`. -/
inductive PMap (K V : Type) where
  | empty
  | put (m : PMap K V) (k : K) (v : V)
  | del (m : PMap K V) (k : K)
  deriving Repr, DecidableEq

instance {K V : Type} : Inhabited (PMap K V) := ⟨.empty⟩

namespace PMap

variable {K V : Type} [DecidableEq K]

/-- Lookup. -/
def get : PMap K V → K → Option V
  | .empty,     _  => none
  | .put m k v, k' => if k' = k then some v else m.get k'
  | .del m k,   k' => if k' = k then none else m.get k'

/-- The key set, as a `PSet` (P's `keys`, and `k in m`). -/
def keySet : PMap K V → PSet K
  | .empty     => .empty
  | .put m k _ => (keySet m).add k
  | .del m k   => (keySet m).del k

/-- P's `k in m`. -/
@[inline] def has (m : PMap K V) (k : K) : Bool := (m.get k).isSome

/-- P's `sizeof` on maps. -/
def card (m : PMap K V) : Nat := m.keySet.card

/-- P's `values(m)`, in key order. -/
def values (m : PMap K V) : List V := m.keySet.support.filterMap m.get

@[simp] theorem get_empty (k : K) : (PMap.empty : PMap K V).get k = none := rfl
@[simp] theorem get_put (m : PMap K V) (k k' : K) (v : V) :
    (m.put k v).get k' = if k' = k then some v else m.get k' := rfl
@[simp] theorem get_del (m : PMap K V) (k k' : K) :
    (m.del k).get k' = if k' = k then none else m.get k' := rfl

theorem mem_keySet : ∀ (m : PMap K V) (k : K), m.keySet.mem k = (m.get k).isSome
  | .empty, _ => rfl
  | .put m k v, k' => by
    simp only [keySet, PSet.mem_add, get_put, mem_keySet m k']
    by_cases h : k' = k <;> simp [h]
  | .del m k, k' => by
    simp only [keySet, PSet.mem_del, get_del, mem_keySet m k']
    by_cases h : k' = k <;> simp [h]

end PMap

/-! ## Surface instances: `∈`, `m[k]?`, `+=`, `-=`, iteration -/

section Instances
variable {T K V : Type} [DecidableEq T] [DecidableEq K]

instance : Membership T (PSet T) := ⟨fun s x => s.mem x = true⟩
instance (s : PSet T) (x : T) : Decidable (x ∈ s) := inferInstanceAs (Decidable (s.mem x = true))
/-- A bounded `∀ x, x ∈ s → P x` ranges over the support. -/
instance PSet.decidableBAll (s : PSet T) (P : T → Prop) [DecidablePred P] :
    Decidable (∀ x, x ∈ s → P x) :=
  decidable_of_iff (∀ x, x ∈ s.support → P x)
    ⟨fun h x hx => h x ((PSet.mem_support s x).mpr hx),
     fun h x hx => h x ((PSet.mem_support s x).mp hx)⟩
instance : Insert T (PSet T) := ⟨fun x s => s.add x⟩
instance : EmptyCollection (PSet T) := ⟨.empty⟩

instance : Membership K (PMap K V) := ⟨fun m k => m.has k = true⟩
instance (m : PMap K V) (k : K) : Decidable (k ∈ m) := inferInstanceAs (Decidable (m.has k = true))
instance : GetElem? (PMap K V) K V (fun m k => k ∈ m) where
  getElem m k h := (m.get k).get (by simpa [Membership.mem, PMap.has, Option.isSome_iff_ne_none] using h)
  getElem? m k := m.get k
instance : EmptyCollection (PMap K V) := ⟨.empty⟩

@[pverifySimp] theorem PSet.mem_iff (s : PSet T) (x : T) : x ∈ s ↔ s.mem x = true := Iff.rfl
@[pverifySimp] theorem PSet.insert_eq (s : PSet T) (x : T) : insert x s = s.add x := rfl
@[pverifySimp] theorem PSet.emptyc_eq : (∅ : PSet T) = .empty := rfl
@[pverifySimp] theorem PMap.mem_iff (m : PMap K V) (k : K) : k ∈ m ↔ (m.get k).isSome = true := Iff.rfl
@[pverifySimp] theorem PMap.getElem?_eq (m : PMap K V) (k : K) : m[k]? = m.get k := rfl
@[pverifySimp] theorem PMap.emptyc_eq : (∅ : PMap K V) = .empty := rfl

attribute [pverifySimp] PSet.mem PMap.get PMap.has

/-! Container lookups through `if` (what a post-state lookup looks like after
`commit`), so they meet the update constructors and unfold. -/
section ite
variable {c : Prop} [Decidable c]
@[pverifySimp] theorem PSet.mem_ite (a b : PSet T) (x : T) :
    (if c then a else b).mem x = if c then a.mem x else b.mem x := by split <;> rfl
@[pverifySimp] theorem PSet.card_ite (a b : PSet T) :
    (if c then a else b).card = if c then a.card else b.card := by split <;> rfl
@[pverifySimp] theorem PMap.get_ite (a b : PMap K V) (k : K) :
    (if c then a else b).get k = if c then a.get k else b.get k := by split <;> rfl
@[pverifySimp] theorem PMap.card_ite (a b : PMap K V) :
    (if c then a else b).card = if c then a.card else b.card := by split <;> rfl
@[pverifySimp] theorem Option.isSome_ite' {α : Type} (a b : Option α) :
    (if c then a else b).isSome = if c then a.isSome else b.isSome := by split <;> rfl
@[pverifySimp] theorem Option.getD_ite' {α : Type} (a b : Option α) (d : α) :
    (if c then a else b).getD d = if c then a.getD d else b.getD d := by split <;> rfl
end ite

attribute [pverifySimp] Option.isSome_some Option.isSome_none Option.getD_some

/-- `c -= (e)`: remove an element (sets) or a key (maps). -/
class PContainerErase (C : Type) (E : outParam Type) where
  erase : C → E → C

instance : PContainerErase (PSet T) T := ⟨PSet.del⟩
instance : PContainerErase (PMap K V) K := ⟨PMap.del⟩

/-- `c += (e)` on sets; `m += (k, v)` on maps (upsert). -/
class PContainerAdd (C : Type) (E : outParam Type) where
  add : C → E → C

instance : PContainerAdd (PSet T) T := ⟨PSet.add⟩
instance : PContainerAdd (PMap K V) (K × V) := ⟨fun m kv => m.put kv.1 kv.2⟩

/-- What `foreach (x in c)` iterates over. -/
class PIterable (C : Type) (T : outParam Type) where
  toList : C → List T

instance : PIterable (List T) T := ⟨id⟩
instance : PIterable (PSet T) T := ⟨PSet.support⟩
instance : PIterable (PMap K V) K := ⟨fun m => m.keySet.support⟩

attribute [pverifySimp] PContainerErase.erase PContainerAdd.add

end Instances

end PLean
