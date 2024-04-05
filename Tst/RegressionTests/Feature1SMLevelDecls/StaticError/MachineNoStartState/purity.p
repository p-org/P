//This sample XYZs checks different contexts for using data impure functions
machine M {
   var x : int;
   var y : bool;

   fun foo1() : int
   {
       var tmp : bool;
       tmp = foo2();
       if (tmp)
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
       var tmp : int;
       tmp = foo3();
       if (tmp < 0)  //error
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
   }

   fun foo6() : int
   {
       var tmp1 : bool;
       var tmp2 : int;
       tmp1 = foo2();
       tmp2 = foo6();
       y = tmp1 || (tmp2 > 0);  //error
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
