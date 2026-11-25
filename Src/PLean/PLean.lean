-- PLean: P state machines in Lean 4, verified over a Verdi-style
-- transition semantics with Lean core's `vcgen` and SMT via lean-crush.
--
-- Top-level facade: `import PLean` brings in the whole public surface.
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Semantics.Log
import PLean.Semantics.Containers
import PLean.Semantics.Core
import PLean.Semantics.Handler
import PLean.Semantics.World
import PLean.Semantics.Theorems
import PLean.Semantics.Msg
import PLean.Syntax.Containers
import PLean.Syntax.Loop
import PLean.Syntax.Module
import PLean.Syntax.Types
import PLean.Syntax.Events
import PLean.Syntax.Machine
import PLean.Syntax.Stmt
import PLean.Syntax.Verify
import PLean.Verify.Norm
import PLean.Verify.Tactic
import PLean.Commands.GenModule
import PLean.Commands.PWf
import PLean.Commands.PVerify
import PLean.Commands.PrintModule
