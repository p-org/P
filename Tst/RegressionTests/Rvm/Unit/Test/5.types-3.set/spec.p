event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var aSet: set[int];
  start state Init {
    on event1 do {
      var bSet: set[int];
      var res: int;

      bSet += (0);
      bSet += (1);
      bSet += (2);

      assert (2 in bSet), format("SetContains: bSet should contain 2");
      assert (!(3 in bSet)), format("SetContains: bSet should not contain 3");

      res = sizeof(bSet);
      assert (res == 3), format("SeqSizeOf: Expected 2 but got {0}", res);

      bSet += (1);
      res = sizeof(bSet);
      assert (res == 3), format("SeqSizeOf: Expected 3 but got {0}", res);

      bSet -= (5);
      res = sizeof(bSet);
      assert (res == 3), format("SeqSizeOf: Expected 3 but got {0}", res);

      bSet -= (2);
      assert (!(2 in bSet)), format("SetContains: bSet should not contain 2");
      res = sizeof(bSet);
      assert (res == 2), format("SeqSizeOf: Expected 2 but got {0}", res);

      aSet += (0);
      assert (aSet != bSet), format("SeqEqual: aSet {0} should not be equal to bSet {1}", aSet, bSet);

      aSet += (1);
      assert (aSet == bSet), format("SeqEqual: aSet {0} should not be equal to bSet {1}", aSet, bSet);

      // null in the set
      bSet += (null as int);
      assert ((null as int) in bSet), format("SetContains: bSet should contain null");
      res = sizeof(bSet);
      assert (res == 3), format("SeqSizeOf: Expected 3 but got {0}", res);
      assert (aSet != bSet), format("SeqEqual: aSet {0} should not be equal to bSet {1}", aSet, bSet);

      aSet += (null as int);
      assert (aSet == bSet), format("SeqEqual: aSet {0} should not be equal to bSet {1}", aSet, bSet);

      bSet -= (null as int);
      assert (!((null as int) in bSet)), format("SetContains: bSet should not contain null");
    }
  }
}
