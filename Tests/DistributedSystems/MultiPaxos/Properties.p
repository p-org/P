/*
Properties :
The property we check is that 
P2b : If a proposal is chosen with value v , then every higher numbered proposal issued by any proposer has value v.

*/

event monitor_valueChosen : (proposer: id, proposal : (round: int, serverId : int), value : int);
event monitor_valueProposed : (proposer: id, proposal : (round: int, serverId : int), value : int);

monitor BasicPaxosInvariant_P2b {
	var lastValueChosen : (proposer: id, proposal : (round: int, serverId : int), value : int);
	var returnVal : bool;
	var receivedValue : (proposer: id, proposal : (round: int, serverId : int), value : int);
	start state Init {
		entry {
			raise(local);
		
		}
		on local goto WaitForValueChosen;
	}
	
	state WaitForValueChosen {
		ignore monitor_valueProposed;
		entry {
			
		}
		on monitor_valueChosen goto CheckValueProposed
		{
			lastValueChosen = ((proposer: id, proposal : (round: int, serverId : int), value : int))payload;
		};
	}
	
	fun lessThan (p1 : (round: int, serverId : int), p2 : (round: int, serverId : int)) : bool {
		if(p1.round < p2.round)
		{
			return true;
		}
		else if(p1.round == p2.round)
		{
			if(p1.serverId < p2.serverId)
				return true;
			else
				return false;
		}
		else
		{
			return false;
		}
	
	}
	
	state CheckValueProposed {
		on monitor_valueChosen goto CheckValueProposed {
			receivedValue = ((proposer: id, proposal : (round: int, serverId : int), value : int)) payload;
			assert(lastValueChosen.value == receivedValue.value);
		};
		on monitor_valueProposed goto CheckValueProposed {
		receivedValue = ((proposer: id, proposal : (round: int, serverId : int), value : int)) payload;
		returnVal = lessThan(lastValueChosen.proposal, receivedValue.proposal);
			if(returnVal)
				assert(lastValueChosen.value == receivedValue.value);
		};
	}

}


/*
Monitor to check if 
the proposed value is from the set send by the client (accept)
chosen value is the one proposed by atleast one proposer (chosen).
*/
event monitor_client_sent : int;
event monitor_proposer_sent : int;
event monitor_proposer_chosen : int;

monitor ValidityCheck {
	var clientSet : map[int, int];
	var ProposedSet : map[int, int];
	
	start state Init {
		entry {
			raise(local);
		}
		on local goto Wait;
	}
	
	state Wait {
		on monitor_client_sent do addClientSet;
		on monitor_proposer_sent do addProposerSet;
		on monitor_proposer_chosen do checkChosenValidity;
	}
	
	action addClientSet {
		clientSet.update((int)payload, 0);
	}
	
	action addProposerSet {
		assert((int)payload in clientSet);
		ProposedSet.update((int)payload, 0);
	}
	
	action checkChosenValidity {
		assert((int)payload in ProposedSet);
	}
}

