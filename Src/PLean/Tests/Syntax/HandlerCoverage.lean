/-
Soundness regressions for VC completeness — every executable handler
shape must produce a Hoare-triple obligation. Pre-fix gaps:

- **S.1.3** `on ev goto tgt` was in `PStateDecl.handles` but the
  obligation generator skipped it because no `_handler` def existed for
  goto-only clauses. The goto's `currentState` / `sent` / `actionCount`
  mutations went unverified.

- **S.1.1** `entry { … }` and `entry (param : T) { … }` blocks were
  silently dropped at registration: they never reached `handles` at all,
  so the loop never emitted any obligation. A user invariant broken by
  the entry's `send` or `var =` writes was reported as proved.

Both fixes synthesise the missing obligations. This file pins the
emitted theorem names so a regression that re-introduces either skip
makes the `#check`-by-name fail with `Unknown constant`.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 8

/-! ## Probe 1 — `on ev goto tgt` gets a per-handler obligation. -/

pmodule HandlerCoverageGoto

  event eGo

  machine M {
    start state A {
      on eGo goto B
    }
    state B { }
  }

  Theorem trivial {
    invariant tt : True
  }

  Proof { prove trivial ; }

end HandlerCoverageGoto

#gen_module HandlerCoverageGoto

set_option pverify.autoProveDefault true in
#pverify HandlerCoverageGoto

namespace HandlerCoverageGoto
-- The goto-only handler now has its own theorem. Before the fix the
-- constant was missing and this `#check` errored with `Unknown
-- constant`. With `pverify.autoProveDefault := true` the synthetic
-- `prove default ;` covers it too.
#check @M.A.eGo_correct_block0_trivial
#check @M.A.eGo_correct_auto_default_default
end HandlerCoverageGoto

/-! ## Probe 2 — `entry { … }` gets a per-handler obligation.

The entry handler runs on transition INTO the state. The obligation
shape skips the dispatcher contract (no `lbl`, no event tag, no
`markReceived` prelude) and instead just requires the machine is in
the state — the conservative pre that suffices for the inductive step. -/

pmodule HandlerCoverageEntry

  event eGo

  machine M {
    var x : Nat
    start state Boot {
      entry {
        x = 1
      }
      on eGo { pure () }
    }
  }

  Theorem trivial {
    invariant tt : True
  }

  Proof { prove trivial ; }

end HandlerCoverageEntry

#gen_module HandlerCoverageEntry

set_option pverify.autoProveDefault true in
#pverify HandlerCoverageEntry

namespace HandlerCoverageEntry
-- Entry obligation under the synthetic event tag `entry`. The user-
-- directive pass emits one for the explicit `prove trivial`; with
-- `pverify.autoProveDefault := true`, the synthetic auto-default pass
-- emits a `prove default` for the same handler.
#check @M.Boot.entry_correct_block0_trivial
#check @M.Boot.entry_correct_auto_default_default
end HandlerCoverageEntry

/-! ## Probe 3 — `entry (param : T) { … }` carries the payload binder. -/

pmodule HandlerCoverageEntryTyped

  event eGo

  type InitArgs = (seed : Nat)

  machine M {
    var x : Nat
    start state Boot {
      entry (input : InitArgs) {
        x = input.seed
      }
      on eGo { pure () }
    }
  }

  Theorem trivial {
    invariant tt : True
  }

  Proof { prove trivial ; }

end HandlerCoverageEntryTyped

#gen_module HandlerCoverageEntryTyped
#pverify HandlerCoverageEntryTyped

namespace HandlerCoverageEntryTyped
-- The typed entry's obligation has the payload binder in scope so the
-- generator's signature matches the entry def `M.Boot.entry (this) (input)`.
#check @M.Boot.entry_correct_block0_trivial
end HandlerCoverageEntryTyped

/-! ## Probe 4 — a `goto`-broken invariant is NOT silently passed.

`always_in_A` is true at init (every machine starts in `A_st`) but is
violated by `goto B`. Pre-fix, no inductive-step VC was emitted, so the
broken invariant was reported as proved. Post-fix the (M, A, eGo)
obligation exists and fails to discharge (the SMT chain returns
no-diagnostic — the invariant is genuinely false and there's no manual
proof).

The pin is `3 obligations from 1 prove-directives` (base + goto user +
goto auto-default) plus a non-zero failure count. Pre-fix the count was
1 (base only). -/

pmodule HandlerCoverageGotoBroken
  system s
  event eGo
  machine M {
    start state A {
      on eGo goto B
    }
    state B { }
  }
  Theorem stays_in_A {

    invariant always_in_A :
      ∀ m : M, (s.machines m.ref).currentState = M.A_st
  
  }
  Proof { prove stays_in_A ; }
end HandlerCoverageGotoBroken

#gen_module HandlerCoverageGotoBroken

-- With `autoProveDefault := true` (below), three obligations: base
-- (disproved — `always_in_A` doesn't hold for arbitrary unallocated
-- refs), inductive step (unknown — z3 can't decide), auto-default
-- (proved). Without the option there's no synthetic default — only
-- two obligations exist, both failing.
/--
warning: HandlerCoverageGotoBroken: 1 proved by SMT, 0 user-proved, 1 disproved, 1 unknown, 0 tactic-error, 0 no-diagnostic, 0 missing-premise
2 obligation(s) need a manual proof; fill in the skeletons above.
---
warning: declaration uses 'sorry'
---
warning: declaration uses 'sorry'
-/
#guard_msgs (warning, drop info) in
set_option pverify.autoProveDefault true in
set_option pverify.failOnIncomplete false in
#pverify HandlerCoverageGotoBroken

/-! ## Probe 5 — an `entry`-broken invariant is NOT silently passed.

`always_false` is true at init (`init-holds m.x = false`) but is
violated by the entry's `x = true` write. Pre-fix the entry was
skipped, so the broken invariant verified. Post-fix the
`<M>.<S>.entry_correct_…` obligation exists. -/

pmodule HandlerCoverageEntryBroken
  system s
  event eGo
  machine M {
    var x : Bool
    start state Boot {
      entry { x = true }
      on eGo { pure () }
    }
  }
  init-holds ∀ m : M, m.x = false
  Theorem boot_keeps_false {

    invariant always_false :
      ∀ m : M, is_M m.ref s → (s.machines m.ref).fields.M_x = false
  
  }
  Proof { prove boot_keeps_false ; }
end HandlerCoverageEntryBroken

#gen_module HandlerCoverageEntryBroken

-- The entry obligation `M.Boot.entry_correct_block0_boot_keeps_false`
-- is the one that disproves — SMT finds the model where `x = true` after
-- entry breaks `always_false`. With `autoProveDefault := true` the
-- four other obligations (base, on-handler step, both auto-defaults)
-- discharge cleanly because they don't touch `x`. Pre-fix only 3
-- obligations were emitted (the two entry ones missing); the `5
-- obligations from 1 prove-directives` info line and the `1
-- disproved` pin together regress the fix.
/--
warning: HandlerCoverageEntryBroken: 4 proved by SMT, 0 user-proved, 1 disproved, 0 unknown, 0 tactic-error, 0 no-diagnostic, 0 missing-premise
1 obligation(s) need a manual proof; fill in the skeletons above.
---
warning: declaration uses 'sorry'
-/
#guard_msgs (warning, drop info) in
set_option pverify.autoProveDefault true in
set_option pverify.failOnIncomplete false in
#pverify HandlerCoverageEntryBroken
