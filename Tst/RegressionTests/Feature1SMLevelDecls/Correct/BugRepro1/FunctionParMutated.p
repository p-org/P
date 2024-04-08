//"send" in function which mutates its parameters
//the XYZ checks that function state is preserved if
//function execution is interrupted by a scheduled event ("send" in this XYZ)

event Ping assert 1 : int;
event Success;

machine Main {
    var x: int;
	var y: int;
    start state Ping_Init {
        entry {
	    raise Success;   	
        }
        on Success do {
			x = Func1(1, 1);
			assert (x == 2);
			y = Func2(x);     //x == 2
		}
		on Ping do (payload: int) {
			assert(x == 4);
			x = x + 1;
			assert (x == 5);
		}	
    }
	//i: value passed; j: identifies caller (1: Success handler;  2: Func2)
	fun Func1(i: int, j: int) : int {
		if (j == 1) {
			i = i + 1;       //i: 2
			return i;
		}
		if (j == 2) {
			assert(i == 3);
			i = i + 1;
			assert (i == 4);
			send this, Ping, i;
			assert (i == 4);
			return i;
		}
		assert false;
		return 0;
	}
	fun Func2(v: int) : int {
		v = v + 1;
		assert (v == 3);
		x = Func1(v, 2);
		assert ( x == 4);
		return x;
	}
}
