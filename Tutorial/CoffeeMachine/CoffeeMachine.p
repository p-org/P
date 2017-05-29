event eUnknownError;

type ICoffeeMachine(ICoffeeMachineController) = 
    { eBeginHeating, eGrindBeans, eStartEspresso, eStartSteamer, eStopSteamer, eDumpGrinds };

model CoffeeMachine : ICoffeeMachine
{
    var controller: ICoffeeMachineController;

    start state Init {
        entry (x: ICoffeeMachineController) {
            controller = x;
        }
        on eBeginHeating do { 
            send controller, eTemperatureReached; 
        }
        on eGrindBeans do { 
            if ($) {
		        send controller, eNoBeans;
	        } else {
		        send controller, eGrindComplete;
	        }
        }
        on eStartEspresso do {
           	if ($) {
		        send controller, eNoWater;
	        } else {
		        send controller, eEspressoComplete;
	        }
        }
        on eStartSteamer do {
            if ($) {
		        send controller, eNoWater;
	        }
        }
        on eStopSteamer do {
            if ($) {
		        send controller, eUnknownError;
	        }
        }
        on eDumpGrinds do {
            if ($) {
		        send controller, eDumpComplete;
	        } else {
		        send controller, eUnknownError;
	        }
        }
    }
}

// turn on heating element, and wait for eTemperatureReached
event eTemperatureReached;
model fun BeginHeating(c: ICoffeeMachine) {
	send c, eBeginHeating;
}

// start grinding beans to fill the filter holder
event eNoBeans;
event eGrindComplete;
model fun GrindBeans(c: ICoffeeMachine){
	send c, eGrindBeans;
}

// start the espresso function
event eEspressoButtonPressed;
event eEspressoComplete;
event eNoWater;
event mMachineBusy;

model fun StartEspresso(c: ICoffeeMachine) {
    announce mMachineBusy;
    send c, eStartEspresso;
}

// start the steamer 
event eSteamerButtonOn;
model fun StartSteamer(c: ICoffeeMachine) {
    announce mMachineBusy;
    send c, eStartSteamer;
}

// stop the steamer 
event eSteamerButtonOff;
model fun StopSteamer(c: ICoffeeMachine) {
    send c, eStopSteamer;
}

// start dumping the grinds
event eDumpComplete;
model fun DumpGrinds(c: ICoffeeMachine) {
    send c, eDumpGrinds;
}

// stop all functions
model fun EmergencyStop() { }

// internal events
event eReadyDoorOpened;

