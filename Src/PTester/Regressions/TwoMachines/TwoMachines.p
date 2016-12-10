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

//enum enumType { e1, e2, e3 }

event Ping assert 1: machine;
//event Ping1 assert 1: nmdtuple;
//event Pong assert 1: seq[int];
//event Success: tpl;
//event Fail: map[any,any];
//event NotUsed: seq[seq[int]];
//event NotUsed2: map[int,map[int,any]];
//event NotUsed3: (a: seq [any], b: map[int, seq[any]]);
event boolPayloadEvent: bool;
event intPayloadEvent: int;

//fun F1(par1: tpl, par2: enumType) {
//fun F1(par1: int, par2: bool) {
	//var varInt: int;
	//send this, Ping;
	//send this, Pong;
//}
//fun F2() {
	//send this, Ping;
	//send this, Pong;
//}

//machine Main assume 222
machine Main
{
    var pongId: machine;
	var varInt: int;
	var varBool: bool;
	//var varTpl: (int, bool);
	//var varNmdTpl: nmdtuple;
	//var varEnum: enumType;
	//var varEnum2: enumType;

    start hot state Init {
        entry {
			send this, boolPayloadEvent, true;
			varBool = false;
			send this, boolPayloadEvent, varBool;
			//pongId = new PONG();
	        //raise Success;   	   
        }
        //on Success goto Ping_SendPing with {F1(varInt, varBool);}
		//on Fail goto Ping_WaitPong;
		//on null goto Ping_WaitPong;
    }

    //cold state Ping_SendPing {
        //entry {
			//varBool = true;
			//send this, Ping, this;
			//send this, boolPayloadEvent, varBool;
			//varInt = 20;
			//send this, intPayloadEvent, varInt;
			//send pongId, Ping, this;
	        //raise Success;
	    //}
        //on Success goto Ping_WaitPong with {foo(varBool, varTpl);}      //foo used 1st time in goto
		//on Pong do {foo(varBool, varTpl);}                         //foo used 1st time in "do"
		//defer Fail;
     //}

     //state Ping_WaitPong {
        //on Pong goto Ping_SendPing with {foo(varBool, varTpl);}   //foo used 2nd time in goto
		//on Success do {}
     //}

     //state Done {
		//on Pong do { foo(varBool, varTpl); }          //foo used 2nd time in "do"
		//on Success do { assert(false); }
		//ignore Fail;
	 //}

	 //fun foo(par1: bool, par2: nmdtuple) {}
	 //fun foo(par1: bool, par2: (int, bool)) {}
	 //fun foo(par1: bool, par2: int) {}
}

//machine PONG assume 111 {
	//start state Pong_WaitPing {
        //entry { }
        //on Ping goto Pong_SendPong with {F2();}
		//ignore Success;
    //}

    //state Pong_SendPong {
	//entry (payload: machine) {
	     //send payload, Pong;
	     //raise Success;		 	  
	//}
        //on Success goto Pong_WaitPing with foo;
		//on Ping do F2;
		//defer Fail;
    //}
	//fun foo() {}
//}