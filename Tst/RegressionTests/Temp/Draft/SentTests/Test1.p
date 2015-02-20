//Tests complex data types in assign/remove/insert: sequences, tuples, maps
//Tests "insert" for sequences errors
event E assert 1; 
main machine M
{    
    var t : (a: seq [int], b: map[int, seq[int]]);
	var tmp3: map[int, seq[int]];
	var s7: seq[int];
    var i: int;
	var mac: machine;
	var m1: map[int,int];
	var m3: map[int,bool];
	//TODO: write asgns for m2
	var m2: map[int,map[int,any]];
	
    start state S
    {
       entry
       { 
	      m1[0] = 1;
		  assert(m1[1] == 1);    //fails: "P Assertion failed: Expression: assert(false)"
		  
		  raise halt;
       }    
    }
}