machine Main {
	start state S {
		entry {
			var x: machine;
			var y: any;
			y = (m1 = this, m2 = null);
			x = (y as (m1: machine, m2: machine)).m2;
			y = (m1 = this, m2 = this);
			assert (x != this);
		}
	}
}
