/-
PLean.Semantics.Log — an append-only log: a total lookup function plus a
size counter. Handlers append their sends, spawned machines and
announcements to logs; `commit` reads them back by index.
-/

namespace PLean

/-- An append-only log. A log may start at an offset: a handler's outbox
starts at the world's packet clock, so each entry sits at its final id.
`size` is the next free index. -/
structure Log (α : Type) where
  get  : Nat → Option α
  size : Nat

namespace Log

variable {α : Type}

/-- The empty log starting at index `start`. -/
@[inline] def startingAt (start : Nat) : Log α := ⟨fun _ => none, start⟩

/-- The empty log. -/
@[inline] def empty : Log α := startingAt 0

/-- Append one entry. -/
@[inline] def push (l : Log α) (a : α) : Log α :=
  ⟨fun i => if i = l.size then some a else l.get i, l.size + 1⟩

instance : Inhabited (Log α) := ⟨empty⟩

@[simp] theorem startingAt_size (n : Nat) : (startingAt n : Log α).size = n := rfl
@[simp] theorem startingAt_get (n i : Nat) : (startingAt n : Log α).get i = none := rfl
@[simp] theorem empty_size : (empty : Log α).size = 0 := rfl
@[simp] theorem empty_get (i : Nat) : (empty : Log α).get i = none := rfl
@[simp] theorem push_size (l : Log α) (a : α) : (l.push a).size = l.size + 1 := rfl
@[simp] theorem push_get (l : Log α) (a : α) (i : Nat) :
    (l.push a).get i = if i = l.size then some a else l.get i := rfl

/-- The entries of a log, in order (for execution and printing). -/
def toList (l : Log α) : List α :=
  (List.range l.size).filterMap l.get

end Log

end PLean
