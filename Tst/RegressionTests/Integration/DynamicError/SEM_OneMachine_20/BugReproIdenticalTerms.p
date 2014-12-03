//Exit function performed while explicitly popping the state
//Zinger line number reporting is wrong
//Workaround: see BugReproIdenticalTerms_workaround.p
event E;
main machine Program {
	start state Init {
		entry { raise E; }
		exit { assert (false); }  //this line is wrongly reported in the error trace
		on E push Call;
	}

	state Call {
		   entry { 
			   
				       pop; 					   
			}
			exit { assert (false);}  //this is the line that should be reported
	}
}
/*************************  Expected result:
Safety Error Trace
Trace-Log 0:
<CreateLog> Created Machine Program-0
<StateLog> Machine Program-0 entering State Init
<RaiseLog> Machine Program-0 raised Event ____E
<StateLog> Machine Program-0 entering State Call
<StateLog> Machine Program-0 exiting State Call

Error:
P Assertion failed:
Expression: assert(tmp_1.bl,)
Comment: (8, 10): Assert failed
******************************/