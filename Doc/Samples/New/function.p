//(13, 7): invalid assignment. right hand side is not a subtype of left hand side
//(13, 7): invalid LHS; must have the form LHS ::= var | LHS[expr] | LHS.name
//(13, 11): function requires arguments
//(19, 5): return value has incorrect type
//(27, 5): function must return a value
//(35, 5): anonymous function cannot return a value

event x;
main machine TestMachine {
	var x1 : int;
	start state Init {
		entry {
      x = foo();
			
		}
	}
	
	fun foo (x : any, y : int) : int {
    return true;
	}

	fun foo1 (x : any, y : int) : int {
    return y;
	}

	fun foo2 (x : any, y : int) : int {
    return;
	}
}

machine Xsender {

	start state Init {
		entry {
		  return 3;
			send this, x;
		}
	}

}