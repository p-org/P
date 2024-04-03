event x : (event, int);
event a : int;
event y;

machine Main {
		var id: machine;
		start state S
		{
			entry {
				raise x, (a, 3);
			}
                        on x do (payload: (event, int)) {
				raise payload.0, payload.1;
			}
		}
	}



