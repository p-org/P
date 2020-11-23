event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

// Test that a member variable is preserved through states.
spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var i: int;
  start state Start {
    entry {
      i = 3;
    }

    on event1 do {
      assert i == 3, "i was initialized to 3";
      i = 4;
      goto Middle;
    }

    on getState do {}
  }

  state Middle {
    on event1 do {
      assert i == 4, "i was set to 4";
      goto Success;
    }

    on getState do {}
  }

  state Success {
    on getState do {}
  }
}
