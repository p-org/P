//"send" in function which mutates its parameters
//the test checks that function state is preserved in
//function execution is interrupted by a scheduled event

event Ping assert 1 : machine;
event Ping1 assert 2 : int;
event Pong assert 1;
event Success;

main machine PING {
    var pongId: machine;
    var x: int;
	var y: int;
    start state Ping_Init {
        entry {
  	    //pongId = new PONG();
	    raise Success;   	   
        }
        on Success do {
			x = Func1(1, 1);
			assert (x == 2);
		};
		on default do {	
		    assert (x == 2);
			y = Func2(x);   //x = 1
			
		};
		on Ping1 do { assert(x == 3); };
    }
	//int: value passed; j: identifies caller (1: Success handler;  
	//2: Func2
	fun Func1(i: int, j: int) : int {
		//i = 1;
		//send this, Ping1, i;
		//assert (i == 1);
		if (j == 1) {     
			i = i + 1;       //i: 2
			return i;
		};
		if (j == 2) {
			assert(i == 2);
			i = i + 1;
			assert (i == 3);
			send this, Ping1, i;
			assert (i == 3);
			return i;
		}
	}
	fun Func2(v: int) : int {
		x = Func1(v, 2);
		assert ( x == 3);
	}
}

machine PONG {
    start state Pong_WaitPing {
        entry { }
        on Ping goto Pong_SendPong;
    }

    state Pong_SendPong {
	entry {
	     send payload as machine, Pong;
	     raise Success;		 	  
	}
        on Success goto Pong_WaitPing;
    }
}
