main machine Entry {
    var a:int;

    start state init {
        entry {
            a = -1;
            assert( a < 0 );
        }
    }
}
