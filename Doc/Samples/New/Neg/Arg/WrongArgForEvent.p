event E:int;

main machine Entry {
    start state foo {
        entry {
            raise E;
        }
        on E goto foo;
    }
}
