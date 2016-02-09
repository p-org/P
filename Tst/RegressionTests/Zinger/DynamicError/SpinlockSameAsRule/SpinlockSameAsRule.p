// Monitor with typestate table
// This P program matches the semantics of the original WDF Spinlock rule
// In the new version of P which does not allow dynamic creation of
// monitors, typestate is maintained in the "typstate" table,
// exactly as in XDV implementation
// Two guards are explored: 227 states to reach all Aborts
//event START assume 1;
event ACQ assume 2: int;
event REL assume 2: int;
event FIN assume 1;
event Error1 assume 1;
event Error2 assume 1;
event Error3 assume 1;
event Halt assume 1;

spec Spinlock monitors ACQ, REL, FIN, Error1, Error2, Error3 {
//tpstate is a map from guards to stvar s value: false: init, true: locked
//Upon creation of the Spinlock monitor, tpstate is initialized as "empty"
//What would be good is to have tpstate initialized with all "false",
// - then we wouldn't need checks on tpstate all the time
var tpstate: map[int,bool]; 
var ev_guard: int; 
var i, k: int; 
var me: int;
	start state Init {
		//entry (payload: int) {
			//me = payload;   //guard value for which this monitor is instantiated
		//}
		//on START do {
		//	assert (!(me in tpstate));             //never fails
		//	tpstate[me] = false;
		//};
		on ACQ do (payload: int) {
			ev_guard = payload;
			me = payload;
			//if (ev_guard == me) {
				//1. Typestate for "me" has not been set up yet: set up the typestate for "me"
				if (!(me in tpstate)) { tpstate[me] = false; };
				//2. Typestate for "me" was set up already:            
				if (tpstate[me] == true) { raise Error1; }     //double acquire
				else  { tpstate[me] = true; };       //acquire lock
			//}
		};
		on REL do (payload: int) {
			ev_guard = payload;
			me = payload;
			assert (me in tpstate);
			//Check that the watch is set up and test the guard:
			//if (me in tpstate && ev_guard == me) {
			if (tpstate[me] == false){ raise Error2; }
			else { raise Halt; };
			//}
		};
		on FIN do {
			//checking that all *initialized* stvars are false upon FIN:
			i = 0;
			while (i < sizeof(keys(tpstate))) {
				if (tpstate[keys(tpstate)[i]] == true) { raise Error3; };
				i = i + 1;
			}
			raise Halt;
		};
		on Halt goto HaltState;
		on Error1 goto Abort1;
		on Error2 goto Abort2;
		on Error3 goto Abort3;
	}
	//TODO: default behavior of "halt" should work instead
	state HaltState {
		ignore ACQ, REL, FIN;
		//raise halt;
	}
	state Abort1 {
		entry { 
		    //New version: re-tested reachability for [acq0,acq0]
			//reachable for: [acq1,acq1]; [acq0,acq0];
			// [acq1,acq0,rel0,acq0,acq1] (monitor #1)
			assert(false);   
		}      
		ignore ACQ, REL, FIN;
	}
	// It is important to keep all asserts different - otherwise, Zing reports only one line 
	// number for all asserts (known issue)
	state Abort2 {
		entry { 
			// Not reachable: bug in the original rule!!!
			assert(false);;  
		}      
		ignore ACQ, REL, FIN;
	}
	state Abort3 {
		entry { 
		//New version: re-tested reachability for:
		//[acq0,fin1], [acq1,fin1], [acq0,fin0], [acq1,acq0,fin0], [acq0,acq1,fin0],
		//[acq0,acq1,fin1],[acq1,acq0,fin1],[acq1,rel0,fin1],[acq0,rel1,fin0],
		//[acq1,rel0,fin0],
			// ???[acq1,rel1,acq0,fin0] (monitor #0 ONLY)
			//assert(false);;;  
		}      
		ignore ACQ, REL, FIN;
	}
}

main machine TestDriver {
var ev: event;
var par: int;
var mon, mon0, mon1: machine;
	start state Init {
		entry {
		    //new Spinlock(0);
			// Manual harnesses to test simplest scenarios, with a single monitor;
			// Uncomment scenarios one-by-one to test (while commenting out non-determ harness)
			// TODO: how to use Zinger option "-m" to test everything at once? ("assumes" to be increased)
			// manual test #1: REL, FIN  (release with no acquire; Abort2 is NOT reached )
			//monitor Spinlock, REL, 0;
			//monitor Spinlock, FIN;
			// manual test #2: ACQ, ACQ  (acq with no release: Abort1 is reached)
			//monitor Spinlock, ACQ, 1;
			//monitor Spinlock, ACQ, 1;
			// manual test #3: ACQ, FIN  (FIN with unreleased lock: Abort3 is reached)
			//monitor Spinlock, ACQ, 0;
			//monitor Spinlock, FIN;
			// manual test #4: ACQ, REL, REL (double release: Abort2 is NOT reached)
			//monitor Spinlock, ACQ, 0;
			//monitor Spinlock, REL, 0;
			//monitor Spinlock, REL, 0;
			// generating random sequences with "assume 2" limits
			/*********************/
			//new Spinlock(1);
			//monitor Spinlock, START;
			while ($) {
				par = ChooseGuard();
				ev = ChooseEvent();
				monitor ev, par;
			}
			/************************/
		}
	}
	fun ChooseGuard() : int {
		if ($) { return 0; }
		else { return 1; }
	}
	fun ChooseEvent() : event {
		if ($) { return ACQ; }
		else if ($) { return REL; }
		else { return FIN; };
	}
}