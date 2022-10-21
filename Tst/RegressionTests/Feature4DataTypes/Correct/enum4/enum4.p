enum Foo { Foo0 = 2, Foo1 = 1 }
enum Bar { Bar0, Bar1 }

machine Main {
    start state Init {
        entry {
            var x: int;
            var y: Foo;
            var z: Bar;

            x = 1;
            assert (Foo0 to int) > (Foo1 to int), format ("Assertion 0 failed");
            // assert the default value
            assert default(Foo) == Foo1, format ("Assertion 1 failed");
            // assert the default value if not set
            assert default(Bar) == Bar0 && (Bar0 to int) == 0, format ("Assertion 2 failed");
            // assert comparison between two enums
            assert (default(Bar) to int) + 1 == (Foo1 to int), format ("Assertion 3 failed");
            assert x + 1 >= Foo0 to int, format ("Assertion 4 failed");
        }
    }
}
