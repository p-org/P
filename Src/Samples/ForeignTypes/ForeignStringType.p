// Foreign declarations
type StringForeignType;
fun GetPassword() : StringForeignType;

// Event declarations
event sendback : (machine, any);
event getback : any;

// Machines
machine TestMachine
{
	var someStringV : StringForeignType;

	start state Init {
		entry {
			var fMachine: machine;
			fMachine = new ForwardingMachine();
			someStringV = GetPassword();
			send fMachine, sendback, (this, someStringV);
		}

		on getback do (payload: any) {
			assert((payload as StringForeignType) == someStringV);
			// print "{0}: success!", payload;
		}
	}
}

machine ForwardingMachine {
	start state Init {
		on sendback do (payload : (machine, any)) {
			send payload.0, getback, payload.1;
		}
	}
}
