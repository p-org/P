/-
PLean.Semantics.Default — the three sanity invariants every handler
must preserve.

Mirrors PVerifier's `_PInv_Unique_Actions` / `_PInv_Increasing_Action_Count`
/ `_PInv_Received_Subset_Sent` (`Uclid5CodeGenerator.cs:1189-1201`).
These are conjoined with the user's invariants in every per-handler
Hoare triple, both as `requires` and as `ensures`. -/
import PLean.Semantics.GlobalState
import PLean.Semantics.Predicates

namespace PLean

variable {P : ProgramSig}

/-- Every two distinct sent labels have distinct `actionCount`s.

UCLID5 source (`Uclid5CodeGenerator.cs:1191`):
```
forall (a1, a2 : Label) ::
  (a1 ≠ a2 ∧ sent[a1] ∧ sent[a2]) ⇒ a1.actionCount ≠ a2.actionCount
``` -/
def UniqueActions (s : GlobalState P) : Prop :=
  ∀ a b : P.Label, a ≠ b →
    s.sent a = true → s.sent b = true →
    a.actionCount ≠ b.actionCount

/-- Every sent label's `actionCount` is strictly less than the global
counter — i.e., the counter monotonically increases past every label
that has ever been issued.

UCLID5 source (`Uclid5CodeGenerator.cs:1194`):
```
forall (a : Label) :: sent[a] ⇒ a.actionCount < ActionCount
``` -/
def IncreasingCount (s : GlobalState P) : Prop :=
  ∀ a : P.Label, s.sent a = true → a.actionCount < s.actionCount

/-- The received set is a subset of the sent set: a label can only be
delivered if it was first sent.

UCLID5 source (`Uclid5CodeGenerator.cs:1199`):
```
forall (a : Label) :: received[a] ⇒ sent[a]
``` -/
def ReceivedSubsetSent (s : GlobalState P) : Prop :=
  ∀ a : P.Label, s.received a = true → s.sent a = true

/-- Bundle of all three default invariants. PVerifier emits each
separately as its own `_PInv_*` and conjoins them at every handler
boundary; the same shape lets us add them as a single hypothesis to
user-invariant-preservation triples. -/
def DefaultInvariants (s : GlobalState P) : Prop :=
  UniqueActions s ∧ IncreasingCount s ∧ ReceivedSubsetSent s

end PLean
