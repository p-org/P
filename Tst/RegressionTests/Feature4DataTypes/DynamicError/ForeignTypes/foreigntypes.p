model type float = int;
model type tyfloat = (float, float);

main machine M
{    
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
   		assert(z.0 != z.1);
   }

   model fun foo(x: float, y: float) : (float, float)
   {
   		var x1 : (int, int);
   		x1 = (x, y);
   		return x1;
   }

   model fun def() : float
   {
   		return 1;
   }
}
