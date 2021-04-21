//This file contains the test drivers for testing the system

machine Main
sends;
{
	var coor: machine;
	var client : machine;
	start state Init {
		entry {
			coor = new Coordinator((isfaultTolerant = false,));
			client = new ClientMachine(coor, 100);
			raise halt;
		}
		
	}
}
