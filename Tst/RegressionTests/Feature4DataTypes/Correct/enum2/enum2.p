enum Foo { Foo0 = 1, Foo1 = 0 }
enum Bar { Bar0, Bar1 }

machine Main {
    start state Init {
        entry {
            var x: int;
            var y: Foo;
            var z: Bar;

            x = Foo0 to int;
            assert x == 1;
            assert x == Foo0 to int;
            y = Foo0;
            assert y == Foo0;

            x = Foo1 to int;
            assert x == 0;
            assert x == Foo1 to int;

            x = Bar0 to int;
            assert x == 0;
            assert x == Bar0 to int;
            z = Bar0;
            assert z == Bar0;

            x = Bar1 to int;
            assert x == 1;
            assert x == Bar1 to int;
        }
    }
}