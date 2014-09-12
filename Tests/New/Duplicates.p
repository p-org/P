event x;
event x: int;
event a;
event b;
event c;

main machine m1 {
	start state S1 {
	
	
	}
	


}

main machine m1 {
	var x : int;
	var x : bool;


}

machine m2 {
	
	state S1 {
	
	on a goto S2;
	on b goto S1;
	on a goto S1;
	
	}
	
	state S2 {
	
		on a do {};
		on a do { assert(false); };
	
	}
	
	

}