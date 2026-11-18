// Foreign code creates tuples from arrays that it reuses, and reads tuples through their
// public fieldValues/fieldNames. Each tuple must keep the values it was created with, and
// a tuple that P code passes to foreign code must stay a copy of the caller's value.
fun MakeTwo(): ((int, int), (int, int));
fun MakeNamed(): ((a: int, b: int), (a: int, b: int));
fun Describe(t: (a: seq[int], b: int)): string;
fun Change(t: (a: seq[int], b: int));

machine Main {
  start state Init {
    entry {
      var r: ((int, int), (int, int));
      var n: ((a: int, b: int), (a: int, b: int));
      var t: (a: seq[int], b: int);

      r = MakeTwo();
      assert r.0.0 == 1 && r.0.1 == 2 && r.1.0 == 3 && r.1.1 == 4,
        format("tuples share the array they were created from: {0}", r);
      n = MakeNamed();
      assert n.0.a == 1 && n.0.b == 2 && n.1.a == 3 && n.1.b == 4,
        format("named tuples share the array they were created from: {0}", n);

      t.a += (0, 1);
      t.b = 2;
      assert Describe(t) == "a=1:b=2", format("unexpected description {0}", Describe(t));
      Change(t);
      assert sizeof(t.a) == 1 && t.b == 2, format("foreign code changed the caller's tuple: {0}", t);
    }
  }
}

test DefaultImpl [main=Main]: { Main };
