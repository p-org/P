event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

enum NumberType {
  FIRST = 1,
  SECOND = 2
}

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Init {
    on event1 do {
      var e: NumberType;
      var f: NumberType;
      var i: int;

      i = FIRST to int;
      assert (i == 1), format("FIRST should cast to 1, but got {0}.", i);

      i = SECOND to int;
      assert (i == 2), format("SECOND should cast to 2, but got {0}.", i);
    }
  }
}
