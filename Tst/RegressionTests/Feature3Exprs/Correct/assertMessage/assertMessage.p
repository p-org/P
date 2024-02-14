machine Main {
  start state Init {
    entry {
      var m: map[int, int];
      assert !(1 in m), format("Yay! {0}", m[1]);
    }
  }
}
