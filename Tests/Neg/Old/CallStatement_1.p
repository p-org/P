event E1 assert 1;
event E2 assert 1;
event E3 assert 1;
event E4 assert 1;
event unit assert 1;

main machine Real {
    ghost var ghost_machine: mid;
    var test: bool;
	var counter:int;
    start state Real_Init {
        entry {
			counter = 1;
			ghost_machine = new Ghost(real_machine = this);  
            call(Real_S1); 
        }
        on E2 do Action1;
        on E4 goto Real_S2;
        exit {
	    test = true;
        }
    }

    state Real_S1 {
    
	entry {
		if(counter == 1)
		{ 
			send(ghost_machine, E1);
		}
		counter = counter + 1;
		return;
		}
    }

    state Real_S2 {
	entry {
		call(Real_S1); 
		call(Real_S1); 
		call(Real_S1); 
		while(counter < 10)
		{
			call(Real_S1); 
		}
	    assert(counter == 10);
        assert(counter != 10);
	}
    }

    action Action1 {
		
        send(ghost_machine, E3);
    }
 
}

ghost machine Ghost {
    var real_machine: mid;
    start state Ghost_Init {
        entry {
        }
        on E1 goto Ghost_S1;
    }

    state Ghost_S1 {
        entry {
			send(real_machine, E2);
        }
        on E3 goto Ghost_S2;
    }

    state Ghost_S2 {
        entry {
	    send(real_machine, E4);
        }
    }
}
