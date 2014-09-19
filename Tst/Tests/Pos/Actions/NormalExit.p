event Unit;

main machine Entry {
    var m:id;

    start state init { 
        entry {
            m = new Foo();
            send(m, Unit);
            send(m, Unit);
            send(m, Unit);
        }
    }
}


machine Foo {
    start state init {
        on Unit do Noop;
    }


    action Noop {
    }
}
