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
	var st1: set [int];
	var tmp: int;
	var tmp1: int;

	
    start state S
    {
       entry
       {
		st += (4);
                st += (5);
                st -= (4);
                b = 4 in st;
	        tmp = sizeof(st);
		assert b == false;
		assert tmp == 1;
		st += (3);
	        assert 3 in st;
		assert (6 in st) == false;
		st1 += (3);
		st1 += (5);
		assert st == st1;
		raise halt;
       }
    }
}

