// The C# backend keeps expressions inline and lifts only what it needs (LoweringMode.CSharp).
// Its order of evaluation and side effects must match the full lowering that the Java backends
// use, where a call is evaluated before the fields of its enclosing expression are read.
event eGo: int;

machine Main {
  var x: int;
  var picks: int;
  var ticks: int;
  var st: set[int];

  fun Bump(): int { x = x + 10; return 1; }
  fun Tick(): int { ticks = ticks + 1; return ticks; }
  fun PickEvent(): event { picks = picks + 1; return eGo; }

  start state Init {
    entry {
      var y: int;
      var sum: int;
      var n: int;

      // A call that changes a field is evaluated before the field is read, on either side.
      x = 1;
      y = x + Bump();
      assert y == 12 && x == 11, format("x + Bump(): y = {0}, x = {1}", y, x);
      x = 1;
      y = Bump() + x;
      assert y == 12 && x == 11, format("Bump() + x: y = {0}, x = {1}", y, x);

      // Print arguments with side effects are evaluated once, whether or not the schedule is traced.
      print format("tick {0}", Tick());
      assert ticks == 1, format("print evaluated its argument {0} times", ticks);

      // Foreach over a set iterates over the elements it had when the loop started.
      st += (1);
      st += (2);
      st += (3);
      foreach (n in st) {
        sum = sum + n;
        st -= (n);
        st += (n + 100);
      }
      assert sum == 6, format("foreach over a set changed in the loop summed {0}", sum);
      assert sizeof(st) == 3 && 101 in st && 102 in st && 103 in st, format("set after the loop: {0}", st);

      // Raise evaluates its event once, before its payload.
      raise PickEvent(), picks;
    }

    on eGo do (p: int) {
      assert picks == 1, format("raise evaluated its event {0} times", picks);
      assert p == 1, format("raise read its payload before its event: {0}", p);
    }
  }
}

test DefaultImpl [main=Main]: { Main };
