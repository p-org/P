/-
PingPong demo — top file.

Aggregates Events + Server + Client. Adds a placeholder invariant,
finalises with `#gen_module`, then runs `#pwf` / `#pverify`. Both
default obligations on the send-bearing handlers close via SMT.
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
