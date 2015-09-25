// This sample tests assert/assume annotations on events
// Number of instances greater than assumed
event E0;
event E1 assume 0;

interface I_E0 E0;
interface I_E1 E1;

main machine Real implements I_E1 {
    var ghost_machine: I_E0;
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

model Ghost implements I_E0 {
    var real_machine: I_E1;
    start state _Init {
	entry { 
		real_machine = payload as I_E1; 
		send real_machine, E1;
		}
    }
}
