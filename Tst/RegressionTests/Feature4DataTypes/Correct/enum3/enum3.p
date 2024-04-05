enum Foo { Foo0 = 1, Foo1 = 0 }
enum Bar { Bar0, Bar1 }

machine Main {
    start state Init {
        entry {
            var x: int;
            var y: Foo;
            var z: Bar;

            assert (Foo0 to int) > (Foo1 to int);
            x =  (Foo1 to int) + 1;
            assert x >= Foo0 to int;

        }
    }
}