-- PLean: a port of the P language and PVerifier into Lean 4, using Loom
-- as the verification backend.
--
-- Top-level facade. Re-exports the public surface so users only need
-- `import PLean`. See docs/PLAN.md and docs/PLAN_P0.md for the design.
import PLean.Internal.Decls
import PLean.Internal.Registry
import PLean.Semantics.Label
import PLean.Semantics.GlobalState
import PLean.Semantics.Monad
import PLean.Semantics.Primitives
import PLean.Semantics.Predicates
import PLean.Semantics.Default
import PLean.Surface.Module
import PLean.Surface.Types
import PLean.Surface.Events
import PLean.Surface.Machine
import PLean.Surface.Stmt
import PLean.Surface.Verify
import PLean.Surface.Notation
import PLean.Verify.Tactic
import PLean.Verify.ProofRegistry
import PLean.Verify.Obligation
import PLean.Commands.GenModule
import PLean.Commands.PWf
import PLean.Commands.PVerify
import PLean.Commands.PrintModule
