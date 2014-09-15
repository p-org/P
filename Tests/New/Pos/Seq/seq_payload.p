event unit;
event seqpayload: seq[int];

main machine Entry {
    var l:seq[int];
    var i:int;
	var mac:id;
    start state init {
        entry {
            l += (0,1);
            l += (0,3);
			mac = new Test(l);
        }
    }
}

machine Test {
	var ss: seq[int];
	start state init {
		entry {
		      ss = payload as seq[int];
			assert(ss[0] == 3);
		}
		
	}
}
