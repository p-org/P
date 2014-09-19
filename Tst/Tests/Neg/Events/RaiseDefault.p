main machine Entry {
    start state Foo {
        entry {
            raise(default);
        }
        on default goto s1;
    }

    state s1 { }
}
