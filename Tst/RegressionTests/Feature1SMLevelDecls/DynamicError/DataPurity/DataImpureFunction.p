//Checks code execution for data impure function f in the following contexts:
//while (f), if (f), return (f).

event Ping assert 1 : int;
event Success;

main machine PING {
    var x: int;
	var y: int;
    start state Ping_Init {
        entry {
	    raise Success;   	   
        }
        on Success do {
			while (foo1())
			{
				assert(x == 1);
				x = x + 100;
			}
			assert (x == 102);
			
			if (foo3())
			{
				assert (false);   //not reachable
			}
			else
			{	
				assert (false);  //reachable
			}
		}

    }
	fun foo1() : bool
   {       
       if (foo2())
       {
          return true;
       }
       else 
       {
           return false;
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
   fun foo3() : bool   //data impure function
   {       
       return foo2();    //OK
   }
}
