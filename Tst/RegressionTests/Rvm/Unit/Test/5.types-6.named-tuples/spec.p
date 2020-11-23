event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

type tuple = (a: int, b: int);

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var aTuple: tuple;
  start state Init {
    on event1 do {
      var bTuple: (a: int, b: int);
      var res: int;
      var nested: (c: int, d: tuple);

      bTuple = (a = 1, b = 2);
      res = bTuple.a;
      assert (res == 1), format("Expected 1 but got {0}.", res);
      res = bTuple.b;
      assert (res == 2), format("Expected 2 but got {0}.", res);

      bTuple.a = 3;
      res = bTuple.a;
      assert (res == 3), format("Expected 3 but got {0}.", res);
      res = bTuple.b;
      assert (res == 2), format("Expected 2 but got {0}.", res);

      aTuple = (a = 3, b = 5);
      assert (aTuple != bTuple), format("aTuple {0} and bTuple {1} should not be equal.", aTuple, bTuple);
      aTuple.b = 2;
      assert (aTuple == bTuple), format("aTuple {0} and bTuple {1} should be equal.", aTuple, bTuple);

      bTuple.b = null as int;
      res = bTuple.b;
      assert (aTuple != bTuple), format("aTuple {0} and bTuple {1} should not be equal.", aTuple, bTuple);
      aTuple = (a = 3, b = null as int);
      assert (aTuple == bTuple), format("aTuple {0} and bTuple {1} should be equal.", aTuple, bTuple);

      bTuple = getTuple();
      res = bTuple.a;
      assert (res == 11), format("Expected 11 but got {0}.", res);
      res = bTuple.b;
      assert (res == 12), format("Expected 12 but got {0}.", res);

      nested = (c = 2, d = (a = 4, b = 6));
      res = nested.c;
      assert (res == 2), format("Expected 2 but got {0}.", res);
      res = nested.d.a;
      assert (res == 4), format("Expected 4 but got {0}.", res);

      nested.d.b = 20;
      res = nested.d.b;
      assert (res == 20), format("Expected 20 but got {0}.", res);

      aTuple = nested.d;
      res = aTuple.b;
      assert (res == 20), format("Expected 20 but got {0}.", res);
    }
  }

  fun getTuple() : (a: int, b: int) {
      return (a = 11, b = 12);
  }
}
