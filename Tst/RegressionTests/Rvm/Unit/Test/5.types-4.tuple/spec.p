event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var aTuple: (string, int);
  start state Init {
    on event1 do {
      var bTuple: (string, int);
      var resStr: string;
      var resInt: int;

      bTuple = ("abc", 1);
      resStr = bTuple.0;
      assert (resStr == "abc"), format("TupleGetField: Expected \"abc\" but got {0}.", resStr);

      bTuple.1 = 2;
      resInt = bTuple.1;
      assert (resInt == 2), format("TupleUpdateField: Expected 2 but got {0}.", resInt);

      aTuple = ("abc", 10);
      assert (aTuple != bTuple), format("TupleNotEqual: aTuple {0} and bTuple {1} should not be equal", aTuple, bTuple);
      aTuple.1 = 2;
      assert (aTuple == bTuple), format("TupleEqual: aTuple {0} and bTuple {1} should be equal", aTuple, bTuple);

      bTuple.1 = null as int;
      resInt = bTuple.1;
      assert (resInt == null), format("TupleUpdateField: Expected null but got {0}.", resInt);
      aTuple = ("abc", null as int);
      assert (aTuple == bTuple), format("TupleEqual: aTuple {0} and bTuple {1} should be equal", aTuple, bTuple); 
    }
  }
}
