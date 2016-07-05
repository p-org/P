event E: int;

main machine M {
	var g: int;
    
	fun G(a : int) : int { 
		return 0; 
	}
	
	fun F(a ref: int, b : int) {
	    a = a + 1;
	}
	
	start state S {
	    entry {
			var y: int;
			assert y == 0;
			F(g ref, y xfer);
			assert g == 1;
			if (G(g) == 0)
			{
				y = 1;
			} 
			else
			{
				y = 0; 
			}
			y = G(y xfer);
			assert y == 0;
		}
	}
}
