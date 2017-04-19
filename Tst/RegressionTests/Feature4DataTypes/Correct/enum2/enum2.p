enum Foo { Foo0 = 1, Foo1 = 0 }
enum Bar { Bar0, Bar1 }

machine Main {
    start state Init {
        entry {
            var x: int;
            var y: Foo;
            var z: Bar;

            x = Foo0;
            assert x == 1;
            assert x == Foo0;
            y = x as Foo;
            assert y == Foo0;

            x = Foo1;
            assert x == 0;
            assert x == Foo1;
            y = x as Foo;
            assert y == Foo1;

            x = Bar0;
            assert x == 0;
            assert x == Bar0;
            z = x as Bar;
            assert z == Bar0;

            x = Bar1;
            assert x == 1;
            assert x == Bar1;
            z = x as Bar;
            assert z == Bar1;
        }
    }
}