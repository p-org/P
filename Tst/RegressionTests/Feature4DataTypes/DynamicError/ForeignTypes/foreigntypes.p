type tFloat = int;
type tytFloat = (tFloat, tFloat);

machine Main {
   var x : tFloat;
   var y : tFloat;
   var z : tytFloat;
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
   		assert(z.0 != z.1);
   }

   fun foo(x: tFloat, y: tFloat) : (tFloat, tFloat)
   {
   		var x1 : (int, int);
   		x1 = (x, y);
   		return x1;
   }

   fun def() : tFloat
   {
   		return 1;
   }
}
