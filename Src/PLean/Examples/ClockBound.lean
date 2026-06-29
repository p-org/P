/-
PLean port of the ClockBound protocol (`https://github.com/aws/clock-bound`)

The P source has three impl machines (GlobalClock, LocalClock, Client)
plus a `spec Correctness` that observes `eLocalResponse`. PLean's
verification model doesn't need the runtime setup machinery — we drop
the per-machine `Init` states and the `Client` machine entirely, and
state the global safety properties directly over the `sent` set,
indexed by `eLocalResponse` labels.

`choose(n)` is modelled as `MonadNonDet.pickSuchThat Int (0 ≤ x ≤ n)`
([`PLean.choose`](../PLean/Semantics/Primitives.lean)): the WP gives
the verifier `0 ≤ x ∧ x ≤ n` as a hypothesis about the chosen value.

Target safety goals:
  G1: sent e0 → sent e1 → e0.latest < e1.earliest → e0.trueTime < e1.trueTime
  G2: sent e0 → sent e1 → e0.trueTime < e1.trueTime → e0.earliest < e1.latest
  G3: sent e0 → sent e1 → e0.trueTime < e1.trueTime →
        e0.target = e1.target → e0.earliest ≤ e1.earliest
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 10
set_option loom.solver.smt.retryOnUnknown false
set_option auto.mono.ignoreNonQuasiHigherOrder true

pmodule ClockBound

  system s

  type tGlobalQuery    = (localClock : PLean.MachineRef)
  type tGlobalResponse = (target : PLean.MachineRef, trueTime : Int)
  type tLocalResponse  = (target : PLean.MachineRef, trueTime : Int,
                          earliest : Int, latest : Int)

  event eGlobalQuery    : tGlobalQuery
  event eLocalQuery
  event eGlobalResponse : tGlobalResponse
  event eLocalResponse  : tLocalResponse

  -- Unique GlobalClock (the global() function from the reference).
  function global_clock : GlobalClock

  -- GlobalClock's `time` is the monotone source of true time.
  machine GlobalClock {
    var time : Int

    start state Serving {
      on eGlobalQuery (payload : tGlobalQuery) {
        let delta ← PLean.choose 10
        time = time + delta + 1
        send payload.localClock, eGlobalResponse,
          (target = payload.localClock, trueTime = time)
      }
    }
  }

  -- LocalClock keeps its current uncertainty bounds plus refs to its
  -- GlobalClock and its Client. The two states (`Idle` and
  -- `Waiting`) track whether this LC has an outstanding
  -- eGlobalQuery — required to prove that at most one
  -- eGlobalResponse to this LC is in flight at any time.
  machine LocalClock {
    var currEarlyBound  : Int
    var currLateBound   : Int
    var maxUncertainty  : Int
    var globalClock     : PLean.MachineRef
    var client          : PLean.MachineRef

    start state Idle {
      on eLocalQuery {
        send globalClock, eGlobalQuery, (localClock = this.ref)
        goto Waiting
      }
    }

    state Waiting {
      on eGlobalResponse (payload : tGlobalResponse) {
        -- P: early = currEarlyBound + choose(payload.trueTime - currEarlyBound + 1)
        --   ⇒ early ∈ [currEarlyBound, payload.trueTime]
        let earlyDelta ← PLean.choose (payload.trueTime - currEarlyBound)
        let lateDelta  ← PLean.choose maxUncertainty
        let early : Int := currEarlyBound + earlyDelta
        let late  : Int := payload.trueTime + lateDelta
        send client, eLocalResponse,
          (target = client, trueTime = payload.trueTime,
           earliest = early, latest = late)
        currEarlyBound = early
        currLateBound = late
        goto Idle
      }
    }
  }

  /-! ## Init-holds: pin the steady-state configuration. -/

  init-holds ∀ g : GlobalClock, g = global_clock
  init-holds is_GlobalClock global_clock s
  init-holds ∀ g : GlobalClock, g.time = 0
  init-holds ∀ lc : LocalClock, lc.currEarlyBound = 0
  init-holds ∀ lc : LocalClock, lc.currLateBound = 0
  init-holds ∀ lc : LocalClock, lc.maxUncertainty > 0
  init-holds ∀ lc : LocalClock, lc.globalClock = global_clock
  init-holds ∀ lc1 lc2 : LocalClock, lc1.client = lc2.client → lc1 = lc2

  /-! ## Inductive invariants

  The safety goals G1–G3 are not inductive on their own; they hold by
  consequence of structural invariants linking each `eLocalResponse`
  payload to the LocalClock that sent it and to the GlobalClock's
  `time`. -/

  -- Topology lemma: there is a unique GlobalClock, every LocalClock
  -- points at it. Mirrors LockServer's `system_config`.
  Lemma topology {

    invariant gc_is_unique : ∀ g : GlobalClock, g = global_clock
    invariant gc_kind : is_GlobalClock global_clock s
    invariant lc_global : ∀ lc : LocalClock, lc.globalClock = global_clock

    -- Each LocalClock has a distinct client (one-to-one mapping).
    invariant lc_client_injective :
      ∀ lc1 lc2 : LocalClock,
        lc1.client = lc2.client → lc1 = lc2

    -- For any sent label `e` whose action is an `eGlobalResponse`,
    -- the label's `target` field matches the payload's `target`
    -- field. The GC handler sets both to `payload.localClock` at
    -- send time. We quantify over `Sig.Label` (not `eGlobalResponse`)
    -- so the `e.target` on the LHS stays the label's `target` field;
    -- the field-projection sugar (`rewriteFieldProjections` in
    -- `Syntax/Verify.lean`) would otherwise rewrite it to
    -- `(eGlobalResponse_payload_of e).target` (since `e` would have a
    -- registered event kind and `target` is a registered payload
    -- field), trivializing the equation.
    invariant gResp_label_payload_target_match :
      ∀ e : Sig.Label,
        is_eGlobalResponse e → s.sent e = true →
        e.target = (eGlobalResponse_payload_of e).target

    -- Similarly for eGlobalQuery: label.target = global_clock.ref.
    invariant gQuery_label_target_is_gc :
      ∀ q : Sig.Label,
        is_eGlobalQuery q → s.sent q = true →
        q.target = global_clock.ref

    -- For sent eLocalResponse, label.target = payload.target = sender's client.
    invariant lResp_label_payload_target_match :
      ∀ e : Sig.Label,
        is_eLocalResponse e → s.sent e = true →
        e.target = (eLocalResponse_payload_of e).target
  
  }

  -- Global-time facts: every sent eGlobalResponse has positive
  -- trueTime ≤ the (unique) GlobalClock's `time`. Also: every
  -- LocalClock's `currEarlyBound` lies in `[0, globalClock.time]`.
  Lemma global_time {

    invariant globalTime_nonneg :
      ∀ g : GlobalClock, 0 ≤ g.time

    invariant gResp_trueTime_le_globalTime :
      ∀ e : eGlobalResponse, ∀ g : GlobalClock,
        s.sent e = true → e.trueTime ≤ g.time

    invariant gResp_trueTime_pos :
      ∀ e : eGlobalResponse, s.sent e = true → 0 < e.trueTime

    invariant lc_currEarly_le_globalTime :
      ∀ lc : LocalClock, ∀ g : GlobalClock,
        lc.currEarlyBound ≤ g.time

    invariant lc_currEarly_nonneg :
      ∀ lc : LocalClock, 0 ≤ lc.currEarlyBound

    -- Every sent eLocalResponse's trueTime ≤ GC.time (since it came
    -- from a received eGlobalResponse, whose trueTime ≤ GC.time).
    invariant lResp_trueTime_le_globalTime :
      ∀ e : eLocalResponse, ∀ g : GlobalClock,
        s.sent e = true → e.trueTime ≤ g.time
  
  }

  -- Causal-chain bundle: per-LC, the in-flight `eGlobalQuery` and
  -- `eGlobalResponse` form a coupled state. Joint invariants
  -- proved together since they're mutually inductive.
  Lemma causal {

    invariant idle_no_gQuery :
      ∀ lc : LocalClock, ∀ q : eGlobalQuery,
        stateOf lc.ref s = LocalClock.Idle_st →
        inflight q s →
        ¬ (q.localClock = lc.ref)

    invariant idle_no_gResp :
      ∀ lc : LocalClock, ∀ e : eGlobalResponse,
        stateOf lc.ref s = LocalClock.Idle_st →
        inflight e s →
        ¬ (e.target = lc.ref)

    invariant gResp_unique_per_lc :
      ∀ lc : LocalClock, ∀ e1 e2 : eGlobalResponse,
        inflight e1 s → inflight e2 s →
        e1.target = lc.ref → e2.target = lc.ref →
        e1 = e2

    invariant gQuery_unique_per_lc :
      ∀ lc : LocalClock, ∀ q1 q2 : eGlobalQuery,
        inflight q1 s → inflight q2 s →
        q1.localClock = lc.ref → q2.localClock = lc.ref →
        q1 = q2

    invariant gQuery_excludes_gResp :
      ∀ lc : LocalClock, ∀ q : eGlobalQuery, ∀ e : eGlobalResponse,
        inflight q s → inflight e s →
        q.localClock = lc.ref → ¬ (e.target = lc.ref)
  
  }

  -- Linking invariants tying gResp in-flight to LC's state.
  Lemma linking {

    invariant gResp_target_currEarly :
      ∀ lc : LocalClock, ∀ e : eGlobalResponse,
        inflight e s →
        e.target = lc.ref →
        lc.currEarlyBound ≤ e.trueTime

    -- Past sent eLocalResponse trueTime ≤ in-flight gResp trueTime
    -- (for matching LC ↔ client). Captures GC time monotonicity.
    invariant lResp_trueTime_le_inflight_gResp :
      ∀ lc : LocalClock,
      ∀ e_lresp : eLocalResponse, ∀ e_gresp : eGlobalResponse,
        s.sent e_lresp = true → e_lresp.target = lc.client →
        inflight e_gresp s → e_gresp.target = lc.ref →
        e_lresp.trueTime ≤ e_gresp.trueTime
  
  }

  -- Local-clock bounds on sent eLocalResponses (one invariant per
  -- Lemma so each SMT query stays small).
  Lemma lresp_earliest_le_trueTime_lemma {

    invariant lResp_earliest_le_trueTime :
      ∀ e : eLocalResponse,
        s.sent e = true → e.earliest ≤ e.trueTime
  
  }

  Lemma lresp_trueTime_le_latest_lemma {

    invariant lResp_trueTime_le_latest :
      ∀ e : eLocalResponse,
        s.sent e = true → e.trueTime ≤ e.latest
  
  }

  Lemma lc_currEarly_bounds_earliest_lemma {

    invariant lc_currEarly_bounds_earliest :
      ∀ lc : LocalClock, ∀ e : eLocalResponse,
        s.sent e = true →
        e.target = lc.client →
        e.earliest ≤ lc.currEarlyBound
  
  }

  /-! ## Target safety properties. -/

  Theorem G1 {

    invariant g1 :
      ∀ e0 e1 : eLocalResponse,
        s.sent e0 = true → s.sent e1 = true →
        e0.latest < e1.earliest →
        e0.trueTime < e1.trueTime
  
  }

  Theorem G2 {

    invariant g2 :
      ∀ e0 e1 : eLocalResponse,
        s.sent e0 = true → s.sent e1 = true →
        e0.trueTime < e1.trueTime →
        e0.earliest < e1.latest
  
  }

  Theorem G3 {

    invariant g3 :
      ∀ e0 e1 : eLocalResponse,
        s.sent e0 = true → s.sent e1 = true →
        e0.trueTime < e1.trueTime →
        e0.target = e1.target →
        e0.earliest ≤ e1.earliest
  
  }

  Proof Safety {
    prove topology ;
    prove causal using topology ;
    prove global_time using topology, causal ;
    prove linking using topology, causal, global_time ;
    prove lresp_earliest_le_trueTime_lemma using topology, causal, global_time, linking ;
    prove lresp_trueTime_le_latest_lemma using topology, causal, global_time, linking ;
    prove lc_currEarly_bounds_earliest_lemma using topology, causal, global_time, linking ;
    prove G1 using topology, causal, global_time, linking,
                   lresp_earliest_le_trueTime_lemma, lresp_trueTime_le_latest_lemma,
                   lc_currEarly_bounds_earliest_lemma ;
    prove G2 using topology, causal, global_time, linking,
                   lresp_earliest_le_trueTime_lemma, lresp_trueTime_le_latest_lemma,
                   lc_currEarly_bounds_earliest_lemma ;
    prove G3 using topology, causal, global_time, linking,
                   lresp_earliest_le_trueTime_lemma, lresp_trueTime_le_latest_lemma,
                   lc_currEarly_bounds_earliest_lemma ;
  }

end ClockBound

#gen_module ClockBound
#pwf        ClockBound

namespace ClockBound
open PLean PartialCorrectness DemonicChoice

end ClockBound

set_option maxHeartbeats 4000000 in
#pverify ClockBound
