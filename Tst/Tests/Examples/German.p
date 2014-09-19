//Event declaration
event unit assert 1;
event req_share:id assert 3;
event req_excl:id assert 3;
event need_invalidate assert 1;
event invalidate_ack assert 3;
event grant assert 1;
event ask_share assume 1;
event ask_excl assume 1;
event invalidate assert 1;
event grant_excl assert 1;
event grant_share assert 1;
event normal assert 1;
event wait assert 1;
event invalidate_sharers:int assert 1;
event sharer_id:id assert 3;

//Host machine 
main machine Host {
	var curr_client:id;
	var clients:(id,id,id);
	var curr_cpu:mid;
	var sharer_list:seq[id];
	var is_curr_req_excl:bool;
	var is_excl_granted:bool;
	var i,s :int;
	start state init {
		entry {
			//clients = (null, null, null);
			clients[0] = new Client((this, false));
			clients[1] = new Client((this, false));
			clients[2] = new Client((this, false));
			curr_client = null;
			curr_cpu = new CPU(clients);
			assert(sizeof(sharer_list) == 0);
			raise(unit);
		}
		on unit goto receive;
	}
	
	state receive {
		defer invalidate_ack;
		entry {}
		
		on req_share goto ShareRequest;
		on req_excl goto ExclRequest;
		
	}
	
	state ShareRequest {
		entry {
			curr_client = (id) payload;
			is_curr_req_excl = false;
			raise(unit);
		}
		
		on unit goto ProcessReq;
	
	}
	
	state ExclRequest {
		entry {
			curr_client = (id) payload;
			is_curr_req_excl = true;
			raise(unit);
		}
		
		on unit goto ProcessReq;
	}
	
	state ProcessReq {
		entry {
			if(is_curr_req_excl || is_excl_granted)
			{
				// need to invalidate before giving access
				raise(need_invalidate);
			}
			else
				raise(grant);
		}
		on need_invalidate goto inv;
		on grant goto grantAccess;
	}
	
	state inv{
		defer req_share, req_excl;
		entry {
			i =0;
			s = sizeof(sharer_list);
			if(s==0)
				raise(grant);
			while(i<s)
			{
				send(sharer_list[i],  invalidate);
				i = i + 1;
			}
		}
		on invalidate_ack do rec_ack;
		on grant goto grantAccess;
	}
	
	action rec_ack {
		sharer_list.remove(0);
		s = sizeof(sharer_list);
		if(s ==0)
			raise(grant);
	}
	
	state grantAccess {
		entry {
			if(is_curr_req_excl)
			{
				is_excl_granted = true;
				send(curr_client, grant_excl);
			}
			else
			{
				send(curr_client, grant_share);
			}
			sharer_list.insert(0, curr_client);
			raise(unit);
		}
		on unit goto receive;
	}
}

//Client Machine
machine Client {
	var host:id;
	var pending:bool;
	start state init {
		entry {
			host = ((id,bool)) payload[0]; 
			pending = ((id,bool)) payload[1];
			raise(unit);
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
			send(host, req_share, this);
			pending = true;
			raise(unit);
		}
		on unit goto invalid_wait;
	}
	
	state asked_excl {
		entry {
			send(host, req_excl, this);
			pending = true;
			raise(unit);
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
			send(host, req_excl, this);
			pending = true;
			raise(unit);
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
			send(host, invalidate_ack);
			if(pending)
			{
				raise(wait);
			}
			else
				raise(normal);
		}
		on wait goto invalid_wait;
		on normal goto invalid;
	}
}


//Environment machine in the form of a CPU which makes request to the clients
model machine CPU {
	var cache : (id,id,id);

	start state init {
		entry {
			cache = ((id,id,id)) payload;
			raise(unit);
		}
		on unit goto makeReq;
	}
	
	state makeReq {
		entry {
			if(*)
			{
				if(*)
					send(cache[0], ask_share);
				else
					send(cache[0], ask_excl);
			}
			else if(*)
			{
				if(*)
					send(cache[1], ask_share);
				else 
					send(cache[2], ask_excl);
			}
			else
			{
				if(*)
				{
					send(cache[2], ask_share);
				}
				else
				{
					send(cache[2], ask_excl);
				}
			}
			raise(unit);
		}
		on unit goto makeReq;
	
	}
}



