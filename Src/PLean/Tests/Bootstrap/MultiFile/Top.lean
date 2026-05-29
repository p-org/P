/-
Multi-file aggregation test — top file.
Imports Events + Machine, finalises, then runs `#pwf`.
-/
import Tests.Bootstrap.MultiFile.Events
import Tests.Bootstrap.MultiFile.Machine

#gen_module MultiTest

/--
info: MultiTest: well-formed (0 types, 2 events, 1 machines, 0 invariants, 0 axioms, 0 instances)
-/
#guard_msgs in
#pwf MultiTest
