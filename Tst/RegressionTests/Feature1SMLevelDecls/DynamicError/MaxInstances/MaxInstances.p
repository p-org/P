// This sample XYZs assert/assume annotations on events
// Number of instances greater than assumed
event E0;
event E1 assume 0;

machine Main {
    var ghost_machine: machine;
	var x: int;
    start state Real_Init {
        entry {
			x = 0;
			ghost_machine = new Ghost(this);
        	send ghost_machine, E0;
        }
		on E1 do Action1;
		}
	fun Action1() {
		assert(false);						
    }
}

machine Ghost {
    var real_machine: machine;
    start state _Init {
	entry (payload: machine) {
		real_machine = payload;
		send real_machine, E1;
		}
    }
}
