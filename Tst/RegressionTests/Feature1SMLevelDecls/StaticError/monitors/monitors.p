event a : int;
event b: bool;

main machine Dummy {
	var model_machine: machine;
	var model_machine1: machine;
	start state Init {
		entry {
		model_machine = new M(); 
		model_machine1 = new M1(this);  //"Undeclared machine" error expected, but is not reported
		model_machine = new M_undef(this); 
		}
		
		defer a;
		ignore a;
	}

}

spec M monitors a {
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
		entry (payload: any) {
			pop;
			new Dummy();
			x = new Dummy();
			send x, a, 1;
			send this, a, 1;
		}
		on null goto next;
	}
}
