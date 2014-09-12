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