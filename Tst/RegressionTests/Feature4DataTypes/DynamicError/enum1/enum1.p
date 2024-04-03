enum Foo { Foo0 = 1, Foo1 = 0 }
enum Bar { Bar0, Bar1 }

machine Main {
    start state Init {
        entry {
            var x: int;
            var y: Foo;
            var z: Bar;

            assert (Foo0 to int) > (Foo1 to int);
            // assert the default value
            assert default(Foo) == Foo1;
            // assert the default value if not set
            assert default(Bar) == Bar1 && (Bar1 to int) == 0;
            // assert comparison between two enums
            assert (default(Bar) to int) == (Foo1 to int);
            assert x >= Foo0 to int;

        }
    }
}