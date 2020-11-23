event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var j: int;

  start state Start {
    on event1Int do f;
    on getState do {}
  }

  state Middle {
    entry {
      assert j == 11, "Expecting j to be set by f.";
      goto Success;
    }
    on getState do {}
  }

  state Success {
    on getState do {}
  }

  fun f(i: int) {
    j = i;
    goto Middle;
  }
}
