event x;
main machine TestMachine {
	var x : int;
	start state Init {
		entry {
			x = x + 1;
		}
	}
	
	fun foo (x : event) {
		send this, x;
	}

}

machine Xsender {

	start state Init {
		entry {
			send this, x;
		}
	}

}