event E1;
event E2;
event E3;

type I1(int) = {E1, E2};
type I2(int) = {E1};
type I3(int) = {E3};

machine Main {
	start state S {
		entry {
			var x: I1;
      var y: I2;
			y = x to I2;
      x = new I2(1);
      x = new I3(1);
		}
	}
}

machine M : I2, I3
receives E1, E3;
{
  start state S {
    entry (x: int) {
      assert x == 1;
    }
  }
}

