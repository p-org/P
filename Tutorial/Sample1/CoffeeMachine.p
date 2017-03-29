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
model fun BeginHeating(){
	raise eTemperatureReached;
}

// start grinding beans to fill the filter holder
event eNoBeans;
event eGrindComplete;
model fun GrindBeans(){
	if ($) {
		raise eNoBeans;
	} else {
		raise eGrindComplete;
	}
}

// star the espresso function
event eEspressoButtonPressed;
event eEspressoComplete;
event eNoWater;
model fun StartEspresso(){
	if ($) {
		raise eNoWater;
	} else {
		raise eEspressoComplete;
	}
}

// start the steamer 
event eSteamerButtonOn;
model fun StartSteamer() {
    if ($) {
		raise eNoWater;
	}
}

// stop the steamer 
event eSteamerButtonOff;
model fun StopSteamer(){    
}

// start dumping the grinds
event eDumpComplete;
model fun DumpGrinds(){
    if ($) {
		raise eDumpComplete;
	} else {
		raise eUnknownError;
	}
}

// stop all functions
model fun EmergencyStop(){
    raise eUnknownError;
}


// internal events
event eReadyDoorOpened;
event eNotHeating;

type ICoffeeMachine() = { eDoorOpened, eDoorClosed, eUnknownError, eTemperatureReached, eNoBeans, eGrindComplete,
         eEspressoButtonPressed, eEspressoComplete, eNoWater, eSteamerButtonOn, eSteamerButtonOff,
         eDumpComplete, eReadyDoorOpened, eNotHeating };

// Now for the the actual state machine
machine CoffeeMachine : ICoffeeMachine
receives eDoorOpened, eDoorClosed, eUnknownError, eTemperatureReached, eNoBeans, eGrindComplete,
         eEspressoButtonPressed, eEspressoComplete, eNoWater, eSteamerButtonOn, eSteamerButtonOff,
         eDumpComplete, eReadyDoorOpened, eNotHeating;
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
                goto Error;
            }
            goto WarmingUp;
        }
        ignore eEspressoButtonPressed;
        ignore eSteamerButtonOn;
        ignore eSteamerButtonOff;
        ignore eTemperatureReached;
    }

    state WarmingUp {
        entry {
            StartTimer(timer, 60000);
            BeginHeating();
        }
        on TIMEOUT do
        {
            raise eNotHeating;    
        }
        on eNotHeating goto Error;
        on eDoorOpened push DoorOpened;
        on eUnknownError goto Error;
        on eTemperatureReached goto Ready;
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
            GrindBeans();   
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
            StartEspresso();
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
            StartSteamer();
        }
        on eSteamerButtonOff  do { pop; }
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
