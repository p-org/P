//Testing variable scoping in nested "receive"

event e1;
event e2;
event e3;
event e4;

enum Foo { foo0, foo1, foo2 }
enum Bar { bar0, bar1 }

machine Main {
	var m: machine;
	start state Init {
		entry {
			m = new Receiver();
			send m, e1;
			send m, e2;
			send m, e3;
			send m, e4;
		}
	}
}
machine Receiver {
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
					//assert x == 0;    //reachable (debug only)
					y = 1;
					foo0 = 5;
					assert foo0 == 5;    //OK
					Foo = default(Bar) to int;
					assert Foo == bar0 to int;   //OK
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
							//assert x == 0;    //reachable (debug only)
							assert foo0 == 5;  //OK
							assert Foo == bar0 to int; //OK
							assert z == 1;		//OK
							assert a == 3;	 	//OK
							assert ts.a == 5;	//OK
							assert y == 1;   //OK
							assert foo0 == 5;   //OK
						}
					}
				}
				
			}
			receive {
				case e3 : {
					assert x == 10;  //OK
					//assert x == 0;    //reachable (debug only)
					assert y == e1;   //OK
					assert default(Foo) == foo0;  //OK
					assert ts.a == 5;	//OK
				}
			}
			
			bar();
			
			assert x == 10;  //OK
			//assert x == 0;    // reachable (debug only)
			assert y == e1;  //OK
			assert foo0 to int == 0 ;  //OK
			assert default(Foo) == foo0; //OK
			assert ts.a == 5;	//OK
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
	fun bar() {
		var x: int;
		receive {
			case e4: {
				//var x: int;
			}
		}
	}
	
}