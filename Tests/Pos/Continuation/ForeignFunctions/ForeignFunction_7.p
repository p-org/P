//Foreign function with multiple sends + new + nondet operation in it
//Foriegn functions are in real machine

event nondeteventT:bool;
event nondeteventF:bool;
event done;
event unit:bool;

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
			setme = setme + 1;
		}
		on nondeteventT goto nnextstate;
		on nondeteventF goto nnextstate; 
	}
	state nnextstate {
		entry {
			assert(trigger == nondeteventT && payload == false || trigger == nondeteventF && payload == true);
		}
	}
}

main machine Ghost {
    var local:int;
	var real : mid;
	var choiceval:bool;
	foreign fun createMachine()
	{	
		real = new Real(setme = 1);
	}
	
	foreign fun nondetsend() 
	{
		if(*)
		{
			send(real, nondeteventT, true);
			send(real, nondeteventT, false);
		}
		else
		{
			send(real, nondeteventF, false);
			send(real, nondeteventF, true);
		}
	}
	
    start state Ghost_Init {
        
		entry {
			createMachine();
			nondetsend();
			raise(done, choiceval);
        }
		on done goto endstate;
    }
	
	state endstate {
		entry {
			if(payload)
			{
				send(real, nondeteventT, true);
				send(real, nondeteventT, false);
			}
			else
			{
				send(real, nondeteventF, false);
				send(real, nondeteventF, true);
			}
		}
	}
}
