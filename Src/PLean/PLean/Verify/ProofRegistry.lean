/-
PLean.Verify.ProofRegistry — `@[pverifyProof]` attribute and lookup.

A theorem tagged `@[pverifyProof]` registers itself as the manual
proof for the obligation whose generated name matches. When
`#pverify` walks obligations it consults this registry first and
skips auto-emission for any name already covered.

The registry is keyed on theorem name (not goal shape): the
obligation generator owns the name, and the user copies it verbatim
from `#pverify`'s failure report. Theorem-name keying keeps the
registration cheap to verify and easy to debug.
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
