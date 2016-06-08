event unit;
main machine TestMachine {
	var x : int;
	start state Init {
		entry {
			x = 0;
			raise(unit);
		}
		on unit goto test;
	}
	
	state test {
		entry {
			if($)
			{
				x = x + 1;
			}
			else
			{
				x = 1;
			}
			assert(x < 3);
			if(x < 10)
				send this, unit;
				
			
		}
		on unit goto test;
	}
	
}

