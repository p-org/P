event Empty   assert 1;
event a;
event b;
event c;
event d;
event e;


machine Node {

	start state Init {
		entry {
			//x = x + 1;
		}
		on a do foo;
		on b do {};
		on c goto xyz;
		on d goto xyz with {};
		on e goto xyz with bar;
	}
	
	action foo {
		x = x + 1;
	}

	fun bar () {
	
	}
	state xyz {
	
	}
	
}
