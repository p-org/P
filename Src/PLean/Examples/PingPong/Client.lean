/-
PingPong demo — client machine.
-/
import Examples.PingPong.Events
import Examples.PingPong.Server

pmodule PingPong

  -- Argument type for Client's entry handler.
  type ClientInit = (server : Server)

  machine Client {
    var server : Server

    start state Booting {
      entry (input : ClientInit) {
        server = input.server
        send server, ePing, (id = 1)
      }

      on ePong goto Done
    }

    state Done { }
  }

end PingPong
