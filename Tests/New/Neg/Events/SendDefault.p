main machine Entry {
    var m:machine;
    start state init {
        entry {
            m = new Foo();
            send m, default;
        }
    }
}

machine Foo {
    start state init { }
}
