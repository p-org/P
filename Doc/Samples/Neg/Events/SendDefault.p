main machine Entry {
    var m:id;
    start state init {
        entry {
            m = new Foo();
            send(m, default);
        }
    }
}

machine Foo {
    start state init { }
}
