event event1;
event event2;
event event3;
event notObserved;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Init {
    on event1 do {
      raise event2;
    }

    on event2 goto S1;

    on getState do {}
  }

  state S1 {
    on event1 do {
      raise notObserved;
    }

    on notObserved goto S2;

    on getState do {}
  }

  state S2 {
    on event1 do {
      raise event1Int, 2;
    }

    on event1Int do (x: int) {
      assert (x == 2), format("Expected 2 but got {0}.", x);
      goto Success;
    }

    on getState do {}
  }

  state Success {
    on getState do {}
  }
}
