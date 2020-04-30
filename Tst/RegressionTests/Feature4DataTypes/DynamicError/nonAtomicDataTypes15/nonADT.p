machine Main {
	start state Init {
		entry {
			var x: seq[(int, int)];
			var result1: (int, int);
			x += (0, (191, 192));
			x += (0, (101, 102));

			result1 = x[0];
			x[0].1 = 103;
			assert result1 == x[0], format ("{0} == {1}", result1, x[0]);
		}
	}

	fun foo(i: int)
	{
		return;
	}
}
