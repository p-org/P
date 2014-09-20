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
	
	action foo {
			if($)
				x = x + 1;
	}

	model fun bar () {
		if($)
			x = x + 1;
	}
	state xyz {
		entry foo;
		exit bar;
	}
	
	state s2 {
		entry {
			if($)
				x = x + 1;
		}
	
	}
	
}

model xx {
	var x : int;
	fun foo (){
		if($)
			x = x + 1;
	}
	
	model fun bar (){
		if($)
		{
			x = x + 1;
		}
	}
	
	start state init {
		entry bar;
		exit {
			if($)
				x = x + 1;
		}
		
		on default goto init with {
			if($)
			{
				x = x + 1;
			}
		};
	}

}

monitor MonitorMe {
	var x : int;
	
	start state init {
		entry {
			if($$)
			{
				x = x + 1;
			}
		}
	}
	
	model fun foo () {
		if($) {
			x = x + 1;
		}
	}
	
	fun bar () {
		if($)
			x = x + 1;
	}
	
}

