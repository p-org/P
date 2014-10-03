//Expected: syntax error "anonfunction.p (13, 3): transition to an undefined state"
event a;
event b;
main machine Sample {
	start state X1 {
		entry {
			foo();
		}
		
		exit {

		}
		on default goto X2 with { };
		on a goto X1 with { };
	}

	fun foo() {
		
	}
}

machine Sample2 {
	start state X1 {
		
		on default do {};
	
	}

}