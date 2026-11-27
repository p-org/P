/-
The proof cache: a goal a solver closed is reused for a goal of the same
shape (here, the second handler's, which do the same), and the run leaves
proofs of the obligations and of the final theorems, and goal lemmas, cached
for the next one, which replays them all. A cached proof that is wrong, uses
an axiom the module does not declare (or one the cache itself would bring in),
or trusts the solver under `pverify.trust reconstruct` is not reused: the
obligation or goal is proved afresh. The test starts from an empty cache and
proves one obligation at a time, so that goals are reused in a fixed order.
-/
import PLean

open PLean Lean

/-- Run a command, then drop the declarations it added, so that `#pverify` can
run again. -/
elab "#again " c:command : command => do
  let env ← getEnv
  Elab.Command.elabCommand c
  setEnv env

/-- Replace the proof of each cached entry of `CacheT` whose name starts with
`_pvcache.<k>` for a `k` in `kinds` by `f i type` (`i` counts them). -/
def forge (kinds : List Name) (f : Nat → Expr → Expr) : IO Unit :=
  PLean.Cache.stores.modify fun m => m.modify `CacheT fun st =>
    let (consts, _) := st.consts.foldl (init := (st.consts, 0)) fun (acc, i) n ci =>
      match ci with
      | .thmInfo v =>
        if kinds.any (fun k => (`_pvcache ++ k).isPrefixOf n) then
          (acc.insert n (.thmInfo { v with value := f i v.type }), i + 1)
        else (acc, i)
      | _ => (acc, i)
    { st with consts }

/-- A proof of `t` from axiom `ax : False`. -/
def fromFalse (ax : Name) (t : Expr) : Expr := mkApp2 (mkConst ``False.elim [.zero]) t (mkConst ax)

/-- A proof of `t` on the solver's word. -/
def trusting (t : Expr) : Expr :=
  mkApp3 (mkConst ``Crush.crushSorryWith) t (mkConst ``True) (mkConst ``True.intro)

/-- An axiom `CacheT` does not declare. -/
axiom CacheT.cheat : False

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
set_option pverify.jobs 1 in
set_option pverify.showTimes false in
#again #pverify CacheT

/-- info: CacheT: 18 obligation proofs and 5 goal lemmas cached -/
#guard_msgs in
#pverify_cache CacheT

-- the next run replays every proof
/--
info: #pverify CacheT: 11 obligations, 11 proved (0 trusting the solver), 0 failed
  proved by normalisation or a hypothesis: 0, by crush before SMT: 0, with an SMT solver: 0, from the cache: 11
  ✓ Node.Run.eUp_correct_safety [cached]
  ✓ Node.Run.eAlso_correct_safety [cached]
  ✓ Node.Run.entry_correct_safety [cached]
  ✓ base_safety [cached]
  ✓ Node.Run.eUp_correct_NoHalt [cached]
  ✓ Node.Run.eAlso_correct_NoHalt [cached]
  ✓ Node.Run.entry_correct_NoHalt [cached]
  ✓ base_NoHalt [cached]
  ✓ Node.Run.eUp_safe [cached]
  ✓ Node.Run.eAlso_safe [cached]
  ✓ Node.Run.entry_safe [cached]
-/
#guard_msgs in
set_option pverify.cache true in
set_option pverify.jobs 1 in
set_option pverify.showTimes false in
#again #pverify CacheT

-- Proofs on the solver's word are reused when `pverify.trust` allows it…
#eval forge [`ob, `goal] fun _ t => trusting t

/--
info: #pverify CacheT: 11 obligations, 11 proved (11 trusting the solver), 0 failed
  proved by normalisation or a hypothesis: 0, by crush before SMT: 0, with an SMT solver: 0, from the cache: 11
  ✓ (trusted) Node.Run.eUp_correct_safety [cached]
  ✓ (trusted) Node.Run.eAlso_correct_safety [cached]
  ✓ (trusted) Node.Run.entry_correct_safety [cached]
  ✓ (trusted) base_safety [cached]
  ✓ (trusted) Node.Run.eUp_correct_NoHalt [cached]
  ✓ (trusted) Node.Run.eAlso_correct_NoHalt [cached]
  ✓ (trusted) Node.Run.entry_correct_NoHalt [cached]
  ✓ (trusted) base_NoHalt [cached]
  ✓ (trusted) Node.Run.eUp_safe [cached]
  ✓ (trusted) Node.Run.eAlso_safe [cached]
  ✓ (trusted) Node.Run.entry_safe [cached]
  CacheT.safe assumes: the solver's answers in the trusted obligations (`Crush.crushSorry`)
-/
#guard_msgs in
set_option pverify.cache true in
set_option pverify.jobs 1 in
set_option pverify.showTimes false in
#again #pverify CacheT

-- …and not under `reconstruct`: everything is proved afresh
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
set_option pverify.jobs 1 in
set_option pverify.trust "reconstruct" in
set_option pverify.showTimes false in
#again #pverify CacheT

-- Wrong obligation proofs: from an axiom the module does not declare, from one
-- the cache would add, ill-typed. None is replayed; the goals are reused.
#eval PLean.Cache.insert `CacheT
  (.axiomInfo { name := `_pvcache.forged, levelParams := [], «type» := mkConst ``False, isUnsafe := false })
#eval forge [`ob] fun i t =>
  match i % 3 with
  | 0 => fromFalse `CacheT.cheat t
  | 1 => fromFalse `_pvcache.forged t
  | _ => mkConst ``True.intro

/--
info: #pverify CacheT: 11 obligations, 11 proved (0 trusting the solver), 0 failed
  proved by normalisation or a hypothesis: 3, by crush before SMT: 0, with an SMT solver: 0, from the cache: 8
  ✓ Node.Run.eUp_correct_safety [cache 1]
  ✓ Node.Run.eAlso_correct_safety [cache 1]
  ✓ Node.Run.entry_correct_safety [cache 1]
  ✓ base_safety [cache 1]
  ✓ Node.Run.eUp_correct_NoHalt [cache 1]
  ✓ Node.Run.eAlso_correct_NoHalt [cache 1]
  ✓ Node.Run.entry_correct_NoHalt [cache 1]
  ✓ base_NoHalt [cache 1]
  ✓ Node.Run.eUp_safe [norm]
  ✓ Node.Run.eAlso_safe [norm]
  ✓ Node.Run.entry_safe [norm]
-/
#guard_msgs in
set_option pverify.cache true in
set_option pverify.jobs 1 in
set_option pverify.showTimes false in
#again #pverify CacheT

-- Goal lemmas from such axioms are not reused either.
#eval forge [`ob, `goal] fun i t => fromFalse (if i % 2 == 0 then `CacheT.cheat else `_pvcache.forged) t

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
set_option pverify.jobs 1 in
set_option pverify.showTimes false in
#again #pverify CacheT
