fun foo() {
	var x: machine;
	send x, e1;
}

event e1;

spec M observes e1 {
	
	start state Init {
		entry {
			foo();
		}

	}
}

machine Main {
	start state Init {
		entry {
			foo();
		}
	}
}

test X [main=Main]: assert M in {Main};