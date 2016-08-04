enum Foo { foo0, foo1, foo2, foo3, foo4 }
enum Bar { bar0, bar1, bar2, bar3 }

machine Main {
	var x: Foo;
	var y: Bar;

	start state Init {
		entry {
			assert x == default(Foo);
			assert y == default(Bar);
			assert x == foo0;
			assert y == bar0;
			x = foo1;
			assert x != foo0;
			y = bar2;
			assert y != default(Bar);
		}
	}
}
