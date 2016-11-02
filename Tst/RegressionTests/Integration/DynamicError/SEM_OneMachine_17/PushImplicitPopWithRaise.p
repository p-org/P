// P semantics XYZ: one machine, "push" with implicit "pop" when the unhandled event was raised
// This XYZ checks implicit popping of the state when there's an unhandled event which was raised

event E2 assert 1;
event E1 assert 1;
event E3 assert 1;

machine Main {
    var XYZ: bool;
    start state Real1_Init {
        entry { 
			send this, E1;
        }
		
        on E2 push Real1_S1; 
		on E1 goto Real1_Init;    //upon goto, "send this, E2;" is executed
		on E3 do Action1;
        exit {  send this, E2; }
	}
	state Real1_S1 {
		entry {
			XYZ  = true;
			raise E1;
			//send this, E3;
		}
	}
	fun Action1() {
		assert(XYZ == false);  //unreachable
	}
}
/*****************************************************
Safety Error Trace
Trace-Log 0:
<CreateLog> Created Machine Real1-0
<StateLog> Machine Real1-0 entering State Real1_Init
<EnqueueLog> Enqueued Event < ____E1, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E1, null > at Machine ____Real1-0
<StateLog> Machine Real1-0 exiting State Real1_Init
<EnqueueLog> Enqueued Event < ____E2, null > in Machine ____Real1-0 by ____Real1-0
<StateLog> Machine Real1-0 entering State Real1_Init
<EnqueueLog> Enqueued Event < ____E1, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E2, null > at Machine ____Real1-0
<StateLog> Machine Real1-0 entering State Real1_S1                                      -- push Real1_S1; Queue: E1
<RaiseLog> Machine Real1-0 raised Event ____E1
<StateLog> Machine Real1-0 exiting State Real1_S1                                       -- implicit pop of Real1_S1
<StateLog> Machine Real1-0 exiting State Real1_Init                                     -- on E1 goto Real1_Init; Queue: E1
<EnqueueLog> Enqueued Event < ____E2, null > in Machine ____Real1-0 by ____Real1-0      -- exit function of Real1_Init
<StateLog> Machine Real1-0 entering State Real1_Init
<EnqueueLog> Enqueued Event < ____E1, null > in Machine ____Real1-0 by ____Real1-0      -- Queue: E1; E1
<Exception> Attempting to enqueue event ____E1 more than max instance of 1

Error:
P Assertion failed:
Expression: assert(false)
Comment: 
*******************************************************/

