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
			assert xloc == 1;                   //holds?
			foo(xloc move);
			xloc = 1;
			yloc = xloc + 1;			          //OK
			
			assert x == 1;                    //holds?
			
			baz(xloc swap, yloc move);
			assert xloc == 5;                    //holds?

			xloc = xloc + 2;                          //OK

			assert xloc == 7;                       //holds?
			
			yloc = 2;                             //to make yloc available
			baz(xloc swap, xloc + yloc);     
			assert xloc == 9;                     //?
			assert yloc == 2;					//?
			
			yloc = baz_1(xloc move, xloc + yloc);
			//assert xloc == 1;						//?
			assert yloc == 7;						//?
			
			xloc = 20;                          //to make xloc available 
			x = xloc move;                        //OK
			assert x == 40;                      //holds?

			//x = 4;                              //to make x available
			
			xloc = 1;                          //to make xloc available 
			
			m[1] = xloc swap;                  //OK
			assert m[1] == 1;                   //?
			m[xloc] = xloc + 2;                    //OK
			assert m[1] == 3;                   //?
						
			x = 1;                              ////to make x available
			client1 = new Client1(xloc move);    //OK
			x = 1;                              ////to make x available
			
			xloc = 7;
			raise E1, xloc move;   			    //OK
			xloc = 15;                           //to make xloc available
			send client1, E2, xloc move;          //OK
			
			xloc = 15;                           //to make xloc available
			goto T, xloc move;                 //OK	
        }
		
        on E1 do (payload: int) { assert payload == 7; }  //?                      
        on E2 do (payload: int) { assert payload == 15; }  //? 
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
		foo(y);                   //OK
		assert y == 1;            //?
		assert x == 2;            //?

		
		x = foo_1(y swap);
		assert x == 2;            //?
		assert y == 1;            //?
		
		y = foo_1(x);                 //OK
		assert y == 3;				//?
		assert x == 2;				//?
		
		x = foo_1(y move);
		assert x == 4;				//?
		
		y = foo_1(x);                 //OK
		assert y == 5;					//?
		assert x == 4;                //?
		
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
		assert a == 9;
		a = b + 5;
		return a;
	}
}

machine Client1 {
	start state S {
		entry (payload: int) {
			assert payload == 1;   //?
		}
	}
}