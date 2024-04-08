//Testing goto statement:
//- constants and variables in goto stmt
//
event E;
type mapInt = map[int,int];

machine M2 {
    start state Init {
        entry  (payload: machine) { send payload, E; }
    }
}

machine Main {
    var m: mapInt;
    start state Init {
        entry {
            new M2(this);
            goto T, 15;
        }
        on E do {
            assert(false);    //unreachable
            goto S, m;
        }
        //exit { goto T, 5; }  //function may cause a change in current state; this is not allowed here
    }
    state S {
        entry (x: mapInt) {
            assert x[0] == 5;   //holds
        }
        on E do { ; }
    }
    state T {
        entry (x: int) {
            assert x == 15;   //holds
            m[0] = 5;
            m[1] = 1;
            goto S, m;
        }
    }
}