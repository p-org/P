//Duplicates: static errors:
//multiple: main machines, transitions/actions over the same event, no start sate

event x;
event a;
event b;
event c;

machine Main {	
	start state S1 {	
	on a goto S2;
	on b goto S1;
	on a goto S1;
	on x do { foo(); }	
	}
	state S2 {	
		on a do {}
	}
	fun foo() {}
}
