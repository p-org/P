//Events
event eD0Entry;
event eD0Exit;
event eTimerFired;
event eSwitchStatusChange;
event eTransferSuccess;
event eTransferFailure;
event eStopTimer;
event eUpdateBarGraphStateUsingControlTransfer;
event eSetLedStateToUnstableUsingControlTransfer;
event eStartDebounceTimer;
event eSetLedStateToStableUsingControlTransfer;
event eStoppingSuccess;
event eStoppingFailure;
event eOperationSuccess;
event eOperationFailure;
event eTimerStopped;
event eYes;
event eNo;
event eUnit;


//interfaces
interface OSRDriverInterface() receives
  eSwitchStatusChange, eD0Exit, eD0Entry, eOperationSuccess, eTransferSuccess,
  eTransferFailure, eTimerFired, eTimerStopped,eStoppingSuccess, eStoppingFailure;

interface SwitchInterface(OSRDriverInterface) receives eYes;

interface TimerInterface(OSRDriverInterface) receives eStartDebounceTimer, eStopTimer;

interface LEDInterface(OSRDriverInterface) receives
  eUpdateBarGraphStateUsingControlTransfer, eSetLedStateToUnstableUsingControlTransfer,
  eSetLedStateToStableUsingControlTransfer;




