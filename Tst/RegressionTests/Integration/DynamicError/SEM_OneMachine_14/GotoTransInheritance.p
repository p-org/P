// P semantics XYZ: one machine, "goto" transition, action is not inherited by the destination state
// This XYZ checks that after "goto" transition, action of the src state is not inherited by the dest state
// Compare error trace for this XYZ with the one for PushTransInheritance.p

event E2 assert 1;
event E1 assert 1;
event E3 assert 1;

machine Main {
    var XYZ: bool;
    start state Real1_Init {
        entry {
			send this, E1;
        }
		
        on E1 goto Real1_S1;
		on E3 goto Real1_S2;         //this E3 handler is not inherited by Real1_S1
        exit {
			//send this, E2;
		}
	}
	state Real1_S1 {
		entry {
			XYZ  = true;
			send this, E3;    		
		}
		on E3 goto Real1_Init;
		exit {
			send this, E3;       //this instance of E3 is not handled in Real1_S1, but in Real1_Init
		}
	}
	state Real1_S2 {
		entry {
			assert(XYZ == false);  //reachable
		}
	}
}
/*****************************************************
afety Error Trace
Trace-Log 0:
<CreateLog> Created Machine Real1-0
<StateLog> Machine Real1-0 entering State Real1_Init
<EnqueueLog> Enqueued Event < ____E1, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E1, null > at Machine ____Real1-0
<StateLog> Machine Real1-0 exiting State Real1_Init
<StateLog> Machine Real1-0 entering State Real1_S1
<EnqueueLog> Enqueued Event < ____E3, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E3, null > at Machine ____Real1-0
<StateLog> Machine Real1-0 exiting State Real1_S1
<EnqueueLog> Enqueued Event < ____E3, null > in Machine ____Real1-0 by ____Real1-0
<StateLog> Machine Real1-0 entering State Real1_Init
<EnqueueLog> Enqueued Event < ____E1, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E3, null > at Machine ____Real1-0
<StateLog> Machine Real1-0 exiting State Real1_Init
<StateLog> Machine Real1-0 entering State Real1_S2

Error:
P Assertion failed:
Expression: assert(tmpVar_1.bl,)
Comment: (34, 4): Assert failed
*******************************************************/

