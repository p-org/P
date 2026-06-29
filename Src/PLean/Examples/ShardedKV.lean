/-
PLean port of P's Tutorial/Advanced/7_ShardedKV.

Surface notes:
- `tKey` / `tValue` are `Int` aliases for parity with the P source.
- Handler `if (e.reshard_key in kv) then ...` becomes `if e.reshard_key
  ∈ kv then do …` via the decidable `PMap` membership instance.
- `kv -= (e.reshard_key)` routes through the `PContainerErase`
  typeclass (set difference for `Set T`, `mapErase` for `PMap K V`).
-/
import PLean

open PLean PartialCorrectness DemonicChoice

set_option loom.solver "cvc5"
set_option loom.solver.smt.timeout 5

pmodule ShardedKV

  system s

  type tKey   = Int
  type tValue = Int

  type tTransfer = (source : PLean.MachineRef, key : tKey, value : tValue)
  type tReshard  = (reshard_key : tKey, reshard_to : PLean.MachineRef)

  event eTransfer : tTransfer
  event eReshard  : tReshard

  machine Node {
    var kv : map[tKey, tValue]

    start state Serving {
      on eReshard (e : tReshard) {
        if e.reshard_key ∈ kv then do
          -- `getD 0` reads the value with `0` as a default — when
          -- the key is present, the default is unused; the SMT-side
          -- reasoning recovers the actual value via the routing
          -- invariants. The simpler form lets `wpgen` step through
          -- without falling into `WPGen.default`.
          let v := (kv e.reshard_key).getD 0
          kv -= (e.reshard_key)
          send e.reshard_to, eTransfer,
            (source = this.ref, key = e.reshard_key, value = v)
      }

      on eTransfer (e : tTransfer) {
        kv[e.key] = e.value
      }
    }
  }

  -- `unique_owner` is a *deployment assumption* — the protocol
  -- preserves uniqueness of key ownership but does not establish
  -- it. The P source leaves `kv` initially unconstrained (no
  -- `init-condition`) and relies on the user to deploy the system
  -- with disjoint shards; PVerifier accepts this implicitly via
  -- UCLID5's `init` block. We make the assumption explicit as an
  -- `init-holds` so the base-case VC discharges.
  init-holds
    ∀ (k : tKey) (n1 n2 : Node),
      k ∈ n1.kv → k ∈ n2.kv → n1 = n2

  Theorem Safety {
    invariant transfer_means_no_owner :
      ∀ (e : eTransfer) (n : Node),
        inflight e s → ¬ (e.key ∈ n.kv)

    invariant unique_key_transfer :
      ∀ e1 e2 : eTransfer,
        inflight e1 s → inflight e2 s →
        e1.key = e2.key →
        e1 = e2

    invariant transfer_means_not_own :
      ∀ (e : eTransfer) (src : Node),
        inflight e s → src.ref = e.source →
        ¬ (e.key ∈ src.kv)

    invariant unique_owner :
      ∀ (k : tKey) (n1 n2 : Node),
        k ∈ n1.kv → k ∈ n2.kv → n1 = n2
  }

  Proof of_Safety {
    prove Safety ;
    prove default ;
  }

end ShardedKV

#gen_module ShardedKV
#pwf        ShardedKV
#pverify    ShardedKV
