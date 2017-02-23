//Interfaces
interface SMR_CLIENT_IN SMR_RESPONSE, SMR_SERVER_UPDATE;
interface SMR_REPLICATED_MACHINE_IN SMR_RM_OPERATION;
interface SMR_SERVER_IN SMR_OPERATION;

//Events used to interact with the SMR Protocol
event SMR_OPERATION : (source: SMR_CLIENT_IN, command: event, val: any);
event SMR_RESPONSE : (response: event, val: any);
event SMR_RM_OPERATION : (source: SMR_CLIENT_IN, command: event, val: any);
event SMR_SERVER_UPDATE : (int, SMR_SERVER_IN);

event reorder;
event noreorder;

module SMR_LINEARIZIBILITY_SPEC
sends SMR_RM_OPERATION, SMR_SERVER_UPDATE
creates SMR_REPLICATED_MACHINE_IN
{
	machine SMR_Machine_spec
	receives SMR_OPERATION
	{
		var repMachine : SMR_REPLICATED_MACHINE_IN;
		var doReordering : bool;
		var myId: int;
		var allClients : seq[SMR_CLIENT_IN];
		
		start state Init {
			entry {
				var i : int;
				i =0;
				allClients = (payload as (seq[SMR_CLIENT_IN], bool, int, int)).0;
				//create the replicated machine
				doReordering = (payload as (seq[SMR_CLIENT_IN], bool, int, int)).1;
				myId = (payload as (seq[SMR_CLIENT_IN], bool, int, int)).2;
				repMachine = new SMR_REPLICATED_MACHINE_IN(myId);
				
				//for the specification case send the current 
				while(i< sizeof(allClients))
				{
					SEND (allClients[i], SMR_SERVER_UPDATE, (myId, this as SMR_SERVER_IN));
					i = i + 1;
				}
				
				if(doReordering)
					raise reorder;
				else
					raise noreorder;
			}
			on reorder goto DoReOrdering;
			on noreorder goto DoNoReOrdering;
		}
		//we have created a parameterized linearizibility abstraction
		//since the coordinator is synchronous message can never be 
		//reordered and for the case of Hash table they can be reordered.
		var pending: seq[(source: SMR_CLIENT_IN, command: event, val: any)];	
		state DoReOrdering {
			entry {
				while(sizeof(pending) >0)
				{
					send repMachine, SMR_RM_OPERATION, pending[0];
					pending -= 0;
					if($)
						return;
				}
			}
			on SMR_OPERATION goto DoReOrdering with {
				pending += (chooseIndex(), payload);
			};
		}
		
		fun chooseIndex() : int {
			var i: int;
			i = 0;
			while(i <sizeof(pending))
			{
				if($)
					return i;
					
				i = i + 1;
				
			}
		}
		
		state DoNoReOrdering {
			on SMR_OPERATION do {
				send repMachine, SMR_RM_OPERATION, payload;
			};
		}
	}
}