event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

enum NumberType {
  FIRST,
  SECOND
}

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Init {
    on event1 do {
      var e: NumberType;
      var f: NumberType;

      e = FIRST;
      assert (e == FIRST), format("e should be FIRST but got {0}.", e);
      assert (e != SECOND), "e should not be SECOND.";

      f = e;
      assert (e == f), format("e and f should be equal but got {0} and {1}.", e, f);

      e = SECOND;
      assert (e != f), format("e and f should be different, but got {0} and {1}.", e, f);
    }
  }
}
