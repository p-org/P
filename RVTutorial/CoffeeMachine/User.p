event eDoorOpened;
event eDoorClosed;

machine DoorMachine 
receives ;
sends eDoorOpened, eDoorClosed;
{
    var coffeeMachineController: CoffeeMakerControllerMachine;
    var iter: int;

    fun JumpIfMoreWork() {
        if (iter > 0) {
            iter = iter - 1;
            goto Closed;
        }
    }

    start state Init {
        entry (x: (CoffeeMakerControllerMachine, int)) { 
            coffeeMachineController = x.0;
            iter = x.1;
            JumpIfMoreWork();
        }    
    }

    state Closed {
        entry {
            send coffeeMachineController, eDoorOpened;
            goto Opened;
        }
    }

    state Opened {
        entry {
            send coffeeMachineController, eDoorClosed;
            JumpIfMoreWork();
        }
    }
}

machine EspressoButtonMachine
receives ;
sends eEspressoButtonPressed;
{
    var coffeeMachineController: CoffeeMakerControllerMachine;
    var iter: int;

    fun JumpIfMoreWork() {
        if (iter > 0) {
            iter = iter - 1;
            goto Press;
        }
    }

    start state Init {
        entry (x: (CoffeeMakerControllerMachine, int)) { 
            coffeeMachineController = x.0;
            iter = x.1;
            JumpIfMoreWork();
        }    
    }

    state Press {
        entry {
            send coffeeMachineController, eEspressoButtonPressed;
            JumpIfMoreWork();
        }
    }
}

machine SteamerButtonMachine
receives ;
sends eSteamerButtonOn, eSteamerButtonOff;
{
    var coffeeMachineController: CoffeeMakerControllerMachine;
    var iter: int;

    fun JumpIfMoreWork() {
        if (iter > 0) {
            iter = iter - 1;
            goto Off;
        }
    }

    start state Init {
        entry (x: (CoffeeMakerControllerMachine, int)) { 
            coffeeMachineController = x.0;
            iter = x.1;
            JumpIfMoreWork();
        }    
    }

    state Off {
        entry {
            send coffeeMachineController, eSteamerButtonOn;
            goto On;
        }
    }

    state On {
        entry {
            send coffeeMachineController, eSteamerButtonOff;
            JumpIfMoreWork();
        }
    }
}
