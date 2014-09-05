event x;
main machine TestMachine {
	var x1 : int;
	start state Init {
		entry {
			x = foo(1);
			
		}
	}
	
	fun foo (x : event) {
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