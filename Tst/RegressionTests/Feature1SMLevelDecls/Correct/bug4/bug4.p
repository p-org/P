event x : (int, int);
event a;
event y;

machine Main {
		var id: machine;
		var part: map[int, int];
		start state S
		{
			entry {
				var container : machine;
				container = CREATECONTAINER();
				new M2(this);
				CreateSMR(container, (this, false, 0));
				receive {
					case x : (payload: (int, int)) { part[payload.0] =  payload.1; }
				}
				CREATECONTAINER();
				CreateSMR(container, (this, false, 1));
			}
		}
		
		fun CREATECONTAINER() : machine {return null as machine;}
		fun CreateSMR(cont: machine, param : any) : machine
		{
			return null as machine;
		}
	}


	machine M2
	{
		var id: machine;
		var part: map[int, int];
		start state S
		{
			entry (payload: machine) {
				send payload, x, (0, 0);
			}
		}
	}
