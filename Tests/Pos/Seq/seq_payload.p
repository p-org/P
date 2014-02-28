event unit;
event seqpayload: seq[int];

main machine Entry {
    var l:seq[int];
    var i:int;
	var mac:id;
    start state init {
        entry {
            l.insert(0,1);
            l.insert(0,3);
			mac = new Test(l);
        }
    }
}

machine Test {
	var ss: seq[int];
	start state init {
		entry {
			ss = (seq[int]) payload;
			assert(ss[0] == 3);
		}
		
	}
}
