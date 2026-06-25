/-
PLean port of P's Tutorial/Advanced/7_ShardedKV.

The benchmark drives the container-port story: every Node owns a
`kv : map[Int, Int]` shard, and `unique_owner` quantifies over two
Nodes' shards at once. `#gen_module` hoists every container var out of
`Fields` into a top-level `Containers` slot uncurried with
`MachineRef`, so `n.kv` desugars to `fun k => s.containers.<M>_<v>
(n.ref, k)` — under `simp` this β-reduces at use sites to a flat
applied symbol lean-auto translates as an uninterpreted function.
Mirrors PVerifier's UCLID5 2D-array layout for `map[K, V]` vars.

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

  -- Every Node starts with an empty shard. Without this, the
  -- base case of `unique_owner` (no two Nodes own the same key) is
  -- not entailed by `InitConditions` alone.
  init-holds ∀ n : Node, ∀ k : tKey, ¬ (k ∈ n.kv)

  Theorem Safety {
    system s {
      invariant transfer_means_no_owner :
        ∀ (e : Sig.Label) (n : Node),
          e is eTransfer → inflight e s →
          ¬ ((eTransfer_payload_of e).key ∈ n.kv)

      invariant unique_key_transfer :
        ∀ e1 e2 : Sig.Label,
          e1 is eTransfer → e2 is eTransfer →
          inflight e1 s → inflight e2 s →
          (eTransfer_payload_of e1).key = (eTransfer_payload_of e2).key →
          e1 = e2

      invariant transfer_means_not_own :
        ∀ e : Sig.Label,
          e is eTransfer → inflight e s →
          ∀ src : Node,
            src.ref = (eTransfer_payload_of e).source →
            ¬ ((eTransfer_payload_of e).key ∈ src.kv)

      invariant unique_owner :
        ∀ (k : tKey) (n1 n2 : Node),
          k ∈ n1.kv → k ∈ n2.kv → n1 = n2
    }
  }

  Proof of_Safety {
    prove Safety ;
    prove default ;
  }

end ShardedKV

#gen_module ShardedKV
#pwf        ShardedKV
#pverify    ShardedKV
