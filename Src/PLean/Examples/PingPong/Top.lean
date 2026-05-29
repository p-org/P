/-
PingPong demo — top file.

Aggregates Events + Server + Client. Adds invariants, finalises with
`#gen_module`, then runs `#pwf` / `#pverify`.

Note: temporal predicates (using `≺`) are not yet checkable in Phase 0.
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
#pverify  PingPong
