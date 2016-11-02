// XYZing assignments to a nested datatype when the right hand side of the assignment 
// is a side-effect free function with a nondeterministic choice inside.  
// Includes XYZs for tuples, maps and sequences

machine Main {
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
	var t, t1: (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var s, s1: seq[int];
	var m: map[int,any];
	//var m9, m10: map[int,any];
	//var s6: seq[map[int,any]];
	//var s3, s33: seq[seq[any]];
	
    start state S {
	    entry {
		    //static error is not reported for the line below, see separate XYZ
		    //D:\PLanguage-0216\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_13
			//i = F() + 1;                    //static error: not reported
			i = default(int);
			//+++++++++++++++++++++++++++++++++++++1. tuples:
			//+++++++++++++++++++++++++++++++++++++1.1. Assigned value is non-det:
		    x.f.g = F();                                          //static error
			
			ts.a = F();                                           //static error
			assert(ts.a == F());                                 //static error
			
			ts.0 = F();                                          //static error	
			//static error is not reported for the line below, see separate XYZ
			//D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_3\nonDetFunctionInExpr_12.p
			//assert(ts.0 == F());                                //static error: not reported
			//+++++++++++++++++++++++++++++++++++++2. Sequences:
			t.a += (0,2);
			t.a += (1,2);                
			//+++++++++++++++++++++++++++++++++++++2.1. Non-det value assigned into a sequence:
			//static error is not reported for the line below, see separate XYZ
			//D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_3\nonDetFunctionInExpr_3.p
			//t.a[foo()] = F() + 5;                                  //static error: not reported
			//+++++++++++++++++++++++++++++++++++2.2. non-det value as an inserted value in +=:
			//static error is not reported for the line below, see separate XYZ
			//D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_4\nonDetFunctionInExpr_4.p
			//t.a += (1,F());                                //caused null deref in Zing: static error now (not reported)               
			//+++++++++++++++++++++++++++++++++++2.3. index into sequence in -= is non-det:
			t.a -= (F());											//static error
			//+++++++++++++++++++++++++++++++++++2.4. non-det value as index into sequence:
			t.a += (0,2);
			t.a += (1,4);
			//static error is not reported for line below, see separate XYZ
            //D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_6\nonDetFunctionInExpr_5.p			
			//t.a[F()] = 5;                                              //static error: not reported 
			
			//++++++++++++++++++++++++++++++++++++++++++3. Maps:
			s += (0,0);
			s += (1,1);
			s1 += (0,2);
			s1 += (1,3);
			t.b += (0, s);
			t.b += (1, s1);
			//+++++++++++++++++++++++++++++++3.1. Value assigned into map is non-det (var m: map[int,int];):
			m += (0,1);
			m += (1,2);
			//static error is not reported for line below, see separate XYZ
            //D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_6\nonDetFunctionInExpr_6.p
			//m += (2,F());             //static error: not reported
			
			//static error is not reported for line below, see separate XYZ
            //D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_6\nonDetFunctionInExpr_7.p
			//m[2] = F() + 2;             //static error: not reported
			
			//+++++++++++++++++++++++++++++++3.2. Index for assigned into map value is non-det
			m = default(map[int,any]);
			//static error is not reported for line below, see separate XYZ
            //D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_6\nonDetFunctionInExpr_8.p
			//m += (F(), 0);               //static error: not reported
			//static error is not reported for line below, see separate XYZ
            //D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_6\nonDetFunctionInExpr_9.p
			//m[F()] = 3;                  //static error: not reported
			//+++++++++++++++++++++++++++++++3.3. Index in += for map is non-det:
			t.b = default(map[int, seq[int]]);
			//static error is not reported for line below, see separate XYZ
            //D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_6\nonDetFunctionInExpr_10.p
			//t.b += (F(), s1);              //static error: not reported
			//+++++++++++++++++++++++++++++++3.4. Index into map in -= is non-det:
			t.b -= (0);
			t.b += (0, s);
			t.b -= (F());                                //static error
			//+++++++++++++++++++++++++++++++3.5. Index into keys sequence in map is non-det:
			t.b = default(map[int, seq[int]]);
			s = default(seq[int]);
			s1 = default(seq[int]);
			s += (0,0);
			s += (1,1);
			s1 += (0,2);
			s1 += (1,3);
			t.b += (0, s);
			t.b += (1, s1);
			//static error is not reported for line below, see separate XYZ
            //D:\PLanguageGitHub\P\Tst\RegressionXYZs\Feature3Exprs\StaticError\NonDetFunctionInExpr_6\nonDetFunctionInExpr_11.p 			
			//j = keys(t.b)[F()];                          //static error: not reported                          	
		}
	}
}
