main machine Entry {
    model fun foo(a:eid) {
    }

    start state init {
        entry {
            foo(null);
        }
    }
}
