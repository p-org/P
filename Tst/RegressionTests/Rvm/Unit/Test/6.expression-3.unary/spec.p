event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Init {
    on event1 do {
      var i: int;
      var f: float;
      var bTrue: bool;
      var bFalse: bool;

      var resi: int;
      var resf: float;
      var resb: bool;

      i = 7;
      f = 7.0;
      bTrue = true;
      bFalse = false;

      resi = -i;
      assert (resi == -7), format("Expected -7 but got {0}.", resi);
      resi = -(7);
      assert (resi == -7), format("Expected -7 but got {0}.", resi);

      resf = -f;
      assert (resf == -7.0), format("Expected -7.0 but got {0}.", resf);
      resf = -(7.0);
      assert (resf == -7.0), format("Expected -7.0 but got {0}.", resf);

      resb = !bTrue;
      assert (resb == false), format("Expected false but got {0}.", resb);
      resb = !true;
      assert (resb == false), format("Expected false but got {0}.", resb);

      resb = !bFalse;
      assert (resb == true), format("Expected true but got {0}.", resb);
      resb = !false;
      assert (resb == true), format("Expected true but got {0}.", resb);
    }
  }
}
