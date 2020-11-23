event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

enum E {
  FIRST = 1,
  SECOND = 2
}

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Failure {
    on event1 do {
      var i: int;
      var f: float;
      var d: float;

      i = (1 to int);
      assert 1 == i, format("1 should convert to 1, but got {0}", i);

      i = (1.1 to int);
      assert 1 == i, format("1.1 should convert to 1, but got {0}", i);

      f = (1 to float);
      assert 1.0 == f, format("1 should convert to 1.0, but got {0}", f);

      f = (1.1 to float);
      assert 1.1 == f, format("1.1 should convert to 1.1, but got {0}", f);

      f = (1 to float) / (2 to float);
      d = f - 0.5;
      assert -0.001 < d && d < 0.001, format("1f/2f should be equal to 0.5, but got {0}", f);

      i = (FIRST to int);
      assert 1 == i, format("FIRST should convert to 1, but got {0}", i);

      i = (SECOND to int);
      assert 2 == i, format("SECOND should convert to 2, but got {0}", i);
    }
  }
}
