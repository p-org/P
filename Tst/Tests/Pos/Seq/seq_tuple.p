event unit;
event seqpayload: seq[int];

main machine Entry {
	var l: seq[int];
    var i:int;
	var mac:id;
	var t :(seq[int], int);
    start state init {
       entry {
			l.insert(0,12);
			l.insert(0,23);
			l.insert(0,12);
			l.insert(0,23);
			l.insert(0,12);
			l.insert(0,23);
			mac = new test((l, 1));
			send(mac, seqpayload, l);
	   }
    }
}

machine test {
	var ii:seq[int];
	var rec:seq[int];
	var i:int;
	start state init {
		entry {
			ii = (((seq[int],int)) payload)[0];
			assert((((seq[int],int)) payload)[1] == 1);
		}
		on seqpayload goto testitnow;
	}
	
	state testitnow {
		entry {
			rec = (seq[int]) payload;
			i = sizeof(rec) - 1;
			while(i >= 0)
			{
				assert(rec[i] == ii[i]);
				i = i - 1;
			}
		}
	}
}