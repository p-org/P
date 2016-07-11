//This sample tests checks different contexts for using data impure functions
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
   
   fun foo3() : int   //data impure function
   {       
       foo2();    //OK
       return 0;
   }
   
   fun foo4() : int
   {       
       if (foo3() < 0)  //error
       {
          return -1;       
       }
       else 
       {
          return x;
       }
   }
   
   fun foo5() : int   //data impure function
   {       
       y = foo2();  //OK
       if (y)
       {
          return 0;
       }
       else 
       {
          return 1;
       }
	   y = 1 + new M();
   }
   
   fun foo6() : int
   {       
       y = foo2() || (foo6() > 0);  //error
       if (y)
       {
          return 0;
       }
       else 
       {
          return 1;
       }
   }   
           
   fun foo2() : bool  //data impure function
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
