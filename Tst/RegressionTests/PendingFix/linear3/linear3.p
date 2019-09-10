//Check contexts for swap and move

event E1 assert 1: int;
event E2 assert 1: int;

machine Main {
	var x: int;
	var y: bool; 
	var m: map[int, int];
	var client1: machine;
    start state S { 
        entry { 
			var locvar: int;
			var xloc, yloc: int;
			var mloc: map[int, int];
			
			foo(xloc swap);
			assert xloc == 1;                   
			foo(xloc move);
			xloc = 1;
			yloc = xloc + 1;			          
			
			assert xloc == 1;                    
			
			baz(xloc swap, yloc move);
			assert xloc == 7;                    

			xloc = xloc + 2;                           

			assert xloc == 9;                        
			
			yloc = 2;                             //to make yloc available
			baz(xloc swap, xloc + yloc);     
			assert xloc == 16;                      
			assert yloc == 2;					 
			
			yloc = baz_1(xloc move, xloc + yloc);
			assert yloc == 23;						 
			
			xloc = 20;                          //to make xloc available 
			x = xloc move;                         
			assert x == 20;                       

			xloc = 1;                          //to make xloc available 
			m[1] = 2;
			m[1] = xloc swap;                  
			assert m[1] == 1;                    
			assert xloc == 2;                    
			xloc = xloc - 1;
			m[xloc] = xloc + 2;                
			assert m[1] == 3;                        

			assert xloc == 1;              
			assert yloc == 23;             
			x = bar();  
			assert x == 4;                 
			
			x = 1;                              //to make x available
			client1 = new Client1(xloc move);     
			x = 1;                              //to make x available
			
			xloc = 7;
			raise E1, xloc move;   			     
			xloc = 15;                           //to make xloc available
			send client1, E2, xloc move;           
			
			xloc = 15;                           //to make xloc available
			goto T, xloc move;                  	
        }
		
        on E1 do (payload: int) { assert payload == 7; }                       
        on E2 do (payload: int) { assert payload == 15; }   
        exit {   }
	}
	state T {
		entry (payload: int) {
			assert payload == 15;
		}
	}
	fun foo(a: int) {
		a = 1;
	}
	fun bar(): int {                 
		var x, y: int;
		
		x = 2;
		y = x swap;
		assert x == 0;             
		assert y == 2;              
		foo(y swap);                    
		assert y == 1;             
		assert x == 0;             

		
		x = foo_1(y swap);
		assert x == 2;             
		assert y == 2;             
		
		y = foo_1(x);                  
		assert y == 3;				 
		assert x == 2;				 
		
		x = foo_1(y move);
		assert x == 4;				 
		
		y = foo_1(x);                  
		assert y == 5;					 
		assert x == 4;                 
		
		return x;
	}
	fun foo_1(a: int): int {
		a = a + 1;
        return a;
	}
	fun baz(a: int, b: int){
		a = b + 5;
	}
	fun baz_1(a: int, b: int): int{
		assert a == 16;                
		a = b + 5;
		return a;
	}
}

machine Client1 {
	start state S {
		entry (payload: int) {
			assert payload == 1;    
		}
	}
}