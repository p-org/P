//Tests complex data types in assignments: seq, tuples, maps

main machine M
{    
    var x : (a: seq [int], b: map[int, seq[int]]);
    var y : int;
	var tmp: int;
	var tmp1: int;
    
    start state S
    {
       entry
       {
          //1 = 2;
		  x.a += (0,1);
          x.a[foo()] = 1;
          //GetX().a[foo()] = 1;  
		  tmp = foo();
		  tmp1 = IncY();
		  x.a[foo()] = tmp1;
          //x.a[tmp] = tmp1;          
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
