event x;
main machine TestMachine {
	var x1 : int;
	start state Init {
		entry {
			x = foo();
		}
	}
	
	fun foo (x : event) : bool {
		return true;
	}

}

machine Xsender {

	start state Init {
		entry {
			send this, x;
		}
	}

}