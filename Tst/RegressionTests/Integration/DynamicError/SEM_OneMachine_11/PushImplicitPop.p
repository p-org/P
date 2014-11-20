// P semantics test: one machine, "push" with implicit "pop"
// This test checks implicit popping of the state when there's an unhandled event

event E2 assert 1;
event E1 assert 1;
event E3 assert 1;

main machine Real1 {
    var test: bool;
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
			test  = true;
			send this, E3;
		}
	}
	fun Action1() {
		assert(test == false);  //reachable
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
<StateLog> Machine Real1-0 entering State Real1_S1
<EnqueueLog> Enqueued Event < ____E3, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E1, null > at Machine ____Real1-0
<StateLog> Machine Real1-0 exiting State Real1_S1                                          -- implicit pop; Queue: E3; E2; E2; (E1 dequeued)
<StateLog> Machine Real1-0 exiting State Real1_Init                                        -- on E1 goto Real1_Init;
<EnqueueLog> Enqueued Event < ____E2, null > in Machine ____Real1-0 by ____Real1-0
<StateLog> Machine Real1-0 entering State Real1_Init
<EnqueueLog> Enqueued Event < ____E1, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E3, null > at Machine ____Real1-0                        -- Queue: E2; E1
<FunctionLog> Machine Real1-0 executing Function Action1

Error:
P Assertion failed:
Expression: assert(tmpVar_1.bl,)
Comment: (27, 3): Assert failed
*******************************************************/

