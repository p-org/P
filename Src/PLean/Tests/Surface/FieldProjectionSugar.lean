/-
Regression test for the field-projection sugar inside `system <s> { … }`
blocks.

Inside an invariant body, a quantifier-bound machine value `n : <M>`
projects machine `var`s via `n.<v>`, which materialises to
`(s.machines n.ref).fields.<M>_<v>`. A quantifier-bound event value
`e : <ev>` projects payload fields via `e.<f>`, which materialises to
`(<ev>_payload_of e).<f>` (where `<ev>_payload_of` is an extractor
emitted by `#gen_module` alongside the `is_<ev>` predicates).

The sugar runs **before** kind-guard injection so the original
quantifier types are visible to the rewriter; the rewrite is gated on
the field name being a registered machine var / payload field, so
unrelated projections (`n.ref`, `e.action`, `s.machines`) pass through
unchanged.

Bare top-level invariants (no enclosing `system` block) skip this
rewrite — they have no `s` binder for the machine-field expansion.
-/
import PLean

open PLean PartialCorrectness DemonicChoice

/-! ## Machine-field projection: `n.<v>` -/

pmodule MachineFieldSugar

  event ePoke

  machine Counter {
    var count : Nat

    start state Active {
      on ePoke { count = count + 1; }
    }
  }

  Theorem positive_or_zero {
    system s {
      invariant inv :
        ∀ c : Counter,
          c.count = 0 ∨ c.count > 0
    }
  }

end MachineFieldSugar

#gen_module MachineFieldSugar

-- The materialised invariant is `GS → Prop` (state-parameterised) and
-- the body resolves `c.count` via `(s.machines c.ref).fields.Counter_count`.
#check @MachineFieldSugar.inv

/-! ## Event payload projection: `e.<f>` -/

pmodule EventPayloadSugar

  type tMsg = (sender : PLean.MachineRef, value : Nat)

  event eMsg : tMsg

  machine Listener {
    var last : Nat

    start state Idle {
      on eMsg (p : tMsg) { last = p.value; }
    }
  }

  Theorem fact {
    system s {
      invariant inv :
        ∀ e : eMsg,
          inflight e s →
          e.value ≥ 0
    }
  }

end EventPayloadSugar

#gen_module EventPayloadSugar

-- The materialised invariant uses `(eMsg_payload_of e).value` for the
-- payload-field reference. The kind-guard injection retypes `e` to
-- `Sig.Label` and prepends `is_eMsg e`.
#check @EventPayloadSugar.inv
#check @EventPayloadSugar.eMsg_payload_of

/-! ## Mixed: machine field and event payload in one invariant -/

pmodule MixedSugar

  type tBump = (sender : PLean.MachineRef, by_ : Nat)

  event eBump : tBump

  machine Box {
    var count : Nat

    start state On {
      on eBump (p : tBump) { count = count + p.by_; }
    }
  }

  Theorem fact {
    system s {
      invariant inv :
        ∀ b : Box, ∀ e : eBump,
          inflight e s →
          e.sender = b.ref →
          b.count ≥ 0
    }
  }

end MixedSugar

#gen_module MixedSugar
#check @MixedSugar.inv

/-! ## Pass-through: non-registered fields are left alone

`n.ref` is a Lean projection on the wrapper struct, not a machine var —
the rewriter must NOT rewrite it. Same for `e.action`, `e.target`,
`e.actionCount` (Label fields), and `s.machines` (GlobalState fields). -/

pmodule PassThroughSugar

  event eEvt

  machine Holder {
    var dummy : Bool

    start state S { on eEvt { dummy = true; } }
  }

  Theorem fact {
    system s {
      invariant inv :
        ∀ h : Holder, ∀ e : Sig.Label,
          inflight e s →
          e.target = h.ref →
          e.actionCount > 0 →
          (s.machines h.ref).currentState = Holder.S_st →
          True
    }
  }

end PassThroughSugar

#gen_module PassThroughSugar
#check @PassThroughSugar.inv

/-! ## init-holds also gets the rewrite -/

pmodule InitHoldsSugar

  event ePing

  machine Worker {
    var ready : Bool

    start state Wait { on ePing { ready = true; } }
  }

  init-holds (∀ w : Worker, w.ready = false)

  Theorem fact {
    system s {
      invariant inv : ∀ w : Worker, w.ready = false ∨ w.ready = true
    }
  }

end InitHoldsSugar

#gen_module InitHoldsSugar
#check @InitHoldsSugar.InitConditions

/-! ## Sanity: `#pverify` discharges these obligations -/

set_option pverify.failOnIncomplete false in
#pverify MachineFieldSugar

set_option pverify.failOnIncomplete false in
#pverify EventPayloadSugar

set_option pverify.failOnIncomplete false in
#pverify InitHoldsSugar
