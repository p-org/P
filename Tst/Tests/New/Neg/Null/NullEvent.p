main machine Entry {
    var m:machine;
    var e:event;

    start state init {
        entry {
            m = new Foo();
            e = null;
            send m, e, null;
        }
    }
}

machine Foo {
    start state init {
    }
}
