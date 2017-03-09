//Testing goto statement:
//- constants and variables in goto stmt
//

event E;

type mapInt = map[int,int];
machine Main {
       var m: mapInt;
	   start state Init {
		  entry { 
			 goto T, 0; 
			 m[0] = 5;
			 m[1] = 1;
			 goto S, m;  
			 new X(0);
			 raise E;
		  }
		  on E do { goto S, m; }
		  //exit { goto T, 0; }
	   }

	   state S {
		  entry (x: mapInt) { assert x.0 == 5; }
	   }

	   state T {
	      entry (x: int) { assert x == 0; }
	   }
}

machine X {
	   start state Init {
			entry { send Main, E; }
	   
	   }
}
