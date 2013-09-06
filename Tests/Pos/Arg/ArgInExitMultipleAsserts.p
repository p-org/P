event EFoo:((int, bool), int);

main machine Entry {
    var m:mid;

    start state init {
        entry {
            m = new Foo();
            send(m, EFoo, ((1, false), 0));
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

        exit {
            if (trigger == EFoo) {
                a = (((int, bool), int)) payload;
            } else {
                b = (int) payload;
            }
        }
    }

    state S1 {
        entry {
        }
    }   
}
