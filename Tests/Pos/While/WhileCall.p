main machine Entry {
    var a,b:bool;
    var i:int;
    var j:int;

    start state init {
        entry {
            i = 0;
            j = 0;
            while ( i < 2 ) {
                call(s1);
                i = i + 1;
            }
            assert(j == 2);
        }
    }

    state s1 {
        entry { j = j + 1; return; }
    }
}
