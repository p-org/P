type LockPtr;

event ACQUIRE_REQ: machine;
event ACQUIRE_RESP: any;
event RELEASE: any;

fun CreateLock(val: any) : LockPtr;
fun AcquireLock(l: LockPtr, client: machine) : any;
fun ReleaseLock(l: LockPtr, val: any);
