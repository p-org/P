main machine Entry {
    foreign fun foo () { return null; }
    start state bar {
        entry {
            foo();
        }
    }
}
