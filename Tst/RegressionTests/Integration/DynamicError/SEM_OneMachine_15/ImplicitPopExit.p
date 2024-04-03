// P semantics XYZ: one machine, exit actions executed upon implicit "pop"
// This XYZ checks that when the state is implicitly popped,  exit function of that state is executed
// Compare this XYZ to PushImplicitPop.p

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
			send this, E3;
		}
		exit {  assert(false); }  //reachable
	}
	fun Action1() {
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
<EnqueueLog> Enqueued Event < ____E2, null > in Machine ____Real1-0 by ____Real1-0
<StateLog> Machine Real1-0 entering State Real1_Init
<EnqueueLog> Enqueued Event < ____E1, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E2, null > at Machine ____Real1-0
<StateLog> Machine Real1-0 entering State Real1_S1
<EnqueueLog> Enqueued Event < ____E3, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E1, null > at Machine ____Real1-0
<StateLog> Machine Real1-0 exiting State Real1_S1                                    --implicit pop

Error:
P Assertion failed:
Expression: assert(tmp_7.bl,)
Comment: (26, 11): Assert failed
*******************************************************/

