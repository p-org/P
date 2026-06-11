/-
PLean.Verify.ProofRegistry — `@[pverifyProof]` attribute and lookup.

Veil's analogue: `@[invProof]` registers a theorem proving an
`(action, invariant)` Hoare triple in a global table; `#check_invariants`
walks the table and skips the SMT call for any obligation already
covered by a registered theorem.

PLean's analogue: `@[pverifyProof]` registers a theorem whose name
matches the obligation generator's emitted name
`<Mod>.<M>.<S>.<ev>_correct_<X>` (see
[`Verify/Obligation.lean::emitOneObligation`]). When `#pverify`
walks obligations, it consults this registry first; if a registered
theorem exists for the obligation name, the SMT call is skipped and
the user's manual proof is used.

We intentionally use **theorem-name matching** rather than goal-shape
matching (Veil's `DiscrTree`-based approach) because the obligation
generator owns the name; the registry just records "the user wrote
this theorem name with `@[pverifyProof]`, so don't emit the auto
version."

This is simpler, deterministic, and easy to debug — the user reads
the failure message ("obligation X failed via SMT"), copies X verbatim
into their `@[pverifyProof] theorem X : ...` skeleton, and re-runs
`#pverify`.

## Usage

```lean
-- Run #pverify, see "DistributedLock.Node.Act.eGrant_correct_Safety_safety failed":
-- copy the printed skeleton into the file:
@[pverifyProof]
theorem DistributedLock.Node.Act.eGrant_correct_Safety_safety
    (this : Node) (param : eGrant_payload) :
    triple ... := by
  pverify_open_triple
  pverify_step_wp
  intro s hpre
  -- ... user finishes the proof manually ...
  pverify_smt_close

-- Re-running #pverify picks the proof up.
```
-/
import Lean

open Lean Elab

namespace PLean

/-- Set of theorem names a user has registered as a manual proof of
some obligation. When the obligation generator considers
`<Mod>.<M>.<S>.<ev>_correct_<X>` for emission, it consults this set:
if the name is present, the obligation is treated as user-proved and
skipped in the auto-discharge pass.

Stored as a persistent env extension so the registration survives
across files. -/
abbrev PVerifyProofMap := Std.HashSet Name

initialize pverifyProofExt :
    SimplePersistentEnvExtension Name PVerifyProofMap ←
  registerSimplePersistentEnvExtension {
    name := `PLean.pverifyProofExt
    addEntryFn := fun s n => s.insert n
    addImportedFn := fun arrs => Id.run do
      let mut s : PVerifyProofMap := {}
      for arr in arrs do
        for n in arr do
          s := s.insert n
      return s
  }

/-- Register a name in the proof map (called by the attribute below). -/
def addPVerifyProof (name : Name) : CoreM Unit := do
  modifyEnv fun env => pverifyProofExt.addEntry env name

/-- Look up whether `name` has a registered manual proof. -/
def hasPVerifyProof (name : Name) : CoreM Bool := do
  return (pverifyProofExt.getState (← getEnv)).contains name

/-! ## The `@[pverifyProof]` attribute. -/

initialize registerBuiltinAttribute {
  name := `pverifyProof
  descr := "Register the tagged theorem as a manual proof for an obligation \
            that `#pverify` would otherwise auto-discharge via SMT. The \
            theorem's name must match the obligation generator's emitted \
            name `<Mod>.<M>.<S>.<ev>_correct_<X>`."
  add := fun declName _ _ => addPVerifyProof declName
}

end PLean
