event Unit;
event Bar;

main machine Entry {
    start state init { 
        entry {
            call(s1);
            raise(Unit);
        }

        on Unit do CallAct;
    }

    action CallAct {
        call(s1);
    }

    state s1 {
        entry {
            return;
        }
    }
}
