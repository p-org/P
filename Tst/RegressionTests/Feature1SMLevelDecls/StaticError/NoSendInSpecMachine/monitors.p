event a : int;
event b: bool;

machine X {
	start state Init {
	}
}

spec M observes a {
	var x : machine;
	start state Init {
		entry {
		}
		on a goto next;
	}
	
	state next {
		entry (payload: any) {
			send x, a, 1;
		}
	}
}
