// hardware abstraction layer represented by events and external functions.
event eDoorOpened;
event eDoorClosed;
event eUnknownError;

// this function returns true if something is open such that machine cannot safely operate.
model fun CheckIsOpen() : bool { return $; }

// turn on red light
model fun ShowError(){
}

// turn on heating element, and wait for eTemperatureReached
event eTemperatureReached;
model fun BeginHeating(c: ICoffeeMachine){
	send c, eTemperatureReached;
}

// start grinding beans to fill the filter holder
event eNoBeans;
event eGrindComplete;
model fun GrindBeans(c: ICoffeeMachine){
	if ($) {
		send c, eNoBeans;
	} else {
		send c, eGrindComplete;
	}
}

// start the espresso function
event eEspressoButtonPressed;
event eEspressoComplete;
event eNoWater;
event mMachineBusy;

model fun StartEspresso(c: ICoffeeMachine){
    announce mMachineBusy;
	if ($) {
		send c, eNoWater;
	} else {
		send c, eEspressoComplete;
	}
}

// start the steamer 
event eSteamerButtonOn;
model fun StartSteamer(c: ICoffeeMachine) {
    announce mMachineBusy;
    if ($) {
		send c, eNoWater;
	}
}

// stop the steamer 
event eSteamerButtonOff;
model fun StopSteamer(c: ICoffeeMachine){
    if ($) {
		send c, eUnknownError;
	}
}

// start dumping the grinds
event eDumpComplete;
model fun DumpGrinds(c: ICoffeeMachine){
    if ($) {
		send c, eDumpComplete;
	} else {
		send c, eUnknownError;
	}
}

// stop all functions
model fun EmergencyStop(){
}


// internal events
event eReadyDoorOpened;

type ICoffeeMachine() = { eDoorOpened, eDoorClosed, eUnknownError, eTemperatureReached, eNoBeans, eGrindComplete,
         eEspressoButtonPressed, eEspressoComplete, eNoWater, eSteamerButtonOn, eSteamerButtonOff,
         eDumpComplete, eReadyDoorOpened, TIMEOUT, CANCEL_SUCCESS, CANCEL_FAILURE
};

// Now for the the actual state machine
machine CoffeeMachine : ICoffeeMachine
receives eDoorOpened, eDoorClosed, eUnknownError, eTemperatureReached, eNoBeans, eGrindComplete,
         eEspressoButtonPressed, eEspressoComplete, eNoWater, eSteamerButtonOn, eSteamerButtonOff,
         eDumpComplete, eReadyDoorOpened, TIMEOUT, CANCEL_SUCCESS, CANCEL_FAILURE;
sends START, CANCEL, eDumpComplete, eUnknownError, eNoWater, eEspressoComplete, eGrindComplete, eNoBeans, eTemperatureReached;
{
    // fields
    var timer: TimerPtr;

    // states
    start state Init
    {
        entry
        {
            var open : bool;
            open = CheckIsOpen();
            timer = CreateTimer(this);
            if (open) {
                raise eDoorOpened;
            }
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
        entry {
            var open : bool;
            open = CheckIsOpen();
            if (open){
                raise eReadyDoorOpened;
            }
        }
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
        entry{
            // do not respond to any user input
            EmergencyStop();
            ShowError();
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
            ShowError();
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
