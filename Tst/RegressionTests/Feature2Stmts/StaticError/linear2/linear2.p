machine Main {
	var a: int;
	start state Init {
		entry {
			if (A(a))
			{
				a = a + 1;
			}
			if (B(a))
			{
				a = a - 1;
			}
			if (C(a))
			{
				a = a + 1;
			}
		}
	}

	fun Foo(x: int) {}

	fun A(y: int) : bool 
	{ 
		var b: int;
		Foo(b xfer); 
		return true;
	}

	fun B(y: int) : bool 
	{ 
		var b: int;
		b = y xfer;
		return true;
	}

	fun C(y: int) : bool 
	{ 
		Foo(y xfer); 
		return true;
	}
}

