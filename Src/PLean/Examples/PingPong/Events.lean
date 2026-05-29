/-
PingPong demo — events file.

Declares the types and events. Imported by Server.lean and Top.lean.
-/
import PLean

pmodule PingPong

  enum Status { Pending, Done }

  -- Payload for ePing: identifies the requester so the server can reply.
  type PingPayload = (id : Nat)

  -- Payload for ePong: echoes the request id and reports a status.
  type PongPayload = (id : Nat, status : Status)

  event ePing : PingPayload
  event ePong : PongPayload

end PingPong
