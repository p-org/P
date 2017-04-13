//This file contains the test drivers for testing the system

machine TestDriver1
sends;
{
	var coor: CoorClientInterface;
	var client : ClientInterface;
	start state Init {
		entry {
			coor = new CoorClientInterface();
			client = new ClientInterface(coor, 100);
			raise halt;
		}
		
	}
}

