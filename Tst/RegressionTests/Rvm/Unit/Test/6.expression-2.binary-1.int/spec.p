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
      var j: int;
      var res: int;
      var b: bool;

      i = 7;
      j = 3;

      res = i + j;
      assert (res == 10), format("Expected 10 but got {0}.", res);
      res = 7 + 3;
      assert (res == 10), format("Expected 10 but got {0}.", res);

      res = i - j;
      assert (res == 4), format("Expected 4 but got {0}.", res);
      res = 7 - 3;
      assert (res == 4), format("Expected 4 but got {0}.", res);

      res = i * j;
      assert (res == 21), format("Expected 21 but got {0}.", res);
      res = 7 * 3;
      assert (res == 21), format("Expected 21 but got {0}.", res);

      res = i / j;
      assert (res == 2), format("Expected 2 but got {0}.", res);
      res = 7 / 3;
      assert (res == 2), format("Expected 2 but got {0}.", res);

      assert !(i < i), "Expected !(7 < 7).";
      assert !(i < j), "Expected !(7 < 3).";
      assert  (j < i), "Expected  (3 < 7).";
      assert !(7 < 7), "Expected !(7 < 7).";
      assert !(7 < 3), "Expected !(7 < 3).";
      assert  (3 < 7), "Expected  (3 < 7).";

      assert  (i <= i), "Expected  (7 <= 7).";
      assert !(i <= j), "Expected !(7 <= 3).";
      assert  (j <= i), "Expected  (3 <= 7).";
      assert  (7 <= 7), "Expected  (7 <= 7).";
      assert !(7 <= 3), "Expected !(7 <= 3).";
      assert  (3 <= 7), "Expected  (3 <= 7).";

      assert !(j > j), "Expected !(3 > 3).";
      assert !(j > i), "Expected !(3 > 7).";
      assert  (i > j), "Expected  (7 > 3).";
      assert !(3 > 3), "Expected !(3 > 3).";
      assert !(3 > 7), "Expected !(3 > 7).";
      assert  (7 > 3), "Expected  (7 > 3).";

      assert  (j >= j), "Expected  (3 >= 3).";
      assert !(j >= i), "Expected !(3 >= 7).";
      assert  (i >= j), "Expected  (7 >= 3).";
      assert  (3 >= 3), "Expected  (3 >= 3).";
      assert !(3 >= 7), "Expected !(3 >= 7).";
      assert  (7 >= 3), "Expected  (7 >= 3).";

      assert  (i == i), "Expected  (7 == 7).";
      assert !(i == j), "Expected !(7 == 3).";

      assert !(i != i), "Expected !(7 != 7).";
      assert  (i != j), "Expected  (7 != 3).";

      res = -(2 * i + j);
      assert (res == -17), format("Expected -17 but got {0}.", res);
    }
  }
}
