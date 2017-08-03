machine Main {
  var x: (a: int, b: float);

  var a: (float, seq[float]);
    start state S0 {
    entry {
      x = (a = 1, b = 1.1);
      a = (1.3, default(seq[float]));
      x = (a = 12 to (int, float), b = 2.4);
    }
  }
}
