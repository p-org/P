event EFoo:((int, bool), int);
event EBar:((int, bool), int);

main machine Entry {
    var m:mid;
    var e1,e2:eid;

    start state init {
        entry {
            m = new Foo();
            e1 = EFoo;
            e2 = EBar;

            if (2<1) {
                send(m, e1, ((1, false), 0));
            } else {
                send(m, e2, ((2, true), 1));
            }
        }
    }
}

machine Foo {
    var a:((int, bool),int);
    var b:int;

    start state dummy {
        entry {
        }

        on EFoo goto S1;
        on EBar goto S1;

        exit {
            if (trigger == EFoo) {
                a = (((int, bool), int)) payload;
            } else if (trigger == EBar) {
                a = (((int, bool), int)) payload;
            } else {
                assert(false);
            }
        }
    }

    state S1 {
        entry {
        }
    }   
}
