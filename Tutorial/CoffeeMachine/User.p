event eDoorOpened;
event eDoorClosed;

model User 
receives ;
sends eDoorOpened, eDoorClosed;
{
    var coffeeMachineController: ICoffeeMachineController;
    var iter: int;

    fun JumpIfMoreWork() {
        if (iter > 0) {
            iter = iter - 1;
            goto Closed;
        }
    }

    start state Init {
        entry (x: (ICoffeeMachineController, int) { 
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