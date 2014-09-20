event Foo:(int,int);

main machine Entry {
    var a, b:(int, int);

    start state dummy {
        entry {
            a = (1, 2);
            raise(Foo, a);
        }

        on Foo goto S1;
    }

    state S1 {
        entry {
            b = ((int, int)) payload;
            assert(b == (1,2));
        }
    }
}
