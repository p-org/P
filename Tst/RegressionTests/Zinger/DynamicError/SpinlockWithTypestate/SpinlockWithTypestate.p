// Design 1: announce with typestate table
// Two guards are explored: 227 states to reach all Aborts
event START assume 1;
event ACQ assume 2: int;
event REL assume 2: int;
event FIN assume 1;
event Error1 assume 1;
event Error2 assume 1;
event Error3 assume 1;
event Halt assume 1;

spec Spinlock observes ACQ, REL, FIN, Error1, Error2, Error3 {
//tpstate is a map from guards to stvar s value: false: init, true: locked
var tpstate: map[int,bool]; 
var ev_guard: int; 
var i, k: int; 
var me: int;
	start state Init {
		//entry (payload: int) {
			//me = payload;   //guard value for which this announce is instantiated
		//}
		//on START do {
			//assert (!(me in tpstate));             //never fails
			//tpstate[me] = false;
		//}
		on ACQ do (payload: int) {
			ev_guard = payload;
			me = payload;
			//if (ev_guard == me) {
				assert (me in tpstate);               
				if (tpstate[me] == false) { tpstate[me] = true; }
				else  { raise Error1; }
			//}
		}
		on REL do (payload: int) {
			ev_guard = payload;
			me = payload;
			assert (me in tpstate);
			//if (ev_guard == me) {
				//assert (me in tpstate);;             //never fails
				if (tpstate[me] == true) { raise Halt; }
				else { raise Error2; }
			//}
		}
		on FIN do {
			//checking that all initialized stvars are false upon FIN:
			i = 0;
			while (i < sizeof(keys(tpstate))) {
				if (tpstate[keys(tpstate)[i]] == true) { raise Error3; }
				i = i + 1;
			}
			raise Halt;
		}
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
			//reachable for: [start,acq1,acq1]; [start,acq0,acq0];
			// [start,acq0,rel0,acq1,acq0,acq1] (announce #1)
			assert(false);   
		}      
		ignore ACQ, REL, FIN;
	}
	// It is important to keep all asserts different - otherwise, Zing reports only one line 
	// number for all asserts (known issue)
	state Abort2 {
		entry { 
			//reachable for: [start,rel1]; [start,acq1,rel0] (announce #0)
			// [start,acq1,rel1,acq1,rel0] (announce #0)
			assert(false);;  
		}      
		ignore ACQ, REL, FIN;
	}
	state Abort3 {
		entry { 
			//reachable for: [start,acq1,fin0] (announce #1 ONLY); 
			// [start,acq1,fin1]
			// [start,acq1,rel1,acq0,acq1,fin0] (announce #0 ONLY)
			assert(false);;;  
		}      
		ignore ACQ, REL, FIN;
	}
}

machine Main {
var ev: event;
var par: int;
var mon, mon0, mon1: machine;
	start state Init {
		entry {
		    //new Spinlock(0);
			// Manual harnesses to test_ simplest scenarios, with a single announce;
			// Uncomment scenarios one-by-one to test_ (while commenting out non-determ harness)
			// TODO: how to use Zinger option "-m" to test_ everything at once? ("assumes" to be increased)
			// manual test_ #1: REL, FIN  (release with no acquire; Abort2 is reached )
			//announce Spinlock, START;
			//announce Spinlock, REL, 0;
			//announce Spinlock, FIN;
			// manual test_ #2: ACQ, ACQ  (acq with no release: Abort1 is reached)
			//announce Spinlock, START;
			//announce Spinlock, ACQ, 1;
			//announce Spinlock, ACQ, 1;
			// manual test_ #3: ACQ, FIN  (FIN with unreleased lock: Abort3 is reached)
			//announce Spinlock, START;
			//announce Spinlock, ACQ, 0;
			//announce Spinlock, FIN;
			// manual test_ #4: ACQ, REL, REL (double release: no abort is reached!)
			//announce Spinlock, START;
			//announce Spinlock, ACQ, 0;
			//announce Spinlock, REL, 0;
			//announce Spinlock, REL, 0;
			// generating random sequences with "assume 2" limits
			//new Spinlock(1);
			//announce START;
			while ($) {
				par = ChooseGuard();
				ev = ChooseEvent();
				announce ev, par;
			}
		}
	}
	fun ChooseGuard() : int {
		if ($) { return 0; }
		else { return 1; }
	}
	fun ChooseEvent() : event {
		if ($) { return ACQ; }
		else if ($) { return REL; }
		else { return FIN; }
	}
}
