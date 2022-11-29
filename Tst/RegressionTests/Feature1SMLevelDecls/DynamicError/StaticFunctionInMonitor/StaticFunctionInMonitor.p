fun foo() {
	var x: machine;
	assert false;
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
			send this, e1;
		}
	}
}

test DefaultImpl [main=Main]: assert M in {Main};