event Foo:(int,int);

main machine Entry {
    var a, b:(any, any);

    start state dummy {
        entry {
            a = (1, 1);
            raise(Foo, a);
        }

        on Foo goto S1;
    }

    state S1 {
        entry {
            b = ((any, any)) payload;
        }
    }
}
