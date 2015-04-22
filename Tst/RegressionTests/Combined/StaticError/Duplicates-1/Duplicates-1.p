//Duplicates: static errors:
//multiple: main machines, transitions/actions over the same event, no start sate

event x;
//event x: int;
event a;
event b;
event c;

main machine m1 {
	start state S1 {
	}
}

main machine m3 {
	var x : int;
}

machine m2 {	
	state S1 {	
	on a goto S2;
	on b goto S1;
	on a goto S1;
	on x do { foo(); };	
	}
	state S2 {	
		on a do {};
		on a do { assert(false); };	  //unreachable
	}
	fun foo() {}
}