main machine Entry {
    var a:seq[int];

    start state init {
        entry {
            a += (0, 5);
            a += (0, 6);
            a -= (1);
        }
    }
}
