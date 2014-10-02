//(35, 4): "if (...)" expects a boolean value
//(35, 7): Nondeterminitistic choice can be used only in model machine and model functions
//(50, 4): "if (...)" expects a boolean value
//(50, 7): Nondeterminitistic choice can be used only in model machine and model functions
//(65, 2): model functions can be declared only in real machines
//(94, 4): "if (...)" expects a boolean value
//(94, 7): Nondeterminitistic choice can be used only in model machine and model functions
//(101, 2): model functions can be declared only in real machines
//(108, 3): "if (...)" expects aboolean value
//(108, 6): Nondeterminitistic choice can be used only in model machine and model functions

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

