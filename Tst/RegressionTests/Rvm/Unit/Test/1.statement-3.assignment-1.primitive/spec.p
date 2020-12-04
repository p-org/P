event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var i: int;
  var f: float;
  var b: bool;
  start state Init {
    on event1 do {
      var j: int;
      var g: float;
      var c: bool;

      // member var = constant
      i = 1;
      assert (i == 1), format("Expected 1 but got {0}.", i);

      // local var = member var
      j = i;
      assert (j == 1), format("Expected 1 but got {0}.", j);

      // local var = constant
      j = 2;
      assert (j == 2), format("Expected 2 but got {0}.", j);

      // member var = local var
      i = j;
      assert (i == 2), format("Expected 2 but got {0}.", i);

      f = 3.0;
      assert (f == 3.0), format("Expected 3.0 but got {0}.", f);

      g = f;
      assert (g == 3.0), format("Expected 3.0 but got {0}.", g);

      b = true;
      assert (b == true), format("Expected true but got {0}.", b);

      c = b;
      assert (c == true), format("Expected true but got {0}.", c);
    }
  }
}
