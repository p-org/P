// P semantics XYZ: two machines, non-constant event expressions
// non-atomic event expressions
// events are sent as payloads from Real1 to Real2,
// and then retrieved in Real2 and sent back to Real1


event E0 assert 1: any;
event E1 assert 1: any;
event E2 assert 5: event;
event E3 assert 1: bool;
event E4 assert 1: any;
event E5 assert 1: bool;
event E6 assert 1: int;
event E7 assert 1: bool;

machine Main {
    var XYZ: bool;
	var mac: machine;
	var ev0: event;
	var ev1: event;
	var ev2: event;
	var sev: seq[event];
	var sev1: seq[event];
	var sAny: seq[any];
	var mev: map[int,event];
	var mAny: map[int,any];
	
    start state Real1_Init {
        entry {
			mac = new Real2(this);
			sev += (0,E0);
			sev += (1,E1);
			sev += (2,E2);
			sev += (3,E3);
			sev += (4,E4);
			
			sAny += (0, E2);
			sAny += (1, true);
			
			mev[0] = E0;
			mev[2] = E2;
			
			ev2 = E2;			
			raise ev2, E1;  		
        } 	
        on E2 do Action1;
		on null goto Real1_S1;
		ignore E0;
		ignore E1;
		ignore E3;
		ignore E4;

        exit {
            send mac, sev[2], E1;	 //E2 with E1
			send mac, sev[0], 100;   //E0 with 100
			send mac, sAny[0] as event;  //E2 with null
			send mac, mev[2], E0;    //E2 with E0
			send mac, mev[2], sev[0];    //E2 with E0
			send mac, mev[2], mev[0];    //E2 with E0
			
			send mac, sev[3], true;    //E3 with true
		}
	}
	state Real1_S1 {
		entry {
		
			sev1 += (0, null as event);
			sev1 += (1, null as event);
			if (sev1[0] == sev1[1]) {
				assert(false);                  //fails
			}
		}
		ignore E4;
		ignore E0;
		ignore E1;
	}
    fun Action1() {
    }
	fun Action2() {
    }
	
}
machine Real2 {
	var pl: bool;
	var ev3: event;
	var mac1: machine;
	start state Real2_Init {
		entry (payload: machine) {	
			ev3 = E4;
			mac1 = payload;
		}
		on E2 do (payload: event) {
			//assert(payload == E1);  //passes
			if (payload == E1)
			{
				ev3 = E1;
				//assert (payload != E1); //fails
			}
			else if (payload == E0)
			{
				ev3 = E0;
				//assert(payload != E0);  //fails
			}
		}
		on E0 do (payload: any) {}
		on E3 do (payload: bool) {}
		
		on null goto Real2_S1;
		exit {
			send mac1, ev3, null; //sending E4, E1 or E0
		}
	}
	state Real2_S1 {
		on E4 do (payload: any) {}
		on E0 do (payload: any) {}
		on E1 do (payload: any) {}
		on E2 do (payload: event) {}   //might not be handled in Init
		on E3 do (payload: bool) {}   //might not be handled in Init
	}
}
