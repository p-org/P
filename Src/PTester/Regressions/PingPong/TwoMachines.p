//Testing:
//two machines; global static funs and local funs;
//goto transitions with/without  functions
//do declarations, ignored events
//deferred events
//null transition
//warm/hot/cold states

event Ping assert 1: machine;
event Pong assert 1: int;
event Success: bool;
event Fail;

fun F1() {
	//send this, Ping;
	//send this, Pong;
}
fun F2() {
	//send this, Ping;
	//send this, Pong;
}

//machine Main assume 222
machine Main
{
    //var pongId: machine;

    start hot state Init {
        entry {
			//pongId = new PONG();
	        //raise Success;   	   
        }
        on Success goto Ping_SendPing with F2;
		ignore Fail;
		on null goto Ping_WaitPong;
    }

    cold state Ping_SendPing {
        entry {
			//send pongId, Ping, this;
	        //raise Success;
	    }
        on Success goto Ping_WaitPong with foo;      //foo used 1st time in goto
		on Pong do foo;                           //foo used 1st time in "do"
		defer Fail;
     }

     state Ping_WaitPong {
        on Pong goto Ping_SendPing with foo;   //foo used 2nd time in goto
		on Success do {}
     }

     state Done {
		on Pong do { foo(); }          //foo used 2nd time in "do"
		on Success do { assert(false); }
		ignore Fail;
	 }

	 fun foo() {}
}

machine PONG assume 111 {
	start state Pong_WaitPing {
        entry { }
        on Ping goto Pong_SendPong with F1;
		ignore Success;
    }

    state Pong_SendPong {
	entry (payload: machine) {
	     //send payload, Pong;
	     //raise Success;		 	  
	}
        on Success goto Pong_WaitPing with foo;
		on Ping do F1;
		defer Fail;
    }
	fun foo() {}
}