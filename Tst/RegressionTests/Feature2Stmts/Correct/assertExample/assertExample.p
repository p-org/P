machine Main {
	start state Init {
		entry {
			var x : string;
			x = format("{0}", foo());
			assert true, foo();
			assert foo() == format("{0}", foo()), foo();
			assert x == foo();
		}

	}

	fun foo(): string
	{
		return "aa";
	}

}