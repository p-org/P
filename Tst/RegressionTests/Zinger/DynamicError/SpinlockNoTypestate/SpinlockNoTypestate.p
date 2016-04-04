// no typestate, no guard	
// For such rules, no need to have Spinlock as a monitor -
// hence, inheritance with "push" can be used.
// Checked that all asserts are reached one-by-one,
// when running Zinger with '-m" option 

event ACQ assume 2;
event REL assume 2;
event FIN assume 1;               //no point in multiple FIN
event Error1 assume 1;
event Error2 assume 1;
event Error3 assume 1;
event Halt assume 2;

main machine Harness {
var ruleMach: machine;
var ev: event;
start state Init {
	entry {
		ruleMach = new Rule();
		while ($) {
			ev = ChooseEvent();
			send ruleMach, ev;
		}
	}
}
fun ChooseEvent() : event {
	if ($) { return ACQ; }
	else if ($) { return REL; }
	else { return FIN; }
}
}

machine Rule {
var stvar: int;
start state Start {
	entry {
		stvar = 0;
	}
	on ACQ push Acquire;
	on REL push Release;
	on FIN push Finish;
}
	state Acquire {
		entry {
		if (stvar == 0) {stvar = 1;}
		else {raise Error1;}
		}
		on Error1 goto Abort1;
		on null do {pop;}
	}
	state Release {
		entry {
			if (stvar == 1) { raise Halt; }
			else {raise Error2;}
		}
		on Error2 goto Abort2;
		on Halt goto OK;
		on null do {pop;}
	}
	state Finish {
		entry {
			if (stvar == 1) {raise Error3;}
		}
		on Error3 goto Abort3;
		on null do {pop;}
	}
	// reachable for double acquire
	state Abort1 {
		entry { 
			assert(false);   //reachable for: [acq,acq]; [acq,acq,fin,rel,rel], etc.
		}      
		ignore ACQ;
		ignore REL;
		ignore FIN;
	}
	// reachable for release with no acquire
	state Abort2 {
		entry { 
			assert(false);; //reachable for [rel]; [rel;acq]; [fin,rel]; [rel,fin]; [rel;rel];
			                //[fin,rel,fin,rel,acq], etc.
		}   
		ignore ACQ;
		ignore REL;
		ignore FIN;
	}
	// reachable for Finish after Acquire with no Release
	state Abort3 {
		entry { 
			assert(false);;;   //reachable for: [acq,fin]; [acq,fin,rel,rel,acq,acq], etc.
		}      
		ignore ACQ;
		ignore REL;
		ignore FIN;
	}
	state OK {
		entry {  }
		ignore ACQ;
		ignore REL;
		ignore FIN;
	}
}