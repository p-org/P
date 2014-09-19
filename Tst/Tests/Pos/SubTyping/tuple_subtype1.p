event Foo:(any,any);

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
    var a:(any, any);
    start state init {
        entry {
        }
        on Foo goto S1;
    }

    state S1 {
        entry {
            a = ((any, any)) payload;
        }
    }
}
