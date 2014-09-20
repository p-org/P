main machine Entry {
    model fun foo () { return null; }
    start state bar {
        entry {
            foo();
        }
    }
}
