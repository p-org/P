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
      var bMap: map[string, int];
      var cMap: map[string, int];
      var dMap: map[string, map[string, int]];

      var res: int;

      bMap["a"] = 1;
      assert ("a" in bMap), "bMap should contain key 'a'.";
      assert (!("b" in bMap)), "bMap should not contain key 'b'.";
      res = bMap["a"];
      assert (res == 1), format("Expected 1 but got {0}.", res);

      cMap = bMap;
      assert (bMap == cMap), "bMap and cMap should be equal.";

      dMap["b"] = cMap;
      res = dMap["b"]["a"];
      assert (res == 1), format("Expected 1 but got {0}.", res);

      dMap["b"]["a"] = 2;
      res = dMap["b"]["a"];
      assert (res == 2), format("Expected 2 but got {0}.", res);
    }
  }
}
