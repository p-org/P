/-
PLean.Syntax.Containers — P's container types:

  set[T]     ↝ PLean.PSet T
  map[K, V]  ↝ PLean.PMap K V
  seq[T]     ↝ List T
  option[T]  ↝ Option T
-/
import Lean
import PLean.Semantics.Containers

namespace PLean

scoped syntax (name := pSetType) "set[" term "]" : term
scoped syntax (name := pMapType) "map[" term ", " term "]" : term
scoped syntax (name := pSeqType) "seq[" term "]" : term
scoped syntax (name := pOptionType) "option[" term "]" : term

scoped macro_rules
  | `(set[$t]) => `(PLean.PSet $t)
  | `(map[$k, $v]) => `(PLean.PMap $k $v)
  | `(seq[$t]) => `(List $t)
  | `(option[$t]) => `(Option $t)

end PLean
