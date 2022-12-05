event unit;
event init:(machine, machine);
event myCount:int;
event Req;
event Resp;

machine Main {
	var p1:machine;
	var p2:machine;
	var p3:machine;
	var count:int;
    start state inits {
        
		entry {
			p1 = new Process(this);
			p2 = new Process(this);
            p3 = new Process(this);
			send p1, init, (p3, p2);
			send p2, init, (p3, p1);
			send p3, init, (p1, p2);
			count = 0;
			raise unit;
			
        }
		on unit goto Sync;
    }
	fun CountReq() {
		count = count + 1;
		if(count == 3)
		{
			count = 0;
			raise Resp;
		}
	}
    state Sync {
		on Req do CountReq;
		exit
		{
			send p1, Resp;
			send p2, Resp;
			send p3, Resp;
		}
		on Resp goto Sync;
    }
}

machine Process {
	var count:int;
	var parent:machine;
	var other1:machine;
	var other2:machine;
	
        start state _init {
			entry (payload: machine) { 
				  parent = payload; 
				  raise unit;
			}
            on unit goto inits;
        }

	state inits {
		entry (payload: any) {
			count = 0;
		}
		on myCount goto inits;
		on init do (payload: (machine, machine)){ initaction(payload); }
		on Resp goto SendCount;
	}
	fun initaction(payload: (machine, machine)) {
		other1 = payload.0;
		other2 = payload.1;
		send parent, Req;
		
	}
	fun ConfirmThatInSync(payload: int) {
		assert(count <= payload && count >= payload - 1);
	}
	state SendCount {
		entry {
			count = count + 1;
			send other1, myCount, count;
			send other2, myCount, count;
			send parent, Req;
			if(count > 10)
			{
				raise unit;
			}
		}
		on unit goto done;
		on Resp goto SendCount;
		on myCount do (payload: int) { ConfirmThatInSync(payload); }
	}
	state done {
	ignore Resp, myCount;
	}
}
