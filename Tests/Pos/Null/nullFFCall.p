main machine Entry {
    foreign fun foo(a:eid) {
    }

    start state init {
        entry {
            foo(null);
        }
    }
}
