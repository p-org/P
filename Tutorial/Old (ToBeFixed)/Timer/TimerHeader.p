type TimerPtr;

fun CreateTimer(owner : machine): TimerPtr;
fun StartTimer(timer: TimerPtr, time: int);

// from client to timer
event START: int;
// from timer to client
event TIMEOUT: TimerPtr;
