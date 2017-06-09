model type LockPtr = machine;

event ACQUIRE_REQ: machine;
event ACQUIRE_RESP: any;
event RELEASE: any;

model Lock {
	var val: any;

	start state Unheld {
		entry (v: any) {
			val = v move;
		}
		on ACQUIRE_REQ goto Held;
	}

	state Held {
		entry (client: machine) {
			var v: any;
			val = v swap;
			send client, ACQUIRE_RESP, v move;
		}
		defer ACQUIRE_REQ;
		on RELEASE goto Unheld;
	}
}

model fun CreateLock(val: any) : LockPtr
{
	var x: machine;
	x = new Lock(val move);
	return x;
}

model fun AcquireLock(l: LockPtr, client: machine) : any 
{
	var val: any;
	send l, ACQUIRE_REQ, client;
	receive {
		case ACQUIRE_RESP: (x: any) {
			val = x move;
		}
	}
	return val;
}

model fun ReleaseLock(l: LockPtr, val: any)
{
	send l, RELEASE, val move;
}
