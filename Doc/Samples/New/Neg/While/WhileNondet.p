main machine Entry {
    var i:int;

    start state init {
        entry {
            i = 0;
            while ( $ ) {
                i = i + 1;
                assert(i < 1);
            }
        }
    }
}
