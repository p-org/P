event unit;
event init:(mid, mid);
event myCount:int;
event Req;
event Resp;

main machine Scheduler {
	var p1:mid;
	var p2:mid;
	var p3:mid;
    start state inits {
        
		entry {
			p1 = new Process(parent = this);
			p2 = new Process(parent = this);
            p3 = new Process(parent = this);
			send(p1, init, (p2, p3));
			send(p2, init, (p3, p1));
			send(p3, init, (p1, p2));
			
        }
    }
}

machine Process {
	var count:int;
	var parent:mid;
	var other1:mid;
	var other2:mid;
	
	start state inits {
		ignore myCount, Resp;
		entry {
			count = 0;
		}
		on init do initaction;
		on unit goto SendCount;
	}
	action initaction {
		other1 = (((mid, mid))payload)[0];
		other2 = (((mid,mid))payload)[1];
		raise(unit);
	}
	action ConfirmThatInSync {
		assert(count <= (payload) && count >= (payload - 1));
	}
	state SendCount {
		entry {
			count = count + 1;
			send(other1, myCount, count);
			send(other2, myCount, count);
			//thats one step execute barrier and wait for other threads
			
			if(count > 10)
			{
				raise(unit);
			}
		}
		on unit goto done;
		on default goto SendCount;
		on myCount do ConfirmThatInSync;
	}
	state done {
	ignore Resp, myCount;
	}
}
