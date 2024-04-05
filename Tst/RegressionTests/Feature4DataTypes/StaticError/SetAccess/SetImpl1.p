// Tests the correctness of set types

machine Main {
    var st: set [int];
	var st1: set [int];
	var sq: seq [int];
	var sq1: seq [int];
	var st2: set [seq [int]];
	var tmp: int;
	var tmp1: int;
	
    start state S
    {
       entry
       {
		st += (4);
        st += (5);
        st += (4);
		st[1] = 3;
		raise halt;
       }
    }
}

