event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var i: int;

  start state Start {
    entry {
      i = 3;
    }
    exit {
      i = 4;
    }
    on event1 do {
      assert i == 3, "i has the value set in the entry handler";
      goto Middle;
    }
    on getState do {}
  }

  state Middle {
    on event1 do {
      assert i == 4, "i has the value set in the previous exit handler";
      goto Success;
    }
    on getState do {}
  }

  state Success {
    on getState do {}
  }
}
