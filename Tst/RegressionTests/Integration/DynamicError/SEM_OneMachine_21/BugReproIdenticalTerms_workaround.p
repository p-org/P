//Exit function performed while explicitly popping the state
//this is a workaround for wrong Zinger line number reporting in error trace:
//Formula terms for asserts on lines 7 and 16 are made different
//Compare this XYZ to BugReproIdenticalTerms.p
event E;
machine Main {
	start state Init {
		entry { raise E; }
		exit { assert (false); }  //unreachable
		on E goto Call;
	}

	state Call {
		   entry {
			
				       goto Init;
			}
			exit { assert (false); ;}  //reachable
	}
}
/************************* Expected result:
Safety Error Trace
Trace-Log 0:
<CreateLog> Created Machine Program-0
<StateLog> Machine Program-0 entering State Init
<RaiseLog> Machine Program-0 raised Event ____E
<StateLog> Machine Program-0 entering State Call
<StateLog> Machine Program-0 exiting State Call

Error:
P Assertion failed:
Expression: assert(tmp_2.bl,)
Comment: (18, 11): Assert failed
******************************/
