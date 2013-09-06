event E;

main machine Entry {
    var m:mid;
    var a:eid;

    start state foo {
        entry {
            m = new Foo();
            a = E;
            send(m, E, a);
        }
    }
}

machine Foo {
    start state init {
        on E goto s1;
    }

    state s1 { }
}
