main machine Entry {
    var l:seq[int];
    var l1:seq[any];

    start state init {
        entry {
            l += (0, 1);
            l += (1, 2);
            l1 = l;
            l -= (0);
            l -= (0);
			assert(sizeof(l) == 0);
        }
    }
}
