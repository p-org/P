type StringType;

event sendback : (machine, any);
event getback : any;

machine TestMachine
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
}

machine ForwardingMachine {
	start state Init {
		on sendback do (payload : (machine, any))
		{
			send payload.0, getback, payload.1;
		}
	}
}

fun GetPassword() : StringType;
