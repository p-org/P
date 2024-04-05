event eOpenDoor assume 1;
event eCloseDoor assume 1;
event eResetDoor assert 1;
event eDoorOpened assert 1;
event eDoorClosed assert 1;
event eDoorStopped assert 1;
event eObjectDetected assert 1;
event eTimerFired assume 1;
event eOperationSuccess assert 1;
event eOperationFailure assert 1;
event eSendCommandToOpenDoor assume 1;
event eSendCommandToCloseDoor assume 1;
event eSendCommandToStopDoor assume 1;
event eSendCommandToResetDoor assume 1;
event eStartDoorCloseTimer assume 1;
event eStopDoorCloseTimer assume 1;
event eUnit assert 1;
event eStopTimerReturned assert 1;
event eObjectEncountered assert 1;


machine Elevator
sends eSendCommandToResetDoor, eSendCommandToOpenDoor, eStartDoorCloseTimer, eSendCommandToCloseDoor, eSendCommandToStopDoor, eStopDoorCloseTimer;
{
    var TimerV: Timer;
    var DoorV: machine;

    start state Init {
        entry {
            TimerV = new Timer(this);
            DoorV = new Door(this);
            raise eUnit;
        }

        on eUnit goto DoorClosed;
    }

    state DoorClosed {
        ignore eCloseDoor;

        entry {
            send DoorV, eSendCommandToResetDoor;
        }

        on eOpenDoor goto DoorOpening;
    }

    state DoorOpening {
        ignore eOpenDoor;
        defer eCloseDoor;

        entry {
            send DoorV, eSendCommandToOpenDoor;
        }

        on eDoorOpened goto DoorOpened;
    }

    state DoorOpened {
        defer eCloseDoor;

        entry {
            send DoorV,eSendCommandToResetDoor;
            send TimerV,eStartDoorCloseTimer;
        }

        on eTimerFired goto DoorOpenedOkToClose;
        on eStopTimerReturned goto DoorOpened;
        on eOpenDoor goto StoppingTimer;
    }

    state DoorOpenedOkToClose {
        defer eOpenDoor;

        entry {
            send TimerV,eStartDoorCloseTimer;
        }

        on eStopTimerReturned, eTimerFired goto DoorClosing;
        on eCloseDoor goto StoppingTimer;
    }

    state DoorClosing {
        defer eCloseDoor;

        entry {
            send DoorV,eSendCommandToCloseDoor;
        }

        on eOpenDoor goto StoppingDoor;
        on eDoorClosed goto DoorClosed;
        on eObjectDetected goto DoorOpening;
    }

    state StoppingDoor {
        defer eCloseDoor;
        ignore eOpenDoor, eObjectDetected;

        entry {
             send DoorV,eSendCommandToStopDoor;
        }

        on eDoorOpened goto DoorOpened;
        on eDoorClosed goto DoorClosed;
        on eDoorStopped goto DoorOpening;
    }

    state StoppingTimer {
        defer eOpenDoor, eCloseDoor, eObjectDetected;

        entry {
             send TimerV,eStopDoorCloseTimer;
        }

        on eOperationSuccess goto ReturnState;
        on eOperationFailure goto WaitingForTimer;
    }

    state WaitingForTimer {
        defer eOpenDoor, eCloseDoor, eObjectDetected;
        entry { }

        on eTimerFired goto ReturnState;
    }

    state ReturnState {
        entry {
            raise eStopTimerReturned;
        }
    }
}

machine Main
sends eOpenDoor, eCloseDoor;
{
    var ElevatorV : Elevator;

    start state Init {
        entry {
            ElevatorV = new Elevator();
            raise eUnit;
        }

        on eUnit goto Loop;
    }

    state Loop {
        entry {
            if ($) {
				send ElevatorV, eOpenDoor;
            } else if ($) {
               send ElevatorV,eCloseDoor;
            }
            raise eUnit;
        }

        on eUnit goto Loop;
    }
}

machine Door
sends eDoorOpened, eObjectDetected, eDoorClosed, eDoorStopped;
{
    var ElevatorV : Elevator;

    start state _Init {
	entry (payload: machine) { ElevatorV = payload as Elevator; raise eUnit; }
        on eUnit goto Init;
    }

    state Init {
        ignore eSendCommandToStopDoor, eSendCommandToResetDoor, eResetDoor;
        entry {}

        on eSendCommandToOpenDoor goto OpenDoor;
        on eSendCommandToCloseDoor goto ConsiderClosingDoor;
    }

    state OpenDoor {
        entry {
            send ElevatorV,eDoorOpened;
            raise eUnit;
        }

        on eUnit goto ResetDoor;
    }

    state ConsiderClosingDoor {
        entry {
            if ($) {
                raise eUnit;
            } else if ($) {
                raise eObjectEncountered;
            }
        }

        on eUnit goto CloseDoor;
        on eObjectEncountered goto ObjectEncountered;
        on eSendCommandToStopDoor goto StopDoor;
    }

    state ObjectEncountered {
        entry {
            send ElevatorV,eObjectDetected;
            raise eUnit;
        }

        on eUnit goto Init;
    }

    state CloseDoor {
        entry {
             send ElevatorV,eDoorClosed; raise eUnit;
        }

        on eUnit goto ResetDoor;
    }

    state StopDoor {
        entry {
            send ElevatorV,eDoorStopped; raise eUnit;
        }

        on eUnit goto OpenDoor;
    }

    state ResetDoor {
        ignore eSendCommandToOpenDoor, eSendCommandToCloseDoor,
            eSendCommandToStopDoor;
        entry { }

        on eSendCommandToResetDoor goto Init;
    }
}

machine Timer
sends eTimerFired, eOperationFailure, eOperationSuccess;
{
    var ElevatorV : Elevator;

    start state _Init {
	entry (payload: machine) { ElevatorV = payload as Elevator; raise eUnit; }
        on eUnit goto Init;
    }

    state Init {
        ignore eStopDoorCloseTimer;
        entry {}
        on eStartDoorCloseTimer goto TimerStarted;
    }

    state TimerStarted {
        defer eStartDoorCloseTimer;
        entry {
             if ($) { raise eUnit; }
        }
        on eUnit goto SendTimerFired;
        on eStopDoorCloseTimer goto ConsiderStopping;
    }

    state SendTimerFired {
        defer eStartDoorCloseTimer;
        entry {
            send ElevatorV,eTimerFired; raise eUnit;
        }
        on eUnit goto Init;
    }

    state ConsiderStopping {
        defer eStartDoorCloseTimer;
        entry {
            if ($) {
                send ElevatorV,eOperationFailure;
                send ElevatorV,eTimerFired;
            } else {
                send ElevatorV,eOperationSuccess;
            }
            raise eUnit;
        }
        on eUnit goto Init;
    }
}

module Elevator = { Elevator, Door, Timer };

module User = { Main };

implementation impl[main = Main]: (compose Elevator, User);

test testcase1[main = Main]: (compose Elevator, User);

