event Ping assert 1: machine;
event Pong assert 1;
event Success;

fun F1() {
	send this, Ping;
	send this, Pong;
}
fun F2() {
	send this, Ping;
	send this, Pong;
}

machine Main
{
    //var pongId: machine;

    start state Init {
        entry {
			//pongId = new PONG();
	        //raise Success;   	   
        }
        on Success goto Ping_SendPing with F2;
    }

    state Ping_SendPing {
        entry {
			//send pongId, Ping, this;
	        //raise Success;
	    }
        on Success goto Ping_WaitPong;
     }

     state Ping_WaitPong {
        on Pong goto Ping_SendPing;
     }

     state Done {}
}

machine PONG assume 111 {
	start state Pong_WaitPing {
        entry { }
        on Ping goto Pong_SendPong with F1;
    }

    state Pong_SendPong {
	entry (payload: machine) {
	     //send payload, Pong;
	     //raise Success;		 	  
	}
        on Success goto Pong_WaitPing;
    }
}