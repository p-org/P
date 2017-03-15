//Check contexts for swap and move

event E1 assert 1: int;
event E2 assert 1: int;
event E3 assert 1: int;

machine Main {
	var x: int;
	var y: bool; 
	var m: map[int, int];
	var client1, client2: machine;
    start state S { 
        entry { 
			var locvar: int;
			var xloc, yloc: int;
			var mloc: map[int, int];
			
			foo(xloc swap);
			assert xloc == 1;                   //holds
			foo(xloc move);
			yloc = xloc + 1;                      //error (variable xloc is not available)
			xloc = 15;                           //to make xloc available 
			yloc = xloc + 1;			          //OK
			
			assert x == 1;                    //holds
			
			baz(xloc swap, yloc move);
			assert xloc == 5;                    //holds
			xloc = yloc + 2;                          //error (variable yloc is not available)
			xloc = xloc + 2;                          //OK
			assert xloc == yloc;                     //error (variable yloc is not available)
			assert xloc == 7;                       //holds
			yloc = 20;                           //to make yloc available 
			
			baz(xloc swap, xloc move);              //error: not detected********************
			xloc = 20;                          //to make xloc available 
			baz(xloc move, xloc move);              //OK
			xloc = 20;                             //to make xloc available 
			baz(xloc swap, xloc swap);              //OK
			
			xloc = 20;                          //to make xloc available 
			x = xloc move;                        //OK
			yloc = xloc;                        //error (variable xloc is not available)
			foo(x move);                        //error (argument should be a local variable)
			x = 4;                              //to make x available
			
			xloc = 20;                          //to make xloc available 
			
			m[0] = x swap;                     //error (argument x should be a local variable)
			m[1] = xloc swap;                  //OK
			m[xloc] = xloc;                    //OK
			
			
			x = 1;                              ////to make x available
			client1 = new Client1(x swap);	    //errors are not detected (x is not local, swap is not allowed)***
			client1 = new Client1(x move);      //error not detected (x is not local)***********************
			client1 = new Client1(xloc move);    //OK
			x = 1;                              ////to make x available
			client2 = new Client2(this move);	//OK
			client2 = new Client2(this swap);   //error not detected (swap is not allowed)******************
			raise E1, x swap;                   //error  (argument x should be a local variable)
			raise E1, xloc swap;                //error (swap not allowed)
			raise E1, xloc move;   			    //OK
			xloc = 15;                           //to make xloc available
			send client1, E2, xloc move;          //OK
			xloc = 15;                           //to make xloc available
			send client1, E2, xloc swap;         //error (swap not allowed)
			
			//baz((bar(xloc) swap), yloc);          //parse error
			foo(5 swap);                              //error (argument should be a variable)
			
			goto S, yloc move;                 //OK
			goto S, yloc swap;                 //error (swap not allowed)
			
			
			
        }
		
        on E1 do { goto S, yloc move; }            //TODO: edit
		on E3 do { goto S, yloc swap; }            //TODO: edit
        on E2 do Action2;
        exit {   }
	}
	fun foo(a: int) {
		a = 1;
		x = a move;
		assert x == 1;               //holds
		x = a;                       //error (variable is not available)
	}
	fun bar(): int {                 //error (swap parameter not available at callee return) - 
	                                 //refers to "y = foo_1(y swap);" below
		var x, y: int;
		y = x swap;
		x = foo(y);                   //OK
		x = foo_1(y) swap;             //error (argument should be a variable)
		y = foo_1(y swap);            //swap parameter accessed at return
		
		x = foo_3(y swap);
		y = foo_2(x);                 //OK
		
		x = foo_3(y move);
		y = foo_2(x);                 //OK
		
		x = foo_3(y swap) + 1;        //errors (this function must be pure, move or swap not allowed)
		
		y = foo_2(foo_3(y swap));        //error (this function must be pure)
		y = foo_2(foo_3(y move));        //error (this function must be pure)
		//return x swap;              //parsing error
		return x;
	}
	fun foo_1(a: int): int {
		a = 1;
		x = a move;
		assert x == 1;               //holds
		return x;
	}
	fun foo_2(a: int): int {
		a = 1;
        return a;
	}
	fun foo_3(a: int): int {
		a = 1;
        return a;
	}
	fun baz(a: int, b: int){
		a = 5;
		b = 7;
	}
    fun Action1(p: bool) {
		p = true;
    }
	fun Action2() {
		//assert(y == false); //unreachable
    }
}

machine Client1 {
	start state S {
		entry (payload: int) {
		
		}
	}
}

machine Client2 {
	start state S {
		entry (payload: machine) {
				var locvar: int;
				send payload, E3, locvar;
		}
	}
}