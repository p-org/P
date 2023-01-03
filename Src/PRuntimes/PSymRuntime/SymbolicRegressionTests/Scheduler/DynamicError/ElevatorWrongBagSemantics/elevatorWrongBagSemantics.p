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
event eUnit assert 1;
event eStartTimer;
event eObjectEncountered assert 1;

[bag]
machine Elevator {
    var TimerV, DoorV: machine;

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
        ignore eOpenDoor;

        entry {
            send DoorV, eSendCommandToResetDoor;
            send TimerV, eStartTimer;
        }

        on eTimerFired goto DoorOpenedOkToClose;
    }

    state DoorOpenedOkToClose {
        defer eOpenDoor;
        entry {
            if($) // automatically close door sometimes
                raise eCloseDoor;
        }
        on eCloseDoor goto DoorClosing;
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
}

machine Main {
    var ElevatorV : machine;
    var count: int;
    start state Init {
        entry {
            ElevatorV = new Elevator();
            goto Loop;
        }
    }

    state Loop {
        entry {
            if ($) {
				send ElevatorV, eOpenDoor;
            } else if ($) {
                send ElevatorV,eCloseDoor;
            }

            if(count == 2)
            {
                goto Done;
            }
            count = count + 1;
            goto Loop;
        }
    }

    state Done {}
}

[bag]
machine Door {
    var ElevatorV : machine;

    start state _Init {
	entry (payload: machine) { ElevatorV = payload; goto WaitForCommands; }

    }

    state WaitForCommands
    {
        ignore eSendCommandToStopDoor, eSendCommandToResetDoor, eResetDoor;
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
            send ElevatorV, eObjectDetected;
            goto WaitForCommands;
        }
    }

    state CloseDoor {
        entry {
             send ElevatorV,eDoorClosed;
             goto ResetDoor;
        }
    }

    state StopDoor {
        entry {
            send ElevatorV,eDoorStopped;
            goto OpenDoor;
        }
    }

    state ResetDoor {
        ignore eSendCommandToOpenDoor, eSendCommandToCloseDoor, eSendCommandToStopDoor;
        on eSendCommandToResetDoor goto WaitForCommands;
    }
}

[bag]
machine Timer {
    var creator: machine;
    start state Init {
        entry (client: machine) {
            creator = client;
        }
        on eStartTimer do {
            send creator, eTimerFired;
        }
    }
}
