machine Main {
  var x: (a: int, b: float);
  var y : seq[float];
  var a: (float, seq[float]);
    start state S0 {
    entry {
      x = (a = 1, b = 1.1);
      y += (0, x.b);
      a = (1.3, default(seq[float]));
      x = (a = 12, b = 2.4);
      y += (0, x.b);
      a.1 = y;
      assert a.1[0] > a.1[1];
    }
  }
}
