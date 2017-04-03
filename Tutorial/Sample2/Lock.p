model type LockPtr = machine;

event ACQUIRE_REQ: machine;
event ACQUIRE_RESP: any;
event RELEASE: any;

model Lock {
	var data: any;

	start state Unheld {
		entry (v: any) {
			data = v move;
		}
		on ACQUIRE_REQ goto Held;
	}

	state Held {
		entry (client: machine) {
			var v: any;
			data = v swap;
			send client, ACQUIRE_RESP, v move;
		}
		defer ACQUIRE_REQ;
		on RELEASE goto Unheld;
	}
}

model fun CreateLock(data: any) : LockPtr
{
	var x: machine;
	x = new Lock(data move);
	return x;
}

model fun AcquireLock(l: LockPtr, client: machine) : any 
{
	var data: any;
	send l, ACQUIRE_REQ, client;
	receive {
		case ACQUIRE_RESP: (x: any) {
			data = x move;
		}
	}
	return data;
}

model fun ReleaseLock(l: LockPtr, data: any)
{
	send l, RELEASE, data move;
}