/-
End-to-end: a handler suspended at a `receive` (its locals saved in the
`W` constructor of the site), resumed by the reply.
-/
import PLean

open PLean

pmodule RecvT

  system s

  type tReq  = (src : PLean.MachineRef)
  type tResp = (val : Int)

  event eReq  : tReq
  event eResp : tResp

  machine Client {
    var server : PLean.MachineRef
    var total  : Int

    start state Run {
      entry {
        var tries : Int
        tries = 2
        send server, eReq, (src = this.ref)
        receive {
          case eResp (r : tResp) => {
            total = total + r.val
          }
        }
        total = total + 100
      }
      ignore eResp
    }
  }

  machine Server {
    start state Serve {
      on eReq (q : tReq) {
        send q.src, eResp, (val = 7)
      }
    }
  }

  init-holds ∀ c : Client, is_Server c.server s ∧ c.total = 0

  Theorem safety {
    invariant total_nonneg : ∀ c : Client, c.total ≥ 0
    invariant resp_val : ∀ e : eResp, sent e s → e.val = 7
    invariant client_server : ∀ c : Client, is_Server c.server s
    invariant req_from_client : ∀ e : eReq, inflight e s → is_Client e.src s
    invariant req_to_server :
      ∀ e : PLean.Msg E, is_eReq e → inflight e s → is_Server e.target s
    invariant resp_to_client :
      ∀ e : PLean.Msg E, is_eResp e → inflight e s → is_Client e.target s
    -- the suspended entry kept `tries`
    invariant saved_tries :
      ∀ (c : Client) (a : Int), (s.machines c.ref).waiting = some (W.site0 a) → a = 2
  }
  Proof {
    prove safety ;
  }

end RecvT

#gen_module RecvT

#pverify RecvT

/-- info: 'RecvT.safety_holds' depends on axioms: [propext, Classical.choice, Quot.sound] -/
#guard_msgs in #print axioms RecvT.safety_holds
