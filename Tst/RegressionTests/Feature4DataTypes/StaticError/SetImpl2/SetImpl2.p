// Tests the correctness of set types

machine Main {
    var t : (a: seq [int], b: map[int, seq[int]]);
	var t1 : (a: seq [int], b: map[int, seq[int]]);
	var ts: (a: int, b: int);
	var tt: (int, int);
	var te: (int, event);
    var y : int;
	var b: bool;
	var e: event;
	var a: any;
        var st: set [int];
	var st1: set [bool];
	var st2: set [set [int]];
	var tmp: int;
	var tmp1: int;
	
    start state S
    {
       entry
       {
		st += (4);
                st += (5);
                st -= (4);
                st1 += (false);	
		tmp = sizeof(st);
		st2 += (st1);  // Type mismatch
       }
    }
}

