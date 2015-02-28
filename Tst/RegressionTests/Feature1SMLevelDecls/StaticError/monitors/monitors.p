event a : int;
event b: bool;

main machine Dummy {
	var model_machine: machine;
	start state Init {
		entry {
		model_machine = new M(this); 
		model_machine = new M_undef(this); 
		}
		
		defer a;
		ignore a;
	}

}

monitor M {
	var x : machine;
	start state Init {
		entry {
			raise a;
			raise b, 0;
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
			send this, a, 1;
		}
		on default goto next;
	}
}
