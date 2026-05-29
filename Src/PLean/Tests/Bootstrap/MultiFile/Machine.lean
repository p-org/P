/-
Multi-file aggregation test — machine file.
Reopens `pmodule MultiTest` and references events from Events.lean.
-/
import Tests.Bootstrap.MultiFile.Events

pmodule MultiTest
  machine Greeter {
    start state Saying {
      on eHello goto Done
      on eWorld goto Done
    }

    state Done { }
  }
end MultiTest
