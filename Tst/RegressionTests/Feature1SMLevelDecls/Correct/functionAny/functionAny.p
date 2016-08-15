event x;
machine Main {
	var x1 : int;
	start state Init {
		entry {
		foo(1, 3, x);	
		}
	}
	
	fun foo (x : any, y : int, z : event) : int {
    return 0;
	}

}
