event Empty   assert 1;
event a;
event b;
event c;
event d;
event e;


machine Node {
	var x: int;
	start state Init {
		
		entry {
			x = x + 1;
			send this, a;
		}
		on a do foo;
		on b do { assert(false);};
		on c goto xyz;
		on d goto xyz with {};
		on e goto xyz with bar;
	}
	
	action foo {
		x = x + 1;
		send this, b;
	}

	fun bar () {
	
	}
	state xyz {
	
	}
	
}
