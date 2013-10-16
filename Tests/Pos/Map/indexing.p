main machine Entry {
    var l:map[int,int];
    var i:int;

    start state init {
        entry {
	      l.update(0, 1);
            assert(l[0] == 1);
	      l.update(0, 2);
            assert(l[0] == 2);
	    l.remove(0);
	    l.update(0, 2);
            i = 0;
            assert(l[i] == 2);
        }
    }
}
