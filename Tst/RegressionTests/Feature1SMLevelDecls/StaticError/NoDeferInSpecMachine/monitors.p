event a : int;
event b: bool;

spec M observes a {
	var x : machine;
	start state Init {
		entry {
		}
		on a goto next;
	}

	state next {
		defer a;
		entry (payload: any) {
		}
	}
}
