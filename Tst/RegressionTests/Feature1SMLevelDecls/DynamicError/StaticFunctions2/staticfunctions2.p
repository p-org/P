event a;
event b;
event c: int;

fun F1(m: machine)
{
	var mInt : map[int, int];
	mInt[0] =  10;
	send m, c, mInt[0];
}

fun F2(m: machine) {
	send m, a;
	send m, b;
}

fun F3()
{
	receive {
		case b : {
		receive {
			case a :
			{
				receive {
				case b : {assert(false);}
				}
			}
		}
	}
	}
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
		on a do { F3(); }
	}
	
	fun F2_wrap()
	{
		F2(this);
	}
}
