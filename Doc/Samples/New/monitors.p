event a : int;

main machine Dummy {
	start state Init {
	
	}

}

monitor M {
	var x : machine;
	start state Init {
		entry {
			raise a;
		}
		on a goto next;
	}
	model fun goo () {
	
	}
	
	state next {
		entry {
			push Init;
			pop;
			new Dummy();
			send x, a, 1;
		}
	
	}
}
