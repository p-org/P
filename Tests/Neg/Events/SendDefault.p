main machine Entry {
    var m:mid;
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
