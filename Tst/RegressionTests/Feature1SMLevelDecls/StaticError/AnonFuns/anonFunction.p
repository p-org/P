event a;
event b;
main machine Sample {
	start state X1 {
		entry {
			foo();
		}
		
		exit {

		}
		on null goto X2 with { }
		on a goto X1 with { }
	}

	fun foo() {
		
	}
}

machine Sample2 {
	start state X1 {
		
		on null do {}
	
	}

}