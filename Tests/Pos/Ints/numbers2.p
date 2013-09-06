main machine Entry {
    var a:int;
    var b:int;

    start state foo {
        entry {
            a = -4;
            b = 4;
            assert( a != b );
        }
    }
}
