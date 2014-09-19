event Unit;
event Bar;

main machine Entry {
    start state init { 
        entry {
            call(s1);
        }
    }

    state s1 {
        entry {
            raise(Unit);
            assert(false);
        }

        on Unit do act1;
        on Bar do act2;
    }

    action act1 {
        raise(Bar);
    }

    action act2 {
        return;
    }
}
