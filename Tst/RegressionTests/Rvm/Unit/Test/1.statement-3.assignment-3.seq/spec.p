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
      var bSeq: seq[int];
      var cSeq: seq[int];
      var dSeq: seq[seq[int]];

      var res: int;

      bSeq += (0, 1);
      assert (1 in bSeq), "bSeq should contain 1.";
      assert (!(2 in bSeq)), "bSeq should not contain 2.";
      res = bSeq[0];
      assert (res == 1), format("Expected 1 but got {0}.", res);

      cSeq = bSeq;
      assert (bSeq == cSeq), "bSeq and cSeq should be equal.";

      dSeq += (0, cSeq);
      res = dSeq[0][0];
      assert (res == 1), format("Expected 1 but got {0}.", res);

      dSeq[0][0] = 2;
      res = dSeq[0][0];
      assert (res == 2), format("Expected 2 but got {0}.", res);
    }
  }
}
