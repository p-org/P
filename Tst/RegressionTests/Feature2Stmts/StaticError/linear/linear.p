event E: int;

machine Main {
	var g: int;

	fun G(a : int) : int {
		return 0;
	}
	
	fun F(a : int, b : int) {
	    a = a + 1;
	}
	
	start state S {
	    entry {
			var y: int;
			var g_local: int;
			assert y == 0;
			g = g_local swap;
			F(g_local swap, y move);
			g = g_local move;
			assert g == 1;
			send this, E, y;
			if (G(g) == 0)
			{
				y = 1;
			}
			else
			{
				y = 0;
			}
			y = G(y move);
			assert y == 0;
		}
	}
}
