// P semantics XYZ: one machine, "push" with implicit "pop" when the unhandled event was sent
// This XYZ checks implicit popping of the state when there's an unhandled event which was sent to the queue
// Also, if a state is re-entered (meaning that the state was already on the stack),
// entry function is not executed

event E2 assert 1;
event E1 assert 1;
event E3 assert 1;

machine Main {
    var XYZ: bool;
    start state Real1_Init {
        entry {
			send this, E1;
        }
		
        on E2 goto Real1_S1;
		on E1 goto Real1_Init;    //upon goto, "send this, E2;" is executed
		on E3 do Action1;
        exit {  send this, E2; }
	}
	state Real1_S1 {
		entry {
			XYZ  = true;
			send this, E3;   //at this point, the queue is: E1; E3; Real1_S1 pops and Real1_Init is re-entered
		}
	}
	fun Action1() {
		assert(XYZ == false);  //reachable
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
Comment: (29, 3): Assert failed
*******************************************************/

