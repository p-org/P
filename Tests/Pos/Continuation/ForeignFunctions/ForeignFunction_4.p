//Foreign function with send + new + nondet operation in it

event nondeteventT:bool;
event nondeteventF:bool;
event done;

machine Real {
var ghostm :mid;
var setme : int;
start state init {
		entry { assert(setme == 1);
		}
		on nondeteventT goto nextstate;
		on nondeteventF goto nextstate;
	}
	state nextstate {
		entry {
			assert(trigger == nondeteventT && payload == true || trigger == nondeteventF && payload == false);
			
		}
	}
}

main ghost machine Ghost {
    var local:int;
	var real : id;
	foreign fun createMachine()
	{	
		real = new Real(setme = 1);
	}
	
	foreign fun nondetsend() 
	{
		if(*)
		{
			send(real, nondeteventT, true);
		}
		else
		{
			send(real, nondeteventF, false);
		}
	}
	
    start state Ghost_Init {
        
		entry {
			createMachine();
			nondetsend();
			raise(done);
        }
		on done goto endstate;
    }
	
	state endstate {
		entry {
		}
	}
}
