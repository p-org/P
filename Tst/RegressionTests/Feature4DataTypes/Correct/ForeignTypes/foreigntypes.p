type float = int;
type tyfloat = (float, float);

machine Main {
   var x : float;
   var y : float;
   var z : tyfloat;
   start state Init {
   	entry 
   	{
   		x = def();
   		y = def();
   		z = foo(x, y);
   		bar();
   	}
   }

   fun bar()
   {
   		assert(z.0 == z.1);
   }

   fun foo(x: float, y: float) : tyfloat
   {
   		return (x, y);
   }

   fun def() : float
   {
   		return 1;
   }
}
