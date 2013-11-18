event Foo:(int,int);

main machine Entry {
    var a:(any, any);
    var m:id;

    start state dummy {
        entry {
            m = new Bar();
            a = (1, 1);
            send(m, Foo, a);
        }
    }
}

machine Bar {
    var a:(int, int);
    start state init {
        entry {
        }
        on Foo goto S1;
    }

    state S1 {
        entry {
            a = ((int, int)) payload;
            assert(a == (1,1));
        }
    }
}
