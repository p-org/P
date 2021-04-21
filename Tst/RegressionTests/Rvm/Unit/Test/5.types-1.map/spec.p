event event1;
event event2;
event event3;
event event1Int: int;
event event2Int: int;
event event3Int: int;

event getState;

spec unittest observes event1, event2, event3, event1Int, event2Int, event3Int, getState {
  var aMap: map[string, int];
  start state Init {
    on event1 do {
        var bMap: map[string, int];
        var cMap: map[string, map[string, int]];
        var res: int;
        var mapSize: int;
        bMap["a"] = 1;
        assert ("a" in bMap), "MapContainsKey: bMap should contain key 'a'";
        assert (!("b" in bMap)), "MapContainsKey: bMap should not contain key 'b'";

        res = bMap["a"];
        assert (res == 1), format("MapGet: Expected 1 but got {0}", res);

        bMap["a"] = 2;
        res = bMap["a"];
        assert (res == 2), format("MapUpate: Expected 2 but got {0}", res);

        bMap += ("b", 3);
        res = bMap["b"];
        assert (res == 3), format("MapAdd: Expected 3 but got {0}", res);

        mapSize = sizeof(bMap);
        assert (mapSize == 2), format("MapSizeOf: Expected 2 but got {0}", mapSize);

        bMap -= "b";
        assert (!("b" in bMap)), "MapRemove: bMap should not contain key 'b'";

        aMap["a"] = 2;
        assert (aMap == bMap), format("MapEqual: aMap {0} and bMap {1} should be equal", aMap, bMap);

        aMap["a"] = 1;
        assert (aMap != bMap), format("MapEqual: aMap {0} and bMap {1} should not be equal", aMap, bMap);

        cMap["a"]= bMap;
        res = cMap["a"]["a"];
        assert (res == 2), format("NestedMapGet: Expected 2 but got {0}", res);

        cMap["a"]["a"] = 3;
        res = cMap["a"]["a"];
        assert (res == 3), format("NestedMapUpdate: Expected 3 but got {0}", res);
        res = bMap["a"];
        assert (res == 2), format("NestedMapUpdate: Expected 2 but got {0}", res);

        cMap["a"] += ("b", 4);
        res = cMap["a"]["b"];
        assert (res == 4), format("NestedAdd: Expected 4 but got {0}", res);

        // null in map
        bMap["a"] = null as int;
        res = bMap["a"];
        assert (res == null as int), format("MapGet: Expected null but got {0}", res);
        assert (!((null as string) in bMap)), "MapContainsKey: bMap should not contain key null";
        bMap[null as string] = 5;
        assert ((null as string) in bMap), "MapContainsKey: bMap should contain key null";
        res = bMap[null as string];
        assert (res == 5), format("MapGet: Expected 5 but got {0}", res);

        assert (aMap != bMap), format("MapEqual: aMap {0} and bMap {1} should not be equal", aMap, bMap);
        aMap["a"] = null as int;
        aMap[null as string] = 5;
        assert (aMap == bMap), format("MapEqual: aMap {0} and bMap {1} should be equal", aMap, bMap);

        // keys and values
        assert ("a" in keys(bMap)), format("MapKeys: 'a' should be in bMap keys {0}", keys(bMap));
        assert ((null as string) in keys(bMap)), format("MapKeys: null should be in bMap keys {0}", keys(bMap));
        assert (!("b" in keys(bMap))), format("MapKeys: 'b' should not be in bMap keys {0}", keys(bMap));

        assert (5 in values(bMap)), format("MapValues: 5 should be in bMap values {0}", values(bMap));
        assert ((null as int) in values(bMap)), format("MapValues: null should be in bMap values {0}", values(bMap));
        assert (!(6 in values(bMap))), format("MapValues: 6 should not be in bMap values {0}", values(bMap));

        goto InsertDuplicateKey;
    }

    on getState do {}
  }

  state InsertDuplicateKey {
    on event2 do {
      var dMap: map[string, int];
      // insert duplicate key
      dMap += ("a", 10);
      dMap += ("a", 11);
    }

    on getState do {}
  }
}
