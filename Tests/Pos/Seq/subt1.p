main machine Entry {
    var l:seq[int];
    var l1:seq[any];

    start state init {
        entry {
            l.insert(0, 1);
            l.insert(1, 2);
            l1 = l;
            l.remove(0);
            l.remove(0);
        }
    }
}
