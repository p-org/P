event x;
main machine TestMachine {
	var x1 : int;
	start state Init {
		entry {
<<<<<<< HEAD
			x = foo(1);
			
		}
	}
	
	fun foo (x : event) {
=======
			x = foo();
		}
	}
	
	fun foo (x : event) : bool {
>>>>>>> 16f266a540d4f3390b69c6dd38952bbc1658b46b
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