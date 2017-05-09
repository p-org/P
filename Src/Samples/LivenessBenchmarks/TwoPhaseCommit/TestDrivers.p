//This file contains the test drivers for testing the system

machine Main
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
