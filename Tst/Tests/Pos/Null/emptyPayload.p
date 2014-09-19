event Foo;

main machine Entry {
    var m : id;
    start state init {
        entry {
            m = new M2();
            assert((mid) payload == null);
            raise(Foo);
        }
        on Foo goto S1;
    }

    state S1 {
        entry {
            assert((mid) payload == null);
            send(m, Foo);
            raise(Foo, null);
        }

        on Foo goto S2;
    }

    state S2 {
        entry {
            assert((mid) payload == null);
            send(m, Foo, null);
        }
    }
}

machine M2 {
    start state SUP {
        entry {
            assert((mid) payload == null);
        }

        on Foo goto BRAH;
    }

    state BRAH {
        entry {
            assert((mid) payload == null);
        }

        on Foo goto JUST_CHILLIN;
    }

    state JUST_CHILLIN {
        entry {
            assert((mid) payload == null);
        }
    }
}
