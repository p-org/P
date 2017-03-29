// hardware abstraction layer represented by events and functions.
event DOOR_OPENED;
event DOOR_CLOSED;
event UNKNOWN_ERROR;
event EXPRESSO_BUTTON;
event STEAMER_ON;
event STEAMER_OFF;
event EXPRESSO_COMPLETE;

// this function returns true if something is open such that machine cannot safely operate.
fun CheckIsOpen() : bool {
    return $;
}
fun CheckWaterLevel() : bool {
    return $;
}
fun CheckBeans() : bool {
    return $;
}
// turn on red light
fun ShowError(){}
// turn on heating element
fun BeginHeating(){}

// this function returns true if heating is complete.
fun CheckHeat() : bool {
    return $;    
}

// star the expresso function
fun StartExpresso(){}

// start the steamer 
fun StartSteamer(){}

// stop the steamer 
fun StopSteamer(){}

// stop all functions
fun EmergencyStop(){}

event ReadyDoorOpened;

// the state machine
machine CoffeeMachine
{
    // fields
    var timer: TimerPtr;

    // states
    start state Init
    {
        entry
        {
            timer = CreateTimer(this);
            if (CheckIsOpen() || !CheckWaterLevel() || !CheckBeans()) {
                goto Error;
            }
            goto WarmingUp;
        }

        ignore START_EXPRESSO;
    }

    state WarmingUp {
        entry {
            StartTimer(timer, 1000);
            BeginHeating();
        }
        on TIMEOUT do
        {
            if (CheckHeat()) {
                goto Ready;
            } else {
                StartTimer(timer, 1000);
            }
        }
        on DOOR_OPENED push DoorOpened;
        on UNKNOWN_ERROR goto Error;
        ignore START_EXPRESSO;
    }
    
    state Ready {
        entry {
            if (CheckIsOpen()){
                raise ReadyDoorOpened;
            }
        }
        on DOOR_OPENED push DoorOpened;
        on UNKNOWN_ERROR goto Error;
        on EXPRESSO_BUTTON push MakeExpresso;
        on STEAMER_ON push MakeSteam;
        on ReadyDoorOpened push DoorOpened;
    }

    state MakeExpresso {
        entry {
            StartExpresso();   
        }
        on EXPRESSO_COMPLETE do { pop; }
        on UNKNOWN_ERROR goto Error;
        on DOOR_OPENED do {
            EmergencyStop();
            pop;
        }
    }

    state MakeSteam {
        entry {
            StartSteamer();
        }
        on STEAMER_OFF  do { pop; }
        on UNKNOWN_ERROR goto Error;
        on DOOR_OPENED do {
            EmergencyStop();
            pop;
        }
    }

    state DoorOpened {
        entry{
            // do not respond to any user input
            EmergencyStop();
            ShowError();
        }
        on DOOR_CLOSED  do { pop; }
        on UNKNOWN_ERROR goto Error;
        ignore START_EXPRESSO;
    }
    
    state Error {
        entry {
            // do not respond to any user input
            ShowError();
        }
        ignore START_EXPRESSO;
    }
}