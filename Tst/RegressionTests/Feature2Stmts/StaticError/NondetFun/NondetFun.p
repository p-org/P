main machine Test
{
  var x: int;
  var y: int;

  fun Foo() : bool
  {
    var n1, n2: bool;
	n1 = $;
	n2 = $;
	return (x < y && $ && (y != 0 || x != 0 || n2));    //error
  }

  fun Baz() : bool 
  {
	return Foo();
  }

  fun Bar() : bool 
  {
	x = x + 1;
	if (x >= 5) return false;
	return $;
  }
  
  start state Init {
    entry {
		if (Foo() || $$)          //error
		{
			x = x + 1;
		}
		assert x == 0;
		while (Bar())   //OK
		{
		
		}
		while (false || $)  //error {}
		assert x <= 5;
    }
  }
}
