-- PLean: P state machines in Lean 4, verified over a Verdi-style
-- transition semantics with Lean core's `vcgen` and SMT via lean-crush.
--
-- Top-level facade: `import PLean` brings in the whole public surface.
import PLean.Semantics.Log
import PLean.Semantics.Containers
import PLean.Semantics.Core
import PLean.Semantics.Handler
import PLean.Semantics.World
import PLean.Semantics.Theorems
import PLean.Verify.Norm
