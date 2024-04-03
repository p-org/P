// P semantics XYZ: one machine, asgn to uninitialized sequence (runtime error)
// Updating a sequence at index 1 (line 21), but the size of the sequence at that time is 0 (the initial sequence size).

machine Main {
    var x : (a: seq [int], b: map[int, seq[int]]);
    var y : int;
	var tmp: int;
	var tmp1: int;

    start state S
    {
       entry
       {
          //1 = 2;
	
          //x.a[foo()] = 1;
          //GetX().a[foo()] = 1;
		  tmp = foo();
		  tmp1 = IncY();
          x.a[tmp] = tmp1;
          y = IncY();
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
}
