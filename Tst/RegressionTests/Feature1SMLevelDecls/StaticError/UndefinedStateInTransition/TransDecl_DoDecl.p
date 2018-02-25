event a;
event b;
machine Main {
	start state X1 {
		entry {
			foo(5);

		}
		
		exit {

		}
		on null goto X2 with { }
	}

	fun foo(x : int) {
		
	}
}