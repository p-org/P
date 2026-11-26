/-
The proof cache: a goal a solver closed is reused for a goal of the same
shape (here, the second handler's, which do the same), and the run leaves
proofs of the obligations and of the final theorems, and goal lemmas, cached
for the next one. The test starts from an empty cache.
-/
import PLean

open PLean

#eval show IO Unit from do
  try IO.FS.removeFile (PLean.Cache.fileOf `CacheT) catch _ => pure ()

pmodule CacheT
  system s
  event eUp
  event eAlso
  machine Node {
    var count : Int
    start state Run {
      on eUp {
        count = count + 1
      }
      on eAlso {
        count = count + 1
      }
    }
  }
  init-holds ∀ n : Node, n.count = 0
  Theorem safety {
    invariant nonneg : ∀ n : Node, n.count ≥ 0
  }
  Proof {
    prove safety ;
  }
end CacheT

#gen_module CacheT

/--
info: #pverify CacheT: 11 obligations, 11 proved (0 trusting the solver), 0 failed
  proved by normalisation or a hypothesis: 3, by crush before SMT: 0, with an SMT solver: 5, from the cache: 3
  ✓ Node.Run.eUp_correct_safety [smt 1]
  ✓ Node.Run.eAlso_correct_safety [cache 1]
  ✓ Node.Run.entry_correct_safety [smt 1]
  ✓ base_safety [smt 1]
  ✓ Node.Run.eUp_correct_NoHalt [smt 1]
  ✓ Node.Run.eAlso_correct_NoHalt [cache 1]
  ✓ Node.Run.entry_correct_NoHalt [cache 1]
  ✓ base_NoHalt [smt 1]
  ✓ Node.Run.eUp_safe [norm]
  ✓ Node.Run.eAlso_safe [norm]
  ✓ Node.Run.entry_safe [norm]
-/
#guard_msgs in
set_option pverify.cache true in
set_option pverify.showTimes false in
#pverify CacheT

/-- info: CacheT: 18 obligation proofs and 5 goal lemmas cached -/
#guard_msgs in
#pverify_cache CacheT
