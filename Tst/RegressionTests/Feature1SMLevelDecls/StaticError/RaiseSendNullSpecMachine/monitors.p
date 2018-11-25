event a : int;
event b : bool;

spec M observes a {
	var x : machine;
	
	start state Init {
		entry {
			raise a;
			raise b, true;
		}
		on a goto next;
	}
	
	state next {
		entry {
		}
	}
}
