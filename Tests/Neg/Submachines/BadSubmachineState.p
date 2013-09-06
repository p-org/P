main machine Entry {
    start state init {
        entry {
            call(Foo.bar);
        }
    }
    submachine Foo {
    }
}
