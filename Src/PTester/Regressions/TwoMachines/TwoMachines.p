//Testing:
//two machines; global static funs and local funs;
//goto transitions with/without  functions
//do declarations, ignored events
//deferred events
//null transition
//warm/hot/cold states
//types
//variables: machine locals, function locals (including formal pars)

//type nmdtuple = (a: int, b: int);
//type tpl = (int, bool);
event Ping assert 1: (a:int, b:bool);
//event Ping assert 1: int;
event Pong assert 1: seq[int];
event Success: bool;
event Fail: (int, bool);
event NotUsed: map[int,bool];
//event NotUsed: seq(seq(int));
//event Fail: tpl;
//event Fail: int;
//event Fail: nmdtuple;

fun F1(par1: int, par2: bool) {
	var varInt: int;
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
    var pongId: machine;
	var varInt: int;
	var varBool: bool;
	var varTpl: (int, bool);
	var varNmdTpl: (a:int, b:int);
	//var varTpl: int;

    start hot state Init {
        entry {
			//pongId = new PONG();
	        //raise Success;   	   
        }
        //on Success goto Ping_SendPing with {F1(varInt, varBool);}
		on Fail goto Ping_WaitPong;
		on null goto Ping_WaitPong;
    }

    cold state Ping_SendPing {
        entry {
			//send pongId, Ping, this;
	        //raise Success;
	    }
        //on Success goto Ping_WaitPong with {foo(varBool, varTpl);}      //foo used 1st time in goto
		//on Pong do {foo(varBool, varTpl);}                         //foo used 1st time in "do"
		defer Fail;
     }

     state Ping_WaitPong {
        //on Pong goto Ping_SendPing with {foo(varBool, varTpl);}   //foo used 2nd time in goto
		on Success do {}
     }

     state Done {
		//on Pong do { foo(varBool, varTpl); }          //foo used 2nd time in "do"
		//on Success do { assert(false); }
		ignore Fail;
	 }

	 //fun foo(par1: bool, par2: nmdtuple) {}
	 //fun foo(par1: bool, par2: (int, bool)) {}
	 fun foo(par1: bool, par2: int) {}
}

machine PONG assume 111 {
	start state Pong_WaitPing {
        entry { }
        //on Ping goto Pong_SendPong with {F2();}
		ignore Success;
    }

    state Pong_SendPong {
	entry (payload: machine) {
	     //send payload, Pong;
	     //raise Success;		 	  
	}
        //on Success goto Pong_WaitPing with foo;
		on Ping do F2;
		defer Fail;
    }
	fun foo() {}
}