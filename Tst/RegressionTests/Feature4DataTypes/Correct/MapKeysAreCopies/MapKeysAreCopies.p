// A map or set stores a copy of each key or element it is given. Changing a tuple after
// using it as a key (through the map indexer, or with +=) or as a set element must not
// change the stored key: the map must still find it under its original value.
machine Main {
  start state Init {
    entry {
      var m: map[(int, int), int];
      var nm: map[(a: int, b: int), int];
      var am: map[(int, int), int];
      var s: set[(int, int)];
      var t: (int, int);
      var n: (a: int, b: int);

      // A new key stored through the map indexer.
      t = (1, 2);
      m[t] = 10;
      t.0 = 5;
      assert sizeof(m) == 1, "indexer: map size changed";
      assert (1, 2) in m, "indexer: stored tuple key changed with its source";
      assert !((5, 2) in m), "indexer: stored tuple key aliases its source";
      assert m[(1, 2)] == 10, "indexer: value under the original key changed";

      // An existing key updated through the indexer keeps its original value.
      t = (1, 2);
      m[t] = 11;
      t.1 = 7;
      assert (1, 2) in m && m[(1, 2)] == 11, "indexer: updated key changed with its source";

      // A named tuple key stored through the indexer.
      n = (a = 3, b = 4);
      nm[n] = 1;
      n.a = 9;
      assert (a = 3, b = 4) in nm, "indexer: stored named tuple key changed with its source";
      assert !((a = 9, b = 4) in nm), "indexer: stored named tuple key aliases its source";

      // A key added with +=.
      t = (6, 6);
      am += (t, 1);
      t.0 = 0;
      assert (6, 6) in am, "+=: stored tuple key changed with its source";

      // A set element.
      t = (8, 8);
      s += (t);
      t.0 = 0;
      assert (8, 8) in s, "set: stored tuple element changed with its source";
      assert !((0, 8) in s), "set: stored tuple element aliases its source";
    }
  }
}

test DefaultImpl [main=Main]: { Main };
