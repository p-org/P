event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Start {
    on event1 do {
      var b: bool;
      // Shortcircuit evaluation, should not throw an exception.
      b = false && (null as bool);

      if (!b) {
        goto And1;
      }
    }

    on getState do {}
  }

  state And1 {
    on event1 do {
      var b: bool;
      b = true && (null as bool);  // true and thing = thing.
      if (b == null) {
        goto And2;
      }
    }

    on getState do {}
  }

  state And2 {
    on event1 do {
      var b: bool;
      var c: bool;
      b = null as bool;
      c = true && b;  // true and thing = thing.
      if (c == null) {
        goto And3;
      }
    }

    on getState do {}
  }

  state And3 {
    on event1 do {
      var b: bool;
      b = (null as bool) && true;  // Should throw exception.
    }

    on event2 goto Not1;

    on getState do {}
  }

  state Not1 {
    on event1 do {
      var b: bool;
      b = !(null as bool);  // Should throw exception.
    }

    on event2 goto Success;

    on getState do {}
  }

  state Success {
    on getState do {}
  }
}
