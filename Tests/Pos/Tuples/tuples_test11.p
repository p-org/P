event EFoo:((int, bool), int);


main machine Entry { 
    var m:id;

    start state init {
        entry {
            m = new Foo();
            send(m, EFoo, ((1, false), 0));
        }
    }
}

machine Foo {
    var a,b:((int, bool),int);

    start state dummy {
        entry {
        }

        on EFoo goto S1;
    }

    state S1 {
        entry {
            a = (((int, bool), int)) payload;
            a = (((int, bool), int)) payload;
        }
    }   
}
