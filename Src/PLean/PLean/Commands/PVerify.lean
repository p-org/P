/-
PLean.Commands.PVerify — `#pverify M`.

Phase 0: this command is a thin wrapper over `#pwf`. Phase 3 will extend it
to also generate Hoare-triple obligations and dispatch to `loom_solve`.

Until then, the user-facing semantics is "check everything you can check
right now," which means structural well-formedness only.
-/
import Lean
import PLean.Commands.PWf

open Lean Elab Command

namespace PLean

syntax (name := pverifyCmd) "#pverify " ident : command

@[command_elab pverifyCmd]
def elabPVerify : CommandElab := fun stx => do
  let `(#pverify $name:ident) := stx
    | throwUnsupportedSyntax
  -- Phase 0: equivalent to `#pwf`. Forward and let it produce the message.
  elabCommand (← `(#pwf $name))

end PLean
