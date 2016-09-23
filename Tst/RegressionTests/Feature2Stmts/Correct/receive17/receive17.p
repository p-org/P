include "temp.p"

event res: int;
event getres: machine;
event e1;
event e2;
event e3;
machine Main {
	var local: machine;
    start state S {
		entry {
			local = new N();
			Dummy(local);
			
			receive {
				case e1: { assert false; }
			}
			receive {
				case getres: (payload: any){ assert true; }
			}
		}
	}
}

