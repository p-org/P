/************************************************************************************************
* Description: This file implements the test drivers for testing the two phase commit protocols
************************************************************************************************/
// TestDriver1 is used to test to the two phase commit protocol without fault-tolerance
// In this case two phase commit protocol is not composed with the SMR.
machine TestDriver1
receives;
sends;
{
	var coor: CoorClientInterface;
	var client : ClientInterface;
	start state Init {
		entry {
			coor = new CoorClientInterface((isfaultTolerant = false,));
			client = new ClientInterface(coor, 100);
			raise halt;
		}
		
	}
}

// TestDriver2 is used to test to the two phase commit protocol with fault-tolerance
// In this case two phase commit protocol is composed with the SMR Protocol.
machine TestDriver2
receives;
sends;
{
	var coor: CoorClientInterface;
	var client : ClientInterface;
	start state Init {
		entry {
			coor = new CoorClientInterface((isfaultTolerant = true,));
			client = new ClientInterface(coor, 100);
			raise halt;
		}
	}
}