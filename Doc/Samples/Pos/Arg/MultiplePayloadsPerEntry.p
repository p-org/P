event E1:int;
event E2:bool; 

main machine Entry {
    var a:int;

    start state init {
        entry {
            // On init the only possible type for payload is nil. Hack up an equality check.
            assert( (id) payload == null );
        }
        on E1 goto exact;
        on E2 goto exact;
    }

    state exact {
        entry {
            a = (int) payload;
        }
    }
}
