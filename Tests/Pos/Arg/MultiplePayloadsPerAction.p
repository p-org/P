event Unit;
event Unit2;
event Bar:int;

main machine Entry {
    var a: int;
    start state Foo {
        entry {
            if (trigger == null) {
                raise(Unit);
            } else {
                assert( trigger == Bar );
                assert( (int) payload == 1 );
                raise(Unit2, null);
            }
        }

        on Unit goto S1;
        on Unit2 goto S2;
        on default goto FAIL;
    }

    state S1 {
        entry {
            assert( (mid) payload == null );
            raise(Bar, 1);
        }

        on Bar goto Foo;
        on default goto FAIL;
    }

    state S2 {
    }

    state FAIL {
        entry {
            assert(false);
        }
    }
}
