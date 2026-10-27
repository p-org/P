// The checker does not format print messages of schedules that it does not trace, but their
// arguments must still be evaluated: calls, choices and accesses keep their side effects.
machine Main {
  var calls: int;
  var m: map[int, int];
  var s: seq[int];

  fun Next(): int {
    calls = calls + 1;
    return calls;
  }

  start state Init {
    entry {
      var before: int;
      m[1] = 10;
      s += (0, 7);
      print format("call {0}", Next());
      print format("calls {0} and {1}", Next(), Next());
      assert calls == 3, format("expected 3 calls, got {0}", calls);
      print format("map {0}, seq {1}, choice {2}", m[1], s[0], choose(3));
      before = calls;
      print format("{0} then {1}", calls, Next());
      assert calls == before + 1, format("expected {0} calls, got {1}", before + 1, calls);
      if (choose(2) == 0) { print "no arguments"; }
    }
  }
}
