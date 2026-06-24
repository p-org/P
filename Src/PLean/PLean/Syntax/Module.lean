/-
PLean.Syntax.Module — `pmodule M` command.

  `pmodule M`
    1. error if there's already an open `pmodule` (no nesting in v1)
    2. open `namespace M`
    3. if `pmoduleExt[M]` exists (re-opening across files), restore that
       fragment into `localPModuleCtx`; otherwise initialize fresh
  Lean's builtin `end M` closes the namespace.

We do NOT intercept `end`. Instead, declarations are persisted eagerly via
each `add*` helper in Registry.lean — so even if a file ends mid-block
(or the user forgets `end M`), the partial fragment is durable.

The `localPModuleCtx` cleanup is best-effort: it auto-resets at file
boundaries (since it's an env extension, fresh per-import). To handle
multiple `pmodule M ... end M ... pmodule N ... end N` blocks per file,
the user must call `#endpmodule` between blocks. (We could intercept
`end` to do this automatically, but the fragility isn't worth it for v1
— most P programs are one module per file anyway.)
-/
import Lean
import PLean.Internal.Registry

open Lean Elab Command

namespace PLean

/-- Open or re-open a PLean module. Opens `namespace M` and seeds the
    local fragment from the persistent registry if it exists. -/
syntax (name := pmoduleCmd) "pmodule" ident : command

/-- Explicitly close the local module fragment. Optional; `pmodule M`
    automatically clears any prior local fragment, so well-formed files
    that have one `pmodule … end` block per file never need this. -/
syntax (name := endPModuleCmd) "#endpmodule" : command

@[command_elab pmoduleCmd]
def elabPModule : CommandElab := fun stx => do
  let `(pmodule $name:ident) := stx
    | throwUnsupportedSyntax
  let modName := name.getId
  -- Auto-close any stale local fragment from a prior block. We don't error
  -- on this — Lean's namespace stack is the source of truth for scoping;
  -- the local ctx is just a parser-side flag.
  if (← getLocalPModuleCtx?).isSome then
    clearLocalPModuleCtx
  -- Open the Lean namespace.
  elabCommand (← `(namespace $name))
  -- Seed the local fragment from the persistent registry, or fresh.
  let ctx ← match ← getPModule? modName with
    | some prior => pure prior
    | none       => pure { name := modName : LocalPModuleCtx }
  setLocalPModuleCtx ctx
  -- Eagerly persist (idempotent if it already existed).
  setPModule ctx

@[command_elab endPModuleCmd]
def elabEndPModule : CommandElab := fun _ => do
  clearLocalPModuleCtx

end PLean
