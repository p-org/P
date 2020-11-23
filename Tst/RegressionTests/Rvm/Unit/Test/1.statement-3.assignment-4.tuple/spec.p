event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var i: int;
  start state Init {
    on event1 do {
      var bTuple: (string, int);
      var cTuple: (string, int);
      var dTuple: (int, (string, int));

      var res1: string;
      var res2: int;

      bTuple = ("a", 1);
      res1 = bTuple.0;
      assert (res1 == "a"), format("Expected 'a' but got '{0}'.", res1);
      res2 = bTuple.1;
      assert (res2 == 1), format("Expected 1 but got {0}.", res2);

      cTuple = bTuple;
      assert (bTuple == cTuple), "bTuple and cTuple should be equal.";

      dTuple = (3, cTuple);
      res2 = dTuple.1.1;
      assert (res2 == 1), format("Expected 1 but got {0}.", res2);

      dTuple.1.1 = 2;
      res2 = dTuple.1.1;
      assert (res2 == 2), format("Expected 2 but got {0}.", res2);
    }
  }
}
