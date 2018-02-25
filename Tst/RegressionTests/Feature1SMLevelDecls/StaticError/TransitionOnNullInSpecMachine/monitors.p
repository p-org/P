event a : int;
event b: bool;

spec M observes a {
	var x : machine;
	start state Init {
		entry {
		}
		on a goto next;
	}
	fun goo () {
	
	}
	
	state next {
		entry (payload: any) {
		}
		on null goto next;
	}
}
