// P semantics XYZ: one machine, "push" transition, inherited action is not executed in the pushed state, due to the explicit "pop"
// This XYZ checks explicit popping of a state
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
		on E3 do Action1;          //handling of E3 happens in Real1_Init
        exit {  send this, E2; }   //never executed
	}
	state Real1_S1 {
		entry {
			XYZ  = true;
			send this, E3;
			goto Real1_Init;
		}
	}
	fun Action1() {
		assert(XYZ == false);  //reachable
	}
}
/*****************************************************
With pop:
Safety Error Trace
Trace-Log 0:
<CreateLog> Created Machine Real1-0
<StateLog> Machine Real1-0 entering State Real1_Init
<EnqueueLog> Enqueued Event < ____E1, null > in Machine ____Real1-0 by ____Real1-0
<DequeueLog> Dequeued Event < ____E1, null > at Machine ____Real1-0
<StateLog> Machine Real1-0 entering State Real1_S1
<EnqueueLog> Enqueued Event < ____E3, null > in Machine ____Real1-0 by ____Real1-0
<StateLog> Machine Real1-0 exiting State Real1_S1                                      --popping Real1_S1 state
<DequeueLog> Dequeued Event < ____E3, null > at Machine ____Real1-0                    --this happens in Real1_Init state
<FunctionLog> Machine Real1-0 executing Function Action1

Error:
P Assertion failed:
Expression: assert(tmpVar_1.bl,)
Comment: (28, 3): Assert failed
*******************************************************/

