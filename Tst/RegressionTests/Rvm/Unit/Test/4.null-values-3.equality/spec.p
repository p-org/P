event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var b_null: bool;
  var b_false: bool;

  start state Start {
    on event1 do {
      b_null = null as bool;
      b_false = false;
      goto If1;
    }

    on getState do {}
  }

  state If1 {
    on event1 do {
      if (b_null == null as bool) {
        goto If2;
      }
    }

    on getState do {}
  }

  state If2 {
    on event1 do {
      if (b_null != null as bool) {
      } else {
        goto If3;
      }
    }

    on getState do {}
  }

  state If3 {
    on event1 do {
      if (b_null == false) {
      } else {
        goto If4;
      }
    }

    on getState do {}
  }

  state If4 {
    on event1 do {
      if (b_null != false) {
        goto If5;
      }
    }

    on getState do {}
  }

  state If5 {
    on event1 do {
      if (b_false == false) {
        goto Success;
      }
    }

    on getState do {}
  }

  state Success {
    on getState do {}
  }
}
