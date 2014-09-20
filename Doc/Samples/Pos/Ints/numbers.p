main machine Entry {
    var a:int;

    start state foo {
        entry {
            a = -1;
            a = a + a;
            assert( a == -2);
        }
    }
}
