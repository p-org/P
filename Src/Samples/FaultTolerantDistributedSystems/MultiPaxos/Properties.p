/*
Properties :
The property we check is that 
P2b : If a proposal is chosen with value v , then every higher numbered proposal issued by any proposer has value v.

*/

event monitor_valueChosen : (proposer: machine, slot: int, proposal : (round: int, servermachine : int), value : int);
event monitor_valueProposed : (proposer: machine, slot:int, proposal : (round: int, servermachine : int), value : int);

monitor BasicPaxosInvariant_P2b {
	var lastValueChosen : map[int, (proposal : (round: int, servermachine : int), value : int)];
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
		on monitor_valueChosen goto CheckValueProposed with
		{
			lastValueChosen[payload.slot] = (proposal = payload.proposal, value = payload.value);
		};
	}
	
	fun lessThan (p1 : (round: int, servermachine : int), p2 : (round: int, servermachine : int)) : bool {
		if(p1.round < p2.round)
		{
			return true;
		}
		else if(p1.round == p2.round)
		{
			if(p1.servermachine < p2.servermachine)
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
		on monitor_valueChosen goto CheckValueProposed with {
			assert(lastValueChosen[payload.slot].value == payload.value);
		};
		on monitor_valueProposed goto CheckValueProposed with {
			if(lessThan(lastValueChosen[payload.slot].proposal, payload.proposal))
				assert(lastValueChosen[payload.slot].value == payload.value);
		};
	}

}


/*
Monitor to check if 
the proposed value is from the set sent by the client (accept)
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
		on monitor_proposer_chosen do checkChosenValmachineity;
	}
	
	fun addClientSet() {
		clientSet[payload] = 0;
	}
	
	fun addProposerSet() {
		assert(payload in clientSet);
		ProposedSet[payload] =  0;
	}
	
	fun checkChosenValmachineity() {
		assert(payload in ProposedSet);
	}
}

