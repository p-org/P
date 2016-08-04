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
		on a goto X1 with foo;
		on b goto X1 with bar;
		on c do { }                   //error
		on a do bar;                  //error
		on b do foo;                  //error
		on b push X3;
		on c push X4;
		on d goto X2;
	}

	fun foo(x : int) {
		
	}
}

machine Sample2 {
	
	fun bar() {
	
	}
	
	start state X2 {

	on null goto X1 with bar;
	on a do bar;
	
	}

}
