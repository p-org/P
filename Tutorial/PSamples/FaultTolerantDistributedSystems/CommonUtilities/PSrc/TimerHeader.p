//Interfaces and model types
interface ITimer(ITimerClient) receives eStartTimer, eCancelTimer;
interface ITimerClient() receives eTimeOut, eCancelSuccess, eCancelFailure; 
type TimerPtr = ITimer;

// events from client to timer
event eStartTimer: int;
event eCancelTimer;
// events from timer to client
event eTimeOut: TimerPtr;
event eCancelSuccess: TimerPtr;
event eCancelFailure: TimerPtr;
