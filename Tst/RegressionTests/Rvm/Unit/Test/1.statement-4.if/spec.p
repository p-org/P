event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Init {
    on event1 do {
      var i: int;

      i = 1;

      if (i == 1) {
        i = 2;
      }
      assert (i == 2), format("Expected 2 but got {0}.", i);

      if (i == 1) {
        i = 3;
      }
      assert (i == 2), format("Expected 2 but got {0}.", i);

      if (i == 1) {
        i = 3;
      } else {
        i = 4;
      }
      assert (i == 4), format("Expected 4 but got {0}.", i);
    }
  }
}
