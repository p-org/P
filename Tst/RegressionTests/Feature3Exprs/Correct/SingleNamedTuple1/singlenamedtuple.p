type T = (source: machine);
//type T = machine;

event E: T;

machine M {
        start state _ {
		entry (m: machine) {
			send m, E, (source = this, );
			//send m, E, this;
		}
	}
}

machine Main {
	start state _ {
		ignore E;

		entry {
			new M(this);
		}
	}
}