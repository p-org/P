main machine Entry {
    var a:mid;

    start state foo {
        entry {
            assert(a == null);
        }
    }
}
