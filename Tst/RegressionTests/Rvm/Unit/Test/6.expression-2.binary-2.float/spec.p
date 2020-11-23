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
      var i: float;
      var j: float;
      var res: float;
      var b: bool;

      i = 7.0;
      j = 3.0;

      res = i + j;
      assert (res == 10.0), format("Expected 10.0 but got {0}.", res);
      res = 7.0 + 3.0;
      assert (res == 10.0), format("Expected 10.0 but got {0}.", res);

      res = i - j;
      assert (res == 4.0), format("Expected 4.0 but got {0}.", res);
      res = 7.0 - 3.0;
      assert (res == 4.0), format("Expected 4.0 but got {0}.", res);

      res = i * j;
      assert (res == 21.0), format("Expected 21.0 but got {0}.", res);
      res = 7.0 * 3.0;
      assert (res == 21.0), format("Expected 21.0 but got {0}.", res);

      res = i / j - 2.333333333;
      assert (-0.0001 < res && res < 0.0001), format("Expected ~2.333333333 but got {0}.", res);
      res = 7.0 / 3.0 - 2.333333333;
      assert (-0.0001 < res && res < 0.0001), format("Expected ~2.333333333 but got {0}.", res);

      assert !(i < i), "Expected !(7.0 < 7.0).";
      assert !(i < j), "Expected !(7.0 < 3.0).";
      assert  (j < i), "Expected  (3.0 < 7.0).";
      assert !(7.0 < 7.0), "Expected !(7.0 < 7.0).";
      assert !(7.0 < 3.0), "Expected !(7.0 < 3.0).";
      assert  (3.0 < 7.0), "Expected  (3.0 < 7.0).";

      assert  (i <= i), "Expected  (7.0 <= 7.0).";
      assert !(i <= j), "Expected !(7.0 <= 3.0).";
      assert  (j <= i), "Expected  (3.0 <= 7.0).";
      assert  (7.0 <= 7.0), "Expected  (7.0 <= 7.0).";
      assert !(7.0 <= 3.0), "Expected !(7.0 <= 3.0).";
      assert  (3.0 <= 7.0), "Expected  (3.0 <= 7.0).";

      assert !(j > j), "Expected !(3.0 > 3.0).";
      assert !(j > i), "Expected !(3.0 > 7.0).";
      assert  (i > j), "Expected  (7.0 > 3.0).";
      assert !(3.0 > 3.0), "Expected !(3.0 > 3.0).";
      assert !(3.0 > 7.0), "Expected !(3.0 > 7.0).";
      assert  (7.0 > 3.0), "Expected  (7.0 > 3.0).";

      assert  (j >= j), "Expected  (3.0 >= 3.0).";
      assert !(j >= i), "Expected !(3.0 >= 7.0).";
      assert  (i >= j), "Expected  (7.0 >= 3.0).";
      assert  (3.0 >= 3.0), "Expected  (3.0 >= 3.0).";
      assert !(3.0 >= 7.0), "Expected !(3.0 >= 7.0).";
      assert  (7.0 >= 3.0), "Expected  (7.0 >= 3.0).";

      assert  (i == i), "Expected  (7.0 == 7.0).";
      assert !(i == j), "Expected !(7.0 == 3.0).";

      assert !(i != i), "Expected !(7.0 != 7.0).";
      assert  (i != j), "Expected  (7.0 != 3.0).";
    }
  }
}
