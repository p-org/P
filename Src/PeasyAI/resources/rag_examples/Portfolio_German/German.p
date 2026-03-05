//Event declaration
event unit;
event req_share: machine;
event req_excl: machine;
event need_invalidate;
event invalidate_ack;
event grant;
event ask_share;
event ask_excl;
event invalidate;
event grant_excl;
event grant_share;
event normal;
event wait;
event invalidate_sharers: int;
event sharer_id: machine;

//Host machine
machine Main {
	var curr_client : machine;
	var clients : seq[machine];
	var curr_cpu : machine;
	var sharer_list : seq[machine];
	var is_curr_req_excl : bool;
	var is_excl_granted : bool;
	var i, s :int;
	var temp: machine;
	start state init {
		entry {
			
			temp = new Client(this, false);
			clients += (0, temp);
			temp = new Client(this, false);
			clients += (0, temp);
			temp = new Client(this, false);
			clients += (0, temp);
			curr_cpu = new CPU(clients);
			assert(sizeof(sharer_list) == 0);
			raise unit;
		}
		on unit goto receiveState;
	}
	
	state receiveState {
		defer invalidate_ack;
		entry {}
		
		on req_share goto ShareRequest;
		on req_excl goto ExclRequest;		
	}
	
	state ShareRequest {
		entry (payload: machine) {
			curr_client = payload;
			is_curr_req_excl = false;
			raise unit;
		}
		
		on unit goto ProcessReq;	
	}
	
	state ExclRequest {
		entry (payload: machine) {
		        curr_client = payload;
			is_curr_req_excl = true;
			raise unit;
		}
		
		on unit goto ProcessReq;
	}
	
	state ProcessReq {
		entry {
			if (is_curr_req_excl || is_excl_granted)
			{
				// need to invalidate before giving access
				raise need_invalidate;
			}
			else
				raise grant;
		}
		on need_invalidate goto inv;
		on grant goto grantAccess;
	}
	
	state inv {
		defer req_share, req_excl;
		entry {
			i = 0;
			s = sizeof(sharer_list);
			if (s == 0)
				raise grant;
			while (i < s)
			{
				send sharer_list[i], invalidate;
				i = i + 1;
			}
		}
		on invalidate_ack do rec_ack;
		on grant goto grantAccess;
	}
	
	fun rec_ack() {
		sharer_list -= 0;
		s = sizeof(sharer_list);
		if (s == 0)
			raise grant;
	}
	
	state grantAccess {
		entry {
			if (is_curr_req_excl)
			{
				is_excl_granted = true;
				send curr_client, grant_excl;
			}
			else
			{
				send curr_client, grant_share;
			}
			sharer_list += (0, curr_client);
			raise unit;
		}
		on unit goto receiveState;
	}
}

//Client Machine
machine Client {
	var host : machine;
	var pending : bool;
	start state init {
		entry (payload: (machine,bool)) {
		        host = payload.0;
		        pending = payload.1;
			raise unit;
		}
		on unit goto invalid;
	}

	state invalid {
		entry {
			
		}
		on ask_share goto asked_share;
		on ask_excl goto asked_excl;
		on invalidate goto invalidating;
		on grant_excl goto exclusive;
		on grant_share goto sharing;
	}
	
	state asked_share {
		entry{
			send host, req_share, this;
			pending = true;
			raise unit;
		}
		on unit goto invalid_wait;
	}
	
	state asked_excl {
		entry {
			send host, req_excl, this;
			pending = true;
			raise unit;
		}
		on unit goto invalid_wait;
	}
	
	state invalid_wait {
		defer ask_share, ask_excl;
		on invalidate goto invalidating;
		on grant_excl goto exclusive;
		on grant_share goto sharing;
	}
	
	state asked_ex2 {
		entry {
			send host, req_excl, this;
			pending = true;
			raise unit;
		}
		on unit goto sharing_wait;
	}
	
	state sharing {
		entry {
			pending = false;
		}
		on invalidate goto invalidating;
		on grant_share goto sharing;
		on grant_excl goto exclusive;
		on ask_share goto sharing;
		on ask_excl goto asked_ex2;
	}
	
	state sharing_wait {
		defer ask_share, ask_excl;
		entry {}
		on invalidate goto invalidating;
		on grant_share goto sharing_wait;
		on grant_excl goto exclusive;
		
	}
	
	state exclusive {
		ignore ask_share, ask_excl;
		entry {
			pending = false;
		}
		on invalidate goto invalidating;
		on grant_share goto sharing;
		on grant_excl goto exclusive;
	}
	
	state invalidating {
		entry {
			send host, invalidate_ack;
			if (pending)
			{
				raise wait;
			}
			else
				raise normal;
		}
		on wait goto invalid_wait;
		on normal goto invalid;
	}
}


//Environment machine in the form of a CPU which makes request to the clients
machine CPU {
	var cache : seq[machine];
        var req_count : int;

	start state init {
		entry (payload: seq[machine]) {
			cache = payload;
			raise unit;
		}
		on unit goto makeReq;
	}
	
	state makeReq {
		entry {
			if ($)
			{
				if ($)
			              send cache[0], ask_share;
				else
			              send cache[0], ask_excl;
			}
			else if ($)
			{
				if ($)
			              send cache[1], ask_share;
				else
			              send cache[1], ask_excl;
			}
			else
			{
				if ($)
				{
				    send cache[2], ask_share;
				}
				else
				{
				    send cache[2], ask_excl;
				}
			}
                        if (req_count < 3) {
                            req_count = req_count + 1;
			    raise unit;
                        }
		}
		on unit goto makeReq;
	}
}



