event e1;

spec M observes e1 {
	start state Init {

	}

	fun foo(x: machine) {
		send x, e1;

		receive {
			case e1: {}
		}

		new Main();
	}
}

machine Main {
	start state Init {

	}
}