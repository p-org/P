main machine Entry {
    var a:seq[int];

    start state init {
        entry {
            a.insert(0, 5);
            a.insert(0, 6);
            a.remove(1);
        }
    }
}
