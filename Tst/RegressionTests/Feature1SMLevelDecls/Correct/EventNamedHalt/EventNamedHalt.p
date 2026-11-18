// An event declared with the name Halt is not the built-in halt event.
event Halt: int;

machine Main {
  var got: int;
  start state Init {
    entry {
      var e: event;
      e = Halt;
      send this, e, 4;
    }
    on Halt do (p: int) {
      assert p == 4, format("expected 4, got {0}", p);
      got = p;
      raise halt;
    }
  }
}
