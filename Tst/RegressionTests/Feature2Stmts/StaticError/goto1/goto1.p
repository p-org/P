//goto statement
//TODO: "swap not allowed"; what about "move"?
//Error rules for goto.
//	[rule_Classes = '"error, msg: undefined state"']
//	TypeOf(c, e, ERROR) :- SubSE(c, e), e = Goto(_, _, _), c.owner = NIL.
//	[rule_Classes = '"error, msg: undefined state"']
//	TypeOf(c, e, ERROR) :- SubSE(c, e), e = Goto(dst, _, _), c.owner != NIL, no StateDecl(dst, c.owner, _, _, _, _).
//	[rule_Classes = '"error, msg: invalid payload type in goto"']
//	TypeOf(c, e, ERROR) :- SubSE(c, e), e = Goto(dst, _, _), c.owner != NIL, s = StateDecl(dst, c.owner, _, _, _, _),
//						   PayloadVar(s, _, _, ft), TypeOfArg1(c, e, et),
//						   TypeRel(et, ft, k), et != ft, k != SUB.

event E;
event E1: int;

fun F1(m: machine)
{
	var mInt : map[int, int];
	mInt[0] = 10;
	send m, E1, mInt[0];
	goto UndefState;            //error: "undefined state"
}

machine Main {
	   start state Init {
		  entry {
			 goto T, true;     //error: "invalid payload type in goto"
			 goto S;
			 new X(0);
		  }
		  on E do { goto UndefState; }   //error: "undefined state"
		  //exit { goto S; }
	   }

	   state S {
	   }

	   state T {
	      entry (x: int) {}
	   }
}

machine X {
	   start state Init {
	
	   }
}
