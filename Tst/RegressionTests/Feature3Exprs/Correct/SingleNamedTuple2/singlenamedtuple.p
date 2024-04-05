machine M {
        start state _ {
		entry (deps: (m: machine)) {
		//entry (m: machine) {
		}
	}
}

machine Main {
	start state _ {
		entry {
			new M((m = this, ));
			//new M(this);
		}
	}
}