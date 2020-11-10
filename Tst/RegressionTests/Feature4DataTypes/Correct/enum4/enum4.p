enum Foo { Foo0 = 2, Foo1 = 1 }
enum Bar { Bar0, Bar1 }

machine Main {
    start state Init {
        entry {
            var x: int;
            var y: Foo;
            var z: Bar;

            x = 1;
            assert (Foo0 to int) > (Foo1 to int);
            // assert the default value
            assert default(Foo) == Foo1;
            // assert the default value if not set
            assert default(Bar) == Bar0 && (Bar0 to int) == 0;
            // assert comparison between two enums
            assert (default(Bar) to int) + 1 == (Foo1 to int);
            assert x + 1 >= Foo0 to int;
        }
    }
}