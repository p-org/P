//xloc is passed as a swap parameter to the call.  Inside the call, however, xloc is represented by a //which is not available at the return because a has been moved to x.  The type checker complains //because a is expected to be available at return of foo.

machine Main {
	var x: int;

    start state S {
        entry {
			var xloc: int;
			foo(xloc swap); //error: "Parameter passed with swap not available at callee return"
		}
	}
	fun foo(a: int) {
		a = 1;
		x = a move;
		assert x == 1;
	}
}