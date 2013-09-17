event Unit;

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

        on Unit do retAct;
    }

    action retAct {
        return;
    }
}
