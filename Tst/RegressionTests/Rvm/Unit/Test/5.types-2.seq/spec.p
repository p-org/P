event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var aSeq: seq[int];
  start state Init {
    on event1 do {
      var bSeq: seq[int];
      var cSeq: seq[seq[int]];
      var res: int;

      // [0, 1, 3, 4]
      bSeq += (0, 4);
      bSeq += (0, 3);
      bSeq += (0, 1);
      bSeq += (0, 0);

      assert (4 in bSeq), "SeqContains: bSeq should contain 4";
      assert (!(5 in bSeq)), "SeqContains: bSeq should contain 5";

      res = bSeq[2];
      assert (res == 3), format("SeqGet: Expected 3 but got {0}", res);

      // [0, 1, 2, 4]
      bSeq[2] = 2;
      res = bSeq[2];
      assert (res == 2), format("SeqUpdate: Expected 2 but got {0}", res);

      res = sizeof(bSeq);
      assert (res == 4), format("SeqSizeOf: Expected 4 but got {0}", res);

      // [0, 1, 2]
      bSeq -= 3;
      res = sizeof(bSeq);
      assert (res == 3), format("SeqSizeOf: Expected 3 but got {0}", res);

      aSeq += (0, 0);
      aSeq += (1, 2);
      assert (bSeq != aSeq), format("SeqEqual: bSeq {0} should not be equal to aSeq {1}", bSeq, aSeq);

      aSeq += (1, 1);
      assert (bSeq == aSeq), format("SeqEqual: bSeq {0} should be equal to aSeq {1}", bSeq, aSeq);

      // [[0, 1, 2]]
      cSeq += (0, bSeq);
      res = cSeq[0][1];
      assert (res == 1), format("NestedSeqGet: Expected 1 but got {0}", res);

      cSeq[0][2] = 10;
      res = cSeq[0][2];
      assert (res == 10), format("NestedSeqUpdate: Expected 10 but got {0}", res);
      res = bSeq[2];
      assert (res == 2), format("NestedSeqUpdate: Expected 2 but got {0}", res);

      // null in the seq [0, 1, 2, null]
      assert (!((null as int) in bSeq)), "SeqContains: bSeq should not contain null";
      bSeq += (3, null as int);
      assert ((null as int) in bSeq), "SeqContains: bSeq should contain null";
      res = bSeq[3];
      assert (res == null), format("SeqGet: Expected null but got {0}", res);
      assert (bSeq != aSeq), format("SeqEqual: bSeq {0} should not be equal to aSeq {1}", bSeq, aSeq);

      aSeq += (3, null as int);
      assert (bSeq == aSeq), format("SeqEqual: bSeq {0} should not be equal to aSeq {1}", bSeq, aSeq);
    }
  }
}
