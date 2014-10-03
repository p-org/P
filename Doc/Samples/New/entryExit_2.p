//(40, 2): function cannot take arguments
//(45, 2): function cannot take arguments
//(52, 8): undefined function for exit

event Empty   assert 1;
event a;
event b;
event c;
event d;
event e;


main machine Node {
	var x: int;
	start state Init {
		entry {
			x = x + 1;
		}
		on a do foo;
		on b do {};
		on c goto xyz;
		on d goto xyz with {};
		on e goto xyz with bar;
		exit {}
	}
	
	fun foo() {
		x = x + 1;
	}

	fun bar () {
	
	}
	
	fun football (x : int) {
	
	}
	
	
	state xyz {
		entry football;
		exit bar;
	}
	
	state abc {
		entry {}
		exit football;
	}
	
	state cde {
		entry {}
		exit x;
	
	}
	
}
