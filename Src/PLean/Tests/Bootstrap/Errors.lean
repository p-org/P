/-
Error-case regression tests for Phase 0.

Each `/-- error: … -/`-tagged block exercises one expected failure mode of
the surface elaborator. We use Lean's `#guard_msgs` so the build fails if a
diagnostic moves around silently.
-/
import PLean

-- An `event` declared outside any `pmodule` must error.
/--
error: `event` declaration must appear inside a `pmodule … end` block
-/
#guard_msgs in
event eOrphan

-- A handler that references an event the module doesn't declare must be
-- caught by `#pwf` after `#gen_module` runs. The undeclared event surfaces
-- at materialisation time as an unknown identifier — but the `#pwf` check
-- itself catches the missing event independently of elaboration.
pmodule WithGhost
  event eReal

  machine M {
    start state S {
      on eGhost goto S
    }
  }
end WithGhost

#gen_module WithGhost

/--
error: machine `M` state `S` handles undeclared event `eGhost`
---
error: WithGhost: 1 well-formedness error(s)
-/
#guard_msgs in
#pwf WithGhost

-- Spec observes an undeclared event.
pmodule SpecBad
  event eOk

  spec Watcher observes [eOk, eMissing] {
    state S { }
  }
end SpecBad

#gen_module SpecBad

/--
error: spec `Watcher` observes undeclared event `eMissing`
---
error: SpecBad: 1 well-formedness error(s)
-/
#guard_msgs in
#pwf SpecBad
