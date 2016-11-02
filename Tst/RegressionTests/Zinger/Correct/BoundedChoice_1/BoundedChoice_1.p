event unit;
machine Main {
	var x : int;
	start state Init {
		entry {
			x = 0;
			raise(unit);
		}
		on unit goto XYZ;
	}
	
	state XYZ {
		entry {
			if($)
			{
				x = x + 1;
			}
			else
			{
				x = 1;
			}
			assert(x < 4);
			if(x < 10)
				send this, unit;
				
			
		}
		on unit goto XYZ;
	}
	
}

