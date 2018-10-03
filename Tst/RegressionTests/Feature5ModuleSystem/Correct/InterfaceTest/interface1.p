event E1;
event E2;
event E3;

interface I1(int) receives E1, E2;
interface I2(int) receives E1;
interface I3(int) receives E3;

machine Main {
	start state S {
		entry {
			var x: I2;
      var y: I2;
			y = x to I2;
      x = new I2(1);
      new I3(1);
		}
	}
}

machine M
receives E1, E3;
{
  start state S {
    entry (x: int) {
      assert x == 1;
    }
  }
}

implementation impl[main = Main]: {M -> I2, M -> I3, Main};

