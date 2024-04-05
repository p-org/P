machine M {
    var x : (a: seq [int], b: map[int, seq[int]]);
    var y : int;
	var b: bool;

    state S
    {
       entry
       {
          1 = 2;
          x.a[foo()] = 1;
          GetX().a[foo()] = 1;
          x.a[foo()] = IncY();
          y = IncY();
		  y = FArg();
		  y = FArg(b);
       }
    }

    fun foo() : int
    {
       return 1;
    }

    fun GetX() : (a: seq [int], b: map[int, seq[int]])
    {
        return x;
    }

    fun IncY() : int
    {
       y = y + 1;
       return y;
    }
    fun FArg(x: int) : int
	{
		x = x + 1;
	}
}
