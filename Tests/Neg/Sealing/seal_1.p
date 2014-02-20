//This example tests the Round robin scheduler with sealing

event increment;
event local;
main machine M1 {
	var M2id: id;
	start state init {
		entry {
			__seal();
			M2id = new M2(this);
			send(M2id, increment);
			send(M2id, increment);
			send(M2id, increment);
			send(M2id, increment);
			__unseal();
			
		}
	}
}

machine M2 {
	var M1id :id;
	var count : int;
	action inc {
		count = count + 1;
		if (count == 4)
			raise(local);
	}
	start state init {
		entry {
		M1id = (id) payload;
			count = 0;
			//wait
		}
		on default goto error;
		on local goto success;
		on increment do inc;
	}
	
	state error {
		entry {
			
		}
	}
	
	state success {
		entry {
		}
	}
}
