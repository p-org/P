type TimerPtr;

fun CreateTimer(owner : machine): TimerPtr;
fun StartTimer(timer: TimerPtr, time: int);
fun CancelTimer(timer: TimerPtr);

// events from client to timer
event START: int;
event CANCEL;
// events from timer to client
event TIMEOUT: TimerPtr;
event CANCEL_SUCCESS: TimerPtr;
event CANCEL_FAILURE: TimerPtr;
