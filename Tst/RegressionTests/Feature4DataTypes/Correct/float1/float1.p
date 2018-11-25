enum Foo { Foo0 = 1, Foo1 = 0 }

machine Main {
	var x: Foo;
	var y: float;

	start state Init {
		entry {
			var z: float;
      		y = .1;
      		z = 0.1;
      		assert(y == z);
      		x = Foo0;
      		y = 1.0;
      		assert((y to int) == (x to int));
		}
	}
}
