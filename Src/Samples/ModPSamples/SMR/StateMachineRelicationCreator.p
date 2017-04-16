include "multipaxos.p"
//include "chainreplication.p"

module SMR
creates SMR_SERVER_IN
{
	machine SMR_Machine
	{	
		var repFactor : int;
		start state Init {
			entry {
				var i : int;
				var container: machine;
				i = 0;
				repFactor = (payload as (seq[SMR_CLIENT_IN], bool, int, int)).3;
				
				while(i< repFactor)
				{
					container = CREATECONTAINER();
					CreateSMR_Replica(container, ((payload as (seq[SMR_CLIENT_IN], bool, int, int)).0, i));
					i = i + 1;
				}
				raise halt;
			}
		}
		fun CreateSMR_Replica(cont : machine, param: any) : machine
		[container = cont]
		{
			var smr_rep : machine;
			smr_rep = new SMR_SERVER_IN(param);
			return smr_rep;
		}
	}
}