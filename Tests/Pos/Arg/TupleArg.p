event EFoo:(int, int);
event EBar:(payload1: int, payload2: int);

main machine Entry {
    var mFoo: mid;

    start state Init {
        entry {
            mFoo = new Foo();
            if (1<2) {
                send(mFoo, EFoo, (1,2));
            } else {
                send(mFoo, EBar, (payload1=1,payload2=2));
            }
        }
    }
}

machine Foo {
    var a:(int, int);
    var b:(payload1:int, payload2:int);

    start state Init {
        entry { }
        on EFoo goto S1;
        on EBar goto S2;
    }

    state S1 {
        entry {
            a = ((int, int)) payload;
        }
    }

    state S2 {
        entry {
            b = ((payload1: int, payload2: int)) payload;
            assert( b == (payload1=1, payload2=2) );
        }
    }
}
