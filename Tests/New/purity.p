machine M
{
   var x : int;
   var y : bool;
   
   fun foo1() : int
   {       
       if (foo2())
       {
          return 0;
       }
       else 
       {
           return 1;
       }   
   }
   
   fun foo3() : int
   {       
       foo2();
       return 0;
   }
   
   fun foo4() : int
   {       
       if (foo3() < 0)
       {
          return -1;       
       }
       else 
       {
          return x;
       }
   }
   
   fun foo5() : int
   {       
       y = foo2();
       if (y)
       {
          return 0;
       }
       else 
       {
          return 1;
       }
   }
   
   fun foo6() : int
   {       
       y = foo2() || (foo6() > 0);
       if (y)
       {
          return 0;
       }
       else 
       {
          return 1;
       }
   }   
           
   fun foo2() : bool
   {
      x = x + 1;
      if (x < 100)
      {
         return true;
      }
      else 
      {
         return false;
      }   
   }
}