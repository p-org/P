event x;
machine Main {
	var x1 : int;
	start state Init {
		entry {
		{ return 0; }	
		}
	}
	
	fun foo (x : any, y : int, z : event) : int {
    return 0;
	}

}
