machine Main {
	var a: int;
	start state Init {
		entry {
			var x: int;
			a = 42;
			assert (A(a) == 42);
			assert (A(a) == 42);
			x = a;
			assert (A(x) == x);
			assert (A(A(x)) == A(A(a)));
			assert (A(3) == 3 && A(4) == 4);

			assert (A(A(x)) == A(B(x)));
		}
	}

	fun A(y: int) : int
	{
		return y;
	}

	fun B(y: int) : int
	{
		var b: int;
		b = y;
		return b;
	}

}

