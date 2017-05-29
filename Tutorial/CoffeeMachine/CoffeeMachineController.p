type ICoffeeMachineController() = { 
    eInit, eDoorOpened, eDoorClosed, eUnknownError, eTemperatureReached, eNoBeans, eGrindComplete,
    eEspressoButtonPressed, eEspressoComplete, eNoWater, eSteamerButtonOn, eSteamerButtonOff,
    eDumpComplete, eReadyDoorOpened, TIMEOUT, CANCEL_SUCCESS, CANCEL_FAILURE
};

machine CoffeeMachineController : ICoffeeMachineController
sends START, CANCEL, 
      eDumpComplete, eUnknownError, eNoWater, eEspressoComplete, eGrindComplete, eNoBeans, eTemperatureReached;
{
    var timer: TimerPtr;
    var coffeeMachine: ICoffeeMachine;

    start state _Init {
        entry (x: ICoffeeMachine) {
            coffeeMachine = x;
            goto Init;
        }
    }
    
    state Init {
        entry {
            timer = CreateTimer(this);
            goto WarmingUp;
        }
        on eDoorOpened push DoorOpened;
        ignore eEspressoButtonPressed;
        ignore eSteamerButtonOn;
        ignore eSteamerButtonOff;
        ignore eTemperatureReached;
    }

    state WarmingUp {
        entry {
            StartTimer(timer, 60000);
            BeginHeating(this);
        }
        on TIMEOUT do
        {
            goto Error;    
        }
        on eDoorOpened push DoorOpened;
        on eUnknownError goto Error;
        on eTemperatureReached goto Ready with {
            CancelTimer(timer);
            receive {
                case CANCEL_SUCCESS: {
                }                
                case CANCEL_FAILURE: {
                    receive {
                        case TIMEOUT: {                            
                        }
                    }
                }
            }
        }
        ignore eEspressoButtonPressed;
        ignore eSteamerButtonOn;
        ignore eSteamerButtonOff;
    }
    
    state Ready {
        on eDoorOpened push DoorOpened;
        on eUnknownError goto Error;
        on eEspressoButtonPressed push Grind;
        on eSteamerButtonOn push MakeSteam;
        on eReadyDoorOpened push DoorOpened;
    }

    state Grind {
        entry {
            GrindBeans(this);   
        }
        on eUnknownError goto Error;
        on eNoBeans goto Error;
        on eGrindComplete goto RunEspresso;
        on eDoorOpened do {
            EmergencyStop();
            pop;
        }
        // Can't make steam while we are making espresso
        ignore eSteamerButtonOn;
        ignore eSteamerButtonOff;
    }

    state RunEspresso {
        entry {
            StartEspresso(this);
        }
        on eEspressoComplete do { pop; }
        on eUnknownError goto Error;
        on eDoorOpened do {
            EmergencyStop();
            pop;
        }
        // Can't make steam while we are making espresso
        ignore eSteamerButtonOn;
        ignore eSteamerButtonOff;
    }

    state MakeSteam {
        entry {
            StartSteamer(this);
        }
        on eSteamerButtonOff  do { 
            StopSteamer(this);
            pop; 
        }
        on eUnknownError goto Error;
        on eDoorOpened do {
            EmergencyStop();
            pop;
        }
        ignore eSteamerButtonOn;
        // can't make espresso while we are making steam
        ignore eEspressoButtonPressed;
        on eNoWater goto Error;
    }

    state DoorOpened {
        entry {
            // do not respond to any user input
            EmergencyStop();
        }
        on eDoorClosed  do { pop; }
        on eUnknownError goto Error;
        ignore eEspressoButtonPressed;
        ignore eSteamerButtonOn;
        ignore eSteamerButtonOff;
    }
    
    state Error {
        entry {
            // do not respond to any user input
            EmergencyStop();
            raise halt;
        }
        ignore eDoorOpened;
        ignore eDoorClosed;
        ignore eEspressoButtonPressed;
        ignore eSteamerButtonOn;
        ignore eSteamerButtonOff;
    }
}