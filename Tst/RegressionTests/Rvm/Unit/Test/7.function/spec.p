event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var i: int;

  start state Init {
    on event1 do {
      var res: int;

      res = Constant();
      assert (res == 5), format("Expected 5 but got {0}.", res);

      res = AddOne(7);
      assert (res == 8), format("Expected 8 but got {0}.", res);

      SetI();
      assert (i == 10), format("Expected 10 but got {0}.", i);
    }
  }

  fun Constant(): int {
      return 5;
  }

  fun AddOne(m: int): int {
      return m + 1;
  }

  fun SetI() {
      i = 10;
  }
}
