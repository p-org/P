//(16, 1): An event has multiple definitions
//(17, 1): An event has multiple definitions
//(22, 1): Multiple machines are declared as main machines
//(22, 1): Multiple machines with the same name
//(32, 1): Multiple machines are declared as main machines
//(32, 1): Multiple machines withthe same name
//(32, 1): no start state in machine
//(33, 6): Multiple variables with the same name
//(34, 6): Multiple variables with the same name
//(39, 1): no start state in machine
//(43, 2): Multiple Transitions over the same event
//(45, 2): Multiple Transitions over the same event
//(51, 3): Multiple actions over the same event
//(52, 3): Multiple actions over the same event

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
	on x do { foo(); };
	
	}
	
	state S2 {
	
		on a do {};
		on a do { assert(false); };
	
	}
	fun foo() {}
	fun foo() :int { return 1; }
	

}