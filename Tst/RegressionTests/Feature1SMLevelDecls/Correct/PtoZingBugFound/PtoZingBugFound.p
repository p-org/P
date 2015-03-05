//This test found a bug in P-to-Zing compiler
event ready;
event local;

main machine M1 {
	var m : machine;
	start state Init {
		entry {
			//m = new M2(this);
			raise ready;
		}
		on ready goto pushstate;
	}
	
	state pushstate {
		entry {
			push endState;
		}
		on local goto done;
	}
	
	state endState {
		entry {
			raise local;
		}
		
	}
	
	state done {
	
	}
}