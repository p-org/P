event unit;
event init:(model, model);
event myCount:int;
event Req;
event Resp;

main model Scheduler {
	var p1:model;
	var p2:model;
	var p3:model;
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
	action CountReq {
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

model Process {
	var count:int;
	var parent:model;
	var other1:model;
	var other2:model;
	
        start state _init {
			entry { 
				  parent = payload as model; 
				  raise unit;
			}
            on unit goto inits;
        }

	state inits {
		entry {
			count = 0;
		}
		on myCount goto inits;
		on init do initaction;
		on Resp goto SendCount;
	}
	action initaction {
		other1 = (payload as (model, model)).0;
		other2 = (payload as (model, model)).1;
		send parent, Req;
		
	}
	action ConfirmThatInSync {
		assert(count <= (payload as int) && count >= (payload as int) - 1);
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
		on myCount do ConfirmThatInSync;
	}
	state done {
	ignore Resp, myCount;
	}
}
