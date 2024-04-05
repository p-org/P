event unit;
event next;
event req;
event resp: TUP;
//type def for a complex type
type TUP = (first: int, second: int);

fun PrintTime() {
	//something here
}
machine Main {
	var cl: machine;
	var randomTup : TUP;
	start state Init{
		entry {
			cl = new client(this);
		}
		on req do {
			PrintTime();
			randomTup = GetRandomVal();
			send cl, resp, randomTup;
		}
	}
	
	fun GetRandomVal() : TUP{
		return default(TUP);
	}
}

machine client
{
	var sr: machine;
	var tmr: machine;
	var counter: int;
	start state Init{
		entry (arg: machine){
			sr = arg;
			tmr = new Timer(this);
			counter = 0;
			raise unit;
		}
		on unit goto StartReqResp;
	}
	
	fun UpdateTuple(arg : TUP)
	{
		//update the value in external context
	}
	
	state StartReqResp {
		entry {
			//start timer
			if(counter == 4)
			{
				//halt all machines
				send sr, halt;
				assert(false);
				raise halt;
			}
			send tmr, START, 3; // 3 secs
		}
		on TIMEOUT do (payload: machine) {
			send sr, req;
			counter = counter + 1;
			receive {
				case resp: (arg: TUP){ /*UpdateTuple(arg);*/ }
			}
			raise next;
		}
		on next goto StartReqResp;
	}
}
