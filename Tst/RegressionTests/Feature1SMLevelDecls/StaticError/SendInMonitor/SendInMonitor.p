event e1;

spec Main observes e1 {

	fun foo(x: machine) {
		send x, e1;
	}

	start state Init {
		entry {
			var x: machine;
			send x, e1;
		}
	}
}