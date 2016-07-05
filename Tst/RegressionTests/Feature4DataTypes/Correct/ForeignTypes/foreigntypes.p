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

   model fun bar()
   {
   		assert(z.0 == z.1);
   }

   model fun foo(x: float, y: float) : tyfloat
   {
   		return (x, y);
   }

   model fun def() : float
   {
   		return 1;
   }
}
