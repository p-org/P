event x : (int, int);
event a;
event y;
interface MI_1 y;

test t1 Mod1;
implementation Mod1;

module Mod1
sends x
creates M1
{
	main machine M1
	receives a, y
	{
		var id: machine;
		var part: map[int, int];
		start state S 
		{
			entry {
				var container : machine;
				send this, x, (0, 0);
				container = CREATECONTAINER();
				CreateSMR(container, (this, false, 0));
				receive {
					case x : { part[payload.0] =  payload.1; }
				}
				container = CREATECONTAINER();
				CreateSMR(container, (this, false, 1));
				receive {
					case x : { part[payload.0] =  payload.1; }
				}
			}
		}
		
		fun CREATECONTAINER() : machine {return null;}
		fun CreateSMR(cont: machine, param : any) : machine
		{
			return null;
		}
	}
}
