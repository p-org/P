event e1;
event e2;

spec Invariant1 observes e1 {
	start state Init {
		on e2 goto Init;
	}
}

machine Main {
	start state Init {
	
	}
}