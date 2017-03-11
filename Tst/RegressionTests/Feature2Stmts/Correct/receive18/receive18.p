//Testing variable scoping in nested "receives"

event e1;
event e2;

enum Foo { foo0, foo1, foo2 }
enum Bar { bar0, bar1 }

machine Main {
	var x: int;
	var y: event; 
	var z : int;
	var ts: (a: int, b: int);
	start state Init {
		entry {
			x = 10;
			y = e1;
			receive {
				case e1 : {
					var x: int;
					var y: int;
					var foo0: int;
					var Foo:  int;
					var a: int;
					x = 19;
					assert x == 19;   //OK
					y = 1;
					foo0 = 5;
					assert foo0 == 5;    //OK
					Foo = default(Bar);
					assert Foo == bar0;   //OK
					z = foo0(y);          
					assert z == 1;      //OK
					z = foo1(y);
					assert z == 1;      //OK
					a = 3;
					ts.a = 5;
					assert a == 3;      //OK
					assert ts.a == 5;	//OK
					receive {
						case e2: {
							assert x == 19;  //OK
							assert foo0 == 5;  //OK
							assert Foo == bar0; //OK
							assert z == 1;		//OK
							assert a == 3;	 	//OK
							assert ts.a == 5;	//OK
							assert y == 1;   //OK
							assert foo0 == 5;   //OK
						}
					}
				}
				case e2 : {
					assert x == 10;  //OK
					assert y == e1;   //OK
					assert foo0 == 0;  //OK
					assert default(Foo) == foo0;  //OK
					assert ts.a == default(int);	//OK
				}
			}
			
			assert x == 10;  //OK
			assert y == e1;  //OK
			assert foo0 == 0;  //OK
			assert default(Foo) == foo0; //OK
			assert ts.a == default(int);	//OK
		}
	}
	
	fun foo0(a: int) : int {
		z = a;
		return a;
	}
	fun foo1(a: int) : int {
		z = a;
		return a;
	}
}