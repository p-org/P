/-
PLean port of the ClockBound protocol (https://github.com/aws/clock-bound).

A GlobalClock serves the true time. Each LocalClock, when its Client
asks, queries the GlobalClock and answers with an uncertainty interval
`[earliest, latest]` around the true time; `choose` models the clock
error.

Safety, over the sent `eLocalResponse`s `e0`, `e1`:
  G1: e0.latest < e1.earliest → e0.trueTime < e1.trueTime
  G2: e0.trueTime < e1.trueTime → e0.earliest < e1.latest
  G3: e0.trueTime < e1.trueTime → e0.target = e1.target →
        e0.earliest ≤ e1.earliest

`eGlobalResponse` / `eLocalResponse` carry a payload field `target`; an
invariant reaches the message's own target through a `PLean.Msg E` binder.
-/
import PLean

open PLean

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

  -- The unique GlobalClock.
  function global_clock : GlobalClock

  machine GlobalClock {
    var time : Int

    start state Serving {
      on eGlobalQuery (payload : tGlobalQuery) {
        let delta ← choose (10 : Int)
        time = time + delta + 1
        send payload.localClock, eGlobalResponse,
          (target = payload.localClock, trueTime = time)
      }
    }
  }

  machine LocalClock {
    var currEarlyBound : Int
    var currLateBound  : Int
    var maxUncertainty : Int
    var globalClock    : PLean.MachineRef
    var client         : PLean.MachineRef

    start state Idle {
      on eLocalQuery {
        send globalClock, eGlobalQuery, (localClock = this.ref)
        goto Waiting
      }
    }

    state Waiting {
      defer eLocalQuery

      on eGlobalResponse (payload : tGlobalResponse) {
        -- early ∈ [currEarlyBound, trueTime], late ∈ [trueTime, trueTime + maxUncertainty]
        let earlyDelta ← choose (payload.trueTime - currEarlyBound)
        let lateDelta  ← choose maxUncertainty
        let early : Int := currEarlyBound + earlyDelta
        let late  : Int := payload.trueTime + lateDelta
        send client, eLocalResponse,
          (target = client, trueTime = payload.trueTime, earliest = early, latest = late)
        currEarlyBound = early
        currLateBound = late
        goto Idle
      }
    }
  }

  -- Asks its LocalClock for the time, again after every answer.
  machine Client {
    var localClock : PLean.MachineRef

    start state Querying {
      entry {
        send localClock, eLocalQuery
      }

      on eLocalResponse (payload : tLocalResponse) {
        send localClock, eLocalQuery
      }
    }
  }

  init-holds ∀ g : GlobalClock, g = global_clock
  init-holds is_GlobalClock global_clock.ref s
  init-holds ∀ g : GlobalClock, g.time = 0
  init-holds ∀ lc : LocalClock, lc.currEarlyBound = 0
  init-holds ∀ lc : LocalClock, lc.currLateBound = 0
  init-holds ∀ lc : LocalClock, lc.maxUncertainty > 0
  init-holds ∀ lc : LocalClock, lc.globalClock = global_clock.ref
  init-holds ∀ lc : LocalClock, is_Client lc.client s
  init-holds ∀ lc1 lc2 : LocalClock, lc1.client = lc2.client → lc1 = lc2
  init-holds ∀ c : Client, is_LocalClock c.localClock s

  -- One GlobalClock; who talks to whom.
  Lemma topology {
    invariant gc_is_unique : ∀ g : GlobalClock, g = global_clock
    invariant gc_kind : is_GlobalClock global_clock.ref s
    invariant lc_global : ∀ lc : LocalClock, lc.globalClock = global_clock.ref
    invariant lc_uncertainty : ∀ lc : LocalClock, lc.maxUncertainty > 0
    invariant lc_client_kind : ∀ lc : LocalClock, is_Client lc.client s
    invariant client_lc_kind : ∀ c : Client, is_LocalClock c.localClock s
    -- Each LocalClock has its own client.
    invariant lc_client_injective :
      ∀ lc1 lc2 : LocalClock, lc1.client = lc2.client → lc1 = lc2

    invariant gResp_target_match :
      ∀ e : PLean.Msg E, is_eGlobalResponse e → sent e s →
        e.target = (eGlobalResponse_payload_of e).target
    invariant gQuery_to_gc :
      ∀ q : PLean.Msg E, is_eGlobalQuery q → sent q s → q.target = global_clock.ref
    invariant gQuery_from_lc :
      ∀ q : eGlobalQuery, inflight q s → is_LocalClock q.localClock s
    invariant gResp_to_lc :
      ∀ e : eGlobalResponse, inflight e s → is_LocalClock e.target s
    invariant lQuery_to_lc :
      ∀ q : PLean.Msg E, is_eLocalQuery q → inflight q s → is_LocalClock q.target s
    invariant lResp_target_match :
      ∀ e : PLean.Msg E, is_eLocalResponse e → sent e s →
        e.target = (eLocalResponse_payload_of e).target
    invariant lResp_to_client :
      ∀ e : PLean.Msg E, is_eLocalResponse e → inflight e s → is_Client e.target s
  }

  -- Every sent `eGlobalResponse` carries a positive time at most the
  -- GlobalClock's; every LocalClock's early bound lies in `[0, time]`.
  Lemma global_time {
    invariant globalTime_nonneg : ∀ g : GlobalClock, 0 ≤ g.time
    invariant gResp_trueTime_le_globalTime :
      ∀ (e : eGlobalResponse) (g : GlobalClock), sent e s → e.trueTime ≤ g.time
    invariant gResp_trueTime_pos : ∀ e : eGlobalResponse, sent e s → 0 < e.trueTime
    invariant lc_currEarly_le_globalTime :
      ∀ (lc : LocalClock) (g : GlobalClock), lc.currEarlyBound ≤ g.time
    invariant lc_currEarly_nonneg : ∀ lc : LocalClock, 0 ≤ lc.currEarlyBound
    invariant lResp_trueTime_le_globalTime :
      ∀ (e : eLocalResponse) (g : GlobalClock), sent e s → e.trueTime ≤ g.time
  }

  -- Per LocalClock, at most one query or response is in flight, and only
  -- while it is `Waiting`.
  Lemma causal {
    invariant idle_no_gQuery :
      ∀ (lc : LocalClock) (q : eGlobalQuery),
        stateOf lc.ref s = some LocalClock.Idle_st → inflight q s → q.localClock ≠ lc.ref
    invariant idle_no_gResp :
      ∀ (lc : LocalClock) (e : eGlobalResponse),
        stateOf lc.ref s = some LocalClock.Idle_st → inflight e s → e.target ≠ lc.ref
    invariant gResp_unique_per_lc :
      ∀ (lc : LocalClock) (e1 e2 : eGlobalResponse),
        inflight e1 s → inflight e2 s → e1.target = lc.ref → e2.target = lc.ref → e1 = e2
    invariant gQuery_unique_per_lc :
      ∀ (lc : LocalClock) (q1 q2 : eGlobalQuery),
        inflight q1 s → inflight q2 s → q1.localClock = lc.ref → q2.localClock = lc.ref → q1 = q2
    invariant gQuery_excludes_gResp :
      ∀ (lc : LocalClock) (q : eGlobalQuery) (e : eGlobalResponse),
        inflight q s → inflight e s → q.localClock = lc.ref → e.target ≠ lc.ref
  }

  Lemma linking {
    invariant gResp_target_currEarly :
      ∀ (lc : LocalClock) (e : eGlobalResponse),
        inflight e s → e.target = lc.ref → lc.currEarlyBound ≤ e.trueTime
    -- Earlier answers to a client carry no later time than the
    -- in-flight response to its LocalClock.
    invariant lResp_trueTime_le_inflight_gResp :
      ∀ (lc : LocalClock) (e_lresp : eLocalResponse) (e_gresp : eGlobalResponse),
        sent e_lresp s → e_lresp.target = lc.client →
        inflight e_gresp s → e_gresp.target = lc.ref →
        e_lresp.trueTime ≤ e_gresp.trueTime
  }

  Lemma lresp_earliest_le_trueTime_lemma {
    invariant lResp_earliest_le_trueTime :
      ∀ e : eLocalResponse, sent e s → e.earliest ≤ e.trueTime
  }

  Lemma lresp_trueTime_le_latest_lemma {
    invariant lResp_trueTime_le_latest :
      ∀ e : eLocalResponse, sent e s → e.trueTime ≤ e.latest
  }

  Lemma lc_currEarly_bounds_earliest_lemma {
    invariant lc_currEarly_bounds_earliest :
      ∀ (lc : LocalClock) (e : eLocalResponse),
        sent e s → e.target = lc.client → e.earliest ≤ lc.currEarlyBound
  }

  Theorem G1 {
    invariant g1 :
      ∀ e0 e1 : eLocalResponse,
        sent e0 s → sent e1 s → e0.latest < e1.earliest → e0.trueTime < e1.trueTime
  }

  Theorem G2 {
    invariant g2 :
      ∀ e0 e1 : eLocalResponse,
        sent e0 s → sent e1 s → e0.trueTime < e1.trueTime → e0.earliest < e1.latest
  }

  Theorem G3 {
    invariant g3 :
      ∀ e0 e1 : eLocalResponse,
        sent e0 s → sent e1 s → e0.trueTime < e1.trueTime →
        e0.target = e1.target → e0.earliest ≤ e1.earliest
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

#pverify ClockBound
