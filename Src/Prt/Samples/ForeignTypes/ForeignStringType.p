type StringType;
type PointerType;

event sendback : (machine, any);
event getback : any;

main machine TestMachine
{
	var someStringV : StringType;
	var somePointerV : PointerType;
	var fMachine: machine;
	start state Init {

		entry {
			fMachine = new ForwardingMachine();
			someStringV = GetPassword();
			somePointerV = GetPointerToSomething(someStringV);
			send fMachine, sendback, (this, someStringV);
			receive {
				case getback: (payload: any) {
					assert((payload as StringType) == someStringV);
				}
			}
			send fMachine, sendback, (this, somePointerV);
			receive {
				case getback: (payload: any) {
					assert((payload as PointerType) == somePointerV);
				}
			}
		}
	}

	model fun GetPassword() : StringType
	{
		return fresh(StringType);
	}

	model fun GetPointerToSomething(password: StringType) : PointerType
	{
		if(password == someStringV)
		{
			return fresh(PointerType);
		}
		else
		{
			assert(false);
		}
	}
}

machine ForwardingMachine {
	start state Init {
		on sendback do (payload : (machine, any))
		{
			send payload.0, getback, payload.1;
		};
	}
}

