event eUnknownError;

event eBeginHeating;
event eGrindBeans;
event eStartEspresso;
event eStartSteamer;
event eStopSteamer;
event eDumpGrinds;

machine CoffeeMakerMachine
receives eBeginHeating, eGrindBeans, eStartEspresso, eStartSteamer, eStopSteamer, eDumpGrinds;
sends eTemperatureReached, eNoBeans, eGrindComplete, eEspressoComplete, eNoWater, eUnknownError, eDumpComplete;
{
    var controller: CoffeeMakerControllerMachine;

    start state Init {
        entry (x: CoffeeMakerControllerMachine) {
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
fun BeginHeating(c: CoffeeMakerMachine) {
	send c, eBeginHeating;
}

// start grinding beans to fill the filter holder
event eNoBeans;
event eGrindComplete;
fun GrindBeans(c: CoffeeMakerMachine){
	send c, eGrindBeans;
}

// start the espresso function
event eEspressoButtonPressed;
event eEspressoComplete;
event eNoWater;
event mMachineBusy;

fun StartEspresso(c: CoffeeMakerMachine) {
    announce mMachineBusy;
    send c, eStartEspresso;
}

// start the steamer 
event eSteamerButtonOn;
fun StartSteamer(c: CoffeeMakerMachine) {
    announce mMachineBusy;
    send c, eStartSteamer;
}

// stop the steamer 
event eSteamerButtonOff;
fun StopSteamer(c: CoffeeMakerMachine) {
    send c, eStopSteamer;
}

// start dumping the grinds
event eDumpComplete;
fun DumpGrinds(c: CoffeeMakerMachine) {
    send c, eDumpGrinds;
}

// stop all functions
fun EmergencyStop() { }

// internal events
event eReadyDoorOpened;

