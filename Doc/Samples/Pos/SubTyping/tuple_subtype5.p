event Foo:(any,any);
event Bar:(int,int);

main machine Entry {
    var a:(any, any);

    start state dummy {
        entry {
            raise(Foo, (1,2));
        }

        on Foo goto S1;
    }

    state S1 {
        entry {
            a = ((any, any)) payload;
            raise(Bar, ((int, int)) payload);
        }
        on Bar goto S2;
    }

    state S2 {
        entry {
            assert( ((int, int)) payload == (1,2));
        }
    }
}
