/-
PingPong demo — server machine.

The server holds a reference to its client (set on entry) so it can reply
to pings without hard-coding any machine reference.
-/
import Examples.PingPong.Events

pmodule PingPong

  -- Argument type for Server's entry handler.
  type ServerInit = (client : Client)

  machine Server {
    var client : Client

    start state Idle {
      entry (input : ServerInit) {
        client = input.client
      }

      on ePing (req : PingPayload) {
        send client, ePong, (id = req.id, status = Status.Done)
      }
    }
  }

end PingPong
