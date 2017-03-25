machine Main {
	var local: machine;
    start state S {
		entry {
			local = new N();
			Dummy(local, this);
			
			receive {
				case e1: { assert false; }
			}
			receive {
				case getres: (payload: any){ assert true; }
			}
		}
	}
}

