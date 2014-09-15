main machine Entry {
    var l:map[int,int];
    var i:int;

    start state init {
        entry {
	      l[0] = 1;
	      assert (0 in l);
	      i = keys(l)[0];
	      assert (i == 0);
            assert(l[0] == 1);
	      l[0] = 2;
            assert(l[0] == 2);
	    l -= (0);
	    l[0] = 2;
            i = 0;
            assert(l[i] == 2);
        }
    }
}
