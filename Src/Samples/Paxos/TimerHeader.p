// Events related to Timer
// events from client to timer
event START: int;
event CANCEL;
// events from timer to client
event TIMEOUT: machine;
event CANCEL_SUCCESS: machine;
event CANCEL_FAILURE: machine;

//Function prototypes related to timer
extern fun CreateTimer(owner : machine): machine;

extern fun StartTimer(timer : machine, time: int);

extern fun CancelTimer(timer : machine);