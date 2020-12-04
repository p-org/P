event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

type tint = int;
type tuple = (a: int, b: tint);

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  start state Init {
    on event1 do {
      var t: tuple;
      var i: tint;
      var res: int;
  
      t = (a = 1, b = 2);
      i = t.a;
      assert (i == 1), format("Expected 1 but got {0}.", i);
      res = t.b;
      assert (res == 2), format("Expected 2 but got {0}.", res);

      i = 3;
      assert (i == 3), format("Expected 3 but got {0}.", i);
    }
  }
}
