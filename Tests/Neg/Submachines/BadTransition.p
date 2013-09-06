event EFoo;

main machine Entry {
    start state init {
        entry {
            call(Foo.inside);
        }
    }
    submachine Foo {
        state inside {
            on EFoo goto outside;
        }
    }

    state outside { }
}
