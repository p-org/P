// An event that foreign code creates without a payload has no (null) payload, as do
// clones of events (as on master). P code that sends an event value assigns the payload.
event eInt: int;
event eTuple: (a: int, s: seq[int]);
event eDone;

fun NewEventsHaveNoPayload(): bool;

machine Main {
  var received: int;
  start state Init {
    entry {
      var e: event;
      assert NewEventsHaveNoPayload(), "an event created without a payload has one";
      e = eInt;
      send this, e, 3;
      e = eTuple;
      send this, e, (a = 1, s = default(seq[int]));
      send this, eTuple, (a = 1, s = default(seq[int]));
      send this, eDone;
    }
    on eInt do (p: int) {
      assert p == 3, format("expected 3, got {0}", p);
      received = received + 1;
    }
    on eTuple do (p: (a: int, s: seq[int])) {
      assert p.a == 1 && sizeof(p.s) == 0, format("unexpected payload {0}", p);
      received = received + 1;
    }
    on eDone do {
      assert received == 3, format("expected 3 events, got {0}", received);
    }
  }
}

test DefaultImpl [main=Main]: { Main };
