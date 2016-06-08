model type StringType = int;

event sendback : (machine, any);
event getback : any;

main machine TestMachine
{
	var someStringV : StringType;
	var fMachine: machine;
	start state Init {

		entry {
			fMachine = new ForwardingMachine();
			someStringV = GetPassword();
			send fMachine, sendback, (this, someStringV);
			receive {
				case getback: (payload: any) {
					assert((payload as StringType) == someStringV);
				}
			}
		}
	}

	model fun GetPassword() : StringType
	{
		//return default(StringType);
		if ($)
			return 0;
		else 
			return 1;
	}

}

machine ForwardingMachine {
	start state Init {
		on sendback do (payload : (machine, any))
		{
			send payload.0, getback, payload.1;
		}
	}
}
