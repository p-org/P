event x;
event y : seq [(a: int, a: int, a: int, a: int)];
machine Main {
  fun foo(x : event, x: event) {
		return true;
		//This is the correct return stmt (inferred return type is NIL):
		//return;
	}

/*
	fun foo1 (x : int, x: int) {
		return true;
	} */
}

machine Xsender {

	start state Init {
		entry {
      send this, x, (a = 1, a = 2);
	  send this, y, (a = 1, a = 2);
		}
	}

}
