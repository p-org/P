// Testing assignments to a nested datatype when the right hand side of the assignment 
// is a side-effect free function with a nondeterministic choice inside.  
// Also, checks that non-det return value from function works correctly
// in allowed contexts, including workarounds for nested types in the LHS
// Includes tests for tuples, maps and sequences

main machine M {
    fun F() : int {
	    if ($) {
		    return 0;
		} else {
		    return 1;
		}
	}
	
	fun foo() : int
    {
       return 1;
    }   
	
	var x: (f: (g: int));
	var i, j: int;
	var s, s1: seq[int];
	var t, t1: (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var m: map[int,int];
	//var m9, m10: map[int,any];
	//var s6: seq[map[int,any]];
	//var s3, s33: seq[seq[any]];
	
    start state S {
	    entry {
			//+++++++++++++++++++++++++++++++++++++1. tuples:
			//+++++++++++++++++++++++++++++++++++++1.1. Assigned value is non-det:
			//x.f.g = F();                                        //static error
		    i = F();                                          
			x.f.g = i;
			assert (x.f.g == 0 || x.f.g == 1);                    //passes
			
			//ts.a = F();                                         //static error
			ts.a = i;
			assert(ts.a == 0 || ts.a == 1);                       //passes
			//assert(ts.a == F());                                //static error
			
			//ts.0 = F();                                          //static error
			ts.0 = i;
			assert(ts.0 == 0 || ts.0 == 1);                       //passes
			//assert(ts.0 == F());                                //static error
			//+++++++++++++++++++++++++++++++++++++2. Sequences:
			t.a += (0,2);
			t.a += (1,2);
			assert (t.a[0] == 2 && t.a[1] == 2);                  //passes
			//+++++++++++++++++++++++++++++++++++++2.1. non-det value assigned into a sequence:
			//t.a[foo()] = F() + 5;                                //static error
			t.a[foo()] = i + 5;
			assert (t.a[0] == 2 && (t.a[1] == 5 || t.a[1] == 6));	//passes
			assert(sizeof(t.a) == 2);                              //fails
			
			//+++++++++++++++++++++++++++++++++++++2.2. non-det value as an inserted value in +=:
			//t.a += (1,F());                         //caused null deref in Zing; static error now
			t.a += (1,i);                                    
			assert (t.a[0] == 2 && (t.a[1] == 0 || t.a[1] == 1) && (t.a[2] == 5 || t.a[2] == 6));  //passes
			
			//+++++++++++++++++++++++++++++++++++++2.3. non-det value removed from a sequence:			
			assert(sizeof(t.a) == 3);                              //passes
			/*********************************************************
			//t.a -= (0);
			//assert(sizeof(t.a) == 2); 
			//assert(t.a[0] == 0 || t.a[0] == 1);        //passes       
			//assert(t.a[1] == 5 || t.a[1] == 6);        //passes 			
			//t.a -= (1);
			//assert(sizeof(t.a) == 1); 
			//assert(t.a[0] == 2);
			********************************************************/
			//+++++++++++++++++++++++++++++++++++++2.4. index into sequence in -= is non-det:
			//t.a -= (F());                               //static error
			t.a -= (i);
			assert(sizeof(t.a) == 2);                               //passes
			assert((t.a[0] == 2 || t.a[0] == 0 || t.a[0] == 1) && (t.a[1] == 5 || t.a[1] == 6));  //passes
			
			//how about det return value as index:
			t.a -= (foo());
			assert(sizeof(t.a) == 1);                               //passes
			assert(t.a[0] == 2 || t.a[0] == 0 || t.a[0] == 1);       //passes

			//+++++++++++++++++++++++++++++++++++++2.5. non-det value as index into sequence in LHS:
			t.a += (0,2);
			t.a += (1,4);   
			//t.a[F()] = 5;                                             //static error
			t.a[i] = 5;
			assert ((t.a[0] == 2 || t.a[0] == 5) && (t.a[1] == 4 || t.a[1] == 5));  //passes
			
			//++++++++++++++++++++++++++++++++++++++++++3. Maps:
			s += (0,0);
			s += (1,1);
			s1 += (0,2);
			s1 += (1,3);
			t.b = default(map[int, seq[int]]);
			t.b += (0, s);
			t.b += (1, s1);
			assert(sizeof(t.b) == 2);                  //passes
			assert(sizeof(t.b[0]) == 2 && t.b[0][0] == 0 && t.b[0][1] == 1);  //passes
			assert(sizeof(t.b[1]) == 2 && t.b[1][0] == 2 && t.b[1][1] == 3);  //passes
			/**************************************/
			//+++++++++++++++++++++++++++++++3.1. Value assigned into map is non-det 
			//var m: map[int,int];
			m += (0,1);
			m += (1,2);
			i = F();
			//m += (2,F());             //static error
			m += (2,i);
			assert(sizeof(m) == 3 && (m[2] == 0 || m[2] == 1));        //passes
			
			m[3] = 5;
			//m[2] = F() + 2;             //static error
			m[2] = i + 2;
			assert(sizeof(m) == 4);       //passes
			assert(m[2] == 0 || m[2] == 1 || m[2] == 2 || m[2] == 3);  //passes
			//+++++++++++++++++++++++++++++++3.2. Index for assigned into map value is non-det
			m = default(map[int,int]);
			//m += (F(), 0);
			i = F();
			m += (i,0);
			if (0 in m) {assert(m[0] == 0);}   //passes
			if (1 in m) {assert(m[1] == 0);}   //passes
			
			m = default(map[int,int]);
			m[i] = 2;
			assert(sizeof(m) == 1);                   //passes
			if (0 in m) {assert(m[0] == 2);}   //passes
			if (1 in m) {assert(m[1] == 2);}   //passes
			/*********************************************/
			//+++++++++++++++++++++++++++++++3.3. Index in += for map is non-det:
			t.b = default(map[int, seq[int]]);
			s = default(seq[int]);
			s1 = default(seq[int]);
			s += (0,0);
			s += (1,1);
			s1 += (0,2);
			s1 += (1,3);
			t.b += (0, s);
			//t.b += (0, s1);                 //dyn error: "key must not exist"
			//i = F();
			//t.b += (i, s1);                     //see separatetest with dynamic error: "key must not exist"
			i = F();
			i = i + 1;
			assert(i == 1 || i == 2);
			t.b += (i, s1);                  
			assert(sizeof(t.b) == 2);                          //passes
			if (i == 1) {assert(t.b[1][0] == 2 && t.b[1][1] == 3);}  //passes
			if (i == 2) {assert(t.b[2][0] == 2 && t.b[2][1] == 3);}  //passes
			//+++++++++++++++++++++++++++++++3.4. Index into map in -= is non-det :
			t.b = default(map[int, seq[int]]);
			t.b += (0, s);
			t.b += (1, s1);
			assert(sizeof(t.b) == 2);                          //passes
			t.b -= (0);
			assert(sizeof(t.b) == 1 && t.b[1][0] == 2 && t.b[1][1] == 3);    //passes
			t.b -= (1);
			assert(sizeof(t.b) == 0);                                          //passes
			
			t.b += (0, s);
			t.b += (1, s1);
			assert(sizeof(t.b) == 2 && sizeof(t.b[0]) == 2 && t.b[0][0] == 0 && t.b[0][1] == 1);  //passes
			assert(sizeof(t.b[1]) == 2 && t.b[1][0] == 2 && t.b[1][1] == 3);  //passes
			
			//t.b -= (F());                                //static error
			i = F();
			//TODO: send repro to Shaz
			t.b -= (i);                                 
			assert(sizeof(t.b) == 1);                      //passes
			j = keys(t.b)[0];
		    assert(j ==  0 || j == 1);                     //passes
			//Check the exact values
			if (j == 0) {assert(t.b[0][0] == 0 && t.b[0][1] == 1);}      //passes
			else {assert(t.b[1][0] == 2 && t.b[1][1] == 3);}		     //passes
			//+++++++++++++++++++++++++++++++3.5. Index into keys sequence is non-det:
			t.b = default(map[int, seq[int]]);
			s = default(seq[int]);
			s1 = default(seq[int]);
			s += (0,0);
			s += (1,1);
			s1 += (0,2);
			s1 += (1,3);
			t.b += (0, s);
			t.b += (1, s1);
			assert(sizeof(t.b) == 2);                          //passes
			j = keys(t.b)[i];                                  
			assert(j == 0 || j == 1);                           //passes

		}
	}
}