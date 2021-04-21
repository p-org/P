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
      goto Tuple1;
    }

    on getState do {}
  }

  state Tuple1 {
    on event1 do {
      var t: (b: bool, i:int);
      t.b = null as bool;
      if (t.b == null as bool) {
        goto Tuple2;
      }
    }

    on getState do {}
  }

  state Tuple2 {
    on event1 do {
      var t: (b: bool, i:int);
      t = (b = null as bool, i = 0);
      if (t.b == null as bool) {
        goto Tuple3;
      }
    }

    on getState do {}
  }

  state Tuple3 {
    on event1 do {
      var t: (b: bool, i:int);
      var b: bool;

      t = null as (b: bool, i:int);
      b = t.b;  // Exception
    }

    on event2 goto Tuple4;

    on getState do {}
  }

  state Tuple4 {
    on event1 do {
      var t: (b: bool, i:int);

      t = null as (b: bool, i:int);
      if (t == null as (b: bool, i:int)) {
        goto Tuple5;
      }
    }

    on getState do {}
  }

  state Tuple5 {
    on event1 do {
      var s: (b: bool, i:int);
      var t: (b: bool, i:int);

      s = null as (b: bool, i:int);
      t = s;
      if (t == null as (b: bool, i:int)) {
        goto Success;
      }
    }

    on getState do {}
  }

  state Success {
    on getState do {}
  }
}
