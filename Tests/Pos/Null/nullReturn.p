main machine Entry {
    foreign fun foo () { }
    start state bar {
        entry {
            foo();
        }
    }
}
