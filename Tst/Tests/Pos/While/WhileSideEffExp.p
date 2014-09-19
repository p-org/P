main machine Entry {
    var a,b:bool;

    start state init {
        entry {
            a = true;
            b = false;
            while ( (1,a)[1] ) {
                b = true;
                a = false;
            }
            assert(b);
        }
    }
}
