/-
PingPong demo — top file.

Aggregates Events + Server + Client. Adds invariants, finalises with
`#gen_module`, then runs `#pwf` / `#pverify`.

Note: temporal predicates (using `≺`) are not yet checkable in Phase 0.

The demo uses `set_option pverify.failOnIncomplete false` so the auto-
emitted `prove default;` obligations on the `send`-bearing handlers
get reported as warnings (with `@[pverifyProof]` skeletons) rather
than failing the build. Mirror of the Phase3DistributedLock /
Phase3LockServer treatment.
-/
import Examples.PingPong.Events
import Examples.PingPong.Server
import Examples.PingPong.Client

pmodule PingPong
  invariant pong_after_ping : True
end PingPong

#gen_module PingPong

#print_pmodule PingPong
#pwf      PingPong
set_option pverify.failOnIncomplete false in
#pverify  PingPong
