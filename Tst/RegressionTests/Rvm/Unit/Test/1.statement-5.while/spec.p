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
      var j: int;

      i = 1;
      j = 1;

      while (i < 10) {
        i = i + 1;
        j = j + 2;
      }
      assert (j == 19), format("Expected 19 but got {0}.", i);

      while (i < 3) {
        i = i + 1;
        j = j + 2;
      }
      assert (j == 19), format("Expected 19 but got {0}.", i);
    }
  }
}
