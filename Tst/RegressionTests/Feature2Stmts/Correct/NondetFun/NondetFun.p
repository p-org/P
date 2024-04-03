machine Main {
  var x: int;
  var y: int;

  fun Foo() : bool
  {
    var n1, n2: bool;
	n1 = $;
	n2 = $;
	return (x < y && n1 && (y != 0 || x != 0 || n2));
  }

  fun Baz() : bool
  {
	var b: bool;
	b = Foo();
	return b;
  }

  fun Bar() : bool
  {
	x = x + 1;
	if (x >= 5) return false;
	return $;
  }

  start state Init {
    entry {
		var b: bool;
		b = Foo();
		if (b)
		{
			x = x + 1;
		}
		assert x == 0;
		b = Bar();
		while (b)
		{
			b = Bar();
		}
		assert x <= 5;
    }
  }
}
