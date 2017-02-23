//This file contains the test drivers for testing the system


module TestDriver1 
creates CLIENT_MACHINE_PUBLIC_IN, COOR_MACHINE_PUBLIC_IN
{
	main machine TD1
	{
		var coor: COOR_MACHINE_PUBLIC_IN;
		var client : CLIENT_MACHINE_PUBLIC_IN;
		start state Init {
			entry {
				var container : machine;
				var i : int;
				i = 0;
				container = CREATECONTAINER();
				coor = CreateCoor(container);
				container = CREATECONTAINER();
				while(i < 1)
				{
					client = CreateClient(container, (coor, 100000, 100, 3));
					i  = i + 1;
				}
				raise halt;
			}
			
		}
		
		fun CreateCoor(cont : machine) : COOR_MACHINE_PUBLIC_IN
		[container = cont]
		{
			var coor : COOR_MACHINE_PUBLIC_IN;
			coor = new COOR_MACHINE_PUBLIC_IN();
			return coor;
		}
		
		fun CreateClient(cont : machine, param: any) : CLIENT_MACHINE_PUBLIC_IN
		[container = cont]
		{
			var coor : CLIENT_MACHINE_PUBLIC_IN;
			coor = new CLIENT_MACHINE_PUBLIC_IN(param);
			return coor;
		}
	}
}
