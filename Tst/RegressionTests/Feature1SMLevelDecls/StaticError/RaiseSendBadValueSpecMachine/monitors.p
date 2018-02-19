event a : int;
event b : bool;

spec M observes a {
	var x : machine;
	
	start state Init {
		entry {
			raise a, 1;
			raise b, 0;
		}
		on a goto next;
	}
	
	state next {
		entry (payload: any) {
		}
	}
}
