event a;
event b;
event c: int;

fun F1(m: machine)
{
	var mInt : map[int, int];
	mInt[0] = 10;
	send m, c, mInt[0];
}

fun F2(m: machine) {
	send m, a;
	send m, b;
}

machine Main {
	start state S {
		entry {
			raise a;
		}
		on a goto S1 with F2_wrap;
	}
	
	state S1 {
		entry F2_wrap;
		on a do {
			receive {
				case b: { assert(false);}
			}
		}
	}
	
	fun F2_wrap()
	{
		F2(this);
	}
}
