event foo:any;
event unit:any;

main machine Entry {
    var a:any;

    start state init {
        entry {
            raise(unit, 4);
        }

        on unit goto s1;
    }

    state s1 {
        entry {
            a = (any) payload;
        }
    }
}
