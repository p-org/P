event EUnit;

main machine Entry {
    start state Foo {
        entry {
            raise(EUnit);
        }
        on EUnit goto s1;
    }

    state s1 { }
}
