event x : (int, int);
event a;
event y;

machine Main {
		var part: map[int, int];
		start state S
		{
			entry {
				var container : machine;
				container = CREATECONTAINER();
				CreateSMR(container, (this, false, 0));
				receive {
					case x : (payload: (int, int)) { part[payload.0] =  payload.1; }
				}
				container = CREATECONTAINER();
				CreateSMR(container, (this, false, 1));
				receive {
					case x : (payload: (int, int)) { part[payload.0] =  payload.1; }
				}
			}
		}
		
		fun CREATECONTAINER() : machine {return null as machine;}
		fun CreateSMR(cont: machine, param : any)
		{

			send (param as (machine, bool, int)).0, x, (100, 100);
		}
	}

