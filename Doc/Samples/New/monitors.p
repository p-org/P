event a : int;

main machine Dummy {
	start state Init {
		defer a;
		ignore a;
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
		defer a;
		ignore a;
		entry {
			push Init;
			pop;
			new Dummy();
			x = new Dummy();
			send x, a, 1;
		}
		on default goto next;
	}
}
