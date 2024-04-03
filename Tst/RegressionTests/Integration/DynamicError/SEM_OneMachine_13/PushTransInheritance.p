// P semantics XYZ: one machine, "push" transition, action inherited by the pushed state
// This XYZ checks that after "push" transition, action of the pushing state is inherited by the pushed state
// Compare error trace for this XYZ with the one for PushExplicitPop.p

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
		on E3 do Action1;          //this E3 handler is inherited by Real1_S1
        exit {  send this, E2; }   //never executed
	}
	state Real1_S1 {
		entry {
			XYZ  = true;
			send this, E3;   //E3 is handled in Real1_S1 by Action1 inherited from Real1_Init
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
<StateLog> Machine Real1-0 entering State Real1_S1
<EnqueueLog> Enqueued Event < ____E3, null > in Machine ____Real1-0 by ____Real1-0     --this happens while still in Real1_S1
<DequeueLog> Dequeued Event < ____E3, null > at Machine ____Real1-0
<FunctionLog> Machine Real1-0 executing Function Action1

Error:
P Assertion failed:
Expression: assert(tmpVar_1.bl,)
Comment: (27, 3): Assert failed
*******************************************************/

