/-
PLean.Semantics.Default — the three sanity invariants every handler
must preserve.

`UniqueActions` / `IncreasingCount` / `ReceivedSubsetSent` are
well-formedness facts about the global buffer: distinct sent labels
have distinct counters, every sent label's counter is below the global
counter, and the received set is contained in the sent set. `#pverify`
discharges them on every handler under the synthetic `prove default`
directive (in addition to any user-written invariants).
-/
import PLean.Semantics.GlobalState
import PLean.Semantics.Predicates

namespace PLean

variable {P : ProgramSig}

/-- Every two distinct sent labels have distinct `actionCount`s. -/
def UniqueActions (s : GlobalState P) : Prop :=
  ∀ a b : P.Label, a ≠ b →
    s.sent a = true → s.sent b = true →
    a.actionCount ≠ b.actionCount

/-- Every sent label's `actionCount` is strictly less than the global
counter — i.e., the counter monotonically increases past every label
that has ever been issued. -/
def IncreasingCount (s : GlobalState P) : Prop :=
  ∀ a : P.Label, s.sent a = true → a.actionCount < s.actionCount

/-- The received set is a subset of the sent set: a label can only be
delivered if it was first sent. -/
def ReceivedSubsetSent (s : GlobalState P) : Prop :=
  ∀ a : P.Label, s.received a = true → s.sent a = true

/-- Bundle of all three default invariants. Bundled so they can be
added as a single hypothesis to user-invariant-preservation triples;
each conjunct is still checked individually by a separate base-case
VC under `prove default`. -/
def DefaultInvariants (s : GlobalState P) : Prop :=
  UniqueActions s ∧ IncreasingCount s ∧ ReceivedSubsetSent s

end PLean
