main machine Entry {
    var l:seq[int];
    var i:int;

    start state init {
        entry {
            l += (0, 1);
            assert(l[0] == 1);
            l[0] = 2;
            assert(l[0] == 2);
            i = 0;
            assert(l[i] == 2);
        }
    }
}
