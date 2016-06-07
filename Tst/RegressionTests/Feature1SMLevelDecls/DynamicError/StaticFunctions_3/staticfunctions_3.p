event a;
event b;
event c: int;

fun F1() 
{
	var mInt : map[int, int];
	mInt += (0, 10);
	send this, c, mInt[0];
}

fun F2() {
	send this, a;
	send this, b;
}

main machine M {
	start state S {
		entry {
			raise a;
		}
		on a goto S1 with F2;
	}
	
	state S1 {
		entry F2;
	on a do {
		receive {
			case b: { assert(false);}
		}
	}
	}
	
}
