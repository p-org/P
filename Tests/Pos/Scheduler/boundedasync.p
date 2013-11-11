event unit;
event init:(mid, mid);
event myCount:int;
event Req;
event Resp;

main machine Scheduler {
	var p1:mid;
	var p2:mid;
	var p3:mid;
	var count:int;
    start state inits {
        
		entry {
			p1 = new Process(parent = this);
			p2 = new Process(parent = this);
            p3 = new Process(parent = this);
			send(p1, init, (p3, p2));
			send(p2, init, (p3, p1));
			send(p3, init, (p1, p2));
			count = 0;
			raise(unit);
			
        }
		on unit goto Sync;
    }
	action CountReq {
		count = count + 1;
		if(count == 3)
		{
			count = 0;
			raise(Resp);
		}
	}
    state Sync {
		on Req do CountReq;
		exit
		{
			send(p1, Resp);
			send(p2, Resp);
			send(p3, Resp);
		}
		on Resp goto Sync;
    }
}

machine Process {
	var count:int;
	var parent:mid;
	var other1:mid;
	var other2:mid;
	
	start state inits {
		ignore myCount;
		entry {
			count = 0;
		}
		on init do initaction;
		on Resp goto SendCount;
	}
	action initaction {
		other1 = (((mid, mid))payload)[0];
		other2 = (((mid,mid))payload)[1];
		send(parent, Req);
		
	}
	action ConfirmThatInSync {
		assert(count <= (payload) && count >= (payload - 1));
	}
	state SendCount {
		entry {
			count = count + 1;
			send(other1, myCount, count);
			send(other2, myCount, count);
			send(parent, Req);
			if(count > 10)
			{
				raise(unit);
			}
		}
		on unit goto done;
		on Resp goto SendCount;
		on myCount do ConfirmThatInSync;
	}
	state done {
	ignore Resp, myCount;
	}
}
