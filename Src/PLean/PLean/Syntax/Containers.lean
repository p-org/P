/-
PLean.Syntax.Containers — type-level sugar for P's container types.

Four term-level macros parse P's container syntax:
  `set[T]`     ↝ `Set T`              (Mathlib)
  `map[K, V]`  ↝ `K → Option V`        (PVerifier's encoding)
  `seq[T]`     ↝ `List T`              (core; no SMT support)
  `option[T]`  ↝ `Option T`            (core inductive)

The macros fire in `term` position so they work inside `var` types,
type aliases, and payload-field annotations. Element / key / value
slots are arbitrary `term`s; nesting (`set[map[K, option[Machine]]]`)
falls out of normal Lean term parsing.

`default(set[T])` reads as the Mathlib empty set through Lean's
`Inhabited` resolution (`Set α` has an `Inhabited` instance giving
`∅`). `default(map[K, V])` reads as the always-`none` function.
`default(option[T])` is `Option.none`.
-/
import Lean
import PLean.Semantics.Containers

namespace PLean

/-- `set[T]` term — `Set T`. -/
scoped syntax (name := pSetType) "set[" term "]" : term

/-- `map[K, V]` term — `K → Option V`. -/
scoped syntax (name := pMapType) "map[" term ", " term "]" : term

/-- `seq[T]` term — `List T`. No SMT support. -/
scoped syntax (name := pSeqType) "seq[" term "]" : term

/-- `option[T]` term — `Option T`. -/
scoped syntax (name := pOptionType) "option[" term "]" : term

-- WHY the map form emits the bare arrow `$k → Option $v` rather than
-- the named `PLean.PMap $k $v`: lean-auto's monomorphizer reads the
-- projection-function type out of the environment (not the goal),
-- and even an `@[reducible] def` for `PMap` survives there as a head
-- constant, tripping "`PMap K V` is not a `∀`". Emitting the arrow
-- directly bypasses the issue — `PMap` remains available as a
-- public alias for users who want to talk about the encoding by name.
scoped macro_rules
  | `(set[$t:term])          => `(Set $t)
  | `(map[$k:term, $v:term]) => `($k → Option $v)
  | `(seq[$t:term])          => `(List $t)
  | `(option[$t:term])       => `(Option $t)

end PLean
