main machine Entry {
    model fun foo () { }
    start state bar {
        entry {
            foo();
        }
    }
}
